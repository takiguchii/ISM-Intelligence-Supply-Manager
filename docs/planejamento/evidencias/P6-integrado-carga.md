# Evidência P6

- Teste de concorrência: `backend/tests/ISM.UnitTests/OrchestrationConcurrencyTests.cs`.
- Configuração validada sem alterar volumes ou executar `docker compose down -v`.
- O cache de build foi limpo; volumes ativos não foram removidos.
- Os bloqueios de banco isolado, Ollama real e Telegram foram registrados como `BLOCKED`.
