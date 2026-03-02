# ⭐ TestProduct.Freemium

**Тип лицензирования:** FREEMIUM (бесплатный базовый + PRO по подписке)

Демонстрационный плагин для тестирования Freemium модели с бесплатным базовым функционалом и PRO-фичами.

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| Product ID | `ffffffff-ffff-ffff-ffff-ffffffffffff` |
| API Key | `GG-FRMI-TEST-0004` |
| Монетизация | Freemium |
| Free tier | Бесплатно |
| PRO план | 490₽/мес (4900₽/год) |
| Trial PRO | 7 дней |

## 🎯 Назначение

Тестирование Freemium сценариев:
- Базовые функции работают без подписки
- PRO-функции требуют подписку
- Upsell механики
- Апгрейд Free → PRO

## 📊 Features по планам

| Feature | Free | PRO |
|---------|------|-----|
| `basic-tools` | ✅ | ✅ |
| `simple-export` | ✅ | ✅ |
| `advanced-tools` | ❌ | ✅ |
| `batch-processing` | ❌ | ✅ |
| `cloud-sync` | ❌ | ✅ |
| `priority-support` | ❌ | ✅ |

## 🔧 Команды AutoCAD

### Free Tier (всегда доступны)

| Команда | Описание |
|---------|----------|
| `GGFMINFO` | Информация о статусе и features |
| `GGFMBASIC` | Базовые инструменты |
| `GGFMEXPORT` | Простой экспорт |
| `GGFMUPGRADE` | Информация об апгрейде до PRO |

### PRO Features (требуют подписку)

| Команда | Описание | Feature |
|---------|----------|---------|
| `GGFMADVANCED` | Продвинутые инструменты | advanced-tools |
| `GGFMBATCH` | Пакетная обработка | batch-processing |
| `GGFMCLOUD` | Облачная синхронизация | cloud-sync |

## 🧪 Тестовые сценарии

### 1. Free Tier — базовые функции
```
1. Загрузить плагин без подписки
2. Выполнить GGFMBASIC → работает
3. Выполнить GGFMEXPORT → работает
4. Выполнить GGFMADVANCED → "🔒 Требуется PRO подписка"
```

### 2. SDK проверка features
```csharp
var result = await GrossGeoLicense.Initialize(new LicenseOptions 
{ 
    ProductKey = "GG-FRMI-TEST-0004" 
});

// Free Tier:
GrossGeoLicense.HasFeature("basic-tools");     // true
GrossGeoLicense.HasFeature("simple-export");   // true
GrossGeoLicense.HasFeature("advanced-tools");  // false
GrossGeoLicense.HasFeature("batch-processing"); // false

// PRO:
GrossGeoLicense.HasFeature("advanced-tools");  // true
GrossGeoLicense.HasFeature("batch-processing"); // true
GrossGeoLicense.HasFeature("cloud-sync");      // true
```

### 3. Защита PRO-функций в коде
```csharp
// Мягкая защита с upsell
FeatureGuard.Require("advanced-tools",
    action: () => {
        // PRO функционал
        RunAdvancedTools();
    },
    onMissing: () => {
        // Upsell
        ShowUpgradeDialog();
    }
);

// Жёсткая защита
try {
    FeatureGuard.OrThrow("batch-processing", () => {
        ProcessBatch();
    });
} catch (FeatureNotAvailableException ex) {
    MessageBox.Show($"Функция {ex.FeatureCode} требует PRO подписку");
}
```

### 4. Проверка через API
```
GET /api/user/licenses/status/ffffffff-ffff-ffff-ffff-ffffffffffff
Authorization: Bearer {token}

Free Tier ответ:
{
  "hasLicense": false,
  "defaultFeatures": ["basic-tools", "simple-export"],
  "availablePlans": [
    {"name": "PRO", "price": 490, "features": [...]}
  ]
}

PRO ответ:
{
  "hasLicense": true,
  "planTier": "Pro",
  "billingModel": "Subscription",
  "features": ["basic-tools", "simple-export", "advanced-tools", "batch-processing", "cloud-sync", "priority-support"]
}
```

## 📦 Сборка

```bash
cd Samples\TestProduct.Freemium
dotnet build --configuration Release
```

## 📁 Структура

```
TestProduct.Freemium/
├── TestProduct.Freemium.csproj
├── TestFreemiumPlugin.cs
└── README.md
```

## 💡 Best Practices для Freemium

1. **Базовые функции должны быть полезными** — пользователь должен получить ценность бесплатно
2. **PRO-функции для power users** — пакетная обработка, автоматизация, облако
3. **Ненавязчивый upsell** — показывайте преимущества PRO, не блокируя работу
4. **Trial PRO** — дайте попробовать PRO функции перед покупкой
