using Ceviri.Api.Data;
using Ceviri.Api.Translation;
using Microsoft.AspNetCore.Mvc;

namespace Ceviri.Api.Controllers;

// seviye: 1 = hafif, 2 = orta, 3 = ağır
public record TranslateRequest(string Text, short Seviye = 2);

[ApiController]
[Route("api/translate")]
public class TranslateController(GlossaryRepository glossary) : ControllerBase
{
    // POST /api/translate  { "text": "Öğretmen okula gitti.", "seviye": 2 }
    [HttpPost]
    public async Task<ActionResult<TranslationResult>> Post(TranslateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Metin boş olamaz.");
        if (request.Seviye is < 1 or > 3)
            return BadRequest("Seviye 1 (hafif), 2 (orta) veya 3 (ağır) olmalı.");

        var translator = new GlossaryTranslator(await glossary.GetAsync(seviye: request.Seviye));
        return translator.Translate(request.Text);
    }
}
