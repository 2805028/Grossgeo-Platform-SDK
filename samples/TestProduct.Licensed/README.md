# 💎 TestProduct.Licensed

**Тип:** Perpetual лицензия + Maintenance аддон  
**Multi-target:** net48 (AutoCAD 2019–2024) + net8.0-windows (AutoCAD 2025–2026) + net10.0-windows (AutoCAD 2027, R26.0)

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| ProductKey | `GG-4CE5-8D13-2CB8-032F` |
| LicensingMode | GrossGeo |
| DistributionType | **Bundle (Multi-target)** |
| Target Frameworks | `net48`, `net8.0-windows`, `net10.0-windows` |
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
- **Multi-target Bundle** (net48 + net8.0-windows + net10.0-windows в одном ZIP)
- Мультитаргетный PackageContents.xml — генерируется панелью участника при установке/обновлении
  из TargetRuntimes, которые сервер определяет по разложению архива (`LGC-1406`); студия свой
  `PackageContents.xml` в архив не кладёт
- Первый в дереве образец, у которого есть нога `net10.0-windows`: `GrossGeo.SDK.Stub.csproj`
  такой цели не имеет (только `net48;net8.0-windows`), ссылка зафиксирована явным
  `SetTargetFramework=net8.0-windows` в `TestProduct.Licensed.csproj` — это и есть проверка
  обещания README SDK «из проекта под net10.0-windows пакет подключается штатно» на практике,
  а не только правилами совместимости NuGet

## 🔧 Пин SDK.Stub фикстуры v1.0.0

Фикстура `fixtures/TestProduct.Licensed.v1.0.0.bundle.zip` собрана против
**SDK.Stub 2.2.4** — ровно Floor (`PanelSdkStubGate.Floor`, он же потолок E2E:
TestContainers не сидит в `AppReleases`, потолком становится Floor). Ворота
сервера при отправке читают наибольшую AssemblyRef-ссылку на SDK.Stub во всех
сборках ног .NET 8+; вложенный файл `GrossGeo.SDK.Stub.dll` — запасной источник,
net48-ноги не считаются (`ReleaseSdkStubProbe`, DOC-166). Поэтому пин фикстуры
обязан быть на потолке или ниже; «равно — проходит» —
граница, ради которой пин и выбран.

Пересборка — только скриптом `scripts/rebuild-licensed-fixture.ps1`:
ставит `<Version>` в `GrossGeo.SDK.Stub.csproj`, собирает сэмпл (все три ноги),
проверяет AssemblyVersion вложенных dll, AssemblyVersion плагина и AssemblyRef
«ссылка на GrossGeo.SDK.Stub» (= пин), сверяет набор файлов с прежним бандлом,
перезаписывает zip и возвращает версию csproj. Сборка без подписи
(`-p:SignAssembly=false -p:PublicSign=false`) — AssemblyVersion от подписи не
зависит. Прежняя сборка фикстуры (06.10) была «ссылка 2.2.5 / dll 2.1.6» —
расхождение, которое и красило E2E при потолке 2.2.4.
