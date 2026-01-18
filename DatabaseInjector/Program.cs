namespace DatabaseInjector
{
    internal class Program
    {
        private static string DatabasePath = @"E:\Backups\Important Backup\School\Class Backup SQL Test";

        static void Main(string[] args)
        {
            LoadEnv();

            SQLClient client = new SQLClient(DatabasePath);

            try
            {
                Console.WriteLine("Database Being Cleaned...");
                client.CleanDatabase();

                Console.WriteLine("Writing Files to Database...");
                client.ProcessBackup(DatabasePath, null, null, null);

                Console.WriteLine("Updating Directory Sizes...");
                client.UpdateDirectorySizes();

                Console.WriteLine("Finished Writing to SQL Database");
           } catch (Exception e)
           {
               Console.WriteLine($"Error : {e.ToString()}");
           }
        }

        private static void LoadEnv()
        {
            string envPath = ".env";

            if (!Path.Exists(envPath))
                return;

            foreach (string line in File.ReadLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                string[] parts = line.Split('=');

                if (parts.Length != 2)
                    continue;

                var key = parts[0];
                var value = parts[1];

                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}