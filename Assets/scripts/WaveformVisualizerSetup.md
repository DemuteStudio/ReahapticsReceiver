# Haptic Waveform Visualizer Setup Guide

## Overview
The waveform visualizer displays your haptic amplitude and frequency data as animated bars that sync with playback.

## Setup Instructions

### 1. Create the Visualizer UI

1. **Create a Panel** in your Canvas:
   - Right-click in Hierarchy → UI → Panel
   - Name it "WaveformVisualizer"
   - Set RectTransform to desired size (e.g., 800x300)

2. **Add the Visualizer Component**:
   - Select the Panel
   - Add Component → `HapticWaveformVisualizer`

3. **Configure Settings**:
   - **Bar Count**: 64 (more bars = smoother waveform)
   - **Amplitude Color**: Light blue (default)
   - **Frequency Color**: Orange (default)
   - **Max Height**: Adjust based on panel height
   - **Show Playback Cursor**: Enable to see current playback position

### 2. Create Integration

1. **Add Integration Component**:
   - Create an empty GameObject named "VisualizerManager"
   - Add Component → `HapticVisualizerIntegration`

2. **Assign References**:
   - Drag the WaveformVisualizer Panel to the `Visualizer` field
   - Enable `Auto Play On Load` if you want it to animate immediately
   - Enable `Sync With Playback` to sync cursor with haptic playback

### 3. Connect to Haptic Receiver

1. **Find your OSCReaperContinuesReceiver**:
   - Select the GameObject with `OSCReaperContinuesReceiver` component

2. **Assign Visualizer**:
   - In the Inspector, find the `Visualizer` section
   - Drag the VisualizerManager to the `Visualizer Integration` field

### 4. That's it!

The visualizer will now:
- Load haptic data automatically when received
- Display amplitude (blue bars) and frequency (orange bars)
- Show a playback cursor that moves with the haptic playback
- Animate smoothly during playback

## Customization Options

### Colors
- **Amplitude Color**: The background waveform (typically amplitude)
- **Frequency Color**: The foreground waveform (typically frequency)
- **Cursor Color**: The playback position indicator

### Animation
- **Animation Speed**: How fast the cursor moves (1.0 = normal speed)
- **Smooth Transitions**: Enable for smooth interpolation between values
- **Smooth Speed**: How quickly bars lerp to target heights

### Visual Style
- **Bar Count**: More bars = smoother but more resource intensive
- **Bar Width**: Width of each bar in pixels
- **Bar Spacing**: Gap between bars
- **Max Height**: Maximum bar height in pixels

## Advanced Usage

### Manual Control
```csharp
// Get reference to visualizer
HapticVisualizerIntegration viz = GetComponent<HapticVisualizerIntegration>();

// Load custom data
viz.LoadHapticData(jsonString);

// Control playback
viz.StartTracking();
viz.StopTracking();
viz.UpdateTime(timeInSeconds);
viz.Clear();
```

### Styling Tips
- Use semi-transparent colors for a layered glass effect
- Increase bar count for smoother waveforms (at performance cost)
- Add a background panel with blur for depth
- Use gradient images instead of solid colors for bars

## Performance Notes
- More bars = more GameObjects = higher overhead
- For mobile, keep bar count around 32-48
- For desktop, 64-128 bars works well
- Disable smooth transitions for better performance
