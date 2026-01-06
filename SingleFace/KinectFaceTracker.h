#pragma once

public delegate void ReceiveKinectData(
    double x, double y, double z,
    double yaw, double pitch, double roll
);

public ref class KinectFaceTracker
{
public:
    KinectFaceTracker();
    ~KinectFaceTracker();
    !KinectFaceTracker();

    void Subscribe(ReceiveKinectData^ cb);
    void Unsubscribe(ReceiveKinectData^ cb);

    bool Start();
    bool StartWithCallback();
    void Stop();

    // Existing (port-only) – keep for compatibility
    bool StartStreaming(int port);

    // NEW (v2.1): target IP + port, applied at stream start (not hot-swapped)
    bool StartStreaming(int port, System::String^ targetIp);

    void StopStreaming();
    bool IsStreaming();

    bool TiltCamera(int angleDelta);
    bool IsReceivingData();

    System::Drawing::Image^ GetImage();

private:
    class SingleFaceNoWindow* app;
    ReceiveKinectData^ m_onReceive;               // multicast delegate
    ReceiveKinectData^ receiveKinectDataCallback; // keeps managed thunk alive

    void ReceiveData(
        double x, double y, double z,
        double yaw, double pitch, double roll
    );
};
