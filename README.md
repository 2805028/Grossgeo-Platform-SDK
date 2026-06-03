# GrossGeo SDK Stub

[![NuGet](https://img.shields.io/nuget/v/GrossGeo.SDK.Stub.svg)](https://www.nuget.org/packages/GrossGeo.SDK.Stub)
[![License](https://img.shields.io/badge/license-Proprietary-red.svg)](LICENSE)

Легковесный SDK (~25 KB) для лицензирования AutoCAD-плагинов через платформу GrossGeo.
Поддерживает планы, features, лимиты, concurrent-сессии и офлайн-режим.

---

## Содержание

- [Требования](#требования)
- [Установка](#установка)
- [Быстрый старт](#быстрый-старт)
- [Ключевые понятия](#ключевые-понятия)
- [Защита кода](#защита-кода)
- [Feature Guards](#feature-guards)
- [Feature Limits](#feature-limits)
- [Usage Tracking (MaxPerDay / MaxPerMonth)](#usage-tracking-maxperday--maxpermonth)
- [Concurrent Sessions](#concurrent-sessions)
- [Офлайн-режим](#офлайн-режим-graceful-degradation)
- [Проверка обновлений](#проверка-обновлений)
- [LicenseOptions](#licenseoptions)
- [API Reference](#api-reference)
- [Манифесты продукта](#манифесты-продукта)
- [Multi-plugin (несколько плагинов)](#multi-plugin-несколько-плагинов-в-одном-процессе)
- [Примеры плагинов](#примеры-плагинов)
- [FAQ](#faq)
- [Поддержка](#поддержка)

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
<PackageReference Include="GrossGeo.SDK.Stub" Version="2.1.0" />
```

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
    private static ProductLicenseAccessor? _license;

    public void Initialize()
    {
        Task.Run(async () =>
        {
            var result = await GrossGeoLicense.Initialize(new LicenseOptions
            {
                ProductKey = ProductKey,
                PluginVersion = "1.0.0"
            });

            // Получаем accessor, привязанный к конкретному продукту
            _license = GrossGeoLicense.ForProduct(ProductKey);

            if (result.IsValid)
            {
                // result.PlanTier    — уровень плана (Free, Pro, ProPlus)
                // result.BillingModel — модель оплаты (Subscription, Perpetual)
                // result.Features    — список доступных фичей
                // result.ExpiresAt   — дата истечения лицензии
            }
        });
    }

    public void Terminate()
    {
        GrossGeoLicense.Shutdown(ProductKey);
    }
}
```

### 3. Защита команды

```csharp
[CommandMethod("MYCOMMAND")]
public void MyCommand()
{
    _license?.Protect(
        action: () => DoWork(),
        onBlocked: () => ShowMessage("Требуется лицензия")
    );
}
```

---

## Ключевые понятия

Прежде чем интегрировать SDK, важно понять модель лицензирования GrossGeo.

### Планы (PlanTier)

План определяет **уровень сервиса** — набор фичей и лимитов, доступных пользователю.

| PlanTier | Описание | Типичное применение |
|----------|----------|---------------------|
| **Free** | Бесплатный план | Демо, базовые утилиты |
| **Pro** | Коммерческий план | Полный функционал |
| **ProPlus** | Расширенный коммерческий | Команды, повышенные лимиты |
| **Enterprise** | Зарезервирован | Индивидуальные условия |

Иерархия: `Free < Pro < ProPlus < Enterprise`.
Все фичи нижнего плана **должны** быть явно включены в верхний (через `planCodes` фичи).

### Модели оплаты (BillingModel)

Определяет **как** пользователь платит за план.

| BillingModel | Описание | Допустимые PlanTier |
|--------------|----------|---------------------|
| **Free** | Бесплатно | Только Free |
| **Subscription** | Подписка (месяц/год) | Pro, ProPlus |
| **Perpetual** | Разовая покупка навсегда | Pro, ProPlus |

**Один план = один PlanTier + один BillingModel.**

Для Subscription период (месяц/год) выбирается пользователем при покупке — это toggle в UI, а не отдельный план. Вы задаёте `monthlyPrice` и `yearlyPrice` на одном плане.

### Perpetual + Maintenance

Perpetual-лицензия бессрочная, но обновления ограничены версией на момент покупки.
**Maintenance** — необязательная годовая подписка на обновления (`maintenanceYearlyPrice`).

```
Покупка v1.0 без Maintenance → работает v1.0, не обновляется
Покупка Maintenance          → доступны все версии (v1.1, v2.0...)
Maintenance истекает          → фиксируется текущая версия (Version Lock)
Продление Maintenance         → снова доступны все новые версии
```

### Режимы лицензирования (LicenseMode)

| LicenseMode | Привязка | Перенос | Offline | Heartbeat |
|-------------|----------|---------|---------|-----------|
| **User** | К пользователю (seat) | Переназначение | Grace 3 дня | Нет |
| **Machine** | К fingerprint машины | Отвязка + привязка | 30 дней | Нет |
| **Concurrent** | К сессии (пул) | Автоматический | Нет | 5 мин |

### Features и Limits

**Feature** — функция плагина, которую вы защищаете через SDK.

- `IsDefault = true` — фича доступна **во всех планах** (включая Free) для авторизованных пользователей
- `IsDefault = false` — фича доступна только в планах, перечисленных в её `planCodes`
- Код **не обёрнутый** в `HasFeature` / `FeatureGuard` — доступен всем, включая пользователей без лицензии

**Limit** — количественное ограничение фичи, отличающееся по планам.

| LimitType | Описание | Проверка |
|-----------|----------|----------|
| `MaxPerCall` | Максимум за один вызов | Локальная (в памяти) |
| `MaxPerSession` | Максимум за сессию AutoCAD | Локальная |
| `MaxSize` | Максимальный размер (байты) | Локальная |
| `MaxPerDay` | Максимум в день (сброс 00:00 UTC) | Через API (кэш 5 мин) |
| `MaxPerMonth` | Максимум в месяц (сброс 1-го числа) | Через API (кэш 5 мин) |

### Trial

Trial — пробный период с полным доступом к плану. Настраивается на каждом плане отдельно:
- `trialDays` — длительность (0 = нет trial)
- `trialBindingMode` — `Account` (один trial на аккаунт) или `AccountAndMachine` (на аккаунт + машину)

---

## Защита кода

### Три способа проверки лицензии

```csharp
// 1. С fallback (рекомендуется)
GrossGeoLicense.Protect(
    action: () => DoWork(),
    onBlocked: () => ShowUpgradeDialog()
);

// 2. С исключением LicenseException
GrossGeoLicense.ProtectOrThrow(() => DoWork());

// 3. Ручная проверка
if (GrossGeoLicense.IsValid)
{
    DoWork();
}
```

### Проверка уровня плана

```csharp
if (GrossGeoLicense.PlanTier >= PlanTier.Pro)
{
    EnableAdvancedTools();
}

if (GrossGeoLicense.BillingModel == BillingModel.Perpetual)
{
    ShowPerpetualBadge();
}
```

---

## Feature Guards

Фичи позволяют гибко управлять функционалом плагина через Developer Portal.
Вы определяете фичи в `plans-manifest.json`, а в коде — проверяете их наличие.

```csharp
// Проверка наличия фичи
if (GrossGeoLicense.HasFeature("advanced-export"))
{
    ShowExportMenu();
}

// Guard с fallback (рекомендуется)
FeatureGuard.Require("batch-processing",
    action: () => ProcessBatch(),
    onMissing: () => ShowUpgradeDialog()
);

// Guard с исключением FeatureNotAvailableException
FeatureGuard.OrThrow("batch-processing", () => ProcessBatch());
```

### Какие фичи доступны кому

```
                    Без лицензии   Free    Pro    Pro+   Trial(Pro)
                    ────────────   ────    ───    ────   ──────────
Не защищено SDK         ✅          ✅      ✅     ✅       ✅
IsDefault=true          ❌          ✅      ✅     ✅       ✅
PlanFeature(Pro)        ❌          ❌      ✅     ✅       ✅
PlanFeature(Pro+)       ❌          ❌      ❌     ✅       ❌
```

> **Совет:** Код, который вы не оборачиваете в `HasFeature` / `FeatureGuard`, работает для всех — включая тех, у кого нет лицензии. Используйте это для демо-функционала.

---

## Feature Limits

Ограничивайте количественные параметры по планам.
Лимиты задаются в `plans-manifest.json` — в `limits` каждой фичи (ключ — код плана).

```csharp
// Получить значение лимита (null = безлимитно)
var limit = GrossGeoLicense.GetFeatureLimit("batch-export", "maxPerCall");
if (limit.HasValue && objects.Count > limit.Value)
{
    ShowMessage($"Лимит: {limit.Value} объектов. Выбрано: {objects.Count}");
    ShowUpgradePrompt();
    return;
}

// Проверка одной строкой (true = не превышен)
if (!GrossGeoLicense.CheckLimit("batch-export", "maxPerCall", objects.Count))
{
    ShowUpgradePrompt("Превышен лимит объектов");
    return;
}

// С исключением LimitExceededException
GrossGeoLicense.RequireLimitOrThrow("batch-export", "maxPerCall", objects.Count);
```

### Graduated Limits (разные лимиты по планам)

В `plans-manifest.json` задайте лимиты внутри фичи — ключ объекта `limits` это код плана:

```json
{
  "features": [
    {
      "code": "export",
      "name": "Экспорт",
      "planCodes": ["free", "pro", "pro-plus"],
      "limits": {
        "free": { "maxPerCall": 10 },
        "pro": { "maxPerCall": 100 }
      }
    }
  ]
}
```

Отсутствие плана в `limits` означает безлимитно (здесь — `pro-plus`). SDK автоматически возвращает лимит текущего плана пользователя.

---

## Usage Tracking (MaxPerDay / MaxPerMonth)

Для дневных и месячных лимитов SDK отслеживает использование через API.

```csharp
[CommandMethod("BATCHEXPORT")]
public void BatchExportCommand()
{
    FeatureGuard.Require("export-batch",
        action: async () =>
        {
            var objects = SelectObjects();

            // 1. Проверка per-call лимита (локальная, мгновенная)
            if (!GrossGeoLicense.CheckLimit("export-batch", "maxPerCall", objects.Count))
            {
                var limit = GrossGeoLicense.GetFeatureLimit("export-batch", "maxPerCall");
                ShowMessage($"Выбрано {objects.Count}, лимит: {limit}");
                ShowUpgradePrompt();
                return;
            }

            // 2. Проверка daily лимита (через API, кэш 5 мин)
            var dailyUsage = await GrossGeoLicense.GetCurrentUsageAsync("export-batch", "maxPerDay");
            var dailyLimit = GrossGeoLicense.GetFeatureLimit("export-batch", "maxPerDay");
            if (dailyLimit.HasValue && dailyUsage + objects.Count > dailyLimit.Value)
            {
                ShowMessage($"Дневной лимит: {dailyLimit.Value}, использовано: {dailyUsage}");
                ShowUpgradePrompt();
                return;
            }

            // 3. Выполнение операции
            ExportObjects(objects);

            // 4. Инкремент usage ПОСЛЕ успешного выполнения
            await GrossGeoLicense.IncrementUsageAsync("export-batch", "maxPerDay", objects.Count);
        },
        onMissing: () => ShowUpgradePrompt()
    );
}
```

### API

```csharp
// Инкрементировать usage (вызывать после успешного выполнения)
Task<UsageResult> IncrementUsageAsync(string featureCode, string limitCode, int count = 1);

// Получить текущее использование
Task<int> GetCurrentUsageAsync(string featureCode, string limitCode);
```

`UsageResult` содержит:
- `IsSuccess` — успешно ли обновлено
- `CurrentUsage` — текущее значение после инкремента
- `Limit` — лимит (если задан)
- `Remaining` — сколько осталось

---

## Concurrent Sessions

Для лицензий с плавающими слотами (`LicenseMode.Concurrent`):

```csharp
// Получение сессии при запуске плагина
var session = await GrossGeoLicense.AcquireSessionAsync(
    clientInfo: Environment.MachineName);

if (!session.IsSuccess)
{
    ShowMessage($"Все слоты заняты: {session.ErrorMessage}");
    return;
}

// Heartbeat отправляется автоматически каждые 5 минут

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

### Сценарии

| Ситуация | Что происходит |
|----------|----------------|
| Нормальная работа | AcquireSession → Heartbeat каждые 5 мин → ReleaseSession |
| Crash / BSOD | Heartbeat прекращается → через 15 мин сессия автоматически освобождается |
| Все слоты заняты | AcquireSession → ошибка `max_sessions_reached` со списком активных сессий |
| Принудительное освобождение | Владелец в User Panel → "Завершить сессию" → плагин получает `session_terminated` |

---

## Офлайн-режим (Graceful Degradation)

SDK автоматически кэширует результат лицензии (DPAPI). При недоступности User Panel:

| Ситуация | Поведение |
|----------|-----------|
| User Panel не запущен | SDK пробует из кэша (до 7 дней) |
| User Panel запущен, но нет интернета | User Panel работает из своего кэша |
| Кэш устарел (>7 дней) | `IsValid = false`, плагин работает в Free-режиме |

Для `Machine` режима доступен расширенный офлайн: до 30 дней по запросу.

---

## Проверка обновлений

```csharp
var update = await GrossGeoLicense.CheckForUpdatesAsync();
if (update.HasUpdate)
{
    ShowMessage($"Доступна версия {update.Version}");
    ShowMessage($"Что нового: {update.Changelog}");
}
```

> Для Perpetual без Maintenance обновления ограничены `VersionLockedAt` — версией, зафиксированной при покупке или при истечении Maintenance.

---

## LicenseOptions

```csharp
new LicenseOptions
{
    ProductKey = "GG-XXXX-XXXX-XXXX-XXXX",  // Обязательно
    PluginVersion = "1.0.0",                 // Версия вашего плагина
    CacheDirectory = null,                   // Каталог кэша (null = по умолчанию)
    GracePeriodDays = 7,                     // Сколько дней работать из кэша
    IpcTimeoutSeconds = 5,                   // Таймаут IPC-запроса
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
| `Enterprise` (99) | Зарезервировано |

#### BillingModel

| Значение | Описание |
|----------|----------|
| `Free` (0) | Бесплатно |
| `Subscription` (1) | Подписка (месяц / год) |
| `Perpetual` (2) | Бессрочная лицензия |

#### LicenseMode

| Значение | Описание |
|----------|----------|
| `User` (0) | Привязка к пользователю (seat) |
| `Machine` (1) | Привязка к машине (fingerprint) |
| `Concurrent` (2) | Плавающие лицензии (пул сессий) |

#### LimitType

| Значение | Описание | Проверка |
|----------|----------|----------|
| `MaxPerCall` (0) | За один вызов | Локальная |
| `MaxPerSession` (1) | За сессию AutoCAD | Локальная |
| `MaxSize` (2) | Размер (байты) | Локальная |
| `MaxPerDay` (3) | За день (сброс 00:00 UTC) | Через API |
| `MaxPerMonth` (4) | За месяц (сброс 1-го числа) | Через API |

### GrossGeoLicense — свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `IsInitialized` | `bool` | SDK инициализирован |
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
| `Shutdown()` | Завершение работы |
| `Shutdown(string productKey)` | Завершение работы конкретного продукта |
| `ForProduct(string productKey)` | Получить `ProductLicenseAccessor` для продукта |
| `Check()` | Быстрая проверка (из памяти) |
| `CheckAsync(CancellationToken)` | Полная проверка (запрос к User Panel) |
| `RefreshAsync(CancellationToken)` | Принудительное обновление |
| `Protect(Action, Action?)` | Защита блока с fallback |
| `ProtectOrThrow(Action)` | Защита с исключением |
| `HasFeature(string)` | Проверка фичи |
| `HasFeatureAsync(string, CancellationToken)` | Проверка фичи (с обновлением) |
| `RequireFeature(string, Action, Action?)` | Фича с fallback |
| `RequireFeatureOrThrow(string, Action)` | Фича с исключением |
| `GetFeatureLimit(string, string)` | Получить лимит |
| `CheckLimit(string, string, int)` | Проверить лимит |
| `RequireLimit(string, string, int, Action, Action?)` | Лимит с fallback |
| `RequireLimitOrThrow(string, string, int)` | Лимит с исключением |
| `IncrementUsageAsync(string, string, int, CancellationToken)` | Инкремент usage (MaxPerDay/MaxPerMonth) |
| `GetCurrentUsageAsync(string, string, CancellationToken)` | Текущий usage |
| `AcquireSessionAsync(string?, CancellationToken)` | Получить concurrent-сессию |
| `ReleaseSessionAsync(CancellationToken)` | Освободить сессию |
| `CheckForUpdatesAsync(CancellationToken)` | Проверка обновлений |
| `ClearLocalCache()` | Очистить кэш |

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
| `Features` | `IReadOnlyList<string>` | Доступные фичи |
| `FeatureLimits` | `IReadOnlyDictionary<string, int>?` | Лимиты (ключ: `featureCode.limitCode`) |
| `ExpiresAt` | `DateTime?` | Дата истечения |
| `DaysRemaining` | `int?` | Дней до истечения |
| `Message` | `string?` | Сообщение для пользователя |

---

## Манифесты продукта

Продукт описывается **двумя** манифестами рядом с проектом — отдельно «что продаём» и отдельно «что выкладываем»:

| Файл | Назначение | Где используется на платформе |
|------|------------|-------------------------------|
| `plans-manifest.json` | Планы, фичи и лимиты продукта | `POST /api/developer/products/{id}/import-manifest/v2` (bulk-импорт) |
| `release-manifest.json` | Метаданные **одного** релиза (версия, канал, дистрибутив) | читается сервером при загрузке файлов релиза |

> ⚠️ **`productKey` в манифестах запрещён.** Единственный источник истины по ключу продукта — `LicenseOptions.ProductKey` в вашей DLL (см. раздел Multi-plugin ниже). Поля `productKey` ни в одном манифесте быть не должно.
>
> Метаданные продукта (имя, описание, теги, режим лицензирования `GrossGeo`/`ExternalOnly`, внешние ссылки) задаются в **Developer Portal** при создании продукта — в манифестах их больше нет.

JSON-схемы (на платформе): `docs/schemas/plans-manifest.v1.json`, `docs/schemas/release-manifest.v1.json`. Готовые примеры — у каждого продукта в [`samples/`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples) лежат собственные `plans-manifest.json` + `release-manifest.json`.

### `plans-manifest.json`

```json
{
  "$schema": "https://grossgeo.example/schemas/plans-manifest.v1.json",
  "manifestVersion": 1,
  "plans": [
    {
      "code": "pro",
      "name": "Pro",
      "displayName": "Стандартный",
      "description": "Стандартный набор инструментов",
      "highlights": ["Экспорт данных", "Базовая обработка"],
      "planTier": "Pro",
      "billingModel": "Subscription",
      "licenseMode": "User",
      "monthlyPrice": 990,
      "yearlyPrice": 9900,
      "oneTimePrice": null,
      "maxSeats": 1,
      "maxConcurrentSessions": null,
      "trialDays": 14,
      "trialBindingMode": "Account"
    }
  ],
  "features": [
    { "code": "basic", "name": "Базовый функционал", "isDefault": true, "planCodes": ["pro"] },
    {
      "code": "export",
      "name": "Экспорт",
      "planCodes": ["pro"],
      "limits": { "pro": { "maxPerMonth": 100 } }
    }
  ]
}
```

#### `plans[]` — тарифные планы

| Поле | Тип | Обяз. | Описание |
|------|-----|:-----:|----------|
| `code` | `string` | ✅ | Код плана, уникален в продукте (`^[a-z0-9-]+$`) |
| `name` | `string` | ✅ | Техническое имя |
| `planTier` | `enum` | ✅ | `Free` \| `Pro` \| `ProPlus` |
| `billingModel` | `enum` | ✅ | `Free` \| `Subscription` \| `Perpetual` (`Free` — только для Free-плана) |
| `maxSeats` | `int` | ✅ | Кол-во рабочих мест (≥ 1) |
| `displayName` | `string?` | — | Отображаемое имя в UI |
| `description` | `string?` | — | Описание плана |
| `highlights` | `string[]?` | — | Маркетинговые буллеты |
| `licenseMode` | `enum?` | — | `User` \| `Machine` \| `Concurrent` |
| `monthlyPrice` / `yearlyPrice` | `number?` | — | Цены подписки (₽) |
| `oneTimePrice` / `maintenanceYearlyPrice` | `number?` | — | Цены Perpetual (₽) |
| `maxConcurrentSessions` | `int?` | — | Лимит одновременных сессий (для `Concurrent`) |
| `trialDays` | `int?` | — | Trial для плана (0 = без trial) |
| `trialBindingMode` | `enum?` | — | `Account` \| `AccountAndMachine` |

> По сравнению со старым единым `product-manifest.json`: `tier` → `planTier`; убраны `currency`, `isActive`, `badge`, `isRecommended` (схема `additionalProperties: false` — лишние поля не пройдут импорт).

#### `features[]` — фичи (с привязкой к планам и лимитами)

Фича сама несёт привязку к планам (`planCodes`) и лимиты (`limits`, ключ — код плана). Отдельных секций `planFeatures` и `featureLimits` больше нет.

| Поле | Тип | Обяз. | Описание |
|------|-----|:-----:|----------|
| `code` | `string` | ✅ | Код фичи (`^[a-z0-9-_]+$`), используется в SDK |
| `name` | `string` | ✅ | Название |
| `description` | `string?` | — | Описание |
| `isDefault` | `bool?` | — | Включена во все планы по умолчанию |
| `planCodes` | `string[]?` | — | Коды планов, к которым привязана фича (каждый должен существовать в `plans[]`) |
| `limits` | `object?` | — | Лимиты по планам: ключ — код плана, значение — `{ maxPerCall?, maxPerDay?, maxPerMonth?, maxTotal? }` |

> Фичи **не наследуются** между планами — перечисляйте каждый план в `planCodes` явно.

### `release-manifest.json`

Один файл — один релиз. При выходе новой версии публикуется новый `release-manifest.json` (история релизов хранится на платформе).

```json
{
  "$schema": "https://grossgeo.example/schemas/release-manifest.v1.json",
  "manifestVersion": 1,
  "version": "1.0.0",
  "channel": "Stable",
  "distributionType": "Bundle",
  "changelog": "Первый релиз.",
  "postInstallAction": "RequireRestart",
  "netloadDllPath": "Contents/MyPlugin.dll",
  "minAutoCADVersion": "R24.4",
  "maxAutoCADVersion": "R25.0",
  "targetPlatforms": ["AutoCAD", "Civil3D"],
  "supportedOS": ["Win64"],
  "loadOnStartup": true
}
```

| Поле | Тип | Обяз. | Описание |
|------|-----|:-----:|----------|
| `manifestVersion` | `1` | ✅ | Версия схемы |
| `version` | `string` | ✅ | SemVer: `1.2.0` или `1.2.0.0` |
| `channel` | `enum` | ✅ | `Stable` \| `Beta` \| `Internal` |
| `distributionType` | `enum` | ✅ | `PluginDll` (одна DLL) \| `Bundle` (.bundle.zip) \| `Installer` (MSI/EXE) |
| `changelog` | `string?` | — | Изменения релиза (Markdown) |
| `postInstallAction` | `enum?` | — | `RequireRestart` \| `CanNetload` |
| `netloadDllPath` | `string?` | — | Путь к DLL внутри bundle (обязателен при `CanNetload`) |
| `minAutoCADVersion` / `maxAutoCADVersion` | `string?` | — | Диапазон версий AutoCAD в R-формате (`R24.4`, `R25.0`) |
| `targetPlatforms` | `string[]?` | — | `AutoCAD`, `Civil3D`, … |
| `supportedOS` | `string[]?` | — | Напр. `["Win64"]` |
| `loadOnStartup` / `loadOnCommandInvocation` | `bool?` | — | Стратегия загрузки плагина |
| `commands` | `string[]?` | — | Команды AutoCAD, регистрируемые плагином |
| `targetRuntimes` | `string[]?` | — | Напр. `["net48", "net8.0-windows"]` |

> Поля `fixtureFile` и блока `expectations` в схеме нет — это была тест-метаданная старого формата.

### Примеры по сценариям (фрагменты `plans-manifest.json`)

**Perpetual + Maintenance:**
```json
{ "code": "pro", "name": "Pro", "planTier": "Pro", "billingModel": "Perpetual", "licenseMode": "Machine",
  "oneTimePrice": 9990, "maintenanceYearlyPrice": 2990, "maxSeats": 1,
  "description": "Бессрочная лицензия", "highlights": ["Покупка навсегда", "Обновления с Maintenance"] }
```

**Concurrent (плавающие лицензии):**
```json
{ "code": "team", "name": "Team", "planTier": "ProPlus", "billingModel": "Subscription", "licenseMode": "Concurrent",
  "monthlyPrice": 1500, "yearlyPrice": 15000, "maxSeats": 10, "maxConcurrentSessions": 10,
  "description": "Для команд", "highlights": ["10 одновременных пользователей"] }
```

**Freemium (Free + Pro с лимитом на фиче):**
```json
{
  "plans": [
    { "code": "free", "name": "Free", "planTier": "Free", "billingModel": "Free", "maxSeats": 1 },
    { "code": "pro",  "name": "Pro",  "planTier": "Pro",  "billingModel": "Subscription", "maxSeats": 1, "monthlyPrice": 490, "yearlyPrice": 4900 }
  ],
  "features": [
    { "code": "export", "name": "Экспорт", "planCodes": ["free", "pro"],
      "limits": { "free": { "maxPerCall": 10 } } }
  ]
}
```

**ExternalOnly (лицензирование вне платформы):** `plans-manifest.json` с пустыми `plans`/`features`; внешние ссылки (`externalPurchaseUrl` и т.п.) задаются при создании продукта в Developer Portal:
```json
{ "$schema": "https://grossgeo.example/schemas/plans-manifest.v1.json", "manifestVersion": 1, "plans": [], "features": [] }
```

### Полные примеры

Все примеры доступны в каталоге [`samples/`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples):

| Пример | Сценарий |
|--------|----------|
| `TestProduct.Free` | Бесплатный продукт |
| `TestProduct.Licensed` | Perpetual + Maintenance |
| `TestProduct.Subscription` | Multi-tier подписка с trial |
| `TestProduct.Freemium` | Free + Pro с Feature Limits |
| `TestProduct.Concurrent` | Плавающие лицензии (Concurrent) |
| `TestProduct.Analytics` | Внешнее лицензирование (ExternalOnly) |
| `TestProduct.Installer` | Установка через EXE/MSI |
| `TestProduct.PluginDll` | Одиночная DLL (PluginDll) |

---

## Multi-plugin (несколько плагинов в одном процессе)

Когда несколько плагинов работают в одном процессе AutoCAD, статические свойства
`GrossGeoLicense.IsValid`, `GrossGeoLicense.PlanTier` и т.д. возвращают данные
**последнего** инициализированного продукта. Это приводит к конфликтам.

### Решение: `ProductLicenseAccessor`

Используйте `GrossGeoLicense.ForProduct(productKey)` для получения accessor,
привязанного к конкретному продукту:

```csharp
public class MyPlugin : IExtensionApplication
{
    private const string ProductKey = "GG-XXXX-XXXX-XXXX-XXXX";
    private static ProductLicenseAccessor? _license;

    public void Initialize()
    {
        _ = Task.Run(async () =>
        {
            await GrossGeoLicense.Initialize(new LicenseOptions
            {
                ProductKey = ProductKey,
                PluginVersion = "1.0.0"
            });

            _license = GrossGeoLicense.ForProduct(ProductKey);
        });
    }

    public void Terminate()
    {
        GrossGeoLicense.Shutdown(ProductKey);
    }

    [CommandMethod("MY_CMD")]
    public void MyCmd()
    {
        var lic = _license;
        if (lic == null) return;

        if (lic.IsValid)
        {
            // Свойства: lic.PlanTier, lic.BillingModel, lic.ExpiresAt ...
        }

        lic.Protect(() => DoWork(), () => ShowUpgrade());

        if (lic.HasFeature("export")) { DoExport(); }

        lic.RequireFeature("batch",
            action: () => ProcessBatch(),
            onMissing: () => ShowUpgrade());

        var limit = lic.GetFeatureLimit("export", "maxPerCall");
        if (lic.CheckLimit("export", "maxPerCall", objects.Count))
        {
            DoExport(objects);
        }
    }
}
```

### ProductLicenseAccessor — свойства

Accessor предоставляет те же свойства, что и статический `GrossGeoLicense`, но изолированно для конкретного продукта:

| Свойство | Тип | Описание |
|----------|-----|----------|
| `ProductKey` | `string` | Ключ продукта |
| `IsValid` | `bool` | Лицензия валидна |
| `PlanTier` | `PlanTier` | Уровень плана |
| `BillingModel` | `BillingModel` | Модель оплаты |
| `LicenseMode` | `LicenseMode` | Режим лицензирования |
| `ExpiresAt` | `DateTime?` | Дата истечения |
| `DaysRemaining` | `int?` | Дней до истечения |
| `IsInGracePeriod` | `bool` | В grace period |
| `IsOfflineMode` | `bool` | Работа из кэша |
| `Features` | `IReadOnlyList<string>` | Доступные features |
| `FeatureLimits` | `IReadOnlyDictionary<string, int>?` | Лимиты фичей |
| `HasActiveSession` | `bool` | Есть concurrent-сессия |
| `SessionToken` | `string?` | Токен concurrent-сессии |
| `LastResult` | `LicenseResult?` | Последний результат |

### ProductLicenseAccessor — методы

| Метод | Описание |
|-------|----------|
| `Check()` | Быстрая проверка из кэша |
| `CheckAsync(CancellationToken)` | Полная проверка (запрос к User Panel) |
| `RefreshAsync(CancellationToken)` | Принудительное обновление |
| `HasFeature(string)` | Проверка фичи |
| `GetFeatureLimit(string, string)` | Получить лимит |
| `CheckLimit(string, string, int)` | Проверить лимит |
| `Protect(Action, Action?)` | Защита с fallback |
| `Protect<T>(Func<T>, Func<T>?)` | Защита с возвратом значения |
| `ProtectOrThrow(Action)` | Защита с исключением |
| `RequireFeature(string, Action, Action?)` | Фича с fallback |
| `RequireFeatureOrThrow(string, Action)` | Фича с исключением |
| `RequireLimit(string, string, int, Action, Action<int>?)` | Лимит с fallback |

### Миграция с GrossGeoLicense на ProductLicenseAccessor

| До (статический) | После (per-product) |
|-------------------|---------------------|
| `GrossGeoLicense.IsValid` | `_license?.IsValid ?? false` |
| `GrossGeoLicense.HasFeature("x")` | `_license?.HasFeature("x") ?? false` |
| `GrossGeoLicense.Protect(...)` | `_license?.Protect(...)` |
| `GrossGeoLicense.GetFeatureLimit(...)` | `_license?.GetFeatureLimit(...)` |
| `FeatureGuard.Require("x", ...)` | `_license?.RequireFeature("x", ...)` |
| `GrossGeoLicense.Shutdown()` | `GrossGeoLicense.Shutdown(ProductKey)` |
| `GrossGeoLicense.CheckAsync()` | `_license?.CheckAsync()` |
| `GrossGeoLicense.RefreshAsync()` | `_license?.RefreshAsync()` |

> **Важно:** Статические свойства `GrossGeoLicense.IsValid`, `GrossGeoLicense.PlanTier` и т.д.
> по-прежнему работают для обратной совместимости, но возвращают данные **последнего**
> инициализированного продукта. Для multi-plugin сценариев всегда используйте `ForProduct()`.

---

## Примеры плагинов

| Пример | Описание |
|--------|----------|
| [`TestProduct.Free`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Free) | Бесплатный плагин без лицензии |
| [`TestProduct.Licensed`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Licensed) | Perpetual + Maintenance |
| [`TestProduct.Subscription`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Subscription) | Multi-tier подписочная модель |
| [`TestProduct.Freemium`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Freemium) | Free-план + платные фичи с лимитами |
| [`TestProduct.Concurrent`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Concurrent) | Плавающие лицензии (Concurrent) |
| [`TestProduct.Analytics`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Analytics) | Телеметрия и аналитика (ExternalOnly) |
| [`TestProduct.Installer`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.Installer) | Установка через EXE/MSI |
| [`TestProduct.PluginDll`](https://github.com/2805028/Grossgeo-Platform-SDK/tree/main/samples/TestProduct.PluginDll) | Одиночная DLL (PluginDll) |

---

## FAQ

### Нужно ли создавать отдельные планы для Monthly и Yearly?

**Нет.** Один план хранит `monthlyPrice` и `yearlyPrice`. Пользователь выбирает период при покупке через toggle в UI.

### Как моделировать Maintenance для Perpetual?

Добавьте `maintenanceYearlyPrice` на Perpetual-план. Отдельный Maintenance-план **не нужен**.

### Что такое `IsDefault` у фичи?

Фича с `IsDefault = true` доступна во **всех планах** для авторизованных пользователей (включая Free). Используйте для базового функционала, который работает без покупки платного плана.

### Что если код не обёрнут в HasFeature / FeatureGuard?

Код работает для **всех** — включая пользователей без лицензии. Если вы хотите защитить функционал, оберните его в `HasFeature` или `FeatureGuard`.

### Как отслеживать дневные/месячные лимиты?

Используйте `IncrementUsageAsync` после успешного выполнения операции и `GetCurrentUsageAsync` для проверки текущего использования. Сброс: `MaxPerDay` — 00:00 UTC, `MaxPerMonth` — 1-е число.

### Что если User Panel не запущен?

SDK работает из кэша до 7 дней. После — `IsValid = false`, плагин переходит в Free-режим.

---

## Поддержка

- Документация: [Grossgeo-Platform-SDK](https://github.com/2805028/Grossgeo-Platform-SDK)
- Developer Portal: [grossgeo.ru/developer](https://grossgeo.ru/developer)
- Вопросы: [GitHub Issues](https://github.com/2805028/Grossgeo-Platform-SDK/issues)

---

## Лицензия

Proprietary. © 2025 GrossGeo. All rights reserved.
