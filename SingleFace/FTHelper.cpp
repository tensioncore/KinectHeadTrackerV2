#include "StdAfx.h"
#include "FTHelper.h"
#include "Visualize.h"

#ifdef SAMPLE_OPTIONS
#include "Options.h"
#else
PVOID _opt = NULL;
#endif

static void FTLogHresult(const wchar_t* step, HRESULT hr)
{
    WCHAR buf[512];
    wsprintf(buf, L"[SingleFace] %s failed. hr=0x%08X\r\n", step, hr);
    OutputDebugStringW(buf);
}

FTHelper::FTHelper()
{
    m_pFaceTracker = 0;
    m_hWnd = NULL;
    m_pFTResult = NULL;
    m_colorImage = NULL;
    m_depthImage = NULL;
    m_ApplicationIsRunning = false;
    m_LastTrackSucceeded = false;
    m_CallBack = NULL;
    m_XCenterFace = 0;
    m_YCenterFace = 0;
    m_hFaceTrackingThread = NULL;
    m_DrawMask = FALSE; // does this save a lot of cycles?
    m_depthType = NUI_IMAGE_TYPE_DEPTH;
    m_depthRes = NUI_IMAGE_RESOLUTION_INVALID;
    m_bNearMode = FALSE;
    m_bFallbackToDefault = FALSE;
    m_colorType = NUI_IMAGE_TYPE_COLOR;
    m_colorRes = NUI_IMAGE_RESOLUTION_INVALID;

    // v2.1 stabilization
    m_hInitDoneEvent = CreateEventW(NULL, TRUE, FALSE, NULL);
    m_initHr = E_FAIL;

    m_KinectSensorPresent = FALSE;
}

FTHelper::~FTHelper()
{
    Stop();

    if (m_hInitDoneEvent)
    {
        CloseHandle(m_hInitDoneEvent);
        m_hInitDoneEvent = NULL;
    }
}

HRESULT FTHelper::Init(HWND hWnd, FTHelperCallBack callBack, PVOID callBackParam,
    NUI_IMAGE_TYPE depthType, NUI_IMAGE_RESOLUTION depthRes, BOOL bNearMode, BOOL bFallbackToDefault,
    NUI_IMAGE_TYPE colorType, NUI_IMAGE_RESOLUTION colorRes, BOOL bSeatedSkeletonMode)
{
    if (!callBack)
    {
        return E_INVALIDARG;
    }

    m_hWnd = hWnd;
    m_CallBack = callBack;
    m_CallBackParam = callBackParam;
    m_ApplicationIsRunning = true;

    m_depthType = depthType;
    m_depthRes = depthRes;
    m_bNearMode = bNearMode;
    m_bFallbackToDefault = bFallbackToDefault;
    m_bSeatedSkeletonMode = bSeatedSkeletonMode;
    m_colorType = colorType;
    m_colorRes = colorRes;

    // v2.1 stabilization: reset init handshake
    m_initHr = E_PENDING;
    if (m_hInitDoneEvent)
    {
        ResetEvent(m_hInitDoneEvent);
    }

    m_hFaceTrackingThread = CreateThread(NULL, 0, FaceTrackingStaticThread, (PVOID)this, 0, 0);
    if (!m_hFaceTrackingThread)
    {
        m_ApplicationIsRunning = false;
        return E_FAIL;
    }

    SetThreadPriority(m_hFaceTrackingThread, THREAD_PRIORITY_LOWEST);

    // v2.1 stabilization:
    // Wait briefly for Kinect init to succeed/fail. If no Kinect is connected,
    // we return a failure HRESULT so SingleFaceNoWindow::Start() returns FALSE
    // and the watchdog thread never starts (no spam, no tight loops).
    if (m_hInitDoneEvent)
    {
        DWORD wait = WaitForSingleObject(m_hInitDoneEvent, 2000); // 2s
        if (wait == WAIT_OBJECT_0 && FAILED(m_initHr))
        {
            // Thread will have exited; join + cleanup handle here.
            m_ApplicationIsRunning = false;

            WaitForSingleObject(m_hFaceTrackingThread, INFINITE);
            CloseHandle(m_hFaceTrackingThread);
            m_hFaceTrackingThread = NULL;

            return m_initHr;
        }
    }

    return S_OK;
}

HRESULT FTHelper::Stop()
{
    m_ApplicationIsRunning = false;

    // ensure Init() can’t hang if Stop happens during init
    if (m_hInitDoneEvent)
    {
        SetEvent(m_hInitDoneEvent);
    }

    // Clean join + close thread handle (prevents leaks/races)
    if (m_hFaceTrackingThread)
    {
        WaitForSingleObject(m_hFaceTrackingThread, INFINITE);
        CloseHandle(m_hFaceTrackingThread);
        m_hFaceTrackingThread = NULL;
    }

    m_LastTrackSucceeded = FALSE;
    return S_OK;
}

