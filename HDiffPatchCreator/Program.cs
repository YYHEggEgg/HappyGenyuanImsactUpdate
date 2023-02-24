using HappyGenyuanImsactUpdate;
using System.Diagnostics;
using System.Text;
using System.Web;

namespace HDiffPatchCreator
{
    internal class Program
    {
        static string path7z = $"{Helper.exePath}\\7z.exe";
        static string hdiffzPath = $"{Helper.exePath}\\hdiffz.exe";

        static void Main(string[] args)
        {
            Console.WriteLine("----------Happy hdiff creator----------");

            Helper.CheckForTools();

            Console.WriteLine("This program is used to create Patch from two versions of a certain anime game.");

            string verFrom = "Unknown", verTo = "Unknown", prefix = "game";
            DirectoryInfo? dirFrom = null, dirTo = null, outputAt = null;
            bool createReverse = false, performCheck = true;
            #region Command Line
            bool[] arghaveread = new bool[6];

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
                        default:
                            Usage();
                            return;
                    }
                }
            }
            else
            {
                Usage();
                Console.ReadLine();
                return;
            }
            #endregion

            #region Input Assert
            if (dirFrom == null || dirTo == null || outputAt == null)
            {
                Usage();
                throw new ArgumentException("Input param lack!");
            }
            if (!File.Exists($"{dirFrom}\\{Helper.certaingame1}.exe")
                && !File.Exists($"{dirFrom}\\{Helper.certaingame2}.exe"))
            {
                Console.WriteLine("Invaild game path! (verFrom)");
                Environment.Exit(1);
            }
            if (!File.Exists($"{dirTo}\\{Helper.certaingame1}.exe")
                && !File.Exists($"{dirTo}\\{Helper.certaingame2}.exe"))
            {
                Console.WriteLine("Invaild game path! (verTo)");
                Environment.Exit(1);
            }
            #endregion

            if (performCheck)
            {
                if (!HappyGenyuanImsactUpdate.Program.UpdateCheck(dirFrom, CheckMode.Basic)
                    || !HappyGenyuanImsactUpdate.Program.UpdateCheck(dirTo, CheckMode.Basic))
                {
                    Console.WriteLine("Original files not correct. Not supported in current version.");
                    Environment.Exit(1);
                }
            }

            // Take a snapshot of the file system
            IEnumerable<FileInfo> list1 = dirFrom.GetFiles("*.*", SearchOption.AllDirectories);
            IEnumerable<FileInfo> list2 = dirTo.GetFiles("*.*", SearchOption.AllDirectories);

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
                Console.WriteLine("The two folders are the same!");
            }
            #endregion

            CreatePatch(list1, list2, dirFrom, dirTo,
                $"{outputAt}\\{prefix}_{verFrom}_{verTo}_hdiff_{Randomstr(16)}.zip", cmp);

            if (createReverse)
            {
                CreatePatch(list2, list1, dirTo, dirFrom,
                    $"{outputAt}\\{prefix}_{verTo}_{verFrom}_hdiff_{Randomstr(16)}.zip", cmp);
            }

            #region Multiple Read Assert
            void ReadAssert(int expected)
            {
                if (arghaveread[expected])
                {
                    Console.WriteLine("Duplicated param!");
                    Environment.Exit(1);
                }
                arghaveread[expected] = true;
            }
            #endregion
        }

        static void Usage()
        {
            Console.WriteLine("Usage: hdiffpatchcreator");
            Console.WriteLine("  -from <versionFrom> <source_directory>");
            Console.WriteLine("  -to <versionTo> <target_directory>");
            Console.WriteLine("  -output_to <output_zip_directory>");
            Console.WriteLine("  [-p <prefix>] [-reverse] [--skip-check]");
            Console.WriteLine();
            Console.WriteLine("By using this program, you can get a package named: ");
            Console.WriteLine("[prefix]_<versionFrom>_<versionTo>_hdiff_<randomstr>.zip");
            Console.WriteLine("e.g. game_3.4_8.0_hdiff_nj89iGjh4d.zip");
            Console.WriteLine("If not given, prefix will be 'game'.");
            Console.WriteLine();
            Console.WriteLine("Notice: For the patch creator, MD5 computing when comparing files is essential.");
            Console.WriteLine("You can't choose not to use it.");
        }

        static void CreatePatch(IEnumerable<FileInfo> filesFrom, IEnumerable<FileInfo> filesTo,
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
                Console.WriteLine($"Copying: {file.FullName} -> {newfile.FullName}");
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
                    Console.WriteLine($"Skip: {fromPath.FullName} == {toPath.FullName}");
                    continue;
                }

                if (file.Name.EndsWith("pkg_version"))
                {
                    // pkg_versions shouldn't use hdiff
                    File.Copy(toPath.FullName, $"{tmpFilePath}\\{relativePath}");
                    continue;
                }

                if (InvokeHDiffz(fromPath.FullName, toPath.FullName, diffPath.FullName))
                {
                    if (diffPath.Length >= toPath.Length)
                    {
                        // Fallback to diff
                        Console.WriteLine($"HDiff = {diffPath.Length}, To = {toPath.Length}, fallback to diff");
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
                    Console.WriteLine($"HDiff failed, fallback to diff");
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
            var proc7z = Process.Start(path7z,
                $"a -tzip \"{createpakPath}\" \"{tmpFilePath}\\*\" -mmt");
            proc7z.WaitForExit();
            #endregion

            // Clear temp files
            Directory.Delete(tmpFilePath, true);
        }

        static bool InvokeHDiffz(string source, string target, string outdiffpath, int retry = 5)
        {
            Directory.CreateDirectory(new FileInfo(outdiffpath).DirectoryName);

            var proc = Process.Start(hdiffzPath, $"-f \"{source}\" \"{target}\" \"{outdiffpath}\"");
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                if (retry > 0)
                    return InvokeHDiffz(source, target, outdiffpath, retry - 1);
                else return false;
            }
            else return true;
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
                        Console.WriteLine($"Start Computing MD5 of: {filePath}");
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