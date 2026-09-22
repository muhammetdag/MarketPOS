# 🛒 Market POS - Hızlı Satış ve Stok Yönetim Sistemi

Bakkal, market ve süpermarketler için özel olarak geliştirilmiş; **BİM ve A101 tarzı** ergonomiye sahip, **Windows 7 ve 1 GB RAM'li** eski kasalarda dahi sıfır takılma ile çalışan profesyonel masaüstü POS (Hızlı Satış) sistemi.

---

## 🚀 Öne Çıkan Özellikler

- **Düşük Donanım & Yüksek Performans:** 
  - Electron veya ağır tarayıcı motorları kullanılmamıştır.
  - Çalışırken yalnızca **~25 MB - 40 MB RAM** tüketir.
  - Windows 7, 8, 10 ve 11 ile %100 uyumludur.
- **Sıfır Kurulumlu Gömülü SQLite:**
  - SQL Server / MySQL gibi harici sunucu kurulumu gerektirmez.
  - Veriler tek bir dosya (`market_pos.db`) içinde saklanır.
  - Otomatik yedekleme desteği mevcuttur.
- **8'den 80'e Herkes İçin Sade ve Büyük Arayüz:**
  - Gözü yormayan yüksek kontrastlı renkler ve büyük puntolar.
  - Devasa **GENEL TOPLAM** ve **PARA ÜSTÜ** göstergesi.
- **Fareye Dokunmadan Satış:**
  - Tüm satış süreci sadece barkod okuyucu, klavye (Numpad, Enter) ve kısayol tuşları (F1-F12) ile tamamlanabilir.

---

## ⌨️ Hızlı Kısayol Tuşları

| Tuş | Fonksiyon | Açıklama |
|---|---|---|
| **Enter** | Sepete Ekle / Onayla | Barkodu listeye ekler veya ödemeyi tamamlar. |
| **5\*Barkod** | Çarpanlı Satış | Örn: `5*869...` yazıldığında aynı üründen 5 adet ekler. |
| **F1** | Barkod Odaklan | İmleci hemen barkod okutma kutusuna alır. |
| **F2** | Ürün & Stok Girişi | Yeni ürün kaydetme ve stok artırma ekranını açar. |
| **F3** | Gün Sonu (Z Raporu) | Günlük ciroyu, nakit/kart dağılımını ve fişleri gösterir. |
| **F4** | Ayarlar & Yedekleme | Market adı, fiş başlığı ve tek tıkla veritabanı yedeği. |
| **F12** | Nakit Satış | Hızlı para butonları (50, 100, 200 TL) ile para üstü hesaplar. |
| **F11** | Kredi Kartı Satış | Tek tuşla kartlı satışı tamamlar ve kasayı kapatır. |
| **DEL** | Seçiliyi Sil | Sepette seçili olan ürünü çıkarır. |
| **+ / -** | Miktar Değiştir | Sepetteki ürünün adedini 1 artırır veya azaltır. |
| **ESC** | Sepeti Sıfırla | Sepetteki tüm ürünleri temizler. |

---

## 📦 İlk Ürün ve Stok Girişi Nasıl Yapılır?

1. Klavyeden **F2** tuşuna basın veya üst menüden **[Ürün & Stok (F2)]** butonuna tıklayın.
2. Barkod okuyucu ile ürünün barkodunu okutun:
   - **Ürün sistemde yoksa:** Ürün adı, satış fiyatı, alış fiyatı ve dükkandaki ilk stok adedini girip **F2 - KAYDET** butonuna basın.
   - **Ürün sistemde zaten varsa:** Ürünün bilgileri ekrana gelir, isterseniz mevcut stoğa ilave yapabilirsiniz.
3. Barkodu olmayan ürünler (ekmek, açık sebze-meyve vb.) için **[Otomatik Üret]** butonuna basarak sistemden seri barkod üretebilir ve **"Hızlı Ürünler Listesine Ekle"** seçeneğini işaretleyebilirsiniz.

---

## 💻 Uygulamayı Çalıştırma

### 1. Hazır Derlenmiş Sürümü Çalıştırma:
`publish` klasörü içerisindeki `MarketPOS.exe` dosyasını çift tıklayarak doğrudan çalıştırabilirsiniz.

### 2. Geliştirici Modunda Çalıştırma:
```powershell
dotnet run --project src/MarketPOS/MarketPOS.csproj
```

### 3. Testleri Çalıştırma:
```powershell
dotnet test tests/MarketPOS.Tests/MarketPOS.Tests.csproj
```
