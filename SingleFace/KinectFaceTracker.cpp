#include "stdafx.h"
#include "KinectFaceTracker.h"
#include "SingleFaceNoWindow.h"

#include <vcclr.h>
#using <System.Drawing.dll>

#include <msclr/marshal_cppstd.h>   // NEW: String^ -> std::string
#include <NuiApi.h>                 // NEW: NuiGetSensorCount

using namespace System;
using namespace System::Drawing;
using namespace System::Runtime::InteropServices;

KinectFaceTracker::KinectFaceTracker()
    : app(new SingleFaceNoWindow())
    , m_onReceive(nullptr)
    , receiveKinectDataCallback(nullptr)
{
}

KinectFaceTracker::~KinectFaceTracker()
{
    this->Stop();
    if (app)
    {
        delete app;
        app = nullptr;
    }
}

KinectFaceTracker::!KinectFaceTracker()
{
    if (app)
    {
        app->Stop();
        delete app;
        app = nullptr;
    }
}

void KinectFaceTracker::Subscribe(ReceiveKinectData^ cb)
{
    if (cb == nullptr) return;
    m_onReceive = safe_cast<ReceiveKinectData^>(Delegate::Combine(m_onReceive, cb));
}

void KinectFaceTracker::Unsubscribe(ReceiveKinectData^ cb)
{
    if (cb == nullptr) return;
    m_onReceive = safe_cast<ReceiveKinectData^>(Delegate::Remove(m_onReceive, cb));
}

bool KinectFaceTracker::Start()
{
    return StartWithCallback();
}

bool KinectFaceTracker::StartWithCallback()
{
    if (!app) return false;

    // Wire native->managed callback thunk once
    if (receiveKinectDataCallback == nullptr)
    {
        receiveKinectDataCallback = gcnew ReceiveKinectData(this, &KinectFaceTracker::ReceiveData);
        IntPtr ptr = Marshal::GetFunctionPointerForDelegate(receiveKinectDataCallback);
        KINECTFACETRACKERCB cb = static_cast<KINECTFACETRACKERCB>(ptr.ToPointer());

        app->SetCallback(cb); // NO UDP here
    }

    return app->Start() != 0;
}

bool KinectFaceTracker::StartStreaming(int port)
{
    if (!app) return false;
    return app->StartStreaming(port) != 0;
}

// NEW (v2.1): StartStreaming with target IP + port.
// Applies only at stream start; does NOT hot-swap while streaming.
bool KinectFaceTracker::StartStreaming(int port, System::String^ targetIp)
{
    if (!app) return false;

    try
    {
        // If empty/null, fall back to existing port-only call (backward compatible)
        if (targetIp == nullptr)
            return app->StartStreaming(port) != 0;

        System::String^ trimmed = targetIp->Trim();
        if (System::String::IsNullOrEmpty(trimmed))
            return app->StartStreaming(port) != 0;

        std::string ip = msclr::interop::marshal_as<std::string>(trimmed);

        // IMPORTANT: requires native overload StartStreaming(int, const char*)
        return app->StartStreaming(port, ip.c_str()) != 0;
    }
    catch (...)
    {
        return false;
    }
}

void KinectFaceTracker::StopStreaming()
{
    if (!app) return;
    app->StopStreaming();
}

void KinectFaceTracker::Stop()
{
    if (!app) return;

    app->Stop();

    // Release thunk
    if (receiveKinectDataCallback)
    {
        delete receiveKinectDataCallback;
        receiveKinectDataCallback = nullptr;
    }
}

Image^ KinectFaceTracker::GetImage()
{
    if (!app) return nullptr;

    bool locked = false;
    Bitmap^ wrapped = nullptr;
    Bitmap^ safeCopy = nullptr;

    try
    {
        app->EnterFtMutex();
        locked = true;

        IFTImage* img = app->GetImage();
        if (!img) return nullptr;

        int w = (int)img->GetWidth();
        int h = (int)img->GetHeight();
        int stride = (int)img->GetStride();
        IntPtr buf(img->GetBuffer());

        wrapped = gcnew Bitmap(w, h, stride, Imaging::PixelFormat::Format32bppRgb, buf);

        try {
            safeCopy = wrapped->Clone(System::Drawing::Rectangle(0, 0, w, h),
                                      Imaging::PixelFormat::Format32bppRgb);
        } catch (...) {
            safeCopy = nullptr;
        }
    }
    catch (...)
    {
        safeCopy = nullptr;
    }
    finally
    {
        if (wrapped != nullptr) { try { delete wrapped; } catch (...) {} wrapped = nullptr; }
        if (locked) { try { app->LeaveFtMutex(); } catch (...) {} }
    }
    return safeCopy; // <- guaranteed return fixes C4715
}

bool KinectFaceTracker::TiltCamera(int angleDelta)
{
    if (!app) return false;
    return app->TiltCamera(angleDelta) != 0;
}

bool KinectFaceTracker::IsReceivingData()
{
    if (!app) return false;
    return app->IsReceivingData() != 0;
}

bool KinectFaceTracker::IsStreaming()
{
    if (!app) return false;
    return app->IsStreaming() != 0;
}

// NEW (v2.1 stabilization): lightweight probe (no engine start required)
bool KinectFaceTracker::IsKinectConnected()
{
    int count = 0;
    HRESULT hr = NuiGetSensorCount(&count);
    return (SUCCEEDED(hr) && count > 0);
}

void KinectFaceTracker::ReceiveData(double x, double y, double z, double yaw, double pitch, double roll)
{
    ReceiveKinectData^ cb = m_onReceive;
    if (cb != nullptr)
    {
        cb(x, y, z, yaw, pitch, roll);
    }
}
