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
<PackageReference Include="GrossGeo.SDK.Stub" Version="2.2.5" />
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
- Framework: выберите `.NET 8.0` (для AutoCAD 2025–2026), `.NET Framework 4.8` (для 2019–2024) или, отдельным таргетом,
  `.NET 10.0` (для AutoCAD 2027 — сборка под net8 в нём не грузится, см. §14 «Таблица версий AutoCAD» ниже).

> **Совет:** Для поддержки AutoCAD 2019–2026 используйте multi-target:
> `<TargetFrameworks>net48;net8.0-windows</TargetFrameworks>`. Если поддерживаете и 2027 — добавьте третий
> таргет `net10.0-windows` (пример полного csproj и манифеста — в §14).

### Шаг 2. Настройте .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- Multi-target для AutoCAD 2019-2024 (net48) и 2025-2026 (net8.0). Для 2027 нужен ещё net10.0-windows — см. §14. -->
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
    <PackageReference Include="GrossGeo.SDK.Stub" Version="2.2.5" />
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

> **Сборка для AutoCAD 2025+ — с `RuntimeIdentifier` `win-x64`.** Собирайте продукт для AutoCAD 2025+ с RuntimeIdentifier win-x64 (`<RuntimeIdentifier>win-x64</RuntimeIdentifier>`, `<SelfContained>false</SelfContained>`). Без него рядом с продуктом ложится заглушка System.Management (~72 КБ) вместо настоящей (~312 КБ): отпечаток машины не читается, машинные лицензии отвечают `MACHINE_NOT_BOUND`. Проверка: `System.Management.dll` в бандле — настоящая, а не заглушка (признаки — ниже; для пакета 8.0.0 это ~312 КБ).
>
> **Почему так.** Пакет `System.Management` 8.0.0 несёт две сборки одной версии: в `lib/net8.0` — заглушку, каждый метод которой бросает `PlatformNotSupportedException`, и в `runtimes/win/lib/net8.0` — настоящую. Сборка без RuntimeIdentifier кладёт в корень выхода (а значит, и в бандл) заглушку. SDK просит `System.Management` 8.0.0.0, а AutoCAD 2025 несёт свою, более раннюю (4.0.0.0), поэтому запрос уходит в каталог вашего продукта — на заглушку. Опыт (образцы платформы, LGC-1512) — для `net8.0-windows`; для `net10.0-windows` то же ожидается по устройству пакета, опытом не проверено; для `net48` RuntimeIdentifier не нужен.
>
> **Как собирать.** Добавьте два свойства в `<PropertyGroup>` проекта (для мультитаргетного проекта условием можно исключить `net48`: `Condition="'$(TargetFramework)' != 'net48'"`). Если хотите, чтобы выход оставался в `bin\<конфигурация>\<цель>\`, добавьте ещё `<AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>`. Свойство сборки теряется при копировании проекта, а уезжает в бандл готовый набор файлов — поэтому **проверяйте бандл, а не .csproj**: найдите в нём `System.Management.dll` и определите, откуда взята сборка: **главный признак — где она лежала в пакете.** Настоящая — из `runtimes/win/lib/net8.0` (так выходит при сборке с `win-x64`), заглушка — из `lib/net8.0` (так выходит при сборке без RuntimeIdentifier). Размер — только улика, не определение: для пакета `System.Management` 8.0.0 настоящая — 311 984 байта (в Проводнике ~305 КБ), заглушка — 72 968 байт (~72 КБ); при другой версии пакета числа будут другими — сравнивайте сборку из вашего выхода с обеими сборками из каталога пакета в кэше NuGet. Если менять сборку не хотите — положите в бандл рядом с продуктом настоящую сборку из пакета (`runtimes/win/lib/net8.0/System.Management.dll` пакета `System.Management` 8.0.0) вместо `lib/net8.0` и проверьте размер так же; этот путь надёжнее всего проверять на машине с AutoCAD.
>
> **С SDK 2.2.6:** при таком отказе в журнале SDK (`%LOCALAPPDATA%\GrossGeo\SDK.Stub\sdk_diag_*.log`) будет строка «MachineFingerprint WARN: Failed to get CPU ID: …».

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

Если нужно дождаться первого вердикта перед продолжением. Поток блокируется **не дольше 5 секунд**, независимо от
`IpcTimeoutSeconds` (с SDK 2.2.5). Если за это время вердикта нет — например, User Panel не отвечает, — `InitializeSync`
возвращает временный результат «лицензия ещё проверяется» (`ErrorCode = NOT_CHECKED`, `Status = Unknown`), а проверка
продолжается в фоне; её исход приходит событием `LicenseRefreshed`. Подпишитесь на событие ДО вызова. Продукт,
собранный против SDK старше 2.1.14, вместо `NOT_CHECKED` получает привычный временный `NETWORK_ERROR`.

```csharp
public void Initialize()
{
    // ДО вызова: если первый вердикт не успеет за 5 с, его исход придёт этим событием
    // (с фонового потока — вывод в AutoCAD через RunOnMainThread, как в примере выше).
    GrossGeoLicense.LicenseRefreshed += OnLicenseRefreshed;

    var result = GrossGeoLicense.InitializeSync(new LicenseOptions
    {
        // DOC-098: замените на настоящий ключ — литерал ниже не пройдёт
        // формат-валидацию SDK, см. раздел 5, шаг 3.
        ProductKey = "GG-XXXX-XXXX-XXXX-XXXX",
        PluginVersion = "1.0.0"
    });

    if (result.ErrorCode == "NOT_CHECKED")
    {
        // Проверка ещё идёт — это не отказ; исход придёт в OnLicenseRefreshed.
    }
    else if (!result.IsValid)
    {
        Ed?.WriteMessage($"\n[MyPlugin] Лицензия недействительна: {result.Message}");
    }
}
```

> **Не ждите на главном потоке внутри `IExtensionApplication.Initialize` фоновую работу, которая первой
> загружает сборки вне общего фреймворка .NET.** На AutoCAD 2025 и новее, пока исполняется `Initialize` вашего
> плагина, AutoCAD не загрузит на другом потоке сборку, которой нет в общем фреймворке (например, NuGet-зависимость
> плагина из его папки): загрузка ждёт выхода из `Initialize`. Если в `Initialize` запустить задачу (`Task.Run`) и
> ждать её (`.Wait()`, `.Result`, `.GetAwaiter().GetResult()`), а задача первой касается такой сборки, ожидание
> продлится до своего предела — снаружи это выглядит как «плагин зависает при загрузке AutoCAD». Замер в
> accoreconsole AutoCAD 2025: 45 с ожидания против 2 мс, когда то же ожидание стоит не в `Initialize`, а в команде.
> Что делать: не ждать фоновую работу в `Initialize` (асинхронная инициализация выше) либо загрузить нужные сборки
> заранее на главном потоке — коснуться типа из каждой (`_ = typeof(SomeType).Assembly;`) до запуска задачи.
>
> Это касается и вашего `ILicenseLogger` (раздел 19): SDK вызывает его из фонового потока, в том числе пока идёт
> `InitializeSync`. Если реализация логгера первой касается своей библиотеки вне общего фреймворка, загрузите её
> зависимости в своём `Initialize` ДО вызова SDK либо держите логгер на сборках общего фреймворка. То же касается
> сателлитных сборок ресурсов (`*.resources.dll` в подпапке культуры): первое чтение локализованной строки на фоновом
> потоке — например, в обработчике исключения — не оставляйте на время ожидания в `Initialize`.
>
> **Не блокируйте главный поток на вызовах SDK (`.Wait()`, `.Result`) и не передавайте вызовы логгера синхронно в
> главный поток** (`Invoke`, `Dispatcher.Invoke`). На .NET Framework (AutoCAD 2019–2024) SDK с версии 2.2.5 вызывает
> `ILicenseLogger` из фонового потока и во время `Initialize`: если главный поток ждёт задачу SDK, а логгер ждёт
> главный поток, оба стоят друг за другом, и предела у такой блокировки нет — AutoCAD зависает насовсем. Ждите через
> `await`; логгер, которому нужен главный поток, пусть передаёт запись асинхронно (`BeginInvoke`) или пишет в файл.

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

> **С SDK 2.2.3 (обязательное обновление)** три изменения важны именно для Concurrent-сценария
> ниже:
> 1. **Исключение в вашем обработчике `SessionExpired` или `LicenseRefreshed` больше не роняет
>    `acad.exe`.** Каждый подписчик вызывается изолированно, ошибка уходит в лог SDK с именем
>    подписчика, `SessionExpired` поднимается один раз за сессию.
> 2. **`SessionExpiredEventArgs` получил `ProductKey` (nullable)** — чей слот снят. Полезно, если
>    один процесс AutoCAD инициализирует несколько Concurrent-продуктов.
> 3. **.NET Framework (AutoCAD 2019–2024): обмен с User Panel, принявшей соединение и не
>    ответившей, раньше блокировал поток вызывающего НАВСЕГДА** — при загрузке, в команде или на
>    выходе. Теперь он ограничен сроком (`IpcTimeoutSeconds`) и завершается `CONNECTION_FAILED`.
>
> Оба события приходят с фонового потока — вызывать API AutoCAD из обработчиков можно только
> через главный поток (см. пример ниже, `RunOnMainThread`).

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
      ОДИН ComponentEntry на модуль и диапазон версий; платформы — через «|» в Platform
      (AutoCAD|Civil3D). Не заводите отдельный ComponentEntry на каждую платформу и
      не используйте звёздочку (*) в Platform.
    -->
    <ComponentEntry
        AppName="MyPlugin"
        ModuleName="./Contents/MyPlugin.dll"
        AppDescription="MyPlugin for AutoCAD and Civil 3D"
        AppType=".Net"
        LoadOnAutoCADStartup="True">
      <!--
        SeriesMin/SeriesMax — диапазон поддерживаемых версий AutoCAD.
        Таблица версий — ниже.
      -->
      <RuntimeRequirements
          OS="Win64"
          Platform="AutoCAD|Civil3D"
          SeriesMin="R24.0"
          SeriesMax="R25.1"/>
    </ComponentEntry>
  </Components>
</ApplicationPackage>
```

