# Mobile Provider API & Gateway Projesi

## Proje Yapısı

Bu proje iki ana bileşenden oluşmaktadır:

1. **MobileProviderApi**: Ana API servisi (Port: 5096)
2. **MobileProviderGateway**: API Gateway servisi (Port: 5098)

## Özellikler

- ✅ JWT Authentication
- ✅ **Custom API Gateway** 
- ✅ **Rate Limiting** (API Gateway'de - Custom Implementation)
- ✅ SQL Server Database
- ✅ Swagger Documentation
- ✅ Entity Framework Core

## Rate Limiting

Rate limiting **Custom API Gateway'de**  

### QueryBill Endpoint
- **Limit**: 3 istek
- **Period**: 1 gün (86400 saniye)
- **Client Identification**: SubscriberNo (JWT token'dan almak için ayarladım)

### Diğer Endpoint'ler
- **Limit**: 5 istek
- **Period**: 1 saniye
- **Client Identification**: IP Address

## Veritabanı

SQL Server kullanılmaktadır. Connection string `appsettings.json` dosyasında yapılandırılmıştır:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MobileProviderDB;Trusted_Connection=True;TrustServerCertificate=True"
}
```

### Veritabanı Oluşturma

1. SQL Server LocalDB'nin yüklü olduğundan emin olun
2. Migration'ları çalıştırın:
   ```bash
   cd MobileProviderApi
   dotnet ef database update
   ```

## Çalıştırma

### 1. API Servisini Başlatma
```bash
cd MobileProviderApi
dotnet run
```
API: http://localhost:5096
Swagger: http://localhost:5096/swagger

### 2. Gateway Servisini Başlatma
```bash
cd MobileProviderGateway
dotnet run
```
Gateway: http://localhost:5098

## Test Kullanıcı Bilgileri

- **Username**: testUser
- **Password**: password

## API Endpoint'leri

### Authentication
- `POST /api/v1/Auth/login` - JWT token almak için

### Bills
- `GET /api/v1/Bills/QueryBill/{subscriberNo}?month=YYYY-MM` - Fatura sorgulama (Auth gerekli, Rate Limit: 3/gün)
- `GET /api/v1/Bills/QueryBillDetailed/{subscriberNo}?month=YYYY-MM` - Detaylı fatura sorgulama (Auth gerekli)
- `GET /api/v1/Bills/BankingAppQueryBill/{subscriberNo}` - Ödenmemiş faturalar (Auth gerekli)
- `POST /api/v1/Bills/PayBill` - Fatura ödeme (Auth gerekmez)
- `POST /api/v1/Bills/Admin/AddBill` - Fatura ekleme (Auth gerekli)

## Örnek Veriler

Proje başlatıldığında otomatik olarak örnek veriler eklenir:

- **SubscriberNo 1** (Ahmet Yılmaz): 3 ödenmemiş fatura
- **SubscriberNo 2** (Ayşe Demir): 1 ödenmiş, 1 kısmen ödenmiş, 1 ödenmemiş fatura
- **SubscriberNo 3** (Mehmet Kaya): 2 ödenmiş, 1 ödenmemiş fatura

## Teknolojiler

- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- **Custom API Gateway** (Kendi yazdığımız middleware - Ocelot kullanılmadı)
- JWT Authentication
- Swagger/OpenAPI

## Custom Gateway Yapısı

Gateway iki ana middleware'den oluşur:

1. **RateLimitingMiddleware**: Rate limiting kontrolü yapar
   - QueryBill için: SubscriberNo bazlı, 3 istek/gün
   - Diğer endpoint'ler için: IP bazlı, 5 istek/saniye

2. **ApiGatewayMiddleware**: İstekleri downstream API'ye yönlendirir
   - Tüm `/api/v1/*` isteklerini `http://localhost:5096`'ya forward eder
   - Headers ve body'yi korur

## Notlar

- **Rate limiting Custom API Gateway'de (kendi yazdığımız middleware) uygulanmıştır**
- Ocelot kullanılmamıştır - tamamen custom implementation
- JWT token'lar 1 saat geçerlidir
- Veritabanı otomatik olarak oluşturulur ve örnek veriler eklenir (Development ortamında)

