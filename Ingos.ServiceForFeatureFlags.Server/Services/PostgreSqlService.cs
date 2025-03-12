using System.Text;
using Ingos.ServiceForFeatureFlags.Server.Models;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace Ingos.ServiceForFeatureFlags.Server.Services;

public class PostgreSqlService
{
    NpgsqlConnection _conn;
    readonly IMemoryCache _cache;
    private readonly List<string> _settingsKeys = new List<string>();
    private string _connectionString;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string,string> _predefinedDatabases = new Dictionary<string, string>();

    public PostgreSqlService(IMemoryCache memoryCache,IConfiguration configuration)
    {
        _cache = memoryCache;
        _configuration = configuration;
        getAllDataBases();
    }

    void clearCache()
    {
        foreach (var key in _settingsKeys)
        {
            _cache.Remove(key);
        }

        _settingsKeys.Clear();
    }
    /// <summary>
    /// если dataBaseName существует в списке предопределенных дб, то идет проверка, если нет, но пропускаем ту строку, которая есть
    /// </summary>
    /// <param name="dataBaseName"></param>
    public void SetConnectionString(string dataBaseName)
    {
        if (!string.IsNullOrEmpty(dataBaseName) && _predefinedDatabases.ContainsKey(dataBaseName))
        {
            _connectionString = _predefinedDatabases.GetValueOrDefault(dataBaseName, dataBaseName);
        }
        else _connectionString = dataBaseName;
    }

    public bool ConnectToDataBase()
    {
        try
        {
            _conn = new NpgsqlConnection(_connectionString);
            _conn.Open();
            clearCache();
            return true;
        }
        catch
        {
            throw new Exception("не удалось подключиться");
        }
    }

    void GetAllSettings()
    {
        string query = "SELECT * FROM settings"; //добавить постраничный вывод
        using var cmd = new NpgsqlCommand(query, _conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var setting = new Setting
            {
                Type = reader["setting_type"].ToString()!,
                Code = reader["code"].ToString()!,
                Name = reader["setting_name"].ToString()!,
                Status = (bool)reader["status"],
                DateTime = (DateTime)reader["datetime"],
                StringValue = reader["stringvalue"].ToString(),
                IntValue = (int)reader["intvalue"],
                BoolValue = (bool)reader["boolvalue"],
                Description = reader["description"].ToString()
            };
            if (_settingsKeys.Contains(setting.Code))
            {
                _cache.TryGetValue(setting.Code, out Setting cacheSetting);
                if (setting.Equals(cacheSetting))
                {
                    cacheSetting = setting;
                }
            }
            else
            {
                //дделать ли проверку на существование в кэшэ, но может быть возможность изменения в базе, тогда кэш бюудет тоже обновлен
                _cache.Set(setting.Code, setting);
                _settingsKeys.Add(setting.Code);
            }
        }
    }

    public List<Setting> RetrieveAllSettings()
    {
        GetAllSettings();
        List<Setting> settings = new List<Setting>();
        ;
        foreach (var key in _settingsKeys)
        {
            settings.Add(_cache.Get<Setting>(key));
        }

        return settings;
    }

    public void InsertSetting(Setting setting)
    {
        string insertQuery =
            @"INSERT INTO settings (setting_type, code, setting_name, status, datetime, stringvalue, intvalue, boolvalue, description, isn_name)
        VALUES (@setting_type, @code, @setting_name, @status, @datetime, @stringvalue, @intvalue, @boolvalue, @description, @isn_name)";

        using var cmd = new NpgsqlCommand(insertQuery, _conn);
        var parameters = new[]
        {
            new NpgsqlParameter("@setting_type", setting.Type),
            new NpgsqlParameter("@code", setting.Code),
            new NpgsqlParameter("@setting_name", setting.Name),
            new NpgsqlParameter("@status", setting.Status),
            new NpgsqlParameter("@datetime", setting.DateTime),
            new NpgsqlParameter("@stringvalue", setting.StringValue),
            new NpgsqlParameter("@intvalue", setting.IntValue),
            new NpgsqlParameter("@boolvalue", setting.BoolValue),
            new NpgsqlParameter("@description", setting.Description),
            new NpgsqlParameter("@isn_name", setting.Isn_Name)
        };

        cmd.Parameters.AddRange(parameters);
        int rowsAffected = cmd.ExecuteNonQuery();

        _cache.Set(setting.Code, setting);
    }

