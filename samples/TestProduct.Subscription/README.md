# 🔄 TestProduct.Subscription

**Тип:** Подписка (Monthly/Yearly) + Trial

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | `GG-SUBS-TEST-0002` |
| LicensingMode | GrossGeo |
| DistributionType | **Bundle** |
| Trial | 14 дней (Account) |

## 📊 Планы

| План | Tier | Billing | Period | LicenseMode | Price | Trial |
|------|------|---------|--------|-------------|-------|-------|
| standard | Pro | Subscription | Monthly | User | 990 ₽/мес | 14 дн. |
| standard-yearly | Pro | Subscription | Yearly | User | 9 900 ₽/год | — |
| pro | ProPlus | Subscription | Monthly | User | 1 990 ₽/мес | — |
| pro-yearly | ProPlus | Subscription | **Yearly** | User | 19 900 ₽/год | — |

## 🏷️ Features

| Feature | IsDefault | Standard | Pro/Pro-Yearly |
|---------|-----------|----------|----------------|
| `basic` | ✅ | ✅ | ✅ |
| `export` | — | ✅ | ✅ |
| `advanced-export` | — | — | ✅ |
| `batch` | — | — | ✅ |

## ⚙️ Feature Limits

| Plan.Feature | LimitCode | LimitType | Value |
|-------------|-----------|-----------|-------|
| standard.export | monthlyExports | MaxPerPeriod | 100 |
| standard-yearly.export | — | — | ∞ |
| pro.export | — | — | ∞ |

## 🔧 Команды AutoCAD

| Команда | Описание | Лицензия |
|---------|----------|----------|
| `GGSUBSINFO` | Информация о лицензии | Нет |
| `GGSUBSTEST` | Базовая команда | Да |
| `GGSUBSFEATURES` | Демо всех features | Да |
| `GGSUBSEXPORT` | Экспорт (feature: export) | Да |
| `GGSUBSBATCH` | Пакетная обработка (feature: batch) | Да |

## 🎯 Что покрывает

- BillingModel.Subscription (Monthly + Yearly)
- PlanTier.Pro + PlanTier.ProPlus
- LicenseMode.User
- Trial 14 дней (TrialBindingMode.Account)
- LimitType.MaxPerPeriod
- ProPlus + Yearly (годовая подписка на ProPlus)
