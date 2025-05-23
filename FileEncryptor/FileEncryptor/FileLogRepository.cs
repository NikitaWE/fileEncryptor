using System;
using System.Data.SQLite;

namespace FileEncryptor
{
    public static class FileLogRepository
    {
        public static void LogAction(int userId, string fileName, string fileHash, string action)
        {
            using var conn = new SQLiteConnection("Data Source=Encryptor.db;");
            conn.Open();

            var cmd = new SQLiteCommand(@"
                INSERT INTO FileLogs (UserId, FileName, FileHash, Action, Timestamp)
                VALUES (@uid, @fname, @fhash, @act, @ts)", conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@fname", fileName);
            cmd.Parameters.AddWithValue("@fhash", fileHash);
            cmd.Parameters.AddWithValue("@act", action);
            cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("s"));
            cmd.ExecuteNonQuery();
        }
    }
}
