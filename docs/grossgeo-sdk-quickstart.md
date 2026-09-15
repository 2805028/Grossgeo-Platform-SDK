# GrossGeo SDK — Быстрый старт

**Версия SDK:** 2.0 | **Платформа:** AutoCAD 2019+ | **.NET:** Framework 4.8 / .NET 8

> ⚠️ **Уведомление (2026-05-08, DOC-033):** §6 «Манифест продукта (product-manifest.json)» описывает legacy-формат до BC-PR2/BC-PR3 (2026-05-06). Текущая модель — `plans-manifest.json` (импорт планов/фич через Dev Panel) + `release-manifest.json` (метаданные релиза внутри bundle). Актуальная спецификация: [docs/dev-portal-manifests.md](../dev-portal-manifests.md).

---

## 1. Установка

### NuGet

```xml
<PackageReference Include="GrossGeo.SDK.Stub" Version="2.1.17" />
```

Зависимость `GrossGeo.Contracts` подтянется автоматически.

Версия указана точно, а не диапазоном `2.*`: диапазон разрешается в любую версию ветки 2,
включая снятые с витрины — unlist не удаляет пакет, и сборка молча получила бы версию, где
проверка лицензии работала неверно.

**Ниже `2.1.13` не опускайтесь.** На .NET Framework (AutoCAD 2019–2024) в версиях до `2.1.13`
SDK не доходил до панели: сборка проходит, пакет ставится, а отказ наступает уже в работе — и
выглядит как отказ лицензии, а не как версия пакета. Если вам достался проект со старым пином,
это первое, что стоит поднять.

### Ручная

Скопируйте `GrossGeo.SDK.Stub.dll` и `GrossGeo.Contracts.dll` в проект и добавьте Reference.

---

## 2. Минимальный плагин

```csharp
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using GrossGeo.SDK;

[assembly: ExtensionApplication(typeof(MyPlugin.Plugin))]
[assembly: CommandClass(typeof(MyPlugin.Plugin))]

namespace MyPlugin
{
    public class Plugin : IExtensionApplication
    {
        // DOC-098: строка ниже — ЗАГЛУШКА, не код для запуска. `X` не hex-символ,
        // поэтому SDK отклонит именно ЭТУ строку исключением ArgumentException
        // ("Invalid ProductKey format") прямо на Initialize() — замените на
        // настоящий ключ (формат GG-XXXX-XXXX-XXXX-XXXX, но с hex-цифрами
        // 0-9/A-F вместо букв X), который Developer Portal покажет в карточке
        // продукта сразу после его создания.
        private const string ProductKey = "GG-XXXX-XXXX-XXXX-XXXX"; // из Developer Portal

        public void Initialize()
        {
            _ = Task.Run(async () =>
            {
                var result = await GrossGeoLicense.Initialize(new LicenseOptions
                {
                    ProductKey = ProductKey,
                    PluginVersion = "1.0.0"
                });
            });
        }

        public void Terminate() => GrossGeoLicense.Shutdown();

        [CommandMethod("MY_CMD")]
        public void MyCommand()
        {
            // LGC-720: дождитесь инициализации — она идёт в фоне, и команду можно запустить
            // раньше, чем появится вердикт. Без ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
            var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
            if (ready.Status == LicenseCheckStatus.Unknown) { Ed?.WriteMessage("\n" + ready.Message); return; }

            GrossGeoLicense.Protect(
                () => { /* защищённый код */ },
                () => Application.DocumentManager.MdiActiveDocument.Editor
                        .WriteMessage("\nТребуется лицензия."));
        }
    }
}
```

---

## 3. API — краткий справочник

### Инициализация

```csharp
// Асинхронная (рекомендуется)
var result = await GrossGeoLicense.Initialize(new LicenseOptions
{
    ProductKey = "GG-XXXX-XXXX-XXXX-XXXX",
    PluginVersion = "1.0.0",
    GracePeriodDays = 7,        // offline grace period (0–30, по умолчанию 7)
    IpcTimeoutSeconds = 10,     // таймаут IPC (1–120, по умолчанию 10)
    CheckForUpdatesOnInit = true
});

// Синхронная (если нужно блокировать поток)
var result = GrossGeoLicense.InitializeSync("GG-XXXX-XXXX-XXXX-XXXX");
```

### Свойства состояния

