# 📊 TestProduct.Analytics

**Тип:** ExternalOnly (каталог + аналитика, лицензирование внешнее)

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | задаётся в `TestAnalyticsPlugin.cs` (демо-плейсхолдер; подставьте свой) |
| LicensingMode | **ExternalOnly** |
| DistributionType | **Bundle** |
| Планы | Нет (лицензирование у разработчика) |
| Features | Нет |
| externalPurchaseUrl | `https://example.com/buy` |
| externalDownloadUrl | `https://example.com/download` |

## 🔧 Команды AutoCAD

| Команда | Описание | Лицензия |
|---------|----------|----------|
| `TEST_ANALYTICS_INFO` | Информация о SDK (ExternalOnly) | Нет |
| `TEST_TRACK_FEATURE` | Трекинг использования | Нет |
| `TEST_ANALYTICS_UPDATE` | Проверка обновлений | Нет |
| `TEST_SESSION_STATS` | Статистика сессии | Нет |
| `TEST_ANALYTICS_HELP` | Справка | Нет |

## 🎯 Что покрывает

- ProductLicensingMode.ExternalOnly
- Каталог GrossGeo + аналитика без лицензирования
- Внешние URL покупки/скачивания
- SDK трекинг без проверки лицензии
