using System;
using System.Collections.Generic;
using System.IO;

namespace KinectHeadtracker
{
    internal static class SettingsStore
    {
        private const string FolderName = "KinectHeadTrackerV2";
        private const string FileName = "settings.ini";

        private static string GetFolderPath()
        {
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(baseDir, FolderName);
        }

        private static string GetFilePath()
        {
            return Path.Combine(GetFolderPath(), FileName);
        }

        public static AppSettings Load()
        {
            var s = new AppSettings();

            try
            {
                string path = GetFilePath();
                if (!File.Exists(path))
                    return s;

                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var lineRaw in File.ReadAllLines(path))
                {
                    string line = (lineRaw ?? "").Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("#")) continue;
                    if (line.StartsWith(";")) continue;

                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();

                    if (key.Length == 0) continue;
                    map[key] = val;
                }

                // Parse helpers
                int GetInt(string k, int defVal)
                {
                    string v;
                    if (!map.TryGetValue(k, out v)) return defVal;
                    int n;
                    return int.TryParse(v, out n) ? n : defVal;
                }

                bool GetBool(string k, bool defVal)
                {
                    string v;
                    if (!map.TryGetValue(k, out v)) return defVal;
                    v = (v ?? "").Trim().ToLowerInvariant();
                    if (v == "1" || v == "true" || v == "yes" || v == "on") return true;
                    if (v == "0" || v == "false" || v == "no" || v == "off") return false;
                    return defVal;
                }

                string GetStr(string k, string defVal)
                {
                    string v;
                    if (!map.TryGetValue(k, out v)) return defVal;
                    return v ?? defVal;
                }

                // Apply
                s.UdpPort = GetInt("UdpPort", s.UdpPort);
                s.UdpTargetIp = GetStr("UdpTargetIp", s.UdpTargetIp);

                s.LiveVideoEnabled = GetBool("LiveVideoEnabled", s.LiveVideoEnabled);

                s.WindowX = GetInt("WindowX", s.WindowX);
                s.WindowY = GetInt("WindowY", s.WindowY);

                s.RunAtStartupEnabled = GetBool("RunAtStartupEnabled", s.RunAtStartupEnabled);
                s.AutoStartEngineEnabled = GetBool("AutoStartEngineEnabled", s.AutoStartEngineEnabled);
                s.AutoStartStreamingEnabled = GetBool("AutoStartStreamingEnabled", s.AutoStartStreamingEnabled);

                s.LastEngineRunning = GetBool("LastEngineRunning", s.LastEngineRunning);
                s.LastStreamingRunning = GetBool("LastStreamingRunning", s.LastStreamingRunning);
            }
            catch
            {
                // never break app startup
            }

            return s;
        }

        public static void Save(AppSettings s)
        {
            if (s == null) return;

            try
            {
                string dir = GetFolderPath();
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string path = GetFilePath();

                // Write atomically: temp -> replace
                string tmp = path + ".tmp";

                using (var sw = new StreamWriter(tmp, false))
                {
                    sw.WriteLine("# Kinect Head Tracker v2.1 settings");
                    sw.WriteLine("UdpPort=" + s.UdpPort);
                    sw.WriteLine("UdpTargetIp=" + (s.UdpTargetIp ?? "127.0.0.1"));

                    sw.WriteLine("LiveVideoEnabled=" + (s.LiveVideoEnabled ? "1" : "0"));

                    sw.WriteLine("WindowX=" + s.WindowX);
                    sw.WriteLine("WindowY=" + s.WindowY);

                    sw.WriteLine("RunAtStartupEnabled=" + (s.RunAtStartupEnabled ? "1" : "0"));
                    sw.WriteLine("AutoStartEngineEnabled=" + (s.AutoStartEngineEnabled ? "1" : "0"));
                    sw.WriteLine("AutoStartStreamingEnabled=" + (s.AutoStartStreamingEnabled ? "1" : "0"));

                    sw.WriteLine("LastEngineRunning=" + (s.LastEngineRunning ? "1" : "0"));
                    sw.WriteLine("LastStreamingRunning=" + (s.LastStreamingRunning ? "1" : "0"));
                }

                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch { }

                File.Move(tmp, path);
            }
            catch
            {
                // never break app shutdown
            }
        }
    }
}
