using System.Data;
using System.Reflection;
using System.Text;
using Ingos.ServiceForFeatureFlags.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Expressions;
using Npgsql;
using NpgsqlTypes;

namespace Ingos.ServiceForFeatureFlags.Server.Helpers;

public class PostgreSqlService
{
    private readonly string _connectionString;
    NpgsqlConnection Conn;
    List<Setting> Settings;

    public PostgreSqlService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        Conn = new NpgsqlConnection(_connectionString);
        Conn.Open();
        Settings = GetAllSettings();
    }

    List<Setting> GetAllSettings()
    {
        var settings = new List<Setting>();
        string query = "SELECT * FROM settings";
        using var cmd = new NpgsqlCommand(query, Conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            settings.Add(new Setting
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
            });
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
            new NpgsqlParameter("setting_type", setting.Type),
            new NpgsqlParameter("code", setting.Code),
            new NpgsqlParameter("setting_name", setting.Name),
            new NpgsqlParameter("status", setting.Status),
            new NpgsqlParameter("datetime", setting.DateTime),
            new NpgsqlParameter("stringvalue", setting.StringValue),
            new NpgsqlParameter("intvalue", setting.IntValue),
            new NpgsqlParameter("boolvalue", setting.BoolValue),
            new NpgsqlParameter("description", setting.Description)
        };

        cmd.Parameters.AddRange(parameters);
        int rowsAffected = cmd.ExecuteNonQuery();

        Settings.Add(setting);
    }

    public bool IsExistSetting(string code)
    {
        if (Settings.Any(s => s.Code == code)) return true;

        string countQuery = @"SELECT count(1) from settings where code = @code";
        using var cmd = new NpgsqlCommand(countQuery, Conn);

        cmd.Parameters.AddWithValue("@code", code);
        var rowsAffected = (Int64)cmd.ExecuteScalar()!;
        if (rowsAffected > 0) return true;

        return false;
    }

    public Setting GetSetting(string code, string setting_type)
    {
        if (Settings.Any(s => s.Code == code)) return Settings.First(s => s.Code == code);
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

    public bool UpdateSetting(string code, bool status, DateTime datetime = default, string stringvalue = "Undefined",
        int intvalue = 0)
    {
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
            Settings.Remove(Settings.First(s => s.Code == code));

            return true;
        }

        return false;
    }

    void UpdateCacheSettings(string code, Setting changedSettingFields, bool dateTimeFlg, bool stringValueFlg,
        bool intValueFlg)
    {
        var setting = Settings.First(s => s.Code == code);

        if (dateTimeFlg) setting.DateTime = changedSettingFields.DateTime;
        if (stringValueFlg) setting.StringValue = changedSettingFields.StringValue;
        if (intValueFlg) setting.IntValue = changedSettingFields.IntValue;
        //cамое топорное решение
    }
}