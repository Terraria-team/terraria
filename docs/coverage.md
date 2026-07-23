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

**Станом на:** 23.07.2026 · 50 тестів (20 юніт + 30 інтеграційних) · xUnit, запускаються в CI

Тести адаптовано під нову DDD-структуру (`Domain` / `UseCases` / `Persistence` /
`ExternalServices`) і під заміну Google-специфіки на узагальнений `IExternalAuthProvider`.

### Загальні метрики

| Метрика | Значення | Було 20.07 | Що означає |
|---|---|---|---|
| **Line coverage** | **64.4%** (639 / 992) | 79.9% (481 / 602) | скільки рядків виконалось хоч раз |
| **Branch coverage** | **21.8%** (43 / 197) | 64.5% (31 / 48) | скільки гілок `if`/`switch`/`&&` пройдено (обидва напрямки) |
| **Method coverage** | **77%** (181 / 235) | 85% (136 / 160) | скільки методів викликано хоч раз |
| **Full method coverage** | **74%** (174 / 235) | 81.8% (131 / 160) | скільки методів покрито *повністю* |

| Клас | Line | Гілок покрито |
|---|---|---|
| `ServerInstanceCleanupService` | 13% | 2 / 72 |
| `ServerInstanceContainerProcessor` | 0% | 0 / 26 |
| `DockerServerInstanceService` | 0% | 0 / 10 |
| `LobbyToServerAuthFilter` | 0% | 0 / 4 |
| `InternalServerInstancesController` | 0% | 0 / 4 |
| `ContainerAction` | 0% | — |

Тобто **branch coverage 21.8% — це метрика нетестованої підсистеми контейнерів**, а не
регрес автентифікації: `AuthService` тримає 100% рядків і 14 / 14 гілок.

### За збірками

| Збірка | Line coverage | Було 20.07 |
|---|---|---|
| `Lobby.Application` | 84.3% | 90.5% |
| `Lobby` | **45.8%** | 85% |
| `Lobby.Infrastructure` | 66.9% | 68.8% |
| `LobbyUnityShared` | 94.7% | 100% |

Обвал `Lobby` з 85% до 45.8% — це цілком hosted services і внутрішній API контейнерів.

### Покрито повністю (100%)

| Клас | Чим покрито |
|---|---|
| `AuthService` | юніт-тести (**14 / 14 гілок** — усі шляхи помилок) |
| `JwtService`, `TokenService` | юніт-тести |
| `EfRefreshTokenRepository` | інтеграційні (Testcontainers + PostgreSQL) |
| `EfPlayerExternalIdentityRepository` | інтеграційні (замінив `EfPlayerGoogleLoginRepository`) |
| усі 6 EF-конфігурацій + `LobbyDbContext` | інтеграційні |
| `ResultModel`, `ResultModel<T>`, `ServerInstanceModel`, `LoginTokensModel`, `PlayerExternalIdentityModel` | юніт + API-тести |

`ServerInstanceModel` раніше значився тут як **мертвий код (0%)** — після рефакторингу
`ServerInstanceService` повертає моделі замість сутностей, і клас став живим і повністю покритим.

### Непокрите — з поясненням причини

| Клас | Line | Причина |
|---|---|---|
| `ServerInstanceContainerProcessor`, `ContainerAction`, `InternalServerInstancesController`, `LobbyToServerAuthFilter` | 0% | нові класи керування контейнерами; тестів не було написано взагалі |
| `ServerInstanceCleanupService` | 13% | покритий лише старт сервісу (його піднімають API-тести), уся логіка очистки — ні |
| `GoogleAuthAdapter` | 0% | тонка обгортка над бібліотекою Google; жива взаємодія недетермінована в CI. Контракт `IExternalAuthProvider` покритий з обох боків (мок + фейк `FakeExternalAuthProvider`) |
| `DockerServerInstanceService` | 0% | справжній спавнер Docker-контейнерів; у тестах свідомо підмінений `FakeServerInstanceSpawner` |
| `PlayerGoogleLoginModel` | 0% | **мертвий код** — його замінив `PlayerExternalIdentityModel`, посилань не лишилось |
| `EfServerInstanceRepository` | 51.5% | нові методи (`GetById`, `GetByContainerId`, `Update`, `UpdateMany`, `CreateMany`) використовує лише непокритий cleanup-сервіс |
| `ServerInstanceService` | 80.9% (5 / 18 гілок) | `UpdatePlayerCount` не покритий зовсім; у `Create` не пройдено гілку `name == null` (світ без імені) |
| `LobbyControllerBase` | 37.5% | `HttpError` має 6 гілок, API реально повертає лише 2 (`Validation`, `Unauthorized`) |
| `TokenCleanupBackgroundService` | 54.5% | старт сервісу покритий, тіло очистки — ні |
| `ErrorModel` | 66.6% | фабрики типів помилок, які наразі не використовуються |
| `ServerInstanceEntity` | 44.8% | методи станів (`MarkAsDead`, `MarkAsRunning`, `IsIdleFor`) кличе лише непокритий cleanup-сервіс |
| `AuthController` | 93.3% | лишились гілки невалідного claim'а |
| `EfPlayerRepository` | 94.5% | не покритий `PlayerExists` |

### Що виключено з вимірювання

Налаштовано в [`coverage.runsettings`](../coverage.runsettings):

- **EF-міграції** (`**/Migrations/*.cs`) — згенеровані; «виконуються» через `MigrateAsync`
  і штучно **завищували** line coverage
- **Сорс-генератор `Microsoft.AspNetCore.OpenApi`** — сотні гілок, які ніколи не виконуються
  в тестах і штучно **занижували** branch coverage у рази

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
