#!/usr/bin/env bash
#
# Вмикає git-хуки з каталогу .githooks/ (він, на відміну від .git/hooks/, версіонується).
# Виконати один раз після клонування репозиторію:
#
#   ./scripts/install-hooks.sh
#
# Вимкнути: git config --unset core.hooksPath

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

if [ -t 1 ] && [ -z "${NO_COLOR:-}" ]; then
    GREEN=$'\033[0;32m'; DIM=$'\033[2m'; RESET=$'\033[0m'
else
    GREEN=''; DIM=''; RESET=''
fi

git config core.hooksPath .githooks

echo "${GREEN}✓ Git-хуки увімкнено (core.hooksPath = .githooks)${RESET}"
echo
echo "  Активний хук: pre-push — попереджає про червоні тести бекенду."
echo "  Пуш він НЕ блокує."
echo
echo "${DIM}  Пропустити разово:  git push --no-verify${RESET}"
echo "${DIM}  Вимкнути повністю:  git config --unset core.hooksPath${RESET}"
