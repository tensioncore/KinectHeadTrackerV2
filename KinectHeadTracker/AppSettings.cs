namespace KinectHeadtracker
{
    public sealed class AppSettings
    {
        public int UdpPort = 5550;
        public string UdpTargetIp = "127.0.0.1";

        public bool LiveVideoEnabled = false;

        public int WindowX = int.MinValue;
        public int WindowY = int.MinValue;

        public bool RunAtStartupEnabled = false;
        public bool AutoStartEngineEnabled = false;
        public bool AutoStartStreamingEnabled = false;

        public bool LastEngineRunning = false;
        public bool LastStreamingRunning = false;
    }
}
