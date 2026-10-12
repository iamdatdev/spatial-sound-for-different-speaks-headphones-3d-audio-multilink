# Double Headphones 3D — starter

A Windows 11 C# starter that captures the default Windows playback endpoint using WASAPI loopback, applies a basic stereo-width effect, and duplicates the processed audio to two selected playback devices.

## Requirements
- Windows 11 x64
- .NET 8 SDK
- Two Bluetooth headphones paired and connected in Windows
- Visual Studio Code (C# Dev Kit is helpful but not required)

## Build and run
Open PowerShell in this folder:

```powershell
dotnet --info
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

The app lists active playback endpoints. Select the two Bluetooth headphones by their numbers. Press `S` to toggle the starter DSP, or `Q` to quit.

## Important setup detail
This starter captures the **Windows default playback endpoint**. Keep that default endpoint different from the two selected outputs to avoid a loopback feedback loop. For a first test, leave Windows' default output set to the laptop speakers and select both Bluetooth headphones in this app. You may hear the laptop speakers as well.

## What the spatial effect is (and is not)
`StereoSpatializer.cs` is a lightweight stereo-width/crossfeed demonstration so the complete audio-routing pipeline is easy to build and test. It is **not a true HRTF-based 3D renderer**. True binaural spatialization generally needs appropriate HRTF impulse responses and convolution, or a spatial-audio engine that can position audio sources. Arbitrary stereo system audio does not contain enough information to recover accurate 3D positions in every case.

## Current limitations
- Console UI only; no installer or tray UI yet.
- The effect processes 32-bit float stereo loopback audio. Other formats are passed through unchanged.
- Bluetooth endpoints can have different latency. This starter does not yet measure or compensate per-device delay.
- Windows shared-mode audio format conversion and Bluetooth codec behavior vary by device/driver.
- Do not select the same endpoint used for loopback capture as an output; it can create feedback.
- This is a prototype, not production audio software.

## Suggested next engineering steps
1. Add a real HRTF convolution DSP with a clear license for the impulse-response dataset.
2. Add per-output delay measurement and compensation.
3. Add a WPF/WinUI UI with endpoint refresh and per-device volume.
4. Add error handling for device disconnect/reconnect and audio format changes.
5. Consider a virtual audio endpoint if you want the app to become the Windows default output without routing through laptop speakers.


## Build a Windows EXE without installing .NET locally

This project includes `.github/workflows/build-windows.yml`. To use it:

1. Create a GitHub repository and upload the contents of this folder (the files inside `DoubleHeadphones3D`, not the outer folder).
2. Open the repository's **Actions** tab.
3. Select **Build Windows EXE** and click **Run workflow** (or push to `main`/`master`).
4. When the workflow finishes, open the completed run and download the `DoubleHeadphones3D-win-x64` artifact.
5. Extract the artifact. It should contain `DoubleHeadphones3D.exe` plus any published supporting files.

The workflow builds a self-contained Windows x64 app, so the target PC does not need the .NET runtime installed. This does not mean the code has been tested on your particular laptop; first run it as a prototype and inspect any errors.
