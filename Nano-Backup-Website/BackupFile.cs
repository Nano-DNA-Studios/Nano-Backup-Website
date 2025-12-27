namespace NanoBackupWebsite
{
    public class BackupFile
    {
        public string Name { get;}

        public bool IsFile { get; }

        public string Size { get; }


        public BackupFile (string name, bool isFile, string size)
        {
            Name = name;
            IsFile = isFile;
            Size = size;
        }
    }
}
