#include "stdafx.h"
#include "SingleFaceNoWindow.h"

#include <Windows.h>
#include <NuiApi.h>

#include <Ws2tcpip.h> // NEW: InetPtonA
#pragma comment(lib, "Ws2_32.lib")

SingleFaceNoWindow::SingleFaceNoWindow()
    : m_depthType(NUI_IMAGE_TYPE_DEPTH)
    , m_colorType(NUI_IMAGE_TYPE_COLOR)
    , m_depthRes(NUI_IMAGE_RESOLUTION_320x240)
    , m_colorRes(NUI_IMAGE_RESOLUTION_640x480)
    , m_bNearMode(FALSE)
    , m_bSeatedSkeletonMode(FALSE)
    , m_streaming(false)
    , m_udpSocket(INVALID_SOCKET)
    , m_callback(nullptr)
    , m_wsaStarted(false)
{
    ZeroMemory(&m_targetAddr, sizeof(m_targetAddr));
    ZeroMemory(m_FTData, sizeof(m_FTData));
}

SingleFaceNoWindow::~SingleFaceNoWindow()
{
    Stop();
}

void SingleFaceNoWindow::SetCallback(KINECTFACETRACKERCB cb)
{
    m_callback = cb;
}

// ----------------------------------
// Mutex accessors (used by C++/CLI layer)
// ----------------------------------
void SingleFaceNoWindow::EnterFtMutex()
{
    m_ftMutex.lock();
}

void SingleFaceNoWindow::LeaveFtMutex()
{
    m_ftMutex.unlock();
}

BOOL SingleFaceNoWindow::Start()
{
    m_stopping = false;
    m_reinitInProgress = false;

    ULONGLONG now = GetTickCount64();
    m_lastGoodTick = now;
    m_lastCallbackTick = now;
    m_lastReinitTick = 0;

    BOOL ok = SUCCEEDED(
        m_FTHelper.Init(
            NULL,
            FTHelperCallingBack,
            this,
            m_depthType,
            m_depthRes,
            m_bNearMode,
            TRUE,
            m_colorType,
            m_colorRes,
            m_bSeatedSkeletonMode
        )
    );

    // Start watchdog only if init succeeded
    if (ok)
    {
        if (!m_watchdogThread.joinable())
        {
            m_watchdogThread = std::thread(&SingleFaceNoWindow::WatchdogLoop, this);
        }
    }

    return ok;
}

BOOL SingleFaceNoWindow::Stop()
{
    // Make Stop idempotent-ish
    m_stopping = true;

    // Stop UDP first (transport is independent, but engine stop implies transport stop)
    StopStreaming();

    // Stop watchdog
    if (m_watchdogThread.joinable())
    {
        m_watchdogThread.join();
    }

    // Stop FaceTrack safely
    {
        std::lock_guard<std::mutex> lock(m_ftMutex);
        m_FTHelper.Stop();
    }

    return TRUE;
}

BOOL SingleFaceNoWindow::StartStreaming(int port)
{
    // Backward compatible: default to localhost
    return StartStreaming(port, "127.0.0.1");
}

BOOL SingleFaceNoWindow::StartStreaming(int port, const char* targetIp)
{
    if (m_streaming)
        return TRUE;

    if (!InitUDP(port, targetIp))
        return FALSE;

    m_streaming = true;
    return TRUE;
}

void SingleFaceNoWindow::StopStreaming()
{
    m_streaming = false;
    ShutdownUDP();
}

BOOL SingleFaceNoWindow::InitUDP(int port, const char* targetIp)
{
    // Normalize target IP (strict: no DNS here; transport-only)
    const char* ip = (targetIp && targetIp[0]) ? targetIp : "127.0.0.1";

    WSADATA wsa;
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
        return FALSE;

    m_wsaStarted = true;

    m_udpSocket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
    if (m_udpSocket == INVALID_SOCKET)
    {
        WSACleanup();
        m_wsaStarted = false;
        return FALSE;
    }

    ZeroMemory(&m_targetAddr, sizeof(m_targetAddr));
    m_targetAddr.sin_family = AF_INET;
    m_targetAddr.sin_port = htons(port);

    // Strict IPv4 parsing:
    // - Prefer InetPtonA (modern)
    // - Fallback to inet_addr for legacy inputs
    IN_ADDR addr;
    if (InetPtonA(AF_INET, ip, &addr) == 1)
    {
        m_targetAddr.sin_addr = addr;
    }
    else
    {
        // Fallback (legacy). If invalid, fail deterministically.
        unsigned long a = inet_addr(ip);
        if (a == INADDR_NONE)
        {
            closesocket(m_udpSocket);
            m_udpSocket = INVALID_SOCKET;

            WSACleanup();
            m_wsaStarted = false;
            return FALSE;
        }
        m_targetAddr.sin_addr.S_un.S_addr = a;
    }

    return TRUE;
}

