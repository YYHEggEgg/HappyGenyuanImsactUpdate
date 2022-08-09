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

            int t = GetZipCount();

            List<FileInfo> zips = new();
            for (int i = 0; i < t; i++)
            {
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
                #endregion

                #region Delete Files
                var deletetxtPath = $"{datadir}\\deletefiles.txt";
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

                foreach (var hdiffFile in hdiffs)
                {
                    File.Delete($"{hdiffFile}.hdiff");
                }
                #endregion

                File.Delete(hdifftxtPath);
                File.Delete(deletetxtPath);

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
                &&!File.Exists($"{datadir}\\{certaingame2}.exe"))
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
        #endregion
    }
}