using System;
using System.Data.SQLite;
using System.IO;

namespace FileEncryptor
{
    public static class DatabaseHelper
    {
        private const string DbFile = "file_encryptor.db";

        public static string ConnectionString => $"Data Source={DbFile};Version=3;";

        public static void InitializeDatabase()
        {
            if (!File.Exists(DbFile))
                SQLiteConnection.CreateFile(DbFile);

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Username TEXT PRIMARY KEY,
                        Salt TEXT NOT NULL,
                        PasswordHash TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS FileRecords (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL,
                        EncryptDate TEXT,
                        OriginalName TEXT,
                        EncryptedPath TEXT,
                        HashOfFile TEXT,
                        HashOfKey TEXT,
                        IsOwnedKey INTEGER
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            EnsurePublicKeyColumnExists();
        }

        public static void EnsurePublicKeyColumnExists()
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("PRAGMA table_info(Users);", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    bool hasColumn = false;
                    while (reader.Read())
                    {
                        if (reader["name"].ToString().Equals("PublicKey", StringComparison.OrdinalIgnoreCase))
                        {
                            hasColumn = true;
                            break;
                        }
                    }

                    if (!hasColumn)
                    {
                        using (var alter = new SQLiteCommand("ALTER TABLE Users ADD COLUMN PublicKey TEXT;", conn))
                        {
                            alter.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}