**Платформы — через «|» в одной записи, а не отдельными записями.** При установке из каталога панель
проверяет манифест из вашего архива и, если платформы разнесены по отдельным `ComponentEntry`
(одна запись — одна платформа для одного и того же модуля), **заменяет его своим**: наш генератор
пишет по одной записи на каждую сборку (`net48` / `net8` / `net10`, со своими `SeriesMin`/`SeriesMax`),
а платформы — через «|» (`AutoCAD|Civil3D`). Сервер читает `Platform` на любом уровне
`RuntimeRequirements`. Если хотите, чтобы установленный манифест остался вашим, — пишите платформы
через «|» и по одной записи на сборку.

Платформа должна быть названа в `Platform` **явно**: `AutoCAD*` у Autodesk покрывает AutoCAD и
продукты на его основе, но платформа сводит его к `AutoCAD` — для Civil 3D и других вертикалей
называйте их в `Platform` прямо. Замер платформы 28.09.2026 на Civil 3D 2027 (при созданном разделе
автозагрузки Civil 3D `AcadAutoLoader`): бандл с `Platform="AutoCAD|Civil3D"` получил модули; у
бандлов с `Platform="AutoCAD"` в автозагрузчике появилась лишь запись бандла, без модулей.

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

### Что объявлять: платформы и серии — AutoCAD 2027 и Civil 3D

