using System;
using System.Data;
using MySql.Data.MySqlClient;
using Dapper;

namespace MyPlugin
{
    public class PluginClass
    {
        public void Run(MySqlConnection cnn, string cmd)
        {
            if (cnn.State != ConnectionState.Open)
            {
                cnn.Open();
            }

            try
            {
                var parts = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (cmd.StartsWith("insert", StringComparison.OrdinalIgnoreCase))
                {
                    if (parts.Length < 4)
                    {
                        Console.WriteLine("Hatalı insert komutu! Kullanım: insert name last_name email ");
                        return;
                    }

                    string name = parts[1];
                    string lastName = parts[2];
                    string email = parts[3];

                    string sql = "INSERT INTO users (name, last_name, email) VALUES (@Name, @LastName, @Email)";
                    cnn.Execute(sql, new { Name = name, LastName = lastName, Email = email });
                    Console.WriteLine("Kullanıcı eklendi!");
                }
                else if (cmd.StartsWith("update", StringComparison.OrdinalIgnoreCase))
                {
                    if (parts.Length < 5)
                    {
                        Console.WriteLine("Hatalı update komutu! Kullanım: update id name last_name email");
                        return;
                    }

                    int id = int.Parse(parts[1]);
                    string name = parts[2];
                    string lastName = parts[3];
                    string email = parts[4];

                    string sql = "UPDATE users SET name = @Name, last_name = @LastName, email = @Email WHERE id = @Id";
                    int rowsAffected = cnn.Execute(sql, new { Id = id, Name = name, LastName = lastName, Email = email });
                    if (rowsAffected > 0)
                    {
                        Console.WriteLine("Kullanıcı güncellendi!");
                    }
                    else
                    {
                        Console.WriteLine("Kullanıcı bulunamadı!");
                    }
                }
                else if (cmd.StartsWith("delete", StringComparison.OrdinalIgnoreCase))
                {
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Hatalı delete komutu! Kullanım: delete id");
                        return;
                    }

                    int id = int.Parse(parts[1]);

                    string sql = "DELETE FROM users WHERE id = @Id";
                    int rowsAffected = cnn.Execute(sql, new { Id = id });
                    if (rowsAffected > 0)
                    {
                        Console.WriteLine("Kullanıcı silindi!");
                    }
                    else
                    {
                        Console.WriteLine("Kullanıcı bulunamadı!");
                    }
                }
                else if (cmd.Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    string sql = "SELECT * FROM users";
                    var users = cnn.Query<dynamic>(sql);

                    Console.WriteLine("=== Kullanıcı Listesi ===");
                    foreach (var user in users)
                    {
                        Console.WriteLine($"ID: {user.id}, Name: {user.name}, Last Name: {user.last_name}, Email: {user.email}, CreatedAt: {user.created_at}");
                    }
                    Console.WriteLine("=========================");
                }
                else if (cmd.StartsWith("get", StringComparison.OrdinalIgnoreCase))
                {
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Hatalı get komutu! Kullanım: get id");
                        return;
                    }
                
                    int id = int.Parse(parts[1]);
                
                    string sql = "SELECT * FROM users WHERE id = @Id";
                    var user = cnn.QueryFirstOrDefault<dynamic>(sql, new { Id = id });
                
                    if (user != null)
                    {
                        Console.WriteLine($"ID: {user.id}, Name: {user.name}, Last Name: {user.last_name}, Email: {user.email}, CreatedAt: {user.created_at}");
                    }
                    else
                    {
                        Console.WriteLine("Kullanıcı bulunamadı!");
                    }
                }
                else
                {
                    Console.WriteLine("Bilinmeyen komut!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }
    }
}
