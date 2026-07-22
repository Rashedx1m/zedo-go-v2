# 🚕 Zedo Go

**اللغة:** العربية · [English](README.md)

واجهة برمجية (Backend API) لتطبيق حجز سيارات الأجرة، مبنية بـ **.NET 10** وفق **المعمارية النظيفة (Clean Architecture)**. يأتي معها واجهة أمامية ثابتة بسيطة (لوحات العميل / السائق / الأدمن)، ويستخدم **SQLite** افتراضياً بحيث يعمل دون أي إعداد لقاعدة بيانات.

> مشروع مفتوح المصدر · رخصة MIT · © 2026 Rashedx1m

---

## ما هو Zedo Go؟

Zedo Go هو الخلفية البرمجية لخدمة نقل/تاكسي. يوفّر REST API نظيفاً يغطّي دورة الرحلة كاملة:

- **الحسابات والمصادقة** — تسجيل، دخول (JWT)، تغيير كلمة المرور، أدوار (عميل / سائق / أدمن).
- **السائقون** — الملف، الحالة (متصل/غير متصل)، الموقع اللحظي، البحث عن الأقرب.
- **طلبات الرحلات** — إنشاء، قبول، وصول السائق، بدء، إنهاء، إلغاء، والطلبات القريبة من السائق.
- **التسعير** — تسعيرة قابلة للضبط (أساسي + لكل كم + لكل دقيقة + حد أدنى)، وتقدير التكلفة.
- **المدفوعات** — حساب الأجرة تلقائياً عند الإنهاء مع تقسيم بين الشركة والسائق، وتقارير الأرباح والإيرادات.
- **لوحة التحكم** — إحصائيات مجمّعة لعرض الأدمن.

## المعمارية

يتبع المشروع **المعمارية النظيفة**: الاعتماديات تتجه إلى **الداخل**، وطبقة النطاق (Domain) لا تعتمد على أي إطار عمل أو قاعدة بيانات، والطبقات الخارجية قابلة للاستبدال.

```
API            → المتحكمات، Program.cs، JWT/Swagger، ربط DI          (العرض)
Application    → الخدمات، DTOs، الواجهات، نمط Result، المحوّلات        (حالات الاستخدام)
Domain         → الكيانات، التعدادات، واجهات المستودعات               (الأساس، بلا اعتماديات)
Infrastructure → EF Core DbContext، المستودعات، UnitOfWork، الهجرات   (البيانات/الخارجي)
```

أنماط أساسية: **Repository + Unit of Work**، ونمط **Result** لتمثيل النجاح/الفشل صراحةً، و**DTOs** لفصل الـ API عن الكيانات، و**حقن الاعتماديات** عبر المُنشِئات.

يوجد شرح عام ومستقل عن أي إطار في [docs/CLEAN_ARCHITECTURE.md](docs/CLEAN_ARCHITECTURE.md).

## قابلية التوسّع

التصميم مبنيّ للنمو دون إعادة كتابة:

- **قاعدة بيانات قابلة للاستبدال** — المزوّد معزول في `Infrastructure`؛ الانتقال من SQLite إلى SQL Server / PostgreSQL / MySQL تغييرٌ في ملف واحد (انظر الأسفل).
- **API عديم الحالة + JWT** — قابل للتوسّع أفقياً خلف موازن حِمل، دون جلسات على الخادم.
- **عزل الطبقات** — منطق العمل في `Domain`/`Application`، فيمكن إضافة BFF للموبايل أو gRPC أو عمّال خلفية دون المساس بالمنطق الأساسي.
- **Unit of Work / المعاملات** — كتابات متسقة، وسهولة لاحقة لإضافة outbox/أحداث.
- **قابلية الاختبار** — `Domain` و`Application` بلا اعتماديات بنية تحتية، فتُختبر بمعزل.

## التقنيات

| المجال | الاختيار |
|------|--------|
| بيئة التشغيل | .NET 10 |
| الـ API | ASP.NET Core Web API + Swagger |
| ORM | Entity Framework Core 10 |
| قاعدة البيانات (افتراضياً) | SQLite (ملف، بلا خادم) |
| المصادقة | JWT Bearer |
| كلمات المرور | BCrypt |

---

## البدء (للمطوّرين)

### المتطلبات
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- لا حاجة لخادم قاعدة بيانات (SQLite ملف محلي).

### الاستنساخ والتشغيل
```bash
git clone <رابط-المستودع>
cd zedo-go-v2
dotnet run --project API
```

يُنشأ ملف قاعدة البيانات (`zedo-go.db`) **تلقائياً**، وتُطبَّق الهجرات **عند الإقلاع** — دون خطوات يدوية.

ثم افتح Swagger:
```
http://localhost:5000
```

### هيكل المشروع
```
zedo-go-v2/
├── API/                 # العرض: المتحكمات، Program.cs، wwwroot (واجهة ثابتة)
├── Application/         # حالات الاستخدام: الخدمات، DTOs، الواجهات، Result، المحوّلات
├── Domain/              # الأساس: الكيانات، التعدادات، واجهات المستودعات
├── Infrastructure/      # EF Core DbContext، المستودعات، UnitOfWork، الهجرات
├── docs/                # التوثيق، مجموعة Postman، مخطط SQL القديم
└── zedo-go.sln
```

