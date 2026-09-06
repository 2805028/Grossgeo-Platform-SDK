# 💎 TestProduct.Licensed

**Тип:** Perpetual лицензия + Maintenance аддон  
**Multi-target:** net48 (AutoCAD 2019–2024) + net8.0-windows (AutoCAD 2025–2026)

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | `GG-4CE5-8D13-2CB8-032F` |
| LicensingMode | GrossGeo |
| DistributionType | **Bundle (Multi-target)** |
| Target Frameworks | `net48`, `net8.0-windows` |
| Trial | Нет |

## 📊 Планы

| План | Tier | Billing | Period | LicenseMode | Price |
|------|------|---------|--------|-------------|-------|
| pro | Pro | Perpetual | OneTime | **Machine** | 9 990 ₽ |
| pro-plus | ProPlus | Perpetual | OneTime | **User** | 24 990 ₽ |
| maintenance | Maintenance | Subscription | Yearly | Machine | 2 990 ₽/год |

> Maintenance — аддон к Perpetual, требует активный plan `pro`.

## 🏷️ Features

| Feature | IsDefault | Pro | Pro+ | Maintenance |
|---------|-----------|-----|------|-------------|
| `export_pdf` | ✅ | ✅ | ✅ | ✅ |
| `batch_processing` | — | ✅ | ✅ | ✅ |
| `premium_tools` | — | — | ✅ | — |
| `enterprise_api` | — | — | ✅ | — |

## 🔧 Команды AutoCAD

| Команда | Описание | Лицензия |
|---------|----------|----------|
| `TEST_LICENSE_INFO` | Информация о лицензии (v2) | Нет |
| `TEST_PROTECTED_CMD` | Защищённая команда (LicenseGuard) | Да |
| `TEST_FEATURE_CHECK` | Проверка feature flags | Нет |
| `TEST_CHECK_UPDATE` | Проверка обновлений (Maintenance) | Нет |
| `TEST_LICENSE_RECHECK` | Повторная проверка лицензии | Нет |
| `TEST_LICENSED_HELP` | Справка | Нет |

## 🎯 Что покрывает

- BillingModel.Perpetual (OneTime покупка, бессрочная)
- PlanTier.Pro (Machine) + PlanTier.ProPlus (**User** — Perpetual+User)
- PlanTier.Maintenance (аддон, Subscription/Yearly)
- Version Lock при истечении Maintenance
- **Multi-target Bundle** (net48 + net8.0-windows в одном ZIP)
- Мультитаргетный PackageContents.xml (два ComponentEntry)
