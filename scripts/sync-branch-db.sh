#!/usr/bin/env bash
set -euo pipefail

# Atualiza apenas ISM_BRANCH_SLUG no .env local; secrets e demais variáveis são preservados.
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ROOT_DIR}/.env"
BRANCH="$(git -C "${ROOT_DIR}" branch --show-current 2>/dev/null || true)"

if [ -z "${BRANCH}" ]; then
  BRANCH="detached-$(git -C "${ROOT_DIR}" rev-parse --short HEAD)"
fi

SLUG="$(printf '%s' "${BRANCH}" | tr '[:upper:]' '[:lower:]' | sed -E 's/[^a-z0-9]+/-/g; s/^-+//; s/-+$//')"
SLUG="${SLUG:-default}"
SLUG="${SLUG:0:48}"

if [ ! -f "${ENV_FILE}" ]; then
  cp "${ROOT_DIR}/.env.example" "${ENV_FILE}"
fi

TEMP_FILE="$(mktemp "${ENV_FILE}.XXXXXX")"
trap 'rm -f "${TEMP_FILE}"' EXIT

if rg -q '^ISM_BRANCH_SLUG=' "${ENV_FILE}"; then
  sed -E "s|^ISM_BRANCH_SLUG=.*$|ISM_BRANCH_SLUG=${SLUG}|" "${ENV_FILE}" > "${TEMP_FILE}"
else
  cat "${ENV_FILE}" > "${TEMP_FILE}"
  printf '\nISM_BRANCH_SLUG=%s\n' "${SLUG}" >> "${TEMP_FILE}"
fi

mv "${TEMP_FILE}" "${ENV_FILE}"
trap - EXIT
printf 'Banco local selecionado para a branch %s: ism_mysql_%s\n' "${BRANCH}" "${SLUG}"
