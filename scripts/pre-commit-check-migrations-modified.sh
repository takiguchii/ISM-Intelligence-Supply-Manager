#!/usr/bin/env bash
# scripts/pre-commit-check-migrations-modified.sh
#
# Implementa FISICAMENTE a "Golden Rule de Migrations EF Core" do DEV_GUIDELINES.md.
# Objetivo: NUNCA permitir que alguém comite alterações em arquivos de
# migration (*.Designer.cs / InitialCreate / IsmDbContextModelSnapshot.cs)
# que JÁ EXISTIAM na develop.
#
# Motivação: Migrations EF são uma trilha auditável imutável do schema.
# Tocar em migration já mergeada produz os erros de "Unknown column",
# "No migrations were found in assembly", volume mysql_data dessincronizado
# e horas de debug (que já aconteceu 2x em 2 dias no projeto).
#
# Estratégia:
#   1. Pega a lista de todos os arquivos do staging area (`git diff --cached --name-only --diff-filter=ACMRT`).
#   2. Filtra só os que estão dentro de backend/**/*Migrations/*.cs (tudo que é migration/Designer/Snapshot).
#   3. Pra cada um, verifica: ESSE ARQUIVO JÁ EXISTIA NO COMMIT MAIS RECENTE DA origin/develop?
#      - Sim (e tem diff no conteúdo) → migration antiga modificada → BLOQUEIA.
#      - Não (arquivo novo) → migration nova → PASSA.
#   4. Também detecta arquivos RENOMEADOS (diff-filter=R) que são migrations (ex.: InitialCreate → Baseline).
#
# Saídas esperadas:
#   exit 0 → nenhuma migration antiga modificada.
#   exit 1 → tem migration antiga no commit → aborta commit + mostra solução.

set -euo pipefail

MIGRATION_GLOB='backend/src/ISM.Infrastructure/Data/Migrations/*.cs'
DEVELOP_REF="${DEVELOP_REF:-origin/develop}"
BYPASS_ENV="ISM_BYPASS_MIGRATION_GOLDEN_RULE"

# ------------------------------------
# 0. Bypass (situacoes excepcionais: reconstrucao baseline, avisa time)
# ------------------------------------
if [[ "${!BYPASS_ENV:-}" == "1" ]]; then
  echo "⚠️  [pre-commit][migrations] BYPASS ATIVADO via \$$BYPASS_ENV. GOLDEN RULE NÃO ESTÁ SENDO APLICADA."
  echo "    Certifique-se que o time INTEIRO foi avisado e que todo mundo vai rodar: docker compose down -v"
  exit 0
fi

# ------------------------------------
# 1. Nenhuma modificacao no staging area? Nada p/ fazer.
# ------------------------------------
if ! git rev-parse --verify --quiet HEAD >/dev/null; then
  echo "ℹ️  [pre-commit][migrations] Primeiro commit do repo (sem HEAD). Pulando checagem."
  exit 0
fi

# ------------------------------------
# 2. Pega todos os arquivos do staging area (incluindo renomes R)
# ------------------------------------
STAGED_FILES=$(git diff --cached --name-only --diff-filter=ACMRTUXB 2>/dev/null || true)
MIGRATION_FILES_STAGED=()
while IFS= read -r f; do
  [[ -z "$f" ]] && continue
  case "$f" in
    backend/src/ISM.Infrastructure/Data/Migrations/*.cs) MIGRATION_FILES_STAGED+=("$f") ;;
  esac
done <<< "$STAGED_FILES"

if [[ ${#MIGRATION_FILES_STAGED[@]} -eq 0 ]]; then
  echo "✅ [pre-commit][migrations] Nenhuma migration no staging area. Segue o commit."
  exit 0
fi

# ------------------------------------
# 3. Pra cada migration staged, verifica: ela existe na origin/develop?
#    Se existia E o arquivo é diferente do da develop, é VIOLAÇÃO.
# ------------------------------------
VIOLATIONS=()
SAFE_NEW=()
for file in "${MIGRATION_FILES_STAGED[@]}"; do

  EXISTE_NA_DEVELOP=$(git cat-file -t "$DEVELOP_REF:$file" 2>/dev/null || true)

  if [[ "$EXISTE_NA_DEVELOP" == "blob" ]]; then
    HASH_STAGED=$(git ls-files -s -- "$file" | awk '{print $2}')
    HASH_DEVELOP=$(git rev-parse "$DEVELOP_REF:$file")
    if [[ "$HASH_STAGED" != "$HASH_DEVELOP" ]]; then
      VIOLATIONS+=("$file")
    fi
  else
    SAFE_NEW+=("$file")
  fi
done

# ------------------------------------
# 4. Resultado
# ------------------------------------
if [[ ${#VIOLATIONS[@]} -gt 0 ]]; then
  echo ""
  echo "❌ [pre-commit][migrations] GOLDEN RULE VIOLADA — COMMIT ABORTADO."
  echo ""
  echo "   Tú tentaste commitar ALTERAÇÕES em ARQUIVOS DE MIGRATION que já existem no"
  echo "   último commit da develop ($DEVELOP_REF). Migrations EF Core são IMUTÁVEIS"
  echo "   depois que são mergeadas: nunca toca nelas. Isso causa erros de coluna faltante"
  echo "   (Unknown column X), migrations que não aplicam, e horas de debug."
  echo ""
  echo "   Arquivos VIOLADOS:"
  for f in "${VIOLATIONS[@]}"; do
    echo "      • $f"
  done
  echo ""
  echo "   O QUE FAZER:"
  echo "   1. Se querias adicionar/ajustar coluna/tabela: FAZ UMA MIGRATION NOVA (sempre)."
  echo "        dotnet ef migrations add NomeDaMigration --startup-project ../ISM.API/ISM.API.csproj"
  echo "   2. Se a migration é local, AINDA NÃO FOI PUSHEADA e tu quer reescrever:"
  echo "        dotnet ef migrations remove  (remove a última)"
  echo "        dotnet ef migrations add NomeCorreto (gera de novo)"
  echo "   3. Se é UMA SITUAÇÃO EXCEPCIONAL (reconstruir baseline do zero, combinado com o time INTEIRO):"
  echo "        Avisa todo mundo que precisa de: docker compose down -v (apagar mysql_data)"
  echo "        Depois, commita com bypass:   $BYPASS_ENV=1 git commit -m 'chore: rebuild baseline'"
  echo ""
  echo "   Dica rápida: reverte essas alterações de migration com:"
  echo "        git restore --source=$DEVELOP_REF --staged --worktree ${VIOLATIONS[*]}"
  exit 1
fi

echo "✅ [pre-commit][migrations] ${#MIGRATION_FILES_STAGED[@]} migration(s) no staging area — todas são NOVAS (inexistiam na develop). Segue o commit."
if [[ ${#SAFE_NEW[@]} -gt 0 ]]; then
  for f in "${SAFE_NEW[@]}"; do
    echo "     + nova: $f"
  done
fi
exit 0
