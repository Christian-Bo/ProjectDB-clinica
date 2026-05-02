using System.Data;

namespace Clinica.Infrastructure.Database;

/// <summary>Extensiones para leer columnas de DataRow de forma segura y sin excepciones.</summary>
internal static class DataRowExtensions
{
    public static string  Str(this DataRow row, string col)         => row.IsNull(col) ? string.Empty : row[col].ToString()!;
    public static string? StrNull(this DataRow row, string col)     => row.IsNull(col) ? null : row[col].ToString();
    public static int     Int32(this DataRow row, string col)       => row.IsNull(col) ? 0 : Convert.ToInt32(row[col]);
    public static int?    Int32Null(this DataRow row, string col)   => row.IsNull(col) ? null : Convert.ToInt32(row[col]);
    public static long    Int64(this DataRow row, string col)       => row.IsNull(col) ? 0L : Convert.ToInt64(row[col]);
    public static long?   Int64Null(this DataRow row, string col)   => row.IsNull(col) ? null : Convert.ToInt64(row[col]);
    public static bool    Bool(this DataRow row, string col)        => !row.IsNull(col) && Convert.ToBoolean(row[col]);
    public static DateTime  DateTime(this DataRow row, string col)  => row.IsNull(col) ? System.DateTime.UtcNow : Convert.ToDateTime(row[col]);
    public static DateTime? DateTimeNull(this DataRow row, string col) => row.IsNull(col) ? null : Convert.ToDateTime(row[col]);

    public static bool HasColumn(this DataTable table, string col)  => table.Columns.Contains(col);
}
