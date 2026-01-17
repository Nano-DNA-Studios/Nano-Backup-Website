using Npgsql;
//using SevenZip;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;

namespace NanoBackupWebsite
{
    public class SQLClient
    {
        private static string BASEPATH = @"/Class Backups";

        private static string ConnectionString = Environment.GetEnvironmentVariable("ConnectionString");

        public SQLClient()
        {
        }

        public static void Initialize()
        {
            CleanDatabase();
            //ProcessBackup(BASEPATH, null, null, null);
            UpdateDirectorySizes();
        }

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
                        string name = (string)reader["name"];
                        bool isFile = (bool)reader["is_file"];
                        string path = (string)reader["path"];
                        long size = (long)reader["size_bytes"];
                        int id = (int)reader["id"];
                        int pID = (int)reader["parent_id"];

                        return new BackupFile(name, isFile, path, size, id, pID, 0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Cleans the Database by removing all Data from the NanoBackupDatabase Table
        /// </summary>
        private static void CleanDatabase()
        {
            string SQLCommand = "TRUNCATE TABLE nanobackupdatabase RESTART IDENTITY CASCADE;";

            using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand(SQLCommand, conn))
                    cmd.ExecuteNonQuery();
            }

            Console.WriteLine("Database Cleaned.");
        }

        /// <summary>
        /// Replaces the True path of files with the Virtual Path to Display on the Website and use in the Docker Container
        /// </summary>
        /// <param name="path">True Path to the File on the Device</param>
        /// <returns>The Path to Display on the Website and to use to Navigate within the Docker Container</returns>
        private static string GetVirtualPath(string path)
        {
            return path.Replace(BASEPATH, "/Class Backups").Replace("\\", "/");
        }

        private static void ProcessBackup(string path, int? parentID, int? id7z, SevenZipArchiveEntry? info)
        {
            //Define the Metadata for the File
            string virtualPath = GetVirtualPath(path);
            string name = Path.GetFileName(path);
            bool isFile = true;
            long size = 0;

            if (info != null)
            {
                SevenZipArchiveEntry realInfo = (SevenZipArchiveEntry)info;
                isFile = !realInfo.IsDirectory;
                size = (long)realInfo.Size;
            }
            else
            {
                isFile = File.Exists(path);

                if (isFile)
                    size = new FileInfo(path).Length;
            }

            if (name.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
            {
                Extract7Z(name, path, parentID ?? 1);
                return;
            }

            int currentID = WriteEntryToSQL(name, isFile, false, size, GetVirtualPath(path), parentID, id7z);

            if (!isFile && info == null)
            {
                string[] children = Directory.GetFileSystemEntries(path);

                foreach (string childPath in children)
                    ProcessBackup(childPath, currentID, id7z, null);
            }
        }

        private static void Extract7Z(string name, string path, int parentID)
        {
            Console.WriteLine($"Extracting Metadata from 7z File {name}");

            SevenZipArchive extractor = SevenZipArchive.Open(path);

            name = name.Replace(".7z", "");
            path = path.Replace(".7z", "");

            Dictionary<string, int> directoryCache = new Dictionary<string, int>();

            int archiveRootID = WriteEntryToSQL(name, false, true, extractor.TotalUncompressSize, GetVirtualPath(path), parentID, null);

            directoryCache[path] = archiveRootID;

            foreach (SevenZipArchiveEntry entry in extractor.Entries.Skip(1))
            {
                if (entry.Key == null) continue;

                string entryName = Path.GetFileName(entry.Key);
                string entryPath = Path.Combine(path.Replace(name, ""), entry.Key);
                string parentPath = Path.GetDirectoryName(entryPath) ?? "";

                int parentDirectoryID = directoryCache.ContainsKey(parentPath) ? directoryCache[parentPath] : archiveRootID;
                int newId = WriteEntryToSQL(entryName, !entry.IsDirectory, false, (long)entry.Size, GetVirtualPath(entryPath), parentDirectoryID, archiveRootID);

                if (entry.IsDirectory)
                    directoryCache[entryPath] = newId;
            }
        }

        private static int WriteEntryToSQL(string name, bool isFile, bool is7z, long size, string vPath, int? parentID, int? id7z)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                string sql = @"INSERT INTO nanobackupdatabase (name, is_file, is_7z, size_bytes, path, parent_id, parent_7z) 
                               VALUES (@n, @if, @i7, @s, @p, @pid, @i7id) RETURNING id;";

                var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("n", name);
                cmd.Parameters.AddWithValue("if", isFile);
                cmd.Parameters.AddWithValue("i7", is7z);
                cmd.Parameters.AddWithValue("s", size);
                cmd.Parameters.AddWithValue("p", vPath);
                cmd.Parameters.AddWithValue("pid", (object)parentID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("i7id", (object)id7z ?? DBNull.Value);

                conn.Open();
                int newId = (int)cmd.ExecuteScalar();
                Console.WriteLine($"Inserted: {name} (ID: {newId})");
                return newId;
            }
        }

        private static void UpdateDirectorySizes()
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();

                UpdateSize(1, conn);
            }
        }

        private static long UpdateSize(int id, NpgsqlConnection connection)
        {
            long size = 0;

            string queryFiles = @"SELECT * FROM nanobackupdatabase WHERE parent_id = @pid;";

            NpgsqlCommand queryCommand = new NpgsqlCommand(queryFiles, connection);

            queryCommand.Parameters.AddWithValue("pid", id);

            List<(int ChildID, bool IsFile, long Size)> children = new List<(int ChildID, bool IsFile, long Size)>();

            using (NpgsqlDataReader reader = queryCommand.ExecuteReader())
            {
                while (reader.Read())
                    children.Add(((int)reader["id"], (bool)reader["is_file"], (long)reader["size_bytes"]));
            }

            foreach ((int ChildID, bool IsFile, long Size) child in children)
            {
                if (child.IsFile)
                    size += (long)child.Size;
                else
                    size += UpdateSize(child.ChildID, connection);
            }

            string updateDirectorySize = @"UPDATE nanobackupdatabase SET size_bytes = @s WHERE id = @id;";

            NpgsqlCommand updateCommand = new NpgsqlCommand(updateDirectorySize, connection);

            updateCommand.Parameters.AddWithValue("id", id);
            updateCommand.Parameters.AddWithValue("s", size);

            int rowAffected = updateCommand.ExecuteNonQuery();

            if (rowAffected == -1)
                throw new Exception("Something wen't wrong updating");

            Console.WriteLine($"Updated Size of {id} : {size}");

            return size;
        }
    }
}