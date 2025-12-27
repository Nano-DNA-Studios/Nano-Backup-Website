using System.Text.Json;

namespace NanoBackupWebsite
{
    public class FileNavigatorService
    {
        public List<BackupFile> Root;

        public List<BackupFile> CurrentFiles;

        private Stack<string> Directories;

        private string RootPath;

        public string FullPath;


        public FileNavigatorService()
        {
            Root = new List<BackupFile>();
            Directories = new Stack<string>();
            RootPath = "Class Backups";

            FullPath = GetFullPath();


            ParseJSON();

            CurrentFiles = Root;
        }

        private void ParseJSON()
        {
            string jsonString = File.ReadAllText("ClassBackups.json");

            using JsonDocument doc = JsonDocument.Parse(jsonString);

            JsonElement root = doc.RootElement;

            Root = ParseJSONRecursive(root, null);
        }

        private List<BackupFile> ParseJSONRecursive(JsonElement element, BackupFile? parent)
        {
            Console.WriteLine($"Current Element : {element}, Raw Text : {element.GetRawText()}");

            if (element.GetRawText() == "null")
                return new List<BackupFile>();

            if (element.GetArrayLength() == 0)
                return new List<BackupFile>();

            List<BackupFile> files = new List<BackupFile>();

            foreach (JsonElement item in element.EnumerateArray())
            {
                string? name = item.GetProperty("Name").GetString();
                bool isFile = (bool)item.GetProperty("IsFile").GetBoolean();
                string? size = item.GetProperty("Size").GetString();
                JsonElement children = item.GetProperty("Content");

                if (name == null)
                    name = string.Empty;

                if (size == null)
                    size = string.Empty;

                files.Add(new BackupFile(name, isFile, size, parent, ParseJSONRecursive(children, parent)));
            }

            return files;
        }

        private string GetFullPath()
        {
            string fullPath = RootPath;

            string[] paths = Directories.ToArray();

            for (int i = paths.Length - 1; i >= 0; i--)
                fullPath = Path.Combine(fullPath, paths[i]);

            return fullPath;
        }

        public void NavigateFolder(BackupFile folder)
        {
            Directories.Push(folder.Name);
            CurrentFiles = folder.Children;
            FullPath = GetFullPath();
        }

        public void GoHome()
        {
            Directories.Clear();
            CurrentFiles = Root;
            FullPath = GetFullPath();
        }

        public void GoBack()
        {
            Directories.Pop();
            CurrentFiles = Root;

            if (CurrentFiles[0].Parent != null && CurrentFiles[0].Parent?.Parent != null)
                CurrentFiles = CurrentFiles[0].Parent?.Parent?.Children;
            else
                CurrentFiles = Root;

            FullPath = GetFullPath();
        }
    }
}
