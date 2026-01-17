
namespace NanoBackupWebsite
{
    public class FileNavigatorService
    {
        public BackupFile[] CurrentFiles { get; private set; }

        private Stack<int> Directories;

        private string RootPath;

        public string FullPath;

        private SQLClient Client;

        public FileNavigatorService()
        {
            Client = new SQLClient();
            Directories = new Stack<int>();

            RootPath = "./Class Backups";
            FullPath = RootPath;

            CurrentFiles = Client.GetFiles(1);
            Directories.Push(1);
        }

        public void NavigateFolder(BackupFile folder)
        {
            FullPath = folder.Path;
            Directories.Push(folder.ID);
            CurrentFiles = Client.GetFiles(folder.ID);
        }

        public void GoHome()
        {
            Directories.Clear();
            CurrentFiles = Client.GetFiles(1);
            FullPath = RootPath;
        }

        public void GoBack()
        {
            if (Directories.Count <= 1)
            {
                GoHome();
                return;
            }

            Directories.Pop();

            CurrentFiles = Client.GetFiles(Directories.Peek());

            int parent2 = Directories.Pop();

            if (Directories.Count == 0)
                FullPath = RootPath;
            else
                FullPath = Client.GetFiles(Directories.Peek())[0].Path;

            Directories.Push(parent2);
        }
    }
}
