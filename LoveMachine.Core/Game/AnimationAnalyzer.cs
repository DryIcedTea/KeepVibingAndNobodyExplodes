﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LoveMachine.Core.Common;
using LoveMachine.Core.Config;
using LoveMachine.Core.NonPortable;
using UnityEngine;

namespace LoveMachine.Core.Game
{
    internal class AnimationAnalyzer : CoroutineHandler
    {
        private readonly Dictionary<TrackingKey, Result> resultCache =
            new Dictionary<TrackingKey, Result>();

        private readonly HashSet<TrackingKey> keysInProgress = new HashSet<TrackingKey>();

        private GameAdapter game;

        private void Start()
        {
            game = GetComponent<GameAdapter>();
            game.OnHEnded += (s, a) => StopAnalyze();
        }

        [HideFromIl2Cpp]
        public bool TryGetCurrentStrokeInfo(TrackingKey trackingKey, float normalizedTime,
            out StrokeInfo strokeInfo)
        {
            if (!TryGetResult(trackingKey, out var result) || result.StrokeDelimiters.Length == 0)
            {
                strokeInfo = default;
                return false;
            }
            var delimiters = result.StrokeDelimiters;
            float animTimeSecs = game.GetAnimationTimeSecs(trackingKey.GirlIndex);
            int delimIndex = Enumerable.Range(0, delimiters.Length)
                .Where(i => delimiters[i] <= normalizedTime % 1f)
                .DefaultIfEmpty(delimiters.Length - 1)
                .Last();
            float start = delimiters[delimIndex];
            float end = delimIndex == delimiters.Length - 1
                ? delimiters[0] + 1f
                : delimiters[delimIndex + 1];
            if (normalizedTime % 1f < start)
            {
                start -= 1f;
                end -= 1f;
            }
            float normalizedStrokeDuration = end - start;
            strokeInfo = new StrokeInfo
            {
                Amplitude = result.Amplitude,
                DurationSecs = animTimeSecs * normalizedStrokeDuration,
                Completion = Mathf.InverseLerp(start, end, normalizedTime % 1f),
                Pattern = result.Patterns[delimIndex]
            };
            return true;
        }
        
        [HideFromIl2Cpp]
        private bool TryGetResult(TrackingKey trackingKey, out Result result)
        {
            try
            {
                if (resultCache.TryGetValue(trackingKey, out result))
                {
                    return true;
                }
                HandleCoroutine(TryAnalyzeAnimation(trackingKey), suppressExceptions: true);
                return false;
            }
            catch (Exception e)
            {
                Logger.LogError($"Error while trying to get wave info: {e}");
                result = default;
                return false;
            }
        }

        private void StopAnalyze()
        {
            StopAllCoroutines();
            resultCache.Clear();
        }

        private IEnumerator TryAnalyzeAnimation(TrackingKey trackingKey)
        {
            if (resultCache.ContainsKey(trackingKey) || keysInProgress.Contains(trackingKey))
            {
                yield break;
            }
            keysInProgress.Add(trackingKey);
            Logger.LogDebug("New animation playing, starting to analyze.");
            yield return HandleCoroutine(AnalyzeAnimation(trackingKey), suppressExceptions: true);
            keysInProgress.Remove(trackingKey);
        }

