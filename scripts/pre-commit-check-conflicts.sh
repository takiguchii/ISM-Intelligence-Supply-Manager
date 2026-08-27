#!/usr/bin/env bash
# scripts/pre-commit-check-conflicts.sh
#
# Previne que markers de merge conflitado (<<<<<<< / ======= / >>>>>>>)
# sejam commitados ACIDENTALMENTE no código.
#
# Se qualquer arquivo trackeado (exceto node_modules, .nuget, bin, obj)
# contiver esses markers NO CONTEÚDO (não só no git diff index), o
# commit É ABORTADO com uma mensagem clara dizendo qual arquivo tem o erro.
#
# Para HABILITAR globalmente no repositório:
#   chmod +x scripts/pre-commit-check-conflicts.sh .githooks/pre-commit
#   git config core.hooksPath .githooks/
#
# Depois disso, TODO git commit do projeto passa por essa verificação.

set -euo pipefail

MARKER_REGEX='^(<<<<<<< |<<<<<<<|=======|>>>>>>> |>>>>>>>)'
CHECK_PATHS=(backend frontend docker devops docs .github)
EXCLUDE_DIRS=(
  'node_modules'
  'bin'
  'obj'
  '.nuget'
  'dist'
  '.output'
  '.nuxt'
)

echo "🔍 [pre-commit] Verificando merge conflict markers..."

# Builda a lista de --exclude-dir para grep
EXCLUDE_ARGS=()
for exc in "${EXCLUDE_DIRS[@]}"; do
  EXCLUDE_ARGS+=("--exclude-dir=${exc}")
done

FOUND=$(grep -rnE "${MARKER_REGEX}" "${EXCLUDE_ARGS[@]}" --include='*.cs' --include='*.csproj' --include='*.ts' --include='*.tsx' --include='*.js' --include='*.vue' --include='*.json' --include='*.yml' --include='*.yaml' --include='*.md' --include='*.env' --include='*.env.*' --include='*.sh' --include='Dockerfile' "${CHECK_PATHS[@]}" 2>/dev/null || true)

if [ -n "${FOUND}" ]; then
  echo ""
  echo "❌ [pre-commit] MERGE CONFLICT MARKERS ENCONTRADOS — commit ABORTADO."
  echo "   Arruma os arquivos abaixo antes de tentar commitar de novo:"
  echo ""
  echo "${FOUND}" | head -n 20
  TOTAL=$(echo "${FOUND}" | wc -l | tr -d ' ')
  echo ""
  echo "   Total de ocorrências: ${TOTAL}"
  echo ""
  echo "   Dica rápida: se foi um merge recém, resolve os conflitos, git add, tenta commit novamente."
  exit 1
fi

echo "✅ [pre-commit] Nenhum merge conflict marker detectado. Segue o commit."
exit 0
