# 🔄 TestProduct.Subscription

**Тип лицензирования:** TRIAL → SUBSCRIPTION с Features

Демонстрационный плагин для тестирования полного цикла: триал → подписка → апгрейд плана.

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| Product ID | `dddddddd-dddd-dddd-dddd-dddddddddddd` |
| API Key | `GG-SUBS-TEST-0002` |
| Монетизация | Subscription |
| Trial | 14 дней |
| Standard план | 990₽/мес |
| Pro план | 1990₽/мес |

## 🎯 Назначение

Тестирование сценариев:
- Запуск и использование триала
- Переход с триала на подписку
- Разные планы с разными features
- Апгрейд/даунгрейд плана

## 📊 Features по планам

| Feature | Trial | Standard | Pro |
|---------|-------|----------|-----|
| `basic` | ✅ | ✅ | ✅ |
| `export` | ✅ | ✅ | ✅ |
| `advanced-export` | ❌ | ❌ | ✅ |
| `batch` | ❌ | ❌ | ✅ |

## 🔧 Команды AutoCAD

| Команда | Описание | Требования |
|---------|----------|------------|
| `GGSUBSINFO` | Статус лицензии и features | — |
| `GGSUBSTEST` | Базовая команда | Лицензия |
| `GGSUBSFEATURES` | Демо всех features | Лицензия |
| `GGSUBSEXPORT` | Экспорт | Feature: export |
| `GGSUBSBATCH` | Пакетная обработка | Feature: batch (PRO) |

## 🧪 Тестовые сценарии

### 1. Проверка доступности триала
```
GET /api/user/licenses/trial/check/dddddddd-dddd-dddd-dddd-dddddddddddd
Authorization: Bearer {token}

Ожидается:
{
  "isAvailable": true,
  "trialDays": 14,
  "reason": null
}
```

### 2. Запуск триала
```
POST /api/user/licenses/trial
Authorization: Bearer {token}
Content-Type: application/json

{
  "productId": "dddddddd-dddd-dddd-dddd-dddddddddddd"
}

Ожидается:
{
  "licenseId": "...",
  "durationDays": 14,
  "expiresAt": "2026-02-16T..."
}
```

### 3. SDK проверка триала
```csharp
var result = await GrossGeoLicense.Initialize(new LicenseOptions 
{ 
    ProductKey = "GG-SUBS-TEST-0002" 
});

// Ожидаемый результат (после запуска триала):
// result.IsValid = true
// result.Status = LicenseCheckStatus.InTrial
// result.PlanTier = PlanTier.Free
// result.DaysRemaining = 14
// GrossGeoLicense.HasFeature("basic") = true
// GrossGeoLicense.HasFeature("advanced-export") = false
```

### 4. Проверка features в AutoCAD
```
Команда: GGSUBSFEATURES

Вывод (Trial/Standard):
✅ [basic] Базовые функции доступны
✅ [export] Экспорт доступен
❌ [advanced-export] Требуется PRO подписка
❌ [batch] Требуется PRO подписка

Вывод (Pro):
✅ [basic] Базовые функции доступны
✅ [export] Экспорт доступен
✅ [advanced-export] Расширенный экспорт доступен (PRO)
✅ [batch] Пакетная обработка доступна (PRO)
```

### 5. Повторный триал (блокировка)
```
GET /api/user/licenses/trial/check/dddddddd-dddd-dddd-dddd-dddddddddddd

Ожидается (после использования триала):
{
  "isAvailable": false,
  "reason": "TRIAL_USED"
}
```

## 📦 Сборка

```bash
cd Samples\TestProduct.Subscription
dotnet build --configuration Release
```

## 📁 Структура

```
TestProduct.Subscription/
├── TestProduct.Subscription.csproj
├── TestSubscriptionPlugin.cs
└── README.md
```

## 💡 Подсказки

- Используйте `LicenseGuard.Protect()` для мягкой защиты
- Используйте `FeatureGuard.OrThrow()` для жёсткой защиты features
- При отсутствии лицензии показывайте upsell диалоги
