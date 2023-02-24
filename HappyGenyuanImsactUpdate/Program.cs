/*******HappyGenyuanImsactUpdate*******/
// A hdiff-using update program of a certain anime game.

using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Numerics;
using System.Web;

namespace HappyGenyuanImsactUpdate
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to the update program!");

            //Not working path, but the path where the program located
            Helper.CheckForTools();

            var path7z = $"{Helper.exePath}\\7z.exe";
            var hpatchzPath = $"{Helper.exePath}\\hpatchz.exe";

            #region Variables
            DirectoryInfo? datadir = null;
            Patch patch;
            CheckMode checkAfter = CheckMode.Null;
            int t = 0;
            List<FileInfo> zips = new();
            bool[] arghaveread = new bool[6];
            #endregion

            #region Console Usage
            if (args.Length == 0)
            {
                Console.WriteLine("You can also use command line like:");
                Usage();

                datadir = GetDataPath();

                Console.WriteLine();
                // 0 -> none, 1 -> basic check (file size), 2 -> full check (size + md5)
                checkAfter = (CheckMode)AskForCheck();

                Console.WriteLine();

                if (!PkgVersionCheck(datadir, checkAfter))
                {
                    Console.WriteLine("Sorry, the update process was exited because the original files aren't correct.");
                    Console.WriteLine("The program will exit after an enter. ");
                    Console.ReadLine();
                    Environment.Exit(0);
                }
                else Console.WriteLine("Congratulations! Check passed!");

                t = GetZipCount();

                Console.WriteLine();

                for (int i = 0; i < t; i++)
                {
                    Console.WriteLine();
                    if (i > 0) Console.WriteLine("Now you should paste the path of another zip file.");
                    zips.Add(GetUpdatePakPath(datadir.FullName));
                }
            }
            #endregion
            #region Command LIne Usage
            else
            {
                #region Remove '"'
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].StartsWith('"') && args[i].EndsWith('"'))
                        args[i] = args[i].Substring(1, args[i].Length - 2);
                }
                #endregion

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
                            case "-patchAt":
                                ReadAssert(0);
                                datadir = new DirectoryInfo(args[i + 1]);
                                i += 1;
                                break;
                            case "-checkmode":
                                ReadAssert(1);
                                checkAfter = (CheckMode)int.Parse(args[i + 1]);
                                i += 1;
                                break;
                            case "-zip_count":
                                ReadAssert(2);
                                t = int.Parse(args[i + 1]);
                                for (int j = 0; j < t; j++)
                                {
                                    zips.Add(new FileInfo(args[i + 2 + j]));
                                }
                                i += t + 1;
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
            }
            #endregion

            #region Input lost Assert
            if (datadir == null || checkAfter == CheckMode.Null || t == 0)
            {
                Usage();
                throw new ArgumentException("Input param lack!");
            }
            #endregion

            patch = new Patch(datadir, path7z, hpatchzPath);

            //Delete the original pkg_version file
            var pkgversionpaths = UpCheck.GetPkgVersion(datadir);
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
                // NOTE: Because some dawn packages from gdrive has a sub folder, we should move it back.
                // Record the directories now
                var predirs = Directory.GetDirectories(datadir.FullName);

                Console.WriteLine("Unzip the package...");
                var pro = Process.Start(path7z, $"x \"{zipfile.FullName}\" -o\"{datadir.FullName}\" -aoa -bsp1");
                await pro.WaitForExitAsync();

                Unzipped.MoveBackSubFolder(datadir, predirs);
                #endregion

                await patch.Hdiff();
                delete_delays.AddRange(patch.DeleteFiles());

                Console.WriteLine();
                Console.WriteLine();
            }

            // For some reasons, the package check is delayed to the end.
            // It is a proper change because only the newest pkg_version is valid.
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

            // Change the config.ini of official launcher
            ConfigChange(datadir, zips[0], zips[zips.Count - 1]);
            Console.WriteLine();

            // Handling with delayed deletions
            foreach (var deletedfile in delete_delays)
                if (File.Exists(deletedfile))
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

        private static void Usage()
        {
            Console.WriteLine("happygenyuanimsactupdate \r\n" +
                "-patchAt <game_directory> \r\n" +
                "-checkmode <0/1/2> (0 -> none, 1 -> basic check (file size), 2 -> full check (size + md5))\r\n" +
                "-zip_count <count> <zipPath1...n>\r\n\r\n" +
                "e.g. happygenyuanimsactupdate -patchAt \"D:\\Game\" -checkmode 1 -zip_count 2 \"game_1_hdiff.zip\" \"zh-cn_hdiff.zip\"\r\n");
        }

        #region Change config for official launcher
        /// <summary>
        /// Change config for official launcher
        /// </summary>
        /// <param name="datadir">Game Data dir</param>
        /// <param name="zipstart">Used for infering the update version</param>
        /// <param name="zipend">Used for infering the update version</param>
        public static void ConfigChange(DirectoryInfo datadir, FileInfo zipstart, FileInfo zipend)
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

            string verstart = ConfigIni.FindStartVersion(zipstart.Name);
            string verto = ConfigIni.FindToVersion(zipend.Name);

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

        /// <summary>
        /// Ask user for applying the inferred update options
        /// </summary>
        /// <param name="configfile">config.ini</param>
        /// <param name="verstart">the update version</param>
        /// <param name="verto">the update version</param>
        public static void GetConfigUpdateOptions(FileInfo configfile, string verstart, string verto)
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
            else if (s.ToLower() == "y")
                ConfigIni.ApplyConfigChange(configfile, verto);
            else if (s.ToLower() == "n") return;
            else if (ConfigIni.VerifyVersionString(s))
                ConfigIni.ApplyConfigChange(configfile, s);
            else
            {
                Console.WriteLine("Invaild version!");
                CustomChangeVersion(configfile);
            }
        }

        /// <summary>
        /// Type a custom version for update
        /// </summary>
        /// <param name="configfile">config.ini</param>
        public static void CustomChangeVersion(FileInfo configfile)
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
            else if (ConfigIni.VerifyVersionString(s))
                ConfigIni.ApplyConfigChange(configfile, s);
            else
            {
                Console.WriteLine("Invaild version!");
                CustomChangeVersion(configfile);
            }
        }
        #endregion

        #region Package Verify
        public static bool UpdateCheck(DirectoryInfo datadir, CheckMode checkAfter)
        {
            Console.WriteLine("Start verifying...");
            Console.WriteLine();

            if (checkAfter == CheckMode.None)
            {
                Console.WriteLine("Due to user's demanding, no checks are performed.");
                return true;
            }

            var pkgversionPaths = UpCheck.GetPkgVersion(datadir);
            if (pkgversionPaths == null || pkgversionPaths.Count == 0)
            {
                Console.WriteLine("Can't find version file. No checks are performed.");
                Console.WriteLine("If you can find it, please tell to us: " +
                    "https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/issues");
                return true;
            }

            return UpCheck.CheckByPkgVersion(datadir, pkgversionPaths, checkAfter,
                str =>
                {
                    Console.WriteLine(str);
                    // Clear the content in Console
                    ClearWrittenLine(str);
                });
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
        static bool PkgVersionCheck(DirectoryInfo datadir, CheckMode checkAfter)
        {
            if (checkAfter == CheckMode.None)
            {
                Console.WriteLine("No checks are performed.");
                return true;
            }

            var pkgversionPaths = UpCheck.GetPkgVersion(datadir);
            if (!pkgversionPaths.Contains($"{datadir}\\pkg_version")) return false;

            // ...\??? game\???_Data\StreamingAssets\Audio\GeneratedSoundBanks\Windows
            string audio1 = $@"{datadir.FullName}\{Helper.certaingame1}_Data\StreamingAssets\Audio\GeneratedSoundBanks\Windows";
            string audio2 = $@"{datadir.FullName}\{Helper.certaingame2}_Data\StreamingAssets\Audio\GeneratedSoundBanks\Windows";
            string[]? audio_pkgversions = null;
            if (Directory.Exists(audio1)) audio_pkgversions = Directory.GetDirectories(audio1);
            else if (Directory.Exists(audio2)) audio_pkgversions = Directory.GetDirectories(audio2);
            else return UpdateCheck(datadir, checkAfter);

            foreach (string audiopath in audio_pkgversions)
            {
                string audioname = new DirectoryInfo(audiopath).Name;
                if (!pkgversionPaths.Contains($"{datadir}\\Audio_{audioname}_pkg_version")) return false;
            }

            return UpdateCheck(datadir, checkAfter);
        }
        #endregion

        #region Param Getting
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
            if (!File.Exists($"{datadir}\\{Helper.certaingame1}.exe")
                && !File.Exists($"{datadir}\\{Helper.certaingame2}.exe"))
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

            // Fuck why I have tested this
            if (pakPath.Length >= 3)
                if (pakPath.Substring(1, 2) != ":\\")
                {
                    //Support relative path
                    pakPath = $"{gamePath}\\{pakPath}";
                    zipfile = new(pakPath);
                }

            //To protect fools who really just paste its name
            if (zipfile.Extension != ".zip" || zipfile.Extension != ".rar" || zipfile.Extension != ".7z")
            {
                if (File.Exists($"{pakPath}.zip")) pakPath += ".zip";
                else if (File.Exists($"{pakPath}.rar")) pakPath += ".rar";
                else if (File.Exists($"{pakPath}.7z")) pakPath += ".7z";
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
