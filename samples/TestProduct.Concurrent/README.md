# 🔁 TestProduct.Concurrent

**Тип:** Multi-tier (Free/Pro/Pro+) + Concurrent Sessions

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | задаётся в `TestConcurrentPlugin.cs` (демо-плейсхолдер; подставьте свой) |
| LicensingMode | GrossGeo |
| DistributionType | **Bundle** |
| Trial | 7 дней (Account) |

> Параметры ниже соответствуют [`plans-manifest.json`](./plans-manifest.json) этого примера.

## 📊 Планы

| План (`code`) | Tier | Billing | LicenseMode | monthlyPrice | Trial | maxSeats |
|------|------|---------|-------------|-------------|-------|----------|
| `free` | Free | Free | Machine | 0 ₽ | — | 1 |
| `pro` | Pro | Subscription | User | 800 ₽ | 7 дн. | 1 |
| `pro-plus` | ProPlus | Subscription | **Concurrent** | 1 500 ₽ | 7 дн. | **10** |

## 🏷️ Features

| Feature (`code`) | isDefault | free | pro | pro-plus |
|---------|-----------|------|-----|----------|
| `view-objects` | ✅ | ✅ | ✅ | ✅ |
| `basic-tools` | ✅ | ✅ | ✅ | ✅ |
| `pro-tools` | — | — | ✅ | ✅ |
| `export-batch` | — | ✅ | ✅ | ✅ |
| `enterprise-api` | — | — | — | ✅ |
| `cloud-sync` | — | — | — | ✅ |

## ⚙️ Feature Limits (Graduated)

Допустимые ключи лимита: `maxPerCall`, `maxPerDay`, `maxPerMonth`, `maxTotal`.
Отсутствие плана в `limits` фичи = безлимитно.

| Feature | План | Лимит | Значение |
|---------|------|-------|----------|
| `export-batch` | `free` | `maxPerCall` | 10 |
| `export-batch` | `pro` | `maxPerCall` | 100 |
| `export-batch` | `pro-plus` | — | ∞ |

## 🔧 Команды AutoCAD

| Команда | Описание | Лицензия |
|---------|----------|----------|
| `GGCONCINFO` | Полная информация | Нет |
| `GGCONCSESSION` | Acquire concurrent session | Да |
| `GGCONCSESSIONRELEASE` | Release session | Да |
| `GGCONCPUBLIC` | Демо публичных фичей | Нет |
| `GGCONCPRO` | Pro-инструменты | Да |
| `GGCONCBATCH` | Пакетный экспорт с лимитами | Да |
| `GGCONCLIMITS` | Демо Feature Limits API | Нет |
| `GGCONCHELP` | Справка | Нет |

## 🎯 Что покрывает

- LicenseMode.Concurrent (плавающие сессии, heartbeat, timeout)
- Multi-tier: Free → Pro → Pro+ в одном продукте
- Graduated Feature Limits (`maxPerCall` разный по планам)
- Незащищённый код / isDefault-фичи (доступ без платного плана)
