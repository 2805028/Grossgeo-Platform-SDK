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
| pro.simple-export | — | — | ∞ |
| pro.batch-processing | — | — | ∞ |

На Free-плане `batch-processing` недоступен вовсе (см. таблицу Features выше): `GGFMUSAGE`
и `GGFMBATCH` печатают отказ доступа, а не сбой учёта. Лимиты `maxPerDay`/`maxPerSession`
для пакетной обработки этим манифестом не заведены — команды читают их как «безлимитно»,
пока студия не задаст значения в Developer Portal.

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
| `GGFMUSAGE` | Демо Usage Tracking: инкремент `maxPerDay` пакетной обработки (PRO) | Да |
| `GGFMUSAGEREPORT` | Отчёт по использованию (только чтение) | Нет |

## 🎯 Что покрывает

- Freemium модель: Free-план с лимитами + PRO без лимитов
- Trial на PRO (7 дней, TrialBindingMode.Account)
- LimitType.MaxPerCall + LimitType.MaxSize + LimitType.MaxPerSession
- Upsell-механика (GGFMUPGRADE)
- LicenseMode.Machine + BillingModel.Subscription
