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
- [product-manifest.json](#product-manifestjson)
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
<PackageReference Include="GrossGeo.SDK.Stub" Version="2.*" />
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
    public void Initialize()
    {
        Task.Run(async () =>
        {
            var result = await GrossGeoLicense.Initialize(new LicenseOptions
            {
                ProductKey = "GG-XXXX-XXXX-XXXX-XXXX",
                PluginVersion = "1.0.0"
            });

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
        GrossGeoLicense.Shutdown();
    }
}
```

### 3. Защита команды

```csharp
[CommandMethod("MYCOMMAND")]
public void MyCommand()
{
    GrossGeoLicense.Protect(
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
Все фичи нижнего плана **должны** быть явно включены в верхний (через `planFeatures`).

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
- `IsDefault = false` — фича доступна только в планах, где она явно добавлена через `planFeatures`
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
Вы определяете фичи в `product-manifest.json`, а в коде — проверяете их наличие.

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
Лимиты задаются в `product-manifest.json` в секции `featureLimits`.

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

В `product-manifest.json` задайте разные лимиты для разных планов:

```json
{
  "featureLimits": {
    "free.export": [
      { "limitCode": "maxPerCall", "limitType": "MaxPerCall", "limitValue": 10 }
    ],
    "pro.export": [
      { "limitCode": "maxPerCall", "limitType": "MaxPerCall", "limitValue": 100 }
    ],
    "pro-plus.export": null
  }
}
```

`null` означает безлимитно. SDK автоматически возвращает лимит текущего плана пользователя.

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

## product-manifest.json

Манифест описывает ваш продукт, планы, фичи, лимиты и релизы.
Создаётся в корне проекта и используется для регистрации продукта на платформе.

### Минимальный пример

```json
{
  "$schema": "./product-manifest.schema.json",

  "product": {
    "name": "My AutoCAD Plugin",
    "slug": "my-autocad-plugin",
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
      "maxAutoCADVersion": "R25.0",
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
| `licensingMode` | `string` | ✅ | `GrossGeo` или `ExternalOnly` |
| `trialDays` | `int` | ✅ | Trial-период по умолчанию (0 = без trial) |
| `tags` | `string[]` | — | Теги для поиска в каталоге |
| `externalPurchaseUrl` | `string?` | — | URL покупки (для ExternalOnly) |
| `externalDownloadUrl` | `string?` | — | URL скачивания (для ExternalOnly) |
| `externalLicenseInstructions` | `string?` | — | Инструкция активации (для ExternalOnly) |

#### `plans[]` — тарифные планы

| Поле | Тип | Описание |
|------|-----|----------|
| `code` | `string` | Уникальный код плана (латиница, дефисы) |
| `name` | `string` | Системное имя |
| `displayName` | `string` | Отображаемое имя в UI |
| `tier` | `string` | Уровень: `Free`, `Pro`, `ProPlus` |
| `billingModel` | `string` | Модель: `Free`, `Subscription`, `Perpetual` |
| `licenseMode` | `string` | Режим: `User`, `Machine`, `Concurrent` |
| `monthlyPrice` | `decimal?` | Цена за месяц (для Subscription) |
| `yearlyPrice` | `decimal?` | Цена за год (для Subscription) |
| `oneTimePrice` | `decimal?` | Разовая цена (для Perpetual) |
| `maintenanceYearlyPrice` | `decimal?` | Maintenance за год (для Perpetual, null = недоступно) |
| `currency` | `string` | Валюта (`RUB`) |
| `maxSeats` | `int` | Количество рабочих мест |
| `maxConcurrentSessions` | `int?` | Макс. concurrent сессий |
| `trialDays` | `int` | Trial для этого плана (0 = без trial) |
| `trialBindingMode` | `string?` | `Account` или `AccountAndMachine` |
| `description` | `string?` | Описание плана для UI (рекомендуется для платных) |
| `highlights` | `string[]?` | Маркетинговые буллеты для pricing table |
| `badge` | `string?` | Бейдж ("Популярный", "Лучшая цена") |
| `isRecommended` | `bool?` | Подсветить как рекомендуемый |
| `isActive` | `bool` | Активен ли план |

#### `features[]` — фичи продукта

| Поле | Тип | Описание |
|------|-----|----------|
| `code` | `string` | Уникальный код фичи (используется в SDK) |
| `name` | `string` | Название |
| `description` | `string` | Описание |
| `isDefault` | `bool` | Включена во все планы по умолчанию |

#### `planFeatures` — привязка фичей к планам

```json
{
  "planFeatures": {
    "free": ["basic-tools"],
    "pro": ["basic-tools", "export", "batch"],
    "pro-plus": ["basic-tools", "export", "batch", "cloud-sync", "api"]
  }
}
```

> **Важно:** фичи **не наследуются** автоматически. Все фичи нижних планов нужно явно включить в верхние.

#### `featureLimits` — ограничения фичей по планам

Ключ: `{planCode}.{featureCode}`, значение: массив лимитов или `null` (безлимитно).

```json
{
  "featureLimits": {
    "free.export": [
      { "limitCode": "maxPerCall", "limitType": "MaxPerCall", "limitValue": 10 },
      { "limitCode": "maxPerDay", "limitType": "MaxPerDay", "limitValue": 50 }
    ],
    "pro.export": [
      { "limitCode": "maxPerCall", "limitType": "MaxPerCall", "limitValue": 100 }
    ],
    "pro-plus.export": null
  }
}
```

#### `releases[]` — релизы продукта

| Поле | Тип | Описание |
|------|-----|----------|
| `version` | `string` | Версия (SemVer) |
| `channel` | `string` | Канал: `Stable`, `Beta`, `Alpha` |
| `changelog` | `string` | Описание изменений |
| `distributionType` | `string` | `Bundle` (Autoloader), `Installer` (EXE), `PluginDll` (одна DLL) |
| `postInstallAction` | `string` | `RequireRestart` или `None` |
| `netloadDllPath` | `string?` | Путь к DLL внутри bundle |
| `minAutoCADVersion` | `string` | Мин. серия AutoCAD |
| `maxAutoCADVersion` | `string` | Макс. серия AutoCAD |
| `targetPlatforms` | `string[]` | `AutoCAD`, `Civil3D`, `Map` |
| `supportedOS` | `string[]` | `Win64` |
| `loadOnStartup` | `bool` | Загружать при старте AutoCAD |
| `fixtureFile` | `string` | Путь к архиву релиза |

### Примеры сценариев

#### Подписка с trial (Monthly + Yearly на одном плане)

```json
{
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
      "highlights": ["Экспорт данных", "Приоритетная поддержка"]
    }
  ]
}
```

> Один план — пользователь выбирает месяц или год при покупке (toggle в UI). Экономия рассчитывается автоматически.

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

> `maintenanceYearlyPrice` — необязательный аддон. Если `null`, Maintenance недоступен и пользователь получает только ту версию, которую купил.

#### Concurrent (плавающие лицензии)

```json
{
  "plans": [
    {
      "code": "team",
      "tier": "ProPlus",
      "billingModel": "Subscription",
      "licenseMode": "Concurrent",
      "monthlyPrice": 1500,
      "yearlyPrice": 15000,
      "maxSeats": 10,
      "maxConcurrentSessions": 10,
      "description": "Для команд с плавающими лицензиями",
      "highlights": ["10 одновременных пользователей", "Enterprise API"]
    }
  ]
}
```

#### Freemium (Free + Pro с лимитами)

```json
{
  "plans": [
    {
      "code": "free",
      "tier": "Free",
      "billingModel": "Free",
      "description": "Базовые инструменты бесплатно",
      "highlights": ["Базовые инструменты", "Экспорт (до 10 объектов)"]
    },
    {
      "code": "pro",
      "tier": "Pro",
      "billingModel": "Subscription",
      "monthlyPrice": 490,
      "yearlyPrice": 4900,
      "description": "Полный набор инструментов",
      "highlights": ["Безлимитный экспорт", "Пакетная обработка"]
    }
  ],
  "featureLimits": {
    "free.export": [{ "limitCode": "maxPerCall", "limitType": "MaxPerCall", "limitValue": 10 }],
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
    "externalLicenseInstructions": "Получите ключ на сайте и введите в настройках плагина"
  },
  "plans": [],
  "features": [],
  "planFeatures": {},
  "featureLimits": {}
}
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
