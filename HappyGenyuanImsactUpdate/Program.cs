/*******HappyGenyuanImsactUpdate*******/
// A hdiff-using update program of a certain anime game.

using System.Diagnostics;
using System.Text.Json;

namespace HappyGenyuanImsactUpdate
{
    internal class Program
    {
        static string exePath = string.Empty;

        static async Task Main()
        {
            Console.WriteLine("Welcome to the update program!");

            //Not working path, but the path where the program located
            exePath = AppDomain.CurrentDomain.BaseDirectory;
            CheckForTools();

            var path7z = $"{exePath}\\7z.exe";
            var hpatchzPath = $"{exePath}\\hpatchz.exe";

            var datadir = GetDataPath();

            Console.WriteLine();

            // 0 -> none, 1 -> basic check (file size), 2 -> full check (size + md5)
            int checkAfter = AskForCheck();

            Console.WriteLine();

            int t = GetZipCount();

            Console.WriteLine();

            List<FileInfo> zips = new();
            for (int i = 0; i < t; i++)
            {
                Console.WriteLine();
                if (i > 0) Console.WriteLine("Now you should paste the path of another zip file.");
                zips.Add(GetUpdatePakPath());
            }

            foreach (var zipfile in zips)
            {
                #region Unzip the package
                Console.WriteLine("Unzip the package...");

                var pro = Process.Start(path7z, $"x \"{zipfile.FullName}\" -o\"{datadir.FullName}\" -aoa -bsp1");

                await pro.WaitForExitAsync();
                #endregion

                List<string> hdiffs = new();//For deleteing

                #region Patch hdiff
                var hdifftxtPath = $"{datadir}\\hdifffiles.txt";
                if (File.Exists(hdifftxtPath))
                {
                    using (StreamReader hdiffreader = new(hdifftxtPath))
                    {
                        while (true)
                        {
                            string? output = hdiffreader.ReadLine();
                            if (output == null) break;
                            else
                            {
                                var doc = JsonDocument.Parse(output);
                                //{"remoteName": "name.pck"}
                                string hdiffName = datadir.FullName + '\\'
                                    + doc.RootElement.GetProperty("remoteName").GetString();
                                //command:  -f (original file) (patch file)   (output file)
                                //  hpatchz -f name.pck        name.pck.hdiff name.pck
                                string hdiffPathstd = new FileInfo(hdiffName).FullName;
                                var proc = Process.Start(hpatchzPath,
                                    $"-f \"{hdiffName}\" \"{hdiffName}.hdiff\" \"{hdiffName}\"");

                                hdiffs.Add(hdiffPathstd);

                                await proc.WaitForExitAsync();
                            }
                        }
                    }

                    File.Delete(hdifftxtPath);
                }
                #endregion

                #region Delete Files
                var deletetxtPath = $"{datadir}\\deletefiles.txt";
                if (File.Exists(deletetxtPath))
                {
                    using (StreamReader hdiffreader = new(deletetxtPath))
                    {
                        while (true)
                        {
                            string? output = hdiffreader.ReadLine();
                            if (output == null) break;
                            else
                            {
                                string deletedName = datadir.FullName + '\\' + output;
                                File.Delete(deletedName);
                            }
                        }
                    }

                    File.Delete(deletetxtPath);
                }

                foreach (var hdiffFile in hdiffs)
                {
                    File.Delete($"{hdiffFile}.hdiff");
                }
                #endregion

                Console.WriteLine();
                Console.WriteLine();

                if (!UpdateCheck(datadir, checkAfter))
                {
                    Console.WriteLine("Sorry, the update process was exited because files aren't correct.");
                    Console.WriteLine("The program will exit after an enter. ");
                    Console.ReadLine();
                    Environment.Exit(0);
                }
                else Console.WriteLine("Congratulations! Check passed!");

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("---------------------------");
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.WriteLine("Update process is done!");
            Console.WriteLine("Press Enter to continue.");

            Console.ReadLine();
        }

        #region Update Verify
        static bool UpdateCheck(DirectoryInfo datadir, int checkAfter)
        {
            Console.WriteLine("Start verifying...");

            bool checkPassed = true;

            if (checkAfter == 0)
            {
                Console.WriteLine("Due to user's demanding, no checks are performed.");
                return true;
            }

            string pkgversionPath = GetPkgVersion(datadir);
            if (pkgversionPath == string.Empty)
            {
                Console.WriteLine("Can't find version file. No checks are performed.");
                Console.WriteLine("If you can find it, please tell to us: " +
                    "https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/issues");
                return true;
            }

            using (StreamReader versionreader = new(pkgversionPath))
            {
                while (true)
                {
                    string? output = versionreader.ReadLine();
                    if (output == null) break;
                    else
                    {
                        var doce = JsonDocument.Parse(output).RootElement;
                        /* {
                         *      "remoteName": "name.pck",
                         *      "md5": "123456QWERTYUIOPASDFGHJKLZXCVBNM",
                         *      "fileSize": 1919810
                         * }
                         */
                        string checkName = datadir.FullName + '\\'
                            + doce.GetProperty("remoteName").GetString();
                        //command:  -f (original file) (patch file)   (output file)
                        //  hpatchz -f name.pck        name.pck.hdiff name.pck
                        var checkFile = new FileInfo(checkName);
                        string checkPathstd = checkFile.FullName;

                        Console.WriteLine($"Checking: {checkPathstd}");

                        if (!File.Exists(checkPathstd))
                        {
                            ReportFileError(checkPathstd, "The file does not exist");
                            checkPassed = false;
                            continue;
                        }

                        RemoveReadOnly(checkFile);

                        #region File Size Check
                        long sizeExpected = doce.GetProperty("fileSize").GetInt64();
                        if (checkFile.Length != sizeExpected)
                        {
                            ReportFileError(checkPathstd, "The file is not correct");
                            checkPassed = false;
                            continue;
                        }
                        #endregion

                        if (checkAfter == 2)
                        {
                            #region MD5 Check
                            string md5Expected = doce.GetProperty("md5").GetString();
                            if (MyMD5.GetMD5HashFromFile(checkPathstd) != md5Expected)
                            {
                                ReportFileError(checkPathstd, "The file is not correct");
                                checkPassed = false;
                                continue;
                            }
                            #endregion
                        }
                    }
                }
            }

            return checkPassed;
        }

        private static void RemoveReadOnly(FileInfo checkFile)
        {
            if (checkFile.Attributes.HasFlag(FileAttributes.ReadOnly))
                checkFile.Attributes = FileAttributes.Normal;
        }

        private static void ReportFileError(string checkPathstd, string reason)
        {
            Console.WriteLine($"{reason} : {checkPathstd}");
        }
        #endregion

        #region Param Getting
        static void CheckForTools()
        {
            bool ok = true;
            if (!File.Exists($"{exePath}\\7z.exe"))
            {
                Console.WriteLine("7z.exe was missing. " +
                    "Please copy it to the path of this program " +
                    "or download the newest release in " +
                    "https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/releases");
                ok = false;
            }
            if (!File.Exists($"{exePath}\\hpatchz.exe"))
            {
                Console.WriteLine("hpatchz.exe was missing. " +
                    "Please copy it to the path of this program " +
                    "or download the newest release in " +
                    "https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/releases");
                ok = false;
            }
            if (!ok)
            {
                Console.WriteLine("The program will exit after an enter. " +
                    "Please get missing file(s) the right location and restart.");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        //"ei hei"
        const string certaingame1 = "\u0067\u0065\u006e\u0073\u0068\u0069\u006e\u0069\u006d\u0070\u0061\u0063\u0074";
        const string certaingame2 = "\u0079\u0075\u0061\u006e\u0073\u0068\u0065\u006e";

        //For standarlizing, we use a DirectoryInfo object. 
        //The same goes for the following methods. 
        static DirectoryInfo GetDataPath()
        {
            Console.WriteLine("Paste the full path of game directory here. " +
                "It's usually ended with \"Genyuan Imsact game\".");
            string dataPath = Console.ReadLine();
            DirectoryInfo datadir = new(dataPath);
            if (!File.Exists($"{datadir}\\{certaingame1}.exe")
                && !File.Exists($"{datadir}\\{certaingame2}.exe"))
            {
                Console.WriteLine("Invaild game path!");
                return GetDataPath();
            }
            else return datadir;
        }

        static FileInfo GetUpdatePakPath()
        {
            Console.WriteLine("Paste the full path of update package here. " +
                "It should be a zip file.");
            string pakPath = Console.ReadLine();
            FileInfo zipfile = new(pakPath);
            if (zipfile.Extension != ".zip")
            {
                Console.WriteLine("Invaild update package!");
                return GetUpdatePakPath();
            }
            else return zipfile;
        }

        static int GetZipCount()
        {
            int rtn = 0;
            Console.WriteLine("Please type the count of zip file you have.");
            if (!int.TryParse(Console.ReadLine(), out rtn))
            {
                Console.WriteLine("Invaild input!");
                return GetZipCount();
            }
            else return rtn;
        }

        // 0 -> none, 1 -> basic check (file size), 2 -> full check (size + md5)
        static int AskForCheck()
        {
            Console.WriteLine("Do you want to have a check after updating?");
            Console.WriteLine("If you don't want any check, type 0;");
            Console.WriteLine("For a fast check (only compares file size, usually < 10s), type 1;");
            Console.WriteLine("For a full check (scans files, takes a long time, usually > 5 minutes), type 2.");
            int rtn = 0;
            if (!int.TryParse(Console.ReadLine(), out rtn))
            {
                Console.WriteLine("Invaild input!");
                return GetZipCount();
            }
            else if (rtn < 0 || rtn > 2)
            {
                Console.WriteLine("Invaild input!");
                return GetZipCount();
            }
            else return rtn;
        }

        /// <summary>
        /// It has two formats:
        /// pkg_version
        /// Audio_[Language]_pkg_version
        /// </summary>
        static string GetPkgVersion(DirectoryInfo datadir)
        {
            string originVersionPath = $"{datadir.FullName}\\pkg_version";
            if (File.Exists(originVersionPath)) return originVersionPath;
            foreach (var file in datadir.GetFiles())
            {
                if (file.Name.StartsWith("Audio_") && file.Name.EndsWith("_pkg_version"))
                {
                    Console.WriteLine(
                        $"The lauguage of this audio package is {file.Name.Substring(6, file.Name.Length - 18)}.");
                    return file.FullName;
                }
            }
            return string.Empty;
        }
        #endregion
    }
}