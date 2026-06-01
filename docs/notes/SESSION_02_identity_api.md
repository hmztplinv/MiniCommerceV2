# Session 02 - Identity.API

## 1. Session Hedefi

Bu session'ın amacı MiniCommerce.Identity.API servisini geliştirmektir.

Bu session sonunda Identity.API şu yeteneklere sahip oldu:

- PostgreSQL bağlantısı
- EF Core DbContext
- User entity
- Register endpoint
- PasswordHasher ile güvenli password hashleme
- Login endpoint
- JWT token üretimi
- JWT validation
- Authenticated `/api/users/me` endpoint'i
- Initial EF Core migration
- Health endpoint
- Postman collection güncellemesi

## 2. Ön Koşullar

Session 1 tamamlanmış olmalıdır.

Gerekli altyapı container'ları çalışıyor olmalıdır:

```bash
docker compose ps
```

Identity PostgreSQL kontrolü:

```bash
docker exec minicommv2.identitydb pg_isready -U admin -d identitydb
```

Beklenen çıktı:

```text
/var/run/postgresql:5432 - accepting connections
```

## 3. Eklenen NuGet Paketleri

Identity.API projesine şu paketler eklendi:

```text
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.Design
Microsoft.EntityFrameworkCore.Tools
Npgsql.EntityFrameworkCore.PostgreSQL
Microsoft.AspNetCore.Authentication.JwtBearer
System.IdentityModel.Tokens.Jwt
```

Paket versiyonları kökteki `Directory.Packages.props` üzerinden merkezi olarak yönetiliyor.

Önemli karar:

```text
Paket versiyonları csproj dosyalarına tek tek yazılmadı.
```

## 4. EF CLI Local Tool

Global `dotnet ef` sürümü EF Core 10 olduğu için projeye local tool olarak `dotnet-ef` 9.0.0 eklendi.

Kontrol:

```bash
dotnet tool run dotnet-ef --version
```

Beklenen:

```text
9.0.0
```

Neden?

Projede EF Core paketleri 9.0.0 olduğu için migration tool'unun da aynı major/minor çizgide olması daha güvenlidir.

## 5. Identity Config

`appsettings.json` içine Identity PostgreSQL connection string eklendi:

```json
{
  "ConnectionStrings": {
    "IdentityDb": "Host=localhost;Port=5434;Database=identitydb;Username=admin;Password=Password123"
  }
}
```

JWT ayarları da config üzerinden yönetildi:

```json
{
  "Jwt": {
    "Issuer": "MiniCommerceV2.Identity",
    "Audience": "MiniCommerceV2.Clients",
    "SecretKey": "MiniCommerceV2_Local_Development_Secret_Key_12345",
    "ExpirationMinutes": 60
  }
}
```

Not:

Local development için secret key appsettings içinde tutuldu. Docker/production yaklaşımında environment variable veya secret manager kullanılmalıdır.

## 6. User Entity

Eklenen entity:

```text
Entities/User.cs
```

Alanlar:

```text
Id
Email
PasswordHash
FullName
CreatedAt
```

Önemli kararlar:

- Password plain text saklanmadı.
- Email normalize edildi.
- Entity içinde controlled creation yaklaşımı kullanıldı.
- Password hash sonradan `SetPasswordHash` metodu ile set edildi.

## 7. IdentityDbContext

Eklenen DbContext:

```text
Persistence/IdentityDbContext.cs
```

Tablo adı:

```text
users
```

Kolonlar:

```text
id
email
password_hash
full_name
created_at
```

Email alanına unique index eklendi:

```text
IX_users_email
```

Neden?

Aynı email ile birden fazla kullanıcı oluşturulmasını engellemek için hem uygulama tarafında kontrol yaptık hem de database seviyesinde unique constraint tanımladık.


## 8. Migration

Initial migration oluşturuldu:

```bash
dotnet tool run dotnet-ef migrations add InitialCreate \
  --project src/services/MiniCommerce.Identity.API/MiniCommerce.Identity.API.csproj \
  --startup-project src/services/MiniCommerce.Identity.API/MiniCommerce.Identity.API.csproj \
  --context IdentityDbContext \
  --output-dir Persistence/Migrations
```

Database'e uygulandı:

