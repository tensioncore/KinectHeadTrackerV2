## Dependencies

This project depends on Microsoft’s Kinect v1 runtime + face tracking components. These are **not bundled** with this repository and must be installed separately.

### Required (Kinect v1)

#### 1) Kinect for Windows SDK v1.8
Includes the core APIs and developer headers/libraries used to build the project.

- Download: https://www.microsoft.com/en-us/download/details.aspx?id=40278

#### 2) Kinect for Windows Runtime v1.8
Includes the USB driver/runtime required for the sensor to function on the machine.

- Download: https://www.microsoft.com/en-us/download/details.aspx?id=40277

#### 3) Face Tracking SDK (FaceTrack)
Microsoft distributed Face Tracking as part of the **Kinect for Windows Developer Toolkit v1.8** (includes Face Tracking SDK + tools/samples).

- Download Toolkit v1.8: https://www.microsoft.com/en-ca/download/details.aspx?id=40276

> Note: The Developer Toolkit requires the **SDK v1.8** to already be installed.

---

### FaceTrack Runtime Files (“all the FaceTrack files”)

The app relies on FaceTrack’s native runtime components installed by the Developer Toolkit / Face Tracking SDK.
These files are **not redistributed** here due to Microsoft licensing and are expected to exist on the system after installation.

Common examples (names can vary by install/version):
- `FaceTrackLib.dll`
- `FaceTrackData.dll`
- FaceTrack model/data assets installed alongside the SDK/toolkit

---

## Common Issues & Fixes (real-world)

### “FaceTrackLib.dll not found” / “FaceTrackData.dll missing” (or similar)
**Fix**
- Install the **Kinect for Windows Developer Toolkit v1.8** (includes Face Tracking SDK).
- Reboot if Windows keeps an old DLL load state.

### Tracking freezes after a while (no pose updates)
**What v2.1 does**
- v2.1 is designed to detect stalled tracking and **recover gracefully** by reinitializing the tracking pipeline automatically.
**Fix (user action)**
- Use the UI to stop/start the engine if you want to force a clean re-init immediately.

### Build succeeds, but app crashes on launch with architecture mismatch (BadImageFormatException)
**Fix**
- Make sure your **EXE and the tracking DLL match architecture** (commonly **x86/Win32** for Kinect v1 + FaceTrack setups).
- If you build the DLL as Win32, build the EXE as x86 as well.

---

## Building (MSBuild)

> These examples assume you’re using a **Developer Command Prompt for Visual Studio** (or have MSBuild in PATH).  
> Replace the placeholder paths with your local clone location.

### Build the Tracking DLL (C++/CLI / VCXPROJ)
```bat
msbuild "C:\Path\To\Repo\SingleFace\KinectFaceTracker.vcxproj" ^
  /t:Build ^
  /p:Configuration=Release ^
  /p:Platform=Win32
