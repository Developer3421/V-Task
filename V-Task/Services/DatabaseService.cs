using System;
using System.IO;
using LiteDB;
using V_Task.Models;

namespace V_Task.Services;

/// <summary>
/// Service for managing LiteDB database operations.
/// Handles database creation, corruption recovery, and settings persistence.
/// Database is stored in AppData folder for Microsoft Store compatibility.
/// </summary>
public class DatabaseService : IDisposable
{
    private static DatabaseService? _instance;
    private static readonly object _lock = new();
    
    public static DatabaseService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new DatabaseService();
                }
            }
            return _instance;
        }
    }

    private LiteDatabase? _database;
    private readonly string _databasePath;
    private readonly string _appDataFolder;
    private const string DatabaseFileName = "vtask_settings.db";
    private const string AppFolderName = "V-Task";
    private const string CollectionName = "settings";

    private DatabaseService()
    {
        _appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName
        );
        
        _databasePath = Path.Combine(_appDataFolder, DatabaseFileName);
        
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            if (!Directory.Exists(_appDataFolder))
            {
                Directory.CreateDirectory(_appDataFolder);
            }

            _database = OpenDatabase();
            
            if (!VerifyDatabaseIntegrity())
            {
                RecreateDatabase();
            }
        }
        catch (Exception)
        {
            RecreateDatabase();
        }
    }

    private LiteDatabase OpenDatabase()
    {
        var connectionString = new ConnectionString
        {
            Filename = _databasePath,
            Connection = ConnectionType.Shared
        };
        
        return new LiteDatabase(connectionString);
    }

    private bool VerifyDatabaseIntegrity()
    {
        try
        {
            if (_database == null) return false;
            
            var collection = _database.GetCollection<AppSettings>(CollectionName);
            _ = collection.Count();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void RecreateDatabase()
    {
        try
        {
            _database?.Dispose();
            _database = null;

            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }

            var journalPath = _databasePath + "-journal";
            if (File.Exists(journalPath))
            {
                File.Delete(journalPath);
            }

            _database = OpenDatabase();
            
            var collection = _database.GetCollection<AppSettings>(CollectionName);
            collection.Insert(new AppSettings());
            collection.EnsureIndex(x => x.Id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to recreate database: {ex.Message}");
        }
    }

    public AppSettings GetSettings()
    {
        try
        {
            if (_database == null)
            {
                InitializeDatabase();
            }

            var collection = _database!.GetCollection<AppSettings>(CollectionName);
            var settings = collection.FindById(1);
            
            if (settings == null)
            {
                settings = new AppSettings();
                collection.Insert(settings);
            }
            
            return settings;
        }
        catch (Exception)
        {
            RecreateDatabase();
            return new AppSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            if (_database == null)
            {
                InitializeDatabase();
            }

            settings.Id = 1;
            var collection = _database!.GetCollection<AppSettings>(CollectionName);
            collection.Upsert(settings);
        }
        catch (Exception)
        {
            RecreateDatabase();
            try
            {
                var collection = _database!.GetCollection<AppSettings>(CollectionName);
                collection.Upsert(settings);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Failed to save settings after database recreation");
            }
        }
    }

    public void SaveLanguage(string languageCode)
    {
        var settings = GetSettings();
        settings.Language = languageCode;
        SaveSettings(settings);
    }

    public string GetLanguage()
    {
        return GetSettings().Language;
    }

    public string GetDatabasePath() => _databasePath;

    public void Dispose()
    {
        _database?.Dispose();
        _database = null;
    }
}
