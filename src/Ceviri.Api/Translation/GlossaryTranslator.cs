using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Ceviri.Api.Data;

namespace Ceviri.Api.Translation;

public record Change(string Original, string Replacement, short Seviye);

public record TranslationResult(string Text, IReadOnlyList<Change> Changes);

// Sözlük tabanlı çeviri: metindeki kelimeleri sözlükte arar, dönem karşılığıyla değiştirir
// ve kelimenin eklerini (çekim eklerini) yeni köke ünlü uyumuyla uydurur.
public class GlossaryTranslator
{
    static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    static readonly Regex Word = new(@"\p{L}+", RegexOptions.Compiled);

    // Ünlü uyumundan bağımsız ek kalıpları: A = e/a, I = i/ı/u/ü.
    // Kökten sonra kalan kısım bu eklerin art arda gelmesinden oluşmuyorsa kelime eşleşmez
    // (örn. "ayak" kelimesi "ay" maddesiyle eşleşmez).
    static readonly string[] Suffixes =
    [
        "lAr", "I", "yI", "sI", "nI", "A", "yA", "nA", "dA", "tA", "ndA", "dAn", "tAn", "ndAn",
        "In", "nIn", "Im", "m", "ImIz", "mIz", "InIz", "nIz", "lA", "ylA", "ki", "dIr", "tIr", "cA", "çA",
    ];

    readonly List<(string[] Words, GlossaryEntry Entry)> entries;

    public GlossaryTranslator(IEnumerable<GlossaryEntry> glossary)
    {
        // Önce çok kelimeli ve uzun maddeler denenir: "amaç", "ama"dan önce gelir.
        entries = glossary
            .Select(e => (Words: e.ModernKelime.ToLower(Tr).Split(' ', StringSplitOptions.RemoveEmptyEntries), Entry: e))
            .OrderByDescending(x => x.Words.Length)
            .ThenByDescending(x => x.Words[^1].Length)
            .ThenBy(x => x.Entry.Seviye)
            .ToList();
    }

    public TranslationResult Translate(string text)
    {
        var words = Word.Matches(text);
        var output = new StringBuilder();
        var changes = new List<Change>();
        var pos = 0;

        for (var i = 0; i < words.Count;)
        {
            if (!TryMatch(text, words, i, out var entry, out var keyWords, out var suffix))
            {
                i++;
                continue;
            }

            var first = words[i];
            var last = words[i + keyWords.Length - 1];
            var end = last.Index + last.Length;
            var replacement = MatchCase(first.Value, Inflect(entry.EskiKarsilik, entry.TdkEk, keyWords[^1], suffix));

            output.Append(text, pos, first.Index - pos).Append(replacement);
            changes.Add(new(text[first.Index..end], replacement, entry.Seviye));
            pos = end;
            i += keyWords.Length;
        }

        output.Append(text, pos, text.Length - pos);
        return new(output.ToString(), changes);
    }

    bool TryMatch(string text, MatchCollection words, int i, out GlossaryEntry entry, out string[] keyWords, out string suffix)
    {
        foreach (var (key, e) in entries)
        {
            if (i + key.Length > words.Count) continue;

            var ok = true;
            for (var k = 0; k < key.Length - 1 && ok; k++)
            {
                var (w, next) = (words[i + k], words[i + k + 1]);
                ok = w.Value.ToLower(Tr) == key[k]
                     && string.IsNullOrWhiteSpace(text[(w.Index + w.Length)..next.Index]);
            }

            var lastWord = words[i + key.Length - 1].Value.ToLower(Tr);
            if (!ok) continue;

            // bağımsızlık -> bağımsızlığ-ı: ünlüyle başlayan ekten önce yumuşamış kök de kabul edilir
            foreach (var stem in new[] { key[^1], Soften(key[^1]) })
            {
                if (!lastWord.StartsWith(stem, StringComparison.Ordinal)) continue;

                var rest = lastWord[stem.Length..];
                if ((stem == key[^1] || (rest.Length > 0 && IsVowel(rest[0]))) && IsSuffixChain(rest))
                {
                    (entry, keyWords, suffix) = (e, key, rest);
                    return true;
                }
            }
        }

        (entry, keyWords, suffix) = (null!, [], "");
        return false;
    }

    static string Soften(string word) => word[^1] switch
    {
        'p' => word[..^1] + 'b',
        'ç' => word[..^1] + 'c',
        't' => word[..^1] + 'd',
        'k' => word[..^1] + 'ğ',
        _ => word,
    };

