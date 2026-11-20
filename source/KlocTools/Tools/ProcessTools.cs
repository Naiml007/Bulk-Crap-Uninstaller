/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using Klocman.Extensions;
using Klocman.Properties;

namespace Klocman.Tools
{
    public static class ProcessTools
    {
        public static bool Is64BitProcess => IntPtr.Size == 8;

        /// <summary>
        ///     Kill all of process's children, grandchildren, etc.
        /// </summary>
        /// <param name="pid">Process ID.</param>
        public static void KillChildProcesses(int pid)
        {
            foreach (var id in GetChildProcesses(pid))
            {
                KillProcessAndChildProcesses(id);
            }
        }

        /// <summary>
        ///     Get IDs of all child processes
        /// </summary>
        public static IEnumerable<int> GetChildProcesses(int pid)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid))
                {
                    var moc = searcher.Get();
                    var childProcesses = moc.Cast<ManagementObject>().Select(mo => Convert.ToInt32(mo["ProcessID"])).ToList();
                    return childProcesses;
                }
            }
            catch
            {
                Process processById;
                try
                {
                    processById = Process.GetProcessById(pid);
                }
                catch (Exception a)
                {
                    Console.WriteLine(a);
                    return Enumerable.Empty<int>();
                }
                return processById.GetChildProcesses().Select(x => x.Id);
            }
        }

        /// <summary>
        ///     Kill a process, and all of its children, grandchildren, etc.
        /// </summary>
        /// <param name="pid">Process ID.</param>
        public static void KillProcessAndChildProcesses(int pid)
        {
            KillChildProcesses(pid);

            try
            {
                var proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        /// <exception cref="ArgumentException">processName</exception>
        /// <exception cref="InvalidOperationException">
        ///     There are problems accessing the performance counter API's used to get
        ///     process information. This exception is specific to Windows NT, Windows 2000, and Windows XP.
        /// </exception>
        public static bool SafeKillProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                throw new ArgumentException(@"Process name can't be null or empty", nameof(processName));

            foreach (var p in Process.GetProcessesByName(processName))
            {
                try
                {
                    p.Kill();
                    p.WaitForExit(); // possibly with a timeout
                    return true;
                }
                catch (Win32Exception)
                {
                    // process was terminating or can't be terminated - deal with it
                    return false;
                }
                catch (InvalidOperationException)
                {
                    // process has already exited - might be able to let this one go
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        private static readonly char[] SeparateArgsFromCommandInvalidChars =
            Path.GetInvalidFileNameChars().Concat(new[] { ',', ';' }).ToArray();

        //static readonly char[] pathFilterChars = StringTools.InvalidPathChars.Except(new char[] { '\"' }).ToArray();
        /// <summary>
        ///     Attempts to separate filename (or filename with path) from the supplied arguments.
        ///
        ///     Handles the following common formats:
        ///     1. Quoted executable:    "C:\Program Files\App\unins000.exe" /SILENT
        ///     2. Unquoted with ext:    C:\Program Files\App\unins000.exe /SILENT
        ///     3. Bare executable:      msiexec /x {GUID} /quiet
        ///     4. Launcher+path:        java -jar C:\App\uninst.jar   (splits at first space only)
        ///     5. Non-dotted with dash: C:\Tools\uninstaller -silent
        /// </summary>
        /// <param name="fullCommand"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">The value of 'fullCommand' cannot be null. </exception>
        /// <exception cref="ArgumentException">fullCommand can't be empty</exception>
        /// <exception cref="FormatException">Filename is in invalid format</exception>
        public static ProcessStartCommand SeparateArgsFromCommand(string fullCommand)
        {
            if (fullCommand == null)
                throw new ArgumentNullException(nameof(fullCommand));

            // Get rid of whitespaces
            fullCommand = fullCommand.Trim();

            if (string.IsNullOrEmpty(fullCommand))
                throw new ArgumentException(Localisation.Error_SeparateArgsFromCommand_Empty, nameof(fullCommand));

            // -----------------------------------------------------------------------
            // Case 1: Command starts with a quote — the executable is quoted.
            // Only treat the leading quote as a path boundary if the FIRST character
            // is a quote (or leading whitespace then a quote). This avoids absorbing
            // argument quotes like /l*v "C:\log.txt" into the executable name.
            // -----------------------------------------------------------------------
            if (fullCommand[0] == '"')
            {
                var closingQuote = fullCommand.IndexOf('"', 1);
                if (closingQuote < 0)
                    throw new FormatException(Localisation.Error_SeparateArgsFromCommand_MissingQuotationMark);

                var filename = fullCommand.Substring(1, closingQuote - 1);
                var args = fullCommand.Substring(closingQuote + 1).TrimStart();
                return new ProcessStartCommand(filename, args);
            }

            // -----------------------------------------------------------------------
            // Case 2: Unquoted — examine structure to find the executable boundary.
            // -----------------------------------------------------------------------

            var firstDot = fullCommand.IndexOf('.');
            var firstSpaceForDotCheck = fullCommand.IndexOf(' ');
            // If the first token (before the first space) contains no dot, the executable
            // has no extension — use the non-dotted path.  We only redirect here when
            // there is no drive root after the first space (which is handled by Case 3 below).
            if (firstDot < 0 ||
                (firstSpaceForDotCheck >= 0 && firstDot > firstSpaceForDotCheck &&
                 fullCommand.IndexOf(":\\", firstSpaceForDotCheck, StringComparison.InvariantCulture) < 0))
                return SeparateNonDottedCommand(fullCommand);

            // -----------------------------------------------------------------------
            // Case 3: "LauncherName C:\path\..." pattern (e.g. java -jar C:\App\x.jar)
            // A drive root (X:\) exists somewhere after the first space.
            // Split at the FIRST space only — not the last space before the drive root —
            // so that "java -jar C:\..." does not accidentally produce FileName="java -jar".
            // -----------------------------------------------------------------------
            {
                var pathRoot = fullCommand.IndexOf(":\\", StringComparison.InvariantCulture);
                var firstSpace = fullCommand.IndexOf(' ');

                if (firstSpace >= 0 && pathRoot > firstSpace)
                {
                    // Confirm there are no invalid filename chars before the first space
                    // (which would mean the leading token is not a plain executable name).
                    var filenameBreaker = fullCommand.IndexOfAny(SeparateArgsFromCommandInvalidChars, 0, firstSpace);
                    if (filenameBreaker < 0)
                    {
                        // The token before the first space is a bare executable name; everything
                        // after is arguments (which may start with flags and then the full path).
                        return new ProcessStartCommand(
                            fullCommand.Substring(0, firstSpace).TrimEnd(),
                            fullCommand.Substring(firstSpace).TrimStart());
                    }
                }
            }

            // -----------------------------------------------------------------------
            // Case 4: Unquoted path with extension (e.g. C:\Program Files\App\unins.exe /S)
            // Walk dots left-to-right and find where the filename ends (first space or
            // invalid char that is not inside a path continuation).
            // -----------------------------------------------------------------------
            {
                var pathEnd = -1;
                var endIndex = 0;
                while (true)
                {
                    var dot = fullCommand.IndexOf('.', endIndex);
                    if (dot < 0)
                        break;

                    var filenameBreaker = fullCommand.IndexOfAny(SeparateArgsFromCommandInvalidChars, dot);
                    var space = fullCommand.IndexOf(' ', dot);
                    if (filenameBreaker < 0)
                    {
                        if (space < 0) break;
                        filenameBreaker = space;
                    }

                    var dash = fullCommand.IndexOf('\\', dot);
                    if (filenameBreaker < dash || dash < 0)
                    {
                        pathEnd = space < 0 ? filenameBreaker : space;
                        break;
                    }

                    var nextBreaker = fullCommand.IndexOfAny(SeparateArgsFromCommandInvalidChars, filenameBreaker + 1);
                    var nextDash = fullCommand.IndexOf('\\', filenameBreaker + 1);

                    if (nextBreaker > 0 && (nextDash < 0 || nextBreaker < nextDash))
                    {
                        var nextDot = fullCommand.IndexOf('.', filenameBreaker + 1);
                        if (nextDot < 0 || nextBreaker < nextDot)
                        {
                            pathEnd = space < 0 ? filenameBreaker : space;
                            break;
                        }
                    }

                    endIndex = dash;
                }

                // If the dot loop found no boundary, the exe has no extension but the
                // directory path contains dots (e.g. App.1.0\uninstaller).  Fall back to
                // the dash-argument check before treating the whole string as a filename.
                if (pathEnd < 0)
                {
                    var spaceBeforeDash = FindSpacePrefixedDashArgument(fullCommand, 0);
                    if (spaceBeforeDash >= 0)
                        return new ProcessStartCommand(
                            fullCommand.Substring(0, spaceBeforeDash).TrimEnd(),
                            fullCommand.Substring(spaceBeforeDash).TrimStart());
                }

                return SeparateCommand(fullCommand, pathEnd);
            }
        }

        private static ProcessStartCommand SeparateCommand(string fullCommand, int splitIndex)
        {
            // Begin extracting filename and arguments
            string filename;
            var args = string.Empty;

            if (splitIndex < 0 || splitIndex >= fullCommand.Length)
            {
                // Looks like there were no arguments, assume whole command is a filename
                filename = fullCommand;
            }
            else
            {
                // pathEnd shows the end of the filename (and start of the arguments)
                filename = fullCommand.Substring(0, splitIndex).TrimEnd();
                args = fullCommand.Substring(splitIndex).TrimStart();
            }

            filename = filename.Trim('"'); // Get rid of the quotation marks
            return new ProcessStartCommand(filename, args);
        }

        private static ProcessStartCommand SeparateNonDottedCommand(string fullCommand)
        {
            // Look for the first root of a path
            var pathRoot = fullCommand.IndexOf(":\\", StringComparison.InvariantCulture);
            var pathRootEnd = pathRoot < 0 ? 0 : pathRoot + 2;

            // Exclude backslash from break chars so path segments are traversed correctly.
            // Also exclude '-' so that flag characters like "-silent" or "--quiet" are
            // recognised as argument breaks (a dash preceded by a space marks the start
            // of arguments even for executables without a file extension).
            var breakChars = SeparateArgsFromCommandInvalidChars.Except(new[] { '\\' }).ToArray();

            // Check if there are any invalid path chars before the start we found.
            if (pathRootEnd > 0 && fullCommand.IndexOfAny(breakChars, 0, pathRootEnd - 2) >= 0)
                pathRootEnd = 0;

            var breakIndex = fullCommand.IndexOfAny(breakChars, pathRootEnd);

            // No invalid path chars — it's probably just a naked filename or directory path.
            if (breakIndex < 0)
            {
                // Still check for a dash-prefixed argument after a space: "uninstaller -silent"
                var spaceBeforeDash = FindSpacePrefixedDashArgument(fullCommand, pathRootEnd);
                if (spaceBeforeDash >= 0)
                    return new ProcessStartCommand(
                        fullCommand.Substring(0, spaceBeforeDash).TrimEnd(),
                        fullCommand.Substring(spaceBeforeDash).TrimStart());

                return new ProcessStartCommand(fullCommand.Trim('"'));
            }

            // The invalid char has to have at least 1 space before it to count as an argument.
            if (breakIndex > 0 && fullCommand[breakIndex - 1] == ' ')
                return new ProcessStartCommand(fullCommand.Substring(0, breakIndex - 1).TrimEnd(),
                    fullCommand.Substring(breakIndex));

            throw new FormatException(Localisation.Error_SeparateArgsFromCommand_NoDot + "\n" + fullCommand);
        }

        /// <summary>
        /// Finds the index of the first space-preceded '-' or '--' sequence that starts an argument,
        /// e.g. the space before "-silent" in "C:\Tools\uninstaller -silent".
        /// Returns -1 if none found.
        /// </summary>
        private static int FindSpacePrefixedDashArgument(string command, int searchFrom)
        {
            var i = searchFrom;
            while (i < command.Length)
            {
                var spaceIdx = command.IndexOf(' ', i);
                if (spaceIdx < 0) break;

                var nextNonSpace = spaceIdx + 1;
                while (nextNonSpace < command.Length && command[nextNonSpace] == ' ')
                    nextNonSpace++;

                if (nextNonSpace < command.Length && command[nextNonSpace] == '-')
                    return spaceIdx;

                i = nextNonSpace;
            }
            return -1;
        }

        /// <summary>
        ///     Change default culture info for new threads
        /// </summary>
        /// <param name="culture"></param>
        public static void SetDefaultCulture(CultureInfo culture)
        {
            var type = typeof(CultureInfo);

            if (Environment.Version.Major < 4)
            {
                // Fields used before .Net 4.0
                try
                {
                    type.InvokeMember("m_userDefaultCulture",
                        BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                        null,
                        culture,
                        new object[] { culture });

                    type.InvokeMember("m_userDefaultUICulture",
                        BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                        null,
                        culture,
                        new object[] { culture });

                    return;
                }
                catch
                {
                    //Ignore failure, try next
                }
            }

            try
            {
                type.InvokeMember("s_userDefaultCulture",
                    BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    culture,
                    new object[] { culture });

                type.InvokeMember("s_userDefaultUICulture",
                    BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    culture,
                    new object[] { culture });
            }
            catch
            {
                //Ignore failure
            }
        }

        /// <summary>
        ///     Bring to foreground main window of all processes with name of the executing process
        /// </summary>
        public static void ShowMainWindow()
        {
            ShowMainWindow(Process.GetCurrentProcess().ProcessName);
        }

        /// <summary>
        ///     Bring to foreground main window of all processes with supplied name
        /// </summary>
        /// <param name="processName">Name of the processes to bring to foreground</param>
        public static void ShowMainWindow(string processName)
        {
            foreach (var p in Process.GetProcessesByName(processName))
            {
                ShowWindow(p.MainWindowHandle, 1); //SW_SHOWNORMAL = 1
                SetForegroundWindow(p.MainWindowHandle);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        public static Icon GetIconFromEntryExe()
        {
            var location = Assembly.GetEntryAssembly()?.Location;
            if (location == null) throw new ArgumentException("Failed to get location of EntryAssembly");
            if (location.EndsWith(".dll")) location = location.Substring(0, location.Length - 3) + "exe";
            var icon = DrawingTools.ExtractAssociatedIcon(location);
            return icon;
        }

        /// <summary>
        /// Process.GetProcessById but doesn't throw on issues and instead returns null
        /// </summary>
        public static Process GetProcessByIdSafe(int processId)
        {
            try
            {
                return Process.GetProcessById(processId);
            }
            catch
            {
                return null;
            }
        }
    }
}