using Microsoft.Data.SqlClient;

namespace Clinica.Infrastructure.Database;

internal static class SqlDataReaderExtensions
{
    public static bool HasColumn(this SqlDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string? GetNullableString(this SqlDataReader reader, string columnName)
    {
        if (!reader.HasColumn(columnName))
        {
            return null;
        }

        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static int GetInt32OrDefault(this SqlDataReader reader, string columnName, int defaultValue = 0)
    {
        if (!reader.HasColumn(columnName))
        {
            return defaultValue;
        }

        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? defaultValue : Convert.ToInt32(reader.GetValue(ordinal));
    }

    public static int? GetNullableInt32(this SqlDataReader reader, string columnName)
    {
        if (!reader.HasColumn(columnName))
        {
            return null;
        }

        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    }

    public static long GetInt64OrDefault(this SqlDataReader reader, string columnName, long defaultValue = 0)
    {
        if (!reader.HasColumn(columnName))
        {
            return defaultValue;
        }

        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? defaultValue : Convert.ToInt64(reader.GetValue(ordinal));
    }

    public static long? GetNullableInt64(this SqlDataReader reader, string columnName)
    {
        if (!reader.HasColumn(columnName))
        {
            return null;
        }

        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt64(reader.GetValue(ordinal));
    }

    public static bool GetBooleanOrDefault(this SqlDataReader reader, string columnName, bool defaultValue = false)
    {
        if (!reader.HasColumn(columnName))
        {
            return defaultValue;
        }

        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? defaultValue : Convert.ToBoolean(reader.GetValue(ordinal));
    }

    public static DateTime GetDateTimeOrDefault(this SqlDataReader reader, string columnName)
    {
        if (!reader.HasColumn(columnName))
        {
            return default;
        }

        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? default : reader.GetDateTime(ordinal);
    }

    public static DateTime? GetNullableDateTime(this SqlDataReader reader, string columnName)
    {
        if (!reader.HasColumn(columnName))
        {
            return null;
        }

        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}