        private IEnumerator AnalyzeAnimation(TrackingKey trackingKey)
        {
            int girlIndex = trackingKey.GirlIndex;
            var penisBases = game.PenisBases;
            var femaleBones = game.GetFemaleBones(girlIndex);
            var trackedFemaleBones = trackingKey.Bone == Bone.Auto
                ? femaleBones
                : new Dictionary<Bone, Transform>
                {
                    { trackingKey.Bone, femaleBones[trackingKey.Bone] }
                };
            string pose = trackingKey.Pose;
            yield return HandleCoroutine(game.WaitAfterPoseChange());
            var samples = new List<Sample>();
            game.GetAnimState(girlIndex, out float startTime, out _, out _);
            float currentTime = startTime;
            while (currentTime - 1f < startTime)
            {
                yield return new WaitForEndOfFrame();
                game.GetAnimState(girlIndex, out currentTime, out _, out _);
                var newSamples = trackedFemaleBones
                    .SelectMany(entry => penisBases, (entry, penisBase) => new Sample
                    {
                        Bone = entry.Key,
                        PenisBase = penisBase,
                        Time = currentTime,
                        MalePos = penisBase.position,
                        FemalePos = entry.Value.position,
                        MaleRot = penisBase.rotation,
                        FemaleRot = entry.Value.rotation
                    });
                samples.AddRange(newSamples);
                if (pose != game.GetPose(girlIndex) || currentTime < startTime)
                {
                    Logger.LogWarning($"Pose {pose} interrupted; canceling analysis.");
                    yield break;
                }
            }
            var keys = trackedFemaleBones
                .Select(entry => { var key = trackingKey; key.Bone = entry.Key; return key; });
            var results = samples
                .GroupBy(sample => sample.PenisBase)
                .Select(group => keys.ToDictionary(key => key, key => EvaluateSamples(group, key)))
                .ToArray();
            var preferredResults = keys.ToDictionary(
                key => key,
                key => results.Maximize(dict => dict[key].Preference)[key]);
            var bestKey = keys.Maximize(key => preferredResults[key].Preference);
            var autoKey = bestKey;
            autoKey.Bone = Bone.Auto;
            preferredResults[autoKey] = preferredResults[bestKey];
            foreach (var kvp in preferredResults)
            {
                resultCache[kvp.Key] = kvp.Value;
            }
            Logger.LogInfo($"Analysis of pose {pose} completed. " +
                $"{samples.Count / trackedFemaleBones.Count} frames inspected.");
        }

        private Result EvaluateSamples(IEnumerable<Sample> samples, TrackingKey trackingKey)
        {
            samples = samples.Where(sample => sample.Bone == trackingKey.Bone).ToArray();
            var femaleCenter = samples
                .Select(sample => sample.FemalePos)
                .Aggregate(Vector3.zero, (acc, pos) => acc + pos / samples.Count());
            var maleFarthest = samples
                .Maximize(sample => (sample.MalePos - femaleCenter).sqrMagnitude)
                .MalePos;
            var femaleFarthest = samples
                .Maximize(sample => (sample.FemalePos - maleFarthest).sqrMagnitude)
                .MalePos;
            Vector3 GetRelativePos(Sample sample) =>
                GetRelativePosition(sample, trackingKey.POV, maleFarthest, femaleFarthest);
            var relativePositions = samples.Select(sample => GetRelativePos(sample)).ToArray();
            var crest = relativePositions.Maximize(pos => pos.magnitude);
            var trough = relativePositions.Maximize(pos => (pos - crest).magnitude);
            var longestAxis = crest - trough;
            Vector3 GetAxis(Sample sample) => this.GetAxis(sample, trackingKey.Axis, longestAxis);
            float GetDistance(Sample sample) =>
                Vector3.Project(GetRelativePos(sample) - trough, GetAxis(sample)).magnitude;
            float GetTwist(Sample sample) =>
                RotationToTwist(GetRelativeRotation(sample, trackingKey.POV), GetAxis(sample));
            var nodes = samples
                .Select(sample => new Node 
                {
                    Time = sample.Time,
                    Position = trackingKey.MovementType == MovementType.Linear
                        ? GetDistance(sample)
                        : GetTwist(sample)
                })
                .ToArray();
            if (trackingKey.MovementType == MovementType.Rotation)
            {
                nodes = NormalizeAngles(nodes).ToArray();
            }
            float amplitude = nodes.Max(node => node.Position) - nodes.Min(node => node.Position);
            var delimiters = GetStrokeDelimiters(nodes, amplitude * game.MinStrokeLength);
            int strokeCount = delimiters.Length;
            var patterns = Enumerable.Range(0, strokeCount)
                .Select(i => GetPattern(
                    nodes,
                    delimiters[i],
                    i == strokeCount - 1 ? delimiters[0] + 1f : delimiters[i + 1]))
                .ToArray();
            return new Result
            {
                StrokeDelimiters = delimiters,
                Patterns = patterns,
                Amplitude = amplitude,
                // Prefer bones that are close and move a lot. Being close is more important.
                Preference = amplitude == 0
                    ? float.NegativeInfinity
                    : -Mathf.Pow(trough.magnitude, 3f) / amplitude
            };
        }

