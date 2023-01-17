using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HappyGenyuanImsactUpdate
{
    public enum CheckMode
    {
        None = 0,
        Basic = 1, //file size
        Full = 2, //size + md5
    }

    internal static class UpCheck
    {
        #region pkg_versions
        /// <summary>
        /// It has two formats:
        /// pkg_version
        /// Audio_[Language]_pkg_version
        /// </summary>
        public static List<string> GetPkgVersion(DirectoryInfo datadir)
        {
            List<string> rtns = new();

            string originVersionPath = $"{datadir.FullName}\\pkg_version";
            if (File.Exists(originVersionPath)) rtns.Add(originVersionPath);
            foreach (var file in datadir.GetFiles())
            {
                if (file.Name.StartsWith("Audio_") && file.Name.EndsWith("_pkg_version"))
                {
#if DEBUG
                    Console.WriteLine(
                        $"The lauguage of this audio package is {file.Name.Substring(6, file.Name.Length - 18)}.");
#endif                        
                    rtns.Add(file.FullName);
                }
            }
            return rtns;
        }
        #endregion

        #region Package Check
        public static bool CheckByPkgVersion(DirectoryInfo datadir, List<string> pkgversionPaths, 
            CheckMode checkAfter, Action<string> log)
        {
            if (checkAfter == CheckMode.None) return true;

            bool checkPassed = true;

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

                            log($"Checking: {checkPathstd}");

                            if (!File.Exists(checkPathstd))
                            {
                                log(ReportFileError(checkPathstd, "The file does not exist"));
                                checkPassed = false;
                                continue;
                            }

                            RemoveReadOnly(checkFile);

                            #region File Size Check
                            long sizeExpected = doce.GetProperty("fileSize").GetInt64();
                            if (checkFile.Length != sizeExpected)
                            {
                                log(ReportFileError(checkPathstd, "The file is not correct"));
                                checkPassed = false;
                                continue;
                            }
                            #endregion

                            if (checkAfter == CheckMode.Full)
                            {
                                #region MD5 Check
                                string md5Expected = doce.GetProperty("md5").GetString();
                                if (MyMD5.GetMD5HashFromFile(checkPathstd) != md5Expected)
                                {
                                    log(ReportFileError(checkPathstd, "The file is not correct"));
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

        private static string ReportFileError(string checkPathstd, string reason)
        {
            return $"{reason} : {checkPathstd}";
        }
        #endregion
    }
}
