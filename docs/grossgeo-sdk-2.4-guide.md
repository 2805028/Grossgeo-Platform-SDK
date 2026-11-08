# GrossGeo SDK 2.4 — облачные настройки, счётчики команд, соглашения и согласие

**Дата:** Ноябрь 2026
**Для кого:** Разработчики AutoCAD-плагинов на `GrossGeo.SDK.Stub` 2.4.0, которые хотят хранить настройки продукта в облаке участника, собирать анонимную статистику команд или показывать участнику соглашение.

> См. также: [Руководство разработчика SDK](grossgeo-sdk-developer-guide.md), [Быстрый старт](grossgeo-sdk-quickstart.md).

```xml
<PackageReference Include="GrossGeo.SDK.Stub" Version="2.4.0" />
```

SDK 2.4.0 добавляет три пространства имён:

- [Облачные настройки](#облачные-настройки) — `GrossGeo.SDK.Settings`;
- [Счётчики команд](#счётчики-команд) — `GrossGeo.SDK.Telemetry`;
- [Соглашения и согласие](#соглашения-и-согласие) — `GrossGeo.SDK.Legal`.

Общее у всех трёх:

- **Только локально.** Методы читают и пишут файлы на машине участника; сеть и канал лицензии они не трогают. На сервер данные отправляет **GrossGeo User Panel** (2.4.0 и новее), когда она запущена и участник вошёл.
- **Не бросают.** Итог — поле `Outcome` результата, а не исключение.
- **`Initialize` и лицензия не нужны.** Вызывать можно до `GrossGeoLicense.Initialize` и без него.
- **net48 и net8** — один публичный API.
- События (`Changed`) приходят **с потока пула**: AutoCAD API — только через главный поток, маршалинг на вашей стороне (см. раздел о потоках в руководстве разработчика).

---

## Облачные настройки

Хранилище документов «ключ → содержимое» (произвольные байты), которое платформа держит для продукта у участника. Плагин кладёт туда, например, конфигурацию интерфейса или пресеты — участник получает те же настройки на другой машине после входа в User Panel.

- **Local-first.** `Get` читает локальный кэш машины (бюджет — до 50 мс, только локальные файлы); `Put`/`Delete` кладут запись в локальную очередь. Очередь отправляет и кэш обновляет User Panel.
- **Не бросает.** Ошибки приходят кодами результата.

### Пространства (scope)

Операция адресует одно из трёх пространств — `CloudSettingsScope`:

| Scope | Чьё пространство | Кто пишет |
|-------|------------------|-----------|
| `Product` | Личное пространство продукта у участника | Сам участник |
| `Studio` | Общее пространство студии — видно всем её продуктам у участника | Сам участник |
| `Organization` | «Стандарт организации» (бизнес-аккаунт × студия) | Только администратор или владелец бизнес-аккаунта |

Право возвращает `GetAccess(scope)` (`CloudSettingsAccess`):

- `Product`/`Studio` — `ReadWrite` при действующей привязке продукта, иначе `None`;
- `Organization` — `ReadWrite` у администратора или владельца бизнес-аккаунта, `Read` у сотрудника, `None` — членства нет.

Активная организация — `ActiveOrganization` (`CloudSettingsOrganization`: `AccountId`, `Name`, `Access`). `null` — членства нет, либо у участника два и более бизнес-аккаунтов и он ещё не выбрал активный в панели. Запись в `Organization` без активной организации или с правом только чтения отвечает `Forbidden`.

### Точка входа

```csharp
using GrossGeo.SDK.Settings;

// Один клиент на productKey: повторный For с тем же ключом возвращает тот же экземпляр.
private static readonly CloudSettingsClient Settings = GrossGeoCloudSettings.For(ProductKey);
```

`For` не проверяет ключ и не бросает. С пустым ключом клиент возвращается, но продукт с ним никогда не будет привязан: после входа участника `Availability` отвечает `NotBound`. Передавайте настоящий ключ продукта.

### Состояние облака

```csharp
CloudSettingsAvailability a = Settings.Availability;
```

| Значение | Что значит | Что доступно |
|----------|-----------|--------------|
| `Unknown` | Облако ещё не проверялось | — |
| `Ready` | Участник вошёл, панель жива (отметка панели не старше 60 с), продукт привязан | Чтение; запись — в очередь (`Queued`), её отправит панель |
| `PanelNotRunning` | Участник известен, панель не запущена или молчит дольше 60 с | Чтение из кэша; запись — в очередь (`Queued`) |
| `SignedOut` | Участник вышел из панели | Отказ на чтение и запись (`Unavailable`) |
| `NotBound` | Панель ещё не привязала продукт | Чтение — `Unavailable` (кэша нет); запись в `Product` уйдёт в очередь, в `Studio` и `Organization` — `Unavailable` |
| `Unavailable` | Облака нет: участник ни разу не входил на машине или панель старше облачных настроек | Отказ на всё |

Свойство вычисляется при каждом обращении и смотрит только в локальные файлы.

### Запись, чтение, удаление

Документ адресуется парой «пространство + ключ», содержимое — `byte[]`.

```csharp
CloudSettingsWriteResult put = Settings.Put(
    CloudSettingsScope.Product,
    "ui/layout.json",
    content,                                              // byte[]
    expectedVersion: CloudSettingsVersion.LastSeenByThisMachine);

CloudSettingsWriteResult del = Settings.Delete(CloudSettingsScope.Product, "ui/layout.json");
```

`expectedVersion` — оптимистичная блокировка «писать поверх версии, которую я видел»:

- `null` (по умолчанию) — версию не проверять;
- `0` — только если ключа нет (или он удалён);
- `CloudSettingsVersion.LastSeenByThisMachine` (`-1`) — SDK подставит последнюю версию ключа, виденную **этой машиной**. Она хранится в общем кэше участника, поэтому несколько продуктов GrossGeo на одной машине видят одну версию ключа и не получают `Conflict` друг от друга;
- другое число — точное совпадение с версией в кэше машины, иначе `Conflict` (актуальная версия — `put.Current`).

Содержимое, байт в байт совпадающее с облачным, отвечает `Unchanged` (раньше проверки версии). `Put` с `content = null` отвечает `Failed` — для пустого документа пишите `Array.Empty<byte>()`. `Delete` оставляет в облаке надгробие: после него `Get` отвечает `Deleted` (метаданные без содержимого).

```csharp
CloudSettingsReadResult read = Settings.Get(CloudSettingsScope.Product, "ui/layout.json");
if (read.Outcome == CloudSettingsReadOutcome.Found)
{
    byte[] content = read.Document!.Content!;
}
```

`CloudSettingsDocument`: `Key`, `Content`, `Version`, `State` (`Synced` / `Conflict` / `Rejected`), `RejectionCode`, `UpdatedAtUtc`, `DeviceName`, `HasPendingUpload` (запись этой машины ещё в очереди).

### Курсор изменений

```csharp
CloudSettingsChangesResult changes = Settings.Changes(CloudSettingsScope.Product, sinceVersion: 0);
if (changes.Outcome == CloudSettingsChangesOutcome.Ok)
{
    foreach (CloudSettingsDocument doc in changes.Items)
    {
        // doc.Key, doc.Version, doc.State; содержимого в перечне нет — берите через Get
    }
    long next = changes.SpaceVersion; // следующий запрос — Changes(scope, next)
}
```

`sinceVersion: 0` — полный перечень ключей пространства, дальше — только ключи с версией больше курсора. `NotAddressable` — пространство не адресуемо (продукт не привязан, организация не активна), `Unavailable` — кэша нет.

### События

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
| `ConflictDetected` | Облачная и местная версии различаются, вопрос ждёт участника |
| `ConflictResolved` | Участник решил конфликт в панели; `e.Resolution`: `TakeCloud` (перепишите свой файл из `Get`) или `KeepLocal` (местная версия уже ушла в облако) |
| `UploadRejected` | Отправка с этой машины отвергнута сервером; код — `e.RejectionCode` |
| `SpaceReset` | Пространство сброшено, кэш машины очищен — перечитайте пространство с `sinceVersion: 0` |
| `AvailabilityChanged` | Облако стало доступно или перестало быть доступно |

### Конфликты: `AskConflict`

Когда `Put` ответил `Conflict`, а нужно решение участника, передайте спор панели — она покажет обе версии и различия полей; решение придёт событием `ConflictResolved`:

```csharp
if (put.Outcome == CloudSettingsWriteOutcome.Conflict)
{
    Settings.AskConflict(
        CloudSettingsScope.Product,
        "ui/layout.json",
        localContent: myLocalBytes,
        cloudVersion: put.Current!.Value,
        differences: new[] { new CloudSettingsFieldDifference("путь.к.полю", localValue, cloudValue) });
}
```

Различий — не больше 200 и не больше 64 КиБ (первое различие передаётся всегда, лишнее отсекается); список может быть `null`. Конструктор `CloudSettingsFieldDifference` бросает `ArgumentNullException` при `field = null`.

### Пределы

| Предел | Значение |
|--------|----------|
| Размер документа | не больше **5 МиБ**; больший в очередь не пишется (`TooLarge`) |
| Длина ключа | 1–256 символов после NFC-нормализации |
| Символы ключа | разделитель `/`; нельзя: ведущий `/`, `\`, `:`, `*`, `?`, `"`, `<`, `>`, `\|`, управляющие символы, пустые сегменты, сегменты `.` и `..`, точка или пробел на конце сегмента |
| Регистр ключа | документы сопоставляются без учёта регистра |

Ключ, не прошедший правило, отвечает `InvalidKey` — и запись, и чтение.

### Итоги записи и чтения

`CloudSettingsWriteResult` (`Put`/`Delete`/`AskConflict`): `Outcome`, `Code`, `Current`, `Message` (пояснение для диагностики, не для показа участнику).

| Outcome | Когда | Что делать |
|---------|-------|-----------|
| `Queued` | Запись легла в локальную очередь | Отправит панель |
| `Unchanged` | Содержимое совпадает с облачным | Ничего |
| `Conflict` | Ожидаемая версия не совпала с виденной машиной | `put.Current` — актуальная; перечитать, переписать или `AskConflict` |
| `Unavailable` | Участник не входил или вышел, панель старше облачных настроек, пространство не адресуемо | Повторить позже |
| `Forbidden` | Нет активной организации или право только чтение | Право — `GetAccess` |
| `InvalidKey` | Ключ не проходит правило | Исправить ключ |
| `TooLarge` | Документ больше 5 МиБ | Уменьшить документ |
| `Failed` | Локальный сбой записи очереди или `content = null` | Повторить позже / передать содержимое |

`CloudSettingsReadResult.Outcome` (`Get`): `Found`, `NotFound`, `Deleted`, `Unavailable` (кэша нет), `InvalidKey`.

Отказ сервера после того, как запись ушла с машины, приходит событием `UploadRejected`; документ при этом имеет `State = Rejected` и `RejectionCode`.

### Со старой панелью

Панель старше облачных настроек для SDK — «облака нет»: `Availability` и все методы отвечают `Unavailable`, в очередь ничего не попадает, события не приходят. Проверять версию панели плагину не нужно.

### Пример

```csharp
using GrossGeo.SDK.Settings;

public sealed class ProductSettings
{
    private static readonly CloudSettingsClient Settings = GrossGeoCloudSettings.For(ProductKey);

    public void Save(byte[] content)
    {
        var put = Settings.Put(CloudSettingsScope.Product, "ui/layout.json", content,
            expectedVersion: CloudSettingsVersion.LastSeenByThisMachine);

        switch (put.Outcome)
        {
            case CloudSettingsWriteOutcome.Queued:
            case CloudSettingsWriteOutcome.Unchanged:
                break; // отправит панель

            case CloudSettingsWriteOutcome.TooLarge:
            case CloudSettingsWriteOutcome.InvalidKey:
                break; // ошибка продукта — исправить документ или ключ

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

С `LastSeenByThisMachine` `Conflict` не возникает; он возможен при явном номере версии.

---

## Счётчики команд

Плагин отдаёт платформе **суточные итоги использования команд** — сколько раз команды продукта выполнены за местные сутки машины. Данные анонимны по построению: в отчёте нет ни участника, ни машины, ни IP. Без согласия участника (см. [Соглашения и согласие](#соглашения-и-согласие)) на диск ничего не пишется.

### Быстрый старт

```csharp
using GrossGeo.SDK.Telemetry;

var usage = GrossGeoUsage.For("<ключ продукта>");

// ваш накопитель отдаёт итог за сутки БЕЗ обнуления:
usage.SubmitTotals(localDay, productVersion, counts);
// ваш накопитель забирает накопленное и обнуляет:
usage.AddCounts(localDay, productVersion, counts);
```

### Сигнатуры

```csharp
namespace GrossGeo.SDK.Telemetry;

public static class GrossGeoUsage
{
    public static UsageReporter For(string productKey); // не бросает
}

public sealed class UsageReporter
{
    // Итог за сутки в этом процессе на сейчас: ЗАМЕНЯЕТ прежний итог.
    public UsageSubmitResult SubmitTotals(DateTime localDay, string productVersion,
        IEnumerable<KeyValuePair<string, long>> commandCounts);

    // Приращение: ПРИБАВЛЯЕТ к прежнему итогу.
    public UsageSubmitResult AddCounts(DateTime localDay, string productVersion,
        IEnumerable<KeyValuePair<string, long>> commandCounts);
}

public sealed class UsageSubmitResult
{
    public UsageSubmitOutcome Outcome { get; }
    public int SkippedCommands { get; } // пропущено проверкой: плохое имя, число вне 1..1 000 000, сверх 500 команд
}

public enum UsageSubmitOutcome
{
    Unknown = 0,      // ответа нет
    Stored = 1,       // записано в локальный файл суток; отправит панель
    NotConsented = 2, // согласия нет — на диск ничего не пишется
    Invalid = 3,      // плохая версия или день, либо ноль валидных команд
    Failed = 4,       // локальный сбой записи; повторить позже
}
```

### Когда звать

| Когда | Чем |
|---|---|
| По таймеру раз в 10–15 минут из фонового потока | `SubmitTotals` или `AddCounts` |
| При выгрузке плагина | `SubmitTotals` или `AddCounts` — последний итог на диск до закрытия |
| При смене местных суток | `SubmitTotals` или `AddCounts` со **вчерашней** датой, затем сброс суточного накопителя |

Звать `For` на каждом такте можно: внутри копии SDK это один репортёр на ключ, удвоения нет. Пустой ключ даёт репортёр, который на всё отвечает `Invalid`.

### Контракт

- **Не бросает, не ходит в сеть и в канал.** Только локальный файл суток; бюджет — единицы миллисекунд на вызов.
- **Итог по продукту — один.** `SubmitTotals` заменяет итог этой копии SDK: если несколько модулей одного процесса зовут `SubmitTotals` каждый со своим итогом, выживет последний. Держите один общий счётчик или пишите приращения через `AddCounts`.
- **Без согласия — `NotConsented`, файл не пишется.** Согласие SDK берёт из локального состояния соглашений. Проверка вызова идёт раньше: плохой вызов при живом согласии отвечает `Invalid`.
- **Повторная отправка не удваивает.** Панель досылает изменившуюся сумму суток тем же отчётом с новой ревизией.
- Панель отправляет только **завершённые** сутки — обычно один раз в сутки; неотправленные файлы старше 35 суток удаляет.

### Правила проверки

- имя команды: 1..64 символа ASCII `A-Z a-z 0-9 _ . : -`; SDK приводит к верхнему регистру;
- число: 1..1 000 000;
- команд в отчёте: не больше 500 — остальные учитываются в `SkippedCommands` (отчёт всё равно `Stored`);
- `productVersion`: до 32 символов `A-Z a-z 0-9 . + -`;
- `localDay`: не в будущем.

### Что уходит на сервер и чего нет

SDK пишет только локальный файл; на сервер отчёты отправляет User Panel под учётной записью участника. В каждом отчёте:

| Поле | Что это |
|---|---|
| `reportId` | Случайный GUID на (участник, продукт, версия, сутки) машины; с машиной не связан |
| `revision` | Растёт при досылке поздних изменений того же отчёта |
| `productKey` | Ключ продукта |
| `productVersion` | Версия, как её передал плагин |
| `day` | Местные сутки машины, `yyyy-MM-dd` |
| `sdkVersion` | Версия SDK |
| `commands` | `{команда: число}` |

**Чего нет** — по построению: участник, машина, IP, время приёма (сервер хранит дату приёма без времени), персональные данные. На сервере сырые отчёты хранятся 40 суток, суточные агрегаты — бессрочно. Студия видит команду, число и число источников (машино-суток); ячейка, где источников меньше 3, скрывается.

### Отключение

Участник выключает счётчики галочкой в плагине **или** переключателем «Анонимные счётчики команд» в User Panel → «Настройки». Отзыв действует сразу: `SubmitTotals`/`AddCounts` отвечают `NotConsented`, неотправленные файлы студии панель удаляет. Счётчики других студий с действующим согласием не трогаются. Уже отправленное с сервера не удаляется: оно анонимно и ни с кем не связано.

### Пример: плагин с таймером

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using GrossGeo.SDK.Telemetry;

public sealed class UsageCollector : IDisposable
{
    private readonly UsageReporter _usage;
    private readonly Timer _timer;
    private DateTime _day = DateTime.Now.Date;

    public UsageCollector(string productKey)
    {
        _usage = GrossGeoUsage.For(productKey);
        _timer = new Timer(_ => Tick(), null, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
    }

    private void Tick()
    {
        var today = DateTime.Now.Date;
        if (today != _day)
        {
            TickFor(_day);        // дослать вчерашний итог со вчерашней датой
            ResetDailyCounters(); // ваш суточный накопитель
            _day = today;
        }

        TickFor(today);
    }

    private void TickFor(DateTime day)
    {
        IEnumerable<KeyValuePair<string, long>> counts = CurrentDailyCounts(); // «команда → число» за сутки

        var result = _usage.SubmitTotals(day, "<версия продукта>", counts);
        // Stored — записано; NotConsented — согласия нет (файла нет);
        // Failed — локальный сбой, следующий такт перезапишет.
    }

    public void Dispose()
    {
        TickFor(_day); // последний итог — на диск до закрытия
        _timer.Dispose();
    }
}
```

---

## Соглашения и согласие

Локальный клиент соглашений продукта: принятие версии соглашения и согласие участника на анонимные счётчики команд. Главная копия решений — учётная запись участника на платформе; на машине — очередь решений (её отправляет панель) и кэш учётной записи (его обновляет панель). Плагин решает «показывать ли окно соглашения» по `GetState`.

### Быстрый старт

```csharp
using GrossGeo.SDK.Legal;

var legal = GrossGeoAgreements.For("<ключ продукта>");

var state = legal.GetState("product-usage-terms", textSha256);
if (state.Acceptance != AgreementAcceptance.Accepted)
{
    ShowDialog(); // окно с текстом соглашения
}
```

### Сигнатуры

```csharp
namespace GrossGeo.SDK.Legal;

public static class GrossGeoAgreements
{
    public static AgreementClient For(string productKey); // не бросает
}

public sealed class AgreementClient
{
    public AgreementState GetState(string agreementCode, string textSha256);
    public AgreementWriteResult Accept(string agreementCode, string textSha256, string? title, bool telemetryConsent);
    public AgreementWriteResult SetTelemetryConsent(bool granted);
    public TelemetryConsent TelemetryConsent { get; }
    public event EventHandler<AgreementChangedEventArgs>? Changed;
}

public sealed class AgreementState
{
    public AgreementAcceptance Acceptance { get; }  // Unknown / Accepted / NotAccepted
    public DateTime? AcceptedAtUtc { get; }
    public TelemetryConsent Telemetry { get; }      // Unknown / NotAsked / Granted / Revoked
    public AgreementStateSource Source { get; }     // Unknown / Account / Machine
    public bool HasPendingUpload { get; }
}

public sealed class AgreementWriteResult
{
    public AgreementWriteOutcome Outcome { get; }   // Unknown / Stored / Invalid / Failed
    public string? Message { get; }
}

public sealed class AgreementChangedEventArgs
{
    public AgreementChangeKind Kind { get; }        // Unknown / TelemetryConsentChanged / AcceptanceChanged
}
```

### Контракт

- **Не бросает, не ходит в сеть и в канал**, бюджет — единицы миллисекунд.
- `GetState(agreementCode, textSha256)`:
  - **Acceptance** — объединение: решение этой машины для этого кода и отпечатка, иначе кэш учётной записи для студии продукта. Другой отпечаток текста — новая версия: все принимают заново. «Отозвать принятие» нельзя.
  - **Telemetry** — последнее неотправленное решение этой машины, иначе кэш учётной записи; сведений нет — `NotAsked`.
  - **Source** — где живёт действующее состояние: `Account` (кэш учётной записи), `Machine` (решение этой машины) или `Unknown` (сведений нет).
  - **HasPendingUpload** — на этой машине есть неотправленное решение продукта.
- `Accept(code, sha, title, telemetryConsent)` — пишет решение о принятии; с `telemetryConsent: true` вторым решением выдаёт согласие на счётчики. Решения копятся в локальной очереди и отправляются панелью — окно соглашения можно показывать без сети.
- `SetTelemetryConsent(granted)` — выдача или отзыв согласия **для студии продукта** (кода соглашения не требует). Отзыв согласия принятие соглашения не отменяет: это независимые решения.
- **Отзыв действует сразу и без сети:** `GetState` отвечает `Revoked`, счётчики — `NotConsented`; панель удаляет неотправленные счётчики студии. Если сервер отзыв отвергнет, местное «нет» продолжает действовать, пока учётная запись не покажет более позднее решение.
- `TelemetryConsent` читается из памяти; файл перечитывается не чаще раза в 2 с.
- `Changed` — отзыв или выдача в другом месте (другой процесс, панель, её страница настроек); виды `TelemetryConsentChanged` и `AcceptanceChanged`; поток пула. Отписка снимает только ваш обработчик.
- Решение, принятое до входа в панель, при первом входе уходит в учётную запись вошедшего участника и засчитывается.
- **Вторая машина без своих решений.** Если у ключа продукта на этой машине ещё нет привязки, а участник в панели вошёл, `For`, `GetState`, `TelemetryConsent`, `GrossGeoUsage.For` и методы счётчиков оставляют панели файл-запрос привязки (`legal\resolve\…json`, без канала и сети). Панель разрешает привязку в ближайшем такте; дальше `GetState` отвечает по кэшу учётной записи (`Accepted`, `Source = Account`). До этого окно соглашения может показаться один раз — повторный `Accept` безвреден.

### Правила формы

- `agreementCode`: `^[a-z0-9][a-z0-9._-]{0,63}$` — только нижний регистр;
- `textSha256`: 64 hex-символа **в нижнем регистре**. SDK проверяет только форму; нормализацию текста выбирает команда продукта, и она должна быть постоянной: другой отпечаток — новая версия;
- `title`: до 200 символов.

Нарушение формы: `Accept` отвечает `Invalid`, `GetState` — состояние `Unknown`.

### Правило расхождения

Главная копия — учётная запись платформы, местные — копии и буфер до входа.

- **Принятие версии — объединение.** Принята на любой машине учётной записи — принята везде (`Accepted`, `Source = Account`).
- **Согласие на счётчики — последнее решение, дошедшее до учётной записи, по студии.** «Да» и «нет» любого продукта студии и со страницы панели — одно согласие. Старое «да», чьё время на машине раньше действующего «нет» той же студии, не действует: «да» с машины, долго бывшей без сети, не отменяет более позднего «нет».
- **Без входа в панель** работает только местное хранение; решения переносятся в учётную запись при первом входе.

### Что удаляется при отзыве

| Что | Судьба |
|---|---|
| Неотправленные счётчики студии на машине | Удаляются панелью |
| Новые отчёты после отзыва | SDK отвечает `NotConsented`; сервер такие отчёты не принимает |
| Уже отправленное | Не удаляется: на сервере оно анонимно и ни с кем не связано |
| Принятие версии соглашения | Не затрагивается |

### Пример: окно соглашения в плагине

```csharp
using GrossGeo.SDK.Legal;

// Отпечаток версии текста — считайте сами и всегда одинаково (net48-совместимо):
static string Sha256Hex(string text)
{
    using (var sha = System.Security.Cryptography.SHA256.Create())
    {
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

var legal = GrossGeoAgreements.For("<ключ продукта>");
var code = "myproduct-usage-terms";
var sha = Sha256Hex(agreementText);

var state = legal.GetState(code, sha);
if (state.Acceptance != AgreementAcceptance.Accepted)
{
    // окно: текст соглашения + галочка «Отправлять анонимные счётчики команд»
    bool agreed = ShowAgreementDialog(agreementText, out bool telemetryConsent);
    if (agreed)
    {
        var write = legal.Accept(code, sha, "Соглашение MyProduct", telemetryConsent);
        // Stored — решение в очереди, уйдёт с панелью; согласие действует уже сейчас
    }
}

legal.Changed += (s, e) =>
{
    if (e.Kind == AgreementChangeKind.TelemetryConsentChanged)
    {
        var consent = legal.TelemetryConsent; // Granted / Revoked / NotAsked
        // переключить свою галочку и сбор счётчиков
    }
};
```

Участник закрыл окно, не приняв, — решения нет: окно покажется при следующем старте (`GetState` всё ещё `NotAccepted`). `Failed` — локальный сбой записи: повторите при следующем старте.

### Текст соглашения

В тексте для участника стоит назвать: что считается (команды продукта, суточные итоги, без участника и машины); что данные анонимны и после отправки не удаляются; где отключить — галочка в плагине или User Panel → «Настройки».
