using HappyGenyuanImsactUpdate;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;
using YYHEggEgg.Logger;
using YYHEggEgg.Utils;

namespace HDiffPatchCreator
{
    internal class Program
    {
        static string path7z = $"{Helper.exePath}\\7z.exe";
        static string hdiffzPath = $"{Helper.exePath}\\hdiffz.exe";

        static async Task Main(string[] args)
        {
            Log.Initialize(new LoggerConfig(
                max_Output_Char_Count: -1,
                use_Console_Wrapper: false,
                use_Working_Directory: false,
                global_Minimum_LogLevel: LogLevel.Verbose,
                console_Minimum_LogLevel: LogLevel.Information,
                debug_LogWriter_AutoFlush: true));

            string? version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
            Log.Info($"----------Happy hdiff creator (v{version ?? "<unknown>"})----------");
            Helper.CheckForRunningInZipFile();

            Helper.CheckForTools();

            Log.Info("This program is used to create Patch from two versions of a certain anime game.");

            string verFrom = "Unknown", verTo = "Unknown", prefix = "game";
            DirectoryInfo? dirFrom = null, dirTo = null, outputAt = null;
            bool createReverse = false, performCheck = true, 
                onlyIncludeDefinedFiles = false, includeAudioVersions = false;
            #region Command Line
            bool[] arghaveread = new bool[8];

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith('"') && args[i].EndsWith('"'))
                    args[i] = args[i].Substring(1, args[i].Length - 2);
            }

            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-from":
                            ReadAssert(0);
                            verFrom = HttpUtility.UrlEncode(args[i + 1]);
                            dirFrom = new DirectoryInfo(args[i + 2]);
                            i += 2;
                            break;
                        case "-to":
                            ReadAssert(1);
                            verTo = HttpUtility.UrlEncode(args[i + 1]);
                            dirTo = new DirectoryInfo(args[i + 2]);
                            i += 2;
                            break;
                        case "-output_to":
                            ReadAssert(2);
                            outputAt = new DirectoryInfo(args[i + 1]);
                            i += 1;
                            break;
                        case "-p":
                            ReadAssert(3);
                            prefix = HttpUtility.UrlEncode(args[i + 1]);
                            i += 1;
                            break;
                        case "-reverse":
                            ReadAssert(4);
                            createReverse = true;
                            break;
                        case "--skip-check":
                            ReadAssert(5);
                            performCheck = false;
                            break;
                        case "--only-include-pkg-defined-files":
                            ReadAssert(6);
                            onlyIncludeDefinedFiles = true;
                            break;
                        case "--include-audios":
                            ReadAssert(7);
                            includeAudioVersions = true;
                            break;
                        default:
                            Usage();
                            return;
                    }
                }
            }
            else
            {
                Usage();
                return;
            }
            #endregion

            #region Input Assert
            if (dirFrom == null || dirTo == null || outputAt == null)
            {
                Log.Erro("Input param lack!");
                Usage();
                Environment.Exit(1);
            }
            if (!Helper.AnyCertainGameExists(dirFrom))
            {
                Log.Warn("<color=Yellow>WARNING</color>: No known game executable under game path. (verFrom)");
            }
            if (!Helper.AnyCertainGameExists(dirTo))
            {
                Log.Warn("<color=Yellow>WARNING</color>: No known game executable under game path. (verTo)");
            }
            if (!onlyIncludeDefinedFiles && includeAudioVersions)
            {
                Log.Erro("--include-audios option is only valid when --only-include-pkg-defined-files option exists!", "InputAssert");
                Environment.Exit(1);
            }
            #endregion

            if (performCheck)
            {
                if (!HappyGenyuanImsactUpdate.Program.UpdateCheck(dirFrom, CheckMode.Basic)
                    || !HappyGenyuanImsactUpdate.Program.UpdateCheck(dirTo, CheckMode.Basic))
                {
                    Log.Erro("Original files not correct. Not supported in current version.");
                    Environment.Exit(1);
                }
            }

            // Take a snapshot of the file system
            Log.Info($"Start Emunerating directories, it'll probably take a long time...");
            IEnumerable<FileInfo> list1 = dirFrom.GetFiles("*.*", SearchOption.AllDirectories);
            IEnumerable<FileInfo> list2 = dirTo.GetFiles("*.*", SearchOption.AllDirectories);

            #region Select files (--only-include-pkg-defined-files)
            if (onlyIncludeDefinedFiles)
            {
                List<string> pkgVersions = new()
                    { Path.GetFullPath("pkg_version", dirFrom.FullName) };
                if (includeAudioVersions)
                    pkgVersions.AddRange(UpCheck.GetPkgVersion(dirFrom));

                var definedFiles1 = new SortedSet<string>((
                    from pkgVersion in pkgVersions
                    from json in File.ReadLines(pkgVersion)
                    let doc = JsonDocument.Parse(json)
                    let fullName = Path.GetFullPath(dirFrom.FullName + '/'
                        + doc.RootElement.GetProperty("remoteName").GetString())
                    select fullName).Concat(
                    from pkgVersion in pkgVersions
                    select Path.Combine(dirFrom.FullName, pkgVersion)));
                list1 = from fileInfo in list1
                        where definedFiles1.Contains(fileInfo.FullName)
                        select fileInfo;

                pkgVersions = new()
                    { Path.GetFullPath("pkg_version", dirTo.FullName) };
                if (includeAudioVersions)
                    pkgVersions.AddRange(UpCheck.GetPkgVersion(dirTo));

                var definedFiles2 = new SortedSet<string>((
                    from pkgVersion in pkgVersions
                    from json in File.ReadLines(pkgVersion)
                    let doc = JsonDocument.Parse(json)
                    let fullName = Path.GetFullPath(dirTo.FullName + '/'
                        + doc.RootElement.GetProperty("remoteName").GetString())
                    select fullName).Concat(
                    from pkgVersion in pkgVersions
                    select Path.Combine(dirTo.FullName, pkgVersion)));
                list2 = from fileInfo in list2
                        where definedFiles2.Contains(fileInfo.FullName)
                        select fileInfo;
            }
            #endregion

            //A custom file comparer defined below  
            FileCompare cmp = new FileCompare(dirFrom, dirTo);

            #region Fail Tips
            // This query determines whether the two folders contain  
            // identical file lists, based on the custom file comparer  
            // that is defined in the FileCompare class.  
            // The query executes immediately because it returns a bool.  
            bool areIdentical = list1.SequenceEqual(list2, cmp);

            if (areIdentical == true)
            {
                Log.Warn("The two folders are the same! Seem not need patch.");
                Log.Info($"from {verFrom}: {dirFrom}");
                Log.Info($"to {verTo}: {dirTo}");
                Log.Warn($"Please confirm the paths are true. Press Enter to continue, or Press Ctrl+C to cancel.");
                Console.ReadLine();
            }
            #endregion

            await CreatePatch(list1, list2, dirFrom, dirTo,
                $"{outputAt}\\{prefix}_{verFrom}_{verTo}_hdiff_{Randomstr(16)}.zip", cmp);

            if (createReverse)
            {
                await CreatePatch(list2, list1, dirTo, dirFrom,
                    $"{outputAt}\\{prefix}_{verTo}_{verFrom}_hdiff_{Randomstr(16)}.zip", cmp);
            }

            #region Multiple Read Assert
            void ReadAssert(int expected)
            {
                if (arghaveread[expected])
                {
                    Log.Erro("Duplicated param!");
                    Usage();
                    Environment.Exit(1);
                }
                arghaveread[expected] = true;
            }
            #endregion
        }

        static void Usage()
        {
            Log.Info("Usage: hdiffpatchcreator", "CommandLine");
            Log.Info("  -from <versionFrom> <source_directory>", "CommandLine");
            Log.Info("  -to <versionTo> <target_directory>", "CommandLine");
            Log.Info("  -output_to <output_zip_directory>", "CommandLine");
            Log.Info("  [-p <prefix>] [-reverse] [--skip-check]", "CommandLine");
            Log.Info("  [--only-include-pkg-defined-files [--include-audios]]", "CommandLine");
            Log.Info("", "CommandLine");
            Log.Info("By using this program, you can get a package named: ", "CommandLine");
            Log.Info("[prefix]_<versionFrom>_<versionTo>_hdiff_<randomstr>.zip", "CommandLine");
            Log.Info("e.g. game_3.4_8.0_hdiff_nj89iGjh4d.zip", "CommandLine");
            Log.Info("If not given, prefix will be 'game'.", "CommandLine");
            Log.Info("", "CommandLine");
            Log.Info("-reverse: After package is created, reverse 'versionFrom' and 'versionTo' and create another package.", "CommandLine");
            Log.Info("", "CommandLine");
            Log.Info("--skip-check: skip the check (Basic Mode, only compare file size). ", "CommandLine");
            Log.Info("Notice: For the patch creator, MD5 computing when comparing files is essential.", "CommandLine");
            Log.Info("You can't choose not to use it.", "CommandLine");
            Log.Info("", "CommandLine");
            Log.Info("--only-include-pkg-defined-files: ignore all files not defined in 'pkg_version' file.", "CommandLine");
            Log.Info("  --include-audios: Apply files defined in 'Audio_*_version' with an exception for ignore.", "CommandLine");
        }

        static async Task CreatePatch(IEnumerable<FileInfo> filesFrom, IEnumerable<FileInfo> filesTo,
            DirectoryInfo dirFrom, DirectoryInfo dirTo, string createpakPath, FileCompare cmp)
        {
            var tmpFilePath = $"{new FileInfo(createpakPath).DirectoryName}\\Temp-{Randomstr(32)}";
            Directory.CreateDirectory(tmpFilePath);

            // Files in From but not in To should be deleted
            var fromOnly = filesFrom.Except(filesTo, cmp);
            #region deletefiles.txt
            StringBuilder strb = new();
            foreach (var file in fromOnly)
            {
                strb.AppendLine(FileCompare.GetRelativePath(file, dirFrom));
            }
            File.WriteAllText($"{tmpFilePath}\\deletefiles.txt", strb.ToString());
            #endregion

            // Files in To but not in From could be directly reserved
            var toOnly = filesTo.Except(filesFrom, cmp);
            #region Copy
            foreach (var file in toOnly)
            {
                var newfile = new FileInfo($"{tmpFilePath}\\{FileCompare.GetRelativePath(file, dirTo)}");
                Directory.CreateDirectory(newfile.DirectoryName);
                Log.Info($"Copying: {file.FullName} -> {newfile.FullName}", $"{nameof(CreatePatch)}_FileCopy");
                File.Copy(file.FullName, newfile.FullName);
            }
            #endregion

            // Files in both should create hdiff patch
            var queryCommonFiles = filesFrom.Intersect(filesTo, cmp);
            #region Hdiff Create
            strb = new();
            foreach (var file in queryCommonFiles)
            {
                var relativePath = FileCompare.GetRelativePath(file, dirFrom);

                var fromPath = new FileInfo($"{dirFrom}\\{relativePath}");
                var toPath = new FileInfo($"{dirTo}\\{relativePath}");
                var diffPath = new FileInfo($"{tmpFilePath}\\{relativePath}.hdiff");

                if (cmp.RealEqual(fromPath, toPath))
                {
                    Log.Verb($"Skip: {fromPath.FullName} == {toPath.FullName}", $"{nameof(CreatePatch)}_Hdiff");
                    continue;
                }

                if (file.Name.EndsWith("pkg_version"))
                {
                    // pkg_versions shouldn't use hdiff
                    File.Copy(toPath.FullName, $"{tmpFilePath}\\{relativePath}");
                    continue;
                }

                if (await InvokeHDiffz(fromPath.FullName, toPath.FullName, diffPath.FullName))
                {
                    if (diffPath.Length >= toPath.Length)
                    {
                        // Fallback to diff
                        Log.Info($"HDiff = {diffPath.Length}, To = {toPath.Length}, fallback to diff", $"{nameof(CreatePatch)}_Hdiff");
                        diffPath.Delete();
                        File.Copy(toPath.FullName, $"{tmpFilePath}\\{relativePath}");
                    }
                    else
                    {
                        // Don't mistake it to diffPath!
                        strb.AppendLine($"{{\"remoteName\": \"{relativePath.Replace('\\', '/')}\"}}");
                    }
                }
                else
                {
                    Log.Warn($"HDiff failed, fallback to diff, file fromVer: {fromPath.FullName}, toVer: {toPath.FullName}", $"{nameof(CreatePatch)}_Hdiff");
                    Directory.CreateDirectory(diffPath.DirectoryName);
                    Debug.Assert(false);
                    File.Copy(toPath.FullName, $"{tmpFilePath}\\{relativePath}");
                }
            }

            File.WriteAllText($"{tmpFilePath}\\hdifffiles.txt", strb.ToString());
            #endregion

            #region Write README
            string readme =
                "This is a hdiff update package created by HappuGenyuanImsactUpdate. \n" +
                "For using, you may download our patcher release here: \n" +
                "https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/releases\n" +
                "Then run Updater\\HappyGenyuanImsactUpdate.exe to perform a update.\n" +
                "\n" +
                "Have a good day! Thanks for using!";
            File.WriteAllText($"{tmpFilePath}\\README.txt", readme);
            #endregion

            #region Create Compressed File
            await OuterInvoke.Run(new OuterInvokeInfo
            {
                ProcessPath = path7z,
                CmdLine = $"a -tzip \"{createpakPath}\" \"{tmpFilePath}\\*\" -mmt",
                StartingNotice = "Compressing output zip archive...",
                AutoTerminateReason = "Output compressing failed. You may retry by yourself."
            }, 6);
            #endregion

            // Clear temp files
            Directory.Delete(tmpFilePath, true);
        }

        static async Task<bool> InvokeHDiffz(string source, string target, string outdiffpath, int retry = 5)
        {
            Directory.CreateDirectory(new FileInfo(outdiffpath).DirectoryName);

            return await OuterInvoke.Run(new OuterInvokeInfo
            {
                ProcessPath = hdiffzPath,
                CmdLine = $"-f \"{source}\" \"{target}\" \"{outdiffpath}\""
            }, max_rerun: retry) == 0;
        }

        #region Random
        static Random ran = new Random();

        static string Randomstr(int len)
        {
            string charset = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
            Debug.Assert(charset.Length == 62);
            string res = "";
            while (len-- > 0)
            {
                res += charset[ran.Next(0, 61)];
            }
            return res;
        }
        #endregion
    }

    internal class FileCompare : IEqualityComparer<FileInfo>
    {
        public FileCompare(DirectoryInfo dir1, DirectoryInfo dir2)
        {
            rel1 = dir1;
            rel2 = dir2;
        }

        #region Relative Path
        DirectoryInfo rel1, rel2;

        public static string GetRelativePath(FileInfo info, DirectoryInfo relative_path_start_from)
        {
            string filePath = info.FullName;
            string dirPath = relative_path_start_from.FullName;

            if (!filePath.StartsWith(dirPath))
                throw new ArgumentException("File not in directory so can't create relative path.");

            return filePath.Remove(0, dirPath.Length + 1);
        }

        public static bool TryGetRelativePath(FileInfo info, 
            DirectoryInfo relative_path_start_from, out string rtn)
        {
            string filePath = info.FullName;
            string dirPath = relative_path_start_from.FullName;

            if (!filePath.StartsWith(dirPath))
            {
                rtn = "File not in directory so can't create relative path.";
                return false;
            }
            rtn = filePath.Remove(0, dirPath.Length + 1);
            return true;
        }
        #endregion

        public bool Equals(FileInfo? f1, FileInfo? f2)
        {
            if (f1 == null && f2 == null) return true;
            if (f1 == null || f2 == null) return false;
            bool tryf1 = TryGetRelativePath(f1, rel1, out string relf1);
            if (!tryf1)
            {
                relf1 = GetRelativePath(f1, rel2);
            }
            bool tryf2 = TryGetRelativePath(f2, rel2, out string relf2);
            if (!tryf2)
            {
                relf2 = GetRelativePath(f2, rel1);
            }
            return relf1 == relf2;
        }

        private Dictionary<string, string> _MD5memory = new();

        /// <summary>
        /// MD5 memory. Invoke to get a file's MD5, so you can get it without read file again next time.
        /// </summary>
        /// <param name="filePath">File's Full path</param>
        /// <returns>MD5 value of this file</returns>
        public string this[string filePath]
        {
            get
            {
                lock (this)
                {
                    if (!_MD5memory.ContainsKey(filePath))
                    {
                        Log.Info($"Start Computing MD5 of: {filePath}");
                        _MD5memory.Add(filePath, MyMD5.GetMD5HashFromFile(filePath));
                    }
                    return _MD5memory[filePath];
                }
            }
        }

        public bool RealEqual(FileInfo? a, FileInfo? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            Debug.Assert(a.Name == b.Name);
            if (a.Length != b.Length) return false;
            return this[a.FullName] == this[b.FullName];
        }

        // Return a hash that reflects the comparison criteria. According to the
        // rules for IEqualityComparer<T>, if Equals is true, then the hash codes must  
        // also be equal. Because equality as defined here is a simple value equality, not  
        // reference identity, it is possible that two or more objects will produce the same  
        // hash code.  
        public int GetHashCode(FileInfo fi)
        {
            bool tryf1 = TryGetRelativePath(fi, rel1, out string relf1);
            if (!tryf1)
            {
                relf1 = GetRelativePath(fi, rel2);
            }
            return relf1.GetHashCode();
        }
    }
}