using Npgsql;

namespace Ceviri.Api.Data;

// TdkEk: TDK'deki belirtme hâli ipucu ("harp, -bi" -> "bi"); düzenli kelimelerde null.
public record GlossaryEntry(string ModernKelime, string EskiKarsilik, short Seviye, string? TdkEk = null);

public class GlossaryRepository(NpgsqlDataSource dataSource)
{
    // seviye: 1 = hafif, 2 = orta, 3 = ağır. Verilen seviyeye kadar olan maddeler döner.
    public async Task<List<GlossaryEntry>> GetAsync(string? modernKelime = null, short seviye = 3)
    {
        await using var cmd = dataSource.CreateCommand(
            """
            SELECT modern_kelime, eski_karsilik, seviye, tdk_ek FROM glossary
            WHERE ($1::text IS NULL OR lower(modern_kelime) = lower($1))
              AND seviye <= $2
            ORDER BY modern_kelime
            """);
        cmd.Parameters.Add(new() { Value = (object?)modernKelime ?? DBNull.Value, DataTypeName = "text" });
        cmd.Parameters.Add(new() { Value = seviye });

        var result = new List<GlossaryEntry>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetInt16(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
        return result;
    }
}
