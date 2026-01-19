namespace NanoBackupWebsite
{
    public class BackupFile
    {
        public int ParentID { get; }

        public int ID { get;  }

        public string Name { get;}

        public bool IsFile { get; }

        public bool Is7z { get; }

        public long Size { get; }

        public string Path { get; }

        public int Parent7Z { get; }

        public BackupFile (string name, bool isFile, string path, long size, int id, int parentID, int parent7Z, bool is7z)
        {
            Name = name;
            IsFile = isFile;
            Path = path;
            Size = size;
            ID = id;
            ParentID = parentID;
            Parent7Z = parent7Z;
            Is7z = is7z;
        }

        public string GetSize()
        {
            return GetSizeRec(0, (double)Size);
        }

        private string GetSizeRec (int depth = 0, double remaining = 0)
        {
            string[] sizeSymbol = ["B", "KB", "MB", "GB", "TB"];

            if (remaining < 1024)
                return $"{remaining:0.##} {sizeSymbol[depth]}";

            return GetSizeRec(depth + 1, remaining / 1024.0);
        }
    }
}
