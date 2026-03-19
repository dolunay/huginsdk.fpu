## FP300Service

FP300Service, FP300 cihazlarıyla entegrasyon sağlamak amacıyla geliştirilmiş WPF NetCore tabanlı bir test ve operasyon çözümüdür.

### Geliştirme ortamı

- Proje `Microsoft Visual Studio 2026` ile geliştirilmiştir.
- Versiyona ait DLL dosyaları `etc` klasörü altında bulunmaktadır.
- T300 entegrasyonu testlerinde derleme hatası almamak için, destek ekibimizden alacağınız `Certificates` klasörünü `fpu\CSharp\NetCore\FP300Service` konumuna atmanız gerekmektedir.

## Genel ekran yapısı

Uygulama açıldığında ekran üç ana bölümden oluşur:

1. Üst bölümde bağlantı alanı bulunur.
   - `TCP/IP` ve `SERIAL PORT` bağlantı seçenekleri yer alır.
   - `Fiscal ID` bilgisi girilir.
   - `CONNECT` butonu ile cihaza bağlantı sağlanır.
2. Sol tarafta ekranlar arası geçiş sağlayan menü bulunur.
3. Sağ tarafta seçilen ekranın içeriği, en sağda ise işlem log alanı gösterilir.

## Ekran bazlı kullanım

### 1. Status Check / Utility ekranı

Bu ekran günlük temel operasyonlar ve hızlı test işlemleri için kullanılır.

İçerdiği başlıca alanlar:

- `Fiscal Operations`
  - `START FM`
- `Status Info`
  - `CHECK STATUS`
  - `LAST RESPONSE`
  - `INTERRUPT PROCESS`
- `Nonfiscal Rcpt.`
  - `START NF RECEIPT`
  - `PRINT SAMPLE CONTEXT`
  - `CLOSE NF RECEIPT`
- `Cashier Options`
  - kasiyer numarası ve şifre ile `LOGIN`
- `Drawer Options`
  - tutar girilerek `CASH IN` ve `CASH OUT`
- `Keypad Options`
  - `LOCK KEYS` ve `UNLOCK KEYS`

Bu ekran, cihazın hazır olup olmadığını kontrol etmek ve temel çevresel işlemleri hızlıca denemek için başlangıç noktasıdır.

Örnek kullanım akışı:

1. `CHECK STATUS` ile cihazın erişilebilir olduğunu doğrulayın.
2. Gerekirse `LAST RESPONSE` ile son cihaz cevabını inceleyin.
3. Kasiyer işlemi test edilecekse `LOGIN` ile oturum açın.
4. Çekmece hareketi test edilecekse `CASH IN` veya `CASH OUT` işlemini çalıştırın.

### 2. Cash Register Info ekranı

Bu ekran cihazdan mali bilgi ve durum verisi okumak için kullanılır.

Başlıca işlemler:

- `Son Z Bilgisi`
- `Son Fiş Bilgisi`
- `Çekmece Bilgileri`
- `EKÜ Limit Ayarla`
- `Mali Uygulama Versiyonu`
- `Kütüphane Versiyonu`
- `Günlük Özet`

Özellikle cihazın son işlem durumu, sürüm bilgileri ve EKÜ limiti gibi operasyonel kontroller bu ekrandan yürütülür.

Örnek kullanım akışı:

1. `Son Z Bilgisi` ile son kapanış bilgisini alın.
2. `Son Fiş Bilgisi` ile en son belgeyi doğrulayın.
3. Gerekirse `Mali Uygulama Versiyonu` ve `Kütüphane Versiyonu` alanlarından ortam kontrolü yapın.
4. EKÜ kapasitesi kontrol edilecekse `EKÜ Limit Ayarla` bölümünü kullanın.

### 3. Program ekranı

Bu ekran cihaz üzerindeki tanım ve parametre yönetimi için kullanılır. Birden fazla sekmeden oluşur:

- `DEPARTMENT`
  - departman tanımı alma ve kaydetme
- `PLU`
  - PLU kartları için listeleme ve kayıt
- `CREDIT`
  - döviz ve kredi tanımlarının yönetimi
- `CATEGORY`
  - ana kategori tanımları
- `CASHIER`
  - kasiyer tanımları
- `PROGRAM OPTIONS`
  - cihaz davranışına ilişkin program seçenekleri
- `GRAPHIC LOGO`
  - grafik logo işlemleri
- `NETWORK SETTINGS`
  - ağ parametreleri
- `LOGO`
  - metinsel veya baskı logo alanları
- `VAT`
  - KDV oran tanımları
- `END OF RECEIPT NOTE`
  - fiş sonu not alanları
- `SEND PRODUCT`
  - ürün verisi gönderim işlemleri

Program ekranı, cihazın satış ve belge üretim davranışını belirleyen temel konfigürasyon alanıdır.

Örnek kullanım akışı:

1. Önce `DEPARTMENT` ve `PLU` tanımlarını hazırlayın.
2. Gerekliyse `CREDIT`, `VAT` ve `CATEGORY` alanlarını güncelleyin.
3. Logo veya fiş sonu notu kullanılacaksa `GRAPHIC LOGO`, `LOGO` ve `END OF RECEIPT NOTE` sekmelerini düzenleyin.
4. Ağ bağlantısı gerekiyorsa `NETWORK SETTINGS` üzerinden parametreleri kontrol edin.

### 4. Reports ekranı

