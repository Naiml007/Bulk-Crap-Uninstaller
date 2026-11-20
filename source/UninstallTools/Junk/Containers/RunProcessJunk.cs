/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Diagnostics;
using System.IO;
using Klocman.Forms.Tools;
using Klocman.Tools;

namespace UninstallTools.Junk.Containers
{
    public class RunProcessJunk : JunkResultBase
    {
        public ProcessStartCommand ProcessToStart { get; }

        private readonly string _junkName;

        public RunProcessJunk(ApplicationUninstallerEntry application, IJunkCreator source, ProcessStartCommand processToStart, string junkName) : base(application, source)
        {
            _junkName = junkName;
            ProcessToStart = processToStart;
        }

        /// <summary>
        /// Process-based junk cannot be automatically reverted once executed — there is no universal
        /// inverse operation for an arbitrary process call (e.g. a Chocolatey uninstall cannot be
        /// "re-installed" automatically to restore prior state).
        ///
        /// Instead, this method writes a human-readable audit record into the backup directory so the
        /// user has a record of exactly what command was executed and can manually undo if needed.
        /// This avoids triggering the "Backup failed" error dialog in JunkRemoveWindow.
        /// </summary>
        public override void Backup(string backupDirectory)
        {
            try
            {
                Directory.CreateDirectory(backupDirectory);

                var safeName = string.Concat(
                    _junkName.Split(Path.GetInvalidFileNameChars()));
                if (safeName.Length > 60)
                    safeName = safeName.Substring(0, 60);

                var logFileName = $"RunProcess_{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var logPath = Path.Combine(backupDirectory, logFileName);

                var content = string.Join(Environment.NewLine, new[]
                {
                    "=== BCU Run-Process Junk Audit Log ===",
                    "",
                    $"Timestamp      : {DateTime.Now:O}",
                    $"Application    : {Application?.DisplayName ?? "(unknown)"}",
                    $"Junk Name      : {_junkName}",
                    $"Command Line   : {ProcessToStart}",
                    "",
                    "NOTE: Process executions cannot be automatically reversed.",
                    "If you need to undo this action, refer to the command above and",
                    "consult the documentation for the tool that was invoked.",
                });

                File.WriteAllText(logPath, content);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"RunProcessJunk.Backup: Failed to write audit log - {ex.Message}");
                // Intentionally swallowed: backup failure must NOT surface a blocking
                // dialog in JunkRemoveWindow when the user is performing deletions.
            }
        }

        public override void Delete()
        {
            try
            {
                var info = ProcessToStart.ToProcessStartInfo();
                info.WindowStyle = ProcessWindowStyle.Minimized;
                info.UseShellExecute = true;
                Process.Start(info)?.WaitForExit();
            }
            catch (SystemException ex)
            {
                Trace.WriteLine($"Failed to delete junk {GetDisplayName()} - {ex}");
            }
        }

        public override string GetDisplayName()
        {
            return $"{_junkName} ({ProcessToStart})";
        }

        public override void Open()
        {
            try
            {
                WindowsTools.OpenExplorerFocusedOnObject(ProcessToStart.FileName);
            }
            catch (SystemException ex)
            {
                PremadeDialogs.GenericError(ex);
            }
        }
    }
}