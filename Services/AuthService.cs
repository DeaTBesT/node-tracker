using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NodeTracker.Models;
using Microsoft.Data.Sqlite;

namespace NodeTracker.Services
{
    public class AuthService
    {
        private readonly string _dbPath;

        public AuthService(string dbPath)
        {
            _dbPath = dbPath;
            using var ctx = new DatabaseContext(_dbPath);
            ctx.Database.EnsureCreated();

            // Ensure Users table exists (for old DBs created before users were added)
            using (var conn = new SqliteConnection($"Data Source={_dbPath}"))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE,
                        PasswordHash TEXT NOT NULL
                    );";
                    cmd.ExecuteNonQuery();
                }

                // Ensure Notes table has UserId column
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA table_info('Notes');";
                    using var reader = cmd.ExecuteReader();
                    bool hasUserId = false;
                    while (reader.Read())
                    {
                        var colName = reader.GetString(1);
                        if (string.Equals(colName, "UserId", StringComparison.OrdinalIgnoreCase))
                        {
                            hasUserId = true;
                            break;
                        }
                    }

                    if (!hasUserId)
                    {
                        using var alter = conn.CreateCommand();
                        alter.CommandText = "ALTER TABLE Notes ADD COLUMN UserId INTEGER DEFAULT 0;";
                        alter.ExecuteNonQuery();
                    }
                }
            }
        }

        public async Task<User?> RegisterAsync(string username, string password)
        {
            using var ctx = new DatabaseContext(_dbPath);
            if (await ctx.Users.AnyAsync(u => u.Username == username))
                return null;

            var user = new User
            {
                Username = username,
                PasswordHash = Hash(password)
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            return user;
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            using var ctx = new DatabaseContext(_dbPath);
            var hash = Hash(password);
            return await ctx.Users.FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == hash);
        }

        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
