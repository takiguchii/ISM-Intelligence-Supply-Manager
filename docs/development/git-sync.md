# Guia de Sincronização Git

Este guia explica como manter suas branchs locais em dia com o repositório remoto.

## Sincronização Geral

Sempre que iniciar o trabalho, é recomendável puxar as últimas alterações de todas as branchs ativas para garantir que você está trabalhando sobre a versão mais recente.

### Comandos Passo a Passo

1. **Atualizar informações do remoto:**
   Atualiza a lista de branchs e deleta referências a branchs que já foram removidas do servidor.
   ```bash
   git fetch --all --prune
   ```

2. **Sincronizar branch `main`:**
   ```bash
   git checkout main
   git pull origin main
   ```

3. **Sincronizar branch `develop`:**
   ```bash
   git checkout develop
   git pull origin develop
   git merge main
   ```

### Bloco "Copia e Cola"

Se você deseja rodar tudo de uma vez para garantir que `main` e `develop` estejam atualizadas localmente:

```bash
git fetch --all --prune && \
git checkout main && git pull origin main && \
git checkout develop && git pull origin develop && git merge main
```

---
**Dica:** Se você estiver trabalhando em uma branch de funcionalidade (ex: `feat/nova-tela`), lembre-se de mergear a `develop` nela periodicamente para evitar conflitos grandes no momento do Merge Request.
