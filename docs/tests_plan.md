# Tests

Тести для двох частин проєкту, поділені на **дві групи** (бекенд — перша),
всередині кожної — категорії за двома осями: **складність** (легко / складно) та **цінність** (низька / висока).

Обмеження: лише локальний запуск, без PlayMode. Тому `MonoBehaviour`/`NetworkBehaviour`
напряму не інстанціюємо — тестуємо чисту логіку (за потреби винесену в POCO).

---

# Група 1 — Бекенд (.NET, лобі)

## Category 1 [Легко / Висока цінність]

### Test 1 — Юніт-тести сервісів авторизації та інстансів

#### brief description
Логіка `AuthService` і `ServerInstanceService` з мокнутими залежностями (Moq):
happy-path + помилки (невалідний токен, дубль гравця, немає вільного інстансу).

#### [чому саме тут]
Інфраструктура (xUnit + Moq) вже підключена — старт миттєвий; логіка ізольована від БД і мережі.
Це ядро бекенду, тому цінність висока.

#### модулі
- `Lobby.Application/Services/AuthService.cs`
- `Lobby.Application/Services/ServerInstanceService.cs`
- контракти-заглушки: `IPlayerRepository`, `IServerInstanceRepository`, `IServerInstanceSpawner`, `ITokenService`, `IJwtService`

#### залежності
- xUnit, Moq (уже в `Lobby.Tests`)
- посилання на `Lobby.Application` (уже є)
- рефакторинг не потрібен

### Test 2 — Юніт-тести JWT / токенів

#### brief description
`JwtService` / `TokenService`: генерація, валідація, час життя, ротація refresh-токенів;
відхилення протермінованого та підробленого токена.

#### [чому саме тут]
Чиста детермінована логіка без зовнішніх залежностей — писати легко.
Стосується безпеки — ціна помилки висока, тож цінність висока.

#### модулі
- `Lobby.Infrastructure/Services/JwtService.cs`
- `Lobby.Infrastructure/Services/TokenService.cs`
- `Lobby.Application/Settings/JwtSettings.cs`, `Lobby.Application/Models/LoginTokensModel.cs`

#### залежності
- xUnit
- **додати посилання на `Lobby.Infrastructure`** у `Lobby.Tests` (зараз є лише на `Lobby.Application`)
- фіксований годинник/`TimeProvider` для тестів часу життя (за потреби — інжекція часу)

## Category 2 [Легко / Низька цінність]

### Test 5 — Маппери та моделі

#### brief description
`ServerInstanceMapper`, `ResultModel`, перетворення DTO ↔ Entity: коректність мапінгу полів.

#### [чому саме тут]
Прості чисті перетворення — писати легко.
Логіка тривіальна, ризик помилки низький → маргінальна цінність низька (переважно «наповнення» coverage).

#### модулі
- `Lobby/Mappers/ServerInstanceMapper.cs`
- `Lobby.Application/Models/ResultModel.cs`, `ServerInstanceModel.cs`
- `LobbyUnityShared/DTOs/ServerInstanceDto.cs`

#### залежності
- xUnit
- маппер у проєкті `Lobby` → **додати посилання на `Lobby`** (або на `LobbyUnityShared`, якщо DTO там)
- рефакторинг не потрібен

## Category 3 [Складно / Висока цінність]

### Test 7 — Інтеграційні тести EF-репозиторіїв

#### brief description
`EfPlayerRepository`, `EfRefreshTokenRepository`, `EfServerInstanceRepository` проти
**SQLite in-memory**: реальні LINQ-запити, конфігурації сутностей, застосування міграцій.

#### [чому саме тут]
Потрібно піднімати реальний `DbContext`, готувати seed-дані, керувати життям з'єднання — складніше за юніт.
Ловить помилки конфігурацій/запитів, яких моки не бачать → цінність висока.

#### модулі
- `Lobby.Infrastructure/Repositories/EfPlayerRepository.cs`, `EfRefreshTokenRepository.cs`, `EfServerInstanceRepository.cs`
- `Lobby.Infrastructure/Data/LobbyDbContext.cs` + `Data/*Configuration.cs`
- `Lobby.Infrastructure/Migrations/*`

