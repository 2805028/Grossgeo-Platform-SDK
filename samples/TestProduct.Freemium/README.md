# ⭐ TestProduct.Freemium

**Тип:** Freemium (бесплатный базовый + PRO по подписке) + Trial

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | задаётся в `TestFreemiumPlugin.cs` (демо-плейсхолдер; подставьте свой) |
| LicensingMode | GrossGeo |
| DistributionType | **Bundle** |
| Trial PRO | **7 дней (Account)** |

> Параметры ниже соответствуют [`plans-manifest.json`](./plans-manifest.json) этого примера.

## 📊 Планы

| План (`code`) | Tier | Billing | LicenseMode | monthlyPrice | Trial |
|------|------|---------|-------------|-------------|-------|
| `free` | Free | Free | Machine | 0 ₽ | — |
| `pro` | Pro | Subscription | Machine | 490 ₽ | 7 дн. |

## 🏷️ Features

| Feature (`code`) | isDefault | free | pro |
|---------|-----------|------|-----|
| `basic-tools` | ✅ | ✅ | ✅ |
| `simple-export` | ✅ | ✅ | ✅ |
| `advanced-tools` | — | — | ✅ |
| `batch-processing` | — | — | ✅ |
| `cloud-sync` | — | — | ✅ |
| `priority-support` | — | — | ✅ |

## ⚙️ Feature Limits

Допустимые ключи лимита: `maxPerCall`, `maxPerDay`, `maxPerMonth`, `maxTotal`.
Отсутствие плана в `limits` фичи = безлимитно.

| Feature | План | Лимит | Значение |
|---------|------|-------|----------|
| `simple-export` | `free` | `maxPerCall` | 5 |
| `simple-export` | `pro` | — | ∞ |

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

- Freemium модель: Free-план с лимитом + PRO без лимитов
- Trial на PRO (7 дней, TrialBindingMode.Account)
- Feature Limit `maxPerCall` на Free-плане
- Upsell-механика (GGFMUPGRADE)
- LicenseMode.Machine + BillingModel.Subscription
