using Npgsql;
using SharpCompress.Archives.SevenZip;

namespace NanoBackupWebsite
{
    public class SQLClient
    {
        private static string ConnectionString = Environment.GetEnvironmentVariable("ConnectionString") ?? "";

        public Stream GetFileStream(int id)
        {
            string getFile = "SELECT * FROM nanobackupdatabase WHERE id = @id;";

            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                NpgsqlCommand command = new NpgsqlCommand(getFile, connection);
                command.Parameters.AddWithValue("id", id);
                connection.Open();

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    if ((bool)reader["is_7z"])
                        return new FileStream((string)reader["path"] + ".7z", FileMode.Open, FileAccess.Read, FileShare.Read);

                    int parent7zId = (int)reader["parent_7z"];
                    string targetFileName = (string)reader["name"];

                    Console.WriteLine($"Got Parent7Z ID : {parent7zId}");

                    Stream archiveStream = GetFileStream(parent7zId);

                    using (SevenZipArchive archive = SevenZipArchive.Open(archiveStream))
                    {
                        SevenZipArchiveEntry? entry = archive.Entries.FirstOrDefault(e => !e.IsDirectory && e.Key.Contains(targetFileName));

                        if (entry == null)
                            throw new FileNotFoundException($"Could not find {targetFileName} inside the archive.");

                        Console.WriteLine($"Extracting File : {entry.Key}");

                        MemoryStream memoryStream = new MemoryStream((int)entry.Size);
                        using (Stream entryStream = entry.OpenEntryStream())
                            entryStream.CopyTo(memoryStream);
                        
                        memoryStream.Position = 0;

                        archiveStream.Dispose();

                        Console.WriteLine("File Extracted Successfully");
                        return memoryStream;
                    }
                }
            }
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
                            int id7Z = -1;

                            if (!reader.IsDBNull(reader.GetOrdinal("parent_7z")))
                                id7Z = (int)reader["parent_7z"];

                            BackupFile file = new BackupFile(name, isFile, path, size, id, pID, id7Z);

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

        public BackupFile Get7ZFile(int id7Z)
        {
            string SQLQuery = "SELECT * FROM nanobackupdatabase WHERE id = @id";

            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                NpgsqlCommand command = new NpgsqlCommand(SQLQuery, connection);

                command.Parameters.AddWithValue("@id", id7Z);

                try
                {
                    connection.Open();

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        reader.Read();

                        string name = (string)reader["name"];
                        bool isFile = (bool)reader["is_file"];
                        string path = (string)reader["path"];
                        long size = (long)reader["size_bytes"];
                        int id = (int)reader["id"];
                        int pID = (int)reader["parent_id"];

                        return new BackupFile(name, isFile, path, size, id, pID, -1);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            return null;
        }
    }
}