#### залежності
- **посилання на `Lobby.Infrastructure`** у `Lobby.Tests`
- пакет `Microsoft.EntityFrameworkCore.Sqlite` (in-memory через `:memory:`)
- фікстура для створення/очищення схеми БД між тестами

### Test 8 — Інтеграційні тести API

#### brief description
Контролери через `WebApplicationFactory`: коди відповідей, валідація вхідних DTO,
робота middleware та JWT-політик — end-to-end HTTP без справжньої мережі.

#### [чому саме тут]
Треба піднімати весь пайплайн ASP.NET, налаштовувати тестову автентифікацію та БД — трудомістко.
Найближче до реального використання бекенду → цінність висока.

#### модулі
- `Lobby/Program.cs`, `Lobby/Controllers/*`
- `Lobby/DTOs/AuthControllerDtos.cs`
- пайплайн автентифікації (JWT-політики, middleware)

#### залежності
- пакет `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`)
- **посилання на `Lobby`** (веб-проєкт має бути тестопридатним; за потреби `public partial class Program`)
- тестова конфігурація: БД (SQLite in-memory) + підміна зовнішніх сервісів (Google, Docker-спавн)

### Test 9 — Логіка фонових сервісів очистки

#### brief description
Правила `TokenCleanupBackgroundService` / `ServerInstanceCleanupService`:
які саме токени/мертві інстанси видаляються і за яких умов (логіка, винесена з таймінгу).

#### [чому саме тут]
Асинхронність і фонове виконання ускладнюють детермінований тест — треба відділити логіку від планувальника.
Некоректна очистка = витік ресурсів або втрата валідних сесій → цінність висока.

#### модулі
- `Lobby/HostedServices/TokenCleanupBackgroundService.cs`
- `Lobby/HostedServices/ServerInstanceCleanupService.cs`
- `Lobby/Settings/BackgroundServicesSettings.cs`
- `IRefreshTokenRepository`, `IServerInstanceRepository` (мокаються)

#### залежності
- xUnit, Moq
- **посилання на `Lobby`**
- **рефакторинг**: винести правило «що видаляти» з циклу `ExecuteAsync`/таймера в чистий метод; інжекція часу (`TimeProvider`)

## Category 4 [Складно / Низька цінність]

### Test 14 — Ізольовані тести `GoogleAuthService`

#### brief description
Перевірка `GoogleAuthService` окремо від решти: обробка відповіді Google, помилкові/протерміновані id-token.

#### [чому саме тут]
Зовнішня залежність (Google) погано мокається — треба підміняти HTTP/валідатор, що трудомістко.
Клас переважно тонка обгортка над бібліотекою Google → маргінальна цінність низька.

#### модулі
- `Lobby.Infrastructure/Services/GoogleAuthService.cs`
- `Lobby.Infrastructure/Settings/GoogleSettings.cs`
- `IGoogleAuthService`

#### залежності
- xUnit, Moq; **посилання на `Lobby.Infrastructure`**
- абстракція/обгортка над валідатором Google-токенів, щоб було що підмінити (інакше тест недетермінований)

---

# Група 2 — Ігрова логіка (Unity EditMode / чиста C#)

## Category 1 [Легко / Висока цінність]

### Test 3 — Round-trip chunk-дельт

#### brief description
`SparseChunkDelta`, `LengthEncodedChunkDelta`: encode → decode повертає вихідні дані;
граничні випадки — порожній чанк, повністю змінений чанк, одна зміна.

#### [чому саме тут]
Чиста C#-логіка без Unity-залежностей, легко викликати напряму.
Це серце мережевої синхронізації блоків — баги тут найдорожчі, тож цінність висока.

#### модулі
- `Shared/Chunks/ChunkDeltas/SparseChunkDelta.cs`
- `Shared/Chunks/ChunkDeltas/LengthEncodedChunkDelta.cs`
- `Shared/Chunks/ChunkDeltas/BaseChunkDelta.cs`
- `Shared/Chunks/ChunkData.cs`

