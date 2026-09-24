# GrossGeo SDK — Руководство разработчика плагинов

**Версия SDK:** 2.0  
**Дата:** Июль 2025  
**Для кого:** Разработчики плагинов для AutoCAD / Civil 3D, желающие монетизировать и распространять свои продукты через платформу GrossGeo.

> ⚠️ **Уведомление об устаревшей секции (2026-05-08, DOC-033):** разделы §14 (структура проекта) и §15 (`product-manifest.json`) описывают legacy-формат манифеста до BC-PR2/BC-PR3 (2026-05-06). Текущая модель — два отдельных файла:
> - **`plans-manifest.json`** — импорт планов и фич на существующий продукт через Developer Panel (грузится через UI).
> - **`release-manifest.json`** — метаданные релиза, лежит **внутри bundle** и читается сервером при upload.
>
> `plans-manifest.json` разрабатывается в репозитории студии и загружается через Developer Panel; `release-manifest.json` формируется и кладётся внутрь `.bundle` при подготовке релиза. Примеры обоих форматов по каждому тарифному сценарию — в `plans-manifest.json`/`release-manifest.json` внутри любого из [samples](../samples/) (например, `samples/TestProduct.Subscription/` — для планов с триалом). Полный sweep §14/§15 — отдельная задача (DOC-033 partial closure 2026-05-08).

---

## Содержание