    public bool IsExistSetting(string code)
    {
        if (_cache.TryGetValue(code, out Setting? setting)) return true;

        string countQuery = @"SELECT count(1) from settings where code = @code";
        using var cmd = new NpgsqlCommand(countQuery, _conn);

        cmd.Parameters.AddWithValue("@code", code);
        var rowsAffected = (Int64)cmd.ExecuteScalar()!;
        if (rowsAffected > 0) return true;

        return false;
    }

    public Setting GetSetting(string code, string setting_type)
    {
        if (_cache.TryGetValue(code, out Setting? s)) return s;
        string selectQuery =
            @"SELECT * from settings where code = @code and setting_type = @setting_type";

        using var cmd = new NpgsqlCommand(selectQuery, _conn);
        cmd.Parameters.AddWithValue("@code", code);
        cmd.Parameters.AddWithValue("@setting_type", setting_type);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var setting = new Setting
            {
                Code = reader["code"].ToString()!,
                Name = reader["setting_name"].ToString()!,
                Type = reader["setting_type"].ToString()!,
                Status = (bool)reader["status"],
                DateTime = (DateTime)reader["datetime"],
                StringValue = reader["stringvalue"].ToString(),
                IntValue = Convert.ToInt32(reader["intvalue"]),
                BoolValue = Convert.ToBoolean(reader["boolvalue"]),
                Description = reader["description"].ToString()
            };
            return setting;
        }

        return null;
    }

    public bool UpdateSetting(Setting setting)
    {
        //по идее проверка не нужна, но нельзя исключать что кто то может дергать апи руками
        if (IsExistSetting(setting.Code))
        {
            string updateQuery =
                @"UPDATE settings  set (setting_type = @setting_type, setting_name= @setting_name, status=@status, datetime=@datetime, stringvalue=@stringvalue, intvalue=@intvalue, boolvalue=@boolvalue, description=@description, isn_name = @isn_name)
        where code=@code)";

            using var cmd = new NpgsqlCommand(updateQuery, _conn);
            var parameters = new[]
            {
                new NpgsqlParameter("@setting_type", setting.Type),
                new NpgsqlParameter("@code", setting.Code),
                new NpgsqlParameter("@setting_name", setting.Name),
                new NpgsqlParameter("@status", setting.Status),
                new NpgsqlParameter("@datetime", setting.DateTime),
                new NpgsqlParameter("@stringvalue", setting.StringValue),
                new NpgsqlParameter("@intvalue", setting.IntValue),
                new NpgsqlParameter("@boolvalue", setting.BoolValue),
                new NpgsqlParameter("@description", setting.Description),
                new NpgsqlParameter("@isn_name", setting.Isn_Name)
            };

            cmd.Parameters.AddRange(parameters);
            int rowsAffected = cmd.ExecuteNonQuery();
            
            _cache.Remove(setting.Code);
            _cache.Set(setting.Code, setting);
            
            return true;
        }

        return false;
    }


    public bool DeleteSetting(string code)
    {
        if (IsExistSetting(code))
        {
            string deleteQuery = "DELETE FROM settings WHERE code = @code";

            using var cmd = new NpgsqlCommand(deleteQuery, _conn);
            cmd.Parameters.AddWithValue("@code", code);
            int rowsAffected = cmd.ExecuteNonQuery();
            _cache.Remove(code);

            return true;
        }

        return false;
    }

    void getAllDataBases()
    {
        var connectionStrings = _configuration.GetSection("ConnectionStrings").GetChildren();

        foreach (var connectionString in connectionStrings)
        {
            if (_predefinedDatabases.Keys.Contains(connectionString.Key))
            {
               continue; 
            }
            _predefinedDatabases.Add(connectionString.Key, connectionString.Value);
        }
    }

    public List<string> GetAllDataBasesNames()
    {

        return _predefinedDatabases.Keys.ToList();
    }

}