Платформы и серии объявляются в двух местах, и они должны говорить одно: в `PackageContents.xml` (выше) и в метаданных релиза (`release-manifest.json` внутри bundle, а при публикации через API или `gg publish` — поля запроса).

| Что | Что писать |
|-----|------------|
| AutoCAD 2027 (R26.0) | Отдельная сборка `net10.0-windows` и своя запись `ComponentEntry` с `SeriesMin="R26.0" SeriesMax="R26.0"` (пример выше). Сборка под `net8` серии `R26.x` не получит: ноги, переданные полями запроса, сервер с такими сериями отвергает (`400`), а из `PackageContents.xml` серии обрезаются полосой `net8` (до `R25.9`). |
| Civil 3D 2027 | Та же сборка `net10.0-windows`; платформа — `Civil3D`. Одной записью `Platform="AutoCAD\|Civil3D"` (значения через «\|») или двумя записями — каждая со своей платформой и той же полосой серий. |
| Платформы релиза (`targetPlatforms`) | Из восьми имён: `AutoCAD`, `Civil3D`, `Map3D`, `Architecture`, `Mechanical`, `Electrical`, `MEP`, `Plant3D`. Для AutoCAD и Civil 3D — `["AutoCAD", "Civil3D"]`. Регистр не важен; `Civil` сервер приводит к `Civil3D`, `AutoCAD*` — к `AutoCAD` (вертикали от звёздочки не добавляются). |

**Что делает сервер.** Если у выпуска есть ноги рантаймов (`TargetRuntimes`), при отправке бандла сервер сверяет платформы с архивом: сначала `targetPlatforms` из `release-manifest.json`, иначе значения `Platform` всех `RuntimeRequirements` из `PackageContents.xml`. Если в выпуске платформ нет или стоит ровно `["AutoCAD"]` — даже заданное явно — а архив объявляет больше, берутся платформы архива; любой другой явный список остаётся как есть. Без ног платформы из архива не берутся, поэтому передавайте `targetPlatforms` явно. Незнакомое имя платформы из архива пропускается (в журнале сервера); незнакомое имя в явном запросе — `400 TARGET_PLATFORM_UNKNOWN` с перечнем допустимых.

**Серии по рантаймам.** Если ноги рантайма вы задаёте полями запроса (`targetRuntimes`: `framework`, `dllPath`, `seriesMin`, `seriesMax`), серии должны лежать внутри полосы своего рантайма, иначе `400`: `net48` — `R23.0`–`R24.3`, `net8` — `R25.0`–`R25.9`, `net10` — `R26.0`–`R26.9`. Допустимые `framework`: `net48`, `net8.0-windows`, `net8.0`, `net10.0-windows`, `net10.0`; повторять `framework` в одном релизе нельзя, `SeriesMin` не может быть выше `SeriesMax`.

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

> **`targetPlatforms` — поле релиза, а не продукта.** В Developer Portal форма нового релиза сама подставляет платформы
> последнего Published/Approved/Submitted релиза того же продукта — повторно отмечать их не нужно. **Публикуя через API/CLI
> (`gg publish`, свой скрипт) — передавайте поле явно.** Оно не наследуется на этом пути: пропущенное значение сервер молча
> заменяет на `["AutoCAD"]`, и продукт остаётся объявлен только для AutoCAD, даже если предыдущий релиз указывал больше.
> В портале правка доступна только для черновика или отклонённого релиза; у опубликованного эту настройку сейчас не видно
> и не поменять — нужен новый релиз.

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