void SingleFaceNoWindow::ShutdownUDP()
{
    if (m_udpSocket != INVALID_SOCKET)
    {
        closesocket(m_udpSocket);
        m_udpSocket = INVALID_SOCKET;
    }

    if (m_wsaStarted)
    {
        WSACleanup();
        m_wsaStarted = false;
    }
}

BOOL SingleFaceNoWindow::IsReceivingData()
{
    return m_FTHelper.IsReceivingData();
}

BOOL SingleFaceNoWindow::TiltCamera(int angleDelta)
{
    LONG angle;
    if (FAILED(NuiCameraElevationGetAngle(&angle)))
        return FALSE;

    angle += angleDelta;
    if (angle >= NUI_CAMERA_ELEVATION_MINIMUM &&
        angle <= NUI_CAMERA_ELEVATION_MAXIMUM)
    {
        NuiCameraElevationSetAngle(angle);
    }
    return TRUE;
}

IFTImage* SingleFaceNoWindow::GetImage()
{
    // IMPORTANT: No locking here.
    // The C++/CLI layer will hold m_ftMutex while it clones the frame.
    return m_FTHelper.GetColorImage();
}

// ----------------------------------
// Watchdog + recovery
// ----------------------------------
void SingleFaceNoWindow::WatchdogLoop()
{
    const ULONGLONG GOOD_POSE_TIMEOUT_MS   = 3000;
    const ULONGLONG NO_CALLBACK_TIMEOUT_MS = 5000;

    const ULONGLONG REINIT_MIN_INTERVAL_MS = 2000;

    while (!m_stopping)
    {
        Sleep(250);

        ULONGLONG now = GetTickCount64();
        ULONGLONG lastGood = m_lastGoodTick.load();
        ULONGLONG lastCb   = m_lastCallbackTick.load();
        ULONGLONG lastRe   = m_lastReinitTick.load();

        ULONGLONG sinceGood = (now >= lastGood) ? (now - lastGood) : 0;
        ULONGLONG sinceCb   = (now >= lastCb)   ? (now - lastCb)   : 0;
        ULONGLONG sinceRe   = (now >= lastRe)   ? (now - lastRe)   : 0;

        if (m_stopping) break;

        bool needReinit = (sinceCb > NO_CALLBACK_TIMEOUT_MS) || (sinceGood > GOOD_POSE_TIMEOUT_MS);
        bool canReinit  = !m_reinitInProgress && (sinceRe > REINIT_MIN_INTERVAL_MS);

        if (needReinit && canReinit)
        {
            m_reinitInProgress = true;
            m_lastReinitTick = now;

            ReinitFaceTrack();

            m_reinitInProgress = false;
        }
    }
}

void SingleFaceNoWindow::ReinitFaceTrack()
{
    if (m_stopping) return;

    std::lock_guard<std::mutex> lock(m_ftMutex);

    if (m_stopping) return;

    m_FTHelper.Stop();
    Sleep(250);

    if (m_stopping) return;

    m_FTHelper.Init(
        NULL,
        FTHelperCallingBack,
        this,
        m_depthType,
        m_depthRes,
        m_bNearMode,
        TRUE,
        m_colorType,
        m_colorRes,
        m_bSeatedSkeletonMode
    );

    ULONGLONG now = GetTickCount64();
    m_lastGoodTick = now;
    m_lastCallbackTick = now;
}

// ----------------------------------
// FaceTrack callback
// ----------------------------------
void SingleFaceNoWindow::FTHelperCallingBack(LPVOID lpParam)
{
    SingleFaceNoWindow* self = reinterpret_cast<SingleFaceNoWindow*>(lpParam);
    if (!self) return;

    self->m_lastCallbackTick = GetTickCount64();

    IFTResult* result = self->m_FTHelper.GetResult();
    if (!result || FAILED(result->GetStatus()))
    {
        return;
    }

    FLOAT scale;
    FLOAT rotationXYZ[3];
    FLOAT translationXYZ[3];

    result->Get3DPose(&scale, rotationXYZ, translationXYZ);

    self->m_lastGoodTick = GetTickCount64();

    self->m_FTData[0] = translationXYZ[0] * 100.0;
    self->m_FTData[1] = translationXYZ[1] * 100.0;
    self->m_FTData[2] = translationXYZ[2] * 100.0;

    self->m_FTData[3] = rotationXYZ[1]; // yaw
    self->m_FTData[4] = rotationXYZ[0]; // pitch
    self->m_FTData[5] = rotationXYZ[2]; // roll

    if (self->m_streaming && self->m_udpSocket != INVALID_SOCKET)
    {
        sendto(
            self->m_udpSocket,
            reinterpret_cast<const char*>(self->m_FTData),
            sizeof(self->m_FTData),
            0,
            reinterpret_cast<sockaddr*>(&self->m_targetAddr),
            sizeof(self->m_targetAddr)
        );
    }

    if (self->m_callback)
    {
        self->m_callback(
            self->m_FTData[0],
            self->m_FTData[1],
            self->m_FTData[2],
            self->m_FTData[3],
            self->m_FTData[4],
            self->m_FTData[5]
        );
    }
}
