## Dependencies

This project depends on Microsoft’s Kinect v1 runtime + face tracking components. These are **not bundled**
with this repository and must be installed separately.

### Required (Kinect v1)

#### 1) Kinect for Windows SDK v1.8
Includes the core APIs and developer headers/libraries used to build the project.
- Download: https://www.microsoft.com/en-us/download/details.aspx?id=40278

#### 2) Kinect for Windows Runtime v1.8
Includes the USB driver/runtime required for the sensor to function on the machine.
- Download: https://www.microsoft.com/en-us/download/details.aspx?id=40277

#### 3) Face Tracking SDK (FaceTrack)
Microsoft distributed Face Tracking as part of the **Kinect for Windows Developer Toolkit v1.8**
(includes Face Tracking SDK + tools/samples).
- Download Toolkit v1.8: https://www.microsoft.com/en-ca/download/details.aspx?id=40276

> Note: The Developer Toolkit requires the **SDK v1.8** to already be installed.

---

### Face Tracking Runtime Files (“all the FaceTrack files”)

The app relies on FaceTrack’s native runtime components installed by the Developer Toolkit / Face Tracking SDK.
These files are **not redistributed** here due to Microsoft licensing and are expected to exist on the system after installation.

Common examples:
- `FaceTrackLib.dll`
- `FaceTrackData.dll`
- FaceTrack model/data assets installed alongside the SDK/toolkit

---

## Common Issues & Fixes (real-world)

### “FaceTrackLib.dll not found” / “FaceTrackData.dll missing”
**Fix**
- Install the **Kinect for Windows Developer Toolkit v1.8** (includes Face Tracking SDK).
- Reboot if Windows keeps an old DLL load state.

### Tracking freezes after a while (no pose updates)
**What v2.1 does**
- v2.1 detects stalled tracking and **recovers gracefully** by reinitializing the tracking pipeline automatically.
**Fix (user action)**
- Use the UI to stop/start the engine if you want to force a clean re-init immediately.

### Build succeeds, but app crashes on launch with architecture mismatch (BadImageFormatException)
**Fix**
- Make sure your **EXE and the tracking DLL match architecture**.
- This repo supports both Win32 and x64, but the **recommended build path is x64** (matches the provided configs).

---

## Building (VS Code Terminal)

**Important:** Build the tracking engine first so the DLL exists before building the WinForms GUI.

> These examples assume you’re using a **Developer Command Prompt for Visual Studio**
> (or have MSBuild in PATH). Replace the placeholder paths with your local clone location.

### 1) Build the Tracking DLL FIRST (SingleFace — C++/CLI)
```bat
msbuild "C:\path\to\KinectHeadTrackerV2\SingleFace\SingleFace.vcxproj" /t:Build /p:Configuration=Release /p:Platform=x64
```

### Build the EXE second (important as it relies on the DLL to be built first)
```bat
msbuild "C:\path\to\KinectHeadTrackerV2\KinectHeadTracker\KinectHeadTracker.csproj" /t:Build  /p:Configuration=Release /p:Platform=x64
```
