# 🔁 TestProduct.Concurrent

**Тип:** Multi-tier (Free/Pro/Pro+) + Concurrent Sessions

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | `GG-CONC-TEST-0006` |
| LicensingMode | GrossGeo |
| DistributionType | **Bundle** |
| Trial | 7 дней (Account) |

## 📊 Планы

| План | Tier | Billing | Period | LicenseMode | Price | Trial | MaxSeats |
|------|------|---------|--------|-------------|-------|-------|----------|
| free | Free | Free | — | Machine | 0 ₽ | — | 1 |
| pro | Pro | Subscription | Monthly | User | 800 ₽/мес | 7 дн. | 1 |
| pro-plus | ProPlus | Subscription | Monthly | **Concurrent** | 1 500 ₽/мес | 7 дн. | **10** |

## 🏷️ Features

| Feature | IsDefault | IsPublic | Free | Pro | Pro+ |
|---------|-----------|----------|------|-----|------|
| `view-objects` | ✅ | ✅ (Guest) | ✅ | ✅ | ✅ |
| `basic-tools` | ✅ | — | ✅ | ✅ | ✅ |
| `pro-tools` | — | — | — | ✅ | ✅ |
| `export-batch` | — | — | ✅ | ✅ | ✅ |
| `enterprise-api` | — | — | — | — | ✅ |
| `cloud-sync` | — | — | — | — | ✅ |

## ⚙️ Feature Limits (Graduated)

| Plan.Feature | LimitCode | LimitType | Value |
|-------------|-----------|-----------|-------|
| free.export-batch | maxPerCall | MaxPerCall | 10 |
| pro.export-batch | maxPerCall | MaxPerCall | 100 |
| pro.export-batch | maxPerSession | MaxPerSession | 500 |
| pro-plus.export-batch | — | — | ∞ |
| pro.pro-tools | maxPerSession | MaxPerSession | 50 |
| pro-plus.pro-tools | — | — | ∞ |
| pro-plus.cloud-sync | maxSize | MaxSize | 100 MB |

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
- Graduated Feature Limits (разные лимиты по планам)
- LimitType.MaxPerCall, MaxPerSession, MaxSize
- IsPublic фичи (Guest-доступ)
