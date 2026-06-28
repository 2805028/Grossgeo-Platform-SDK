# 🆓 TestProduct.Free

**Тип:** FREE (полностью бесплатный, публичные фичи)

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | `GG-974A-E898-5FBB-66FF` |
| LicensingMode | GrossGeo |
| DistributionType | **Bundle** |
| PlanTier | Free |
| BillingModel | Free |
| LicenseMode | Machine |
| Trial | Нет |

## 📊 Планы

| План | Tier | Billing | Price |
|------|------|---------|-------|
| free | Free | Free | 0 ₽ |

## 🏷️ Features

| Feature (`code`) | isDefault | free |
|---------|-----------|------|
| `view-objects` | ✅ | ✅ |
| `basic-info` | ✅ | ✅ |

> В модели v3 нет флага `IsPublic`. Код, не обёрнутый в `HasFeature` / `FeatureGuard`,
> доступен всем (включая пользователей без лицензии). Фичи с `isDefault: true` доступны
> всем авторизованным пользователям во всех планах.

## 🔧 Команды AutoCAD

| Команда | Описание | Лицензия |
|---------|----------|----------|
| `GGFREETEST` | Бесплатная команда | Нет |
| `GGFREEINFO` | Информация о лицензии | Нет |
| `GGFREEPUBLIC` | Демо незащищённого функционала | Нет |
| `GGFREEHELP` | Справка | Нет |

## 🎯 Что покрывает

- PlanTier.Free + BillingModel.Free
- Незащищённый код (без SDK-обёртки) — доступен всем
- isDefault=true фичи (доступны всем авторизованным)
- DistributionType.Bundle
