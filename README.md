CEFR Subtitle Masker (Altyazı Sansürleme Aracı)
İngilizce dizi ve film izlerken gözün istemsizce altyazıya kaymasını engelleyen, beyni dinlemeye ve duymaya zorlayan masaüstü aracı. Ekrandaki altyazı alanını gerçek zamanlı tarar, CEFR (A1–C2) seviye veri tabanıyla eşleştirir ve kullanıcının seçtiği seviyenin altında kalan (zaten bildiği) kelimelerin üzerine şeffaf bir Win32 katmanı ile dijital sansür bandı çeker.

Nasıl Çalışır?
Bölge Yakalama: Ekranın alt %20'lik altyazı koordinatları gerçek zamanlı taranır.

OCR İşleme: Görüntüdeki metin ve piksel koordinatları OCR motoru üzerinden parse edilir.

CEFR Sözlük Filtresi: Ayrıştırılan kelimeler, Kaggle CEFR veri setindeki seviyelerle karşılaştırılır.

Şeffaf Katman (Overlay): Win32 API kullanılarak hedeflenen kelimelerin tam koordinatlarına tıkalamayı engellemeyen (click-through) siyah maskeler bindirilir.

Temel Özellikler
Dinamik CEFR Slider'ı: Maskeleme seviyesini anlık olarak A1'den C2'ye kadar ayarlayabilme.

DRM Aşımı (Donanım/Ekran Seviyesi): Ekran kaydı korumalı platformlarda (Prime Video, Exxen vb.) kayıt siyah çıksa dahi doğrudan ekran pikselleri üzerinden çalışabilme.

Düşük Kaynak Tüketimi: GPU/CPU yükünü minimumda tutmak için optimize edilmiş polling rate (tarama sıklığı).

Kurulum ve Çalıştırma
Gereksinimler
.NET 8.0 SDK / Visual Studio 2022

Windows 10/11 (Win32 Overlay desteği için)

Adımlar
Depoyu klonlayın:

Bash
git clone https://github.com/Niyazicancinkir/movie-subtitle-masker.git
cd cefr-subtitle-masker
Bağımlılıkları yükleyin:

Bash
dotnet restore
Uygulamayı derleyin ve çalıştırın:

Bash
dotnet run --configuration Release
Veri Seti & Kaynak
Kelimelerin dil seviyesi sınıflandırması Kaggle üzerindeki açık kaynaklı CEFR veri setine dayanmaktadır:

[Kaggle CEFR English Vocabulary Dataset
](https://www.kaggle.com/datasets/nezahatkk/10-000-english-words-cerf-labelled/data)
Geliştirici Notu: Veri setinde aynı kelimenin farklı anlamları nedeniyle birden fazla seviyede tanımlanmış olabileceğini göz önünde bulundurun (Örn: blue kelimesinin hem renk [A1] hem de melankolik/hüzünlü [mecazi] bağlamda bulunması).

Lisans
Bu proje MIT lisansı altındadır. İnceleyin, kurcalayın, kırın ve geliştirin.
