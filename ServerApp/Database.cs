using System;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;

namespace ServerApp
{
    public static class Database
    {
        private static string _connectionString = "Data Source=CyberCafe.db;Version=3;";

        public static void Initialize()
        {
            if (!File.Exists("CyberCafe.db"))
            {
                SQLiteConnection.CreateFile("CyberCafe.db");
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var schema = File.ReadAllText("DatabaseSchema.sql");
                    var command = new SQLiteCommand(schema, connection);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
        }

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}