### Коды отказа (`ErrorCode`)

`Message` — текст для показа участнику. **Ветвиться нужно по `Status` и `ErrorCode`, а не по тексту**: тексты
меняются от версии к версии и локализуются, коды — нет (значения кодов не меняются, новые только добавляются).
**Код, которого ваша версия SDK ещё не знает, читайте как временный сбой:** покажите `Message` и не отзывайте
право — SDK так же читает незнакомый код.

Как читать таблицы:

- **Как читает SDK.** `окончательный` — отказ по праву: офлайн-кэш такому ответу не помогает; `нужен вход` —
  участнику надо войти в User Panel (статус `Unavailable`, кэш тоже не выдаётся); `временный` — не окончательный
  отказ: связь или проверка могут вернуться, и продукт не должен считать право отнятым.
- **Офлайн-кэш.** `стирается` — на этот отказ SDK стирает офлайн-кэш продукта (право отнято); `не отдаётся` — кэш не
  выдаётся и при этом не стирается (он пригодится, когда причина отпадёт); `отдаётся до конца срока` — при таком сбое продукт продолжает работать из кэша в
  пределах его срока; `—` — вопрос не ставится: это результат самого SDK («не спросили», «не смогли», ошибка запроса).
- **Где.** Тип результата, в котором встречается код (`LicenseResult`, `SessionResult`, `SessionReleaseResult`,
  `UsageResult`), либо «ответы панели» для запросов, не связанных с лицензией.
- **SDK.** С какой версии SDK код читается так, как в таблице. `≤ 2.1.17` — версии до 2.1.17 в перечне не
  различаются (код известен уже в ней); `стирание — с …` — версия, с которой SDK стирает офлайн-кэш по этому коду.

Таблица описывает SDK 2.2.5. Более старым SDK панель подменяет новые коды на те, что они знают, — вот почему у
продукта на старой версии код может отличаться от таблицы:

- `LICENSE_NOT_ACTIVE` для SDK младше 2.1.15 приходит как `LICENSE_EXPIRED`;
- `USER_NOT_ASSIGNED` (SDK младше 2.1.7), `FINGERPRINT_REQUIRED` (младше 2.1.17), `MACHINE_NOT_BOUND` и
  `MACHINE_LIMIT_EXCEEDED` (младше 2.0.0), `SDK_TOO_OLD_FOR_MACHINE_LICENSE` (любой SDK) приходят как
  `LICENSE_NOT_ACTIVE`;
- `FEATURE_LIMITS_UNAVAILABLE` и `FEATURE_LIMIT_NOT_CONFIGURED` для SDK младше 2.2.4 — как `FEATURE_NOT_AVAILABLE`;
- `CLOCK_UNTRUSTED` для SDK младше 2.2.5 — как `SERVICE_UNAVAILABLE`.

**Право на продукт**

| Код | Что значит | Как читает SDK | Офлайн-кэш | Где | SDK |
|-----|------------|----------------|------------|-----|-----|
| `LICENSE_NOT_FOUND` | У аккаунта нет лицензии на продукт. | окончательный | стирается | LicenseResult | ≤ 2.1.17, стирание — с 2.2.4 |
| `LICENSE_EXPIRED` | Срок лицензии истёк. | окончательный | стирается | LicenseResult | 2.0.0, стирание — с 2.2.4 |
| `LICENSE_NOT_ACTIVE` | Лицензия есть, но не действует: отозвана, приостановлена или заблокирована. | окончательный | стирается | LicenseResult | 2.1.15, стирание — с 2.2.4 |
| `PRODUCT_BLOCKED` | Продукт закрыт модерацией платформы. | окончательный | стирается | LicenseResult | 2.2.0, стирание — с 2.2.4 |
| `USER_NOT_ASSIGNED` | Лицензия на команду, а этому участнику место не назначено. | окончательный | стирается | LicenseResult | 2.1.7, стирание — с 2.2.4 |
| `INVALID_PRODUCT_KEY` | Ключ продукта не той формы или неизвестен платформе: проверьте `ProductKey`. | окончательный | не отдаётся | LicenseResult | ≤ 2.1.17 |
| `MACHINE_NOT_BOUND` | Лицензия режима Machine, а эта машина к ней не привязана. | окончательный | не отдаётся | LicenseResult | 2.0.0 |
| `MACHINE_LIMIT_EXCEEDED` | Исчерпан предел машин лицензии. | окончательный | не отдаётся | LicenseResult | 2.0.0 |
| `FINGERPRINT_REQUIRED` | Режиму лицензии нужен отпечаток машины, а продукт его не прислал. | окончательный | не отдаётся | LicenseResult | 2.1.17 |
| `SDK_TOO_OLD_FOR_MACHINE_LICENSE` | Продукт собран на SDK старше 2.1.17 и присылает устаревший отпечаток машины. Продукту этот код не приходит: он получает `LICENSE_NOT_ACTIVE`, а причину показывает панель. Лечится пересборкой продукта. | не приходит продукту | — | LicenseResult | — |
| `OFFLINE_NOT_ALLOWED` | Офлайн-работа для этой лицензии не разрешена. | временный | — | LicenseResult | ≤ 2.1.17 |
| `PRODUCT_NOT_FOUND` | Продукт не найден. Временный намеренно: намеренное отсечение продукта — `PRODUCT_BLOCKED`. | временный | отдаётся до конца срока | LicenseResult | ≤ 2.1.17 |
| `LICENSE_NOT_ASSIGNED` | У участника нет назначения на лицензию (`Status = NoAssignment`): запросите привязку через User Panel. | временный | — | LicenseResult | ≤ 2.1.17 |
| `NO_SEATS` | Все места лицензии заняты (`Status = NoAvailableSeats`). | временный | — | LicenseResult | ≤ 2.1.17 |

