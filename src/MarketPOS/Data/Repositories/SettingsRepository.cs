using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace MarketPOS.Data.Repositories;

public class SettingsRepository
{
    private static readonly string ConfigJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    private static readonly object FileLock = new();

    public string Get(string key, string defaultValue = "")
    {
        // 1. Try reading from config.json first (resilient file-based configuration)
        try
        {
            var jsonDict = ReadJsonConfigFile();
            if (jsonDict.TryGetValue(key, out string? jsonVal) && !string.IsNullOrEmpty(jsonVal))
            {
                return jsonVal;
            }
        }
        catch { }

        // 2. Fallback to SQLite Settings table
        try
        {
            using var conn = DatabaseContext.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key = @k LIMIT 1;";
            cmd.Parameters.AddWithValue("@k", key);

            var val = cmd.ExecuteScalar();
            if (val != null)
            {
                string dbVal = val.ToString() ?? defaultValue;
                // Sync back to config.json
                SaveToJsonConfigFile(key, dbVal);
                return dbVal;
            }
        }
        catch { }

        return defaultValue;
    }

    public void Set(string key, string value)
    {
        // 1. Save to config.json immediately
        SaveToJsonConfigFile(key, value);

        // 2. Save to SQLite database
        try
        {
            using var conn = DatabaseContext.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Settings (Key, Value) VALUES (@k, @v)
                ON CONFLICT(Key) DO UPDATE SET Value = @v;
            ";
            cmd.Parameters.AddWithValue("@k", key);
            cmd.Parameters.AddWithValue("@v", value);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }

    public Dictionary<string, string> GetAll()
    {
        var dict = ReadJsonConfigFile();

        try
        {
            using var conn = DatabaseContext.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Key, Value FROM Settings;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string k = reader.GetString(0);
                string v = reader.IsDBNull(1) ? "" : reader.GetString(1);
                if (!dict.ContainsKey(k))
                {
                    dict[k] = v;
                }
            }
        }
        catch { }

        return dict;
    }

    private static Dictionary<string, string> ReadJsonConfigFile()
    {
        lock (FileLock)
        {
            try
            {
                if (File.Exists(ConfigJsonPath))
                {
                    string json = File.ReadAllText(ConfigJsonPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null) return dict;
                }
            }
            catch { }

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void SaveToJsonConfigFile(string key, string value)
    {
        lock (FileLock)
        {
            try
            {
                var dict = ReadJsonConfigFile();
                dict[key] = value;

                string json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigJsonPath, json);
            }
            catch { }
        }
    }

    public static void BackupDatabase(string destinationFilePath)
    {
        string currentDb = DatabaseContext.DatabasePath;
        if (!File.Exists(currentDb))
            throw new FileNotFoundException("Veritabanı dosyası bulunamadı.");

        // Checkpoint SQLite WAL file
        try
        {
            using var conn = DatabaseContext.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            cmd.ExecuteNonQuery();
        }
        catch { }

        File.Copy(currentDb, destinationFilePath, true);
    }
}
