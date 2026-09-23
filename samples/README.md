# GrossGeo SDK — Примеры плагинов

Тестовые AutoCAD-плагины, демонстрирующие разные модели лицензирования GrossGeo Platform
через NuGet-пакет [`GrossGeo.SDK.Stub`](https://www.nuget.org/packages/GrossGeo.SDK.Stub).

> Примеры подключают SDK из NuGet; **актуальная версия закреплена в [README пакета](../README.md)**.
> Все они используют паттерн `ProductLicenseAccessor` для корректной работы нескольких плагинов
> в одном процессе AutoCAD. См. [Multi-plugin](../README.md#multi-plugin-несколько-плагинов-в-одном-процессе).

## Продукты

| Проект | Сценарий лицензирования | Distribution |
|--------|-------------------------|--------------|
| [TestProduct.Free](./TestProduct.Free/) | Бесплатный продукт, работает без лицензии (default-фичи) | Bundle |
| [TestProduct.Subscription](./TestProduct.Subscription/) | Подписка (Pro/Pro+) с Trial и Feature Limits | Bundle |
| [TestProduct.Freemium](./TestProduct.Freemium/) | Free-план + Pro по подписке с лимитами | Bundle |
| [TestProduct.Licensed](./TestProduct.Licensed/) | Perpetual + Maintenance | Bundle |
| [TestProduct.Concurrent](./TestProduct.Concurrent/) | Плавающие лицензии (Concurrent sessions) | Bundle |
| [TestProduct.Analytics](./TestProduct.Analytics/) | Внешнее лицензирование (ExternalOnly) + аналитика | Bundle |
| [TestProduct.Installer](./TestProduct.Installer/) | Дистрибуция через EXE/MSI | Installer |
| [TestProduct.PluginDll](./TestProduct.PluginDll/) | Одиночная DLL без bundle | PluginDll |

Каждый продукт описан двумя манифестами рядом с проектом — `plans-manifest.json` (планы и фичи)
и `release-manifest.json` (метаданные релиза). Подробности формата — в [корневом README](../README.md#манифесты-продукта).

## Быстрый старт

### 1. Получите ProductKey в Developer Portal

1. Войдите в **Developer Portal** ([grossgeo.ru/developer](https://grossgeo.ru/developer)) под учётной записью разработчика.
2. Создайте продукт (или откройте существующий) и скопируйте его **ProductKey** (формат `GG-XXXX-XXXX-XXXX-XXXX`).
3. Подставьте ключ в константу `ProductKey` соответствующего примера (в файле `Test*Plugin.cs`).

> Ключи в примерах — демонстрационные плейсхолдеры. С чужим ключом SDK инициализируется,
> но лицензия будет невалидной (плагин работает в Free-режиме). Для рабочей проверки нужен
> ваш собственный ключ зарегистрированного продукта.

### 2. Соберите плагины

Примеры подключают SDK из NuGet, поэтому собираются автономно (интернет нужен только для первого `restore`).

```powershell
# Все сразу (PowerShell)
Get-ChildItem samples -Directory -Filter 'TestProduct.*' | ForEach-Object { dotnet build $_.FullName -c Release }

# Или по одному
dotnet build samples/TestProduct.Free -c Release
dotnet build samples/TestProduct.Subscription -c Release
dotnet build samples/TestProduct.Freemium -c Release
dotnet build samples/TestProduct.Licensed -c Release
dotnet build samples/TestProduct.Concurrent -c Release
dotnet build samples/TestProduct.Analytics -c Release
dotnet build samples/TestProduct.Installer -c Release
dotnet build samples/TestProduct.PluginDll -c Release
```

> **Требуется AutoCAD на машине сборки.** Проекты ссылаются на сборки AutoCAD (`accoremgd.dll`,
> `acdbmgd.dll`, `acmgd.dll`, `AdWindows.dll`) по стандартным путям. Если установлена другая версия
> или путь отличается — отредактируйте `<HintPath>` в `.csproj` (см. раздел «Пути к AutoCAD» ниже).

### 3. Опубликуйте релиз в Developer Portal

1. Для своего продукта создайте релиз и загрузите собранный дистрибутив
   (для Bundle — ZIP из `bin/Release/<tfm>/<Project>.bundle_<tfm>.zip`; для PluginDll — DLL; для Installer — EXE/MSI).
2. Платформа читает `release-manifest.json` из bundle для автозаполнения метаданных релиза.

### 4. Установите через User Panel и загрузите в AutoCAD

1. Запустите **User Panel** и войдите в свою учётную запись.
2. Найдите продукт в каталоге и установите его. Лицензия и обновления доставляются плагину
   через User Panel по Named Pipe IPC — отдельная настройка не требуется.
3. Bundle-плагины автозагружаются в AutoCAD; одиночную DLL можно загрузить вручную:

```
NETLOAD → выберите DLL из bin/Release/net8.0-windows/
```

## Multi-plugin паттерн

Все примеры используют `ProductLicenseAccessor` для изоляции лицензий между плагинами в одном процессе AutoCAD:

```csharp
private static ProductLicenseAccessor? _license;

// В Initialize():
await GrossGeoLicense.Initialize(new LicenseOptions { ProductKey = ProductKey, PluginVersion = "1.0.0" });
_license = GrossGeoLicense.ForProduct(ProductKey);

// В командах:
_license?.Protect(() => DoWork(), () => ShowUpgrade());
_license?.RequireFeature("export", () => DoExport(), () => ShowUpgrade());

// В Terminate():
GrossGeoLicense.Shutdown(ProductKey);
```

**Почему?** Статические свойства `GrossGeoLicense.IsValid`, `GrossGeoLicense.PlanTier` и т.д. возвращают данные
**последнего** инициализированного продукта. При нескольких плагинах в одном AutoCAD это даёт конфликты —
`ProductLicenseAccessor` решает проблему. Подробности — в [корневом README](../README.md#multi-plugin-несколько-плагинов-в-одном-процессе).

## Офлайн-режим

Если User Panel не запущен, SDK работает из локального кэша (DPAPI) — верит последнему подписанному
сервером вердикту, пока не истечёт его срок. Срок зависит от пары «модель плана × тип привязки», а
не от одной общей константы, и большинство сочетаний упираются в общий потолок 30 дней:

| Модель плана · привязка | Срок дискового кэша (Panel не запущен) |
|---|---|
| Subscription · `User` | 7 дней, но не дольше оплаченного периода подписки |
| Subscription · `Machine` | 30 дней автоматически, но не дольше оплаченного периода подписки |
| `Concurrent` (любая модель плана) | не поддерживается — без сети `IsValid = false` сразу |
| Perpetual · `User` / `Machine` | 30 дней (общий потолок) |
| Free (базис) | 30 дней (общий потолок) |
| Trial (любая привязка) | по сроку триала: истёк — истёк, независимо от связи |

Активация участником больше не требуется — окно подписывается автоматически при каждой успешной
проверке лицензии, и это единственное, что решает срок дискового кэша SDK. У машинной привязки (`Machine`)
отдельно есть серверная аренда (lease, 24 ч + 3 дня grace, у `Concurrent` короче); у привязки к участнику
(`User`) её нет — место участника к машине не привязывается, и продление аренды не запускается. Аренда
переживает отсутствие сети иначе — на пути **User Panel**, которая при недоступном сервере проверяет её локально
сохранённое состояние. Офлайн-грант (`OfflineModeExpiresAt`, машинная привязка, задуман на 30 дней) сегодня не запрашивает
никто из отгруженных клиентов: кнопка активации снята из панели, а у SDK публичного вызова нет — типы
`ActivateOffline`/`DeactivateOffline` в протоколе IPC сохранены, но не отправляются; серверная дверь
запроса при этом существует. Ни аренда, ни
грант в подписанный payload SDK не входят и решений дискового кэша не меняют — при НЕЗАПУЩЕННОЙ
панели действует только срок из таблицы выше.

По истечении срока `IsValid = false` и плагин переходит в Free-режим (если он есть у продукта). Это
позволяет проверять graceful degradation без запущенного User Panel. Полные правила, включая grace
по неоплате (3 дня, действует наименьший срок) и приоритет между сроком продукта
(`LicenseOptions.GracePeriodDays`) и сервера, — `licensing-model-v3-spec.md` §11.2 в приватном
репозитории.

## Пути к AutoCAD

Проекты настроены на стандартные пути установки:

- AutoCAD 2025–2026 (net8.0-windows): `C:\Program Files\Autodesk\AutoCAD 2025\`
- AutoCAD 2019–2024 (net48): `C:\Program Files\Autodesk\AutoCAD 2024\`

Если пути отличаются — отредактируйте `<HintPath>` в соответствующих `.csproj`.

## Ссылки

- [Корневой README SDK](../README.md) — полная документация по интеграции лицензирования
- [CHANGELOG](../CHANGELOG.md) — история версий SDK
- [Developer Portal](https://grossgeo.ru/developer)
