using System.Data;
using Microsoft.Data.SqlClient;

namespace Clinica.Infrastructure.Database;

/// <summary>Fábrica de parámetros SQL tipados — evita repetición y errores de tipo.</summary>
public static class Sql
{
    public static SqlParameter BigInt(string name, long? value)     => new(name, SqlDbType.BigInt)    { Value = value.HasValue ? value : DBNull.Value };
    public static SqlParameter Int(string name, int? value)         => new(name, SqlDbType.Int)       { Value = value.HasValue ? value : DBNull.Value };
    public static SqlParameter NVarChar(string name, string? value, int size = 500) => new(name, SqlDbType.NVarChar, size) { Value = (object?)value ?? DBNull.Value };
    public static SqlParameter Bit(string name, bool? value)        => new(name, SqlDbType.Bit)       { Value = value.HasValue ? value : DBNull.Value };
    public static SqlParameter UniqueId(string name, Guid? value)   => new(name, SqlDbType.UniqueIdentifier) { Value = value.HasValue ? value : DBNull.Value };
}
