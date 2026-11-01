using Npgsql;

namespace Ceviri.Api.Data;

public static class Database
{
    // DATABASE_URL önce ortam değişkeninden, yoksa repo kökündeki .env.local'den okunur.
    public static string GetConnectionString(IConfiguration config, string contentRoot)
    {
        var url = config["DATABASE_URL"] ?? ReadEnvFile(Path.Combine(contentRoot, "..", "..", ".env.local"), "DATABASE_URL");
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("DATABASE_URL bulunamadı. Ortam değişkeni olarak verin veya .env.local dosyasına ekleyin.");

        return ToNpgsqlConnectionString(url);
    }

    // Npgsql postgresql:// URI biçimini kabul etmez; anahtar=değer biçimine çevirir.
    static string ToNpgsqlConnectionString(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            SslMode = SslMode.Require,
        };

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        if (query["channel_binding"] == "require")
            builder.ChannelBinding = ChannelBinding.Require;

        return builder.ConnectionString;
    }

    static string? ReadEnvFile(string path, string key)
    {
        if (!File.Exists(path)) return null;

        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split('=', 2);
            if (parts.Length == 2 && parts[0].Trim() == key)
                return parts[1].Trim().Trim('"');
        }
        return null;
    }

    // Şemayı uygular ve data/glossary.csv'yi yükler. CSV esas kaynaktır: tablo her açılışta
    // CSV ile eşitlenir, böylece CSV'de düzeltilen veya silinen maddeler veritabanından da kalkar.
    public static async Task InitializeAsync(NpgsqlDataSource dataSource, string contentRoot)
    {
        var repoRoot = Path.Combine(contentRoot, "..", "..");
        var schema = await File.ReadAllTextAsync(Path.Combine(repoRoot, "db", "schema.sql"));
        await using (var cmd = dataSource.CreateCommand(schema))
            await cmd.ExecuteNonQueryAsync();

        var rows = File.ReadLines(Path.Combine(repoRoot, "data", "glossary.csv"))
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(','));

        await using var conn = await dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        await using var batch = new NpgsqlBatch(conn, tx);
        batch.BatchCommands.Add(new NpgsqlBatchCommand("TRUNCATE glossary RESTART IDENTITY"));
        foreach (var r in rows)
        {
            var bc = new NpgsqlBatchCommand(
                "INSERT INTO glossary (modern_kelime, eski_karsilik, seviye, tdk_ek) VALUES ($1, $2, $3, $4) ON CONFLICT DO NOTHING");
            bc.Parameters.Add(new() { Value = r[0].Trim() });
            bc.Parameters.Add(new() { Value = r[1].Trim() });
            bc.Parameters.Add(new() { Value = short.Parse(r[2]) });
            bc.Parameters.Add(new() { Value = r.Length > 3 && r[3].Trim() != "" ? r[3].Trim() : DBNull.Value, DataTypeName = "text" });
            batch.BatchCommands.Add(bc);
        }
        await batch.ExecuteNonQueryAsync();
        await tx.CommitAsync();
    }
}
