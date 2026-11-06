# Облачные настройки продукта — руководство (предварительное)

**Дата:** Ноябрь 2026
**Для кого:** Разработчики AutoCAD-плагинов, которые хотят хранить настройки своего продукта у участника в облаке GrossGeo и видеть их одинаковыми на всех его машинах.

> **Предварительно: появится в SDK 2.4.0 (ожидается конец ноября 2026) вместе с обновлением User Panel. В 2.3.0 этих методов нет; выпускать сборку на 2.4.0 — только после выхода панелей с ним.**

> См. также: [Руководство разработчика SDK](grossgeo-sdk-developer-guide.md), [Быстрый старт](grossgeo-sdk-quickstart.md).
>
> Телеметрия и соглашение (правила содержания настроек) — отдельными руководствами позже.

---

## 1. Что это

Облачные настройки — хранилище документов «ключ → содержимое» (произвольные байты), которое платформа держит для каждого продукта. Плагин кладёт туда, например, конфигурацию интерфейса, пресеты, серверный адрес своего сервиса — участник получает те же настройки на другой машине сразу после входа в User Panel.

Две главные черты:

- **Local-first.** Чтение и запись работают без сети. `Get` читает локальный кэш машины; `Put`/`Delete` кладут запись в локальную очередь на диске. Всю сеть делает **User Panel**: она отправляет очередь на сервер и подтягивает обновления в кэш машины. Сам SDK сетевых обращений не выполняет — так же, как и проверка лицензии, настройки идут через панель.
- **Не бросает.** Все методы отвечают результатом с полем `Outcome` — ошибки приходят кодами результата, а не через `try/catch`.

## 2. Пространства (scope)

Операция адресует одно из трёх пространств — `CloudSettingsScope`:

| Scope | Чьё пространство | Кто пишет |
|-------|------------------|-----------|
| `Product` | «Своё» — личное пространство продукта у участника | Сам участник |
| `Studio` | «Своё» — общее пространство студии, видно всем её продуктам участника | Сам участник |
| `Organization` | Общие объекты компании (бизнес-аккаунт × студия) — «стандарт организации» | Только администратор или владелец бизнес-аккаунта |

Право в пространстве возвращает `GetAccess(scope)` (`CloudSettingsAccess`):

- `Product`/`Studio` — `ReadWrite` при действующей привязке продукта к участнику, иначе `None`;
- `Organization` — `ReadWrite`, если участник администратор или владелец бизнес-аккаунта; `Read` — обычный сотрудник; `None` — членства нет.

Активная организация — `ActiveOrganization` (`CloudSettingsOrganization`: `AccountId`, `Name`, `Access`). `null` значит: членства нет, либо у участника два и более бизнес-аккаунтов и он ещё не выбрал активный в панели. Запись в `Organization` без активной организации или с правом только чтение отвечает `Forbidden`.

## 3. Точка входа

```csharp
using GrossGeo.SDK.Settings;

// Один клиент на productKey. Не бросает; лицензия и Initialize не нужны —
// можно вызывать до GrossGeoLicense.Initialize и без него.
private static readonly CloudSettingsClient Settings = GrossGeoCloudSettings.For(ProductKey);
```

`For` не проверяет ключ заранее и не бросает: с пустым ключом вернётся клиент, который ведёт себя как обычный (пока участник не вошёл — `Unavailable`). Валидность ключа — на вашей стороне.

## 4. Состояние облака

```csharp
CloudSettingsAvailability a = Settings.Availability;
```

| Значение | Что значит | Что доступно |
|----------|-----------|--------------|
| `Unknown` | Облако ещё не проверялось | — |
| `Ready` | Участник вошёл, панель жива (сердцебиение свежее, порог 60 с), продукт привязан | Чтение и запись |
| `PanelNotRunning` | Участник известен, панель не запущена или молчит дольше 60 с | Чтение из кэша; запись — в очередь (`Queued`) |
| `SignedOut` | Участник вышел из панели | Отказ на всё (`Unavailable`) — и чтение, и запись |
| `NotBound` | Панель ещё не привязала продукт | Чтение — `Unavailable` (кэша нет); запись в `Product` уйдёт в очередь |
| `Unavailable` | Облака нет: участник ни разу не входил на машине, панель старше облачных настроек или участник вышел | Отказ на всё |

Свойство вычисляется при каждом обращении и смотрит только в локальные файлы — сети нет и блокировок нет.

## 5. Запись, чтение, удаление

Документ адресуется парой «пространство + ключ». Содержимое — произвольные байты (`byte[]`).

### Запись

