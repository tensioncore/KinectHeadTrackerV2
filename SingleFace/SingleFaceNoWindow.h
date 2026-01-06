#pragma once

#include "Resource.h"
#include "FTHelper.h"
#include <FaceTrackLib.h>
#include <winsock2.h>

#include <atomic>
#include <mutex>
#include <thread>

typedef void(__stdcall *KINECTFACETRACKERCB)(
    double, double, double,
    double, double, double
);

class SingleFaceNoWindow
{
public:
    SingleFaceNoWindow();
    ~SingleFaceNoWindow();

    // Engine lifecycle (always-on tracking)
    BOOL Start();          // starts Kinect + FaceTrack only
    BOOL Stop();           // stops everything cleanly

    // Streaming control (explicit)
    // Existing: port-only (kept for compatibility; defaults to 127.0.0.1)
    BOOL StartStreaming(int port);

    // NEW (v2.1): port + target IP (latched at start; no hot-swap)
    BOOL StartStreaming(int port, const char* targetIp);

    void StopStreaming();

    // Callback wiring (explicit)
    void SetCallback(KINECTFACETRACKERCB cb);

    BOOL TiltCamera(int angleDelta);
    IFTImage* GetImage();
    BOOL IsReceivingData();

    BOOL IsStreaming() const { return m_streaming ? TRUE : FALSE; }

    // ----------------------------------
    // FaceTrack mutex accessors
    // ----------------------------------
    void EnterFtMutex();
    void LeaveFtMutex();

protected:
    static void FTHelperCallingBack(LPVOID lpParam);

private:
    // Face tracking
    FTHelper m_FTHelper;

    // Kinect config
    NUI_IMAGE_TYPE       m_depthType;
    NUI_IMAGE_TYPE       m_colorType;
    NUI_IMAGE_RESOLUTION m_depthRes;
    NUI_IMAGE_RESOLUTION m_colorRes;
    BOOL                 m_bNearMode;
    BOOL                 m_bSeatedSkeletonMode;

    // Streaming state
    bool        m_streaming;
    SOCKET      m_udpSocket;
    sockaddr_in m_targetAddr;

    // NEW: track WSA lifetime correctly (prevents leaks on failure paths)
    bool        m_wsaStarted;

    // Callback
    KINECTFACETRACKERCB m_callback;

    // Output buffer
    double m_FTData[6];

    // Internal helpers
    BOOL InitUDP(int port, const char* targetIp);   // NEW: target IP aware
    void ShutdownUDP();

    // -----------------------------
    // Auto-recovery / watchdog
    // -----------------------------
    std::atomic<bool> m_stopping{ false };
    std::atomic<bool> m_reinitInProgress{ false };

    // Timestamp tracking (ms ticks)
    std::atomic<ULONGLONG> m_lastGoodTick{ 0 };
    std::atomic<ULONGLONG> m_lastCallbackTick{ 0 };
    std::atomic<ULONGLONG> m_lastReinitTick{ 0 }; // basic backoff gate

    std::mutex m_ftMutex;
    std::thread m_watchdogThread;

    void WatchdogLoop();
    void ReinitFaceTrack(); // Stop/Init safely (engine-only)
};
