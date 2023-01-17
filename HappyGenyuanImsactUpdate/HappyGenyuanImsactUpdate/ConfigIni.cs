using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyGenyuanImsactUpdate
{
    //config.ini contains version info
    internal static class ConfigIni
    {
        #region Find Version from zip name
        //index 012345678901234567890
        // zip: game_2.9.0_3.10.0_hdiff_abCdEFgHIjKLMnOP.zip
        public static string FindStartVersion(string zipName)
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

        public static string FindToVersion(string zipName)
        {
            int index = zipName.IndexOf("_hdiff");
            string substr = zipName.Substring(0, index);
            if (index == -1) return string.Empty;
            int veridx = substr.LastIndexOf('_');
            if (veridx == -1) return string.Empty;
            string rtn = substr.Substring(veridx + 1, substr.Length - veridx - 1);
            return VerifyVersionString(rtn) ? rtn : string.Empty;
        }

        public static bool VerifyVersionString(string verstr)
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
        public static void ApplyConfigChange(FileInfo configfile, string version)
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
    }
}