```csharp
GrossGeoLicense.IsInitialized   // bool
GrossGeoLicense.IsValid         // bool — лицензия валидна
GrossGeoLicense.PlanTier        // Free | Pro | ProPlus | Enterprise
GrossGeoLicense.BillingModel    // Free | Subscription | Perpetual | Contract
GrossGeoLicense.LicenseMode     // User | Machine | Concurrent
GrossGeoLicense.ExpiresAt       // DateTime?
GrossGeoLicense.DaysRemaining   // int?
GrossGeoLicense.IsInGracePeriod // bool
GrossGeoLicense.IsOfflineMode   // bool
GrossGeoLicense.Features        // IReadOnlyList<string>
GrossGeoLicense.FeatureLimits   // IReadOnlyDictionary<string, int>?
GrossGeoLicense.LastResult      // LicenseResult?
```

### Защита кода

```csharp
// С fallback'ом
GrossGeoLicense.Protect(
    () => DoWork(),
    () => ShowBlocked());

// С исключением
GrossGeoLicense.ProtectOrThrow(() => DoWork()); // throws LicenseException
var val = GrossGeoLicense.ProtectOrThrow(() => Calculate()); // generic
```

### Features

```csharp
if (GrossGeoLicense.HasFeature("advanced-export")) { ... }

GrossGeoLicense.RequireFeature("batch",
    () => DoBatch(),
    () => ShowUpgrade());

GrossGeoLicense.RequireFeatureOrThrow("batch", () => DoBatch());
// throws FeatureNotAvailableException
```

### Feature Limits

```csharp
// Получить лимит
int? limit = GrossGeoLicense.GetFeatureLimit("export", "maxObjects");

// Проверить
if (!GrossGeoLicense.CheckLimit("export", "maxObjects", objects.Count))
{
    ShowUpgrade();
    return;
}

// Или с исключением
GrossGeoLicense.RequireLimitOrThrow("export", "maxObjects", objects.Count);
// throws LimitExceededException { Limit, ActualValue }
```

### Guard-классы (алиасы)

```csharp
LicenseGuard.Protect(() => DoWork(), () => ShowBlocked());
LicenseGuard.OrThrow(() => DoWork());

FeatureGuard.Has("batch");
FeatureGuard.Require("batch", () => DoBatch());
FeatureGuard.OrThrow("batch", () => DoBatch());
```

### Public Features (доступны без лицензии)

```csharp
await GrossGeoLicense.LoadPublicFeaturesAsync(productId);
GrossGeoLicense.IsPublicFeature("basic-viewer"); // true
GrossGeoLicense.HasFeature("basic-viewer");      // true даже без лицензии
```

### Concurrent Sessions (плавающие лицензии)

```csharp
// Получить сессию
var session = await GrossGeoLicense.AcquireSessionAsync(Environment.MachineName);
if (!session.IsSuccess) { /* все слоты заняты */ }

// Обработка потери сессии
GrossGeoLicense.SessionExpired += (s, e) => { /* e.Message */ };

// Освободить при завершении
await GrossGeoLicense.ReleaseSessionAsync();
```

### Проверка обновлений

```csharp
var update = await GrossGeoLicense.CheckForUpdatesAsync();
if (update.HasUpdate)
{
    // update.AvailableVersion, update.Changelog, update.DownloadUrl
}
```

---

## 4. Бизнес-модели — шаблоны кода

### Free (бесплатный)

```csharp
public void Initialize()
{
    _ = Task.Run(async () =>
    {
        await GrossGeoLicense.Initialize(new LicenseOptions
        {
            ProductKey = "GG-FREE-PROD-0001"
        });
        // PlanTier = Free, BillingModel = Free
    });
}

// Все команды работают без проверок
[CommandMethod("MY_FREE_CMD")]
public void FreeCommand() { DoWork(); }
```

### Freemium (бесплатный базовый + платные фичи)

```csharp
[CommandMethod("BASIC_CMD")]
public void BasicCommand()
{
    FeatureGuard.Require("basic-tools", () => DoBasicWork());
}

[CommandMethod("PRO_CMD")]
public void ProCommand()
{
    FeatureGuard.Require("advanced-tools",
        () => DoAdvancedWork(),
        () => ShowUpgradeDialog());
}

[CommandMethod("EXPORT")]
public void ExportCommand()
{
    // LGC-720: дождитесь инициализации — она идёт в фоне, и команду можно запустить
    // раньше, чем появится вердикт. Без ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
    var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
    if (ready.Status == LicenseCheckStatus.Unknown) { Ed?.WriteMessage("\n" + ready.Message); return; }

    if (!GrossGeoLicense.CheckLimit("export", "maxObjects", selection.Count))
    {
        var limit = GrossGeoLicense.GetFeatureLimit("export", "maxObjects");
        editor.WriteMessage($"\nЛимит: {limit} объектов. Обновитесь до Pro.");
        return;
    }
    DoExport(selection);
}
```

