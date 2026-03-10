# ⭐ TestProduct.Freemium

**Тип:** Freemium (бесплатный базовый + PRO по подписке) + Trial

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | `GG-FRMI-TEST-0004` |
| LicensingMode | GrossGeo |
| DistributionType | **Bundle** |
| Trial PRO | **7 дней (Account)** |

## 📊 Планы

| План | Tier | Billing | Period | LicenseMode | Price | Trial |
|------|------|---------|--------|-------------|-------|-------|
| free | Free | Free | — | Machine | 0 ₽ | — |
| pro | Pro | Subscription | Monthly | Machine | 490 ₽/мес | 7 дн. |

## 🏷️ Features

| Feature | IsDefault | Free | PRO |
|---------|-----------|------|-----|
| `basic-tools` | ✅ | ✅ | ✅ |
| `simple-export` | ✅ | ✅ | ✅ |
| `advanced-tools` | — | — | ✅ |
| `batch-processing` | — | — | ✅ |
| `cloud-sync` | — | — | ✅ |
| `priority-support` | — | — | ✅ |

## ⚙️ Feature Limits

| Plan.Feature | LimitCode | LimitType | Value |
|-------------|-----------|-----------|-------|
| free.simple-export | maxPerCall | MaxPerCall | 5 |
| free.simple-export | maxSize | MaxSize | 10 MB |
| free.batch-processing | maxPerCall | MaxPerCall | 10 |
| free.batch-processing | maxPerSession | MaxPerSession | 50 |
| pro.simple-export | — | — | ∞ |
| pro.batch-processing | — | — | ∞ |

## 🔧 Команды AutoCAD

| Команда | Описание | Лицензия |
|---------|----------|----------|
| `GGFMINFO` | Информация + features + limits | Нет |
| `GGFMBASIC` | Базовые инструменты | Нет |
| `GGFMEXPORT` | Простой экспорт (с лимитами) | Нет |
| `GGFMUPGRADE` | Информация об апгрейде | Нет |
| `GGFMADVANCED` | Продвинутые инструменты (PRO) | Да |
| `GGFMBATCH` | Пакетная обработка (PRO) | Да |
| `GGFMCLOUD` | Облачная синхронизация (PRO) | Да |
| `GGFMLIMITS` | Демо Feature Limits API | Нет |
| `GGFMREFRESH` | Обновление данных лицензии | Нет |

## 🎯 Что покрывает

- Freemium модель: Free-план с лимитами + PRO без лимитов
- Trial на PRO (7 дней, TrialBindingMode.Account)
- LimitType.MaxPerCall + LimitType.MaxSize + LimitType.MaxPerSession
- Upsell-механика (GGFMUPGRADE)
- LicenseMode.Machine + BillingModel.Subscription
