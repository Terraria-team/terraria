# Coverage

Відстеження покриття коду тестами. Оновлюється вручну після значних змін у тестах.

**Як отримати цифри:**

```bash
# 1. Прогін із збором покриття
dotnet test Terraria.slnx --configuration Release \
  --settings coverage.runsettings \
  --collect:"XPlat Code Coverage" --results-directory ./TestResults

# 2. Звіт (потрібен dotnet tool install -g dotnet-reportgenerator-globaltool)
reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" \
  -targetdir:"TestResults/CoverageReport" -reporttypes:"Html;TextSummary"

# 3. Підсумок у консолі / HTML-звіт у браузері
cat TestResults/CoverageReport/Summary.txt
open TestResults/CoverageReport/index.html
```

У CI ці ж дані складаються в артефакт `backend-test-results`.

> **Важливо про межі вимірювання.** Числові метрики покриття рахуються лише для
> `Terraria.slnx` — це **backend-only** solution. Ігровий код (`UnityProject/Assets/`)
> у метрику **не входить** — ні в чисельник, ні в знаменник.
> Тому «80% покриття» нижче означає 80% **бекенду**, а не проєкту.
> Unity-тести (розділ GameLogic) існують, але їхнє *числове* покриття не збирається:
> для цього потрібен пакет Code Coverage + прогін через Unity, чого в CI немає (ліцензія).

---

## Backend

**Станом на:** 24.07.2026 · 68 тестів (23 юніт + 45 інтеграційних) · xUnit, запускаються в CI ·
`ServerInstanceCleanupService` виключено з метрик (див. нижче).