#### залежності
- Unity **EditMode** тест-асемблі (`.asmdef`) з посиланням на `Shared`
- com.unity.test-framework (уже підключено)
- перевірити, що дельти не тягнуть `NetworkBehaviour`-типи (лише дані/серіалізація)

### Test 4 — Координатна математика чанків

#### brief description
`ChunkUtils` / `ChunkData`: перетворення world↔chunk↔local координат, поведінка на межах чанків.

#### [чому саме тут]
Маленькі детерміновані функції — тривіально тестуються.
Часте джерело off-by-one, від якого залежить уся система чанків → цінність висока.

#### модулі
- `Shared/Chunks/ChunkUtils.cs`
- `Shared/Chunks/ChunkData.cs`

#### залежності
- Unity EditMode тест-асемблі з посиланням на `Shared`
- методи мають бути статичними/чистими; якщо ні — виділити чисті функції координат

## Category 2 [Легко / Низька цінність]

### Test 6 — Пошук у реєстрах даних

#### brief description
`ItemData`/`ItemID`, `BlockData`/`BlockID`, `ActionRegistry`: коректність пошуку за ID,
відсутність дублів/дір у реєстрі.

#### [чому саме тут]
Прості lookup-и по колекціях — легко.
Здебільшого перевіряє дані, а не поведінку → цінність низька.

#### модулі
- `Shared/Items/Defs/ItemData.cs`, `ItemID.cs`
- `Shared/BlockDefs/BlockData.cs`, `BlockID.cs`
- `Shared/Actions/Implementation/ActionRegistry.cs`

#### залежності
- Unity EditMode тест-асемблі з посиланням на `Shared`
- **ризик**: якщо реєстри — `ScriptableObject`, потрібні тестові ассети або фейкові дані (`DataManager`/`ScriptableObjectRegistry`)

## Category 3 [Складно / Висока цінність]

### Test 10 — Детермінізм генерації світу

#### brief description
`MapGenerator`: однаковий seed → однаковий світ; вихід у межах масиву; біоми/печери в допустимих діапазонах.

#### [чому саме тут]
Можлива потреба відв'язати генератор від Unity-залежностей; сама логіка об'ємна й неочевидна для перевірки.
Ядро «пісочниці» → цінність висока.

#### модулі
- `Core/WorldGeneration/WorldGenerator.cs` (`MapGenerator`)
- `Core/WorldGeneration/FastNoiseLite.cs`, `BiomeType.cs`, `BlockType.cs`
- конфіги: `Shared/Generation/Defs/WorldGenerationConfig.cs`, `BiomeGenerationConfig.cs`, `BiomeBordersGenerationConfig.cs`
- інтерфейси `IWorldGenerationConfig`, `IBiomeGenerationConfig`

#### залежності
- Unity EditMode тест-асемблі з посиланням на `Core` (+ конфіги з `Shared`)
- фейкові реалізації `IWorldGenerationConfig`/`IBiomeGenerationConfig` (щоб не залежати від `ScriptableObject`)
- перевірити, що `MapGenerator` не тягне `UnityEngine`-типи

### Test 11 — Property-based тести генерації

#### brief description
`MapGenerator` на багатьох випадкових seed'ах (FsCheck): інваріанти для *будь-якого* seed —
немає виходу за межі, поверхня не вище стелі, частка блоків у розумних рамках.

#### [чому саме тут]
Потрібно ввести FsCheck і сформулювати коректні інваріанти — концептуально складніше за приклад-тести.
Для процедурної генерації дає покриття, недосяжне точковими тестами → цінність висока.

#### модулі
- ті самі, що в Test 10 (`MapGenerator` + конфіги)

