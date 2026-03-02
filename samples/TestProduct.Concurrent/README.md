# 🔁 TestProduct.Concurrent

**Тип лицензирования:** Multi-tier (Free / Pro / Pro+) + Concurrent Sessions

Демонстрационный плагин для тестирования полного набора v2 возможностей:
concurrent лицензирование с heartbeat, multi-tier планы, feature limits, публичные фичи.

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| Product ID | `aabbccdd-1122-3344-5566-778899aabbcc` |
| API Key | `GG-CONC-TEST-0006` |
| Монетизация | Subscription |
| Trial | 7 дней |

## 🎯 Назначение

Тестирование v2 сценариев, которые **не покрыты** другими тестовыми продуктами:
- ✅ **Concurrent Sessions** — AcquireSession / ReleaseSession / Heartbeat
- ✅ **Multi-tier планы** — Free → Pro → Pro+
- ✅ **Feature Limits** — GetFeatureLimit, CheckLimit, RequireLimitOrThrow
- ✅ **Public Features** — IsPublic=true фичи для Guest
- ✅ **SessionExpired** — событие потери сессии
- ✅ **v2 Properties** — PlanTier, BillingModel, LicenseMode

## 📊 Планы и Features

### Тарифные планы

| План | Цена | LicenseMode | MaxSeats |
|------|------|-------------|----------|
| Free | 0₽ | — | 1 |
| Pro | 800₽/мес | User | 3 |
| Pro+ | 1500₽/мес | Concurrent | 10 |

### Features по планам

| Feature | Free | Pro | Pro+ | IsPublic |
|---------|------|-----|------|----------|
| `view-objects` | ✅ | ✅ | ✅ | ✅ (Guest) |
| `basic-tools` | ✅ | ✅ | ✅ | ❌ |
| `pro-tools` | ❌ | ✅ | ✅ | ❌ |
| `export-batch` | ❌ | ✅ | ✅ | ❌ |
| `enterprise-api` | ❌ | ❌ | ✅ | ❌ |
| `cloud-sync` | ❌ | ❌ | ✅ | ❌ |

## 🔧 Команды AutoCAD

### Информация

| Команда | Описание |
|---------|----------|
| `GGCONCINFO` | Полная информация: лицензия, сессия, features, limits |
| `GGCONCHELP` | Справка по командам |

### Concurrent Sessions (v2)

| Команда | Описание |
|---------|----------|
| `GGCONCSESSION` | Получить сессию / отправить heartbeat |
| `GGCONCSESSIONRELEASE` | Освободить concurrent-сессию |

### Features & Limits

| Команда | Описание | Min Plan |
|---------|----------|----------|
| `GGCONCPUBLIC` | Демо публичных фичей (IsPublic) | Guest |
| `GGCONCPRO` | Pro инструменты | Pro |
| `GGCONCBATCH` | Пакетный экспорт + Limits | Pro |
| `GGCONCLIMITS` | Обзор Feature Limits | Free |

## 🧪 Тестовые сценарии

### 1. Concurrent Session Lifecycle
```
1. GGCONCINFO → проверяем LicenseMode=Concurrent
2. GGCONCSESSION → получаем сессию (AcquireSessionAsync)
3. GGCONCSESSION → heartbeat (SendSessionHeartbeatAsync)
4. GGCONCSESSIONRELEASE → освобождаем (ReleaseSessionAsync)
5. GGCONCSESSION → получаем новую сессию
```

### 2. Multi-tier Upgrade
```
1. Free tier: GGCONCPRO → 🔒 заблокировано
2. Обновиться до Pro → GGCONCPRO → ✅ работает
3. GGCONCBATCH → ✅ + проверка лимита
4. Обновиться до Pro+ → enterprise-api доступен
```

### 3. Public Features (Guest)
```
1. Не авторизоваться → GGCONCPUBLIC
2. view-objects: HasFeature=true (IsPublic)
3. basic-tools: HasFeature=false (требует авторизацию)
```

### 4. Feature Limits
```
1. GGCONCLIMITS → просмотр лимитов
2. GGCONCBATCH → пакетный экспорт с проверкой maxPerCall
3. При превышении → LimitExceededException
```

### 5. Session Expiry
```
1. Получить сессию (GGCONCSESSION)
2. Закрыть User Panel (симулирует потерю heartbeat)
3. Через ~5 мин → SessionExpired event
4. GGCONCSESSION → получить новую сессию
```

## 🔗 v2 SDK API (покрытие)

| API | Покрыт | Команда |
|-----|--------|---------|
| `GrossGeoLicense.PlanTier` | ✅ | GGCONCINFO |
| `GrossGeoLicense.BillingModel` | ✅ | GGCONCINFO |
| `GrossGeoLicense.LicenseMode` | ✅ | GGCONCINFO |
| `GrossGeoLicense.HasActiveSession` | ✅ | GGCONCSESSION |
| `GrossGeoLicense.SessionToken` | ✅ | GGCONCINFO |
| `GrossGeoLicense.SessionExpiresAt` | ✅ | GGCONCINFO |
| `GrossGeoLicense.AcquireSessionAsync` | ✅ | GGCONCSESSION |
| `GrossGeoLicense.ReleaseSessionAsync` | ✅ | GGCONCSESSIONRELEASE |
| `GrossGeoLicense.SendSessionHeartbeatAsync` | ✅ | GGCONCSESSION |
| `GrossGeoLicense.SessionExpired` | ✅ | OnSessionExpired |
| `GrossGeoLicense.HasFeature` | ✅ | GGCONCPUBLIC |
| `GrossGeoLicense.IsPublicFeature` | ✅ | GGCONCPUBLIC |
| `GrossGeoLicense.GetFeatureLimit` | ✅ | GGCONCLIMITS |
| `GrossGeoLicense.CheckLimit` | ✅ | GGCONCLIMITS |
| `GrossGeoLicense.RequireLimitOrThrow` | ✅ | GGCONCBATCH |
| `FeatureGuard.Require` | ✅ | GGCONCPRO, GGCONCBATCH |
| `LimitExceededException` | ✅ | GGCONCBATCH |