```csharp
CloudSettingsWriteResult put = Settings.Put(
    CloudSettingsScope.Product,
    "ui/layout.json",
    content,                 // byte[]? — содержимое документа
    expectedVersion: CloudSettingsVersion.LastSeenByThisMachine);
```

`expectedVersion` — оптимистичная блокировка: «писать поверх версии, которую я видел». Особое значение `CloudSettingsVersion.LastSeenByThisMachine` (`-1`): SDK сам подставит последнюю версию, виденную **этой машиной** (она хранится в общем кэше участника, поэтому несколько GrossGeo-продуктов на одной машине видят одну версию ключа и не сыплют друг другу `Conflict`). `null` — не проверять версию вовсе. Удаление:

```csharp
CloudSettingsWriteResult del = Settings.Delete(CloudSettingsScope.Product, "ui/layout.json");
```

`Delete` оставляет в облаке надгробие: после него `Get` отвечает `Deleted` (метаданные без содержимого), и документ не «воскреснет» от старой отложенной записи.

Оба метода не бросают. `Put` с `content = null` отвечает `Failed` (для пустого документа пишите пустой массив `Array.Empty<byte>()`).

### Чтение

```csharp
CloudSettingsReadResult read = Settings.Get(CloudSettingsScope.Product, "ui/layout.json");
if (read.Outcome == CloudSettingsReadOutcome.Found)
{
    byte[] content = read.Document!.Content!; // содержимое из кэша машины
}
```

`Get` читает только локальный кэш машины — быстрые локальные файлы, без сети и IPC-вызова панели. Свежесть кэша обеспечивает панель: она подтягивает изменения облака, пока работает.

### Курсор изменений пространства

```csharp
CloudSettingsChangesResult changes = Settings.Changes(CloudSettingsScope.Product, sinceVersion: 0);
if (changes.Outcome == CloudSettingsChangesOutcome.Ok)
{
    foreach (CloudSettingsDocument doc in changes.Items)
    {
        // doc.Key, doc.Version, doc.State; содержимого в перечне нет (doc.Content == null) — берите через Get
    }
    long next = changes.SpaceVersion; // следующий запрос — Changes(scope, next)
}
```

`sinceVersion: 0` — полный перечень ключей пространства. Дальше — только ключи с версией больше курсора.

## 6. События

```csharp
Settings.Changed += (s, e) =>
{
    // e.Kind, e.Key (null — событие про всё пространство), e.Scope,
    // e.Resolution (при ConflictResolved), e.RejectionCode (при UploadRejected)
};
```

| `CloudSettingsChangeKind` | Когда приходит |
|---------------------------|----------------|
| `RemoteUpdate` | Облачная версия ключа изменилась — перечитайте через `Get` |
| `ConflictDetected` | Открылся конфликт: облачная и местная версии различаются, вопрос ждёт участника |
| `ConflictResolved` | Участник решил конфликт в панели; `e.Resolution`: `TakeCloud` (перепишите свой файл из `Get`) или `KeepLocal` (местная версия уже ушла в облако) |
| `UploadRejected` | Отправка с этой машины отвергнута сервером; код — `e.RejectionCode` |
| `SpaceReset` | Пространство сброшено (новый Epoch): кэш машины очищен; перечитайте пространство с `sinceVersion: 0` |
| `AvailabilityChanged` | Облако стало доступно или перестало быть доступно |

Событие приходит с потока пула потоков. Как и с `SessionExpired`: AutoCAD API — только через главный поток, маршалинг на вашей стороне (см. раздел о потоках в руководстве разработчика).

## 7. Конфликты: `AskConflict`

Когда `Put` ответил `Conflict` (в `put.Current` — актуальная версия), а вам нужно решение участника, передайте спор панели:

```csharp
if (put.Outcome == CloudSettingsWriteOutcome.Conflict)
{
    Settings.AskConflict(
        CloudSettingsScope.Product,
        "ui/layout.json",
        localContent: myLocalBytes,      // местная версия
        cloudVersion: put.Current!.Value,
        differences: new[] { new CloudSettingsFieldDifference("путь.к.полю", localValue, cloudValue) });
}
```

Панель покажет участнику обе версии и различия полей; решение приходит событием `ConflictResolved`. Различий может быть не больше 200 и не больше 64 КиБ полезной нагрузки — лишнее отсекается (кроме первого различия и имён полей), список допустим `null`; конструктор `CloudSettingsFieldDifference` требует непустое `field`.

## 8. Пределы