**Вход участника**

| Код | Что значит | Как читает SDK | Офлайн-кэш | Где | SDK |
|-----|------------|----------------|------------|-----|-----|
| `NOT_AUTHENTICATED` | Участник не вошёл в User Panel. | нужен вход | не отдаётся | LicenseResult | ≤ 2.1.17 |
| `NO_ACCOUNT` | Сессия есть, но аккаунт по ней не определён; нужен вход. | нужен вход | не отдаётся | LicenseResult | 2.2.0 |

**Функции и лимиты**

| Код | Что значит | Как читает SDK | Офлайн-кэш | Где | SDK |
|-----|------------|----------------|------------|-----|-----|
| `FEATURE_NOT_AVAILABLE` | Функции нет в тарифе участника. | окончательный | не отдаётся | LicenseResult | ≤ 2.1.17 |
| `FEATURE_LIMITS_UNAVAILABLE` | Данные о лимитах функции сейчас недоступны; повторите позже. SDK младше 2.2.4 получает вместо него `FEATURE_NOT_AVAILABLE`. | временный | отдаётся до конца срока | LicenseResult | 2.2.4 |
| `FEATURE_LIMIT_NOT_CONFIGURED` | У функции, входящей в тариф, не настроен лимит: ошибка конфигурации продукта, а не отказ участнику. SDK младше 2.2.4 получает вместо него `FEATURE_NOT_AVAILABLE`. | окончательный | не отдаётся | LicenseResult | 2.2.4 |
| `USAGE_LIMIT_EXCEEDED` | Исчерпан предел использования функции. | временный | — | UsageResult | ≤ 2.1.17 |
| `INVALID_ARGS` | Не указаны `featureCode` и `limitCode`. | временный | — | UsageResult | ≤ 2.1.17 |

**Concurrent-сессии**

| Код | Что значит | Как читает SDK | Офлайн-кэш | Где | SDK |
|-----|------------|----------------|------------|-----|-----|
| `SESSION_EXPIRED` | Concurrent-сессия истекла. | окончательный | не отдаётся | SessionResult | ≤ 2.1.17 |
| `SESSION_TOKEN_LOST` | У активной Concurrent-сессии пропал токен; сессию надо получить заново. | временный | — | SessionResult | 2.2.3 |
| `SESSION_LIMIT_REACHED` | Concurrent: все слоты заняты. | окончательный | не отдаётся | SessionResult | 2.2.4 |
| `MACHINE_NOT_OWNED` | Concurrent: слот принадлежит другой машине. | окончательный | не отдаётся | SessionResult | 2.2.4 |
| `CONCURRENCY_CONFLICT` | Concurrent: конфликт при получении сессии. | окончательный | не отдаётся | SessionResult | 2.2.4 |
| `CONCURRENT_OVERLAY_REQUIRED` | Concurrent: к лицензии нужен дополнительный слой Concurrent. | окончательный | не отдаётся | SessionResult | 2.2.4 |
| `NOT_CONCURRENT` | Сервер: лицензия не Concurrent, а запрошена сессия. | окончательный | не отдаётся | SessionResult | 2.2.4 |
| `NOT_CONCURRENT_MODE` | SDK: известный режим лицензии — не Concurrent, сессия не нужна. | временный | — | SessionResult | 1.0.0 |
| `LICENSE_MODE_UNKNOWN` | SDK не смог определить режим лицензии; повторите, когда панель ответит. | временный | — | SessionResult | 2.2.0 |
| `NOT_ACQUIRED` | Освобождение сессии, которая не была получена в этом процессе. | временный | — | SessionReleaseResult | 2.2.0 |

**Связь, часы и кэш (не отказ в праве)**

