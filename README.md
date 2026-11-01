# [Proje Adı]

Günümüz Türkçesiyle yazdığınız metni, geç Osmanlı ve erken Cumhuriyet
dönemi Türkçesine (Latin harfleriyle) çeviren bir web uygulaması.

"Bu cümle 1925'te nasıl yazılırdı?" sorusunun cevabı.

> Proje geliştirme aşamasındadır.

## Örnekler

| Günümüz Türkçesi | Dönem Türkçesi |
|---|---|
| Öğretmen, öğrencilere bilimin önemini anlattı. | Muallim, talebeye ilmin ehemmiyetini anlattı. |
| Gelecekte ekonomi gelişecek. | İstikbalde iktisat inkişaf edecek. |
| Bağımsızlık ve özgürlük için toplum birlikte çalıştı. | İstiklal ve hürriyet için cemiyet müştereken çalıştı. |

## Özellikler

- Günümüz Türkçesinden dönem Türkçesine çeviri
- Dönem yoğunluğu ayarı (hafif / orta / ağır)
- Değişen kelimelerin vurgulanması ve anlamlarının gösterilmesi
- Çeviriyi görsel olarak paylaşma (planlanıyor)
- Telefona yüklenebilir uygulama (PWA, planlanıyor)

## Nasıl Çalışır?

1. Metin cümlelere ve kelimelere ayrılır.
2. Kelimeler, dönem karşılıklarını içeren bir sözlükte aranır.
3. Bulunan karşılıklar ipucu olarak yapay zekâ modeline verilir,
   model cümleyi dönem üslubuyla yeniden yazar.
4. Değişen kelimeler listelenir ve arayüzde vurgulanır.

## Teknolojiler

- **Backend:** .NET 9 Web API
- **Veritabanı:** PostgreSQL (Neon)
- **Frontend:** React, TypeScript
- **Yapay zekâ:** LLM API
- **Altyapı:** Docker, GitHub Actions

## Çalıştırma

Repo kökünde `.env.local` dosyasına `DATABASE_URL` ekleyin, sonra:

```bash
dotnet run --project src/Ceviri.Api --launch-profile http
dotnet test
```

API açılışta sözlük tablosunu oluşturur ve `data/glossary.csv` dosyasını yükler.

| Uç nokta | Açıklama |
|---|---|
| `POST /api/translate` | `{ "text": "...", "seviye": 2 }` → çeviri ve değişen kelimeler |
| `GET /api/glossary?q=okul&seviye=3` | Sözlükte arama |
| `GET /api/glossary/health` | Veritabanı bağlantı kontrolü |

Seviye: 1 = hafif, 2 = orta, 3 = ağır.

## Yol Haritası

- [x] Sözlük verisi ve veritabanı
- [x] Sözlük tabanlı ilk çeviri
- [ ] Yapay zekâ katmanı
- [ ] Web arayüzü ve PWA
- [ ] Test seti ve doğruluk ölçümü
- [ ] Canlı yayın

## Geri Bildirim

Yanlış bir karşılık mı gördünüz? Lütfen bir
[issue](../../issues) açarak bildirin.

## Lisans

[Lisans seçilecek]

---

### English summary

A web app that translates modern Turkish text into late Ottoman /
early Republic-era Turkish (Latin script), combining a curated
glossary with an LLM. Built with .NET 8, React and PostgreSQL.
