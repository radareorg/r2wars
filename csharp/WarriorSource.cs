using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace r2warsTorneo
{
    public sealed class BrowserWarriorSource
    {
        public string Name { get; set; } = "";
        public string Source { get; set; } = "";
    }

    public sealed class WarriorAssemblyResult
    {
        public bool Ok { get; set; }
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
        public int Size { get { return Bytes.Length; } }
        public string Message { get; set; } = "";
    }

    public static class WarriorCompiler
    {
        public const int MaximumWarriorSize = 512;

        public static r2archs.eArch ArchitectureFromName(string name)
        {
            return r2archs.archfromfileext((name ?? "").ToLowerInvariant());
        }

        public static WarriorAssemblyResult Assemble(BrowserWarriorSource warrior)
        {
            if (warrior == null)
                return Failure("No bot source was provided.");

            r2archs.eArch architecture = ArchitectureFromName(warrior.Name);
            if (architecture == r2archs.eArch.unknown)
                return Failure((warrior.Name ?? "Unnamed bot") + ": architecture must be encoded in the filename");
            if (string.IsNullOrWhiteSpace(warrior.Source))
                return Failure((warrior.Name ?? "Unnamed bot") + ": source is empty");
            if (warrior.Source.Length > 64 * 1024)
                return Failure((warrior.Name ?? "Unnamed bot") + ": source is too large to preview");

            string temporaryFile = Path.Combine(Path.GetTempPath(), "r2wars-assemble-" + Guid.NewGuid().ToString("N") + ".asm");
            try
            {
                File.WriteAllText(temporaryFile, warrior.Source);
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = r2paths.rasm2,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                foreach (string argument in r2archs.rasm2param(architecture).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    startInfo.ArgumentList.Add(argument);
                startInfo.ArgumentList.Add("-f");
                startInfo.ArgumentList.Add(temporaryFile);

                using Process process = Process.Start(startInfo);
                if (process == null)
                    return Failure("rasm2 could not be started.");
                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                string stdout = stdoutTask.GetAwaiter().GetResult();
                string stderr = stderrTask.GetAwaiter().GetResult().Trim();
                string hex = new string(stdout.Where(character => !char.IsWhiteSpace(character)).ToArray());

                if (process.ExitCode != 0 || hex.Length == 0 || hex.Length % 2 != 0 || !hex.All(Uri.IsHexDigit))
                    return Failure(stderr.Length > 0 ? stderr : "rasm2 could not assemble this bot.");

                byte[] bytes = new byte[hex.Length / 2];
                for (int index = 0; index < bytes.Length; index++)
                    bytes[index] = Convert.ToByte(hex.Substring(index * 2, 2), 16);
                bool oversized = bytes.Length > MaximumWarriorSize;
                return new WarriorAssemblyResult
                {
                    Ok = !oversized,
                    Bytes = bytes,
                    Message = oversized
                        ? "Compiles, but exceeds the " + MaximumWarriorSize + "-byte warrior limit."
                        : "Compiles successfully."
                };
            }
            catch (Exception exception)
            {
                return Failure(exception.Message);
            }
            finally
            {
                try { File.Delete(temporaryFile); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static WarriorAssemblyResult Failure(string message)
        {
            return new WarriorAssemblyResult { Ok = false, Message = message };
        }
    }
}
