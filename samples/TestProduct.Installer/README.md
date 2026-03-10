# 📦 TestProduct.Installer

**Тип:** DistributionType.Installer (EXE/MSI)

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | `GG-INST-TEST-0007` |
| LicensingMode | GrossGeo |
| DistributionType | **Installer** |
| Trial | 14 дней (**AccountAndMachine**) |

## 📊 Планы

| План | Tier | Billing | Period | LicenseMode | Price | Trial |
|------|------|---------|--------|-------------|-------|-------|
| pro | Pro | Subscription | Monthly | Machine | 1 490 ₽/мес | 14 дн. |

## 🏷️ Features

| Feature | IsDefault | Pro |
|---------|-----------|-----|
| `core` | ✅ | ✅ |
| `advanced` | — | ✅ |

## 🔧 Команды AutoCAD

| Команда | Описание | Лицензия |
|---------|----------|----------|
| `GGINSTTEST` | Базовая команда | Да |
| `GGINSTINFO` | Информация о SDK | Нет |

## 🎯 Что покрывает

- DistributionType.Installer (EXE/MSI, не Bundle)
- TrialBindingMode.AccountAndMachine (защита от повторного trial)
- Установка через внешний установщик
