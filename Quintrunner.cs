using EnvDTE;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;

using DProcess = EnvDTE.Process;
using Process = System.Diagnostics.Process;


namespace Quintrunner {
    class Quintrunner {
        static readonly string settingsFileName = "paths.txt";

        static bool startGame = false;
        static bool readLogs = true;
        static bool attachVS = true;
        static bool autoExit = false;
        static string compiledQuintDir = "";
        static string lightningDir = "";
        static string mutatumArgs = "";
        static string lightningArgs = "";
        static List<string> boundVSProjects = new List<string>();
        static string modName = "";
        static List<string> modContents = new List<string>();

        static int Main() {

            string settingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), settingsFileName);
            if (!File.Exists(settingsFilePath)) {
                Console.WriteLine("Didn't find config file at: " + settingsFilePath);
                Console.WriteLine("Do you want to generate the example file? Y/n :");
                string line = Console.ReadLine().Trim();
                if (line != "Y" && line != "Yes") return 0;

                Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath));
                using (var file = File.CreateText(settingsFilePath)) {
                    file.WriteLineAsync(exampleFileData);
                }

                return 0;
            }
            ReadPaths(settingsFileName);
            if (!Directory.Exists(lightningDir)) {
                Console.WriteLine("Didn't find lightning directory at the given path.");
                if (lightningDir == "") Console.WriteLine("LightningDir was not specified.");
                throw new ApplicationException("Didn't find lightning directory.");
            }
            Directory.SetCurrentDirectory(lightningDir);


            if (Directory.Exists(compiledQuintDir)) {
                Console.WriteLine(" -#- Copy Quint Begin.");
                if (File.Exists(Path.Combine(lightningDir, "Quintessential.dll"))) File.Delete(Path.Combine(lightningDir, "Quintessential.dll"));
                File.Copy(Path.Combine(compiledQuintDir, "Quintessential.dll"), Path.Combine(lightningDir, "Quintessential.dll"));
                Console.WriteLine(" -#- Copy Quint End.");
                Console.WriteLine();



                Console.WriteLine(" -#- Mutatum Begin.");
                int mutatumExitCode = RunProcessInConsole(new ProcessStartInfo(Path.Combine(lightningDir, "OpusMutatum.exe"), mutatumArgs), false);
                Console.WriteLine(" -#- Mutatum End.");
                if (mutatumExitCode != 0 && mutatumExitCode != -532462766) { // -532462766 is for the ReadKey unavoidable crash in old versions.
                    Console.WriteLine("Invalid exitcode for mutatum.");
                    Console.ReadKey();
                    throw new ApplicationException("Invalid exitcode for mutatum.");
                }
            }

            if (modName != "") {
                Console.WriteLine();
                Console.WriteLine(" -#- Copy Mod Begin.");
                string modPath = Path.Combine(lightningDir, "Mods", modName);
                if (Directory.Exists(modPath)) Directory.Delete(modPath, true);
                Directory.CreateDirectory(modPath);
                foreach (var path in modContents) {

                    if (Directory.Exists(path)) {
                        string pathWDir = Path.Combine(modPath, Path.GetFileName(path));
                        Console.WriteLine("  Copying directory: " + path);
                        var allDirectories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);

                        foreach (string dir in allDirectories) {
                            string dirToCreate = dir.Replace(path, pathWDir);
                            Directory.CreateDirectory(dirToCreate);
                        }
                        var allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                        foreach (string filePath in allFiles) {
                            File.Copy(filePath, filePath.Replace(path, pathWDir), true);
                        }
                    } else if (File.Exists(path)) {
                        Console.WriteLine("  Copying file: " + path);
                        File.Copy(path, Path.Combine(modPath, Path.GetFileName(path)), true);
                    } else {
                        Console.WriteLine("! Found nothing at: " + path);
                    }
                }
                Console.WriteLine(" -#- Copy Mod End.");
            }




            if (startGame) {
                Console.WriteLine();
                Console.WriteLine(" -#- Game Begin.");
                Process p = Process.Start(Path.Combine(lightningDir, "ModdedLightning.exe"), lightningArgs);
                if (attachVS) AttachProcessToVS(p);

                AsyncStreamRedirector gameLog = null;
                if (readLogs && File.Exists(Path.Combine(lightningDir, "log.txt"))) {
                    System.Threading.Thread.Sleep(1000); // Sleepnig to avoid crashing the game by reading log.txt before the game opens the file
                    FileStream logStream = new FileStream(Path.Combine(lightningDir, "log.txt"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (logStream != null && logStream.CanRead) {
                        Console.WriteLine(" -#- Reading Logs.");
                        gameLog = new AsyncStreamRedirector(logStream, Console.OpenStandardOutput(), false);
                    } 
                }

                p.WaitForExit();
                gameLog?.Cancel();
                Console.WriteLine(" -#- Game End.");
            }

            if (!autoExit) Console.ReadKey();

            return 0;
        }
        static void ReadPaths(string fileName) {
            StreamReader st = new StreamReader(fileName);

            string line;
            ReadingMode readingMode = ReadingMode.None;
            bool isList = false;

            while ((line = st.ReadLine()) != null) {
                if (line == "") continue;
                line = line.Trim(new char[] { ' ' });
                if (line.StartsWith("#")) { readingMode = ReadingMode.None; continue; }

                if (line.StartsWith("*") && readingMode != ReadingMode.None) {
                    line = line.Substring(1).Trim(new char[] { ' ' });
                    isList = true;
                } else if (isList) readingMode = ReadingMode.None;

                if (readingMode == ReadingMode.None) {
                    isList = false;
                    if (line == "CompiledQuintDir:") {
                        readingMode = ReadingMode.CompiledQuintDir;
                    } else if (line == "LightningDir:") {
                        readingMode = ReadingMode.LightningDir;
                    } else if (line == "MutatumArgs:") {
                        readingMode = ReadingMode.MutatumArgs;
                    } else if (line == "LightningArgs:") {
                        readingMode = ReadingMode.LightningArgs;
                    } else if (line == "BoundVSProjects:") {
                        readingMode = ReadingMode.VSBindeProjectPath;
                    } else if (line == "RunGame:") {
                        readingMode = ReadingMode.RunGame;
                    } else if (line == "ReadLogs:") {
                        readingMode = ReadingMode.ReadLogs;
                    } else if (line == "AttachVS:") {
                        readingMode = ReadingMode.AttachVS;
                    } else if (line == "AutoExit:") {
                        readingMode = ReadingMode.AutoExit;
                    } else if (line == "Mod:") {
                        readingMode = ReadingMode.CreateMod;
                    } else if (line == "ModFiles:") {
                        readingMode = ReadingMode.ModContent;
                    }
                } else {
                    setSetting(readingMode, line);
                    if (!isList) readingMode = ReadingMode.None;
                }
            }
        }
        static void setSetting(ReadingMode readingMode, string line) {
            if (readingMode == ReadingMode.CompiledQuintDir) {
                compiledQuintDir = line;
            } else if (readingMode == ReadingMode.LightningDir) {
                lightningDir = line;
            } else if (readingMode == ReadingMode.MutatumArgs) {
                mutatumArgs = mutatumArgs + " " + line;
            } else if (readingMode == ReadingMode.LightningArgs) {
                lightningArgs = lightningArgs + " " + line;
            } else if (readingMode == ReadingMode.VSBindeProjectPath) {
                boundVSProjects.Add(line);
            } else if (readingMode == ReadingMode.RunGame) {
                if (line == "true") startGame = true;
                else if (line == "false") startGame = false;
            } else if (readingMode == ReadingMode.ReadLogs) {
                if (line == "true") readLogs = true;
                else if (line == "false") readLogs = false;
            } else if (readingMode == ReadingMode.AttachVS) {
                if (line == "true") attachVS = true;
                else if (line == "false") attachVS = false;
            } else if (readingMode == ReadingMode.AutoExit) {
                if (line == "true") autoExit = true;
                else if (line == "false") autoExit = false;
            } else if (readingMode == ReadingMode.CreateMod) {
                modName = line;
            } else if (readingMode == ReadingMode.ModContent) {
                modContents.Add(line);
            }
            
        }

        static int RunProcessInConsole(ProcessStartInfo pInfo, bool bindInput = true) {
            pInfo.UseShellExecute = false;
            pInfo.CreateNoWindow = true;
            pInfo.RedirectStandardOutput = true;
            pInfo.RedirectStandardError = true;
            pInfo.RedirectStandardInput = true;
            Process p = Process.Start(pInfo) ?? throw new NullReferenceException("Process can't be null.");

            AsyncStreamRedirector pout = new AsyncStreamRedirector(p.StandardOutput.BaseStream, Console.OpenStandardOutput(), false);
            AsyncStreamRedirector perr = new AsyncStreamRedirector(p.StandardError.BaseStream, Console.OpenStandardError(), false);
            AsyncStreamRedirector pin = null;
            if (bindInput) pin = new AsyncStreamRedirector(Console.OpenStandardInput(), p.StandardInput.BaseStream, false);
            p.WaitForExit();
            int exitCode = p.ExitCode;
            pout.Cancel();
            perr.Cancel();
            pin?.Cancel();
            p.Dispose();
            return exitCode;
        }

        static bool AttachProcessToVS(Process p) {
            foreach (string path in boundVSProjects) {

                DTE visualStudio = VisualStudioAttacher.GetVisualStudioDTE(path);
                if (visualStudio != null) {

                    IEnumerator processes = visualStudio.Debugger.LocalProcesses.GetEnumerator();
                    while (processes.MoveNext()) {
                        if (((DProcess)processes.Current).ProcessID == p.Id) {
                            ((DProcess)processes.Current).Attach();
                            Console.WriteLine("Succesfuly bound OM to VisualStudio.");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        static string exampleFileData = "# This is a comment\r\n# lines starting with # will be ignored\r\n\r\n# This setting specifies the path to the game folder that will be used\r\nLightningDir:\r\n  D:\\C#_projects\\Quintessential\\bin\\Debug\\TestEnvironment\r\n\r\n# LightningArgs:\r\n#   -devEnv -pseudoLang\r\n\r\nRunGame:\r\n  true\r\nReadLogs:\r\n  true\r\nAttachVS:\r\n  true\r\nAutoExit:\r\n  false\r\n\r\n# Some settings accept multiple values at once, use * to specify them\r\nBoundVSProjects:\r\n* D:\\C#_projects\\Quintessential\\Quintessential.sln\r\n* D:\\C#_projects\\Quintrunner\\Quintrunner.sln";

    }
    enum ReadingMode {
        None,
        CompiledQuintDir,
        LightningDir,
        MutatumArgs,
        LightningArgs,
        VSBindeProjectPath,
        RunGame,
        ReadLogs,
        AttachVS,
        CreateMod,
        ModContent,
        AutoExit
    }

    // Based on https://gist.github.com/antopor/5515bed636c3d99395ea
    class AsyncStreamRedirector {
        private readonly CancellationTokenSource cancellation;

        public AsyncStreamRedirector(Stream Source, Stream Sink, bool exitOnEmptySource = true) {
            cancellation = new CancellationTokenSource();

            var task = Task.Run(async () => {
                byte[] buffer = new byte[16384];
                while (!cancellation.IsCancellationRequested) {

                    var inputCount = await Source.ReadAsync(buffer, 0, 16384, cancellation.Token);
                    if (!exitOnEmptySource) {
                        System.Threading.Thread.Sleep(2); // TODO find a better way to wait for new available data 
                    } else if (inputCount <= 0) break;
                    
                    await Sink.WriteAsync(buffer, 0, inputCount, cancellation.Token);
                    await Sink.FlushAsync(cancellation.Token);
                }
            }, cancellation.Token);
        }

        public void Cancel() {
            cancellation.Cancel();
        }
    }

    // Heavily based on https://infosys.beckhoff.com/english.php?content=../content/1033/tc3_automationinterface/242747787.html&id=
    static class VisualStudioAttacher {
        [DllImport("ole32.dll")] private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);
        [DllImport("ole32.dll")] public static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
        private static Hashtable GetRunningObjectTable() {
            Hashtable result = new Hashtable();
            IMoniker[] monikers = new IMoniker[1];
            GetRunningObjectTable(0, out IRunningObjectTable runningObjectTable);
            runningObjectTable.EnumRunning(out IEnumMoniker monikerEnumerator);
            monikerEnumerator.Reset();
            IntPtr numFetched = IntPtr.Zero;
            while (monikerEnumerator.Next(1, monikers, numFetched) == 0) {
                CreateBindCtx(0, out IBindCtx ctx);
                monikers[0].GetDisplayName(ctx, null, out string runningObjectName);
                runningObjectTable.GetObject(monikers[0], out object runningObjectVal);
                result[runningObjectName] = runningObjectVal;
            }
            return result;
        }
        private static Hashtable GetIDEInstances(bool openSolutionsOnly, string progId) {
            Hashtable runningIDEInstances = new Hashtable();
            Hashtable runningObjects = GetRunningObjectTable();
            IDictionaryEnumerator rotEnumerator = runningObjects.GetEnumerator();
            while (rotEnumerator.MoveNext()) {
                string candidateName = (string)rotEnumerator.Key;

                if (!candidateName.StartsWith("!" + progId))
                    continue;
                if (!(rotEnumerator.Value is DTE ide))
                    continue;
                if (openSolutionsOnly) {
                    try {
                        string solutionFile = ide.Solution.FullName;
                        if (solutionFile != String.Empty)
                            runningIDEInstances[candidateName] = ide;
                    } catch { }
                } else
                    runningIDEInstances[candidateName] = ide;
            }
            return runningIDEInstances;
        }
        public static DTE GetVisualStudioDTE(string solutionPath) {
            DTE dte = null;
            Hashtable dteInstances = GetIDEInstances(false, "VisualStudio");
            IDictionaryEnumerator hashtableEnumerator = dteInstances.GetEnumerator();

            while (hashtableEnumerator.MoveNext()) {
                DTE dteTemp = hashtableEnumerator.Value as DTE;
                if (dteTemp.Solution.FullName == solutionPath) {
                    Console.WriteLine("Found Visual Studio instance."); 
                    dte = dteTemp;
                }
            }
            return dte;
        }
    }
}