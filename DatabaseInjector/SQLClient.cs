using Npgsql;
using SharpCompress.Archives.SevenZip;

namespace DatabaseInjector
{
    internal class SQLClient
    {
        public string BASEPATH { get; }

        private string VIRTUALBASEPATH { get; }

        private string ConnectionString { get; }

        public SQLClient(string basePath)
        {
            ConnectionString = "";
            ConnectionString += "Host=" + (Environment.GetEnvironmentVariable("Host") ?? "") + ";";
            ConnectionString += "Port=" +(Environment.GetEnvironmentVariable("Port") ?? "") + ";";
            ConnectionString += "Username=" +(Environment.GetEnvironmentVariable("Username") ?? "") + ";";
            ConnectionString += "Password=" +(Environment.GetEnvironmentVariable("Password") ?? "") + ";";
            ConnectionString += "Database=" + (Environment.GetEnvironmentVariable("Database") ?? "") + ";";

            VIRTUALBASEPATH = @"/Class Backups";
            BASEPATH = basePath;
        }

        /// <summary>
        /// Cleans the Database by removing all Data from the NanoBackupDatabase Table
        /// </summary>
        public void CleanDatabase()
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
        private string GetVirtualPath(string path)
        {
            return path.Replace(BASEPATH, VIRTUALBASEPATH).Replace("\\", "/");
        }

        public void ProcessBackup(string path, int? parentID, int? id7z, SevenZipArchiveEntry? info)
        {
            string virtualPath = GetVirtualPath(path);
            string name = Path.GetFileName(path);
            bool isFile = true;
            long size = 0;

            if (info != null)
            {
                SevenZipArchiveEntry realInfo = info;
                isFile = !realInfo.IsDirectory;
                size = realInfo.Size;
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

        private void Extract7Z(string name, string path, int parentID)
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

                string entryName = Path.GetFileName(entry.Key.TrimEnd('\\', '/'));
                string entryPath = Path.Combine(path.Replace(name, ""), entry.Key).Replace("\\", "/").TrimEnd('/');
                string parentPath = Path.GetDirectoryName(entryPath)?.Replace("\\", "/") ?? path;

                int parentDirectoryID = directoryCache.ContainsKey(parentPath) ? directoryCache[parentPath] : archiveRootID;
                int newId = WriteEntryToSQL(entryName, !entry.IsDirectory, false, entry.Size, GetVirtualPath(entryPath), parentDirectoryID, archiveRootID);

                if (entry.IsDirectory)
                    directoryCache[entryPath] = newId;
            }
        }

        private int WriteEntryToSQL(string name, bool isFile, bool is7z, long size, string vPath, int? parentID, int? id7z)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                string sql = @"INSERT INTO nanobackupdatabase (name, is_file, is_7z, size_bytes, path, parent_id, parent_7z) 
                               VALUES (@n, @if, @i7, @s, @p, @pid, @i7id) RETURNING id;";

                object pid;
                object i7;

                if (parentID == null)
                    pid = DBNull.Value;
                else
                    pid = parentID;

                if (id7z == null)
                    i7 = DBNull.Value;
                else
                    i7 = id7z;

                NpgsqlCommand cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("n", name);
                cmd.Parameters.AddWithValue("if", isFile);
                cmd.Parameters.AddWithValue("i7", is7z);
                cmd.Parameters.AddWithValue("s", size);
                cmd.Parameters.AddWithValue("p", vPath);
                cmd.Parameters.AddWithValue("pid", pid);
                cmd.Parameters.AddWithValue("i7id", i7);

                conn.Open();

                object? idObj = cmd.ExecuteScalar();

                if (idObj == null)
                    return 0;

                int newId = (int)idObj;
                Console.WriteLine($"Inserted: {name} (ID: {newId})");
                return newId;
            }
        }

        public void UpdateDirectorySizes()
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();

                UpdateSize(1, conn);
            }
        }

        private long UpdateSize(int id, NpgsqlConnection connection)
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
                    size += child.Size;
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