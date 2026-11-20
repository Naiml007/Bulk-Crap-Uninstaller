/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BulkCrapUninstaller.Functions.Tools
{
    public static class StartupArgumentTools
    {
        public static string GetStartupUninstallListPath(IEnumerable<string> commandLineArgs)
        {
            if (commandLineArgs == null) return null;

            foreach (var candidate in commandLineArgs.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                try
                {
                    if (!candidate.EndsWith(".bcul", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (File.Exists(candidate))
                        return candidate;
                }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
                {
                    // Ignore invalid candidate and continue
                }
            }

            return null;
        }
    }
}
