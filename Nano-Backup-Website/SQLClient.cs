using Npgsql;
using SharpCompress.Archives.SevenZip;

namespace NanoBackupWebsite
{
    public class SQLClient : IDisposable
    {
        private static string ConnectionString = Environment.GetEnvironmentVariable("ConnectionString") ?? "";

        public SevenZipArchive? Archive;

        public void Dispose()
        {
            if (Archive != null)
                Archive.Dispose();
            
            GC.Collect(2, GCCollectionMode.Aggressive, true);
        }

        ~SQLClient()
        {
            if (Archive != null)
                Archive.Dispose();

            GC.Collect(2, GCCollectionMode.Optimized, true);
        }

        public Stream? GetFileStream(int id)
        {
            BackupFile? file = GetFile(id);

            if (file == null)
                return null;

            if (file.Is7z)
                return new FileStream(file.Path + ".7z", FileMode.Open, FileAccess.Read, FileShare.Read);

            if (file.Parent7Z == -1)
                return new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read);

            BackupFile? parentFile = GetFile(file.Parent7Z);

            if (parentFile == null)
                return null;

            Archive = SevenZipArchive.Open(parentFile.Path + ".7z");
            SevenZipArchiveEntry? entry = Archive.Entries.FirstOrDefault(e =>
            {
                if (e.Key == null)
                    return false;

                return !e.IsDirectory && e.Key.Contains(file.Name);
            });

            if (entry == null)
                throw new FileNotFoundException($"Could not find {file.Name} inside the archive.");

            return entry.OpenEntryStream();
        }

        public BackupFile[] GetFiles(int parentID)
        {
            string SQLQuery = "SELECT * FROM nanobackupdatabase WHERE parent_id = @id";

            List<BackupFile> Files = new List<BackupFile>();

            NpgsqlConnection connection = new NpgsqlConnection(ConnectionString);
            NpgsqlCommand command = new NpgsqlCommand(SQLQuery, connection);

            command.Parameters.AddWithValue("@id", parentID);

            try
            {
                connection.Open();

                NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string name = (string)reader["name"];
                    bool isFile = (bool)reader["is_file"];
                    bool is7z = (bool)reader["is_7z"];
                    string path = (string)reader["path"];
                    long size = (long)reader["size_bytes"];
                    int id = (int)reader["id"];
                    int pID = (int)reader["parent_id"];
                    int id7Z = -1;

                    if (!reader.IsDBNull(reader.GetOrdinal("parent_7z")))
                        id7Z = (int)reader["parent_7z"];

                    BackupFile file = new BackupFile(name, isFile, path, size, id, pID, id7Z, is7z);

                    Files.Add(file);
                }

                reader.Close();
                reader.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            command.Dispose();
            connection.Close();
            connection.Dispose();

            return Files.ToArray();
        }

        public BackupFile? GetFile(int id)
        {
            string SQLQuery = "SELECT * FROM nanobackupdatabase WHERE id = @id";

            NpgsqlConnection connection = new NpgsqlConnection(ConnectionString);
            NpgsqlCommand command = new NpgsqlCommand(SQLQuery, connection);

            command.Parameters.AddWithValue("@id", id);

            try
            {
                connection.Open();

                NpgsqlDataReader reader = command.ExecuteReader();

                reader.Read();

                string name = (string)reader["name"];
                bool isFile = (bool)reader["is_file"];
                bool is7z = (bool)reader["is_7z"];
                string path = (string)reader["path"];
                long size = (long)reader["size_bytes"];
                int fileID7Z = -1;

                int fid;
                int pID;

                if (reader["id"] == DBNull.Value)
                    fid = 0;
                else
                    fid = (int)reader["id"];

                if (reader["parent_id"] == DBNull.Value)
                    pID = 0;
                else
                    pID = (int)reader["parent_id"];

                if (!reader.IsDBNull(reader.GetOrdinal("parent_7z")))
                    fileID7Z = (int)reader["parent_7z"];

                reader.Close();
                reader.DisposeAsync();
                command.Dispose();
                connection.Close();
                connection.Dispose();

                return new BackupFile(name, isFile, path, size, fid, pID, fileID7Z, is7z);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return null;
        }

        public int GetNumberOfFilesOrFolders(bool file)
        {
            string SQLQuery = "SELECT COUNT(*) FROM nanobackupdatabase WHERE is_file = @file";

            NpgsqlConnection connection = new NpgsqlConnection(ConnectionString);
            NpgsqlCommand command = new NpgsqlCommand(SQLQuery, connection);

            command.Parameters.AddWithValue("@file", file);

            try
            {
                connection.Open();

                object? result = command.ExecuteScalar();

                command.Dispose();
                connection.Close();
                connection.Dispose();

                if (result != null)
                    return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return 0;
        }
    }
}