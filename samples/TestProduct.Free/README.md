# 🆓 TestProduct.Free

**Тип лицензирования:** FREE (полностью бесплатный)

Демонстрационный плагин для тестирования сценария бесплатного продукта без лицензирования.

## 📋 Характеристики

| Параметр | Значение |
|----------|----------|
| Product ID | `cccccccc-cccc-cccc-cccc-cccccccccccc` |
| API Key | `GG-FREE-TEST-0001` |
| Монетизация | Free |
| Лицензия | Не требуется |
| DistributionMode | Platform |

## 🎯 Назначение

Тестирование сценария, когда продукт:
- Полностью бесплатен
- Работает без проверки лицензии
- SDK инициализируется, но `IsValid = true` всегда

## 🔧 Команды AutoCAD

| Команда | Описание |
|---------|----------|
| `GGFREETEST` | Базовая команда (всегда работает) |
| `GGFREEINFO` | Информация о статусе SDK |

## 🧪 Тестовые сценарии

### 1. Установка и запуск
```
1. Загрузить плагин в AutoCAD: NETLOAD → TestProduct.Free.dll
2. Выполнить команду GGFREETEST
3. Ожидается: команда выполняется без проверки лицензии
```

### 2. Проверка SDK
```csharp
// Инициализация
var result = await GrossGeoLicense.Initialize(new LicenseOptions 
{ 
    ProductKey = "GG-FREE-TEST-0001" 
});

// Ожидаемый результат:
// result.IsValid = true (FREE продукт)
// result.PlanTier = PlanTier.Free
// result.BillingModel = BillingModel.Free
```

### 3. API проверка
```
GET /api/user/licenses/status/cccccccc-cccc-cccc-cccc-cccccccccccc
Authorization: Bearer {token}

Ожидается:
{
  "isFreeProduct": true,
  "hasLicense": false,
  "licenseRequired": false
}
```

## 📦 Сборка

```bash
cd Samples\TestProduct.Free
dotnet build --configuration Release
```

Выходной файл: `bin\Release\net8.0-windows\TestProduct.Free.dll`

## 📁 Структура

```
TestProduct.Free/
├── TestProduct.Free.csproj    # Проект
├── TestFreePlugin.cs          # Код плагина
└── README.md                  # Этот файл
```