Bu ekran mali ve operasyonel raporların alınması için kullanılır. Rapor çıktısı hem içerik olarak görüntülenebilir hem de baskıya yönlendirilebilir.

Üst alandaki seçenekler:

- `CONTEXT`
- `PRINT`

Sekmeler:

- `X REPORTS`
  - X raporu, PLU raporu, sistem bilgi raporu, toplam fiş raporu
- `Z REPORTS`
  - Z raporu ve gün sonu raporu
- `FM REPORTS`
  - mali hafıza raporları, Z aralığına veya tarihe göre
- `EJ SINGLE REPORT`
  - tek belge veya Z kopyası bazlı EKÜ raporları
- `EJ PERIODIC`
  - dönemsel EKÜ raporları
- `OTHER DOCS`
  - diğer belge türlerine ait raporlar

Sağ panelde alınan raporun içeriği metin olarak görüntülenir.

Örnek kullanım akışı:

1. Raporun sadece ekranda gösterilmesi isteniyorsa `CONTEXT`, yazdırılması isteniyorsa `PRINT` seçimini yapın.
2. İlgili sekmeden rapor türünü seçin.
3. Tarih, Z numarası veya belge aralığı gibi parametreleri girin.
4. `GET REPORT` ile çıktıyı alın ve sonucu sağ panelden kontrol edin.

### 5. Sale ekranı

Bu ekran satış, belge başlatma, ödeme ve belge kapanış süreçlerinin test edilmesi için hazırlanmıştır. Birkaç ana bölümden oluşur.

Belge başlatma sekmeleri:

- `RECEIPT`
- `INVOICE TYPES`
- `ADVANCE`
- `OTOPARK`
- `FOOD`
- `COLLECTION INV`
- `CUURENT ACCOUNT COLLECTION DOC`
- `E-BELGE`
- `DATA TEST`
- `RETURN DOC`

Satış işlem sekmeleri:

- `SALE`
- `VOID SALE`
- `ADJUSTMENT`
- `DEPT SALE`

Ödeme sekmeleri:

- `CASH PAYMENTS`
- `EFT PAYMENT`
- `VOID PAYMENT`
- `EFT REFUND`
- `BANK LIST`
- `EFT SLIP COPY`
- `EXTERNAL SLIP`

Ek belge alanları:

- `FOOTER NOTES`
- `FOOTER NOTE EXTRA`
- `BARCODE`
- `STOPPAGE`

Bu ekran, satış senaryolarının uçtan uca test edilmesi için en kapsamlı işlem alanıdır.

Örnek kullanım akışı:

1. Belge türünü üst bölümden seçin ve `START DOCUMENT` ile süreci başlatın.
2. Orta bölümde satış, iptal veya düzeltme satırlarını ekleyin.
3. Ödeme tipini ilgili sekmeden seçerek tahsilatı tamamlayın.
4. Gerekliyse alt bölümlerde dip not, barkod veya stopaj bilgilerini ekleyin.

### 6. Service ekranı

Bu ekran servis modunda çalışan bakım, cihaz ayarı ve test işlemlerini içerir.

`SERVICE OPERATIONS` sekmesindeki başlıca işlemler:

- `ORDER CODE`
- `ENTER SERVICE MODE` / `EXIT SERVICE MODE`
- `SET DATE/TIME`
- `SET EXT DEVICE SETTINGS`
- `UPDATE FIRMWARE`
- `FISCAL MODE NOW`
- `FILE TRANSFER`
- `PRINT LOGS`
- `CLOSE FM`
- `CREATE SALE DB`
- `START FM TEST`
- `FORMAT DAILY MEMORY`
- `INITIALIZE EJ`
- `FACTORY SETTINGS`

`TEST COMMANDS` sekmesinde ise GMP test işlemleri yer alır:

- `Test`
- `GMP Port`
- IP ve port parametreleri ile test komutu çalıştırma

Bu ekran, servis ekipleri ve ileri seviye teknik doğrulama işlemleri için kullanılmaktadır.

Örnek kullanım akışı:

1. Cihaz servis moduna alınacaksa `ORDER CODE` ve şifre bilgileriyle `ENTER SERVICE MODE` çalıştırılır.
2. Tarih/saat, harici cihaz veya firmware ayarları ilgili alandan güncellenir.
3. Teknik doğrulama gerekiyorsa `PRINT LOGS` veya `TEST COMMANDS` sekmesi kullanılır.
4. İşlem tamamlandığında gerekiyorsa servis modundan çıkış yapılır.

## Notlar

- Bağlantı kurulmadan önce doğru haberleşme tipi seçilmelidir.
- Log paneli üzerinden tüm işlem sonuçları takip edilebilir.
- Servis ve program ekranlarındaki birçok işlem cihaz üzerinde kalıcı değişiklik yapabilir; test ortamında dikkatli kullanılmalıdır.

## Hızlı başlangıç önerisi

Uygulamayı ilk kez kullanan ekipler için önerilen sıra aşağıdaki gibidir:

1. Bağlantı tipini seçin ve `CONNECT` ile cihaza bağlanın.
2. `Status Check / Utility` ekranında temel erişim kontrolünü yapın.
3. `Cash Register Info` ekranında cihaz bilgilerini doğrulayın.
4. Gerekirse `Program` ekranından tanımları yükleyin.
5. `Sale` ekranında örnek belge ve ödeme akışını test edin.
6. `Reports` ekranında rapor çıktısını doğrulayın.
7. Teknik servis işlemleri gerekiyorsa en son `Service` ekranını kullanın.
