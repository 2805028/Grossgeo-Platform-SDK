# GrossGeo SDK Stub

[![NuGet](https://img.shields.io/nuget/v/GrossGeo.SDK.Stub.svg)](https://www.nuget.org/packages/GrossGeo.SDK.Stub)
[![License](https://img.shields.io/badge/license-Proprietary-red.svg)](LICENSE)

Легковесный SDK (~25 KB) для лицензирования AutoCAD-плагинов через платформу GrossGeo.
Поддерживает планы, features, лимиты, concurrent-сессии и офлайн-режим.

---

## Требования

| Компонент | Подробности |
|-----------|-------------|
| **GrossGeo User Panel** | Установлен и запущен. [Скачать](https://grossgeo.ru/download) |
| **AutoCAD 2019–2024** | .NET Framework 4.8 |
| **AutoCAD 2025+** | .NET 8.0 |

> SDK общается с User Panel через Named Pipe IPC.
> User Panel должен быть запущен — без него SDK работает в офлайн-режиме из кэша (до 7 дней).

---

## Установка

```xml
<PackageReference Include="GrossGeo.SDK.Stub" Version="2.1.15" />
```

> Версия закрепляется точно, а не диапазоном. `2.*` разрешается в любую версию ветки 2 —
> в том числе в 2.1.0, где обфускация ломала разбор типов и продукт видел лицензию как
> `Free`/`Invalid`. Снятие версии с витрины этого не лечит: unlist не удаляет пакет, и уже
> объявленный диапазон продолжает его тянуть.

Все необходимые типы (`PlanTier`, `BillingModel`, `LicenseMode`, `LicenseCheckStatus`) включены в пакет.

**Поддерживаемые TFM:**
- `net48` — AutoCAD 2019–2024
- `net8.0-windows` — AutoCAD 2025+

---

## Быстрый старт

### 1. Получите Product Key

Зарегистрируйтесь на [grossgeo.ru/developer](https://grossgeo.ru/developer), создайте продукт и получите ключ формата `GG-XXXX-XXXX-XXXX-XXXX`.

### 2. Инициализация

```csharp
using GrossGeo.SDK;
using GrossGeo.Contracts.Licensing;

public class MyPlugin : IExtensionApplication
{
    private const string ProductKey = "GG-XXXX-XXXX-XXXX-XXXX";

    /// <summary>
    /// Доступ к лицензии через computed property.
    /// ProductLicenseAccessor — лёгкий stateless-объект,
    /// читает данные из внутреннего кэша SDK при каждом обращении.
    /// </summary>
    private static ProductLicenseAccessor License => GrossGeoLicense.ForProduct(ProductKey);

    public void Initialize()
    {
        Task.Run(async () =>
        {
            var result = await GrossGeoLicense.Initialize(new LicenseOptions
            {
                ProductKey = ProductKey,
                PluginVersion = "1.0.0"
            });

            if (result.IsValid)
            {
                // result.PlanTier  — уровень плана (Free, Pro, ProPlus)
                // result.BillingModel — модель оплаты (Subscription, Perpetual)
                // result.ExpiresAt — дата истечения лицензии
            }
        });
    }

    public void Terminate()
    {
        GrossGeoLicense.Shutdown(ProductKey);
    }
}
```

> **Почему computed property?** `ProductLicenseAccessor` — stateless-объект (~24 байта), который при каждом обращении читает актуальные данные лицензии из внутреннего кэша SDK. Нет необходимости сохранять его в поле — это устраняет проблемы с таймингом асинхронной инициализации в AutoCAD.

### 3. Защита команды

```csharp
[CommandMethod("MYCOMMAND")]
public void MyCommand()
{
    // Дождитесь инициализации. Она идёт в фоне — иначе встанет загрузка AutoCAD, — и
    // команду можно запустить раньше, чем появится вердикт. Без ожидания вы получите
    // отказ, НЕОТЛИЧИМЫЙ от «лицензии нет», и покажете пользователю «купите» там,
    // где надо было «подождите пару секунд».
    var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
    if (ready.Status == LicenseCheckStatus.Unknown)
    {
        ShowMessage(ready.Message);   // «спросить не удалось» — это НЕ «прав нет»
        return;
    }

    License.Protect(
        action: () => DoWork(),
        onBlocked: () => ShowMessage("Требуется лицензия")
    );
}
```

> **Дождаться инициализации — обязательная половина приёма.** `Initialize` в фоне — верно;
> спрашивать о правах, не дождавшись, — нет. `WaitUntilReady` для обычных команд,
> `await WhenReadyAsync(...)` для `async`-обработчиков. Оба по истечении срока возвращают
> `Status = Unknown` и `ErrorCode = NOT_CHECKED` — **явное «не спрашивали», а не отказ**.
> Доступно с версии **2.1.15**.

---

## Защита кода

### Три способа проверки лицензии

```csharp
// 1. С fallback
License.Protect(
    action: () => DoWork(),
    onBlocked: () => ShowUpgradeDialog()
);

// 2. С исключением LicenseException
License.ProtectOrThrow(() => DoWork());

// 3. Ручная проверка
if (License.IsValid)
{
    DoWork();
}
```

### Проверка уровня плана

```csharp
if (License.PlanTier >= PlanTier.Pro)
{
    EnableAdvancedTools();
}

if (License.BillingModel == BillingModel.Perpetual)
{
    ShowPerpetualBadge();
}
```

---

## Feature Guards

Фичи позволяют гибко управлять функционалом плагина через Developer Portal.

```csharp
// Проверка наличия фичи
if (License.HasFeature("advanced-export"))
{
    ShowExportMenu();
}

// Guard с fallback
License.RequireFeature("batch-processing",
    action: () => ProcessBatch(),
    onMissing: () => ShowUpgradeDialog()
);

// Guard с исключением FeatureNotAvailableException
License.RequireFeatureOrThrow("batch-processing", () => ProcessBatch());
```

---

## Feature Limits

Ограничивайте количественные параметры по планам:

```csharp
// Проверка лимита
var limit = License.GetFeatureLimit("batch-export", "maxPerCall");
if (limit.HasValue && objects.Count > limit.Value)
{
    ShowMessage($"Лимит: {limit.Value} объектов. Выбрано: {objects.Count}");
    ShowUpgradePrompt();
    return;
}

// Проверка одной строкой (true = не превышен)
if (!License.CheckLimit("batch-export", "maxPerCall", objects.Count))
{
    ShowUpgradePrompt("Превышен лимит объектов");
    return;
}

// С исключением LimitExceededException
License.RequireLimitOrThrow("batch-export", "maxPerCall", objects.Count);
```

---

## Usage Tracking (v3)

Для лимитов `MaxPerDay` / `MaxPerMonth` необходим серверный подсчёт использования.

```csharp
// Проверка дневного лимита перед выполнением
var dailyUsage = await License.GetCurrentUsageAsync("export-batch", "maxPerDay");
var dailyLimit = License.GetFeatureLimit("export-batch", "maxPerDay");
if (dailyLimit.HasValue && dailyUsage + objects.Count > dailyLimit.Value)
{
    ShowMessage($"Дневной лимит: {dailyLimit.Value}, использовано: {dailyUsage}");
    ShowUpgradePrompt();
    return;
}

// Выполняем операцию
ExportObjects(objects);

// Инкремент после успешного выполнения
var result = await License.IncrementUsageAsync("export-batch", "maxPerDay", objects.Count);
if (!result.IsSuccess)
{
    // Обработка ошибки (лимит превышен или проблема сети)
}
```

### UsageResult

| Свойство | Тип | Описание |
|----------|-----|----------|
| `IsSuccess` | `bool` | Успешна ли операция |
| `CurrentUsage` | `int` | Текущее значение usage |
| `Limit` | `int?` | Значение лимита (null = безлимитно) |
| `Remaining` | `int?` | Оставшийся quota |
| `ErrorCode` | `string?` | Код ошибки |
| `ErrorMessage` | `string?` | Сообщение об ошибке |

---

## Concurrent Sessions

Для лицензий с плавающими слотами (`LicenseMode.Concurrent`):

```csharp
// Получение сессии при запуске
var session = await GrossGeoLicense.AcquireSessionAsync(
    clientInfo: Environment.MachineName);

if (!session.IsSuccess)
{
    ShowMessage($"Все слоты заняты: {session.ErrorMessage}");
    return;
}

// Heartbeat отправляется автоматически

// Подписка на потерю сессии
GrossGeoLicense.SessionExpired += (s, e) =>
    ShowMessage($"Сессия потеряна: {e.Message}");

// Освобождение при завершении
public void Terminate()
{
    GrossGeoLicense.ReleaseSessionAsync().Wait();
    GrossGeoLicense.Shutdown();
}
```

---

## Офлайн-режим (Graceful Degradation)

SDK автоматически кэширует результат лицензии (DPAPI). При недоступности User Panel:

| Ситуация | Поведение |
|----------|-----------|
| User Panel доступен | Полная проверка → результат кэшируется |
| User Panel недоступен, кэш ≤ 7 дней | Работа из кэша, `IsInGracePeriod = true` |
| User Panel недоступен, кэш истёк | `IsValid = false` |

```csharp
if (License.IsInGracePeriod)
{
    ShowWarning($"Офлайн-режим. Осталось дней: {License.DaysRemaining}");
}
```

---

## Проверка обновлений

```csharp
var update = await GrossGeoLicense.CheckForUpdatesAsync();
if (update.HasUpdate)
{
    ShowMessage($"Доступна версия {update.AvailableVersion}!\n{update.Changelog}");
}
```

---

## Deep-link: старт Trial из плагина

Для кнопки «Попробовать бесплатно» — открывает User Panel на карточке текущего продукта (по `LicenseOptions.ProductKey`) и просит показать подтверждение старта trial. **Лицензию не активирует сама** — только открывает панель; сам trial стартует в панели, и только с подтверждением пользователя и если сервер это разрешает (`CanStartTrial=true`). Если User Panel не запущен — SDK пытается его запустить, как и остальные IPC-вызовы.

```csharp
[CommandMethod("MY_TRIAL_BUTTON")]
public async void OnTrialButtonClick()
{
    await GrossGeoLicense.RequestTrialAsync();
}
```

Для открытия карточки продукта без trial (например по кнопке «Подробнее») — `OpenProductPageAsync`:

```csharp
await GrossGeoLicense.OpenProductPageAsync();              // просто открыть карточку продукта
await GrossGeoLicense.OpenProductPageAsync("purchase");    // сразу на блоке покупки
```

Эквивалент через `grossgeo://` URI-протокол (например для кнопок вне AutoCAD-контекста) описан в руководстве по deep-link'ам — оно выдаётся вместе с доступом в [Developer Portal](https://grossgeo.ru/developer).

---

## LicenseOptions

```csharp
new LicenseOptions
{
    ProductKey = "GG-XXXX-XXXX-XXXX-XXXX",  // Обязательно
    PluginVersion = "1.0.0",                 // Для аналитики
    IpcTimeoutSeconds = 10,                  // Таймаут IPC (по умолчанию 10)
    GracePeriodDays = 7,                     // Grace period (по умолчанию 7)
    CheckForUpdatesOnInit = true,            // Проверка обновлений при старте
    CacheDirectory = null,                   // Путь к кэшу (null = по умолчанию)
    Logger = null                            // ILicenseLogger для диагностики
}
```

---

## API Reference

### Enums (GrossGeo.Contracts)

#### PlanTier

| Значение | Описание |
|----------|----------|
| `Free` (0) | Бесплатный план |
| `Pro` (1) | Основной коммерческий план |
| `ProPlus` (2) | Расширенный план |
| ~~`Maintenance` (10)~~ | ~~Право на обновления~~ — **deprecated** в v3 (Maintenance как атрибут Perpetual-плана: `MaintenanceYearlyPrice`) |
| `Enterprise` (99) | Индивидуальный контракт (зарезервирован) |

#### BillingModel

| Значение | Описание |
|----------|----------|
| `Free` (0) | Бесплатно |
| `Subscription` (1) | Подписка |
| `Perpetual` (2) | Бессрочная лицензия |
| ~~`Contract` (3)~~ | ~~Индивидуальный контракт~~ — **deprecated** в v3 |

#### LicenseMode

| Значение | Описание |
|----------|----------|
| `User` (0) | Привязка к пользователю (seat) |
| `Machine` (1) | Привязка к машине (fingerprint) |
| `Concurrent` (2) | Плавающие лицензии (пул сессий) |

### GrossGeoLicense — свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `IsInitialized` | `bool` | SDK инициализирован |
| `HasActiveSession` | `bool` | Есть concurrent-сессия |

### ProductLicenseAccessor — свойства

Получается через `GrossGeoLicense.ForProduct(productKey)`. Рекомендуется использовать как computed property:

```csharp
private static ProductLicenseAccessor License => GrossGeoLicense.ForProduct(ProductKey);
```

| Свойство | Тип | Описание |
|----------|-----|----------|
| `IsValid` | `bool` | Лицензия валидна |
| `PlanTier` | `PlanTier` | Уровень плана |
| `BillingModel` | `BillingModel` | Модель оплаты |
| `LicenseMode` | `LicenseMode` | Режим лицензирования |
| `ExpiresAt` | `DateTime?` | Дата истечения |
| `DaysRemaining` | `int?` | Дней до истечения |
| `IsInGracePeriod` | `bool` | В grace period (офлайн) |
| `IsOfflineMode` | `bool` | Работа из кэша |
| `Features` | `IReadOnlyList<string>` | Список доступных features |
| `FeatureLimits` | `IReadOnlyDictionary<string, int>?` | Лимиты фичей |
| `HasActiveSession` | `bool` | Есть concurrent-сессия |
| `LastResult` | `LicenseResult?` | Последний результат проверки |

### GrossGeoLicense — методы

| Метод | Описание |
|-------|----------|
| `Initialize(LicenseOptions)` | Async-инициализация |
| `Initialize(string productKey)` | Упрощённая инициализация |
| `InitializeSync(LicenseOptions)` | Синхронная инициализация |
| `ForProduct(string productKey)` | Получить `ProductLicenseAccessor` для продукта |
| `Shutdown()` | Завершение работы (все продукты) |
| `Shutdown(string productKey)` | Завершение работы (конкретный продукт) |
| `AcquireSessionAsync(string?, CancellationToken)` | Получить concurrent-сессию |
| `ReleaseSessionAsync(CancellationToken)` | Освободить сессию |
| `SendSessionHeartbeatAsync()` | Отправить heartbeat |
| `CheckForUpdatesAsync(CancellationToken)` | Проверка обновлений |
| `ClearLocalCache()` | Очистить кэш |
| `IncrementUsageAsync(string, string, int, CancellationToken)` | v3: Инкремент usage (featureCode, limitCode, count) для текущего продукта |
| `GetCurrentUsageAsync(string, string, CancellationToken)` | v3: Текущий usage (featureCode, limitCode) для текущего продукта |
| `RequestTrialAsync(CancellationToken)` | Открыть панель на карточке продукта и запросить старт trial (с подтверждением; лицензию не активирует сама) |
| `OpenProductPageAsync(string?, CancellationToken)` | Открыть панель на карточке продукта, опционально на секции (`purchase`, `reviews`) |

### ProductLicenseAccessor — методы

| Метод | Описание |
|-------|----------|
| `Check()` | Быстрая проверка (из памяти) |
| `CheckAsync(CancellationToken)` | Полная проверка (запрос к User Panel) |
| `RefreshAsync(CancellationToken)` | Принудительное обновление |
| `Protect(Action, Action?)` | Защита блока с fallback |
| `ProtectOrThrow(Action)` | Защита с исключением |
| `HasFeature(string)` | Проверка фичи |
| `RequireFeature(string, Action, Action?)` | Фича с fallback |
| `RequireFeatureOrThrow(string, Action)` | Фича с исключением |
| `GetFeatureLimit(string, string)` | Получить лимит |
| `CheckLimit(string, string, int)` | Проверить лимит |
| `RequireLimit(string, string, int, Action, Action?)` | Лимит с fallback |
| `IncrementUsageAsync(string, string, int, CancellationToken)` | v3: Инкремент usage для MaxPerDay/MaxPerMonth |
| `GetCurrentUsageAsync(string, string, CancellationToken)` | v3: Текущий usage для MaxPerDay/MaxPerMonth |

### Guards (вспомогательные классы)

```csharp
// LicenseGuard — обёртка над лицензией
LicenseGuard.Protect(() => DoWork(), () => ShowFallback());
LicenseGuard.OrThrow(() => DoWork());

// FeatureGuard — обёртка над фичами
FeatureGuard.Has("export");
FeatureGuard.Require("export", () => DoExport(), () => ShowUpgrade());
FeatureGuard.OrThrow("export", () => DoExport());
```

### Исключения

| Исключение | Ключевые свойства | Когда выбрасывается |
|------------|-------------------|---------------------|
| `LicenseException` | `Status` | `ProtectOrThrow` при невалидной лицензии |
| `FeatureNotAvailableException` | `FeatureCode` | `FeatureGuard.OrThrow` при отсутствии фичи |
| `LimitExceededException` | `FeatureCode`, `LimitCode`, `Limit`, `ActualValue` | `RequireLimitOrThrow` при превышении лимита |

### LicenseResult

| Свойство | Тип | Описание |
|----------|-----|----------|
| `IsValid` | `bool` | Лицензия валидна |
| `Status` | `LicenseCheckStatus` | Статус проверки |
| `PlanTier` | `PlanTier` | Уровень плана |
| `BillingModel` | `BillingModel` | Модель оплаты |
| `LicenseMode` | `LicenseMode` | Режим лицензирования |
| `LicenseId` | `Guid?` | ID лицензии |
| `ExpiresAt` | `DateTime?` | Дата истечения |
| `Features` | `IReadOnlyList<string>` | Доступные features |
| `FeatureLimits` | `IReadOnlyDictionary<string, int>?` | Лимиты фичей |
| `Message` | `string` | Сообщение для пользователя |
| `IsInGracePeriod` | `bool` | В grace period |
| `IsOfflineMode` | `bool` | Офлайн-режим |

---

## Манифест продукта (product-manifest.json)

При создании продукта на платформе GrossGeo разработчик описывает его через `product-manifest.json`. Этот файл определяет метаданные продукта, тарифные планы, фичи, лимиты и релизы.

> **Примечание:** `PackageContents.xml` для AutoCAD Autoloader генерируется платформой автоматически при установке — создавать его вручную не нужно.

### Минимальный манифест (бесплатный продукт)

```json
{
  "product": {
    "name": "My Plugin",
    "slug": "my-plugin",
    "shortDescription": "Краткое описание плагина",
    "fullDescription": "Полное описание плагина для страницы в каталоге",
    "licensingMode": "GrossGeo",
    "trialDays": 0,
    "tags": ["autocad", "tools"]
  },

  "plans": [
    {
      "code": "free",
      "name": "Free",
      "displayName": "Бесплатный",
      "tier": "Free",
      "billingModel": "Free",
      "licenseMode": "Machine",
      "monthlyPrice": 0,
      "yearlyPrice": 0,
      "oneTimePrice": null,
      "currency": "RUB",
      "maxSeats": 1,
      "maxConcurrentSessions": null,
      "trialDays": 0,
      "trialBindingMode": null,
      "isActive": true
    }
  ],

  "features": [
    {
      "code": "basic",
      "name": "Базовый функционал",
      "description": "Основные инструменты",
      "isDefault": true
    }
  ],

  "planFeatures": {
    "free": ["basic"]
  },

  "featureLimits": {},

  "releases": [
    {
      "version": "1.0.0",
      "channel": "Stable",
      "changelog": "Первый релиз",
      "distributionType": "Bundle",
      "postInstallAction": "RequireRestart",
      "netloadDllPath": "Contents/MyPlugin.dll",
      "minAutoCADVersion": "R24.4",
      "maxAutoCADVersion": "R25.1",
      "targetPlatforms": ["AutoCAD", "Civil3D"],
      "supportedOS": ["Win64"],
      "loadOnStartup": true,
      "fixtureFile": "fixtures/MyPlugin.v1.0.0.bundle.zip"
    }
  ]
}
```

### Структура манифеста

#### `product` — метаданные продукта

| Поле | Тип | Обязательно | Описание |
|------|-----|:-----------:|----------|
| `name` | `string` | ✅ | Название продукта |
| `slug` | `string` | ✅ | URL-идентификатор (латиница, дефисы) |
| `shortDescription` | `string` | ✅ | Краткое описание (1-2 предложения) |
| `fullDescription` | `string` | ✅ | Полное описание для каталога |
| `licensingMode` | `string` | ✅ | Режим лицензирования (см. ниже) |
| `trialDays` | `int` | ✅ | Длительность trial-периода (0 = без trial) |
| `tags` | `string[]` | — | Теги для поиска в каталоге |
| `externalPurchaseUrl` | `string?` | — | URL покупки (для ExternalOnly) |
| `externalDownloadUrl` | `string?` | — | URL скачивания (для ExternalOnly) |
| `externalLicenseInstructions` | `string?` | — | Инструкция активации (для ExternalOnly) |

**`licensingMode`:**

| Значение | Описание |
|----------|----------|
| `GrossGeo` | Лицензирование через платформу (SDK + планы + фичи) |
| `ExternalOnly` | Каталог и аналитика через GrossGeo, лицензирование — на стороне разработчика |

#### `plans[]` — тарифные планы

| Поле | Тип | Описание |
|------|-----|----------|
| `code` | `string` | Уникальный код плана (латиница, дефисы) |
| `name` | `string` | Системное имя |
| `displayName` | `string` | Отображаемое имя |
| `tier` | `string` | Уровень: `Free`, `Pro`, `ProPlus`, `Enterprise` |
| `billingModel` | `string` | Модель: `Free`, `Subscription`, `Perpetual` |
| `billingPeriod` | `string?` | **Deprecated в v3** — период на Order/Subscription, не на плане |
| `licenseMode` | `string` | Режим: `User`, `Machine`, `Concurrent` |
| `monthlyPrice` | `decimal?` | Цена за месяц |
| `yearlyPrice` | `decimal?` | Цена за год |
| `oneTimePrice` | `decimal?` | Разовая цена (Perpetual) |
| `currency` | `string` | Валюта (`RUB`, `USD`) |
| `maxSeats` | `int` | Количество рабочих мест |
| `maxConcurrentSessions` | `int?` | Макс. одновременных сессий (для Concurrent) |
| `trialDays` | `int` | Trial для этого плана (0 = без trial) |
| `trialBindingMode` | `string?` | Привязка trial: `Account`, `AccountAndMachine` |
| `requiresPlanCode` | `string?` | **Deprecated в v3** — Maintenance через `maintenanceYearlyPrice` |
| `maintenanceYearlyPrice` | `decimal?` | v3: Годовая цена Maintenance (только для Perpetual) |
| `description` | `string?` | v3: Короткое описание плана |
| `highlights` | `string[]?` | v3: Маркетинговые буллеты для pricing table |
| `isActive` | `bool` | Активен ли план |

#### `features[]` — фичи продукта

| Поле | Тип | Описание |
|------|-----|----------|
| `code` | `string` | Уникальный код фичи |
| `name` | `string` | Название |
| `description` | `string` | Описание |
| `isDefault` | `bool` | Включена в план по умолчанию |

#### `planFeatures` — привязка фичей к планам

```json
{
  "free": ["basic-tools", "simple-export"],
  "pro": ["basic-tools", "simple-export", "advanced-tools", "batch"]
}
```

Ключ — `code` плана, значение — массив `code` фичей.

#### `featureLimits` — количественные ограничения

Ключ формата `{planCode}.{featureCode}`, значение — массив лимитов или `null` (без ограничений):

```json
{
  "free.simple-export": [
    {
      "limitCode": "maxObjects",
      "limitType": "MaxPerCall",
      "limitValue": 5
    },
    {
      "limitCode": "maxFileSize",
      "limitType": "MaxSize",
      "limitValue": 10485760
    }
  ],
  "pro.simple-export": null
}
```

**Типы лимитов (`limitType`):**

| Тип | Описание | Пример |
|-----|----------|--------|
| `MaxPerCall` | Максимум за одну операцию | 5 объектов в экспорте |
| `MaxPerSession` | Максимум за сессию | 50 операций |
| `MaxPerDay` | Максимум за день (сброс в 00:00 UTC) | 100 экспортов/день |
| `MaxPerMonth` | Максимум за месяц (сброс 1-го числа) | 500 экспортов/мес |
| `MaxSize` | Максимальный размер (байты) | 10 MB |

#### `releases[]` — релизы продукта

| Поле | Тип | Описание |
|------|-----|----------|
| `version` | `string` | Версия (SemVer) |
| `channel` | `string` | Канал: `Stable`, `Beta`, `Alpha` |
| `changelog` | `string` | Описание изменений |
| `distributionType` | `string` | Тип: `Bundle` (Autoloader) или `Installer` (EXE) |
| `postInstallAction` | `string` | Действие: `RequireRestart`, `None` |
| `netloadDllPath` | `string?` | Путь к DLL внутри bundle (для `Bundle`) |
| `minAutoCADVersion` | `string` | Мин. серия AutoCAD (например, `R24.4`) |
| `maxAutoCADVersion` | `string` | Макс. серия AutoCAD (например, `R25.1`) |
| `targetPlatforms` | `string[]` | Платформы: `AutoCAD`, `Civil3D`, `Map` |
| `supportedOS` | `string[]` | ОС: `Win64` |
| `loadOnStartup` | `bool` | Загружать при старте AutoCAD |
| `fixtureFile` | `string` | Путь к `.bundle.zip` файлу |

### Примеры сценариев

#### Подписка с trial

```json
{
  "product": {
    "name": "GeoExport Pro",
    "slug": "geoexport-pro",
    "licensingMode": "GrossGeo",
    "trialDays": 14,
    ...
  },
  "plans": [
    {
      "code": "pro",
      "tier": "Pro",
      "billingModel": "Subscription",
      "licenseMode": "User",
      "monthlyPrice": 990,
      "yearlyPrice": 9900,
      "trialDays": 14,
      "trialBindingMode": "Account",
      "description": "Полный набор инструментов",
      "highlights": ["Безлимитный экспорт", "Приоритетная поддержка"],
      ...
    }
  ]
}
```

#### Perpetual + Maintenance

```json
{
  "plans": [
    {
      "code": "pro",
      "tier": "Pro",
      "billingModel": "Perpetual",
      "licenseMode": "Machine",
      "oneTimePrice": 9990,
      "maintenanceYearlyPrice": 2990,
      "description": "Бессрочная лицензия",
      "highlights": ["Покупка навсегда", "Обновления с Maintenance"]
    }
  ]
}
```

#### Concurrent (плавающие лицензии)

```json
{
  "plans": [
    {
      "code": "team",
      "tier": "ProPlus",
      "billingModel": "Subscription",
      "licenseMode": "Concurrent",
      "maxSeats": 10,
      "maxConcurrentSessions": 10,
      ...
    }
  ]
}
```

#### Freemium (Free + Pro с лимитами)

```json
{
  "plans": [
    { "code": "free", "tier": "Free", "billingModel": "Free", ... },
    { "code": "pro", "tier": "Pro", "billingModel": "Subscription", ... }
  ],
  "planFeatures": {
    "free": ["basic-tools", "export"],
    "pro": ["basic-tools", "export", "advanced", "batch"]
  },
  "featureLimits": {
    "free.export": [{ "limitCode": "maxObjects", "limitType": "MaxPerCall", "limitValue": 5 }],
    "pro.export": null
  }
}
```

#### Внешнее лицензирование (ExternalOnly)

```json
{
  "product": {
    "name": "My External Plugin",
    "licensingMode": "ExternalOnly",
    "externalPurchaseUrl": "https://example.com/buy",
    "externalDownloadUrl": "https://example.com/download",
    "externalLicenseInstructions": "Получите ключ на сайте и введите в настройках плагина",
    ...
  },
  "plans": [],
  "features": [],
  "planFeatures": {},
  "featureLimits": {}
}
```

### Полные примеры

Все примеры `product-manifest.json` доступны в каталоге [`samples/`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples):

| Пример | Сценарий |
|--------|----------|
| `TestProduct.Free` | Бесплатный продукт |
| `TestProduct.Licensed` | Perpetual + Maintenance |
| `TestProduct.Subscription` | Подписка с trial |
| `TestProduct.Freemium` | Free + Pro с Feature Limits |
| `TestProduct.Concurrent` | Плавающие лицензии (Concurrent) |
| `TestProduct.Analytics` | Внешнее лицензирование (ExternalOnly) |
| `TestProduct.Installer` | Installer-дистрибуция (MSI/EXE) |

---

## Примеры плагинов

| Пример | Описание |
|--------|----------|
| [`TestProduct.Free`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Free) | Бесплатный плагин без лицензии |
| [`TestProduct.Licensed`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Licensed) | Базовое лицензирование |
| [`TestProduct.Subscription`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Subscription) | Подписочная модель |
| [`TestProduct.Freemium`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Freemium) | Free-план + платные фичи |
| [`TestProduct.Concurrent`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Concurrent) | Плавающие лицензии |
| [`TestProduct.Analytics`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Analytics) | Телеметрия и аналитика |
| [`TestProduct.Installer`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Installer) | Installer-дистрибуция (MSI/EXE) |

---

## Поддержка

- Документация: [Grossgeo-Platform-SDK](https://github.com/2805028/Grossgeo-Platform-SDK)
- Developer Portal: [grossgeo.ru/developer](https://grossgeo.ru/developer)
- Вопросы: [GitHub Issues](https://github.com/2805028/Grossgeo-Platform-SDK/issues)

---

## Лицензия

Proprietary. © 2025 GrossGeo. All rights reserved.