### Subscription (подписка)

```csharp
[CommandMethod("SUB_CMD")]
public void SubscriptionCommand()
{
    // LGC-720: дождитесь инициализации — она идёт в фоне, и команду можно запустить
    // раньше, чем появится вердикт. Без ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
    var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
    if (ready.Status == LicenseCheckStatus.Unknown) { Ed?.WriteMessage("\n" + ready.Message); return; }

    GrossGeoLicense.Protect(
        () =>
        {
            // PlanTier = Pro, BillingModel = Subscription
            DoWork();
        },
        () =>
        {
            if (GrossGeoLicense.LastResult?.Status == LicenseCheckStatus.Expired)
                editor.WriteMessage("\nПодписка истекла. Продлите в User Panel.");
            else
                editor.WriteMessage("\nТребуется подписка.");
        });
}
```

### Perpetual (бессрочная лицензия)

```csharp
[CommandMethod("PERP_CMD")]
public void PerpetualCommand()
{
    // LGC-720: дождитесь инициализации — она идёт в фоне, и команду можно запустить
    // раньше, чем появится вердикт. Без ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
    var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
    if (ready.Status == LicenseCheckStatus.Unknown) { Ed?.WriteMessage("\n" + ready.Message); return; }

    GrossGeoLicense.Protect(
        () =>
        {
            // PlanTier = Pro, BillingModel = Perpetual
            DoWork();
        },
        () => editor.WriteMessage("\nТребуется покупка лицензии."));
}

// Проверка обновлений — зависит от Maintenance
[CommandMethod("CHECK_UPD")]
public async void CheckUpdates()
{
    var upd = await GrossGeoLicense.CheckForUpdatesAsync();
    if (upd.HasUpdate)
        editor.WriteMessage($"\nДоступна v{upd.AvailableVersion}");
    else
        editor.WriteMessage("\nОбновлений нет (проверьте Maintenance подписку).");
}
```

### Concurrent (плавающие лицензии)

```csharp
public void Initialize()
{
    _ = Task.Run(async () =>
    {
        var result = await GrossGeoLicense.Initialize(options);

        if (result.IsValid && result.LicenseMode == LicenseMode.Concurrent)
        {
            var session = await GrossGeoLicense.AcquireSessionAsync(
                Environment.MachineName);
            if (!session.IsSuccess)
                WriteMessage($"Все слоты заняты: {session.ErrorMessage}");
        }
    });

    GrossGeoLicense.SessionExpired += (s, e) =>
        WriteMessage($"Сессия потеряна: {e.Message}");
}

public void Terminate()
{
    if (GrossGeoLicense.HasActiveSession)
        GrossGeoLicense.ReleaseSessionAsync().Wait();
    GrossGeoLicense.Shutdown();
}
```

---

## 5. Структура проекта

```
MyPlugin/
├── MyPlugin.csproj
├── Plugin.cs                         # IExtensionApplication + команды
└── MyPlugin.bundle/
    ├── PackageContents.xml           # AutoCAD bundle manifest
    └── Contents/
        ├── MyPlugin.dll
        ├── GrossGeo.SDK.Stub.dll
        └── GrossGeo.Contracts.dll
```

### .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net48;net8.0-windows</TargetFrameworks>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="GrossGeo.SDK.Stub" Version="2.1.17" />
  </ItemGroup>

  <!-- AutoCAD References -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net48'">
    <Reference Include="accoremgd" HintPath="..." Private="False" />
    <Reference Include="acdbmgd"   HintPath="..." Private="False" />
    <Reference Include="acmgd"     HintPath="..." Private="False" />
  </ItemGroup>
</Project>
```

### PackageContents.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage
    SchemaVersion="1.0"
    AppVersion="1.0.0"
    Name="MyPlugin"
    Description="Описание"
    Author="Studio Name">

  <CompanyDetails Name="Studio Name" Url="https://example.com"/>

  <Components Description="MyPlugin Components">
    <!-- AutoCAD -->
    <ComponentEntry
        AppName="MyPlugin"
        ModuleName="./Contents/MyPlugin.dll"
        AppDescription="MyPlugin for AutoCAD"
        AppType=".Net"
        LoadOnAutoCADStartup="True">
      <RuntimeRequirements OS="Win64" Platform="AutoCAD"
          SeriesMin="R24.0" SeriesMax="R25.1"/>
    </ComponentEntry>

    <!-- Civil 3D (отдельный ComponentEntry) -->
    <ComponentEntry
        AppName="MyPlugin"
        ModuleName="./Contents/MyPlugin.dll"
        AppDescription="MyPlugin for Civil3D"
        AppType=".Net"
        LoadOnAutoCADStartup="True">
      <RuntimeRequirements OS="Win64" Platform="Civil3D"
          SeriesMin="R24.0" SeriesMax="R25.1"/>
    </ComponentEntry>
  </Components>
</ApplicationPackage>
```

