# 📦 TestProduct.Installer

**Тип:** DistributionType.Installer (EXE/MSI)

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | задаётся в `TestInstallerPlugin.cs` (демо-плейсхолдер; подставьте свой) |
| LicensingMode | GrossGeo |
| DistributionType | **Installer** |
| Trial | 14 дней (**AccountAndMachine**) |

> Параметры ниже соответствуют [`plans-manifest.json`](./plans-manifest.json) этого примера.

## 📊 Планы

| План (`code`) | Tier | Billing | LicenseMode | monthlyPrice | Trial |
|------|------|---------|-------------|-------------|-------|
| `pro` | Pro | Subscription | Machine | 1 490 ₽ | 14 дн. (AccountAndMachine) |

## 🏷️ Features

| Feature (`code`) | isDefault | pro |
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