        private Vector3 GetAxis(Sample sample, Axis axis, Vector3 longest)
        {
            switch (axis)
            {
                case Axis.Longest:
                    return longest;

                case Axis.X:
                    return sample.MaleRot * Vector3.right;

                case Axis.Y:
                    return sample.MaleRot * Vector3.up;

                case Axis.Z:
                    return sample.MaleRot * Vector3.forward;
            }
            throw new Exception("unreachable");
        }

        private Vector3 GetRelativePosition(Sample sample, POV pov, Vector3 male, Vector3 female)
        {
            switch(pov)
            {
                case POV.Balanced:
                    return sample.MalePos - sample.FemalePos;

                case POV.Male:
                    return male - sample.FemalePos;

                case POV.Female:
                    return sample.MalePos - female;
            }
            throw new Exception("unreachable");
        }

        private Quaternion GetRelativeRotation(Sample sample, POV pov)
        {
            switch (pov) {
                case POV.Balanced:
                    return sample.MaleRot * Quaternion.Inverse(sample.FemaleRot);

                case POV.Male:
                    return sample.FemaleRot;

                case POV.Female:
                    return sample.MaleRot;
            }
            throw new Exception("unreachable");
        }

        public static float RotationToTwist(Quaternion rotation, Vector3 axis)
        {
            (rotation * Quaternion.FromToRotation(rotation * axis, axis))
                .ToAngleAxis(out float angle, out _);
            return angle;
        }

        private IEnumerable<Node> NormalizeAngles(IEnumerable<Node> nodes)
        {
            var normalized = new List<Node> { nodes.First() };
            foreach (var node in nodes.Skip(1))
            {
                float lastAngle = normalized.Last().Position;
                float angle = node.Position - lastAngle;
                angle = (angle + 360f + 180f) % 360f - 180f;
                normalized.Add(new Node
                {
                    Time = node.Time,
                    Position = lastAngle + angle
                });
            }
            return normalized;
        }

        private static float[] GetStrokeDelimiters(IEnumerable<Node> nodes, float tolerance)
        {
            var edge = nodes.Minimize(node => node.Position);
            int index = nodes.ToList().IndexOf(edge);
            nodes = nodes.Skip(index).Concat(nodes.Take(index));
            int direction = 1;
            var edges = new List<Node>();
            foreach (var node in nodes)
            {
                float delta = edge.Position - node.Position;
                edge = Math.Sign(delta) == direction ? node : edge;
                if (Mathf.Abs(delta) > tolerance)
                {
                    edges.Add(edge);
                    edge = node;
                    direction *= -1;
                }
            }
            return edges.Where((node, i) => i % 2 == 0)
                .Select(node => node.Time % 1f)
                .OrderBy(time => time)
                .ToArray();
        }
        
        private static float[] GetPattern(IEnumerable<Node> nodes, float start, float end)
        {
            float min = nodes.Min(node => node.Position);
            float max = nodes.Max(node => node.Position);
            return nodes
                .Where(node => (node.Time - start + 1f) % 1f < end - start)
                .OrderBy(node => (node.Time - start + 1f) % 1f)
                .Select(node => Mathf.InverseLerp(min, max, value: node.Position))
                .ToArray();
        }

        private struct Sample
        {
            public Bone Bone { get; set; }
            public Transform PenisBase { get; set; }
            public float Time { get; set; }
            public Vector3 MalePos { get; set; }
            public Vector3 FemalePos { get; set; }
            public Quaternion MaleRot { get; set; }
            public Quaternion FemaleRot { get; set; }
        }

        private struct Node
        {
            public float Time { get; set; }
            public float Position { get; set; }
        }

        private struct Result
        {
            public float[] StrokeDelimiters { get; set; }
            public float[][] Patterns { get; set; } // delimiter index -> pattern
            public float Amplitude { get; set; }
            public float Preference { get; set; } // larger is better
        }
    }
}