1. [Что такое GrossGeo SDK](#1-что-такое-grossgeo-sdk)
2. [Как это работает — общая картина](#2-как-это-работает--общая-картина)
3. [Требования к окружению](#3-требования-к-окружению)
4. [Установка SDK](#4-установка-sdk)
5. [Создание плагина с нуля — пошаговое руководство](#5-создание-плагина-с-нуля--пошаговое-руководство)
6. [Инициализация SDK в плагине](#6-инициализация-sdk-в-плагине)
7. [Защита функций лицензией](#7-защита-функций-лицензией)
8. [Работа с Feature Flags (фичи)](#8-работа-с-feature-flags-фичи)
   - [Ключевой материал возможности (Feature Key Material)](#ключевой-материал-возможности-feature-key-material)
9. [Feature Limits — количественные ограничения](#9-feature-limits--количественные-ограничения)
10. [Фичи по умолчанию (isDefault)](#10-фичи-по-умолчанию-isdefault)
11. [Concurrent Sessions — плавающие лицензии](#11-concurrent-sessions--плавающие-лицензии)
12. [Проверка обновлений](#12-проверка-обновлений)
13. [Бизнес-модели — полные примеры](#13-бизнес-модели--полные-примеры)
14. [Структура проекта и упаковка bundle](#14-структура-проекта-и-упаковка-bundle)
15. [Манифест продукта (product-manifest.json)](#15-манифест-продукта-product-manifestjson)
16. [Процесс публикации на платформе](#16-процесс-публикации-на-платформе)
17. [Обработка ошибок и исключения](#17-обработка-ошибок-и-исключения)
18. [Offline-режим и Grace Period](#18-offline-режим-и-grace-period)
19. [Диагностика и логирование](#19-диагностика-и-логирование)
20. [API-справочник](#20-api-справочник)
21. [Troubleshooting — частые проблемы](#21-troubleshooting--частые-проблемы)
22. [FAQ](#22-faq)

> **См. также:** [Deep-links `grossgeo://`](grossgeo-sdk-deeplinks-guide.md) — как открыть из плагина страницу покупки лицензии, карточку продукта или отзывы в GrossGeo User Panel.

---

## 1. Что такое GrossGeo SDK

GrossGeo SDK — это библиотека, которую вы подключаете к своему AutoCAD-плагину, чтобы:

- **Защитить** свой код лицензией (подписка, разовая покупка, freemium)
- **Управлять** доступом к отдельным функциям (feature flags)
- **Ограничивать** количественные параметры по плану (лимиты)
- **Распространять** плагин через каталог платформы GrossGeo
- **Монетизировать** продукт (подписки, разовые покупки, concurrent лицензии)
- **Обновлять** плагин с автоматическим уведомлением пользователей

SDK — это тонкий клиент (~25 КБ). Он не содержит серверной логики, не делает HTTP-запросов и не хранит конфиденциальные данные. Вся тяжёлая работа происходит в приложении **GrossGeo User Panel**, которое установлено у конечного пользователя.

---

## 2. Как это работает — общая картина

```
┌──────────────────────────┐         ┌─────────────────────────┐
│   AutoCAD                │         │   GrossGeo User Panel   │
│                          │         │   (отдельное приложение) │
│   Ваш плагин             │ ──IPC──▶│                         │──HTTP──▶ Сервер GrossGeo
│   + GrossGeo.SDK.Stub    │◀──────  │   Проверка лицензий     │
│                          │         │   Управление подписками  │
└──────────────────────────┘         │   Кэш и безопасность    │
                                     └─────────────────────────┘
```

**Пояснение:**

1. Ваш плагин загружается в AutoCAD и вызывает `GrossGeoLicense.Initialize(...)`.
2. SDK связывается с User Panel через локальный канал (IPC — межпроцессное взаимодействие).
3. User Panel проверяет лицензию (локально или на сервере) и возвращает результат.
4. SDK кэширует результат, чтобы плагин мог работать даже при временной недоступности User Panel (offline-режим).

**Для вас это значит:**
- Вам не нужно писать серверный код.
- Вам не нужно обрабатывать платежи.
- Вам не нужно реализовывать систему лицензирования — всё готово.
- Вы просто вызываете методы SDK для защиты своего кода.

---

## 3. Требования к окружению

### У разработчика (вашего компьютера)

| Компонент | Версия | Зачем |
|-----------|--------|-------|
| Visual Studio | 2022+ | Разработка |
| .NET SDK | 8.0 | Сборка проекта |
| .NET Framework | 4.8 Developer Pack | Если плагин для AutoCAD 2019–2024 |
| AutoCAD / Civil 3D | 2019–2027 | Тестирование |
| GrossGeo User Panel | 2.0+ | Тестирование лицензирования |

### У конечного пользователя

| Компонент | Версия |
|-----------|--------|
| Windows | 10/11 x64 |
| AutoCAD / Civil 3D | 2019–2027 |
| GrossGeo User Panel | 2.0+ (должен быть установлен и запущен) |

> **Важно:** User Panel — это отдельное приложение, которое пользователь устанавливает один раз. Все ваши плагины используют один и тот же User Panel.

---

## 4. Установка SDK

### Через NuGet (рекомендуется)

В файле `.csproj` вашего плагина добавьте:

```xml
<PackageReference Include="GrossGeo.SDK.Stub" Version="2.2.1" />
```

**Версия закреплена точно, и это не перестраховка.** Плавающий диапазон `2.*` разрешается в
любую версию ветки 2 — включая ту, где обфускация ломала разбор типов, и продукт видел
действующую лицензию как `Free` или `Invalid`. Такие версии сняты с витрины, но уже собранные
проекты продолжают их тянуть: unlist не удаляет пакет. Обновляйте версию осознанно, а не
диапазоном.

Или через Package Manager Console в Visual Studio:

```
Install-Package GrossGeo.SDK.Stub
```

NuGet автоматически подтянет зависимость `GrossGeo.Contracts`.

### Ручная установка

Если вы не используете NuGet:

1. Скачайте файлы из Developer Portal:
   - `GrossGeo.SDK.Stub.dll`
   - `GrossGeo.Contracts.dll`
2. Скопируйте их в папку вашего проекта.
3. Добавьте Reference на оба файла.

---

## 5. Создание плагина с нуля — пошаговое руководство

Разберём создание плагина от пустого проекта до рабочего bundle.

### Шаг 1. Создайте проект

В Visual Studio: **File → New → Project → Class Library**.

- Имя: `MyPlugin`
- Framework: выберите `.NET 8.0` (для AutoCAD 2025+) или `.NET Framework 4.8` (для 2019–2024).

> **Совет:** Для поддержки обеих версий AutoCAD используйте multi-target:
> `<TargetFrameworks>net48;net8.0-windows</TargetFrameworks>`

### Шаг 2. Настройте .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- Multi-target для AutoCAD 2019-2024 (net48) и 2025+ (net8.0) -->
    <TargetFrameworks>net48;net8.0-windows</TargetFrameworks>
    <RootNamespace>MyPlugin</RootNamespace>
    <AssemblyName>MyPlugin</AssemblyName>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>

    <!-- Копировать зависимости в output -->
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>

    <Version>1.0.0</Version>
    <Authors>Ваша студия</Authors>
    <Description>Описание плагина</Description>
  </PropertyGroup>

  <!-- GrossGeo SDK -->
  <ItemGroup>
    <PackageReference Include="GrossGeo.SDK.Stub" Version="2.2.1" />
  </ItemGroup>

  <!-- AutoCAD References для .NET Framework 4.8 (AutoCAD 2019-2024) -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net48'">
    <Reference Include="accoremgd">
      <HintPath>C:\ObjectARX\2024\inc\accoremgd.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="acdbmgd">
      <HintPath>C:\ObjectARX\2024\inc\acdbmgd.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="acmgd">
      <HintPath>C:\ObjectARX\2024\inc\acmgd.dll</HintPath>
      <Private>False</Private>
    </Reference>
  </ItemGroup>

  <!-- AutoCAD References для .NET 8 (AutoCAD 2025+) -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0-windows'">
    <Reference Include="accoremgd">
      <HintPath>C:\ObjectARX\2025\inc\accoremgd.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="acdbmgd">
      <HintPath>C:\ObjectARX\2025\inc\acdbmgd.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="acmgd">
      <HintPath>C:\ObjectARX\2025\inc\acmgd.dll</HintPath>
      <Private>False</Private>
    </Reference>
  </ItemGroup>

</Project>
```

> **Пути к ObjectARX:** Замените `C:\ObjectARX\...` на реальные пути к DLL AutoCAD на вашей машине. Скачать ObjectARX SDK можно с сайта Autodesk.

### Шаг 3. Напишите код плагина

Создайте файл `Plugin.cs`:

```csharp
using System;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GrossGeo.SDK;

// Регистрация плагина в AutoCAD
[assembly: ExtensionApplication(typeof(MyPlugin.Plugin))]
[assembly: CommandClass(typeof(MyPlugin.Plugin))]

namespace MyPlugin
{
    /// <summary>
    /// Главный класс плагина. AutoCAD вызовет Initialize() при загрузке.
    /// </summary>
    public class Plugin : IExtensionApplication
    {
        // Ключ продукта — получите в Developer Portal после создания продукта.
        // DOC-098: строка ниже — ЗАГЛУШКА, а не код для запуска. `X` не hex-символ,
        // и SDK отклонит ИМЕННО ЭТУ строку исключением ArgumentException
        // ("Invalid ProductKey format") прямо на Initialize() — замените на
        // настоящий ключ (0-9/A-F вместо X), который Developer Portal покажет
        // в карточке продукта сразу после его создания.
        private const string ProductKey = "GG-XXXX-XXXX-XXXX-XXXX";

        // Для удобства доступа к редактору AutoCAD
        private static Editor? Ed =>
            Application.DocumentManager?.MdiActiveDocument?.Editor;

        // Главный поток AutoCAD — Control, созданный в Initialize (см. RunOnMainThread).
        private static System.Windows.Forms.Control? _ui;

        /// <summary>
        /// Вызывается AutoCAD при загрузке плагина.
        /// </summary>
        public void Initialize()
        {
            // Главный поток запоминаем ЗДЕСЬ — синхронно, до первого await и не в Task.Run.
            _ui = new System.Windows.Forms.Control();
            _ = _ui.Handle;

            Ed?.WriteMessage("\n[MyPlugin] Загрузка...");

            // Инициализируем SDK в фоновом потоке, чтобы не блокировать AutoCAD.
            // Внутри Task.Run мы НЕ на главном потоке: Editor и прочий AutoCAD API
            // трогать только через RunOnMainThread (ниже).
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await GrossGeoLicense.Initialize(new LicenseOptions
                    {
                        ProductKey = ProductKey,
                        PluginVersion = "1.0.0",
                        GracePeriodDays = 7
                    });

                    if (result.IsValid)
                        RunOnMainThread(() => Ed?.WriteMessage("\n[MyPlugin] Лицензия активна!"));
                    else
                        RunOnMainThread(() => Ed?.WriteMessage($"\n[MyPlugin] Лицензия: {result.Message}"));
                }
                catch (Exception ex)
                {
                    RunOnMainThread(() => Ed?.WriteMessage($"\n[MyPlugin] Ошибка SDK: {ex.Message}"));
                }
            });
        }

        /// <summary>
        /// Выполнить действие на главном потоке AutoCAD через Control, созданный в Initialize.
        /// Нужна везде, где код идёт с фонового потока: после await (в Task.Run и в async-командах)
        /// и в обработчиках событий SDK (SessionExpired, LicenseRefreshed). InvokeRequired и BeginInvoke
        /// безопасны с любого потока; на главном потоке действие выполняется сразу. Исключение действия
        /// ловится здесь: на главном потоке оно уронило бы AutoCAD.
        /// </summary>
        private static void RunOnMainThread(Action action)
        {
            var ui = _ui;
            if (ui == null)
            {
                System.Diagnostics.Debug.WriteLine("[MyPlugin] главный поток не запомнен в Initialize — вывод пропущен");
                return;
            }

            if (!ui.InvokeRequired)
            {
                RunSafely(action);
                return;
            }

            try
            {
                ui.BeginInvoke(new Action(() => RunSafely(action)));
            }
            catch (InvalidOperationException ex)
            {
                // Control уже уничтожен — AutoCAD закрывается.
                System.Diagnostics.Debug.WriteLine($"[MyPlugin] вывод не доставлен: {ex.Message}");
            }
        }

        private static void RunSafely(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MyPlugin] {ex}");
            }
        }

        /// <summary>
        /// Вызывается AutoCAD при выгрузке плагина.
        /// </summary>
        public void Terminate()
        {
            GrossGeoLicense.Shutdown();
        }

        /// <summary>
        /// Команда, доступная только при наличии лицензии.
        /// Пользователь вводит MYPLUGIN_RUN в командной строке AutoCAD.
        /// </summary>
        [CommandMethod("MYPLUGIN_RUN")]
        public void RunCommand()
        {
            // ОБЯЗАТЕЛЬНО: дождитесь инициализации. Она идёт в фоне (иначе встанет загрузка
            // AutoCAD), и команду можно запустить раньше, чем появится вердикт. Без ожидания
            // вы получите отказ, НЕОТЛИЧИМЫЙ от «лицензии нет», и покажете пользователю
            // «купите лицензию» там, где надо было «подождите пару секунд».
            var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
            if (ready.Status == LicenseCheckStatus.Unknown)
            {
                Ed?.WriteMessage("\n" + ready.Message);
                return;
            }

            GrossGeoLicense.Protect(
                action: () =>
                {
                    // Этот код выполнится только если лицензия валидна
                    Ed?.WriteMessage("\n[MyPlugin] Команда выполнена!");
                    // ... ваш код работы с чертежом ...
                },
                onBlocked: () =>
                {
                    // Этот код выполнится если лицензия невалидна
                    Ed?.WriteMessage("\n[MyPlugin] Требуется лицензия.");
                    Ed?.WriteMessage("\n[MyPlugin] Откройте GrossGeo User Panel для активации.");
                });
        }
    }
}
```

> **Какой приём выбрать.** В WinForms-проекте — `System.Windows.Forms.Control`, как выше (net48:
> `<Reference Include="System.Windows.Forms" />`, net8: `<UseWindowsForms>true</UseWindowsForms>`); в WPF-проекте —
> `System.Windows.Threading.Dispatcher.CurrentDispatcher`, запомненный в `Initialize`, с `CheckAccess()`/`BeginInvoke`
> (так сделано в образцах `Samples/`). В обоих случаях захват — синхронно в `Initialize`, до первого `await`: иначе
> запомнится поток пула. Пишите тип полным именем — в одном файле с `Autodesk.AutoCAD.ApplicationServices` не должно
> быть второго `Application`.

### Шаг 4. Соберите проект

```bash
dotnet build
```

В папке `bin\Debug\net8.0-windows\` (или `net48`) появятся:
- `MyPlugin.dll` — ваш плагин
- `GrossGeo.SDK.Stub.dll` — SDK
- `GrossGeo.Contracts.dll` — контракты

### Шаг 5. Создайте bundle

AutoCAD загружает плагины из bundle — специальной папки со структурой. Подробнее — в разделе [14. Структура проекта и упаковка bundle](#14-структура-проекта-и-упаковка-bundle).

### Шаг 6. Протестируйте

1. Запустите GrossGeo User Panel.
2. Запустите AutoCAD.
3. Загрузите плагин через `NETLOAD` или поместите bundle в
   `%APPDATA%\Autodesk\ApplicationPlugins\`.
4. Введите команду `MYPLUGIN_RUN` в командной строке AutoCAD.

---

## 6. Инициализация SDK в плагине

### Зачем инициализировать

При вызове `Initialize` SDK:
1. Подключается к User Panel.
2. Проверяет лицензию для вашего продукта.
3. Получает список доступных фич и лимитов.
4. Кэширует результат для offline-работы.

### Асинхронная инициализация (рекомендуется)

```csharp
public void Initialize()
{
    // Запускаем в фоне, чтобы не блокировать загрузку AutoCAD
    _ = Task.Run(async () =>
    {
        var result = await GrossGeoLicense.Initialize(new LicenseOptions
        {
            // DOC-098: замените на настоящий ключ из Developer Portal — этот
            // литерал не пройдёт формат-валидацию SDK, см. предупреждение в
            // разделе 5, шаг 3.
            ProductKey = "GG-XXXX-XXXX-XXXX-XXXX",
            PluginVersion = "1.0.0",
            GracePeriodDays = 7,          // сколько дней работать без сети
            IpcTimeoutSeconds = 10,       // таймаут подключения к User Panel
            CheckForUpdatesOnInit = true   // проверить обновления при старте
        });

        // result содержит информацию о лицензии
        if (result.IsValid)
        {
            // Лицензия активна
            // result.PlanTier — уровень плана (Free, Pro, ProPlus)
            // result.Features — список доступных фич
            // result.FeatureLimits — лимиты фич
        }
    });
}
```

### Синхронная инициализация

Если нужно дождаться результата перед продолжением (блокирует поток):

```csharp
public void Initialize()
{
    var result = GrossGeoLicense.InitializeSync(new LicenseOptions
    {
        // DOC-098: замените на настоящий ключ — литерал ниже не пройдёт
        // формат-валидацию SDK, см. раздел 5, шаг 3.
        ProductKey = "GG-XXXX-XXXX-XXXX-XXXX",
        PluginVersion = "1.0.0"
    });

    if (!result.IsValid)
    {
        Ed?.WriteMessage($"\n[MyPlugin] Лицензия недействительна: {result.Message}");
    }
}
```

### Параметры LicenseOptions

| Параметр | Тип | Значение по умолчанию | Описание |
|----------|-----|-----------------------|----------|
| `ProductKey` | `string` | **(обязательно)** | Ключ вашего продукта. Формат: `GG-XXXX-XXXX-XXXX-XXXX`, где каждый `X` — hex-цифра (`0-9`/`A-F`). Получите в Developer Portal — карточка продукта показывает готовую строку сразу после создания. **Буквальная подстановка `GG-XXXX-XXXX-XXXX-XXXX` из примеров этого раздела не пройдёт валидацию SDK** (`X` — не hex) и `Initialize`/`InitializeSync` вернёт `ArgumentException`, а не «продукт не найден» или иной сетевой отказ. |
| `PluginVersion` | `string?` | `null` | Версия вашего плагина. Используется для проверки обновлений и аналитики. |
| `GracePeriodDays` | `int` | `7` | Сколько дней плагин может работать без связи с User Panel (offline). Допустимо: 0–30. |
| `IpcTimeoutSeconds` | `int` | `10` | Таймаут подключения к User Panel, в секундах. Допустимо: 1–120. |
| `CheckForUpdatesOnInit` | `bool` | `true` | Проверять наличие обновлений при инициализации. |
| `CacheDirectory` | `string?` | `%LocalAppData%\GrossGeo\SDK.Stub\` | Папка для кэша лицензии. |
| `Logger` | `ILicenseLogger?` | `null` | Пользовательский логгер (см. раздел 19). |

### Завершение работы

Вызывайте `Shutdown()` в методе `Terminate()`:

```csharp
public void Terminate()
{
    GrossGeoLicense.Shutdown();
}
```

### Несколько продуктов в одном процессе AutoCAD (DOC-099)

Если у вас два плагина (два разных `ProductKey`) грузятся в один и тот же
`acad.exe` — например, разработчик ставит себе оба своих продукта сразу, —
**статические свойства и методы `GrossGeoLicense`** (`IsValid`, `LastResult`,
`WaitUntilReady`, статический `Protect`) отражают результат **последнего
инициализированного продукта**, а не вашего собственного. Второй плагин,
вызвавший `Initialize` позже первого, «перетирает» то, что видят статические
свойства для обоих.

Используйте `GrossGeoLicense.ForProduct(productKey)` — он возвращает
`ProductLicenseAccessor`, привязанный к конкретному продукту, и именно его
`IsValid`/`Protect`/`HasFeature` стоит вызывать в командах, а не статические
аналоги:

```csharp
private static ProductLicenseAccessor License => GrossGeoLicense.ForProduct(ProductKey);

[CommandMethod("MY_CMD")]
public void MyCommand()
{
    // Ждём готовности ИМЕННО СВОЕГО продукта — не статический GrossGeoLicense.WaitUntilReady,
    // который в этом сценарии, как и статический Protect ниже, отвечает за ЛЮБОЙ из
    // инициализированных продуктов, а не обязательно за ваш (см. предупреждение ниже).
    var ready = License.WaitUntilReady(TimeSpan.FromSeconds(10));
    if (ready.Status == LicenseCheckStatus.Unknown)
    {
        Ed?.WriteMessage("\n" + ready.Message);
        return;
    }

    License.Protect(
        action: () => { /* защищённый код */ },
        onBlocked: reason => Ed?.WriteMessage("\n" + (reason?.Message ?? "Лицензия недействительна.")));
}
```

**Ловушка, которую эта замена закрывала бы не полностью без `WaitUntilReady` на самом
`ProductLicenseAccessor`.** Статический `GrossGeoLicense.WaitUntilReady` ждёт готовности
ЛЮБОГО из инициализированных продуктов, а не именно вашего: если в процессе два продукта и
чужой закончил проверку раньше вашего, он вернёт управление, а ваш `License` (через
`ForProduct`) всё ещё не будет знать своего вердикта — `Protect` получил бы `onBlocked` с
`LicenseResult.NotCheckedYet()` (статус `Unknown`), неотличимый на вид от настоящего отказа.
Пример выше поэтому зовёт `License.WaitUntilReady`, а не статический — она ждёт готовности
именно своего продукта и в этом сценарии не подвержена гонке.

---

## 7. Защита функций лицензией

SDK предоставляет несколько способов защитить ваш код.

### Способ 1: Protect — мягкая защита (рекомендуется)

Выполняет действие, если лицензия валидна. Иначе вызывает fallback-функцию:

```csharp
[CommandMethod("EXPORT")]
public void ExportCommand()
{
    // LGC-720: дождитесь инициализации — она идёт в фоне, и команду можно запустить
    // раньше, чем появится вердикт. Без ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
    var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
    if (ready.Status == LicenseCheckStatus.Unknown) { Ed?.WriteMessage("\n" + ready.Message); return; }
    GrossGeoLicense.Protect(
        action: () =>
        {
            // Этот код выполнится ТОЛЬКО при валидной лицензии
            DoExport();
            Ed?.WriteMessage("\nЭкспорт завершён!");
        },
        onBlocked: () =>
        {
            // Этот код выполнится, если лицензия невалидна
            Ed?.WriteMessage("\nДля экспорта требуется лицензия.");
            Ed?.WriteMessage("\nОткройте GrossGeo User Panel для активации.");
        });
}
```

С возвратом значения:

```csharp
var result = GrossGeoLicense.Protect(
    () => CalculateArea(selection),   // вернёт double
    () => 0.0);                      // fallback значение
```

### Способ 2: ProtectOrThrow — строгая защита

Выбрасывает `LicenseException`, если лицензия невалидна:

```csharp
[CommandMethod("CRITICAL_CMD")]
public void CriticalCommand()
{
    // LGC-720: дождитесь инициализации — она идёт в фоне, и команду можно запустить
    // раньше, чем появится вердикт. Без ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
    var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
    if (ready.Status == LicenseCheckStatus.Unknown) { Ed?.WriteMessage("\n" + ready.Message); return; }
    try
    {
        GrossGeoLicense.ProtectOrThrow(() =>
        {
            // Выполнится только при валидной лицензии
            DoCriticalWork();
        });
    }
    catch (LicenseException ex)
    {
        Ed?.WriteMessage($"\nОшибка лицензии: {ex.Status}");
    }
}
```

### Способ 3: Ручная проверка

Если нужна более гибкая логика:

```csharp
[CommandMethod("FLEXIBLE_CMD")]
public void FlexibleCommand()
{
    if (!GrossGeoLicense.IsValid)
    {
        Ed?.WriteMessage("\nЛицензия недействительна.");

        // Можно узнать почему
        var lastResult = GrossGeoLicense.LastResult;
        if (lastResult != null)
        {
            switch (lastResult.Status)
            {
                case LicenseCheckStatus.Expired:
                    Ed?.WriteMessage("\nПодписка истекла. Продлите в User Panel.");
                    break;
                case LicenseCheckStatus.NotFound:
                    Ed?.WriteMessage("\nЛицензия не найдена. Купите в User Panel.");
                    break;
                case LicenseCheckStatus.NetworkError:
                    Ed?.WriteMessage("\nНет связи с User Panel. Запустите его.");
                    break;
            }
        }
        return;
    }

    // Лицензия валидна — выполняем работу
    DoWork();
}
```

### Guard-классы (удобные алиасы)

Для более лаконичного кода можно использовать классы-обёртки:

```csharp
// Вместо GrossGeoLicense.Protect(...)
LicenseGuard.Protect(() => DoWork(), () => ShowBlocked());

// Вместо GrossGeoLicense.ProtectOrThrow(...)
LicenseGuard.OrThrow(() => DoWork());

// Проверка
if (LicenseGuard.IsValid) { ... }
```

---

## 8. Работа с Feature Flags (фичи)

**Feature Flag** — это строковый код, определяющий доступность конкретной функции плагина.

Например, ваш плагин может иметь фичи:
- `basic-tools` — базовые инструменты (доступны всем)
- `advanced-export` — расширенный экспорт (только для Pro)
- `batch-processing` — пакетная обработка (только для ProPlus)

Фичи и их привязка к планам настраиваются в Developer Portal.

### Проверка наличия фичи

```csharp
// Простая проверка
if (GrossGeoLicense.HasFeature("advanced-export"))
{
    DoAdvancedExport();
}
else
{
    Ed?.WriteMessage("\nДля расширенного экспорта обновитесь до Pro.");
}
```

### Мягкая защита фичи

```csharp
GrossGeoLicense.RequireFeature("batch-processing",
    action: () =>
    {
        // Фича доступна — выполняем
        DoBatchProcessing();
    },
    onMissing: () =>
    {
        // Фичи нет — показываем сообщение
        Ed?.WriteMessage("\nПакетная обработка доступна в плане Pro+.");
    });
```

### Строгая защита фичи (с исключением)

```csharp
try
{
    GrossGeoLicense.RequireFeatureOrThrow("advanced-export", () =>
    {
        DoAdvancedExport();
    });
}
catch (FeatureNotAvailableException ex)
{
    Ed?.WriteMessage($"\nФункция '{ex.FeatureCode}' недоступна в вашем плане.");
}
```

### Guard-класс для фичей

```csharp
// Краткий синтаксис
if (FeatureGuard.Has("advanced-export")) { ... }

FeatureGuard.Require("batch-processing",
    () => DoBatch(),
    () => ShowUpgrade());

FeatureGuard.OrThrow("batch-processing", () => DoBatch());
```

### Асинхронная проверка (с обновлением данных)

```csharp
// Если нужно получить актуальные данные с сервера (не из кэша)
var hasFeature = await GrossGeoLicense.HasFeatureAsync("advanced-export");
```

### Ключевой материал возможности (Feature Key Material)

> **Начиная с SDK 2.2.0** (SDK, серверная и панельная части выпущены — панели с `1.0.2637.18001`,
> 18.09.2026).

`HasFeature` отвечает «да/нет» и ничего не защищает от подмены: патч, возвращающий `true`,
открывает фичу мгновенно. Для фичей, чья ценность лежит не в коде, а в **данных продукта**
(справочные таблицы, шаблоны, коэффициенты), этого недостаточно — нужен настоящий секрет, а не
флаг.

Для таких фичей платформа хранит и отдаёт **ключевой материал** — случайные байты, привязанные к
паре «код возможности + `kid`» (идентификатор версии секрета, который вы придумываете сами, например
`2026-09`, — нужен для ротации без поломки уже выпущенных релизов: старый `kid` продолжает
открывать данные, зашифрованные под ним, новый — новые). Вы шифруете этим материалом ценные данные
продукта при сборке релиза и расшифровываете на машине пользователя, получив материал через SDK.
Секрет заводится и ротируется в Developer Portal — сам SDK его не генерирует и панель разработчика
никогда его не показывает.

**Доступен только через продукт-скоупный аксессор**, как и остальной API, зависящий от конкретного
продукта в мультипродуктовом процессе (раздел 6 выше) — у статического `GrossGeoLicense` этого
члена нет:

```csharp
var accessor = GrossGeoLicense.ForProduct(ProductKey);

// Синхронно, из уже полученного вердикта
if (accessor.TryGetFeatureKey("advanced-templates", "2026-09", out byte[]? material))
{
    var decrypted = DecryptTemplates(EncryptedTemplatesBytes, material);
    // material — копия на каждый вызов; можно безопасно обнулить после использования,
    // на следующий вызов это не повлияет
}

// Или сразу узнать причину, если материала нет
var availability = accessor.GetFeatureKeyAvailability("advanced-templates", "2026-09");

// Асинхронно — дождаться готовности продукта перед первым обращением
var lookup = await accessor.GetFeatureKeyAsync(
    "advanced-templates", "2026-09", TimeSpan.FromSeconds(10));

if (lookup.Found)
    DecryptTemplates(EncryptedTemplatesBytes, lookup.Material);
```

**`FeatureKeyAvailability`** — почему материала может не быть и что сказать пользователю:

| Значение | Когда | Что сказать пользователю |
|---|---|---|
| `Unknown` | Проверка лицензии ещё не завершилась | «лицензия проверяется» |
| `Available` | Материал получен, срок не истёк | — (используйте `material`) |
| `NotEntitled` | Вердикт получен, но возможности нет в текущем плане | «возможность не входит в ваш план» |
| `KidNotIssued` | Возможность есть, но именно этот `kid` платформа не выдавала (отозван или никогда не существовал) | «обновите продукт до новой версии» |
| `PanelUnavailable` | GrossGeo User Panel не отвечает, вердикт взят из офлайн-запаса SDK | «откройте GrossGeo User Panel» |
| `OfflineExpired` | Материал был, но истёк срок офлайн-работы без новой связи с панелью | «восстановите подключение к User Panel» |
| `Denied` | Лицензия окончательно отклонена (истекла, отозвана и т. п.) | текст из `LicenseResult` |

**`PanelUnavailable` — не то же самое, что «лицензии нет».** Материал живёт только в памяти
процесса и **никогда не пишется на диск** — ни в офлайн-кэш SDK, ни куда-либо ещё; его единственный
источник — запущенная User Panel. SDK может при этом подтверждать саму лицензию (`IsValid = true`)
из собственного офлайн-кэша уже несколько дней — это разные механизмы с разным сроком жизни. Без
хотя бы недавнего живого ответа панели `GetFeatureKeyAvailability` вернёт `PanelUnavailable`, даже
когда лицензия в порядке. Показывайте пользователю разные сообщения для «нет прав» и «нет связи с
панелью» — второе решается открытием User Panel, а не покупкой.

---

## 9. Feature Limits — количественные ограничения

Feature Limits позволяют ограничивать не просто доступность фичи, а **количественные параметры**. Например:

- Free план: экспорт до 10 объектов за раз
- Pro план: экспорт до 1000 объектов
- ProPlus план: без ограничений

Лимиты настраиваются в Developer Portal и привязываются к связке «план + фича».

### Получить значение лимита

```csharp
// Получить лимит "maxObjects" для фичи "export"
int? limit = GrossGeoLicense.GetFeatureLimit("export", "maxObjects");

if (limit.HasValue)
    Ed?.WriteMessage($"\nВаш лимит: {limit.Value} объектов за раз.");
else
    Ed?.WriteMessage("\nОграничений нет — экспортируйте сколько хотите!");
```

### Проверить лимит

```csharp
// Проверить, не превышен ли лимит
int selectedCount = GetSelectedObjectsCount();

if (!GrossGeoLicense.CheckLimit("export", "maxObjects", selectedCount))
{
    int? max = GrossGeoLicense.GetFeatureLimit("export", "maxObjects");
    Ed?.WriteMessage($"\nВы выбрали {selectedCount} объектов, лимит: {max}.");
    Ed?.WriteMessage("\nОбновитесь до Pro для увеличения лимита.");
    return;
}

// Лимит не превышен — выполняем
DoExport(selectedObjects);
```

### Защита с лимитом

```csharp
// С fallback-функцией
GrossGeoLicense.RequireLimit(
    featureCode: "export",
    limitCode: "maxObjects",
    actualValue: selectedCount,
    action: () =>
    {
        // Лимит не превышен — работаем
        DoExport(selectedObjects);
    },
    onLimitExceeded: (maxValue) =>
    {
        // Лимит превышен — maxValue содержит максимальное значение
        Ed?.WriteMessage($"\nМаксимум: {maxValue} объектов. Обновитесь до Pro.");
    });
```

### Строгая проверка (с исключением)

```csharp
try
{
    GrossGeoLicense.RequireLimitOrThrow("export", "maxObjects", selectedCount);
    DoExport(selectedObjects);
}
catch (LimitExceededException ex)
{
    Ed?.WriteMessage($"\nПревышен лимит '{ex.LimitCode}': " +
                     $"максимум {ex.Limit}, запрошено {ex.ActualValue}.");
}
```

---

## 10. Фичи по умолчанию (`isDefault`)

> **Решение владельца №14 (18.09.2026).** Прежняя редакция этого раздела обещала фичи,
> которые «работают даже без лицензии», и код `LoadPublicFeaturesAsync`/`IsPublicFeature`,
> которого в SDK не существует — грепом по `src/SDK/GrossGeo.SDK.Stub/` таких методов нет
> ни одного, только упоминание в `CHANGELOG.md` как СНЯТЫХ («no longer needed, use
> `HasFeature` for default features»). Правка ниже меняет не только текст: отклонённый
> вердикт лицензии сегодня не оставляет НИКАКИХ прав, включая фичи, отмеченные `isDefault`
> — исключения для «публичных» фичей не существует.

`isDefault` — признак фичи в `plans-manifest.json` (`FeatureV1.IsDefault`), а не отдельный
режим доступа. Он значит «эта фича входит во ВСЕ планы продукта, включая бесплатный»,
и ничего не говорит про то, нужна ли лицензия — она нужна ровно как для любой другой фичи.
Проверяется тем же `HasFeature`, и он безусловно требует действующего вердикта
(`GrossGeoLicense.cs`, `HasFeature`: `if (_lastResult?.IsValid != true) return false;` —
до этой проверки список фичей даже не смотрится). Отдельного `IsPublicFeature`/
`LoadPublicFeaturesAsync`, которые работали бы раньше инициализации или без лицензии
вовсе, в SDK нет и не задумано.

Единственный случай, где функциональность доступна без ПОКУПКИ, — весь продукт целиком
бесплатный (`billingModel: Free` в манифесте). Тогда лицензия всё равно нужна и всё равно
проверяется, но её вердикт для бесплатного продукта операционный по построению (`Р-182`) —
это свойство продукта, а не отдельного флага у фичи.

```csharp
public void Initialize()
{
    _ = Task.Run(async () =>
    {
        await GrossGeoLicense.Initialize(options);
    });
}

[CommandMethod("VIEWER")]
public void ViewerCommand()
{
    // LGC-720: дождитесь инициализации — она идёт в фоне, и команду можно запустить
    // раньше, чем появится вердикт. Без ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
    var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
    if (ready.Status == LicenseCheckStatus.Unknown) { Ed?.WriteMessage("\n" + ready.Message); return; }
    // HasFeature проверяет ЛЮБУЮ фичу против текущего вердикта — isDefault здесь ничего не меняет.
    // Отклонённый вердикт вернёт false и для этой фичи, даже если она isDefault.
    if (GrossGeoLicense.HasFeature("basic-viewer"))
    {
        ShowViewer();
    }
}

[CommandMethod("EDITOR")]
public void EditorCommand()
{
    // Фича не из isDefault — тот же HasFeature, разница только в наборе планов, где она включена
    FeatureGuard.Require("advanced-editor",
        () => ShowEditor(),
        () => Ed?.WriteMessage("\nРедактор доступен в плане Pro."));
}
```

---

## 11. Concurrent Sessions — плавающие лицензии

Concurrent (плавающие) лицензии позволяют организации купить, например, 10 лицензий для 50 сотрудников. Одновременно работать могут максимум 10 человек.

Этот режим использует **сессии**: при запуске плагин «занимает» слот, при завершении — освобождает.

### Получение сессии

```csharp
public void Initialize()
{
    // Главный поток — запомнить ЗДЕСЬ, до Task.Run (RunOnMainThread — раздел 5, шаг 3).
    _ui = new System.Windows.Forms.Control();
    _ = _ui.Handle;

    _ = Task.Run(async () =>
    {
        var result = await GrossGeoLicense.Initialize(options);

        // Проверяем, нужен ли concurrent-режим
        if (result.IsValid &&
            result.LicenseMode == GrossGeo.Contracts.Licensing.LicenseMode.Concurrent)
        {
            // Получаем слот
            var session = await GrossGeoLicense.AcquireSessionAsync(
                clientInfo: Environment.MachineName); // передаём имя машины

            // Мы в Task.Run — не на главном потоке AutoCAD: вывод через RunOnMainThread (ниже).
            if (session.IsSuccess)
            {
                RunOnMainThread(() => Ed?.WriteMessage("\n[MyPlugin] Сессия получена!"));
            }
            else
            {
                RunOnMainThread(() =>
                {
                    Ed?.WriteMessage($"\n[MyPlugin] Нет свободных слотов: {session.ErrorMessage}");
                    Ed?.WriteMessage("\n[MyPlugin] Попросите коллегу закрыть AutoCAD.");
                });
            }
        }
    });

    // Обработка потери сессии (таймаут, сессия отнята администратором).
    // Событие приходит с ФОНОВОГО потока (таймер heartbeat), а API AutoCAD — только с главного:
    // вывод уходит через Control главного потока, созданный в Initialize.
    GrossGeoLicense.SessionExpired += (sender, e) => RunOnMainThread(() =>
    {
        Ed?.WriteMessage($"\n[MyPlugin] ⚠ Сессия потеряна: {e.Message}");
        Ed?.WriteMessage("\n[MyPlugin] Защищённые команды недоступны.");
    });
}

// Главный поток AutoCAD — Control, созданный в Initialize (см. RunOnMainThread).
private static System.Windows.Forms.Control? _ui;

/// <summary>
/// Выполнить действие на главном потоке AutoCAD через Control, созданный в Initialize.
/// Нужна везде, где код идёт с фонового потока: после await (в Task.Run и в async-командах)
/// и в обработчиках событий SDK (SessionExpired, LicenseRefreshed). InvokeRequired и BeginInvoke
/// безопасны с любого потока; на главном потоке действие выполняется сразу. Исключение действия
/// ловится здесь: на главном потоке оно уронило бы AutoCAD.
/// </summary>
private static void RunOnMainThread(Action action)
{
    var ui = _ui;
    if (ui == null)
    {
        System.Diagnostics.Debug.WriteLine("[MyPlugin] главный поток не запомнен в Initialize — вывод пропущен");
        return;
    }

    if (!ui.InvokeRequired)
    {
        RunSafely(action);
        return;
    }

    try
    {
        ui.BeginInvoke(new Action(() => RunSafely(action)));
    }
    catch (InvalidOperationException ex)
    {
        // Control уже уничтожен — AutoCAD закрывается.
        System.Diagnostics.Debug.WriteLine($"[MyPlugin] вывод не доставлен: {ex.Message}");
    }
}

private static void RunSafely(Action action)
{
    try
    {
        action();
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[MyPlugin] {ex}");
    }
}
```

### Проверка сессии

```csharp
// Свойства для проверки состояния concurrent сессии
GrossGeoLicense.HasActiveSession      // bool — есть ли активная сессия
GrossGeoLicense.SessionToken          // string? — токен текущей сессии
GrossGeoLicense.SessionExpiresAt      // DateTime? — когда истечёт
```

### Освобождение сессии

```csharp
public void Terminate()
{
    // Shutdown сам освобождает слот Concurrent-сессии и ждёт панель не дольше 2 с.
    // Не зовите здесь ReleaseSessionAsync().Wait(): Terminate идёт на главном потоке AutoCAD,
    // и ожидание держало бы его выход до 20 с, а на .NET Framework при зависшей панели — без конца.
    GrossGeoLicense.Shutdown();
}
```

> **Важно:** Heartbeat (пульс) сессии отправляется автоматически. Если ваш плагин зависнет или AutoCAD аварийно завершится, сессия освободится по таймауту.

---

## 12. Проверка обновлений

SDK может проверить, есть ли новая версия вашего плагина:

```csharp
[CommandMethod("CHECK_UPDATES")]
public async void CheckUpdates()
{
    // После await продолжение может прийти не на главный поток — весь вывод через
    // RunOnMainThread (раздел 5, шаг 3), где бы ни пришло продолжение.
    try
    {
        var update = await GrossGeoLicense.CheckForUpdatesAsync();

        RunOnMainThread(() =>
        {
            if (update.HasUpdate)
            {
                Ed?.WriteMessage($"\nДоступна новая версия: {update.AvailableVersion}");
                Ed?.WriteMessage($"\nОпубликована: {update.PublishedAt:dd.MM.yyyy}");

                if (!string.IsNullOrEmpty(update.Changelog))
                    Ed?.WriteMessage($"\nИзменения: {update.Changelog}");

                Ed?.WriteMessage("\nОбновите плагин через GrossGeo User Panel.");
            }
            else
            {
                Ed?.WriteMessage("\nУ вас последняя версия.");
            }
        });
    }
    catch (Exception ex)
    {
        RunOnMainThread(() => Ed?.WriteMessage($"\nОшибка проверки обновлений: {ex.Message}"));
    }
}
```

**Автоматическая проверка при старте:**

Если `CheckForUpdatesOnInit = true` (по умолчанию), SDK проверит обновления при `Initialize()`. Результат можно получить через `GrossGeoLicense.CheckForUpdatesAsync()`.

**Perpetual + Maintenance:**

Для продуктов с моделью `BillingModel.Perpetual` обновления зависят от подписки Maintenance. Если Maintenance истёк, новые версии не будут доступны (version lock).

---

## 13. Бизнес-модели — полные примеры

> Во всех примерах ниже вывод в `Editor` после `await` внутри `Task.Run` идёт через
> `RunOnMainThread`: этот поток не главный, а AutoCAD API вызывается только с главного.
> Сам метод (Control, созданный в `Initialize`, и `BeginInvoke`) — в разделе 5, шаг 3; добавьте
> в класс плагина и его, и поле `_ui` (захват `_ui` в начале `Initialize` в примерах уже есть).

### 13.1 Free — полностью бесплатный продукт

Продукт без лицензирования. Все функции доступны всем.

**Когда использовать:** open-source инструменты, утилиты, демо-продукты.

```csharp
public class FreePlugin : IExtensionApplication
{
    private const string ProductKey = "GG-FREE-PROD-0001";

    public void Initialize()
    {
        // Инициализация для аналитики и обновлений
        _ = Task.Run(async () =>
        {
            await GrossGeoLicense.Initialize(new LicenseOptions
            {
                ProductKey = ProductKey,
                PluginVersion = "1.0.0"
            });
            // PlanTier = Free, BillingModel = Free
        });
    }

    public void Terminate() => GrossGeoLicense.Shutdown();

    // Все команды работают без проверок
    [CommandMethod("FREE_TOOL")]
    public void FreeTool()
    {
        Ed?.WriteMessage("\nИнструмент работает для всех!");
        DoWork();
    }
}
```

### 13.2 Freemium — базовый бесплатно + PRO по подписке

Часть функций бесплатна, расширенные — по подписке. Самая популярная модель.

**Когда использовать:** когда хотите дать попробовать продукт перед покупкой.

```csharp
public class FreemiumPlugin : IExtensionApplication
{
    private const string ProductKey = "GG-FRMI-PROD-0001";

    public void Initialize()
    {
        // Главный поток — запомнить ЗДЕСЬ, до Task.Run (RunOnMainThread — раздел 5, шаг 3).
        _ui = new System.Windows.Forms.Control();
        _ = _ui.Handle;

        _ = Task.Run(async () =>
        {
            var result = await GrossGeoLicense.Initialize(new LicenseOptions
            {
                ProductKey = ProductKey,
                PluginVersion = "1.0.0"
            });

            // Free: PlanTier = Free, BillingModel = Free
            // Pro:  PlanTier = Pro,  BillingModel = Subscription
            RunOnMainThread(() => Ed?.WriteMessage($"\n[Plugin] План: {result.PlanTier}"));
        });
    }

    public void Terminate() => GrossGeoLicense.Shutdown();

    // ═══ БЕСПЛАТНЫЕ КОМАНДЫ ═══

    [CommandMethod("BASIC_TOOL")]
    public void BasicTool()
    {
        // Используем feature flag для проверки
        FeatureGuard.Require("basic-tools",
            () =>
            {
                Ed?.WriteMessage("\nБазовый инструмент — бесплатно!");
                DoBasicWork();
            },
            () => Ed?.WriteMessage("\nОшибка: базовые инструменты недоступны."));
    }

    [CommandMethod("SIMPLE_EXPORT")]
    public void SimpleExport()
    {
    // LGC-720: дождитесь инициализации — она идёт в фоне, и команду можно запустить
    // раньше, чем появится вердикт. Без ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
    var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
    if (ready.Status == LicenseCheckStatus.Unknown) { Ed?.WriteMessage("\n" + ready.Message); return; }
        FeatureGuard.Require("simple-export", () =>
        {
            int objectCount = GetSelectedCount();

            // Feature Limit: Free = max 10, Pro = безлимитно
            if (!GrossGeoLicense.CheckLimit("simple-export", "maxObjects", objectCount))
            {
                var limit = GrossGeoLicense.GetFeatureLimit("simple-export", "maxObjects");
                Ed?.WriteMessage($"\nВы выбрали {objectCount}, лимит: {limit}.");
                Ed?.WriteMessage("\nОбновитесь до Pro для снятия ограничения.");
                return;
            }

            DoExport();
            Ed?.WriteMessage("\nЭкспорт завершён!");
        });
    }

    // ═══ PRO КОМАНДЫ ═══

    [CommandMethod("ADVANCED_TOOL")]
    public void AdvancedTool()
    {
        FeatureGuard.Require("advanced-tools",
            () =>
            {
                Ed?.WriteMessage("\nПродвинутый инструмент — PRO!");
                DoAdvancedWork();
            },
            () =>
            {
                Ed?.WriteMessage("\nПродвинутые инструменты доступны в плане Pro.");
                Ed?.WriteMessage("\nОткройте User Panel → Каталог → Подписка.");
            });
    }

    [CommandMethod("BATCH_PROCESS")]
    public void BatchProcess()
    {
        FeatureGuard.Require("batch",
            () =>
            {
                Ed?.WriteMessage("\nПакетная обработка — PRO!");
                DoBatchProcessing();
            },
            () => Ed?.WriteMessage("\nПакетная обработка доступна в плане Pro."));
    }
}
```

### 13.3 Subscription — подписка

Весь функционал доступен только по подписке (месячной или годовой).

**Когда использовать:** SaaS-модель с регулярным доходом.

```csharp
public class SubscriptionPlugin : IExtensionApplication
{
    private const string ProductKey = "GG-SUBS-PROD-0001";

    public void Initialize()
    {
        // Главный поток — запомнить ЗДЕСЬ, до Task.Run (RunOnMainThread — раздел 5, шаг 3).
        _ui = new System.Windows.Forms.Control();
        _ = _ui.Handle;

        _ = Task.Run(async () =>
        {
            var result = await GrossGeoLicense.Initialize(new LicenseOptions
            {
                ProductKey = ProductKey,
                PluginVersion = "2.1.0",
                GracePeriodDays = 7 // 7 дней offline-работы
            });

            // PlanTier = Pro, BillingModel = Subscription
            RunOnMainThread(() =>
            {
                if (result.IsValid)
                {
                    Ed?.WriteMessage("\n[Plugin] Подписка активна!");
                    Ed?.WriteMessage($"\n[Plugin] Истекает: " +
                        $"{result.ExpiresAt:dd.MM.yyyy} ({result.DaysRemaining} дней)");
                }
                else
                {
                    Ed?.WriteMessage("\n[Plugin] Подписка не найдена или истекла.");
                    Ed?.WriteMessage("\n[Plugin] Оформите подписку в GrossGeo User Panel.");
                }
            });
        });
    }

    public void Terminate() => GrossGeoLicense.Shutdown();

    [CommandMethod("SUB_COMMAND")]
    public void SubscriptionCommand()
    {
    // LGC-720: дождитесь инициализации — она идёт в фоне, и команду можно запустить
    // раньше, чем появится вердикт. Без ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
    var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
    if (ready.Status == LicenseCheckStatus.Unknown) { Ed?.WriteMessage("\n" + ready.Message); return; }
        GrossGeoLicense.Protect(
            () =>
            {
                DoWork();
                Ed?.WriteMessage("\nКоманда выполнена!");
            },
            () =>
            {
                // Детальная диагностика для пользователя
                var result = GrossGeoLicense.LastResult;
                switch (result?.Status)
                {
                    case LicenseCheckStatus.Expired:
                        Ed?.WriteMessage("\nПодписка истекла. Продлите в User Panel.");
                        break;
                    case LicenseCheckStatus.InGracePeriod:
                        Ed?.WriteMessage(
                            $"\nGrace period: осталось {result.DaysRemaining} дней. " +
                            "Восстановите интернет для продления.");
                        break;
                    default:
                        Ed?.WriteMessage("\nТребуется подписка.");
                        break;
                }
            });
    }
}
```

### 13.4 Perpetual — бессрочная лицензия (разовая покупка)

Пользователь покупает лицензию один раз. Обновления — по отдельной подписке Maintenance.

**Когда использовать:** традиционная модель продажи ПО.

```csharp
public class PerpetualPlugin : IExtensionApplication
{
    private const string ProductKey = "GG-PAID-PROD-0001";

    public void Initialize()
    {
        // Главный поток — запомнить ЗДЕСЬ, до Task.Run (RunOnMainThread — раздел 5, шаг 3).
        _ui = new System.Windows.Forms.Control();
        _ = _ui.Handle;

        _ = Task.Run(async () =>
        {
            var result = await GrossGeoLicense.Initialize(new LicenseOptions
            {
                ProductKey = ProductKey,
                PluginVersion = "3.0.0",
                CheckForUpdatesOnInit = true // проверить Maintenance + обновления
            });

            // PlanTier = Pro, BillingModel = Perpetual
            if (result.IsValid)
            {
                RunOnMainThread(() =>
                {
                    Ed?.WriteMessage("\n[Plugin] Лицензия активна (бессрочная)!");
                    Ed?.WriteMessage($"\n[Plugin] План: {result.PlanTier}");
                });

                // Проверить доступность обновлений
                var upd = await GrossGeoLicense.CheckForUpdatesAsync();
                if (upd.HasUpdate)
                    RunOnMainThread(() => Ed?.WriteMessage(
                        $"\n[Plugin] Доступна v{upd.AvailableVersion} " +
                        "(требуется Maintenance)."));
            }
        });
    }

    public void Terminate() => GrossGeoLicense.Shutdown();

    [CommandMethod("PERP_TOOL")]
    public void PerpetualTool()
    {
    // LGC-720: дождитесь инициализации — она идёт в фоне, и команду можно запустить
    // раньше, чем появится вердикт. Без ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
    var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
    if (ready.Status == LicenseCheckStatus.Unknown) { Ed?.WriteMessage("\n" + ready.Message); return; }
        GrossGeoLicense.Protect(
            () => DoWork(),
            () => Ed?.WriteMessage("\nТребуется покупка лицензии."));
    }

    [CommandMethod("PREMIUM_TOOL")]
    public void PremiumTool()
    {
        // ProPlus имеет дополнительные фичи
        FeatureGuard.Require("premium",
            () => DoPremiumWork(),
            () => Ed?.WriteMessage("\nДоступно в Pro+ плане."));
    }
}
```

### 13.5 Concurrent — плавающие лицензии

Организация покупает пул лицензий. Одновременно работает ограниченное число пользователей.

**Когда использовать:** корпоративные клиенты с большим числом сотрудников.

```csharp
public class ConcurrentPlugin : IExtensionApplication
{
    private const string ProductKey = "GG-CONC-PROD-0001";

    public void Initialize()
    {
        // Главный поток — запомнить ЗДЕСЬ, до Task.Run (RunOnMainThread — раздел 5, шаг 3).
        _ui = new System.Windows.Forms.Control();
        _ = _ui.Handle;

        _ = Task.Run(async () =>
        {
            var result = await GrossGeoLicense.Initialize(new LicenseOptions
            {
                ProductKey = ProductKey,
                PluginVersion = "1.0.0"
            });

            // Для Concurrent — нужно получить сессию
            if (result.IsValid &&
                result.LicenseMode == GrossGeo.Contracts.Licensing.LicenseMode.Concurrent)
            {
                var session = await GrossGeoLicense.AcquireSessionAsync(
                    Environment.MachineName);

                if (session.IsSuccess)
                {
                    RunOnMainThread(() => Ed?.WriteMessage("\n[Plugin] Сессия получена."));
                }
                else
                {
                    RunOnMainThread(() => Ed?.WriteMessage($"\n[Plugin] Нет свободных слотов: " +
                        $"{session.ErrorMessage}"));
                }
            }
        });

        // Реагируем на потерю сессии: событие приходит с фонового потока, в Editor пишем
        // через главный поток (RunOnMainThread — Control, созданный в Initialize,
        // см. раздел 5, шаг 3).
        GrossGeoLicense.SessionExpired += (s, e) => RunOnMainThread(() =>
        {
            Ed?.WriteMessage($"\n[Plugin] ⚠ Сессия потеряна: {e.Message}");
        });
    }

    public void Terminate()
    {
        // Shutdown сам освобождает слот и ждёт панель не дольше 2 с (раздел 11, «Освобождение сессии»).
        GrossGeoLicense.Shutdown();
    }

    [CommandMethod("CONC_TOOL")]
    public void ConcurrentTool()
    {
        // Проверяем и лицензию, и наличие сессии
        if (!GrossGeoLicense.IsValid || !GrossGeoLicense.HasActiveSession)
        {
            Ed?.WriteMessage("\nТребуется активная сессия. Попробуйте перезапустить.");
            return;
        }

        DoWork();
    }
}
```

---

## 14. Структура проекта и упаковка bundle

### Структура папок

```
MyPlugin/
├── MyPlugin.csproj                       # Файл проекта
├── Plugin.cs                             # Главный класс (IExtensionApplication)
├── Commands/                             # Команды AutoCAD (если много)
│   ├── ExportCommand.cs
│   └── AnalysisCommand.cs
├── product-manifest.json                 # Манифест для платформы GrossGeo
└── MyPlugin.bundle/                      # AutoCAD Bundle
    ├── PackageContents.xml               # Манифест bundle (читает AutoCAD)
    └── Contents/                         # DLL-файлы
        ├── MyPlugin.dll                  # Ваш плагин
        ├── GrossGeo.SDK.Stub.dll         # SDK
        └── GrossGeo.Contracts.dll        # Контракты
```

### PackageContents.xml

Это файл, который AutoCAD читает для загрузки вашего плагина.

> **Если распространение — через каталог GrossGeo** (стандартный путь), `PackageContents.xml`
> **генерирует User Panel при установке** — писать его руками не нужно, это нужно только для
> локальной разработки/отладки вне каталога (`netload`, ручной bundle). Создайте его в папке
> `MyPlugin.bundle/`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage
    SchemaVersion="1.0"
    AppVersion="1.0.0"
    Name="MyPlugin"
    Description="Описание вашего плагина"
    Author="Название вашей студии">

  <CompanyDetails
      Name="Название вашей студии"
      Url="https://your-website.com"/>

  <Components Description="MyPlugin Components">
    <!--
      Для КАЖДОЙ платформы (AutoCAD, Civil3D) создавайте ОТДЕЛЬНЫЙ ComponentEntry.
      Не используйте звёздочку (*) в Platform.
    -->

    <!-- AutoCAD -->
    <ComponentEntry
        AppName="MyPlugin"
        ModuleName="./Contents/MyPlugin.dll"
        AppDescription="MyPlugin for AutoCAD"
        AppType=".Net"
        LoadOnAutoCADStartup="True">
      <!--
        SeriesMin/SeriesMax — диапазон поддерживаемых версий AutoCAD.
        Таблица версий — ниже.
      -->
      <RuntimeRequirements
          OS="Win64"
          Platform="AutoCAD"
          SeriesMin="R24.0"
          SeriesMax="R25.1"/>
    </ComponentEntry>

    <!-- Civil 3D -->
    <ComponentEntry
        AppName="MyPlugin"
        ModuleName="./Contents/MyPlugin.dll"
        AppDescription="MyPlugin for Civil3D"
        AppType=".Net"
        LoadOnAutoCADStartup="True">
      <RuntimeRequirements
          OS="Win64"
          Platform="Civil3D"
          SeriesMin="R24.0"
          SeriesMax="R25.1"/>
    </ComponentEntry>
  </Components>
</ApplicationPackage>
```

### Таблица версий AutoCAD

| AutoCAD | Civil 3D | Series | .NET |
|---------|----------|--------|------|
| 2019 | 2019 | R23.0 | Framework 4.7 |
| 2020 | 2020 | R23.1 | Framework 4.7 |
| 2021 | 2021 | R24.0 | Framework 4.8 |
| 2022 | 2022 | R24.1 | Framework 4.8 |
| 2023 | 2023 | R24.2 | Framework 4.8 |
| 2024 | 2024 | R24.3 | Framework 4.8 |
| 2025 | 2025 | R25.0 | .NET 8.0 |
| 2026 | 2026 | R25.1 | .NET 8.0 |
| 2027 | 2027 | R26.0 | .NET 10.0 |

**AutoCAD 2027 (R26.0) работает на .NET 10** — сборки AutoCAD 2027 помечены `.NETCoreApp v10.0`,
пакет `AutoCAD.NET 26.0.0` собран только под `net10.0`. Родная цель для него — `net10.0-windows`.

**Для AutoCAD 2027 требуется сборка под `net10.0-windows`; сборка под `net8` не поддерживается.**
Замер 23.09.2026 показал, что продукт под `net8.0-windows` с этим SDK физически ЗАГРУЖАЕТСЯ и в
AutoCAD 2027, и в Civil 3D 2027 — более ранняя редакция этого раздела читала «загружается» как
«значит можно объявлять 2027 после проверки» и была неверна: по данным Autodesk сборка под `net8`
не совместима с AutoCAD 2027 — нужна пересборка под `net10.0-windows`. Дело не в хосте .NET 10
как таковом: на AutoCAD 2026.1.2 и 2025 U1.4, тоже на хосте .NET 10, `net8`-сборки грузятся
штатно, несовместимость — именно с AutoCAD 2027. Объявляйте 2027 только сборкой под
`net10.0-windows`: выпуск, где у `net8` серии выше `R25.x`, сервер отвергает, а портал таких серий
для `net8` не предлагает.

**Загрузка в AutoCAD 2027 — замер 23.09.2026** (AutoCAD 2027, `SECURELOAD=1` — значение AutoCAD по
умолчанию): неподписанная сборка продукта из каталога, которого нет в доверенных путях
(`TRUSTEDPATHS`), вызывает окно безопасности AutoCAD «исполняемый файл без подписи… не в доверенной
папке» и без подтверждения пользователя не загружается. Подписанную сборку этот замер не проверял.

Если в бандле две сборки — под `net8` и под `net10` — объявите каждой свою полосу, без пересечения:
```xml
<ComponentEntry AppName="MyPlugin" ModuleName="./Contents/net8/MyPlugin.dll" AppType=".Net" LoadOnAutoCADStartup="True">
  <RuntimeRequirements OS="Win64" Platform="AutoCAD" SeriesMin="R25.0" SeriesMax="R25.1"/>
</ComponentEntry>
<ComponentEntry AppName="MyPlugin" ModuleName="./Contents/net10/MyPlugin.dll" AppType=".Net" LoadOnAutoCADStartup="True">
  <RuntimeRequirements OS="Win64" Platform="AutoCAD" SeriesMin="R26.0" SeriesMax="R26.0"/>
</ComponentEntry>
```

**Пример:** Если ваш плагин поддерживает AutoCAD 2021–2025:
```xml
<RuntimeRequirements OS="Win64" Platform="AutoCAD"
    SeriesMin="R24.0" SeriesMax="R25.1"/>
```

### Загрузка по команде (опционально)

Если плагин должен загружаться не при старте, а по команде:

```xml
<ComponentEntry
    AppName="MyPlugin"
    ModuleName="./Contents/MyPlugin.dll"
    AppDescription="MyPlugin for AutoCAD"
    AppType=".Net"
    LoadOnCommandInvocation="True">
  <Commands>
    <Command Local="MYPLUGIN_RUN" Global="MYPLUGIN_RUN"/>
  </Commands>
  <RuntimeRequirements OS="Win64" Platform="AutoCAD"
      SeriesMin="R24.0" SeriesMax="R25.1"/>
</ComponentEntry>
```

### Сборка bundle для загрузки

Для загрузки на платформу упакуйте bundle в ZIP:

```
MyPlugin.v1.0.0.bundle.zip
└── MyPlugin.bundle/
    ├── PackageContents.xml
    └── Contents/
        ├── MyPlugin.dll
        ├── GrossGeo.SDK.Stub.dll
        └── GrossGeo.Contracts.dll
```

> **Важно:** ZIP должен содержать папку `.bundle/` в корне. Не кладите файлы напрямую в ZIP.

---

## 15. Манифест продукта (product-manifest.json)

Манифест описывает ваш продукт, планы, фичи и лимиты. Он используется при регистрации продукта на платформе.

### Минимальный пример (Free)

```json
{
  "product": {
    "name": "My Free Tool",
    "slug": "my-free-tool",
    "shortDescription": "Краткое описание (до 150 символов)",
    "fullDescription": "Полное описание. Поддерживает Markdown.",
    "licensingMode": "GrossGeo",
    "trialDays": 0,
    "tags": ["utilities", "free"]
  },
  "plans": [
    {
      "code": "free",
      "name": "Free",
      "displayName": "Бесплатный",
      "tier": "Free",
      "billingModel": "Free",
      "licenseMode": "Machine",
      "monthlyPrice": 0,
      "isActive": true
    }
  ],
  "features": [
    {
      "code": "all-tools",
      "name": "Все инструменты",
      "description": "Полный набор инструментов",
      "isDefault": true
    }
  ],
  "planFeatures": {
    "free": ["all-tools"]
  },
  "featureLimits": {}
}
```

### Полный пример (Freemium с лимитами)

```json
{
  "product": {
    "name": "GeoExporter Pro",
    "slug": "geoexporter-pro",
    "shortDescription": "Экспорт геоданных из AutoCAD в GIS-форматы",
    "fullDescription": "Экспортирует объекты AutoCAD в GeoJSON, SHP, KML. Free план — до 10 объектов. Pro — без ограничений + пакетный экспорт.",
    "licensingMode": "GrossGeo",
    "trialDays": 14,
    "tags": ["export", "gis", "geojson"]
  },

  "plans": [
    {
      "code": "free",
      "name": "Free",
      "displayName": "Бесплатный",
      "tier": "Free",
      "billingModel": "Free",
      "licenseMode": "Machine",
      "monthlyPrice": 0,
      "isActive": true
    },
    {
      "code": "pro",
      "name": "Pro",
      "displayName": "Профессионал",
      "tier": "Pro",
      "billingModel": "Subscription",
      "billingPeriod": "Monthly",
      "licenseMode": "Machine",
      "monthlyPrice": 990,
      "yearlyPrice": 9900,
      "currency": "RUB",
      "maxSeats": 1,
      "trialDays": 14,
      "isActive": true
    }
  ],

  "features": [
    {
      "code": "basic-export",
      "name": "Базовый экспорт",
      "description": "Экспорт в GeoJSON",
      "isDefault": true
    },
    {
      "code": "advanced-export",
      "name": "Расширенный экспорт",
      "description": "Экспорт в SHP, KML, GPX с настройками",
      "isDefault": false
    },
    {
      "code": "batch-export",
      "name": "Пакетный экспорт",
      "description": "Экспорт нескольких файлов за раз",
      "isDefault": false
    },
    {
      "code": "preview",
      "name": "Предпросмотр",
      "description": "Просмотр данных перед экспортом",
      "isDefault": true
    }
  ],

  "planFeatures": {
    "free": ["basic-export", "preview"],
    "pro": ["basic-export", "advanced-export", "batch-export", "preview"]
  },

  "featureLimits": {
    "free.basic-export": [
      {
        "limitCode": "maxObjects",
        "limitType": "MaxPerCall",
        "limitValue": 10
      },
      {
        "limitCode": "maxFileSize",
        "limitType": "MaxSize",
        "limitValue": 10485760
      }
    ],
    "pro.basic-export": null,
    "pro.batch-export": null
  },

  "releases": [
    {
      "version": "1.0.0",
      "channel": "Stable",
      "changelog": "Первый релиз. Free и Pro планы.",
      "distributionType": "Bundle",
      "postInstallAction": "RequireRestart",
      "netloadDllPath": "Contents/GeoExporter.dll",
      "minAutoCADVersion": "R24.0",
      "maxAutoCADVersion": "R25.1",
      "targetPlatforms": ["AutoCAD", "Civil3D"],
      "supportedOS": ["Win64"],
      "loadOnStartup": true
    }
  ]
}
```

### Описание полей

#### Секция `product`

| Поле | Тип | Описание |
|------|-----|----------|
| `name` | string | Название продукта (отображается в каталоге) |
| `slug` | string | URL-имя (латиница, дефисы, строчные) |
| `shortDescription` | string | Краткое описание (до 150 символов) |
| `fullDescription` | string | Полное описание (Markdown) |
| `licensingMode` | string | `"GrossGeo"` — лицензирование через платформу |
| `trialDays` | int | Длительность пробного периода (0 — нет trial) |
| `tags` | string[] | Теги для поиска в каталоге |

#### Секция `plans[]`

| Поле | Тип | Описание |
|------|-----|----------|
| `code` | string | Уникальный код плана (для API) |
| `tier` | string | Уровень: `Free`, `Pro`, `ProPlus`, `Enterprise` |
| `billingModel` | string | Модель: `Free`, `Subscription`, `Perpetual`, `Contract` |
| `billingPeriod` | string? | `Monthly`, `Annual`, `OneTime`, `null` |
| `licenseMode` | string | Привязка: `User`, `Machine`, `Concurrent` |
| `monthlyPrice` | decimal? | Цена в месяц |
| `yearlyPrice` | decimal? | Цена в год |
| `oneTimePrice` | decimal? | Разовая цена (Perpetual) |
| `currency` | string | Валюта (`RUB`) |
| `maxSeats` | int | Макс. количество пользователей/машин |
| `maxConcurrentSessions` | int? | Макс. одновременных сессий (для Concurrent) |
| `trialDays` | int | Пробный период для этого плана |
| `trialBindingMode` | string? | `ByUser` или `ByMachine` |

#### Секция `features[]`

| Поле | Тип | Описание |
|------|-----|----------|
| `code` | string | Код фичи (используется в SDK: `HasFeature("code")`) |
| `name` | string | Название для отображения |
| `description` | string | Описание |
| `isDefault` | bool | Включена во все планы продукта, включая бесплатный — не отменяет проверку лицензии (решение владельца №14) |

#### Секция `planFeatures`

Связывает планы и фичи:

```json
{
  "free": ["basic-export", "preview"],
  "pro": ["basic-export", "advanced-export", "batch-export", "preview"]
}
```

#### Секция `featureLimits`

Ключ — `"планКод.фичаКод"`, значение — массив лимитов или `null` (безлимитно):

```json
{
  "free.basic-export": [
    { "limitCode": "maxObjects", "limitType": "MaxPerCall", "limitValue": 10 }
  ],
  "pro.basic-export": null
}
```

Типы лимитов (`limitType`):
- `MaxPerCall` — максимальное количество за один вызов
- `MaxSize` — максимальный размер (в байтах)
- `MaxDuration` — максимальная длительность (в секундах)
- `Boolean` — доступность (0 = нет, 1 = да)

---

## 16. Процесс публикации на платформе

### Шаг 1. Регистрация студии разработчика

1. Зайдите в **GrossGeo Developer Portal**.
2. Создайте **студию** (DeveloperStudio) — организацию-разработчика.
3. Заполните **KYC-профиль**: юридические данные, ИНН, контакты.
4. Дождитесь одобрения KYC (статус: `KycPending` → `KycApproved`).

> KYC (Know Your Customer) необходим для получения выплат и публикации платных продуктов. Бесплатные продукты можно публиковать без полного KYC.

### Шаг 2. Создание продукта

1. В Developer Portal: **Продукты → Создать продукт**.
2. Заполните:
   - Название, описание, иконка.
   - Категория и теги.
   - Режим лицензирования: `GrossGeo` (через платформу).
3. Настройте **ценовые планы** (Free, Pro, ProPlus и т.д.).
4. Настройте **фичи** и привяжите их к планам.
5. Настройте **лимиты** для фичей (если нужны).
6. Получите **ProductKey** (формат `GG-XXXX-XXXX-XXXX-XXXX`) — используйте его в SDK.

### Шаг 3. Подготовка релиза

1. Соберите плагин (bundle с `PackageContents.xml`).
2. Упакуйте в `.bundle.zip`.
3. В Developer Portal: **Релизы → Создать релиз**.
4. Заполните:
   - Версия (semver: `1.0.0`, `1.1.0`, `2.0.0`).
   - Канал: `Stable` (основной), `Beta` (бета-тестирование) или `Internal` (внутренний).
   - Changelog — описание изменений.
   - Совместимость: версии AutoCAD, ОС.
5. Загрузите `.bundle.zip`.

### Шаг 4. Модерация

1. Нажмите **Submit** (отправить на модерацию).
2. Статус: `Draft` → `PendingReview`.
3. Модератор проверяет плагин:
   - Безопасность (нет вредоносного кода).
   - Совместимость (работает в заявленных версиях).
   - Описание (соответствует функционалу).
4. Результат:
   - **Approved** → автоматическая публикация в каталог.
   - **Rejected** → вы получите комментарий с причиной. Исправьте и отправьте заново.

### Шаг 5. После публикации

- Продукт появляется в каталоге User Panel.
- Пользователи могут устанавливать и покупать лицензии.
- Вы видите аналитику (установки, доходы) в Developer Portal.
- Для обновлений — повторите шаги 3–4 с новой версией.

### Жизненный цикл релиза

```
Draft → PendingReview → [Approved → Published]
                      → [Rejected → (исправления) → PendingReview]
```

---

## 17. Обработка ошибок и исключения

### Типы исключений SDK

| Исключение | Когда | Свойства |
|------------|-------|----------|
| `LicenseException` | `ProtectOrThrow()` при невалидной лицензии | `Status` (LicenseCheckStatus) |
| `FeatureNotAvailableException` | `RequireFeatureOrThrow()` при отсутствии фичи | `FeatureCode` |
| `LimitExceededException` | `RequireLimitOrThrow()` при превышении лимита | `FeatureCode`, `LimitCode`, `Limit`, `ActualValue` |

### Статусы лицензии (LicenseCheckStatus)

| Статус | Описание | Что делать |
|--------|----------|------------|
| `Valid` | Лицензия валидна | Работаем |
| `InGracePeriod` | Offline grace period | Предупредить пользователя |
| `NotFound` | Лицензия не найдена | Предложить купить |
| `Expired` | Подписка истекла | Предложить продлить |
| `Blocked` | Заблокирована | Обратиться в поддержку |
| `NetworkError` | Нет связи с User Panel | Запустить User Panel |
| `InvalidProductKey` | Неверный ключ | Проверить ProductKey |
| `MachineNotBound` | Машина не привязана | Активировать в User Panel |
| `MachineLimitExceeded` | Превышен лимит машин | Отвязать старые машины |
| `OfflineMode` | Offline режим | Работает из кэша |
| `Trial` | Пробный период | Показать оставшиеся дни |
| `NoAvailableSeats` | Все concurrent-слоты заняты | Дождаться / увеличить план |
| `SessionTerminated` | Concurrent-сессия завершена | Получить новую |

### Рекомендации по обработке

```csharp
[CommandMethod("ROBUST_CMD")]
public void RobustCommand()
{
    if (!GrossGeoLicense.IsInitialized)
    {
        Ed?.WriteMessage("\nSDK не инициализирован. Перезагрузите AutoCAD.");
        return;
    }

    GrossGeoLicense.Protect(
        () => DoWork(),
        () =>
        {
            var status = GrossGeoLicense.LastResult?.Status;
            var message = status switch
            {
                LicenseCheckStatus.Expired =>
                    "Подписка истекла. Продлите в User Panel.",
                LicenseCheckStatus.NotFound =>
                    "Лицензия не найдена. Оформите в User Panel.",
                LicenseCheckStatus.NetworkError =>
                    "Нет связи с User Panel. Убедитесь, что он запущен.",
                LicenseCheckStatus.NoAvailableSeats =>
                    "Все лицензии заняты. Дождитесь освобождения.",
                _ => "Требуется лицензия. Откройте User Panel."
            };
            Ed?.WriteMessage($"\n{message}");
        });
}
```

---

## 18. Offline-режим и Grace Period

SDK поддерживает работу без подключения к интернету:

1. При первом подключении SDK получает подписанный результат и кэширует его локально.
2. Кэш зашифрован и защищён от подделки (подпись, привязка к машине).
3. Если User Panel недоступен, SDK использует кэш.
4. Кэш действителен в течение **Grace Period** (по умолчанию 7 дней).

**Настройка:**

```csharp
var result = await GrossGeoLicense.Initialize(new LicenseOptions
{
    ProductKey = ProductKey,
    GracePeriodDays = 7 // 0 = без offline-режима, макс. 30
});
```

**Проверка:**

```csharp
if (GrossGeoLicense.IsInGracePeriod)
{
    Ed?.WriteMessage(
        $"\n⚠ Offline-режим. Осталось {GrossGeoLicense.DaysRemaining} дней. " +
        "Подключитесь к интернету.");
}

if (GrossGeoLicense.IsOfflineMode)
{
    Ed?.WriteMessage("\nРабота в offline-режиме.");
}
```

---

## 19. Диагностика и логирование

### Логи SDK

SDK пишет диагностические логи в файл:

```
%LocalAppData%\GrossGeo\SDK.Stub\sdk_diag_YYYYMMDD.log
```

Формат: `[HH:mm:ss.fff] сообщение`

### Пользовательский логгер

Вы можете перенаправить логи SDK в свою систему:

```csharp
public class MyLogger : ILicenseLogger
{
    public void Debug(string message) => System.Diagnostics.Debug.WriteLine($"[SDK] {message}");
    public void Info(string message) => System.Diagnostics.Debug.WriteLine($"[SDK] {message}");
    public void Warning(string message) => System.Diagnostics.Debug.WriteLine($"[SDK] ⚠ {message}");
    public void Error(string message) => System.Diagnostics.Debug.WriteLine($"[SDK] ❌ {message}");
}

// Использование
var result = await GrossGeoLicense.Initialize(new LicenseOptions
{
    ProductKey = ProductKey,
    Logger = new MyLogger()
});
```

---

## 20. API-справочник

### Класс `GrossGeoLicense` (static)

#### Инициализация

| Метод | Описание |
|-------|----------|
| `Initialize(string productKey)` | Асинхронная инициализация |
| `Initialize(LicenseOptions options)` | Асинхронная с параметрами |
| `InitializeSync(string productKey)` | Синхронная инициализация |
| `InitializeSync(LicenseOptions options)` | Синхронная с параметрами |
| `WaitUntilReady(TimeSpan timeout)` | Синхронно дождаться результата первой проверки (не дольше `timeout`) |
| `WhenReadyAsync(TimeSpan timeout, CancellationToken)` | Асинхронно дождаться результата первой проверки |
| `ForProduct(string productKey)` | Получить [`ProductLicenseAccessor`](#класс-productlicenseaccessor-через-forproduct) — обёртку над лицензией конкретного продукта (для плагинов с несколькими продуктами в одном процессе) |
| `Shutdown()` | Освобождение ресурсов |

#### Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `IsInitialized` | `bool` | SDK инициализирован |
| `IsValid` | `bool` | Лицензия валидна |
| `PlanTier` | `PlanTier` | Уровень плана |
| `BillingModel` | `BillingModel` | Модель оплаты |
| `LicenseMode` | `LicenseMode` | Режим привязки |
| `ExpiresAt` | `DateTime?` | Дата истечения |
| `DaysRemaining` | `int?` | Дней до истечения |
| `IsInGracePeriod` | `bool` | Offline grace period |
| `IsOfflineMode` | `bool` | Offline-режим |
| `Features` | `IReadOnlyList<string>` | Доступные фичи |
| `FeatureLimits` | `IReadOnlyDictionary<string, int>?` | Лимиты |
| `SessionToken` | `string?` | Токен concurrent-сессии |
| `SessionExpiresAt` | `DateTime?` | Срок сессии |
| `HasActiveSession` | `bool` | Есть активная сессия |
| `LastResult` | `LicenseResult?` | Последний результат |

#### Защита

| Метод | Описание |
|-------|----------|
| `Protect(Action, Action?)` | Мягкая защита |
| `Protect<T>(Func<T>, Func<T>?)` | Мягкая с возвратом |
| `ProtectOrThrow(Action)` | Строгая (LicenseException) |
| `ProtectOrThrow<T>(Func<T>)` | Строгая с возвратом |

#### Features

| Метод | Описание |
|-------|----------|
| `HasFeature(string)` | Проверить фичу |
| `HasFeatureAsync(string, CancellationToken)` | Асинхронная проверка |
| `RequireFeature(string, Action, Action?)` | Мягкая защита фичи |
| `RequireFeatureOrThrow(string, Action)` | Строгая (FeatureNotAvailableException) |
| `RequireFeatureOrThrow<T>(string, Func<T>)` | Строгая с возвратом |

#### Feature Limits

| Метод | Описание |
|-------|----------|
| `GetFeatureLimit(string, string)` | Получить лимит |
| `CheckLimit(string, string, int)` | Проверить лимит |
| `RequireLimit(string, string, int, Action, Action<int>?)` | Мягкая проверка |
| `RequireLimitOrThrow(string, string, int)` | Строгая (LimitExceededException) |
| `IncrementUsageAsync(string featureCode, string limitCode, ...)` | Увеличить счётчик использования лимита |
| `GetCurrentUsageAsync(string featureCode, string limitCode, ...)` | Прочитать текущее значение счётчика |

#### Concurrent Sessions

| Метод / Событие | Описание |
|------------------|----------|
| `AcquireSessionAsync(string?, CancellationToken)` | Получить сессию |
| `ReleaseSessionAsync(CancellationToken)` | Освободить сессию |
| `ReleaseSessionWithResultAsync(CancellationToken)` | Освободить сессию, вернув результат операции (`SessionReleaseResult`) вместо `void` |
| `SendSessionHeartbeatAsync(CancellationToken)` | Отправить heartbeat |
| `SessionExpired` (event) | Событие потери сессии |

#### Обновления

| Метод / Событие | Описание |
|-------|----------|
| `Check()` | Быстрая проверка (из памяти, без запроса к User Panel) |
| `CheckForUpdatesAsync(CancellationToken)` | Проверить обновления |
| `CheckAsync(CancellationToken)` | Проверить лицензию асинхронно (запрос к User Panel) |
| `RefreshAsync(CancellationToken)` | Обновить данные лицензии |
| `ClearLocalCache()` | Очистить локальный кэш |
| `InvalidateCacheAsync(string? productKey, CancellationToken)` | Сбросить локальный кэш конкретного продукта (или активного, если `productKey` не задан) |
| `CheckAndUpdateCacheVersion(long serverCacheVersion)` | Сверить версию кэша с сервером; `true`, если кэш был очищен из-за расхождения версий |
| `LicenseRefreshed` (event) | Событие обновления данных лицензии (после `RefreshAsync`/`CheckAsync`/фонового опроса) |

#### Навигация в User Panel

| Метод | Описание |
|-------|----------|
| `OpenProductPageAsync(string? section, CancellationToken)` | Открыть страницу продукта в User Panel (опционально — конкретный раздел) |
| `RequestTrialAsync(CancellationToken)` | Открыть в User Panel запуск триала для текущего продукта |

### Класс `ProductLicenseAccessor` (через `ForProduct`)

Возвращается `GrossGeoLicense.ForProduct(productKey)`. Даёт тот же набор операций, что статический
`GrossGeoLicense`, но в области конкретного продукта — нужен плагинам, обслуживающим несколько
продуктов в одном процессе (`GrossGeoLicense.*` без `ForProduct` всегда отвечает за «активный»/последний
инициализированный продукт).

| Категория | Члены |
|-----------|-------|
| Инициализация | `WhenReadyAsync`, `WaitUntilReady` |
| Свойства | `ProductKey`, `LastResult`, `IsValid`, `PlanTier`, `BillingModel`, `LicenseMode`, `FeatureLimits`, `SessionToken`, `SessionExpiresAt`, `HasActiveSession`, `ExpiresAt`, `DaysRemaining`, `IsInGracePeriod`, `IsOfflineMode`, `Features` |
| Ключи фич | `GetFeatureKeyAvailability`, `TryGetFeatureKey`, `GetFeatureKeyAsync` |
| Защита | `HasFeature`, `GetFeatureLimit`, `CheckLimit`, `Protect` (3 перегрузки), `ProtectOrThrow`, `RequireFeature`, `RequireFeatureOrThrow`, `RequireLimit` |
| Проверка/обновление | `Check`, `CheckAsync`, `RefreshAsync` |
| Concurrent Sessions | `AcquireSessionAsync`, `ReleaseSessionAsync`, `ReleaseSessionWithResultAsync`, `SendSessionHeartbeatAsync` |
| Usage-лимиты | `IncrementUsageAsync`, `GetCurrentUsageAsync` |

Сигнатуры совпадают с одноимёнными статическими членами `GrossGeoLicense` выше — здесь не дублируются
таблицами, чтобы не разойтись при правке одной из копий.

### Guard-классы

| Класс | Метод | Аналог |
|-------|-------|--------|
| `LicenseGuard` | `.IsValid` | `GrossGeoLicense.IsValid` |
| | `.Protect(action, onBlocked)` | `GrossGeoLicense.Protect(...)` |
| | `.OrThrow(action)` | `GrossGeoLicense.ProtectOrThrow(...)` |
| `FeatureGuard` | `.Has(code)` | `GrossGeoLicense.HasFeature(...)` |
| | `.HasAsync(code, ct)` | `GrossGeoLicense.HasFeatureAsync(...)` |
| | `.Require(code, action, onMissing)` | `GrossGeoLicense.RequireFeature(...)` |
| | `.OrThrow(code, action)` | `GrossGeoLicense.RequireFeatureOrThrow(...)` |

---

## 21. Troubleshooting — частые проблемы

| Проблема | Возможная причина | Решение |
|----------|------------------|---------|
| `LicenseCheckStatus.NetworkError` | User Panel не запущен | Запустите GrossGeo User Panel |
| `LicenseCheckStatus.InvalidProductKey` | Неверный ProductKey | Проверьте ключ в Developer Portal |
| `LicenseCheckStatus.MachineNotBound` | Машина не привязана к лицензии | Активируйте лицензию в User Panel |
| `LicenseCheckStatus.Expired` | Подписка или trial истекли | Продлите подписку в User Panel |
| `LicenseCheckStatus.NoAvailableSeats` | Все concurrent-слоты заняты | Дождитесь освобождения или увеличьте план |
| `LicenseCheckStatus.ProtocolVersionMismatch` | Старая версия User Panel | Обновите User Panel до 2.0+ |
| `IsInGracePeriod = true` | Нет подключения к интернету | Восстановите интернет в течение GracePeriodDays |
| DLL не загружается в AutoCAD | Файлы не в Contents/ | Проверьте структуру bundle и `CopyLocalLockFileAssemblies` |
| Команда не найдена | Нет `[assembly: CommandClass]` | Добавьте атрибуты в assembly |
| `Initialize` зависает | Большой `IpcTimeoutSeconds` | Уменьшите таймаут или используйте `Task.Run` |
| Плагин работает, но лицензия `NotFound` | Не куплена/не активирована | Пользователь должен купить лицензию в User Panel |

**Диагностические логи:**

```
%LocalAppData%\GrossGeo\SDK.Stub\sdk_diag_YYYYMMDD.log
```

Откройте этот файл для детальной информации о работе SDK.

---

## 22. FAQ

### Обязательно ли пользователю устанавливать User Panel?

Да. User Panel — это центральное приложение, которое управляет лицензиями всех плагинов GrossGeo. Пользователь устанавливает его один раз.

### Что будет, если User Panel не запущен?

SDK попробует использовать кэшированный результат (offline-режим). Если кэш действителен (не старше GracePeriodDays), плагин продолжит работать. Если кэша нет — лицензия будет невалидной.

### Можно ли использовать SDK без лицензирования?

Да. Для бесплатных продуктов SDK используется для аналитики, обновлений и распространения через каталог. Просто не добавляйте проверки `Protect`/`RequireFeature`.

### Поддерживается ли multi-target (net48 + net8.0)?

Да. SDK поддерживает оба таргета. Используйте `<TargetFrameworks>net48;net8.0-windows</TargetFrameworks>` в вашем `.csproj`.

### Как протестировать лицензирование локально?

1. Запустите User Panel.
2. Создайте тестовый продукт в Developer Portal.
3. Активируйте тестовую лицензию.
4. Запустите AutoCAD с вашим плагином.

### Можно ли подключить свою систему логирования?

Да. Реализуйте интерфейс `ILicenseLogger` и передайте в `LicenseOptions.Logger`.

### Как работает Maintenance для Perpetual?

Perpetual лицензия бессрочная. Maintenance — это отдельная годовая подписка, дающая право на обновления. Если Maintenance истёк, плагин продолжает работать на текущей версии, но новые версии не устанавливаются (version lock).

### Где получить ProductKey?

В Developer Portal: создайте продукт → на странице продукта будет API Key (ProductKey).

### Какие форматы bundle поддерживаются?

- **Bundle** (`.bundle.zip`) — стандартный AutoCAD bundle. Рекомендуемый формат.
- **PluginDll** — одиночная DLL (для простых плагинов).
- **Installer** — MSI/EXE (для сложных продуктов с дополнительными зависимостями).