Дописано два рівні тестів на підсистему server-instances: Рівень 1 (PR #69, вже в `dev`) —
юніт-тести `ServerInstanceContainerProcessor`; Рівень 2 (ця гілка) — EF-репозиторії,
internal-ендпоінт із auth-фільтром та `ServerInstanceService.UpdatePlayerCount`.

### Загальні метрики

Цифри «ця гілка» — це Рівень 2 без #69 (його тести тут відсутні); «+ #69» — комбіновано
з уже змердженим у `dev` Рівнем 1 (`ServerInstanceContainerProcessor` покрито на 100%).

| Метрика | Ця гілка | + #69 | Було 23.07 |
|---|---|---|---|
| **Line coverage** | **76.9%** (674 / 877) | **83.5%** (732 / 877) | 64.4% |
| **Branch coverage** | **48.8%** (62 / 127) | **70.9%** (90 / 127) | 21.8% |
| **Method coverage** | **83.1%** (192 / 231) | ~90% | 77% |

Стрибок відносно 23.07 має дві причини: (1) дописані тести Рівнів 1–2; (2) з метрики
прибрано `ServerInstanceCleanupService` — один його метод мав 74 гілки й сам занижував
branch coverage удвічі.

### За збірками (ця гілка, cleanup виключено)

| Збірка | Line coverage |
|---|---|
| `LobbyUnityShared` | 100% |
| `Lobby.Application` | 88.9% |
| `Lobby.Infrastructure` | 72% |
| `Lobby` | 68.9% |

### Покрито повністю (100%)

| Клас | Чим покрито |
|---|---|
| `AuthService` | юніт-тести (**14 / 14 гілок** — усі шляхи помилок) |
| `JwtService`, `TokenService` | юніт-тести |
| `ServerInstanceContainerProcessor` | юніт-тести (**28 / 28 гілок**, Рівень 1 / #69) |
| `EfServerInstanceRepository` | інтеграційні — усі методи, включно з `GetById`, `GetByContainerId`, `Update`, `CreateMany`, `UpdateMany` |
| `EfPlayerRepository` | інтеграційні (додано `PlayerExists`) |
| `EfRefreshTokenRepository`, `EfPlayerExternalIdentityRepository` | інтеграційні (Testcontainers + PostgreSQL) |
| `InternalServerInstancesController`, `LobbyToServerAuthFilter` | API-тести (серверний ключ + `UpdatePlayerCount`) |
| усі 6 EF-конфігурацій + `LobbyDbContext` | інтеграційні |
| `ResultModel`, `ResultModel<T>`, `ServerInstanceModel`, DTO | юніт + API-тести |

### Непокрите — з поясненням причини

| Клас | Line | Причина |
|---|---|---|
| `GoogleAuthAdapter` | 0% | тонка обгортка над бібліотекою Google; жива взаємодія недетермінована в CI. Контракт `IExternalAuthProvider` покритий з обох боків (мок + фейк `FakeExternalAuthProvider`) |
| `DockerServerInstanceService` | 0% | справжній спавнер Docker-контейнерів; у тестах свідомо підмінений `FakeServerInstanceSpawner` |
| `PlayerGoogleLoginModel` | 0% | **мертвий код** — його замінив `PlayerExternalIdentityModel`, посилань не лишилось |
| `TerrariaWorldStorageEntity` | 0% | ще не використовується — сховище світів дописується |
| `ServerInstanceEntity` | 44.8% | методи станів (`MarkAsDead` тощо) на цій гілці кличе лише процесор із #69; після мержу покриття зросте |
| `TokenCleanupBackgroundService` | 54.5% | старт сервісу покритий, тіло циклу очистки — ні (той самий патерн `BackgroundService`, що й cleanup) |
| `LobbyControllerBase` | 50% | `HttpError` має 6 гілок, API реально повертає лише 3 (`Validation`, `Unauthorized`, `NotFound`) |
| `ServerInstanceService` | 98.4% (15 / 18 гілок) | лишились дрібні краї `UpdatePlayerCount`: `&&`-умова коли лічильник уже 0, і guard `Update` → null |
| `AuthController` | 93.3% | лишились гілки невалідного claim'а |

### Що виключено з вимірювання

Налаштовано в [`coverage.runsettings`](../coverage.runsettings):

- **EF-міграції** (`**/Migrations/*.cs`) — згенеровані; «виконуються» через `MigrateAsync`
  і штучно **завищували** line coverage
- **Сорс-генератор `Microsoft.AspNetCore.OpenApi`** — сотні гілок, які ніколи не виконуються
  в тестах і штучно **занижували** branch coverage у рази
- **`ServerInstanceCleanupService`** — на відміну від попередніх, це **не** згенерований код,
  а фоновий сервіс-оркестратор (`BackgroundService` з нескінченним циклом і синхронізацією
  Docker↔БД). Уся його логіка **прийняття рішень** винесена в `ServerInstanceContainerProcessor`
  і покрита на 100% гілок; те, що лишилось, — оркестраційний клей навколо живого `IDockerClient`,
  який не тестується без рефакторингу циклу. Один цей метод має 74 гілки й сам занижував branch
  coverage удвічі, тому вимірюємо його **окремо як TODO**, а не в загальній метриці.

> `CompilerGeneratedAttribute` навмисно **не** виключається: він позначає стейт-машини
> `async`-методів, і його виключення викинуло б з вимірювання майже весь реальний код.

---

## GameLogic

**Станом на:** 22.07.2026 · **56 активних тестів** · Unity EditMode / NUnit,
запуск **лише локально** через Unity Test Runner (у CI немає — потрібна ліцензія Unity)

### Що покрито

| Клас | Тестів | Що перевіряється |
|---|---|---|
| `ChunkData` | 6 | заповнення, Get/Set, індексатор, незалежність `Clone()` |
| `SparseChunkDelta` | 8 | обчислення та накладання мережевих дельт блоків; головний інваріант: `Apply(original, delta(original, updated)) == updated` |
| `ChunkUtils` | 7 | координатна математика: межі, унікальність індексів, взаємна оберненість (x,y) ↔ index |
| `ChunkUtils.ChunkCoordsAtWorldPosition` | 5 | світова позиція → координати чанка: межі чанків, дробові та від'ємні позиції |
| `ItemStack` / `NullableItemStack` | 9 | створення (клемп невалідної кількості), інкремент/декремент, порожній vs зайнятий слот |
| `ScriptableObjectRegistry` | 8 | наповнення, дублікати ID (лишається перший + error-лог), null-елементи, пошук, перелічення, очищення |
| `DataManager` (зонд) | 1 | ініціалізація в EditMode (Addressables), непорожні реєстри конфігів і біомів |
| `MapGenerator.GetBiomeTypeAt` | 8 | вибір біому за координатами: Lava, Caves/JungleCaves, Jungle/Ocean/Desert/Plains, пріоритет Lava |
| `MapGenerator.GenerateMapChunks` | 4 | повний конвеєр: детермінізм, кількість і повнота сітки чанків, наявність суцільних блоків |

### Знайдені та виправлені баги

- **#54 — інверсія осей у `ChunkCellCoordinates`**: функція повертала (y,x) замість (x,y)
  для 4032 із 4096 клітинок чанка. Латентний баг — не проявлявся, бо єдине використання
  було закоментоване; знайдений тестом на взаємну оберненість, виправлений одним рядком.
- **Truncation від'ємних координат у `ChunkCoordsAtWorldPosition`**: `(int)`-каст округлює
  до нуля, а не вниз — усі позиції з від'ємними координатами зсувались на +1 чанк
  (наприклад, (-10,-10) → чанк (0,0) замість (-1,-1)). Метод стоїть на шляху постановки
  блоків (`ActionContext`) і визначення біому (`PlayerController`). Виправлено на
  `Mathf.FloorToInt`; додатні позиції не змінились.

> Обидва баги — однієї природи (напрямок округлення/переплутані осі в координатній
> математиці) та обидва латентні. Систематичне покриття координатних перетворень
> ловить цей клас помилок до того, як вони проявляться в грі.

### Не покрито (відомі межі)
- **AI ворогів і босів** — стейт-машина (`IEnemyState`, стани slime/flyer/shooter) архітектурно
  чиста, але кожен стан у конструкторі вимагає `ServerEnemyController` (`NetworkBehaviour` +
  `Rigidbody2D`, `Time.deltaTime`, фізика). Тестопридатність потребує інтерфейсу над
  контролером — до обговорення з командою.
- **`HealthComponent`** — `NetworkBehaviour` із `SyncVar`; логіка урону/лікування недосяжна
  без винесення в POCO.
- **Інвентар** (`InventoryComponent`) — `NetworkBehaviour`; потребує винесення логіки в POCO.
  Чиста частина (`ItemStack`) уже покрита.
- **`ItemStack.IsFilled` та глобальні реєстри `DataManager`** — тягнуть ассети через
  Addressables; сам клас реєстру покритий ізольовано.
- **Усе PlayMode-залежне** (фізика, колізії, мережа Mirror, спавн мобів) — поза скоупом,
  потребує ліцензії Unity для CI.