| Предел | Значение |
|--------|----------|
| Размер документа | не больше **5 МиБ**; больший документ в очередь не пишется (`TooLarge`) |
| Длина ключа | 1–256 символов после NFC-нормализации |
| Символы ключа | разделитель `/`; нельзя: ведущий `/`, `\`, `:`, `*`, `?`, `"`, `<`, `>`, `|`, управляющие символы, пустые сегменты, сегменты `.` и `..`, точка или пробел на конце сегмента |
| `AskConflict` | не больше 200 различий и 64 КиБ полезной нагрузки |

Ключ, не прошедший правило, отвечает `InvalidKey` — и запись, и чтение. Имя ключа — ваш формат: примеры выше используют `ui/layout.json`, но расширение не требуется.

## 9. Отказы, которые видит продукт

Через `CloudSettingsWriteResult.Outcome` (`Put`/`Delete`/`AskConflict`):

| Outcome | Когда | Что делать |
|---------|-------|-----------|
| `Queued` | Запись легла в локальную очередь | Отправит панель, когда сможет |
| `Unchanged` | Содержимое байт в байт совпадает с облачным | Ничего |
| `Conflict` | Ожидаемая версия не совпала с виденной машиной | Актуальная версия — `put.Current`; решайте: перечитать, переписать поверх или `AskConflict` |
| `Unavailable` | Нет участника (не входил, вышел, панель старше облачных настроек) или пространство не адресуемо | Повторить позже |
| `Forbidden` | В пространство организации писать нельзя: нет активной организации или право только чтение | Не повторять вслепую: право даёт `GetAccess` |
| `InvalidKey` | Ключ не проходит правило ключа (§8) | Исправить ключ |
| `TooLarge` | Документ больше 5 МиБ | Уменьшить документ |
| `Failed` | Локальный сбой записи очереди (диск и т.п.) | Повторить позже |

Через `CloudSettingsReadResult.Outcome` (`Get`): `Found`, `NotFound` (ключа в кэше нет), `Deleted` (надгробие), `Unavailable` (кэша нет: облако недоступно, участник не входил, продукт не привязан), `InvalidKey`.

Отдельно — отказ сервера после того, как запись уже ушла с машины: событие `UploadRejected` с `e.RejectionCode` (конкретные коды — сторона сервера, SDK их не задаёт). Документ в перечнях при этом имеет `State = Rejected` и `RejectionCode`. У записи поля `Message` — пояснение для диагностики плагина, не для показа участнику.

## 10. Поведение со старой панелью

Поддержка облачных настроек панель объявляет в своём файле состояния (признак `cloudSettings:1`). Панель без этого признака — старше облачных настроек — для SDK «облака нет»:

- `Availability` отвечает `Unavailable`;
- `Get` — `Unavailable` (кэш не завёлся);
- `Put`/`Delete`/`AskConflict` — `Unavailable`, в очередь ничего не попадает;
- события не приходят.

Плагину ничего проверять не надо: пока панель не объявила поддержку, все методы честно отвечают `Unavailable`. Отдельный случай — участник в панели не входил: тот же `Unavailable`. После выхода участника запись тоже не копится в очереди — всё с тем же `Unavailable`.

## 11. Пример

```csharp
using GrossGeo.SDK.Settings;

public sealed class ProductSettings
{
    private static readonly CloudSettingsClient Settings = GrossGeoCloudSettings.For(ProductKey);

    public void Save(byte[] content)
    {
        var put = Settings.Put(
            CloudSettingsScope.Product,
            "ui/layout.json",
            content,
            expectedVersion: CloudSettingsVersion.LastSeenByThisMachine);

        switch (put.Outcome)
        {
            case CloudSettingsWriteOutcome.Queued:
            case CloudSettingsWriteOutcome.Unchanged:
                break; // отправит панель

            case CloudSettingsWriteOutcome.Conflict:
                // Возможен при явном expectedVersion; с LastSeenByThisMachine — нет.
                // put.Current — версия, которую видит машина; спор — участнику:
                Settings.AskConflict(CloudSettingsScope.Product, "ui/layout.json",
                    localContent: content, cloudVersion: put.Current!.Value, differences: null);
                break;

            case CloudSettingsWriteOutcome.TooLarge:
            case CloudSettingsWriteOutcome.InvalidKey:
                // ваша ошибка — исправить документ или ключ
                break;

            default:
                break; // Unavailable / Forbidden / Failed — повторить позже
        }
    }

    public byte[]? Load()
    {
        var read = Settings.Get(CloudSettingsScope.Product, "ui/layout.json");
        return read.Outcome == CloudSettingsReadOutcome.Found ? read.Document!.Content : null;
    }
}
```

Подписи сверены с кодом SDK ветки 2.4.0 на ноябрь 2026. Формулировки про панель и сервер — по тому же состоянию; до выпуска уточняйте по финальному руководству 2.4.0.
