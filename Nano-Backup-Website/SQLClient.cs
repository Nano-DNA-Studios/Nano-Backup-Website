using Npgsql;

namespace NanoBackupWebsite
{
    public class SQLClient
    {
        private static string Host = "Host=localhost;";

        private static string Port = "Port=5433;";

        private static string Database = "Database=nanobackupwebsite;";

        private static string Username = "Username=postgres;";

        private static string Password = $"Password={Environment.GetEnvironmentVariable("POSTGRESPASSWORD") ?? ""};";

        private static string ConnectionString = "";

        public SQLClient ()
        {
            ConnectionString += Host;
            ConnectionString += Port;
            ConnectionString += Database;
            ConnectionString += Username;
            ConnectionString += Password;
        }

        public BackupFile[] GetFiles(int parentID)
        {
            string SQLQuery = "SELECT * FROM nanobackupdatabase WHERE parent_id = @id";

            List<BackupFile> Files = new List<BackupFile>();

            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                NpgsqlCommand command = new NpgsqlCommand(SQLQuery, connection);

                command.Parameters.AddWithValue("@id", parentID);

                try
                {
                    connection.Open();

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = (string)reader["name"];
                            bool isFile = (bool)reader["is_file"];
                            string path = (string)reader["path"];
                            long size = (long)reader["size_bytes"];
                            int id = (int)reader["id"];
                            int pID = (int)reader["parent_id"];

                            BackupFile file = new BackupFile(name, isFile, path, size, id, pID);

                            Files.Add(file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            return Files.ToArray();
        }
    }
}