#### залежності
- усе з Test 10
- **пакет FsCheck** у Unity EditMode-асемблі (перевірити сумісність DLL з Unity; інакше — власний генератор seed'ів)

### Test 12 — Логіка інвентаря / стеків

#### brief description
Стекування, max-stack, додавання/вилучення, переповнення, вибір слота —
чиста логіка, винесена з `InventoryComponent` (`NetworkBehaviour`) у POCO.

#### [чому саме тут]
Вимагає попереднього **рефакторингу** (винесення логіки з `NetworkBehaviour`), щоб узагалі стати тестопридатним.
Інвентар — центральна ігрова механіка → цінність висока.

#### модулі
- `Shared/Inventory/InventoryComponent.cs` (логіка → новий POCO, напр. `InventoryModel`)
- `Shared/Items/Defs/ItemStack.cs`, `ItemID.cs`, `ItemData.cs`

#### залежності
- Unity EditMode тест-асемблі з посиланням на `Shared`
- **рефакторинг (передумова)**: винести операції над слотами/стеками з `NetworkBehaviour` у чистий клас без Mirror/Unity

### Test 13 — Регресійний тест: креш при постановці блоку на headless

#### brief description
Фіксація сценарію з історії комітів (креш block-placing на реальному headless-сервері):
відтворення умов і перевірка, що баг не повертається.

#### [чому саме тут]
Треба відтворити headless-контекст і виділити чисту логіку постановки блоку — нетривіально.
Захищає від регресії вже болючого бага → цінність висока.

#### модулі
- `Shared/Chunks/ChunkManager.cs` (шлях постановки/руйнування блоку)
- `Shared/Chunks/ChunkData.cs`, `ChunkDeltas/*`
- дотичне: серверна логіка з `Server/DedicatedServerStartup.cs`

#### залежності
- Unity EditMode тест-асемблі з посиланням на `Shared`
- **рефакторинг (передумова)**: виділити чисту логіку постановки блоку, щоб відтворити без запущеного Mirror-сервера
- зафіксувати точні вхідні дані бага з історії комітів як тест-кейс

## Category 4 [Складно / Низька цінність]

### Test 15 — Тести обгортки шуму `FastNoiseLite`

#### brief description
Прямі перевірки виходу `FastNoiseLite` (діапазони, детермінізм за seed) окремо від `MapGenerator`.

#### [чому саме тут]
Це стороння бібліотека шуму — змістовні асерти на «правильність» шуму сформулювати важко.
Тестувати чужу бібліотеку замість власної логіки → цінність низька (детермінізм і так покриє Test 10).

#### модулі
- `Core/WorldGeneration/FastNoiseLite.cs`

#### залежності
- Unity EditMode тест-асемблі з посиланням на `Core`
- рефакторинг не потрібен (перевірка чорної скриньки: seed → діапазон значень)

---

# Зведення залежностей проєкту (крос-довідка)

### Пакети / інструменти, які треба додати
- `Microsoft.EntityFrameworkCore.Sqlite` — Test 7
- `Microsoft.AspNetCore.Mvc.Testing` — Test 8
- **FsCheck** — Test 11
- окрема **Unity EditMode тест-асемблі** (`.asmdef` → `Core`/`Shared`) — Test 3, 4, 6, 10, 11, 12, 13, 15

### Посилання, яких бракує в `Lobby.Tests` (зараз лише на `Lobby.Application`)
- на `Lobby.Infrastructure` — Test 2, 7, 14
- на `Lobby` — Test 5, 8, 9

### Рефакторинг як передумова
- **Test 12** — винести логіку інвентаря з `NetworkBehaviour` у POCO
- **Test 13** — виділити чисту логіку постановки блоку
- **Test 9** — відділити правило очистки від таймера (+ інжекція часу)
- **Test 2** — інжекція часу (`TimeProvider`) для тестів TTL
- **Test 10/11** — переконатися, що `MapGenerator` без `UnityEngine`-залежностей

### Поза скоупом (за домовленістю)
- Крафт — відкладено.
- Фічі «у процесі» (вороги, нові drops) — не покриваємо.
- PlayMode-залежні тести (фізика/колізії, мережа Mirror, продуктивність, e2e) — розділ B, поки недоступні.
- Save/load ігрового світу — застосовне лише за наявності такої підсистеми (у коді наразі не виявлено).
