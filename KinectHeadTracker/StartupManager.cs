using System;
using System.IO;

namespace KinectHeadtracker
{
    /// <summary>
    /// Creates/updates/removes a per-user Task Scheduler entry to run the app at logon.
    /// Uses the built-in Windows Task Scheduler COM API (no extra dependencies).
    /// </summary>
    internal static class StartupManager
    {
        // Keep the task name stable across versions.
        private const string TaskName = "KinectHeadTrackerV2";

        // Put our task in a dedicated folder so the root Task Scheduler view stays clean.
        private const string TaskFolderPath = "\\KinectHeadTrackerV2";

        // COM constants (Task Scheduler)
        private const int TASK_TRIGGER_LOGON = 9;
        private const int TASK_ACTION_EXEC = 0;
        private const int TASK_CREATE_OR_UPDATE = 6;
        private const int TASK_LOGON_INTERACTIVE_TOKEN = 3;

        public static void Sync(bool enabled, string exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath)) return;

            if (enabled) Ensure(exePath);
            else Remove();
        }

        public static bool Ensure(string exePath)
        {
            try
            {
                exePath = Path.GetFullPath(exePath);

                // If it exists and matches, we’re done.
                var existing = TryGetExistingExePath();
                if (!string.IsNullOrWhiteSpace(existing) && PathsEqual(existing, exePath))
                    return true;

                // Otherwise create/update it.
                return CreateOrUpdate(exePath);
            }
            catch
            {
                return false;
            }
        }

        public static bool Remove()
        {
            try
            {
                dynamic service = CreateService();
                dynamic folder = TryGetFolder(service) ?? service.GetFolder("\\");

                try
                {
                    folder.DeleteTask(TaskName, 0);
                }
                catch
                {
                    // Task didn’t exist: treat as success
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string TryGetExistingExePath()
        {
            try
            {
                dynamic service = CreateService();
                dynamic folder = TryGetFolder(service);
                if (folder == null) return null;

                dynamic task = null;
                try { task = folder.GetTask(TaskName); }
                catch { return null; }

                if (task == null) return null;

                try
                {
                    dynamic def = task.Definition;
                    dynamic actions = def.Actions;
                    if (actions == null) return null;

                    // 1-based indexing in COM collections
                    if (actions.Count >= 1)
                    {
                        dynamic action = actions.Item(1);
                        try
                        {
                            return (string)action.Path;
                        }
                        catch { }
                    }
                }
                catch { }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool CreateOrUpdate(string exePath)
        {
            try
            {
                dynamic service = CreateService();
                dynamic folder = GetOrCreateFolder(service);

                dynamic taskDef = service.NewTask(0);

                // Registration
                taskDef.RegistrationInfo.Description = "Launch Kinect Head Tracker V2 at user logon.";

                // Settings
                taskDef.Settings.Enabled = true;
                taskDef.Settings.Hidden = false;
                taskDef.Settings.DisallowStartIfOnBatteries = false;
                taskDef.Settings.StopIfGoingOnBatteries = false;
                taskDef.Settings.StartWhenAvailable = true;

                // Trigger: at logon
                dynamic trigger = taskDef.Triggers.Create(TASK_TRIGGER_LOGON);
                trigger.Enabled = true;

                // Action: run EXE
                dynamic action = taskDef.Actions.Create(TASK_ACTION_EXEC);
                action.Path = exePath;
                action.WorkingDirectory = Path.GetDirectoryName(exePath);

                // Run as the interactive user (no password stored)
                taskDef.Principal.LogonType = TASK_LOGON_INTERACTIVE_TOKEN;
                taskDef.Principal.RunLevel = 0; // LUA (not highest)

                // Register in our folder
                folder.RegisterTaskDefinition(
                    TaskName,
                    taskDef,
                    TASK_CREATE_OR_UPDATE,
                    null,
                    null,
                    TASK_LOGON_INTERACTIVE_TOKEN,
                    null
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static dynamic CreateService()
        {
            var t = Type.GetTypeFromProgID("Schedule.Service");
            dynamic service = Activator.CreateInstance(t);
            service.Connect();
            return service;
        }

        private static dynamic TryGetFolder(dynamic service)
        {
            try
            {
                return service.GetFolder(TaskFolderPath);
            }
            catch
            {
                return null;
            }
        }

        private static dynamic GetOrCreateFolder(dynamic service)
        {
            // If it already exists, return it.
            var existing = TryGetFolder(service);
            if (existing != null) return existing;

            // Otherwise create it under root.
            dynamic root = service.GetFolder("\\");

            try
            {
                // CreateFolder expects just the folder name when called on a parent folder.
                root.CreateFolder("KinectHeadTrackerV2", null);
            }
            catch
            {
                // If it raced or already exists, ignore.
            }

            // Try again, fallback to root if creation failed for any reason.
            return TryGetFolder(service) ?? root;
        }

        private static bool PathsEqual(string a, string b)
        {
            try
            {
                return string.Equals(
                    Path.GetFullPath(a).TrimEnd('\\'),
                    Path.GetFullPath(b).TrimEnd('\\'),
                    StringComparison.OrdinalIgnoreCase
                );
            }
            catch
            {
                return string.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