| Код | Что значит | Как читает SDK | Офлайн-кэш | Где | SDK |
|-----|------------|----------------|------------|-----|-----|
| `SERVICE_UNAVAILABLE` | Служба лицензирования недоступна; так же читается код, которого ваша версия SDK ещё не знает. | временный | отдаётся до конца срока | LicenseResult | ≤ 2.1.17 |
| `TIMEOUT` | Панель не ответила за отведённое время. | временный | отдаётся до конца срока | LicenseResult | ≤ 2.1.17 |
| `RATE_LIMIT_EXCEEDED` | Панель или сервер отказали: превышен предел обращений. Повторите позже. | временный | отдаётся до конца срока | LicenseResult | 1.0.0 |
| `LOCAL_RATE_LIMITED` | Ограничитель самого SDK: «мы не спрашивали», а не «нам отказали». | временный | — | LicenseResult | ≤ 2.1.17 |
| `IPC_CONNECTION_FAILED` | SDK не смог соединиться с панелью. | временный | — | LicenseResult | 1.0.0 |
| `IPC_CONNECTION_TIMEOUT` | Соединение с панелью не установлено за отведённое время. | временный | — | LicenseResult | 1.0.0 |
| `USER_PANEL_NOT_RUNNING` | User Panel не запущена (и запустить её не удалось). | временный | — | LicenseResult | 1.0.0 |
| `USER_PANEL_BUSY` | Приложение GrossGeo запущено, но не ответило: занято, ещё запускается или закрывается. Повторите позже. | временный | — | LicenseResult | 2.2.4 |
| `NETWORK_ERROR` | Спросить не удалось: нет связи с панелью. | временный | — | LicenseResult | 1.0.0 |
| `NOT_INITIALIZED` | SDK не инициализирован: вызов до `Initialize`. Это «не смогли спросить», а не отказ. | временный | — | LicenseResult | 1.0.0 |
| `NOT_CHECKED` | Лицензию ещё не проверяли (`Status = Unknown`): это «не спрашивали», а не отказ. | временный | — | LicenseResult | ≤ 2.1.17 |
| `MACHINE_NOT_IDENTIFIED` | SDK не смог определить машину (отпечаток не собран за отведённое время). Временный: SDK переспрашивает сам; офлайн-кэш не выдаётся и не стирается — без отпечатка его не прочесть и не сверить. Только для продукта, собранного против SDK 2.2.4 и новее; собранному против более старого тот же случай приходит как `NETWORK_ERROR`. | временный | не отдаётся | LicenseResult | 2.2.4 |
| `CLOCK_UNTRUSTED` | Внутренний код приложения (часы недостоверны по суждению панели). Продукту не приходит: SDK 2.2.5 и новее отдаёт его как `CACHE_CLOCK_MISMATCH`, SDK младше 2.2.5 получает `SERVICE_UNAVAILABLE`. | не приходит продукту | — | LicenseResult | 2.2.5 |
| `SIGNING_UNAVAILABLE` | Панель не может подписать вердикт: ключ подписи недоступен. Закрывает только подписанный путь. | временный | отдаётся до конца срока | LicenseResult | 2.2.1 |
| `SIGNATURE_INVALID` | Подпись ответа панели не сошлась; ответ не используется. | временный | — | LicenseResult | 1.0.0 |
| `SIGNATURE_MISSING` | В ответе панели нет подписи; ответ не используется. | временный | — | LicenseResult | ≤ 2.1.17 |
| `CACHE_EXPIRED` | Срок годности офлайн-кэша истёк, а панели нет. | временный | — | LicenseResult | 1.0.0 |
| `CACHE_CLOCK_MISMATCH` | Часы машины недостоверны (`Status = Blocked`): срок кэша проверить нечем. Отказ не окончательный — повторный запрос его перепроверит; офлайн-кэш не выдаётся и не стирается. Исправьте часы или дождитесь связи. | временный | не отдаётся | LicenseResult | ≤ 2.1.17 (от панели — с 2.2.5) |
| `CACHE_BINDING_MISMATCH` | Сохранённая лицензия относится к другой машине или другой учётной записи Windows (`Status = Blocked`); пользователь исправить не может. | временный | — | LicenseResult | ≤ 2.1.17 |
| `EXCEPTION` | Неожиданное исключение внутри SDK; текст — в `ErrorMessage`. | временный | — | SessionResult, SessionReleaseResult, UsageResult | 1.0.0 |

**Протокол и прочие запросы к панели**