    static bool IsSuffixChain(string rest)
    {
        var pattern = new string(rest.Select(c => c switch
        {
            'e' or 'a' => 'A',
            'i' or 'ı' or 'u' or 'ü' => 'I',
            _ => c,
        }).ToArray());

        return Split(0);

        bool Split(int start) =>
            start == pattern.Length
            || Suffixes.Any(s => string.CompareOrdinal(pattern, start, s, 0, s.Length) == 0
                                 && start + s.Length <= pattern.Length
                                 && Split(start + s.Length));
    }

    // Ekleri yeni köke uydurur: kaynaştırma harfi, ünsüz yumuşaması, ünlü düşmesi, ünlü uyumu, d/t benzeşmesi.
    // tdkEk, TDK'nin belirtme hâli ipucudur ("harp, -bi", "ilim, -lmi"); düzensiz kelimeleri bu belirler.
    static string Inflect(string root, string? tdkEk, string modernRoot, string suffix)
    {
        if (suffix.Length == 0) return root;

        // Ünlüyle başlayan eklerden önceki kök: harp -> harb-, ilim -> ilm-, maksat -> maksad-
        var vowelStem = root;
        var harmonyVowel = LastVowel(root);
        if (!string.IsNullOrEmpty(tdkEk))
        {
            var consonants = tdkEk[..^1];
            vowelStem = consonants.Length == 1 ? root[..^1] + consonants : root[..^3] + consonants;
            harmonyVowel = tdkEk[^1];
        }

        var modernEndsVowel = IsVowel(modernRoot[^1]);
        var rootEndsVowel = IsVowel(root[^1]);

        if (modernEndsVowel && !rootEndsVowel && suffix.Length > 1 && suffix[0] is 'y' or 's' or 'n' && IsVowel(suffix[1]))
        {
            // bilgi-yi -> malumat-ı, bilgi-si -> malumat-ı
            suffix = suffix[1..];
        }
        else if (!modernEndsVowel && rootEndsVowel && IsVowel(suffix[0]))
        {
            // kurum-un -> müessese-nin, kurum-um -> müessese-m, kurum-a -> müessese-ye
            var possessive = suffix is [_, 'm'] or [_, 'm', _, 'z'] or [_, 'n', _, 'z'];
            if (suffix is [_, 'n']) suffix = "n" + suffix;
            else if (possessive) suffix = suffix[1..];
            else suffix = "y" + suffix;
        }

        if (!rootEndsVowel && IsVowel(suffix[0])) root = vowelStem;

        var harmonized = Harmonize(suffix, harmonyVowel);

        // malumat-da -> malumat-ta
        if (harmonized[0] is 'd' or 't' or 'c' or 'ç' && !rootEndsVowel)
        {
            var voiceless = "çfhkpsşt".Contains(root[^1]);
            harmonized = (harmonized[0], voiceless) switch
            {
                ('d', true) => 't' + harmonized[1..],
                ('t', false) => 'd' + harmonized[1..],
                ('c', true) => 'ç' + harmonized[1..],
                ('ç', false) => 'c' + harmonized[1..],
                _ => harmonized,
            };
        }

        return root + harmonized;
    }

    static string Harmonize(string suffix, char previous)
    {
        var sb = new StringBuilder(suffix.Length);
        foreach (var c in suffix)
        {
            var front = previous is 'e' or 'i' or 'ö' or 'ü' or 'î';
            var rounded = previous is 'o' or 'u' or 'ö' or 'ü' or 'û';
            var v = c switch
            {
                'e' or 'a' => front ? 'e' : 'a',
                'i' or 'ı' or 'u' or 'ü' => (front, rounded) switch
                {
                    (true, false) => 'i',
                    (true, true) => 'ü',
                    (false, false) => 'ı',
                    (false, true) => 'u',
                },
                _ => c,
            };
            if (IsVowel(v)) previous = v;
            sb.Append(v);
        }
        return sb.ToString();
    }

    static char LastVowel(string word)
    {
        var v = word.LastOrDefault(IsVowel);
        return v switch { 'â' => 'a', 'î' => 'i', 'û' => 'u', '\0' => 'e', _ => v };
    }

    static bool IsVowel(char c) => "aeıioöuüâîû".Contains(c);

    static string MatchCase(string original, string replacement)
    {
        if (original.Length > 1 && original == original.ToUpper(Tr)) return replacement.ToUpper(Tr);
        if (char.IsUpper(original[0])) return replacement[..1].ToUpper(Tr) + replacement[1..];
        return replacement;
    }
}