BOOL FTHelper::SubmitFaceTrackingResult(IFTResult* pResult)
{
    if (pResult != NULL && SUCCEEDED(pResult->GetStatus()))
    {
        if (m_CallBack)
        {
            (*m_CallBack)(m_CallBackParam);
        }

        if (m_DrawMask)
        {
            FLOAT* pSU = NULL;
            UINT numSU;
            BOOL suConverged;
            m_pFaceTracker->GetShapeUnits(NULL, &pSU, &numSU, &suConverged);
            POINT viewOffset = { 0, 0 };
            FT_CAMERA_CONFIG cameraConfig;
            if (m_KinectSensorPresent)
            {
                m_KinectSensor.GetVideoConfiguration(&cameraConfig);
            }
            else
            {
                cameraConfig.Width = 640;
                cameraConfig.Height = 480;
                cameraConfig.FocalLength = 500.0f;
            }
            IFTModel* ftModel;
            HRESULT hr = m_pFaceTracker->GetFaceModel(&ftModel);
            if (SUCCEEDED(hr))
            {
                hr = VisualizeFaceModel(m_colorImage, ftModel, &cameraConfig, pSU, 1.0, viewOffset, pResult, 0x00FFFF00);
                ftModel->Release();
            }
        }
    }
    return TRUE;
}

// We compute here the nominal "center of attention" that is used when zooming the presented image.
void FTHelper::SetCenterOfImage(IFTResult* pResult)
{
    float centerX = ((float)m_colorImage->GetWidth()) / 2.0f;
    float centerY = ((float)m_colorImage->GetHeight()) / 2.0f;
    if (pResult)
    {
        if (SUCCEEDED(pResult->GetStatus()))
        {
            RECT faceRect;
            pResult->GetFaceRect(&faceRect);
            centerX = (faceRect.left + faceRect.right) / 2.0f;
            centerY = (faceRect.top + faceRect.bottom) / 2.0f;
        }
        m_XCenterFace += 0.02f * (centerX - m_XCenterFace);
        m_YCenterFace += 0.02f * (centerY - m_YCenterFace);
    }
    else
    {
        m_XCenterFace = centerX;
        m_YCenterFace = centerY;
    }
}

// Get a video image and process it.
void FTHelper::CheckCameraInput()
{
    HRESULT hrFT = E_FAIL;

    if (m_KinectSensorPresent && m_KinectSensor.GetVideoBuffer())
    {
        HRESULT hrCopy = m_KinectSensor.GetVideoBuffer()->CopyTo(m_colorImage, NULL, 0, 0);
        if (SUCCEEDED(hrCopy) && m_KinectSensor.GetDepthBuffer())
        {
            hrCopy = m_KinectSensor.GetDepthBuffer()->CopyTo(m_depthImage, NULL, 0, 0);
        }
        // Do face tracking
        if (SUCCEEDED(hrCopy))
        {
            FT_SENSOR_DATA sensorData(m_colorImage, m_depthImage, m_KinectSensor.GetZoomFactor(), m_KinectSensor.GetViewOffSet());

            FT_VECTOR3D* hint = NULL;
            if (SUCCEEDED(m_KinectSensor.GetClosestHint(m_hint3D)))
            {
                hint = m_hint3D;
            }
            if (m_LastTrackSucceeded)
            {
                hrFT = m_pFaceTracker->ContinueTracking(&sensorData, hint, m_pFTResult);
            }
            else
            {
                hrFT = m_pFaceTracker->StartTracking(&sensorData, NULL, hint, m_pFTResult);
            }
        }
    }

    m_LastTrackSucceeded = SUCCEEDED(hrFT) && SUCCEEDED(m_pFTResult->GetStatus());
    if (m_LastTrackSucceeded)
    {
        SubmitFaceTrackingResult(m_pFTResult);
    }
    else
    {
        m_pFTResult->Reset();
    }
}

DWORD WINAPI FTHelper::FaceTrackingStaticThread(PVOID lpParam)
{
    FTHelper* context = static_cast<FTHelper*>(lpParam);
    if (context)
    {
        return context->FaceTrackingThread();
    }
    return 0;
}

