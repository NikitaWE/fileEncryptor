using System;
using System.Data.SQLite;
using System.IO;

namespace FileEncryptor
{
    public static class Database
    {
        private const string DbFile = "Encryptor.db";

        public static void Initialize()
        {
            if (!File.Exists(DbFile))
            {
                SQLiteConnection.CreateFile(DbFile);
                using var conn = new SQLiteConnection($"Data Source={DbFile};");
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        Salt TEXT NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        PublicKey TEXT NOT NULL,
                        PrivateKey TEXT NOT NULL
                    );

                    CREATE TABLE FileLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        FileName TEXT NOT NULL,
                        FileHash TEXT NOT NULL,
                        Action TEXT NOT NULL,
                        Timestamp TEXT NOT NULL,
                        FOREIGN KEY(UserId) REFERENCES Users(Id)
                    );";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
