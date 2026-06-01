# Session 01 - Infrastructure and Solution Setup

## 1. Session Hedefi

Bu session'ın amacı MiniCommerceV2 projesinin temel iskeletini ve local geliştirme altyapısını hazırlamaktır.

Bu aşamada business logic yazılmadı. Öncelik, tüm ekibin aynı başlangıç noktasından ilerleyebilmesini sağlayacak tekrarlanabilir bir proje yapısı kurmaktır.

## 2. Ön Koşullar

Geliştirme ortamı:

- WSL2 Ubuntu
- .NET 9 SDK
- Docker / Docker Compose
- Git
- VS Code

Doğrulama komutları:

```bash
pwd
dotnet --version
dotnet --list-sdks
docker ps
```

Beklenen durum:

- .NET 9 SDK yüklü olmalı.
- Docker çalışıyor olmalı.
- Çalışma dizini `/home/hmztpl/projects` altında olmalı.

## 3. Oluşturulan Proje Yapısı

Solution adı:

```text
MiniCommerceV2
```

Servis projeleri:

```text
src/services/MiniCommerce.Identity.API
src/services/MiniCommerce.Catalog.API
src/services/MiniCommerce.Basket.API
src/services/MiniCommerce.Ordering.API
src/services/MiniCommerce.Gateway.API
```

Shared proje:

```text
src/shared/MiniCommerce.Shared
```

Ek klasörler:

```text
docs/notes
postman
```

## 4. Kullanılan Ana Komutlar

Solution oluşturma:

```bash
dotnet new sln -n MiniCommerceV2
```

API projeleri:

```bash
dotnet new web -n MiniCommerce.Identity.API -o src/services/MiniCommerce.Identity.API
dotnet new web -n MiniCommerce.Catalog.API -o src/services/MiniCommerce.Catalog.API
dotnet new web -n MiniCommerce.Basket.API -o src/services/MiniCommerce.Basket.API
dotnet new web -n MiniCommerce.Ordering.API -o src/services/MiniCommerce.Ordering.API
dotnet new web -n MiniCommerce.Gateway.API -o src/services/MiniCommerce.Gateway.API
```

Shared library:

```bash
dotnet new classlib -n MiniCommerce.Shared -o src/shared/MiniCommerce.Shared
```

Projeleri solution'a ekleme:

```bash
dotnet sln add src/services/MiniCommerce.Identity.API/MiniCommerce.Identity.API.csproj
dotnet sln add src/services/MiniCommerce.Catalog.API/MiniCommerce.Catalog.API.csproj
dotnet sln add src/services/MiniCommerce.Basket.API/MiniCommerce.Basket.API.csproj
dotnet sln add src/services/MiniCommerce.Ordering.API/MiniCommerce.Ordering.API.csproj
dotnet sln add src/services/MiniCommerce.Gateway.API/MiniCommerce.Gateway.API.csproj
dotnet sln add src/shared/MiniCommerce.Shared/MiniCommerce.Shared.csproj
```

Git başlangıcı:

```bash
git init
dotnet new gitignore
```

## 5. Central Package Management

Kökte şu dosya oluşturuldu:

```text
Directory.Packages.props
```

Amaç:

- NuGet paket versiyonlarını merkezi yönetmek.
- Servisler arasında paket versiyonu dağınıklığını engellemek.
- Özellikle MassTransit paketlerini `8.4.1` olarak sabitlemek.

Önemli not:

`Directory.Packages.props` sadece versiyonları tanımlar. Paketleri projelere otomatik eklemez.

Bir paketin gerçekten kullanılabilmesi için ilgili `.csproj` dosyasında `PackageReference` bulunmalıdır.

Örnek:

```bash
dotnet add src/services/MiniCommerce.Identity.API/MiniCommerce.Identity.API.csproj package Microsoft.EntityFrameworkCore
```

Versiyon yazılmaz. Versiyon kökteki `Directory.Packages.props` içinden gelir.

## 6. Docker Compose Altyapısı

Bu session'da API servisleri Dockerize edilmedi.

Sadece altyapı servisleri Docker Compose ile ayağa kaldırıldı:

```text
RabbitMQ
MongoDB
Redis
PostgreSQL Identity DB
PostgreSQL Ordering DB
```

Container bilgileri:

| Servis | Container | Host Port |
|---|---|---|
| RabbitMQ AMQP | minicommv2.rabbitmq | 5673 |
| RabbitMQ Management UI | minicommv2.rabbitmq | 15673 |
| MongoDB | minicommv2.catalogdb | 27031 |
| Redis | minicommv2.basketdb | 6380 |
| PostgreSQL Identity | minicommv2.identitydb | 5434 |
| PostgreSQL Ordering | minicommv2.orderdb | 5435 |

RabbitMQ Management UI:

```text
http://localhost:15673
```

Credentials:

