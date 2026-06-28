# 📄 TestProduct.PluginDll

**Тип дистрибуции:** PluginDll (одиночная DLL)

Демонстрационный плагин для тестирования типа поставки PluginDll — самого простого варианта распространения без AutoCAD bundle.

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | задаётся в `TestPluginDllPlugin.cs` (демо-плейсхолдер; подставьте свой) |
| DistributionType | **PluginDll** |
| BillingModel | Perpetual (разовая покупка) |
| LicenseMode | Machine |
| PlanTier | Pro |
| Цена | 4990 ₽ |
| Trial | 14 дней (AccountAndMachine) |

## 🎯 Назначение

Тестирование сценария, когда продукт:
- Распространяется как одиночная DLL (не bundle, не installer)
- Загружается через `NETLOAD` или устанавливается в `GrossGeoPlatformApps.bundle`
- Использует Perpetual лицензию с привязкой к машине
- Имеет Feature Limits (reporting.maxPerCall = 500)

## Отличия PluginDll от Bundle

| Аспект | PluginDll | Bundle |
|--------|-----------|--------|
| Формат | Одна DLL | .bundle.zip (PackageContents.xml) |
| Установка | NETLOAD / копирование | Распаковка в ApplicationPlugins |
| Автозагрузка | Через GrossGeoPlatformApps.bundle | Через PackageContents.xml |
| Зависимости | Минимальные | Любые (в Contents/) |
| Подходит для | Простых плагинов | Сложных плагинов |

## 🔧 Команды AutoCAD

| Команда | Описание | Требует лицензию |
|---------|----------|------------------|
| `GGDLLTEST` | Базовая команда | ✅ Да |
| `GGDLLINFO` | Информация о лицензии (v2 API) | ❌ Нет |
| `GGDLLREPORT` | Отчёт с Feature Limits | ✅ Да (feature: reporting) |
| `GGDLLHELP` | Справка по командам | ❌ Нет |

## 🧪 Тестовые сценарии

1. **Установка**: Загрузить DLL через `NETLOAD` → плагин инициализируется
2. **Trial**: Запустить Trial 14 дней → все команды доступны
3. **Perpetual покупка**: Купить Pro → бессрочный доступ
4. **Feature Limits**: `GGDLLREPORT` проверяет лимит maxPerCall=500
5. **Offline**: Отключить сеть → Grace Period 7 дней

## 📦 Структура

```
TestProduct.PluginDll/
├── TestProduct.PluginDll.csproj
├── TestPluginDllPlugin.cs
├── plans-manifest.json
├── release-manifest.json
└── README.md
```

> **Примечание:** В отличие от Bundle-продуктов, здесь нет папки `.bundle/` с `PackageContents.xml`.
> DLL устанавливается непосредственно в `GrossGeoPlatformApps.bundle/Contents/`.
