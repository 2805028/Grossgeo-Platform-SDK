# SDK Migration Guide: v1 → v2

**Дата:** Февраль 2025  
**Для:** Разработчиков AutoCAD-плагинов, использующих GrossGeo SDK

---

## Обзор

SDK v2 вводит новую модель лицензирования: **PlanTier + BillingModel** вместо единого `LicenseType`. Это даёт разработчикам гибкость в настройке планов, лимитов и режимов лицензирования.

### Что нового в v2

| Возможность | v1 | v2 |
|-------------|----|----|
| Типы лицензий | `LicenseType` (Trial, Standard, Subscription, Enterprise) | `PlanTier` (Free, Pro, ProPlus, Enterprise) + `BillingModel` (Free, Subscription, Perpetual) |
| Режимы привязки | Только Machine | `LicenseMode` (User, Machine, Concurrent) |
| Feature Limits | ❌ | `GetFeatureLimit()`, `CheckLimit()`, `RequireLimitOrThrow()` |
| Public Features | ❌ | `LoadPublicFeaturesAsync()`, `IsPublicFeature()` ❌ удалено в v3 |
| Concurrent Sessions | ❌ | `AcquireSessionAsync()`, `ReleaseSessionAsync()`, heartbeat |
| Кэш | AES (hardcoded key) | DPAPI (Windows Data Protection) |
| IPC Protocol | v3 | v4 (версионирование, rate limiting) |

### Обратная совместимость

- `LicenseType` сохранён с атрибутом `[Obsolete]` — код компилируется с предупреждением.
- Все методы v1 (`Protect`, `HasFeature`, `RequireFeature`, `CheckForUpdatesAsync`) работают без изменений.
- `Initialize()` и `Shutdown()` не изменились.

---

## 1. Обновление зависимостей

### PackageReference

```xml
<!-- БЫЛО -->
<PackageReference Include="GrossGeo.SDK.Stub" Version="1.*" />

<!-- СТАЛО -->
<PackageReference Include="GrossGeo.SDK.Stub" Version="2.1.17" />
```

Версия закрепляется точно. Диапазон `2.*` разрешается в любую версию ветки 2 — в том числе в
снятые с витрины, где проверка лицензии работала неверно: unlist не удаляет пакет, и сборка
получила бы такую версию молча.

### Новая зависимость

SDK v2 ссылается на `GrossGeo.Contracts` (netstandard2.0) — подключается автоматически через NuGet.

При ручной установке скопируйте оба файла:
- `GrossGeo.SDK.Stub.dll`
- `GrossGeo.Contracts.dll`

---

## 2. Замена LicenseType

### Добавить using

```csharp
using GrossGeo.Contracts.Licensing;
```

### Маппинг LicenseType → PlanTier + BillingModel

| LicenseType (v1) | PlanTier (v2) | BillingModel (v2) |
|-------------------|---------------|-------------------|
| `None` | `Free` | `Free` |
| `Trial` | `Free` | `Free` |
| `Standard` | `Pro` | `Perpetual` |
| `Subscription` | `Pro` | `Subscription` |
| `Enterprise` | `Enterprise` | `Contract` ⚠️ `BillingModel.Contract` помечен `[Obsolete]` в v3 (`Enterprise` зарезервирован) |

### Примеры замены

```csharp
// ─── Проверка "есть ли платная лицензия" ───

// БЫЛО
if (GrossGeoLicense.LicenseType != LicenseType.None &&
    GrossGeoLicense.LicenseType != LicenseType.Trial)

// СТАЛО
if (GrossGeoLicense.PlanTier >= PlanTier.Pro)
```

```csharp
// ─── Проверка типа подписки ───

// БЫЛО
if (GrossGeoLicense.LicenseType == LicenseType.Subscription)

// СТАЛО
if (GrossGeoLicense.BillingModel == BillingModel.Subscription)
```

```csharp
// ─── Проверка trial ───

// БЫЛО
if (GrossGeoLicense.LicenseType == LicenseType.Trial)

// СТАЛО
if (GrossGeoLicense.PlanTier == PlanTier.Free && GrossGeoLicense.IsValid)
// Или проверить через LicenseResult:
if (GrossGeoLicense.LastResult?.Status == LicenseCheckStatus.InTrial)
```

```csharp
// ─── Проверка Enterprise ───

// БЫЛО
if (GrossGeoLicense.LicenseType == LicenseType.Enterprise)

// СТАЛО
if (GrossGeoLicense.PlanTier == PlanTier.Enterprise)
```

```csharp
// ─── switch по типу лицензии ───

// БЫЛО
switch (GrossGeoLicense.LicenseType)
{
    case LicenseType.Trial:
        ShowTrialBanner();
        break;
    case LicenseType.Standard:
    case LicenseType.Subscription:
        ShowProFeatures();
        break;
    case LicenseType.Enterprise:
        ShowEnterpriseFeatures();
        break;
}

// СТАЛО
switch (GrossGeoLicense.PlanTier)
{
    case PlanTier.Free:
        ShowTrialBanner();
        break;
    case PlanTier.Pro:
    case PlanTier.ProPlus:
        ShowProFeatures();
        break;
    case PlanTier.Enterprise:
        ShowEnterpriseFeatures();
        break;
}
```

---

## 3. Новые возможности (опционально)

### 3.1 Feature Limits

Если вы хотите ограничить количественные параметры (объекты, вызовы, размер):

