/*******HappyGenyuanImsactUpdate*******/
// A hdiff-using update program of a certain anime game.

using Microsoft.Toolkit.Uwp.Notifications;
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

            if (!PkgVersionCheck(datadir, checkAfter))
            {
                Console.WriteLine("Sorry, the update process was exited because the original files aren't correct.");
                Console.WriteLine("The program will exit after an enter. ");
                Console.ReadLine();
                Environment.Exit(0);
            }
            else Console.WriteLine("Congratulations! Check passed!");

            int t = GetZipCount();

            Console.WriteLine();

            List<FileInfo> zips = new();
            for (int i = 0; i < t; i++)
            {
                Console.WriteLine();
                if (i > 0) Console.WriteLine("Now you should paste the path of another zip file.");
                zips.Add(GetUpdatePakPath(datadir.FullName));
            }

            //Delete the original pkg_version file
            var pkgversionpaths = GetPkgVersion(datadir);
            foreach (var pkgversionpath in pkgversionpaths)
            {
                File.Delete(pkgversionpath);
            }

            // Due to some reasons, if the deleted files are not there,
            // we'll try to delete them afterwards.
            List<string> delete_delays = new();

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
                                if (File.Exists(deletedName))
                                    File.Delete(deletedName);
                                else delete_delays.Add(deletedName);
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
                    //Require Windows 10.0.17763.0+
                    if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
                    {
                        // Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
                        new ToastContentBuilder()
                            .AddArgument("action", "viewConversation")
                            .AddText("Update failed.")
                            .AddText("Sorry, the update process was exited because files aren't correct.")
                            .Show();
                    }

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

            ConfigChange(datadir, zips[0], zips[zips.Count - 1]);
            Console.WriteLine();

            // Handling with delayed deletions
            foreach (var deletedfile in delete_delays)
                File.Delete(deletedfile);

            //Require Windows 10.0.17763.0+
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
            {
                // Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
                new ToastContentBuilder()
                    .AddArgument("action", "viewConversation")
                    .AddText("Update process is done!")
                    .AddText("Enjoy the new version!")
                    .Show();
            }

            DeleteZipFiles(zips);
            Console.WriteLine();

            Console.WriteLine("Update process is done!");
            Console.WriteLine("Press Enter to continue.");

            Console.ReadLine();
        }

        //config.ini contains version info
        #region Change config for official launcher
        static void ConfigChange(DirectoryInfo datadir, FileInfo zipstart, FileInfo zipend)
        {
            if (!File.Exists($"{datadir}\\config.ini")) return;

            //Require Windows 10.0.17763.0+
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763, 0))
            {
                // Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
                new ToastContentBuilder()
                    .AddArgument("action", "viewConversation")
                    .AddText("The update program meets some problem.")
                    .AddText("Check it out in the console.")
                    .Show();
            }

            Console.WriteLine("We have noticed that you're probably using an official launcher.");
            Console.WriteLine("To make it display the correct version, we would make some change on related file.");

            string verstart = FindStartVersion(zipstart.Name);
            string verto = FindToVersion(zipend.Name);

            FileInfo configfile = new($"{datadir}\\config.ini");

            if (verstart == string.Empty || verto == string.Empty)
            {
                Console.WriteLine("We can't infer the version you're updating to.");
                CustomChangeVersion(configfile);
            }
            else
            {
                GetConfigUpdateOptions(configfile, verstart, verto);
            }
        }

        static void GetConfigUpdateOptions(FileInfo configfile, string verstart, string verto)
        {
            Console.WriteLine($"We infer that you're updating from {verstart} to {verto} .");
            Console.WriteLine("Is it true? Type 'y' to apply the change " +
                "or type the correct version you're updating to.");
            Console.WriteLine("If you don't use a launcher or don't want to change the display version, type 'n' to refuse it.");
            string? s = Console.ReadLine();
            if (s == null || s == string.Empty)
            {
                Console.WriteLine("Invaild version!");
                CustomChangeVersion(configfile);
            }
            else if (s.ToLower() == "y") ApplyConfigChange(configfile, verto);
            else if (s.ToLower() == "n") return;
            else if (VerifyVersionString(s)) ApplyConfigChange(configfile, s);
            else
            {
                Console.WriteLine("Invaild version!");
                CustomChangeVersion(configfile);
            }
        }

        static void CustomChangeVersion(FileInfo configfile)
        {
            Console.WriteLine("Please type the version you're updating to, and we'll apply the change:");
            Console.WriteLine("If you don't use a launcher or don't want to change the display version, type 'n' to refuse it.");

            string? s = Console.ReadLine();
            if (s == null || s == string.Empty)
            {
                Console.WriteLine("Invaild version!");
                CustomChangeVersion(configfile);
            }
            else if (s.ToLower() == "n") return;
            else if (VerifyVersionString(s)) ApplyConfigChange(configfile, s);
            else
            {
                Console.WriteLine("Invaild version!");
                CustomChangeVersion(configfile);
            }
        }

        #region Find Version from zip name
        //      012345678901234567890
        // zip: game_2.9.0_3.10.0_hdiff_abCdEFgHIjKLMnOP.zip
        static string FindStartVersion(string zipName)
        {
            int index = zipName.IndexOf("_hdiff");
            if (index == -1) return string.Empty;
            string substr = zipName.Substring(0, index);
            int veridx = substr.IndexOf('_');
            int endidx = substr.IndexOf('_', veridx + 1);
            if (veridx == -1 || endidx == -1) return string.Empty;
            string rtn = substr.Substring(veridx + 1, endidx - veridx - 1);
            return VerifyVersionString(rtn) ? rtn : string.Empty;
        }

        static string FindToVersion(string zipName)
        {
            int index = zipName.IndexOf("_hdiff");
            string substr = zipName.Substring(0, index);
            if (index == -1) return string.Empty;
            int veridx = substr.LastIndexOf('_');
            if (veridx == -1) return string.Empty;
            string rtn = substr.Substring(veridx + 1, substr.Length - veridx - 1);
            return VerifyVersionString(rtn) ? rtn : string.Empty;
        }

        static bool VerifyVersionString(string verstr)
        {
            var strs = verstr.Split('.');
            foreach (string str in strs)
            {
                if (!int.TryParse(str, out _)) return false;
            }
            return true;
        }
        #endregion

        // i don't want to write a real ini writer lol
        static void ApplyConfigChange(FileInfo configfile, string version)
        {
            string fullfile = string.Empty;
            using (StreamReader reader = new(configfile.FullName))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null || line == string.Empty) break;
                    if (line.StartsWith("game_version="))
                        line = $"game_version={version}";
                    fullfile += line + "\r\n";
                }
            }

            File.WriteAllText(configfile.FullName, fullfile);
        }
        #endregion

        #region Package Verify
        static bool UpdateCheck(DirectoryInfo datadir, int checkAfter)
        {
            Console.WriteLine("Start verifying...");
            Console.WriteLine();

            bool checkPassed = true;

            if (checkAfter == 0)
            {
                Console.WriteLine("Due to user's demanding, no checks are performed.");
                return true;
            }

            var pkgversionPaths = GetPkgVersion(datadir);
            if (pkgversionPaths.Count == 0)
            {
                Console.WriteLine("Can't find version file. No checks are performed.");
                Console.WriteLine("If you can find it, please tell to us: " +
                    "https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/issues");
                return true;
            }
            foreach (var pkgversionPath in pkgversionPaths)
            {
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
                            // Clear the content in Console
                            ClearWrittenLine($"Checking: {checkPathstd}");

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

        #region Clear the Written Content in Console
        // Reference:
        // [ Can Console.Clear be used to only clear a line instead of whole console? ]
        // https://stackoverflow.com/questions/8946808/can-console-clear-be-used-to-only-clear-a-line-instead-of-whole-console
        
        private static void ClearSingleLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        private static void ClearWrittenLine(string wstr)
        {
            int times = (int)Math.Ceiling((decimal)wstr.Length / Console.WindowWidth);
            while (times-- > 0) ClearSingleLine();
        }
        #endregion

        // Check if pkg_version and Audio_pkg_version can match the real condition
        static bool PkgVersionCheck(DirectoryInfo datadir, int checkAfter)
        {
            var pkgversionPaths = GetPkgVersion(datadir);
            if (!pkgversionPaths.Contains("pkg_version")) return false;

            // ...\??? game\???_Data\StreamingAssets\Audio\GeneratedSoundBanks\Windows
            string audio1 = $@"{datadir.FullName}\{certaingame1}_Data\StreamingAssets\Audio\GeneratedSoundBanks\Windows";
            string audio2 = $@"{datadir.FullName}\{certaingame2}_Data\StreamingAssets\Audio\GeneratedSoundBanks\Windows";
            string[]? audio_pkgversions = null;
            if (Directory.Exists(audio1)) audio_pkgversions = Directory.GetDirectories(audio1);
            else if (Directory.Exists(audio2)) audio_pkgversions = Directory.GetDirectories(audio2);
            else return false;

            foreach (string audioname in audio_pkgversions)
            {
                if (!pkgversionPaths.Contains($"Audio_{audioname}_pkg_version")) return false;
            }

            return UpdateCheck(datadir, checkAfter);
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
            string? dataPath = Console.ReadLine();
            if (dataPath == null || dataPath == string.Empty)
            {
                Console.WriteLine("Invaild game path!");
                return GetDataPath();
            }

            DirectoryInfo datadir = new(dataPath);
            if (!File.Exists($"{datadir}\\{certaingame1}.exe")
                && !File.Exists($"{datadir}\\{certaingame2}.exe"))
            {
                Console.WriteLine("Invaild game path!");
                return GetDataPath();
            }
            else return datadir;
        }

        static FileInfo GetUpdatePakPath(string gamePath)
        {
            Console.WriteLine("Paste the full path of update package here. " +
                "It should be a zip file.");
            Console.WriteLine("If it's under the game directory, you can just paste the name of zip file here.");
            string? pakPath = Console.ReadLine();
            if (pakPath == null || pakPath == string.Empty)
            {
                Console.WriteLine("Invaild update package!");
                return GetUpdatePakPath(gamePath);
            }

            FileInfo zipfile = new(pakPath);

            if (pakPath.Substring(1, 2) != ":\\")
            {
                //Support relative path
                pakPath = $"{gamePath}\\{pakPath}";
                zipfile = new(pakPath);
            }

            //To protect fools who really just paste its name
            if (zipfile.Extension != ".zip")
            {
                pakPath += ".zip";
                zipfile = new(pakPath);
            }

            if (!zipfile.Exists)
            {
                Console.WriteLine("Invaild update package!");
                return GetUpdatePakPath(gamePath);
            }

            return zipfile;
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
            Console.WriteLine("For a fast check (recommended, only compares file size, usually < 10s), type 1;");
            Console.WriteLine("For a full check (scans files, takes a long time, usually > 5 minutes), type 2.");
            int rtn = 0;
            if (!int.TryParse(Console.ReadLine(), out rtn))
            {
                Console.WriteLine("Invaild input!");
                return AskForCheck();
            }
            else if (rtn < 0 || rtn > 2)
            {
                Console.WriteLine("Invaild input!");
                return AskForCheck();
            }
            else return rtn;
        }

        /// <summary>
        /// It has two formats:
        /// pkg_version
        /// Audio_[Language]_pkg_version
        /// </summary>
        static List<string> GetPkgVersion(DirectoryInfo datadir)
        {
            List<string> rtns = new();

            string originVersionPath = $"{datadir.FullName}\\pkg_version";
            if (File.Exists(originVersionPath)) rtns.Add(originVersionPath);
            foreach (var file in datadir.GetFiles())
            {
                if (file.Name.StartsWith("Audio_") && file.Name.EndsWith("_pkg_version"))
                {
                    Console.WriteLine(
                        $"The lauguage of this audio package is {file.Name.Substring(6, file.Name.Length - 18)}.");
                    rtns.Add(file.FullName);
                }
            }
            return rtns;
        }
        #endregion

        #region Delete Update Zip File
        static void DeleteZipFiles(List<FileInfo> zips)
        {
            Console.WriteLine("The pre-download packages aren't needed any more.");
            Console.WriteLine("Do you want to delete them? Type 'y' to accept or 'n' to refuse.");
            string? s = Console.ReadLine();
            if (s == null)
            {
                Console.WriteLine("Invaild input!");
                DeleteZipFiles(zips);
                return;
            }
            else if (s.ToLower() == "y")
            {
                foreach (var zip in zips)
                {
                    zip.Delete();
                }
            }
            else if (s.ToLower() == "n")
            {
                return;
            }
            else
            {
                Console.WriteLine("Invaild input!");
                DeleteZipFiles(zips);
                return;
            }
        }
        #endregion
    }
}