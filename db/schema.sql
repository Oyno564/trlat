-- Sözlük: günümüz kelimesi -> dönem karşılığı
CREATE TABLE IF NOT EXISTS glossary (
    id             serial PRIMARY KEY,
    modern_kelime  text     NOT NULL,
    eski_karsilik  text     NOT NULL,
    seviye         smallint NOT NULL CHECK (seviye BETWEEN 1 AND 3),
    UNIQUE (modern_kelime, eski_karsilik)
);

-- TDK'deki belirtme hâli ipucu: "harp, -bi" -> "bi", "ilim, -lmi" -> "lmi".
-- Ünsüz yumuşaması, ünlü düşmesi ve ünlü uyumu istisnalarını gösterir; düzenli kelimelerde boştur.
ALTER TABLE glossary ADD COLUMN IF NOT EXISTS tdk_ek text;

CREATE INDEX IF NOT EXISTS glossary_modern_kelime_idx ON glossary (lower(modern_kelime));
