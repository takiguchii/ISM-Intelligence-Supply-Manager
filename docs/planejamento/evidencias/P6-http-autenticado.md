# Evidência P6 — HTTP autenticado e migration

Ambiente temporário: MySQL 8.4 em `127.0.0.1:3309`, banco `ism_isolated`, API em `127.0.0.1:8180`, com seed de demonstração. O container foi encerrado após a validação.

Resultados observados:

- login de `Manager`: sucesso;
- rota protegida sem token: HTTP 401;
- registry: 8 ferramentas listadas;
- criação de thread `general`: sucesso;
- mensagem composta de faturamento + estoque: sucesso, modo determinístico e evidência persistida;
- tentativa de enviar `restaurantId` ao executor: decisão `Denied`;
- carga concorrente: 10/10 requisições autenticadas de chat retornaram HTTP 200;
- banco: 1 thread e 3 mensagens persistidas durante o cenário;
- `__EFMigrationsHistory`: última migration `20261124010135_AddAssistantThreadSpecialistIdentity`.
