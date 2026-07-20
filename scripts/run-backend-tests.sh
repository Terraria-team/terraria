#!/usr/bin/env bash
#
# Запускає юніт- та інтеграційні тести бекенду (Terraria.slnx).
#
# Використання:
#   ./scripts/run-backend-tests.sh                 # Debug (швидко, для локальних прогонів)
#   CONFIGURATION=Release ./scripts/run-backend-tests.sh
#   ./scripts/run-backend-tests.sh --filter FullyQualifiedName~AuthService
#
# Код виходу дублює результат `dotnet test`: 0 — успіх, інакше — провал.
# Рішення «блокувати чи ні» лишається за тим, хто викликає (напр. pre-push хук).

set -euo pipefail

CONFIGURATION="${CONFIGURATION:-Debug}"
SOLUTION="Terraria.slnx"

# Працюємо від кореня репозиторію, звідки б скрипт не викликали.
REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "✗ Не знайдено .NET SDK (команда 'dotnet'). Встанови .NET 10 SDK." >&2
    exit 1
fi

# Інтеграційні тести піднімають PostgreSQL у Docker. Без Docker вони не падають,
# а пропускаються ([DockerFact]) — попереджаємо, щоб \"Skipped\" не був сюрпризом.
if docker info >/dev/null 2>&1; then
    echo "→ Docker доступний — інтеграційні тести виконуватимуться."
else
    echo "⚠ Docker не запущено — інтеграційні тести буде пропущено (Skipped)."
    echo "  Щоб прогнати їх повністю, запусти Docker Desktop."
fi

echo "→ Конфігурація: $CONFIGURATION"
echo

dotnet test "$SOLUTION" --configuration "$CONFIGURATION" "$@"
