# TestProduct.Subscription

**Тип:** Подписка (месячная/годовая цена на одном плане) + Trial + Feature Limits

> Параметры ниже соответствуют [`plans-manifest.json`](./plans-manifest.json) этого примера.
> Период (месяц/год) — это не отдельный план, а toggle при покупке: на одном плане заданы
> `monthlyPrice` и `yearlyPrice` (модель лицензирования v3).

## Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | задаётся в `TestSubscriptionPlugin.cs` (демо-плейсхолдер; подставьте свой) |
| LicensingMode | GrossGeo |
| DistributionType | **Bundle** |

## Планы

| План (`code`) | Tier | Billing | LicenseMode | monthlyPrice | yearlyPrice | Trial |
|------|------|---------|-------------|-------------|------------|-------|
| `pro` | Pro | Subscription | User | 990 ₽ | 9 900 ₽ | 14 дн. (Account) |
| `pro-plus` | ProPlus | Subscription | User | 1 990 ₽ | 19 900 ₽ | — |

## Features

| Feature (`code`) | isDefault | pro | pro-plus |
|---------|-----------|-----|----------|
| `basic` | ✅ | ✅ | ✅ |
| `export` | — | ✅ | ✅ |
| `advanced-export` | — | — | ✅ |
| `batch` | — | — | ✅ |

## Feature Limits

Лимиты задаются в фиче (`limits`, ключ — код плана). Допустимые ключи лимита:
`maxPerCall`, `maxPerDay`, `maxPerMonth`, `maxTotal`. Отсутствие плана в `limits` = безлимитно.

| Feature | План | Лимит | Значение |
|---------|------|-------|----------|
| `export` | `pro` | `maxPerMonth` | 100 |
| `export` | `pro-plus` | — | ∞ |

## Команды AutoCAD

| Команда | Описание | Лицензия |
|---------|----------|----------|
| `GGSUBSINFO` | Информация о лицензии | Нет |
| `GGSUBSTEST` | Базовая команда | Да |
| `GGSUBSFEATURES` | Демо всех features | Да |
| `GGSUBSEXPORT` | Экспорт (feature: export, лимит maxPerMonth) | Да |
| `GGSUBSBATCH` | Пакетная обработка (feature: batch) | Да |

## Что покрывает

- BillingModel.Subscription (monthlyPrice + yearlyPrice на одном плане)
- PlanTier.Pro + PlanTier.ProPlus
- LicenseMode.User
- Trial 14 дней (TrialBindingMode.Account)
- Feature Limit `maxPerMonth` (usage tracking через API)
