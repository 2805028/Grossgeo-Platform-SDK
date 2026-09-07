# =====================================================================
# LGC-759: порождение двух входов сборки MSI-образца
# =====================================================================
#
# Пишет в каталог бандла два файла, которых НЕТ и не должно быть в git
# (.gitignore:401 исключает Samples/**/*.bundle/ как выход сборки):
#
#   <BundleDir>/PackageContents.xml
#   <BundleDir>/Contents/grossgeo-settings.json
#
# ЗАЧЕМ ОТДЕЛЬНЫЙ СКРИПТ, А НЕ ЦЕЛЬ MSBuild. Источник истины для границ
# AutoCAD — release-manifest.json, а MSBuild не умеет читать JSON без
# внешней задачи. Дублировать значения в .csproj значило бы завести вторую
# копию границы, которая разойдётся с манифестом молча. Один генератор
# зовут ОБА пути сборки (csproj и build-samples.ps1) — именно затем, чтобы
# не завести третье соглашение о том, где что лежит: расхождение двух
# таких соглашений и есть причина 2 записи LGC-759.
#
# ЧЕГО ЭТОТ СКРИПТ НЕ ДЕЛАЕТ. Он не трогает release-manifest.json и
# test-product-info.json — только читает. Границы AutoCAD берутся ИЗ
# манифеста, а не копируются из прежнего артефакта: остаток от 15.03
# нёс SeriesMin=R25.0 SeriesMax=R25.0 при манифесте R24.3..R25.1, то есть
# протух по обеим границам.
# =====================================================================

[CmdletBinding()]
param(
    # Каталог бандла: <...>/TestProduct.Installer.bundle
    [Parameter(Mandatory = $true)]
    [string]$BundleDir,

    # Каталог образца, где лежат release-manifest.json и test-product-info.json
    [Parameter(Mandatory = $true)]
    [string]$ProductDir,

    # Ключ продукта. По умолчанию читается из исходника образца — он там
    # и есть источник истины (TestInstallerPlugin.cs, private const ProductKey),
    # а build-samples.ps1 его же и переписывает при -Keys.
    [string]$ProductKey = "",

    # Версия. Пусто — берётся из release-manifest.json.
    [string]$Version = "",

    # UpgradeCode бандла. ПЕРЕНЕСЁН ДОСЛОВНО из артефакта, который ставился
    # (остаток от 15.03). Происхождение значения установить не удалось, а
    # смена UpgradeCode меняет поведение обновления — поэтому оно сохранено
    # как есть, а не выведено заново. Это НЕ тот UpgradeCode, что у MSI
    # (MsiInstaller.wixproj: 23C5894A-...): у пакета и у бандла они разные.
    [string]$BundleUpgradeCode = "{FC25EC10-3975-4B1A-8DEC-24B9C2CBEAB4}"
)

$ErrorActionPreference = "Stop"

function Fail([string]$Message) {
    # Отказывать ГРОМКО. Прежний механизм (Update-GrossGeoSettings) стоял под
    # сторожем Test-Path и на чистом клоне молча не делал ничего, а скрипт
    # отчитывался успехом. Это причина 3 записи LGC-759, и здесь её быть
    # не должно: любой недостижимый вход — ненулевой код возврата.
    Write-Host "  [x] LGC-759 generate-bundle-inputs: $Message" -ForegroundColor Red
    exit 1
}

$releaseManifestPath = Join-Path $ProductDir "release-manifest.json"
$productInfoPath     = Join-Path $ProductDir "test-product-info.json"
$pluginSourcePath    = Join-Path $ProductDir "TestInstallerPlugin.cs"

if (-not (Test-Path $releaseManifestPath)) { Fail "не найден release-manifest.json: $releaseManifestPath" }
if (-not (Test-Path $productInfoPath))     { Fail "не найден test-product-info.json: $productInfoPath" }

$release = Get-Content $releaseManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$info    = Get-Content $productInfoPath     -Raw -Encoding UTF8 | ConvertFrom-Json

# ── Ключ продукта ────────────────────────────────────────────────────
if (-not $ProductKey) {
    if (-not (Test-Path $pluginSourcePath)) { Fail "не найден TestInstallerPlugin.cs и не передан -ProductKey" }
    $src = Get-Content $pluginSourcePath -Raw -Encoding UTF8
    if ($src -match 'private\s+const\s+string\s+ProductKey\s*=\s*"([^"]*)"') {
        $ProductKey = $Matches[1]
    }
    else {
        Fail "в TestInstallerPlugin.cs не найдено объявление ProductKey и -ProductKey не передан"
    }
}
if (-not $ProductKey) { Fail "ключ продукта пуст" }

# ── Границы и платформы: ТОЛЬКО из манифеста ─────────────────────────
if (-not $Version) { $Version = $release.version }
if (-not $Version) { Fail "версия не задана и в release-manifest.json поля version нет" }

$seriesMin = $release.minAutoCADVersion
$seriesMax = $release.maxAutoCADVersion
if (-not $seriesMin) { Fail "в release-manifest.json нет minAutoCADVersion" }
if (-not $seriesMax) { Fail "в release-manifest.json нет maxAutoCADVersion" }

$platforms = ($release.targetPlatforms) -join "|"
if (-not $platforms) { Fail "в release-manifest.json пуст targetPlatforms" }

$os = ($release.supportedOS) -join "|"
if (-not $os) { Fail "в release-manifest.json пуст supportedOS" }

$loadOnStartup = if ($release.loadOnStartup) { "True" } else { "False" }

$productName = $info.name
if (-not $productName) { Fail "в test-product-info.json нет name" }

# ── Запись ───────────────────────────────────────────────────────────
$contentsDir = Join-Path $BundleDir "Contents"
New-Item -ItemType Directory -Force -Path $contentsDir | Out-Null

$xml = @"
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage SchemaVersion="1.0" AppVersion="$Version" Name="TestProduct.Installer" Description="$($info.shortDescription)" Author="GrossGeo" UpgradeCode="$BundleUpgradeCode">
  <CompanyDetails Name="GrossGeo" Url="https://grossgeo.ru"/>
  <Components Description="GrossGeo: $productName">
    <ComponentEntry AppName="TestProduct.Installer" ModuleName="./Contents/TestProduct.Installer.dll" AppDescription="$($info.shortDescription)" AppType=".Net" LoadOnAutoCADStartup="$loadOnStartup">
      <RuntimeRequirements OS="$os" Platform="$platforms" SeriesMin="$seriesMin" SeriesMax="$seriesMax"/>
    </ComponentEntry>
  </Components>
</ApplicationPackage>
"@

$packageContentsPath = Join-Path $BundleDir "PackageContents.xml"
[System.IO.File]::WriteAllText($packageContentsPath, $xml, [System.Text.UTF8Encoding]::new($false))

$settings = @{ productKey = $ProductKey } | ConvertTo-Json
$settingsPath = Join-Path $contentsDir "grossgeo-settings.json"
[System.IO.File]::WriteAllText($settingsPath, $settings, [System.Text.UTF8Encoding]::new($false))

# Проверяем СВОЙ результат, а не намерение: отсутствие файла после записи —
# отказ, а не повод для тишины.
if (-not (Test-Path $packageContentsPath)) { Fail "PackageContents.xml не появился по пути $packageContentsPath" }
if (-not (Test-Path $settingsPath))        { Fail "grossgeo-settings.json не появился по пути $settingsPath" }

Write-Host "  [v] LGC-759: PackageContents.xml (SeriesMin=$seriesMin SeriesMax=$seriesMax) и grossgeo-settings.json порождены в $BundleDir"