### كيف تُعدّل عليه
- **إضافة endpoint:** عرّف الواجهة في `Application/Interfaces`، نفّذها في `Application/Services`، سجّلها في `Infrastructure/DependencyInjection.cs`، ثم اعرضها من متحكّم في `API/Controllers`.
- **تعديل البيانات:** عدّل الكيانات في `Domain/Entities` و`Infrastructure/Data/AppDbContext.cs`، ثم أنشئ هجرة:
  ```bash
  dotnet tool restore
  dotnet ef migrations add <الاسم> -p Infrastructure -s API
  ```
  تُطبَّق الهجرات تلقائياً عند التشغيل التالي.

---

## تغيير قاعدة البيانات (مثال: إلى MySQL)

بما أنّ المزوّد معزول في `Infrastructure`، فالتبديل تغييرٌ صغير ومحدّد.

**1. استبدل حزمة مزوّد EF Core** في `Infrastructure/Infrastructure.csproj` (و`API/API.csproj`):
```diff
- <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.1" />
+ <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="..." />
```
> ملاحظة عن الإصدارات: استخدم مزوّداً يطابق إصدار EF Core الرئيسي لديك. Pomelo يتبع EF Core؛ وإن كنت على EF Core 10 ولم يتوفّر إصدار Pomelo مطابق بعد، فاستخدم مزوّد Oracle الرسمي `MySql.EntityFrameworkCore` 10.x، أو وائم إصدار EF Core مع أحدث نسخة يدعمها Pomelo.

**2. غيّر استدعاء المزوّد** في `Infrastructure/DependencyInjection.cs`:
```diff
- options.UseSqlite(configuration.GetConnectionString("DefaultConnection"))
+ options.UseMySql(
+     configuration.GetConnectionString("DefaultConnection"),
+     ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection")))
```
(لـ SQL Server استخدم `UseSqlServer(...)`؛ ولـ PostgreSQL أضف `Npgsql.EntityFrameworkCore.PostgreSQL` واستخدم `UseNpgsql(...)`.)

**3. حدّث سلسلة الاتصال** في `API/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3306;Database=zedogo;User=root;Password=كلمة_المرور;"
}
```

**4. أعِد توليد الهجرات** (هجرات SQLite خاصة بالمزوّد):
```bash
# احذف مجلد Infrastructure/Migrations الحالي، ثم:
dotnet ef migrations add InitialCreate -p Infrastructure -s API
dotnet ef database update -p Infrastructure -s API
```

**5. شغّل** — يطبّق التطبيق الهجرات عند الإقلاع:
```bash
dotnet run --project API
```

> تلميح: SQLite يخزّن `decimal` كنص، فتُتجاهَل `HasPrecision(...)` فيه. أما على MySQL/SQL Server/PostgreSQL فتُطبَّق هذه الدقّة كأعمدة `DECIMAL` حقيقية — دون تغيير في الكود.

---

## اختبار الـ API (Postman)

مرفقة مجموعة Postman جاهزة ليبدأ مطوّر الواجهة الأمامية فوراً بمجرد أن تكون الخلفية شغّالة (محلياً أو مرفوعة):

- استورد [`docs/zedo-go.postman_collection.json`](docs/zedo-go.postman_collection.json) في Postman.
- اضبط متغيّر المجموعة `baseUrl` (الافتراضي `http://localhost:5000`).
- شغّل **Auth → Login** أولاً؛ يُلتقَط التوكن تلقائياً في المتغيّر `token` وتستخدمه بقية الطلبات المحمية.

---

## الإعداد والنشر (Deployment)

> ⚠️ **هذه المرحلة متروكة عمداً لخبير نشر/DevOps.**
> إعداد النشر الإنتاجي (الحاويات، إعداد البيئة والأسرار، قاعدة بيانات إنتاجية، HTTPS/بروكسي عكسي، CI/CD، السجلّات والمراقبة) يجب أن يصمّمه ويكتبه خبير بحسب البيئة المستهدفة. اعتبر ما هنا نقطة انطلاق لا إعداداً إنتاجياً جاهزاً.

الحدّ الأدنى من التحصين قبل النشر:
- انقل `Jwt:Key` وأي أسرار خارج `appsettings.json` إلى متغيّرات بيئة/خزنة أسرار، واستخدم مفتاحاً قوياً.
- انتقل إلى قاعدة بيانات على خادم (انظر الأعلى)، وعطّل الهجرة التلقائية في الإنتاج إن كنت تطبّق الهجرات بعملية منفصلة.
- اضبط CORS لأصل الواجهة الأمامية الحقيقي بدل `AllowAll`.

---

## مقارنة بالنسخة السابقة

توجد نسخة أقدم من هذا المشروع بلا معمارية نظيفة. إن أردت ملاحظة الفرق في البنية وفصل المسؤوليات، قارن هذا المستودع بتلك النسخة القديمة.

## الرخصة

مُرخّص برخصة **MIT** — انظر [LICENSE](LICENSE). © 2026 Rashedx1m.
