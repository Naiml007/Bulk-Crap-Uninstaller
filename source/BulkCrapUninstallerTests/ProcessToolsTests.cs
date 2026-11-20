/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using Klocman.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BulkCrapUninstallerTests
{
    /// <summary>
    /// Unit tests for ProcessTools.SeparateArgsFromCommand.
    ///
    /// These cover the bugs identified in the EGM Goldfish review:
    ///   - Quote absorption: argument quotes must not be treated as executable boundaries
    ///   - Launcher+path: "java -jar C:\..." must split at the first space, not last space before :\
    ///   - Non-dotted dash flags: "uninstaller -silent" must recognise -silent as an argument
    /// </summary>
    [TestClass]
    public class ProcessToolsTests
    {
        // -------------------------------------------------------------------------
        // Quoted executable paths
        // -------------------------------------------------------------------------

        [TestMethod]
        public void SeparateArgsFromCommand_QuotedExe_Standard()
        {
            var result = ProcessTools.SeparateArgsFromCommand(@"""C:\Program Files\App\unins000.exe"" /SILENT");
            Assert.AreEqual(@"C:\Program Files\App\unins000.exe", result.FileName);
            Assert.AreEqual("/SILENT", result.Arguments.Trim());
        }

        [TestMethod]
        public void SeparateArgsFromCommand_QuotedExe_NoArgs()
        {
            var result = ProcessTools.SeparateArgsFromCommand(@"""C:\Program Files\App\unins000.exe""");
            Assert.AreEqual(@"C:\Program Files\App\unins000.exe", result.FileName);
            Assert.AreEqual(string.Empty, result.Arguments.Trim());
        }

        // -------------------------------------------------------------------------
        // Argument quotes must NOT absorb into the executable name
        // -------------------------------------------------------------------------

        [TestMethod]
        public void SeparateArgsFromCommand_MsiExec_QuotedLogArg()
        {
            // Bug: old code saw the " before .txt and merged everything into FileName.
            // Expected: FileName=msiexec, Arguments contain the rest.
            var result = ProcessTools.SeparateArgsFromCommand(@"msiexec /x {GUID} /l*v ""C:\log.txt""");
            Assert.AreEqual("msiexec", result.FileName);
            StringAssert.Contains(result.Arguments, "/x");
            StringAssert.Contains(result.Arguments, "/l*v");
        }

        [TestMethod]
        public void SeparateArgsFromCommand_Cmd_EchoWithDot()
        {
            // cmd /c "echo hello.txt" — quote appears before the dot in hello.txt
            // Bug: old code absorbed the entire string into FileName.
            var result = ProcessTools.SeparateArgsFromCommand(@"cmd /c ""echo hello.txt""");
            Assert.AreEqual("cmd", result.FileName);
            StringAssert.Contains(result.Arguments, "/c");
        }

        [TestMethod]
        public void SeparateArgsFromCommand_Regsvr32_QuotedPath()
        {
            var result = ProcessTools.SeparateArgsFromCommand(@"regsvr32 /u ""C:\My.Folder\test.dll""");
            Assert.AreEqual("regsvr32", result.FileName);
            StringAssert.Contains(result.Arguments, "/u");
        }

        // -------------------------------------------------------------------------
        // Launcher + path pattern (e.g. java -jar C:\App\uninst.jar)
        // -------------------------------------------------------------------------

        [TestMethod]
        public void SeparateArgsFromCommand_JavaJar()
        {
            // Bug: old code produced FileName="java -jar" (split at last space before :\).
            var result = ProcessTools.SeparateArgsFromCommand(@"java -jar C:\App\uninst.jar");
            Assert.AreEqual("java", result.FileName);
            StringAssert.Contains(result.Arguments, "-jar");
            StringAssert.Contains(result.Arguments, @"C:\App\uninst.jar");
        }

        [TestMethod]
        public void SeparateArgsFromCommand_DotnetExec()
        {
            // Bug: old code produced FileName="dotnet exec".
            var result = ProcessTools.SeparateArgsFromCommand(@"dotnet exec C:\App\uninst.dll");
            Assert.AreEqual("dotnet", result.FileName);
            StringAssert.Contains(result.Arguments, "exec");
            StringAssert.Contains(result.Arguments, @"C:\App\uninst.dll");
        }

        // -------------------------------------------------------------------------
        // Unquoted path with extension (happy path — must keep working)
        // -------------------------------------------------------------------------

        [TestMethod]
        public void SeparateArgsFromCommand_UnquotedExe_Silent()
        {
            var result = ProcessTools.SeparateArgsFromCommand(@"C:\Program Files\App\unins000.exe /SILENT");
            Assert.AreEqual(@"C:\Program Files\App\unins000.exe", result.FileName);
            Assert.AreEqual("/SILENT", result.Arguments.Trim());
        }

        [TestMethod]
        public void SeparateArgsFromCommand_UnquotedExe_NoArgs()
        {
            var result = ProcessTools.SeparateArgsFromCommand(@"C:\Program Files\App\unins000.exe");
            Assert.AreEqual(@"C:\Program Files\App\unins000.exe", result.FileName);
            Assert.AreEqual(string.Empty, result.Arguments.Trim());
        }

        // -------------------------------------------------------------------------
        // Non-dotted executable with dash flags
        // -------------------------------------------------------------------------

        [TestMethod]
        public void SeparateArgsFromCommand_NondottedExe_DashFlag()
        {
            // Bug: old SeparateNonDottedCommand did not recognise - as an arg boundary.
            var result = ProcessTools.SeparateArgsFromCommand(@"C:\Tools\uninstaller -silent");
            Assert.AreEqual(@"C:\Tools\uninstaller", result.FileName);
            Assert.AreEqual("-silent", result.Arguments.Trim());
        }

        [TestMethod]
        public void SeparateArgsFromCommand_NondottedExe_DoubleDash()
        {
            var result = ProcessTools.SeparateArgsFromCommand(@"C:\Tools\remover --quiet --no-restart");
            Assert.AreEqual(@"C:\Tools\remover", result.FileName);
            StringAssert.StartsWith(result.Arguments.Trim(), "--quiet");
        }

        [TestMethod]
        public void SeparateArgsFromCommand_NondottedExe_DotInDirDashFlagOnExe()
        {
            // Dot in directory path, executable itself has no extension
            // e.g. C:\Program Files\App.1.0\uninstaller -silent
            var result = ProcessTools.SeparateArgsFromCommand(@"C:\Program Files\App.1.0\uninstaller -silent");
            Assert.AreEqual(@"C:\Program Files\App.1.0\uninstaller", result.FileName);
            Assert.AreEqual("-silent", result.Arguments.Trim());
        }

        // -------------------------------------------------------------------------
        // Edge cases & guards
        // -------------------------------------------------------------------------

        [TestMethod]
        public void SeparateArgsFromCommand_NullThrows()
        {
            try
            {
                ProcessTools.SeparateArgsFromCommand(null);
                Assert.Fail("Expected ArgumentNullException was not thrown.");
            }
            catch (ArgumentNullException) { /* expected */ }
        }

        [TestMethod]
        public void SeparateArgsFromCommand_EmptyThrows()
        {
            try
            {
                ProcessTools.SeparateArgsFromCommand("   ");
                Assert.Fail("Expected ArgumentException was not thrown.");
            }
            catch (ArgumentException) { /* expected */ }
        }

        [TestMethod]
        public void SeparateArgsFromCommand_UnclosedQuoteThrows()
        {
            try
            {
                ProcessTools.SeparateArgsFromCommand(@"""C:\unclosed path\unins.exe /SILENT");
                Assert.Fail("Expected FormatException was not thrown.");
            }
            catch (FormatException) { /* expected */ }
        }

        [TestMethod]
        public void SeparateArgsFromCommand_BareExecutableName_NoArgs()
        {
            var result = ProcessTools.SeparateArgsFromCommand("msiexec");
            Assert.AreEqual("msiexec", result.FileName);
            Assert.AreEqual(string.Empty, result.Arguments.Trim());
        }

        [TestMethod]
        public void SeparateArgsFromCommand_BareExecutableName_WithSlashSwitch()
        {
            var result = ProcessTools.SeparateArgsFromCommand("msiexec /x {00000000-0000-0000-0000-000000000000}");
            Assert.AreEqual("msiexec", result.FileName);
            StringAssert.StartsWith(result.Arguments.Trim(), "/x");
        }
    }
}
