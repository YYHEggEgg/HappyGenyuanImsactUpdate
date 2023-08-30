namespace HappyGenyuanImsactUpdate
{
    // NOTE: Because some dawn packages from gdrive has a sub folder, we should move it back.
    internal class Unzipped
    {
        #region Move Back Sub Folder from zip file
        public static void MoveBackSubFolder(DirectoryInfo datadir, string[]? predirs)
        {
            var nowdirs = Directory.GetDirectories(datadir.FullName);
            var newappeared_dirs = GetNewlyAppearedFolders(predirs, nowdirs);
            foreach (var dir in newappeared_dirs)
            {
                MoveDir(dir, datadir.FullName);
            }
        }

        private static List<string> GetNewlyAppearedFolders(string[]? predirs, string[]? nowdirs)
        {
            List<string> newdirs = new();
            foreach (var dir in nowdirs)
            {
                if (!predirs.Contains(dir)) newdirs.Add(dir);
            }
            return newdirs;
        }

        private static void MoveDir(string source, string target)
        {
            foreach (var file in Directory.GetFiles(source))
            {
                File.Move(file, $"{target}\\{new FileInfo(file).Name}", true);
            }
            foreach (var dir in Directory.GetDirectories(source))
            {
                string newdir = $"{target}\\{new DirectoryInfo(dir).Name}";
                Directory.CreateDirectory(newdir);
                MoveDir(dir, newdir);
            }
            Directory.Delete(source);
        }
        #endregion
    }
}