```csharp
// Проверить лимит перед операцией
var limit = GrossGeoLicense.GetFeatureLimit("batch-export", "maxPerCall");
if (limit.HasValue && objects.Count > limit.Value)
{
    editor.WriteMessage($"\nЛимит: {limit.Value} объектов. Обновите план.");
    return;
}

// Или компактно:
if (!GrossGeoLicense.CheckLimit("batch-export", "maxPerCall", objects.Count))
{
    ShowUpgradeDialog();
    return;
}

// Или с автоматическим исключением:
try
{
    GrossGeoLicense.RequireLimitOrThrow("batch-export", "maxPerCall", objects.Count);
    DoExport(objects);
}
catch (LimitExceededException ex)
{
    editor.WriteMessage($"\nПревышен лимит: макс. {ex.Limit}, запрошено {ex.ActualValue}");
}
```

### ~~3.2 Public Features~~

> ⚠️ **Удалено в v3:** публичные фичи убраны вместе с `IsPublic`; код без SDK-обёртки доступен всем.

~~Если часть функционала доступна без лицензии:~~

```csharp
// Загрузить список публичных фичей (при старте, до авторизации)
await GrossGeoLicense.LoadPublicFeaturesAsync(productId);

// HasFeature автоматически проверяет и публичные фичи
[CommandMethod("VIEW")]
public void ViewCommand()
{
    if (GrossGeoLicense.HasFeature("basic-viewer"))
    {
        ShowViewer(); // Работает без лицензии
    }
}
```

### 3.3 Concurrent Sessions

Если лицензия использует плавающие лицензии:

```csharp
public void Initialize()
{
    Task.Run(async () =>
    {
        var result = await GrossGeoLicense.Initialize(options);

        if (result.IsValid && result.LicenseMode == LicenseMode.Concurrent)
        {
            var session = await GrossGeoLicense.AcquireSessionAsync(
                clientInfo: Environment.MachineName);

            if (!session.IsSuccess)
            {
                editor.WriteMessage($"\nВсе слоты заняты: {session.ErrorMessage}");
            }
        }
    });

    // Обработка потери сессии
    GrossGeoLicense.SessionExpired += (s, e) =>
    {
        editor.WriteMessage($"\nСессия потеряна: {e.Message}");
    };
}

public void Terminate()
{
    // Освободить слот
    GrossGeoLicense.ReleaseSessionAsync().Wait();
    GrossGeoLicense.Shutdown();
}
```

### 3.4 Deep-link: старт Trial из плагина (SDK.Stub 2.1.6+)

Кнопка «Попробовать бесплатно» может открыть User Panel на карточке продукта и запросить подтверждение старта trial без ручной сборки `grossgeo://` URI и без ProductId (только `LicenseOptions.ProductKey`, который у плагина уже есть):

```csharp
[CommandMethod("MY_TRIAL_BUTTON")]
public async void OnTrialButtonClick()
{
    await GrossGeoLicense.RequestTrialAsync();
}
```

`RequestTrialAsync()` **не активирует лицензию сама** — она открывает панель, а панель сама показывает подтверждение и стартует trial только если сервер это разрешает (`CanStartTrial=true`). Подробнее (включая URI-форму `grossgeo://start-trial/{productKey}` для непанельных сценариев) — [docs/external/grossgeo-sdk-deeplinks-guide.md](external/grossgeo-sdk-deeplinks-guide.md).

---

## 4. Матрица совместимости

| Компонент | Минимальная версия |
|-----------|--------------------|
| SDK.Stub | 2.0.0 |
| GrossGeo.Contracts | 1.0.0 |
| User Panel | 2.0.0 |
| CoreAPI | 2.0.0 |
| .NET Framework | 4.8 |
| .NET | 8.0 |

### IPC Protocol

SDK v2 использует IPC Protocol v4. При подключении к User Panel v1 сработает graceful degradation:
- `IsVersionMismatch = true` в ответе
- SDK продолжит работать с ограниченным функционалом (v2 фичи недоступны)
- Предупреждение в логе: "Требуется обновление User Panel"

---

## 5. Чеклист миграции

- [ ] Обновить `PackageReference` до точной версии `2.1.16` (не диапазоном `2.*`)
- [ ] Добавить `using GrossGeo.Contracts.Licensing;`
- [ ] Заменить `LicenseType` на `PlanTier` / `BillingModel` (см. раздел 2)
- [ ] Убрать предупреждения `CS0618` (obsolete)
- [ ] (Опционально) Добавить Feature Limits для количественных ограничений
- [x] ~~(Опционально) Добавить Public Features для гостевого доступа~~ ❌ удалено в v3
- [ ] (Опционально) Добавить Concurrent Sessions для плавающих лицензий
- [ ] Протестировать плагин с новой версией User Panel
- [ ] Пересобрать и опубликовать плагин

---

## 6. FAQ

### Обязательно ли мигрировать на v2?

Нет. Старый `LicenseType` продолжает работать с предупреждением `[Obsolete]`. Рекомендуется мигрировать до версии 3.0, в которой `LicenseType` будет удалён.

### Нужно ли пересобирать плагин?

Да, нужно пересобрать с новой версией SDK. Но это drop-in replacement: обновите NuGet-пакет и пересоберите.

### Что если User Panel старой версии?

SDK v2 определяет несоответствие версий по IPC Protocol и работает в режиме ограниченной совместимости. Новые фичи (Limits, Concurrent Sessions) будут недоступны, но базовая проверка лицензии продолжит работать.

### Изменился ли формат кэша?

Да. SDK v2 использует DPAPI вместо AES с фиксированным ключом. При первом запуске старый кэш будет автоматически мигрирован.
