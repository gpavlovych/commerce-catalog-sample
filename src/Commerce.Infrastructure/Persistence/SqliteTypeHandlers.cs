using System.Data;
using System.Globalization;
using Dapper;

namespace Commerce.Infrastructure.Persistence;

/// <summary>
/// SQLite has no native GUID, decimal, or datetimeoffset type. These handlers store those values as
/// round-trippable text so the same Dapper queries work against SQLite (demo) and SQL Server (production).
/// Registered only when the configured provider is SQLite.
/// </summary>
public static class SqliteTypeHandlers
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new DecimalHandler());
        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        _registered = true;
    }

    private sealed class GuidHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value) => Guid.Parse((string)value);
        public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value.ToString();
    }

    private sealed class DecimalHandler : SqlMapper.TypeHandler<decimal>
    {
        public override decimal Parse(object value) =>
            decimal.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);

        public override void SetValue(IDbDataParameter parameter, decimal value) =>
            parameter.Value = value.ToString(CultureInfo.InvariantCulture);
    }

    private sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value) =>
            DateTimeOffset.Parse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value) =>
            parameter.Value = value.ToString("O", CultureInfo.InvariantCulture);
    }
}
