# 📊 TestProduct.Analytics

**Тип лицензирования:** Analytics (внешнее лицензирование)

Демонстрационный плагин для режима DistributionMode.Analytics — файлы хостятся на GrossGeo, но лицензирование управляется внешне (на сайте разработчика).

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| Product ID | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` |
| API Key | `GG-FA06-B797-8461-2CD1` |
| DistributionMode | Analytics |
| Лицензирование | Внешнее (у разработчика) |

## 🎯 Назначение

Тестирование сценария когда:
- Файлы плагина хостятся на GrossGeo (CDN, статистика скачиваний)
- Продукт отображается в каталоге
- Лицензирование НЕ через GrossGeo (разработчик использует своё)
- Аналитика использования отправляется в GrossGeo

## 🏗️ Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                     AutoCAD Plugin                          │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  SDK.Stub (Analytics Mode)                            │  │
│  │  - Инициализация без проверки лицензии               │  │
│  │  - Отправка событий использования                     │  │
│  │  - Проверка обновлений через GrossGeo                 │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐
  │  GrossGeo     │  │  Developer    │  │  GrossGeo     │
  │  Analytics    │  │  License      │  │  CDN/Storage  │
  │  (события)    │  │  Server       │  │  (файлы)      │
  └───────────────┘  └───────────────┘  └───────────────┘
```

## 🔧 Команды AutoCAD

| Команда | Описание |
|---------|----------|
| `TEST_ANALYTICS_INFO` | Информация о SDK |
| `TEST_TRACK_FEATURE` | Симуляция с трекингом |
| `TEST_ANALYTICS_UPDATE` | Проверка обновлений |
| `TEST_USAGE_REPORT` | Отчёт об использовании |

## 🧪 Тестовые сценарии

### 1. Инициализация (всегда успешна)
```csharp
var result = await GrossGeoLicense.Initialize(new LicenseOptions 
{ 
    ProductKey = "GG-FA06-B797-8461-2CD1" 
});

// Analytics Mode — лицензия не проверяется:
// result.IsValid = true (или false, не влияет на работу)
// result.IsOfflineMode = возможно true
// Все команды работают независимо от result
```

### 2. Трекинг использования
```csharp
// SDK автоматически отправляет события:
// - plugin_loaded (при Initialize)
// - command_executed (при Protect/RequireFeature)
// - plugin_unloaded (при Shutdown)

// Разработчик может добавить кастомные события через свой сервер
```

### 3. Проверка обновлений
```
Команда: TEST_ANALYTICS_UPDATE

Вывод:
  Текущая версия:   1.0.0
  Обновление:       ✅ Доступно / ❌ Нет
  Новая версия:     2.0.0
  Дата релиза:      01.02.2026
```

### 4. API — продукт в каталоге
```
GET /api/catalog/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa

Ожидается:
{
  "id": "aaaaaaaa-...",
  "name": "Test Plugin Analytics",
  "distributionMode": "Analytics",
  "externalLicenseUrl": "https://developer-site.com/license",
  "pricing": null  // Цены на стороне разработчика
}
```

## 📦 Сборка

```bash
cd Samples\TestProduct.Analytics
dotnet build --configuration Release
```

## 📁 Структура

```
TestProduct.Analytics/
├── TestProduct.Analytics.csproj
├── TestAnalyticsPlugin.cs
├── PackageContents.xml
└── README.md
```

## 💡 Когда использовать Analytics Mode

- У вас уже есть своя система лицензирования
- Вы хотите использовать GrossGeo только для дистрибуции
- Вам нужна статистика скачиваний и использования
- Вы хотите присутствовать в каталоге GrossGeo

## ⚠️ Ограничения Analytics Mode

- Нет триалов через GrossGeo
- Нет подписок через GrossGeo
- Нет feature management через GrossGeo
- Только базовая аналитика (скачивания, активные пользователи)