```bash
dotnet tool run dotnet-ef database update \
  --project src/services/MiniCommerce.Identity.API/MiniCommerce.Identity.API.csproj \
  --startup-project src/services/MiniCommerce.Identity.API/MiniCommerce.Identity.API.csproj \
  --context IdentityDbContext
```

Doğrulama:

```bash
docker exec minicommv2.identitydb psql -U admin -d identitydb -c "\dt"
docker exec minicommv2.identitydb psql -U admin -d identitydb -c "\d users"
docker exec minicommv2.identitydb psql -U admin -d identitydb -c "SELECT * FROM \"__EFMigrationsHistory\";"
```

Beklenen tablolar:

```text
users
__EFMigrationsHistory
```

## 9. Register Endpoint

Endpoint:

```text
POST /api/auth/register
```

Request:

```json
{
  "email": "hamza@example.com",
  "password": "Password123",
  "fullName": "Hamza Topal"
}
```

Başarılı response:

```text
200 OK
```

Duplicate email response:

```text
409 Conflict
```

Önemli karar:

İlk başta `201 Created` düşünüldü. Ancak henüz `/api/users/{id}` endpoint'i olmadığı için `Location` header'ın var olmayan endpoint göstermesi doğru bulunmadı. Bu nedenle register response için `Results.Ok(response)` kullanıldı.

## 10. PasswordHasher

Password hashlemek için ASP.NET Core dahili `PasswordHasher<User>` kullanıldı.

Neden?

- Plain text password saklamamak için.
- Framework tarafından sağlanan battle-tested hashing yaklaşımını kullanmak için.
- Eğitim projesinde custom crypto yazma hatasından kaçınmak için.

Doğrulama:

```bash
docker exec minicommv2.identitydb psql -U admin -d identitydb -c "SELECT email, password_hash FROM users;"
```

Beklenen:

Password hash alanında gerçek password görünmemelidir.

## 11. Login Endpoint

Endpoint:

```text
POST /api/auth/login
```

Request:

```json
{
  "email": "hamza@example.com",
  "password": "Password123"
}
```

Başarılı response:

```text
200 OK
```

Response alanları:

```text
id
email
fullName
accessToken
tokenType
expiresAt
```

Yanlış password response:

```text
401 Unauthorized
```

## 12. JWT Token Üretimi

JWT üretimi endpoint içine gömülmedi. Bunun yerine ayrı servis oluşturuldu:

```text
Services/JwtTokenService.cs
```

Token claim'leri:

```text
sub
userId
email
fullName
jti
exp
iss
aud
```

Önemli düzeltme:

Başta email claim iki kez üretilmişti. Bunun sonucunda token payload içinde email array gibi görünüyordu.

Yanlış durum:

```json
"email": [
  "hamza@example.com",
  "hamza@example.com"
]
```

Düzeltildi.

Beklenen doğru durum:

```json
"email": "hamza@example.com"
```


## 13. JWT Validation

`Program.cs` içinde JWT Bearer authentication eklendi.

Önemli ayarlar:

```text
ValidateIssuer = true
ValidateAudience = true
ValidateIssuerSigningKey = true
ValidateLifetime = true
ClockSkew = 1 minute
MapInboundClaims = false
NameClaimType = userId
```

Neden `MapInboundClaims = false`?

.NET'in bazı JWT claim type'larını legacy claim isimlerine çevirmesini istemiyoruz. Token içinde ne ürettiysek uygulamada onu okumak istiyoruz.

## 14. Current User Endpoint

Endpoint:

```text
GET /api/users/me
```

Authorization gerektirir.

Token yoksa:

```text
401 Unauthorized
```

Geçerli token varsa:

```text
200 OK
```

Invalid token varsa:

```text
401 Unauthorized
```

Önemli karar:

Kullanıcı id route'dan alınmadı.

Yanlış yaklaşım:

```text
GET /api/users/{userId}
```

Doğru yaklaşım:

```text
GET /api/users/me
Authorization: Bearer <token>
```

Neden?

Client route'a başka bir kullanıcının id'sini yazmamalı. Kimlik bilgisi güvenilir token claim'inden okunmalıdır.

## 15. Health Endpoint

Endpoint:

```text
GET /health
```

Beklenen response:

