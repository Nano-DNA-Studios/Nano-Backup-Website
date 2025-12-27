using System.Text.Json.Nodes;

namespace NanoBackupWebsite
{
    public class FileNavigatorService
    {
        public List<BackupFile> Files;

        public FileNavigatorService()
        {
            Files = new List<BackupFile>();

            string jsonString = File.ReadAllText("ClassBackups.json");

            JsonNode rootNode = JsonNode.Parse(jsonString);

            // 2. Treat the root as an array
            JsonArray topLevelItems = rootNode.AsArray();

            Console.WriteLine("--- Top Level Folders ---");

            foreach (var item in topLevelItems)
            {
                // Extract property values manually
                string name = item["Name"]?.ToString();
                bool isFile = (bool)item["IsFile"];
                string size = item["Size"]?.ToString();

                if (!isFile)
                {
                    Console.WriteLine($"Folder: {name}");

                    // 3. Drill down into "1B" specifically
                    if (name == "1B")
                    {
                        Console.WriteLine("  Contents of 1B:");
                        JsonArray contents = item["Content"]?.AsArray();

                        foreach (var subItem in contents)
                        {
                            Console.WriteLine($"    - {subItem["Name"]} ({subItem["Size"]})");
                        }
                    }
                }

                Files.Add(new BackupFile(name, isFile, size));
            }
        }
    }
}
