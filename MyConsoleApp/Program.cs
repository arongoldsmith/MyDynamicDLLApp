using MySql.Data.MySqlClient;
using MyConsoleApp.Settings;
using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MyConsoleApp.Context;
using System.Reflection;

namespace MyConsoleApp
{
    internal static class Program
    {
        private static string dllPath;
        private static FileSystemWatcher? watcher;
        private static CancellationTokenSource cts;
        private static readonly object reloadLock = new object();
        private static PluginLoadContext? loadContext;
        private static object? pluginInstance;
        private static MethodInfo? runMethod;
        private static MySqlConnection? connection;
        private static bool isReloading = false;

        private static void Main()
        {
            var projectRoot = FindProjectRoot();
            if (string.IsNullOrEmpty(projectRoot))
            {
                Console.WriteLine("Hata: Proje kök dizini bulunamadı!");
                return;
            }

            string pluginsDirectory = Path.Combine(projectRoot, "Plugins");
            dllPath = Path.Combine(pluginsDirectory, "MyPlugin.dll");

            StartWatcher(pluginsDirectory);
            LoadPlugin();

            // Ana thread'i bloke et
            new ManualResetEvent(false).WaitOne();
        }

        private static void StartWatcher(string path)
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            watcher = new FileSystemWatcher
            {
                Path = path,
                Filter = "MyPlugin.dll",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watcher.Created += OnDllChanged;
            watcher.Renamed += OnDllChanged;

            Console.WriteLine($"📢 FileSystemWatcher başlatıldı, {path} izleniyor...");
        }

        private static void OnDllChanged(object sender, FileSystemEventArgs e)
        {
            if (isReloading) return;
            lock (reloadLock)
            {
                isReloading = true;
                watcher.EnableRaisingEvents = false;
                Console.WriteLine("\n📢 DLL değişikliği algılandı! Yeniden yükleniyor...");
                Reload();
                watcher.EnableRaisingEvents = true;
                isReloading = false;
            }
        }

        private static void Reload()
        {
            Console.WriteLine("🔄 DLL yeniden yükleniyor...");

            cts?.Cancel();
            LoadPlugin();
        }

        private static void LoadPlugin()
        {
            try
            {
                if (!File.Exists(dllPath))
                {
                    Console.WriteLine($"Hata: DLL bulunamadı! Yol: {dllPath}");
                    return;
                }

                // Shadow Copying
                string uniqueDllPath = GetUniqueDllPath();
                File.Copy(dllPath, uniqueDllPath, true);
                Console.WriteLine($"✅ Yeni DLL kopyalandı: {uniqueDllPath}");

                //  Eski yüklenen DLL'leri ve context'i temizle
                loadContext?.Dispose();
                pluginInstance = null;
                runMethod = null;

                // Yeni DLL
                loadContext = new PluginLoadContext(uniqueDllPath);
                var assembly = loadContext.LoadFromAssemblyPath(uniqueDllPath);
                var pluginType = assembly.GetType("MyPlugin.PluginClass");

                if (pluginType == null)
                {
                    Console.WriteLine("Hata: Plugin sınıfı bulunamadı!");
                    return;
                }

                pluginInstance = Activator.CreateInstance(pluginType);
                runMethod = pluginType.GetMethod("Run");

                if (runMethod == null)
                {
                    Console.WriteLine("Hata: Run metodu bulunamadı!");
                    return;
                }
                
                if (connection == null || connection.State == ConnectionState.Closed)
                {
                    connection = new MySqlConnection(AppSettings.LoadSettings().ConnectionString);
                    connection.Open();
                }

                Console.WriteLine("🔌 Bağlantı başarılı. Komut girin ('exit' ile çıkış):");

                cts = new CancellationTokenSource();
                Task.Run(() => CommandLoop(cts.Token));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }

        private static string GetUniqueDllPath()
        {
            string directory = Path.GetDirectoryName(dllPath) ?? string.Empty;
            string fileName = $"MyPlugin_{DateTime.UtcNow.Ticks}.dll";
            return Path.Combine(directory, fileName);
        }

        private static async Task CommandLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var input = await Task.Run(() => Console.ReadLine(), token);

                    if (string.IsNullOrEmpty(input)) continue;
                    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) Environment.Exit(0);
                    
                    if (pluginInstance != null && runMethod != null)
                    {
                        try
                        {
                            if (connection.State == ConnectionState.Closed)
                                connection.Open();

                            runMethod.Invoke(pluginInstance, new object[] { connection, input });
                        }
                        catch (TargetInvocationException tie)
                        {
                            Console.WriteLine($"Hata: {tie.InnerException?.Message ?? tie.Message}");
                            Console.WriteLine($"Detaylar: {tie.InnerException?.StackTrace ?? tie.StackTrace}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Hata: Plugin yüklü değil veya metot eksik!");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Yeniden yükleme yapıldığı için iptal edilebilir.
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata: {ex.Message}");
                }
            }
        }

        private static string FindProjectRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Plugins")))
            {
                directory = directory.Parent;
            }
            return directory?.FullName ?? string.Empty;
        }
    }
}