```text
guest / guest
```

## 7. Neden İki Ayrı PostgreSQL Container?

Identity ve Ordering servisleri için iki ayrı PostgreSQL container kullanıldı:

```text
minicommv2.identitydb
minicommv2.orderdb
```

Bunun sebebi `database per service` prensibini daha görünür hale getirmektir.

Gerçek mikroservis mimarisinde her servis kendi verisinin sahibidir. Başka servislerin tablolarına doğrudan erişmez.

## 8. Just-in-Time Artifact Kararı

Başlangıçta tüm shared message contract'larını Session 1'de üretme fikri vardı.

Ancak eğitim hedefi nedeniyle bu yaklaşım revize edildi.

Yeni karar:

```text
Artifact, ilk gerçek ihtiyacı doğduğu anda üretilir.
```

Bu yüzden Session 1'de aşağıdaki contract'lar henüz oluşturulmadı:

```text
SubmitOrderCommand
ReserveStockCommand
StockReservedEvent
StockReservationFailedEvent
OrderCreatedEvent
OrderRejectedEvent
OrderItemDto / OrderItemSnapshot
```

Neden?

Henüz bu mesajları publish eden veya consume eden gerçek servis davranışı yok.

Junior geliştiriciler için daha anlaşılır olması adına her command/event, onu kullanan servis davranışı ortaya çıktığında eklenecek.

## 9. Ne Yaptık?

- MiniCommerceV2 solution oluşturuldu.
- API servis iskeletleri oluşturuldu.
- Shared library oluşturuldu.
- Git repository başlatıldı.
- Central Package Management eklendi.
- Docker Compose altyapısı hazırlandı.
- RabbitMQ, MongoDB, Redis ve PostgreSQL container'ları çalıştırıldı.
- Tüm servislerin build aldığı doğrulandı.

## 10. Neden Yaptık?

Mikroservis geliştirmeye başlamadan önce tekrar üretilebilir bir local development ortamına ihtiyaç vardır.

Bu yapı sayesinde her geliştirici aynı altyapıyı aynı portlar ve aynı container isimleriyle ayağa kaldırabilir.

## 11. Ne İşe Yaradı?

Artık sonraki session'larda servis geliştirmeye geçebiliriz.

Örneğin Identity.API geliştirirken PostgreSQL hazır olacak. Catalog.API geliştirirken MongoDB hazır olacak. Basket.API geliştirirken Redis hazır olacak. Messaging aşamasına gelindiğinde RabbitMQ hazır olacak.

## 12. Karşılaşılabilecek Hatalar

### Directory.Packages.props XML hatası

Hata:

```text
MSB4024: The imported project file could not be loaded.
Unexpected end of file has occurred.
```

Sebep:

XML dosyasında kapanmayan tag vardır.

Çözüm:

`Directory.Packages.props` dosyasının sonunda şu tag'lerin olduğundan emin olun:

```xml
</ItemGroup>
</Project>
```

### Container health: starting

İlk açılışta normaldir.

Kontrol:

```bash
docker compose ps
```

Birkaç saniye sonra `healthy` olması beklenir.

### Port çakışması

Eğer local makinede aynı portları kullanan başka servisler varsa Docker container başlayamayabilir.

Kullanılan host portları:

```text
5673
15673
27031
6380
5434
5435
```

## 13. Session Sonu Doğrulama Checklist

Aşağıdaki komutlar başarılı çalışmalıdır:

```bash
dotnet build
docker compose ps
docker exec minicommv2.identitydb pg_isready -U admin -d identitydb
docker exec minicommv2.orderdb pg_isready -U admin -d orderdb
docker exec minicommv2.basketdb redis-cli ping
docker exec minicommv2.catalogdb mongosh --eval "db.adminCommand('ping')"
docker exec minicommv2.rabbitmq rabbitmq-diagnostics ping
```

Beklenen sonuçlar:

- `dotnet build` başarılı olmalı.
- Tüm container'lar `healthy` olmalı.
- Redis `PONG` dönmeli.
- MongoDB `{ ok: 1 }` dönmeli.
- PostgreSQL `accepting connections` dönmeli.
- RabbitMQ `Ping succeeded` dönmeli.

## 14. Git Checkpoint

Önerilen commit mesajları:

```bash
git add .
git commit -m "docs: add session 01 notes"
git push
```

Session tag:

```bash
git tag -a session-01-complete -m "Session 01 complete"
git push origin session-01-complete
```

## 15. Sonraki Session Önizlemesi

Session 2'de Identity.API geliştirilecek.

Kapsam:

- PostgreSQL bağlantısı
- EF Core DbContext
- User entity
- Register endpoint
- Login endpoint
- PasswordHasher kullanımı
- JWT üretimi
- Authenticated `/api/users/me` endpoint'i
- Migration
- Health endpoint
- Postman collection güncellemesi
