using Ceviri.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Ceviri.Api.Controllers;

[ApiController]
[Route("api/glossary")]
public class GlossaryController(GlossaryRepository glossary, NpgsqlDataSource dataSource) : ControllerBase
{
    // GET /api/glossary?q=öğretmen&seviye=2
    [HttpGet]
    public Task<List<GlossaryEntry>> Get(string? q, short seviye = 3) => glossary.GetAsync(q, seviye);

    // GET /api/glossary/health — veritabanı bağlantısını kontrol eder
    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        await using var cmd = dataSource.CreateCommand("SELECT count(*) FROM glossary");
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        return Ok(new { database = "ok", glossaryCount = count });
    }
}
