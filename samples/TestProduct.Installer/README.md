# 📦 TestProduct.Installer

**Тип:** DistributionType.Installer (MSI)

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

- DistributionType.Installer (MSI, не Bundle)
- TrialBindingMode.AccountAndMachine (защита от повторного trial)
- Установка через MSI в `%ProgramData%\Autodesk\ApplicationPlugins\`
- Собственный `PackageContents.xml` с `UpgradeCode={FC25EC10-3975-4B1A-8DEC-24B9C2CBEAB4}`

## 🛠️ Требования к MSI

### Поведение при установке

| Сценарий | Поведение |
|----------|-----------|
| Первый запуск | EULA → Установка |
| Повторный запуск (продукт установлен) | Диалог: **Repair** / **Remove** |
| Запуск более новой версии | Автоматический MajorUpgrade (удаление старой → установка новой) |
| Запуск более старой версии | Ошибка: «A newer version is already installed» |

### Командная строка (msiexec)

```powershell
# Тихая установка
msiexec /i TestProduct.Installer.Setup.msi /qn

# Тихая установка с логом
msiexec /i TestProduct.Installer.Setup.msi /qn /l*v install.log

# Установка в произвольную папку
msiexec /i TestProduct.Installer.Setup.msi /qn INSTALLFOLDER="C:\MyPlugins\TestProduct.Installer.bundle"

# Восстановление (Repair)
msiexec /f TestProduct.Installer.Setup.msi /qn

# Удаление по файлу MSI
msiexec /x TestProduct.Installer.Setup.msi /qn

# Удаление по ProductCode (GUID из MSI)
msiexec /x {PRODUCT-CODE-GUID} /qn
```

### Используемые технологии

- **WiX Toolset v5** (SDK `WixToolset.Sdk/5.0.2`)
- **WixUI_Minimal** — стандартный UI с EULA и Repair/Remove
- **WixToolset.UI.wixext** — расширение для диалогов

## 📁 Структура

```
TestProduct.Installer/
├── TestInstallerPlugin.cs          # AutoCAD plugin
├── TestProduct.Installer.csproj    # Plugin DLL project
├── TestProduct.Installer.bundle/   # Bundle для AutoCAD
│   ├── PackageContents.xml         # UpgradeCode={FC25EC10-...}
│   └── Contents/
│       └── grossgeo-settings.json
├── MsiInstaller/                   # WiX v5 MSI project
│   ├── MsiInstaller.wixproj
│   ├── Product.wxs
│   └── License.rtf                # EULA (WixUI_Minimal)
├── plans-manifest.json             # Импорт планов и фич через Developer Panel
└── fixtures/                       # Артефакты сборки (*.msi)
```

## 🔨 Сборка

```powershell
# Через общий скрипт
.\scripts\build-samples.ps1 -Products Installer

# Вручную
dotnet build TestProduct.Installer.csproj -c Release
dotnet build MsiInstaller\MsiInstaller.wixproj -c Release -p:ProductVersion=1.0.0
```
