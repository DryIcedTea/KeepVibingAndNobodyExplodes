# Keep Vibing And Nobody Explodes

## A [Buttplug.io](https://buttplug.io/) mod for Keep Talking And Nobody Explodes

This mod allows you to connect your Buttplug.io compatible toys to Keep Talking And Nobody Explodes!

## Features

### 🎮 Game Event Vibrations
- **Strike Events**: Feel the consequences when you make a mistake
- **Bomb Explosion**: Intense vibration when the bomb detonates
- **Module Interactions**: Satisfying feedback when interacting with modules

### 🔧 Module-Specific Haptic Feedback
The plugin supports vibration feedback for all vanilla Keep Talking and Nobody Explodes modules:

#### Standard Modules
- **Simple Wires**: Vibration when cutting wires
- **The Button**: Different intensities for press and release
- **Keypad/Symbols**: Vibration based on symbol presses
- **Simon Says**: Dynamic feedback for sequence interactions
- **Who's On First**: Vibrations for button presses
- **Memory**: Feedback for button interactions
- **Morse Code**: Feeback when scrolling through frequencies
- **Complicated Wires**: Feedback for wire cutting
- **Wire Sequences**: Multi-stage vibration for wire cutting and progression
- **Maze**: Navigation feedback
- **Passwords**: Letter selection feedback

#### Needy Modules & Miscellaneous
- **Capacitor Discharge**: Different vibrations for push/release actions
- **Knob**: Rotation feedback
- **Venting Gas**: Pressure release vibrations
- **Alarm Clock**: Vibration when alarm is buzzing

## Installation

### Prerequisites
1. **Keep Talking and Nobody Explodes**
2. **[BepInEx](https://github.com/BepInEx/BepInEx)**: 
3. **[Intiface Central](https://intiface.com/central/)**
4. Compatible device (see [Buttplug.io device support](https://iostindex.com/?filter0Availability=Available,DIY&filter1ButtplugSupport=4))

### Setup Steps
1. Download the latest release from the releases page
2. Pleace the BepInEx folder in the same folder as your game exe file.
3. Install and run **Intiface Central**
4. Connect your haptic device and ensure it's recognized in Intiface
5. Start the game - the plugin will automatically attempt to connect

## Configuration

The plugin creates a detailed configuration file at `BepInEx/config/dryicedmatcha.keepvibing.cfg` with the following options:

### Connection Settings
- **Intiface Host**: Server address (default: `127.0.0.1`)
- **Intiface Port**: Server port (default: `12345`)

### General Game Events
- **Strike Vibration**: Enable/disable and intensity (default: 70%)
- **Explosion Vibration**: Enable/disable and intensity (default: 100%)
- **Module Solve Vibration**: Enable/disable and intensity (default: 100%)

### Per-Module Settings
Each supported module has individual settings:
- **Enable/Disable**: Toggle vibration for specific modules
- **Intensity**: Adjust vibration strength (0.1 = 10%, 1.0 = 100%)

## Feedback
If you have any feedback, feel free to open an issue, [tweet](https://twitter.com/DryIcedMatcha) at me or message me on discord. You'll find me in the [buttplug.io discord](https://discord.buttplug.io/)!