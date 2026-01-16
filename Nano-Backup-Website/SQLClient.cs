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

        public string[] GetFiles(int parentID)
        {
            string SQLQuery = "SELECT * FROM nanobackupdatabase WHERE parent_id = @id";

            List<string> Files = new List<string>();

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
                            Console.WriteLine(reader.ToString());
                        }
                           // Files.Add((string)reader["name"]);
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