# TestProduct.Licensed

**Тип:** Perpetual-лицензия с опциональным Maintenance

> Параметры ниже соответствуют [`plans-manifest.json`](./plans-manifest.json) этого примера.
> Maintenance в модели v3 — это **атрибут** Perpetual-плана (`maintenanceYearlyPrice`),
> а **не** отдельный план. Годовая подписка Maintenance даёт доступ к новым версиям;
> при её истечении версия фиксируется (Version Lock).

## Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | `GG-4CE5-8D13-2CB8-032F` (демо-плейсхолдер; подставьте свой) |
| LicensingMode | GrossGeo |
| DistributionType | **Bundle** |
| Trial | Нет |

## Планы

| План (`code`) | Tier | Billing | LicenseMode | oneTimePrice | maintenanceYearlyPrice |
|------|------|---------|-------------|-------------|------------------------|
| `pro` | Pro | Perpetual | Machine | 9 990 ₽ | 2 990 ₽/год |
| `pro-plus` | ProPlus | Perpetual | User | 24 990 ₽ | 4 990 ₽/год |

## Features

| Feature (`code`) | isDefault | pro | pro-plus |
|---------|-----------|-----|----------|
| `export_pdf` | ✅ | ✅ | ✅ |
| `batch_processing` | — | ✅ | ✅ |
| `premium_tools` | — | — | ✅ |
| `enterprise_api` | — | — | ✅ |

## Команды AutoCAD

| Команда | Описание | Лицензия |
|---------|----------|----------|
| `TEST_LICENSE_INFO` | Информация о лицензии (v2) | Нет |
| `TEST_PROTECTED_CMD` | Защищённая команда (LicenseGuard) | Да |
| `TEST_FEATURE_CHECK` | Проверка feature flags | Нет |
| `TEST_CHECK_UPDATE` | Проверка обновлений (Maintenance) | Нет |
| `TEST_LICENSE_RECHECK` | Повторная проверка лицензии | Нет |
| `TEST_LICENSED_HELP` | Справка | Нет |

## Что покрывает

- BillingModel.Perpetual (разовая покупка, бессрочная)
- PlanTier.Pro (Machine) + PlanTier.ProPlus (User)
- Maintenance как атрибут плана (`maintenanceYearlyPrice`), без отдельного плана
- Version Lock при истечении Maintenance