```json
{
  "service": "MiniCommerce.Identity.API",
  "status": "Healthy",
  "timestamp": "..."
}
```

## 16. Postman Collection

Session 2'de Postman collection güncellendi.

Dosyalar:

```text
postman/MiniCommerceV2.postman_collection.json
postman/MiniCommerceV2.local.postman_environment.json
```

Eklenen Identity request'leri:

```text
Health
Register
Login
Login - Wrong Password
Current User
```

Login request'i başarılı olursa `jwtToken` environment değişkenini otomatik set eder.

## 17. Final Test Komutları

API başlatma:

```bash
dotnet run --project src/services/MiniCommerce.Identity.API/MiniCommerce.Identity.API.csproj
```

Health:

```bash
curl -i http://localhost:5000/health
```

Register:

```bash
curl -i -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "session2@example.com",
    "password": "Password123",
    "fullName": "Session Two"
  }'
```

Login:

```bash
curl -i -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "hamza@example.com",
    "password": "Password123"
  }'
```

Wrong password:

```bash
curl -i -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "hamza@example.com",
    "password": "WrongPassword123"
  }'
```

Token ile current user:

```bash
JWT_TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "hamza@example.com",
    "password": "Password123"
  }' | jq -r '.accessToken')

curl -i http://localhost:5000/api/users/me \
  -H "Authorization: Bearer $JWT_TOKEN"
```

Invalid token:

```bash
curl -i http://localhost:5000/api/users/me \
  -H "Authorization: Bearer invalid-token"
```

## 18. Karşılaşılabilecek Hatalar

### dotnet ef version mismatch

Belirti:

Global `dotnet ef` farklı sürümde olabilir.

Kontrol:

```bash
dotnet ef --version
dotnet tool run dotnet-ef --version
```

Çözüm:

Projede local tool kullan:

```bash
dotnet tool restore
dotnet tool run dotnet-ef --version
```

### İlk migration sırasında __EFMigrationsHistory log hatası

İlk migration'da EF Core `__EFMigrationsHistory` tablosunu okumaya çalışırken log seviyesinde failure görülebilir.

Migration sonunda `Done.` dönüyorsa ve tablo oluştuysa sorun değildir.

### 401 Unauthorized

Kontrol edilecekler:

- Authorization header var mı?
- Header formatı doğru mu?

```text
Authorization: Bearer <token>
```

- Token expired mı?
- Issuer/Audience/SecretKey appsettings ile uyumlu mu?

### Data Protection warning

Görülebilecek warning:

```text
No XML encryptor configured.
```

Bu session'da cookie/session kullanmadığımız için auth akışını bozmaz. Production konularında ayrıca ele alınmalıdır.

## 19. Session Sonu Checklist

- [x] Identity.API build alıyor.
- [x] PostgreSQL connection çalışıyor.
- [x] `users` tablosu oluştu.
- [x] Email unique index var.
- [x] Register endpoint çalışıyor.
- [x] Duplicate email `409 Conflict` dönüyor.
- [x] Password hash olarak saklanıyor.
- [x] Login endpoint çalışıyor.
- [x] Wrong password `401 Unauthorized` dönüyor.
- [x] JWT access token üretiliyor.
- [x] Token payload içinde duplicate email claim yok.
- [x] `/api/users/me` token olmadan `401` dönüyor.
- [x] `/api/users/me` valid token ile `200` dönüyor.
- [x] Invalid token `401` dönüyor.
- [x] Postman collection güncellendi.

## 20. Git Checkpoint

Önerilen commit mesajı:

```bash
git add docs/notes/SESSION_02_identity_api.md
git commit -m "docs: add session 02 identity notes"
git push
```

Session tag:

```bash
git tag -a session-02-complete -m "Session 02 complete"
git push origin session-02-complete
```

## 21. Sonraki Session Önizlemesi

Session 3'te Catalog.API geliştirilecek.

Kapsam:

- MongoDB options
- Product document/entity
- Product CRUD
- DTO projection
- Health endpoint
- Product seed/test
- Postman update
- Session notes

Bu session'da henüz messaging contract eklenmeyecek. Just-in-Time artifact prensibine göre Catalog stock reservation mesajları ancak gerçek consume/publish ihtiyacı doğduğunda eklenecek.