DWORD WINAPI FTHelper::FaceTrackingThread()
{
    FT_CAMERA_CONFIG videoConfig;
    FT_CAMERA_CONFIG depthConfig;
    FT_CAMERA_CONFIG* pDepthConfig = NULL;

    // Try to get the Kinect camera to work
    HRESULT hr = m_KinectSensor.Init(m_depthType, m_depthRes, m_bNearMode, m_bFallbackToDefault, m_colorType, m_colorRes, m_bSeatedSkeletonMode);
    if (SUCCEEDED(hr))
    {
        m_KinectSensorPresent = TRUE;
        m_KinectSensor.GetVideoConfiguration(&videoConfig);
        m_KinectSensor.GetDepthConfiguration(&depthConfig);
        pDepthConfig = &depthConfig;
        m_hint3D[0] = m_hint3D[1] = FT_VECTOR3D(0, 0, 0);

        // v2.1 stabilization: signal Init() success
        m_initHr = S_OK;
        if (m_hInitDoneEvent) SetEvent(m_hInitDoneEvent);
    }
    else
    {
        // v2.1 stabilization: no modal dialogs, just fail fast.
        m_KinectSensorPresent = FALSE;
        m_initHr = hr;
        FTLogHresult(L"KinectSensor::Init", hr);
        if (m_hInitDoneEvent) SetEvent(m_hInitDoneEvent);
        return 1;
    }

    // Try to start the face tracker.
    m_pFaceTracker = FTCreateFaceTracker(_opt);
    if (!m_pFaceTracker)
    {
        m_initHr = E_FAIL;
        FTLogHresult(L"FTCreateFaceTracker", m_initHr);
        if (m_hInitDoneEvent) SetEvent(m_hInitDoneEvent);
        m_KinectSensor.Release();
        m_KinectSensorPresent = FALSE;
        return 2;
    }

    hr = m_pFaceTracker->Initialize(&videoConfig, pDepthConfig, NULL, NULL);
    if (FAILED(hr))
    {
        m_initHr = hr;
        FTLogHresult(L"IFTFaceTracker::Initialize", hr);
        if (m_hInitDoneEvent) SetEvent(m_hInitDoneEvent);

        m_pFaceTracker->Release();
        m_pFaceTracker = NULL;

        m_KinectSensor.Release();
        m_KinectSensorPresent = FALSE;
        return 3;
    }

    hr = m_pFaceTracker->CreateFTResult(&m_pFTResult);
    if (FAILED(hr) || !m_pFTResult)
    {
        m_initHr = FAILED(hr) ? hr : E_FAIL;
        FTLogHresult(L"CreateFTResult", m_initHr);
        if (m_hInitDoneEvent) SetEvent(m_hInitDoneEvent);

        m_pFaceTracker->Release();
        m_pFaceTracker = NULL;

        m_KinectSensor.Release();
        m_KinectSensorPresent = FALSE;
        return 4;
    }

    // Initialize the RGB image.
    m_colorImage = FTCreateImage();
    if (!m_colorImage || FAILED(hr = m_colorImage->Allocate(videoConfig.Width, videoConfig.Height, FTIMAGEFORMAT_UINT8_B8G8R8X8)))
    {
        m_initHr = FAILED(hr) ? hr : E_FAIL;
        FTLogHresult(L"m_colorImage->Allocate", m_initHr);
        if (m_hInitDoneEvent) SetEvent(m_hInitDoneEvent);

        if (m_pFTResult) { m_pFTResult->Release(); m_pFTResult = NULL; }
        m_pFaceTracker->Release(); m_pFaceTracker = NULL;
        m_KinectSensor.Release(); m_KinectSensorPresent = FALSE;
        return 5;
    }

    if (pDepthConfig)
    {
        m_depthImage = FTCreateImage();
        if (!m_depthImage || FAILED(hr = m_depthImage->Allocate(depthConfig.Width, depthConfig.Height, FTIMAGEFORMAT_UINT16_D13P3)))
        {
            m_initHr = FAILED(hr) ? hr : E_FAIL;
            FTLogHresult(L"m_depthImage->Allocate", m_initHr);
            if (m_hInitDoneEvent) SetEvent(m_hInitDoneEvent);

            if (m_colorImage) { m_colorImage->Release(); m_colorImage = NULL; }
            if (m_pFTResult) { m_pFTResult->Release(); m_pFTResult = NULL; }
            m_pFaceTracker->Release(); m_pFaceTracker = NULL;
            m_KinectSensor.Release(); m_KinectSensorPresent = FALSE;
            return 6;
        }
    }

    SetCenterOfImage(NULL);
    m_LastTrackSucceeded = false;

    while (m_ApplicationIsRunning)
    {
        CheckCameraInput();
        if (m_hWnd != NULL)
        {
            InvalidateRect(m_hWnd, NULL, FALSE);
            UpdateWindow(m_hWnd);
        }
        Sleep(33); // 30fps loop
    }

    m_pFaceTracker->Release();
    m_pFaceTracker = NULL;

    if (m_colorImage)
    {
        m_colorImage->Release();
        m_colorImage = NULL;
    }

    if (m_depthImage)
    {
        m_depthImage->Release();
        m_depthImage = NULL;
    }

    if (m_pFTResult)
    {
        m_pFTResult->Release();
        m_pFTResult = NULL;
    }

    m_KinectSensor.Release();
    m_KinectSensorPresent = FALSE;

    return 0;
}

HRESULT FTHelper::GetCameraConfig(FT_CAMERA_CONFIG* cameraConfig)
{
    return m_KinectSensorPresent ? m_KinectSensor.GetVideoConfiguration(cameraConfig) : E_FAIL;
}
