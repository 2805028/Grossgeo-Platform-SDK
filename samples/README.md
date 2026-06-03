# 🧪 GrossGeo SDK Test Products

Тестовые AutoCAD плагины для демонстрации различных моделей лицензирования GrossGeo Platform.

> **SDK v2.1.0:** Все примеры используют паттерн `ProductLicenseAccessor` для корректной работы
> нескольких плагинов в одном процессе AutoCAD. См. [Multi-plugin](../README.md#multi-plugin-несколько-плагинов-в-одном-процессе).

## 📦 Продукты

| Проект | Тип | API Key | Описание |
|--------|-----|---------|----------|
| [TestProduct.Free](./TestProduct.Free/) | 🆓 FREE | `GG-FREE-TEST-0001` | Бесплатный, без лицензии |
| [TestProduct.Subscription](./TestProduct.Subscription/) | 🔄 SUBSCRIPTION | `GG-SUBS-TEST-0002` | Trial → Подписка с Features |
| [TestProduct.Freemium](./TestProduct.Freemium/) | ⭐ FREEMIUM | `GG-FRMI-TEST-0004` | Бесплатный базовый + PRO |
| [TestProduct.Licensed](./TestProduct.Licensed/) | 💎 PAID | `GG-PAID-TEST-0003` | Разовая покупка, бессрочная |
| [TestProduct.Analytics](./TestProduct.Analytics/) | 📊 ANALYTICS | `GG-FA06-B797-8461-2CD1` | Внешнее лицензирование |

## 🚀 Быстрый старт

### 1. Получите ProductKey в Developer Portal

1. Войдите в **Developer Portal** под учётной записью разработчика.
2. Создайте продукт (или откройте существующий) и скопируйте его **ProductKey**.
3. Подставьте ключ в константу `ProductKey` соответствующего примера.

### 2. Соберите плагины

```bash
# Все сразу (PowerShell)
Get-ChildItem Samples -Directory | ForEach-Object { dotnet build $_.FullName -c Release }

# Или по одному
dotnet build Samples\TestProduct.Free -c Release
dotnet build Samples\TestProduct.Subscription -c Release
dotnet build Samples\TestProduct.Freemium -c Release
dotnet build Samples\TestProduct.Licensed -c Release
dotnet build Samples\TestProduct.Analytics -c Release
```

### 3. Установите через User Panel

1. Откройте **User Panel** и войдите в свою учётную запись.
2. Найдите продукт в каталоге и установите его.
3. Лицензия и обновления доставляются плагину через User Panel (Named Pipe IPC) —
   отдельная настройка не требуется.

### 4. Загрузите в AutoCAD

```
NETLOAD → выберите DLL из bin\Release\net8.0-windows\
```

## 📊 Матрица возможностей

| Возможность | Free | Subscription | Freemium | Paid | Analytics |
|-------------|------|--------------|----------|------|-----------|
| Без лицензии | ✅ | ❌ | ✅* | ❌ | ✅ |
| Trial | — | ✅ 14д | ✅ 7д | ❌ | — |
| Features | — | ✅ | ✅ | — | — |
| Подписка | — | ✅ | ✅ | — | — |
| Разовая покупка | — | — | — | ✅ | — |
| Бессрочная | — | — | — | ✅ | — |
| Внешняя лицензия | — | — | — | — | ✅ |

*Freemium: базовые функции без лицензии, PRO по подписке

## 🔑 Модели лицензирования

### 🆓 FREE
- Полностью бесплатный продукт
- SDK инициализируется для аналитики
- `IsValid = true` всегда

### 🔄 SUBSCRIPTION
- Начинается с Trial (14 дней)
- Требует подписку после триала
- Поддерживает планы с разными Features
- Standard/Pro/Enterprise

### ⭐ FREEMIUM
- Базовые функции бесплатны
- PRO-функции по подписке
- Default features для Free tier
- Upsell механики

### 💎 PAID (One-Time)
- Разовая покупка
- Бессрочная лицензия
- Привязка к машинам
- Grace period 7 дней

### 📊 ANALYTICS
- Файлы на GrossGeo
- Лицензирование внешнее
- Только статистика использования

## 🧪 Тестовые учётные записи

| Email | Пароль | Роль |
|-------|--------|------|
| `test@grossgeo.com` | `Test123!` | User |
| `dev@grossgeo.com` | `Dev123!` | Developer |
| `admin@grossgeo.com` | `Admin123!` | Admin |

## 📁 Структура

```
Samples/
├── README.md                      # Этот файл
├── TestProduct.Free/
│   ├── TestProduct.Free.csproj
│   ├── TestFreePlugin.cs
│   └── README.md
├── TestProduct.Subscription/
│   ├── TestProduct.Subscription.csproj
│   ├── TestSubscriptionPlugin.cs
│   └── README.md
├── TestProduct.Freemium/
│   ├── TestProduct.Freemium.csproj
│   ├── TestFreemiumPlugin.cs
│   └── README.md
├── TestProduct.Licensed/
│   ├── TestProduct.Licensed.csproj
│   ├── TestLicensedPlugin.cs
│   ├── PackageContents.xml
│   └── README.md
└── TestProduct.Analytics/
    ├── TestProduct.Analytics.csproj
    ├── TestAnalyticsPlugin.cs
    ├── PackageContents.xml
    └── README.md
```

## 🔗 Ссылки

- [GrossGeo SDK Stub](../src/SDK/GrossGeo.SDK.Stub/README.md)
- [Manual Testing Guide](../docs/testing/manual-testing-guide.md)

## 🚀 Тестирование

### 1. Создайте продукты в Developer Portal

1. Войдите в Developer Portal под учётной записью разработчика.
2. Создайте два продукта:
   - **Test Product Licensed** (Distribution: Licensed)
   - **Test Product Analytics** (Distribution: Analytics/Free)

### 2. Загрузите релизы

1. Для каждого продукта создайте релиз v1.0.0
2. Загрузите соответствующий ZIP-файл

### 3. Установите через User Panel

1. Запустите User Panel
2. Найдите продукты в каталоге
3. Установите оба продукта

### 4. Тестируйте в AutoCAD

```
# Загрузка (если не autoloader)
NETLOAD → выберите DLL

# TestProduct.Licensed — команды:
TEST_LICENSE_INFO      — информация о лицензии
TEST_PROTECTED_CMD     — защищённая команда
TEST_FEATURE_CHECK     — проверка features
TEST_CHECK_UPDATE      — проверка обновлений
TEST_LICENSE_RECHECK   — повторная проверка
TEST_LICENSED_HELP     — справка

# TestProduct.Analytics — команды:
TEST_ANALYTICS_INFO    — информация о SDK
TEST_TRACK_FEATURE     — трекинг с параметрами
TEST_CUSTOM_EVENT      — кастомные события
TEST_ANALYTICS_UPDATE  — проверка обновлений
TEST_OPEN_PRODUCT_PAGE — открыть страницу продукта
TEST_SESSION_STATS     — статистика сессии
TEST_ANALYTICS_HELP    — справка
```

## 🔌 Multi-plugin паттерн (v2.1.0)

Все примеры используют `ProductLicenseAccessor` для изоляции лицензий между плагинами:

```csharp
private static ProductLicenseAccessor? _license;

// В Initialize:
var result = await GrossGeoLicense.Initialize(new LicenseOptions { ProductKey = ProductKey, ... });
_license = GrossGeoLicense.ForProduct(ProductKey);

// В командах:
_license?.Protect(() => DoWork(), () => ShowUpgrade());
_license?.RequireFeature("export", () => DoExport());

// В Terminate:
GrossGeoLicense.Shutdown(ProductKey);
```

**Почему?** Когда несколько плагинов работают в одном AutoCAD, статические свойства
`GrossGeoLicense.IsValid` и т.д. возвращают данные последнего инициализированного продукта.
`ProductLicenseAccessor` решает эту проблему.

## ⚠️ Важно

### Product Keys

В коде используются тестовые ключи:
- `TEST-LICENSED-PRODUCT-KEY`
- `TEST-ANALYTICS-PRODUCT-KEY`

**Замените их на реальные ключи из Developer Portal** после создания продуктов!

### Пути к AutoCAD

Проекты настроены на стандартные пути:
- AutoCAD 2024: `C:\Program Files\Autodesk\AutoCAD 2024\`
- AutoCAD 2025: `C:\Program Files\Autodesk\AutoCAD 2025\`

Если пути отличаются — отредактируйте `.csproj` файлы.

## 📋 Чек-лист тестирования

### Дистрибуция
- [ ] Создание продукта в Developer Portal
- [ ] Загрузка релиза (ZIP bundle)
- [ ] Отображение в каталоге User Panel
- [ ] Установка продукта
- [ ] Автозагрузка в AutoCAD (bundle)
- [ ] Обновление продукта

### GrossGeo.SDK (Licensed)
- [ ] Инициализация при загрузке плагина
- [ ] Проверка лицензии
- [ ] Защита команд (Protect)
- [ ] Feature flags
- [ ] Grace period
- [ ] Offline mode
- [ ] Проверка обновлений

### GrossGeo.SDK.Stub (Analytics)
- [ ] Инициализация
- [ ] TrackLaunch (автоматический)
- [ ] TrackFeatureUsage
- [ ] TrackEvent (кастомные события)
- [ ] CheckForUpdates
- [ ] ShowProductPage

### IPC
- [ ] Связь SDK ↔ User Panel
- [ ] Буферизация при отключённом User Panel
- [ ] Переподключение при восстановлении связи
