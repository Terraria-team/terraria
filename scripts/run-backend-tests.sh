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

# Кольори лише для інтерактивного терміналу: у пайпах, файлах логів і CI
# escape-послідовності перетворилися б на сміття. NO_COLOR — загальноприйнятий опт-аут.
if [ -t 1 ] && [ -z "${NO_COLOR:-}" ]; then
    GREEN=$'\033[0;32m'
    ORANGE=$'\033[38;5;208m'
    RED=$'\033[0;31m'
    RESET=$'\033[0m'
else
    GREEN='' ORANGE='' RED='' RESET=''
fi

# Працюємо від кореня репозиторію, звідки б скрипт не викликали.
REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "${RED}✗ Не знайдено .NET SDK (команда 'dotnet'). Встанови .NET 10 SDK.${RESET}" >&2
    exit 1
fi

# Інтеграційні тести піднімають PostgreSQL у Docker. Без Docker вони не падають,
# а пропускаються ([DockerFact]) — попереджаємо, щоб \"Skipped\" не був сюрпризом.
if docker info >/dev/null 2>&1; then
    echo "${GREEN}✓ Docker доступний — інтеграційні тести виконуватимуться.${RESET}"
else
    echo "${ORANGE}⚠ Docker не запущено — інтеграційні тести буде пропущено (Skipped).${RESET}"
    echo "${ORANGE}  Щоб прогнати їх повністю, запусти Docker Desktop.${RESET}"
fi

echo "→ Конфігурація: $CONFIGURATION"
echo

dotnet test "$SOLUTION" --configuration "$CONFIGURATION" "$@"
