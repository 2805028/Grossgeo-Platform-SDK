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
<PackageReference Include="GrossGeo.SDK.Stub" Version="1.*" />
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
                // result.PlanTier  — уровень плана (Free, Pro, ProPlus)
                // result.BillingModel — модель оплаты (Subscription, Perpetual)
                // result.ExpiresAt — дата истечения лицензии
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

## Защита кода

### Три способа проверки лицензии

```csharp
// 1. С fallback
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

```csharp
// Проверка наличия фичи
if (GrossGeoLicense.HasFeature("advanced-export"))
{
    ShowExportMenu();
}

// Guard с fallback
FeatureGuard.Require("batch-processing",
    action: () => ProcessBatch(),
    onMissing: () => ShowUpgradeDialog()
);

// Guard с исключением FeatureNotAvailableException
FeatureGuard.OrThrow("batch-processing", () => ProcessBatch());
```

### Публичные фичи

Фичи с `IsPublic=true` доступны без лицензии:

```csharp
await GrossGeoLicense.LoadPublicFeaturesAsync(productId);

if (GrossGeoLicense.HasFeature("view-objects"))
{
    ShowViewer(); // Работает даже без лицензии
}
```

---

## Feature Limits

Ограничивайте количественные параметры по планам:

```csharp
// Проверка лимита
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
if (GrossGeoLicense.IsInGracePeriod)
{
    ShowWarning($"Офлайн-режим. Осталось дней: {GrossGeoLicense.DaysRemaining}");
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
| `Maintenance` (10) | Право на обновления (аддон к Perpetual) |
| `Enterprise` (99) | Индивидуальный контракт |

#### BillingModel

| Значение | Описание |
|----------|----------|
| `Free` (0) | Бесплатно |
| `Subscription` (1) | Подписка |
| `Perpetual` (2) | Бессрочная лицензия |
| `Contract` (3) | Индивидуальный контракт |

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
| `RequireFeature(string, Action, Action?)` | Фича с fallback |
| `RequireFeatureOrThrow(string, Action)` | Фича с исключением |
| `LoadPublicFeaturesAsync(Guid, CancellationToken)` | Загрузить публичные фичи |
| `GetFeatureLimit(string, string)` | Получить лимит |
| `CheckLimit(string, string, int)` | Проверить лимит |
| `RequireLimitOrThrow(string, string, int)` | Лимит с исключением |
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
| `LicenseId` | `Guid?` | ID лицензии |
| `ExpiresAt` | `DateTime?` | Дата истечения |
| `Features` | `IReadOnlyList<string>` | Доступные features |
| `FeatureLimits` | `IReadOnlyDictionary<string, int>?` | Лимиты фичей |
| `Message` | `string` | Сообщение для пользователя |
| `IsInGracePeriod` | `bool` | В grace period |
| `IsOfflineMode` | `bool` | Офлайн-режим |

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

---

## Поддержка

- Документация: [Grossgeo-Platform-SDK](https://github.com/2805028/Grossgeo-Platform-SDK)
- Developer Portal: [grossgeo.ru/developer](https://grossgeo.ru/developer)
- Вопросы: [GitHub Issues](https://github.com/2805028/Grossgeo-Platform-SDK/issues)

---

## Лицензия

Proprietary. © 2025 GrossGeo. All rights reserved.
