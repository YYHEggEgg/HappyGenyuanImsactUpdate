using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HappyGenyuanImsactUpdate
{
    internal class Patch
    {
        public Patch(DirectoryInfo datadir, string path7z, string pathHdiff)
        {
            Path7z = path7z;
            PathHdiff = pathHdiff;
            this.datadir = datadir;
        }

        public string Path7z { get; set; }
        public string PathHdiff { get; set; }
        public DirectoryInfo datadir { get; set; }

        #region Patch hdiff
        /// <summary>
        /// Patch hdiff
        /// </summary>
        /// <returns>hdiff files used for deleteing</returns>
        public async Task Hdiff()
        {
            var hdiffs = new List<string>();

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
                            var proc = Process.Start(PathHdiff,
                                $"-f \"{hdiffName}\" \"{hdiffName}.hdiff\" \"{hdiffName}\"");

                            hdiffs.Add(hdiffPathstd);

                            await proc.WaitForExitAsync();
                        }
                    }
                }

                File.Delete(hdifftxtPath);
            }

            // Delete .hdiff afterwards
            foreach (var hdiffFile in hdiffs)
            {
                File.Delete($"{hdiffFile}.hdiff");
            }
        }
        #endregion

        #region Delete Files
        /// <summary>
        /// Process deletedFiles.txt. Notice that files that failed to be deleted will be returned.
        /// </summary>
        /// <returns>files failed to be deleted</returns>
        public List<string> DeleteFiles()
        {
            var delete_delays = new List<string>();

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

            return delete_delays;
        }
        #endregion
    }
}
