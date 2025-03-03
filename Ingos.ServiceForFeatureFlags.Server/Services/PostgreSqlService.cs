using System.Text;
using Ingos.ServiceForFeatureFlags.Server.Models;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace Ingos.ServiceForFeatureFlags.Server.Services;

public class PostgreSqlService
{
    readonly string _connectionString;
    NpgsqlConnection Conn;
    readonly IMemoryCache _cache;
    private readonly List<string> _settingsKeys = new List<string>();
    

    public PostgreSqlService(IConfiguration configuration, IMemoryCache memoryCache)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        Conn = new NpgsqlConnection(_connectionString);
        Conn.Open();
        _cache = memoryCache;
        GetAllSettings();
    }

    void GetAllSettings()
    {
        string query = "SELECT * FROM settings";
        using var cmd = new NpgsqlCommand(query, Conn);
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
        List<Setting> settings =new List<Setting>(); ;
        foreach (var key in _settingsKeys)
        {
            settings.Add(_cache.Get<Setting>(key));
        }
        return settings;
    }

    public void InsertSetting(Setting setting)
    {
        string insertQuery =
            @"INSERT INTO settings (setting_type, code, setting_name, status, datetime, stringvalue, intvalue, boolvalue, description)
        VALUES (@setting_type, @code, @setting_name, @status, @datetime, @stringvalue, @intvalue, @boolvalue, @description)";

        using var cmd = new NpgsqlCommand(insertQuery, Conn);
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
            new NpgsqlParameter("@description", setting.Description)
        };

        cmd.Parameters.AddRange(parameters);
        int rowsAffected = cmd.ExecuteNonQuery();

        _cache.Set(setting.Code, setting);
    }

    public bool IsExistSetting(string code)
    {
        if(_cache.TryGetValue(code, out Setting? setting)) return true;

        string countQuery = @"SELECT count(1) from settings where code = @code";
        using var cmd = new NpgsqlCommand(countQuery, Conn);

        cmd.Parameters.AddWithValue("@code", code);
        var rowsAffected = (Int64)cmd.ExecuteScalar()!;
        if (rowsAffected > 0) return true;

        return false;
    }

    public Setting GetSetting(string code, string setting_type)
    {
        if(_cache.TryGetValue(code, out Setting? s)) return s;
        string selectQuery =
            @"SELECT * from settings where code = @code and setting_type = @setting_type";

        using var cmd = new NpgsqlCommand(selectQuery, Conn);
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

    public bool UpdateSetting(string code, bool status, DateTime datetime = default, string stringvalue = "Undefined", int intvalue = 0)
    {
        //добавить условие авторизации
        if (IsExistSetting(code))
        {
            var updateQuery = new StringBuilder("UPDATE settings SET status = @status");

            var changedSettingFields = new Setting();
            bool dateTimeFlg = false;
            bool stringValueFlg = false;
            bool intValueFlg = false;

            var parameters = new List<NpgsqlParameter>();
            parameters.Add(new NpgsqlParameter("status", status));

            changedSettingFields.Status = status;

            if (datetime != default)
            {
                updateQuery.Append(", datetime = @datetime");
                parameters.Add(new NpgsqlParameter("datetime", datetime));
                changedSettingFields.DateTime = datetime;
                dateTimeFlg = true;
            }

            if (stringvalue != "Undefined")
            {
                updateQuery.Append(", stringvalue = @stringvalue");
                parameters.Add(new NpgsqlParameter("@stringvalue", stringvalue));
                changedSettingFields.StringValue = stringvalue;
                stringValueFlg = true;
            }

            if (intvalue != 0)
            {
                updateQuery.Append(", intvalue = @intvalue");
                parameters.Add(new NpgsqlParameter("@intvalue", intvalue));
                changedSettingFields.IntValue = intvalue;
                intValueFlg = true;
            }

            updateQuery.Append(" WHERE code = @code");

            parameters.Add(new NpgsqlParameter("@code", code));
            using var cmd = new NpgsqlCommand(updateQuery.ToString(), Conn);
            cmd.Parameters.AddRange(parameters.ToArray());

            int rowsAffected = cmd.ExecuteNonQuery();
            UpdateCacheSettings(code, changedSettingFields, dateTimeFlg, stringValueFlg, intValueFlg);
            return true;
        }

        return false;
    }


    public bool DeleteSetting(string code)
    {
        if (IsExistSetting(code))
        {
            string deleteQuery = "DELETE FROM settings WHERE code = @code";

            using var cmd = new NpgsqlCommand(deleteQuery, Conn);
            cmd.Parameters.AddWithValue("@code", code);
            int rowsAffected = cmd.ExecuteNonQuery();
           _cache.Remove(code);

            return true;
        }

        return false;
    }

    void UpdateCacheSettings(string code, Setting changedSettingFields, bool dateTimeFlg, bool stringValueFlg, bool intValueFlg)
    {
        var setting = (Setting)_cache.Get(code);

        if (dateTimeFlg) setting.DateTime = changedSettingFields.DateTime;
        if (stringValueFlg) setting.StringValue = changedSettingFields.StringValue;
        if (intValueFlg) setting.IntValue = changedSettingFields.IntValue;
    }
}