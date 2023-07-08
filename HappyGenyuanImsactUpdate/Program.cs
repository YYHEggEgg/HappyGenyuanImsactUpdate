/*******HappyGenyuanImsactUpdate*******/
// A hdiff-using update program of a certain anime game.

using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Numerics;
using System.Web;
using YYHEggEgg.Logger;

namespace HappyGenyuanImsactUpdate
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Log.Initialize(new LoggerConfig(
                max_Output_Char_Count: -1,
                use_Console_Wrapper: false,
                use_Working_Directory: true,
                global_Minimum_LogLevel: LogLevel.Verbose,
                console_Minimum_LogLevel: LogLevel.Information, 
                debug_LogWriter_AutoFlush: true));

            Log.Info($"Welcome to the update program! (v{Environment.Version})");
            Helper.CheckForRunningInZipFile();

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
            bool ifconfigchange = true;
            bool? ifdeletepackage = null;
            bool[] arghaveread = new bool[6];

            bool usingcommandline = false;
            #endregion

            #region Console Usage
            if (args.Length == 0)
            {
                Log.Info("You can also use command line args to execute this program.", "CommandLine");

                datadir = GetDataPath();

                Log.Info("");
                // 0 -> none, 1 -> basic check (file size), 2 -> full check (size + md5)
                checkAfter = (CheckMode)AskForCheck();

                Log.Info("");

                if (!PkgVersionCheck(datadir, checkAfter))
                {
                    Log.Erro("Sorry, the update process was exited because the original files aren't correct.", nameof(PkgVersionCheck));
                    Log.Erro("Press any key to continue. ", nameof(PkgVersionCheck));
                    Console.Read();
                    Environment.Exit(1);
                }
                else Log.Info("Congratulations! Check passed!", nameof(PkgVersionCheck));

                t = GetZipCount();

                Log.Info("");

                for (int i = 0; i < t; i++)
                {
                    Log.Info("");
                    if (i > 0) Log.Info("Now you should paste the path of another zip file.", nameof(GetUpdatePakPath));
                    zips.Add(GetUpdatePakPath(datadir.FullName));
                }
            }
            #endregion
            #region Command LIne Usage
            else
            {
                usingcommandline = true;

                #region Remove '.\'
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].StartsWith('.'))
                        args[i] = args[i].Substring(1);
                }
                #endregion

                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = RemoveDoubleQuotes(args[i]) ?? string.Empty;
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
                            case "--config_change_guidance":
                                ReadAssert(3);
                                ifconfigchange = bool.Parse(args[i + 1]);
                                i += 1;
                                break;
                            case "--delete_update_packages":
                                ReadAssert(4);
                                ifdeletepackage = true;
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

                ifdeletepackage ??= false;
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

            // Backup the original pkg_version file
            var pkgversionpaths = UpCheck.GetPkgVersion(datadir);
            foreach (var pkgversionpath in pkgversionpaths)
            {
                FileInfo pkgver = new(pkgversionpath);
                if (pkgver.Name == "pkg_version") continue;
                File.Move(pkgversionpath, $"{Helper.tempPath}\\{pkgver.Name}");
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

                Log.Info("Unzip the package...", "OuterInvoke");
                var pro = Process.Start(path7z, $"x \"{zipfile.FullName}\" -o\"{datadir.FullName}\" -aoa -bsp1");
                await pro.WaitForExitAsync();

                Unzipped.MoveBackSubFolder(datadir, predirs);
                #endregion

                await patch.Hdiff();
                delete_delays.AddRange(patch.DeleteFiles());

                Log.Info("\n\n");
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

                Log.Erro("Sorry, the update process was exited because the original files aren't correct.", nameof(PkgVersionCheck));
                Log.Erro("Press any key to continue. ", nameof(PkgVersionCheck));
                Console.Read();
                Environment.Exit(1);
            }
            else Log.Info("Congratulations! Check passed!", nameof(PkgVersionCheck));

            foreach (var pkgversionpath in pkgversionpaths)
            {
                FileInfo pkgver = new(pkgversionpath);
                if (pkgver.Name == "pkg_version") continue;
                if (pkgver.Exists) continue; // pkg_version Overrided

                var backuppath = $"{Helper.tempPath}\\{pkgver.Name}";
                File.Move(backuppath, pkgversionpath);
                if (checkAfter == CheckMode.None)
                {
                    Log.Warn($"{pkgver.Name} hasn't checked and may not fit the current version.");
                    continue;
                }

                var checkres = UpCheck.CheckByPkgVersion(datadir, pkgversionpath, checkAfter);

                if (!checkres)
                {
                    Log.Warn($"{pkgver.Name} isn't fit with current version any more. You may fix the error or remove the file under the game data directory.");
                }
            }

            Log.Info("\n\n\n\n\n---------------------\n\n\n\n\n");

            // Change the config.ini of official launcher
            if ((usingcommandline && ifconfigchange) || !usingcommandline)
                ConfigChange(datadir, zips[0], zips[zips.Count - 1]);

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

            DeleteZipFilesReq(zips, ifdeletepackage);
            Log.Info("-------------------------");

            Helper.TryDisposeTempFiles();

            Log.Info("Update process is done!");

            Log.Info("Press Enter to continue.");

            Console.ReadLine();

            #region Multiple Read Assert
            void ReadAssert(int expected)
            {
                if (arghaveread[expected])
                {
                    Log.Info("Duplicated param!");
                    Usage();
                    Environment.Exit(1);
                }
                arghaveread[expected] = true;
            }
            #endregion
        }

        private static void Usage()
        {
            Log.Info("CommandLine usage: \n" +
                "happygenyuanimsactupdate \n" +
                "-patchAt <game_directory> \n" +
                "-checkmode <0/1/2> (0 -> none, 1 -> basic check (file size), 2 -> full check (size + md5))\n" +
                "-zip_count <count> <zipPath1...n> \n" +
                "[--config_change_guidance <true/false>] (change the showing version of official launcher, default is true)\n" +
                "[--delete_update_packages] (delete update packages, won't delete if the param isn't given)" +
                "\n\n" +
                "e.g. happygenyuanimsactupdate -patchAt \"D:\\Game\" -checkmode 1 -zip_count 2 \"game_1_hdiff.zip\" \"zh-cn_hdiff.zip\" " +
                "--config_change_guidance false\n", "CommandLine");
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

            Log.Info("We have noticed that you're probably using an official launcher.", nameof(ConfigChange));
            Log.Info("To make it display the correct version, we would make some change on related file.", nameof(ConfigChange));

            string verstart = ConfigIni.FindStartVersion(zipstart.Name);
            string verto = ConfigIni.FindToVersion(zipend.Name);

            FileInfo configfile = new($"{datadir}\\config.ini");

            if (verstart == string.Empty || verto == string.Empty)
            {
                Log.Warn("We can't infer the version you're updating to.", nameof(ConfigChange));
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
            Log.Info($"We infer that you're updating from {verstart} to {verto} .", nameof(ConfigChange));
            Log.Info("Is it true? Type 'y' to apply the change " +
                "or type the correct version you're updating to.", nameof(ConfigChange));
            Log.Info("If you don't use a launcher or don't want to change the display version, type 'n' to refuse it.", nameof(ConfigChange));
            string? s = Console.ReadLine();
            if (s == null || s == string.Empty)
            {
                Log.Warn("Invaild version!", nameof(ConfigChange));
                CustomChangeVersion(configfile);
            }
            else if (s.ToLower() == "y")
                ConfigIni.ApplyConfigChange(configfile, verto);
            else if (s.ToLower() == "n") return;
            else if (ConfigIni.VerifyVersionString(s))
                ConfigIni.ApplyConfigChange(configfile, s);
            else
            {
                Log.Warn("Invaild version!", nameof(ConfigChange));
                CustomChangeVersion(configfile);
            }
        }

        /// <summary>
        /// Type a custom version for update
        /// </summary>
        /// <param name="configfile">config.ini</param>
        public static void CustomChangeVersion(FileInfo configfile)
        {
            Log.Info("Please type the version you're updating to, and we'll apply the change:", nameof(CustomChangeVersion));
            Log.Info("If you don't use a launcher or don't want to change the display version, type 'n' to refuse it.", nameof(CustomChangeVersion));

            string? s = Console.ReadLine();
            if (s == null || s == string.Empty)
            {
                Log.Warn("Invaild version!", nameof(CustomChangeVersion));
                CustomChangeVersion(configfile);
            }
            else if (s.ToLower() == "n") return;
            else if (ConfigIni.VerifyVersionString(s))
                ConfigIni.ApplyConfigChange(configfile, s);
            else
            {
                Log.Warn("Invaild version!", nameof(CustomChangeVersion));
                CustomChangeVersion(configfile);
            }
        }
        #endregion

        #region Package Verify
        public static bool UpdateCheck(DirectoryInfo datadir, CheckMode checkAfter)
        {
            Log.Info("Start verifying...\n", nameof(UpdateCheck));

            if (checkAfter == CheckMode.None)
            {
                Log.Info("Due to user's demanding, no checks are performed.", nameof(UpdateCheck));
                return true;
            }

            var pkgversionPaths = UpCheck.GetPkgVersion(datadir);
            if (pkgversionPaths == null || pkgversionPaths.Count == 0)
            {
                Log.Info("Can't find version file. No checks are performed.", nameof(UpdateCheck));
                Log.Info("If you can find it, please tell to us: " +
                    "https://github.com/YYHEggEgg/HappyGenyuanImsactUpdate/issues", nameof(UpdateCheck));
                return true;
            }

            return UpCheck.CheckByPkgVersion(datadir, pkgversionPaths, checkAfter);
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
            // int times = (int)Math.Ceiling((decimal)wstr.Length / Console.WindowWidth);
            // while (times-- > 0) ClearSingleLine();
        }
        #endregion

        // Check if pkg_version and Audio_pkg_version can match the real condition
        static bool PkgVersionCheck(DirectoryInfo datadir, CheckMode checkAfter)
        {
            if (checkAfter == CheckMode.None)
            {
                Log.Info("No checks are performed.", nameof(PkgVersionCheck));
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
            Log.Info("Paste the full path of game directory here. " +
                "It's usually ended with \"Genyuan Imsact game\".", nameof(GetDataPath));
            string? dataPath = RemoveDoubleQuotes(Console.ReadLine());
            if (dataPath == null || dataPath == string.Empty)
            {
                Log.Warn("Invaild game path!", nameof(GetDataPath));
                return GetDataPath();
            }

            DirectoryInfo datadir = new(dataPath);
            if (!File.Exists($"{datadir}\\{Helper.certaingame1}.exe")
                && !File.Exists($"{datadir}\\{Helper.certaingame2}.exe"))
            {
                Log.Warn("Invaild game path!", nameof(GetDataPath));
                return GetDataPath();
            }
            else return datadir;
        }

        static FileInfo GetUpdatePakPath(string gamePath)
        {
            Log.Info("Paste the full path of update package here. " +
                "It should be a zip file.", nameof(GetUpdatePakPath));
            Log.Info("If it's under the game directory, you can just paste the name of zip file here.", nameof(GetUpdatePakPath));
            string? pakPath = RemoveDoubleQuotes(Console.ReadLine());
            if (pakPath == null || pakPath == string.Empty)
            {
                Log.Warn("Invaild update package!", nameof(GetUpdatePakPath));
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
                Log.Warn("Invaild update package!", nameof(GetUpdatePakPath));
                return GetUpdatePakPath(gamePath);
            }

            return zipfile;
        }

        static int GetZipCount()
        {
            int rtn = 0;
            Log.Info("Please type the count of zip file you have.", nameof(GetUpdatePakPath));
            if (!int.TryParse(Console.ReadLine(), out rtn))
            {
                Log.Warn("Invaild input!", nameof(GetUpdatePakPath));
                return GetZipCount();
            }
            else return rtn;
        }

        // 0 -> none, 1 -> basic check (file size), 2 -> full check (size + md5)
        static int AskForCheck()
        {
            Log.Info("Do you want to have a check after updating?", nameof(AskForCheck));
            Log.Info("If you don't want any check, type 0;", nameof(AskForCheck));
            Log.Info("For a fast check (recommended, only compares file size, usually < 10s), type 1;", nameof(AskForCheck));
            Log.Info("For a full check (scans files, takes a long time, usually > 5 minutes), type 2.", nameof(AskForCheck));
            int rtn = 0;
            if (!int.TryParse(Console.ReadLine(), out rtn))
            {
                Log.Warn("Invaild input!", nameof(AskForCheck));
                return AskForCheck();
            }
            else if (rtn < 0 || rtn > 2)
            {
                Log.Warn("Invaild input!", nameof(AskForCheck));
                return AskForCheck();
            }
            else return rtn;
        }
        #endregion

        #region Delete Update Zip File
        /// <param name="delete">true=delete; false=reserve; null=not given, ask the user</param>
        static void DeleteZipFilesReq(List<FileInfo> zips, bool? delete = null)
        {
            if (delete == null)
            {
                Log.Info("The pre-download packages aren't needed any more.", nameof(DeleteZipFilesReq));
                Log.Info("Do you want to delete them? Type 'y' to accept or 'n' to refuse.", nameof(DeleteZipFilesReq));
                string? s = Console.ReadLine();
                if (s == null)
                {
                    Log.Warn("Invaild input!", nameof(DeleteZipFilesReq));
                    DeleteZipFilesReq(zips);
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
                    Log.Warn("Invaild input!", nameof(DeleteZipFilesReq));
                    DeleteZipFilesReq(zips);
                    return;
                }
            }
            else
            {
                if ((bool)delete)
                {
                    foreach (var zip in zips)
                    {
                        zip.Delete();
                    }
                }
            }
        }
        #endregion

        public static string? RemoveDoubleQuotes(string? str)
        {
            if (str == null) return null;
            if (str.StartsWith('"') && str.EndsWith('"'))
                return str.Substring(1, str.Length - 2);
            else return str;
        }
    }
}