| Код | Что значит | Как читает SDK | Офлайн-кэш | Где | SDK |
|-----|------------|----------------|------------|-----|-----|
| `INVALID_REQUEST` | Панель не разобрала запрос продукта. | временный | — | LicenseResult | 1.0.0 |
| `INVALID_RESPONSE` | SDK не разобрал ответ панели. | временный | — | LicenseResult | ≤ 2.1.17 |
| `INTERNAL_ERROR` | Внутренняя ошибка панели или SDK при обработке запроса. | временный | — | LicenseResult | 1.0.0 |
| `PROTOCOL_VERSION_MISMATCH` | Версии протокола SDK и панели не совпали: обновите SDK продукта или панель. | временный | — | LicenseResult | 1.0.0 |
| `CATEGORY_NOT_FOUND` | Категория каталога не найдена (запросы каталога, не лицензирование). | временный | — | ответы панели | ≤ 2.1.17 |
| `CATALOG_UNAVAILABLE` | Каталог недоступен (запросы каталога, не лицензирование). | временный | — | ответы панели | ≤ 2.1.17 |
| `PLUGIN_NOT_INSTALLED` | Плагин не установлен (запросы панели о плагинах). | временный | — | ответы панели | ≤ 2.1.17 |
| `UPDATE_CHECK_FAILED` | Проверка обновлений не удалась. | временный | — | ответы панели | ≤ 2.1.17 |
| `PLUGIN_NOT_REGISTERED` | Плагин не зарегистрирован в панели. | временный | — | ответы панели | ≤ 2.1.17 |
| `WINDOW_NOT_AVAILABLE` | Окно панели недоступно (запросы навигации). | временный | — | ответы панели | ≤ 2.1.17 |
| `NAVIGATION_FAILED` | Переход в панели не выполнен. | временный | — | ответы панели | ≤ 2.1.17 |

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
// IsInGracePeriod в SDK 2.2.x не выставляется (всегда false): ответ из офлайн-кэша определяйте по IsOfflineMode,
// оставшиеся дни офлайна SDK кладёт в Message
if (GrossGeoLicense.IsOfflineMode)
{
    Ed?.WriteMessage($"\n{GrossGeoLicense.LastResult?.Message}");
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

Формат: `[2026-09-25T13:48:15.917Z] [pid:48196] [acad] сообщение` — время в UTC с датой, номер и имя процесса.

### Пользовательский логгер

Вы можете перенаправить логи SDK в свою систему. SDK вызывает логгер из фоновых потоков, в том числе во время
инициализации, — зависимости реализации загрузите заранее (см. предупреждение в разделе 6):

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
| `InitializeSync(string productKey)` | Синхронная инициализация: ждёт первый вердикт не дольше 5 с, иначе `NOT_CHECKED` и позже `LicenseRefreshed` |
| `InitializeSync(LicenseOptions options)` | Синхронная с параметрами (тот же предел 5 с) |
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
| `IsInGracePeriod` | `bool` | В SDK 2.2.x не выставляется (всегда `false`); ответ из кэша определяйте по `IsOfflineMode` |
| `IsOfflineMode` | `bool` | Offline-режим |
| `Features` | `IReadOnlyList<string>` | Доступные фичи |
| `FeatureLimits` | `IReadOnlyDictionary<string, int>?` | Лимиты |
| `SessionToken` | `string?` | Токен concurrent-сессии |
| `SessionExpiresAt` | `DateTime?` | Срок сессии |
| `HasActiveSession` | `bool` | Есть активная сессия |
| `LastResult` | `LicenseResult?` | Последний результат |

> **Тексты `Message` — для показа участнику, не для сравнения в коде.** У `LicenseResult` и исключений SDK
> (`LicenseException`, `FeatureNotAvailableException`) текст `Message` SDK вправе смягчать и уточнять в любой версии, не
> меняя кода отказа. Ветвитесь по `LicenseResult.ErrorCode` и `Status` (у исключения — `LicenseException.Status`), а
> `Message` только выводите.

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
| `IsOfflineMode = true` | Нет подключения к интернету, ответ из офлайн-кэша | Восстановите интернет; оставшиеся дни — в `Message` (`IsInGracePeriod` в SDK 2.2.x не выставляется) |
| DLL не загружается в AutoCAD | Файлы не в Contents/ | Проверьте структуру bundle и `CopyLocalLockFileAssemblies` |
| Команда не найдена | Нет `[assembly: CommandClass]` | Добавьте атрибуты в assembly |
| `Initialize` зависает | Большой `IpcTimeoutSeconds` | Уменьшите таймаут или используйте `Task.Run` |
| Плагин работает, но лицензия `NotFound` | Не куплена/не активирована | Пользователь должен купить лицензию в User Panel |
| При старте `TypeLoadException`/`FileLoadException` из `GrossGeo.SDK.Stub`/`GrossGeo.Contracts` | Общая причина — в процессе AutoCAD уже загружена ЧУЖАЯ (не ваша) версия SDK.Stub/Contracts, и первая загруженная копия решает за всех — не порядок команд в вашем плагине и не ваш `.csproj`. Раньше всех, измеренно, грузится плагин ПЛАТФОРМЫ (см. ниже): на AutoCAD 2025+ это SDK.Stub и Contracts, на AutoCAD 2019–2024 — только Contracts (там SDK.Stub у каждого продукта свой, сосуществуют без конфликта). Значит: на AutoCAD 2025+ конфликт возможен и БЕЗ другого GrossGeo-продукта рядом — достаточно, чтобы ваш SDK.Stub был новее платформенного; на AutoCAD 2019–2024 `FileLoadException` по SDK.Stub возможен только при ДРУГОМ продукте рядом, но `TypeLoadException` по Contracts (новый тип, которого нет в платформенной копии) возможен и без него | На AutoCAD 2019–2024, конфликт с другим продуктом: обновите свой пакет `GrossGeo.SDK.Stub` до последней версии. **На AutoCAD 2025+ обновление своего пакета до версии новее платформы НЕ поможет — платформенная копия всё равно грузится первой и побеждает**, см. правило ниже; если не помогает — сообщите нам версии обоих продуктов и версию платформы участника |

**Если в одном AutoCAD уже загружен другой GrossGeo-продукт со своей копией SDK.** Резолвер сборок,
общий для всех продуктов платформы (`AppDomain.AssemblyResolve`, часть платформы, не вашего кода),
и сама модель загрузки .NET решают, чья копия `GrossGeo.SDK.Stub`/`GrossGeo.Contracts` останется в
процессе — не порядок команд в вашем плагине и не ваш `.csproj`. Если победила более старая копия,
ваш код может получить `TypeLoadException` (тип, добавленный в `GrossGeo.Contracts` позже, недоступен)
или `FileLoadException` (сама ваша `SDK.Stub` не загрузилась). Начиная с SDK 2.2.4 такой случай для
`GrossGeo.Contracts` не роняет `Initialize` — SDK один раз при старте проверяет, что фактически
загруженная копия Contracts достаточно новая, и, если нет, пишет об этом одну строку в свой
диагностический журнал, а функции, которым нужен более новый тип, отвечают «недоступно» вместо
падения. `TypeLoadException`/`FileLoadException` из СВОЕЙ собственной `SDK.Stub` эта проверка не
предотвращает — увидеть его можно только на машине, где уже установлен продукт со старой версией
SDK, и лечится это обновлением платформы на той машине, а не правкой вашего плагина.

**На AutoCAD 2025 и новее ваш продукт работает на SDK из комплекта платформы.** Платформа при старте
AutoCAD первой загружает из своего комплекта `GrossGeo.SDK.Stub` (последний выпущенный на момент
выпуска ЭТОЙ версии платформы) и `GrossGeo.Contracts` — и продукт, собранный против более старого SDK,
работает на этой копии. На AutoCAD 2019–2024 ваш продукт работает на своей копии SDK.Stub; общая у
всех продуктов только `GrossGeo.Contracts`.

**Если ваш продукт собран против SDK, который новее платформы, установленной у участника — на
AutoCAD 2025+ он не загрузится.** Платформа предзагружает свой комплект в самом начале `Initialize` —
измеренно первой в процессе AutoCAD (её плагин стартует раньше плагинов продуктов). Побеждает не более
новая копия, а та, что загрузилась первой; версия вашего собственного пакета `GrossGeo.SDK.Stub` тут
роли не играет. Ваш продукт получит `FileLoadException` (SDK.Stub) уже при собственном старте, без
какого-либо другого GrossGeo-продукта рядом. На AutoCAD 2019–2024 SDK.Stub у каждого продукта свой —
там этого риска нет; но и там платформа предзагружает общую `GrossGeo.Contracts`, и если ваш продукт
опирается на тип, добавленный в `Contracts` позже платформенной копии, вы получите `TypeLoadException`
даже на 2019–2024. Практический вывод: **выпускайте продукт на новом SDK ПОСЛЕ выхода платформы,
несущей этот SDK** — не раньше.

**Правило выпуска SDK внутри одной мажорной версии — что меняется у уже выпущенного продукта без пересборки:**

- **Исправления** — падение, зависание, ложный отказ или ложный доступ — действуют для всех продуктов, в том
  числе собранных против более старого SDK. На AutoCAD 2025+ они доезжают до вашего продукта с обновлением
  платформы у пользователя, без пересборки.
- **Смена поведения, которую видит ваш код** — новый статус или код отказа, иной смысл прежнего значения; событие поднимается на другом потоке исполнения,
  в другом порядке, или появился либо пропал шаг — только для продукта, пересобранного против версии, где она введена. Продукту, собранному
  против более старой версии, SDK сохраняет прежнее поведение: версию «собран против» он определяет по ссылке
  вашей сборки на `GrossGeo.SDK.Stub`.
- **Добавления** — новое событие, свойство, метод — доступны сразу и ничего не ломают: код, который о них не
  знает, их не использует.

Публичный API внутри мажорной версии только расширяется — это проверяется при каждом выпуске пакета.

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

### Поддерживается ли multi-target?

Да, SDK поддерживает три таргета: `net48` (AutoCAD 2019–2024), `net8.0-windows` (AutoCAD 2025–2026) и
`net10.0-windows` (**AutoCAD 2027 — сборка под net8 в нём не грузится**, см. §14 «Таблица версий AutoCAD»).
Для 2019–2026 достаточно `<TargetFrameworks>net48;net8.0-windows</TargetFrameworks>`; для 2027 добавьте
третьим `net10.0-windows` в тот же список и свою полосу `RuntimeRequirements` в манифесте (пример — там же).

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
