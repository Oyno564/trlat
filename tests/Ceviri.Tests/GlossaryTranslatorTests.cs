using Ceviri.Api.Data;
using Ceviri.Api.Translation;

namespace Ceviri.Tests;

public class GlossaryTranslatorTests
{
    // data/glossary.csv ile aynı veri; testler veritabanı gerektirmez.
    static readonly List<GlossaryEntry> Glossary = File.ReadLines(Path.Combine(RepoRoot(), "data", "glossary.csv"))
        .Skip(1)
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .Select(l => l.Split(','))
        .Select(r => new GlossaryEntry(r[0].Trim(), r[1].Trim(), short.Parse(r[2]), r.Length > 3 && r[3] != "" ? r[3] : null))
        .ToList();

    static string Translate(string text, short seviye = 3) =>
        new GlossaryTranslator(Glossary.Where(e => e.Seviye <= seviye)).Translate(text).Text;

    [Theory]
    [InlineData("Öğretmen geldi.", "Muallim geldi.")]
    [InlineData("Okula gittim.", "Mektebe gittim.")]
    [InlineData("öğrencilere", "talebelere")]
    [InlineData("bilgiyi", "malumatı")]
    [InlineData("bilgisinde", "malumatında")]
    [InlineData("kurumun", "müessesenin")]
    [InlineData("kuruma", "müesseseye")]
    [InlineData("barışta", "sulhta")]
    [InlineData("savaşta", "harpte")]
    [InlineData("savaşı", "harbi")]
    [InlineData("bilimin", "ilmin")]
    [InlineData("bilimde", "ilimde")]
    [InlineData("amacı", "maksadı")]
    [InlineData("gerçeği", "hakikati")]
    [InlineData("yüzyılda", "asırda")]
    [InlineData("yüzyılı", "asrı")]
    [InlineData("seçimde", "intihapta")]
    [InlineData("Gelecekte", "İstikbalde")]
    [InlineData("bağımsızlığı", "istiklali")]
    [InlineData("kanıtı", "delili")]
    [InlineData("bağımsızlık için", "istiklal için")]
    [InlineData("istiklali", "istiklali")]
    [InlineData("sebebi", "sebebi")]
    [InlineData("nedeni", "sebebi")]
    public void Kelimeleri_ve_ekleri_cevirir(string input, string expected) =>
        Assert.Equal(expected, Translate(input));

    [Fact]
    public void Ornek_cumleyi_cevirir() =>
        Assert.Equal(
            "İstiklal ve hürriyet için cemiyet birlikte çalıştı.",
            Translate("Bağımsızlık ve özgürlük için toplum birlikte çalıştı."));

    [Fact]
    public void Cok_kelimeli_maddeleri_cevirir() =>
        Assert.Equal("Daima böyle.", Translate("Her zaman böyle."));

    [Fact]
    public void Ek_olmayan_devami_eslestirmez()
    {
        Assert.Equal("ayak", Translate("ayak"));
        Assert.Equal("maksat", Translate("amaç"));
        Assert.Equal("lâkin", Translate("ama"));
    }

    [Fact]
    public void Seviyeye_gore_filtreler()
    {
        Assert.Equal("güneş", Translate("güneş", seviye: 2));
        Assert.Equal("şems", Translate("güneş", seviye: 3));
    }

    [Fact]
    public void Buyuk_harfi_korur() => Assert.Equal("MUALLİM", Translate("ÖĞRETMEN"));

    [Fact]
    public void Degisiklikleri_listeler()
    {
        var result = new GlossaryTranslator(Glossary).Translate("Öğretmen okula gitti.");
        Assert.Equal(["Öğretmen", "okula"], result.Changes.Select(c => c.Original));
        Assert.Equal(["Muallim", "mektebe"], result.Changes.Select(c => c.Replacement));
    }

    static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Ceviri.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Ceviri.sln bulunamadı.");
    }
}