Версии AutoCAD:

| AutoCAD | Series | .NET |
|---------|--------|------|
| 2019 | R23.0 | 4.7 |
| 2020 | R23.1 | 4.7 |
| 2021 | R24.0 | 4.8 |
| 2022 | R24.1 | 4.8 |
| 2023 | R24.2 | 4.8 |
| 2024 | R24.3 | 4.8 |
| 2025 | R25.0 | 8.0 |

---

## 6. Манифест продукта (product-manifest.json)

```json
{
  "product": {
    "name": "My Plugin",
    "slug": "my-plugin",
    "shortDescription": "Краткое описание",
    "fullDescription": "Полное описание...",
    "licensingMode": "GrossGeo",
    "trialDays": 14,
    "tags": ["export", "analysis"]
  },
  "plans": [
    {
      "code": "free",
      "tier": "Free",
      "billingModel": "Free",
      "licenseMode": "Machine",
      "monthlyPrice": 0,
      "isActive": true
    },
    {
      "code": "pro",
      "tier": "Pro",
      "billingModel": "Subscription",
      "billingPeriod": "Monthly",
      "licenseMode": "Machine",
      "monthlyPrice": 990,
      "yearlyPrice": 9900,
      "trialDays": 14,
      "isActive": true
    }
  ],
  "features": [
    { "code": "basic-tools", "name": "Базовые", "isDefault": true, "isPublic": false },
    { "code": "pro-tools",   "name": "PRO",     "isDefault": false, "isPublic": false }
  ],
  "planFeatures": {
    "free": ["basic-tools"],
    "pro": ["basic-tools", "pro-tools"]
  },
  "featureLimits": {
    "free.basic-tools": [
      { "limitCode": "maxObjects", "limitType": "MaxPerCall", "limitValue": 10 }
    ],
    "pro.basic-tools": null
  }
}
```

---

## 7. Публикация

1. **Студия** → Developer Portal → создать студию, пройти KYC
2. **Продукт** → создать продукт, настроить планы/фичи/лимиты, получить `ProductKey`
3. **Релиз** → загрузить `.bundle.zip`, заполнить changelog, указать совместимость
4. **Модерация** → Submit → PendingReview → Approved/Rejected
5. **Публикация** → Approved → автоматическая публикация в каталог

Статусы релиза: `Draft` → `PendingReview` → `Approved` → `Published` (или `Rejected`).

---

## 8. Требования к окружению

| Компонент | Версия |
|-----------|--------|
| GrossGeo User Panel | 2.0+ (должен быть установлен и запущен) |
| .NET Framework | 4.8 (AutoCAD 2019–2024) |
| .NET | 8.0 (AutoCAD 2025+) |
| Windows | 10/11 x64 |
| AutoCAD / Civil 3D | 2019–2025 |

---

## 9. Исключения SDK

| Класс | Когда | Свойства |
|-------|-------|----------|
| `LicenseException` | `ProtectOrThrow` — лицензия невалидна | `Status` |
| `FeatureNotAvailableException` | `RequireFeatureOrThrow` — фичи нет | `FeatureCode` |
| `LimitExceededException` | `RequireLimitOrThrow` — лимит превышен | `FeatureCode`, `LimitCode`, `Limit`, `ActualValue` |

---

## 10. Troubleshooting

| Проблема | Причина | Решение |
|----------|---------|---------|
| `LicenseCheckStatus.NetworkError` | User Panel не запущен | Запустить GrossGeo User Panel |
| `LicenseCheckStatus.InvalidProductKey` | Неверный ключ | Проверить ProductKey в Developer Portal |
| `LicenseCheckStatus.MachineNotBound` | Машина не привязана | Активировать лицензию в User Panel |
| `LicenseCheckStatus.Expired` | Подписка истекла | Продлить подписку в User Panel |
| `LicenseCheckStatus.NoAvailableSeats` | Все concurrent-слоты заняты | Дождаться освобождения / увеличить план |
| `IsInGracePeriod = true` | Нет связи с сервером | Восстановить интернет в течение GracePeriodDays |
| DLL не загружается | Нет в Contents/ | Проверить CopyLocalLockFileAssemblies и структуру bundle |

Диагностические логи SDK: `%LocalAppData%/GrossGeo/SDK.Stub/sdk_diag_YYYYMMDD.log`
