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

**Станом на:** 20.07.2026 · 50 тестів (20 юніт + 30 інтеграційних) · xUnit, запускаються в CI

### Загальні метрики

| Метрика | Значення | Що означає |
|---|---|---|
| **Line coverage** | **79.9%** (481 / 602) | скільки рядків виконалось хоч раз |
| **Branch coverage** | **64.5%** (31 / 48) | скільки гілок `if`/`switch`/`&&` пройдено (обидва напрямки) |
| **Method coverage** | **85%** (136 / 160) | скільки методів викликано хоч раз |
| **Full method coverage** | **81.8%** (131 / 160) | скільки методів покрито *повністю* |

### За збірками

| Збірка | Line coverage |
|---|---|
| `Lobby.Application` | 90.5% |
| `Lobby` | 85% |
| `Lobby.Infrastructure` | 68.8% |
| `LobbyUnityShared` | 100% |

### Покрито повністю (100%)

| Клас | Чим покрито |
|---|---|
| `AuthService` | юніт-тести (**28 / 28 гілок** — усі шляхи помилок) |
| `ServerInstanceService` | юніт-тести |
| `JwtService`, `TokenService` | юніт-тести |
| `EfRefreshTokenRepository` | інтеграційні (Testcontainers + PostgreSQL) |
| `EfServerInstanceRepository` | інтеграційні |
| `EfPlayerGoogleLoginRepository` | інтеграційні |
| `ServerInstancesController` | API-тести (`WebApplicationFactory`) |
| усі 4 EF-конфігурації + `LobbyDbContext` | інтеграційні |

### Непокрите — з поясненням причини

| Клас | Line | Причина |
|---|---|---|
| `GoogleAuthService` | 0% | Test 14 скасовано: тонка обгортка над бібліотекою Google; жива взаємодія недетермінована в CI. Контракт `IGoogleAuthService` покритий з обох боків (мок + фейк) |
| `DockerServerInstanceService` | 0% | справжній спавнер Docker-контейнерів; у тестах свідомо підмінений `FakeServerInstanceSpawner` |
| `ServerInstanceModel` | 0% | **мертвий код** — не використовується ніде в бекенді |
| `LobbyControllerBase` | 42.8% | `HttpError` має 5 гілок, API реально повертає лише 2 (`Validation`, `Unauthorized`); `Conflict`/`NotFound` не використовує жоден сервіс |
| `TokenCleanupBackgroundService` | 54.5% | старт сервісу покритий (його піднімають API-тести), тіло очистки — ні → це **Test 9** |
| `ErrorModel` | 62.5% | фабрики типів помилок, які наразі не використовуються |
| `EfPlayerRepository` | 94.8% | лишились дрібні краєві гілки |
| `AuthController` | 91.6% | лишились гілки невалідного claim'а |

### Що виключено з вимірювання

Налаштовано в [`coverage.runsettings`](../coverage.runsettings):

- **EF-міграції** (`**/Migrations/*.cs`) — згенеровані; «виконуються» через `MigrateAsync`
  і штучно **завищували** line coverage
- **Сорс-генератор `Microsoft.AspNetCore.OpenApi`** — 404 гілки, які ніколи не виконуються
  в тестах і штучно **занижували** branch coverage (12% замість 64.5%)

> `CompilerGeneratedAttribute` навмисно **не** виключається: він позначає стейт-машини
> `async`-методів, і його виключення викинуло б з вимірювання майже весь реальний код.

---

## GameLogic

**Станом на:** 21.07.2026 · **21 активний тест** (+6 тимчасово вимкнених) ·
Unity EditMode / NUnit, запуск **лише локально** через Unity Test Runner (у CI немає — потрібна ліцензія Unity)

### Що покрито

| Клас | Тестів | Що перевіряється |
|---|---|---|
| `ChunkData` | 6 | заповнення, Get/Set, індексатор, незалежність `Clone()` |
| `SparseChunkDelta` | 8 | обчислення та накладання мережевих дельт блоків; головний інваріант: `Apply(original, delta(original, updated)) == updated` |
| `ChunkUtils` | 7 | координатна математика: межі, унікальність індексів, взаємна оберненість (x,y) ↔ index |

### Знайдені та виправлені баги

- **#54 — інверсія осей у `ChunkCellCoordinates`**: функція повертала (y,x) замість (x,y)
  для 4032 із 4096 клітинок чанка. Латентний баг — не проявлявся, бо єдине використання
  було закоментоване; знайдений тестом на взаємну оберненість, виправлений одним рядком.

### Тимчасово вимкнено (⏸ чекає узгодження з командою)

- **6 тестів `MapGenerator`** (детермінізм seed, розміри, валідність блоків, шаруватість
  поверхні, вимкнені печери) — написані під конструктор з інжекцією `IWorldGenerationConfig`/
  `IBiomeGenerationConfig`. Після перепису генератора (конфіги зі статичного `DataManager`,
  інтерфейси видалено) тести не компілюються — обгорнуті в `#if false` у
  `UnityProject/Assets/Tests/EditMode/Core/`. Повернення можливе після відновлення точки
  інжекції конфігів (обговорюється з командою).

### Не покрито (відомі межі)

- **Генерація світу** — див. вище (⏸).
- **Інвентар** (`InventoryComponent`) — це `NetworkBehaviour`; потребує винесення логіки
  в POCO (рефакторинг, обговорюється). Чиста частина (`ItemStack`) — кандидат найближчої черги.
- **Реєстри даних** (`ItemData`/`BlockData`) — залежать від `DataManager`/Addressables;
  розвідка можливості EditMode-ініціалізації триває.
- **Усе PlayMode-залежне** (фізика, колізії, мережа Mirror) — поза скоупом, потребує ліцензії
  Unity для CI.
