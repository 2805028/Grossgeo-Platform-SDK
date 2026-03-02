# 💎 TestProduct.Licensed

**Тип лицензирования:** PAID (разовая покупка, бессрочная лицензия)

Демонстрационный плагин для тестирования модели One-Time Purchase с Perpetual лицензией.

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| Product ID | `eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee` |
| API Key | `GG-PAID-TEST-0003` |
| Монетизация | Paid (One-Time) |
| Цена | 4990₽ |
| Лицензия | Бессрочная (Perpetual) |
| Машин на лицензию | 2 |

## 🎯 Назначение

Тестирование сценариев:
- Продукт требует покупки для использования
- Лицензия без срока действия
- Привязка к машинам
- Grace period при офлайн работе

## 🔧 Команды AutoCAD

| Команда | Описание | Требования |
|---------|----------|------------|
| `TEST_LICENSE_INFO` | Полная информация о лицензии | — |
| `TEST_PROTECTED_CMD` | Защищённая команда | Лицензия |
| `TEST_FEATURE_CHECK` | Проверка features | — |
| `TEST_CHECK_UPDATE` | Проверка обновлений | — |
| `TEST_LICENSE_RECHECK` | Повторная проверка | — |
| `TEST_DIAG` | Диагностика системы | — |

## 🧪 Тестовые сценарии

### 1. До покупки — блокировка
```csharp
var result = await GrossGeoLicense.Initialize(new LicenseOptions 
{ 
    ProductKey = "GG-PAID-TEST-0003" 
});

// Ожидается:
// result.IsValid = false
// result.PlanTier = PlanTier.Free
// result.Status = LicenseCheckStatus.Blocked
```

```
Команда: TEST_PROTECTED_CMD
Вывод: ❌ Лицензия недействительна! Для использования требуется лицензия.
```

### 2. API проверка до покупки
```
GET /api/user/licenses/status/eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee
Authorization: Bearer {token}

Ожидается:
{
  "hasLicense": false,
  "price": 4990,
  "currency": "RUB",
  "billingModel": "Perpetual",
  "planTier": "Pro"
}
```

### 3. После покупки — активация
```csharp
// После покупки и активации лицензии:
// result.IsValid = true
// result.PlanTier = PlanTier.Pro
// result.BillingModel = BillingModel.Perpetual
// result.ExpiresAt = null (бессрочная!)
```

```
Команда: TEST_PROTECTED_CMD
Вывод: ✅ Лицензия валидна! Команда успешно выполнена!
```

### 4. Привязка машины
```
POST /api/user/licenses/{licenseId}/bind-machine
Authorization: Bearer {token}
Content-Type: application/json

{
  "machineId": "MACHINE-FINGERPRINT-123",
  "machineName": "DESKTOP-WORK"
}

Ожидается:
{
  "success": true,
  "machinesUsed": 1,
  "machinesLimit": 2
}
```

### 5. Grace Period (офлайн)
```
1. Инициализировать SDK с активной лицензией
2. Отключить интернет / остановить User Panel
3. Перезапустить плагин

Ожидается (в течение 7 дней):
- IsValid = true
- IsOfflineMode = true
- IsInGracePeriod = true

После 7 дней:
- IsValid = false
- Status = "GRACE_PERIOD_EXPIRED"
```

### 6. Диагностика
```
Команда: TEST_DIAG

Вывод:
╔══════════════════════════════════════════════════════════════╗
║          TEST_DIAG — Диагностика системы                     ║
╚══════════════════════════════════════════════════════════════╝

[PATHS]
  LocalAppData:     C:\Users\...\AppData\Local
  Security Dir:     ...\GrossGeo\Security
  Security Exists:  True

[TOKEN FILES]
  auth.token:           EXISTS
  user/tokens.dat:      EXISTS

[USER PANEL]
  Named Pipe:       RUNNING
  Process Count:    1

[RECOMMENDATION]
  ✓ Токен и User Panel найдены
```

## 📦 Сборка

```bash
cd Samples\TestProduct.Licensed
dotnet build --configuration Release
```

## 📁 Структура

```
TestProduct.Licensed/
├── TestProduct.Licensed.csproj
├── TestLicensedPlugin.cs
├── PackageContents.xml        # Manifest для AutoCAD
└── README.md
```

## 💡 Подсказки

- Используйте `LicenseGuard.OrThrow()` для критичных функций
- Показывайте пользователю причину блокировки
- Реализуйте fallback для демо-режима если нужно
- Логируйте попытки использования без лицензии для аналитики
