namespace NanoBackupWebsite
{
    public class BackupFile
    {
        public BackupFile? Parent { get; }

        public string Name { get;}

        public bool IsFile { get; }

        public string Size { get; }

        public List<BackupFile> Children { get; }

        public BackupFile (string name, bool isFile, string size, BackupFile? parent, List<BackupFile> children)
        {
            Name = name;
            IsFile = isFile;
            Size = size;
            Parent = parent;
            Children = children;
        }
    }
}
