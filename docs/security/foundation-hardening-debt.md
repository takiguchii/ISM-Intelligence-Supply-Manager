# Dívidas de segurança da fundação

## JWT no navegador

O frontend mantém o JWT de acesso em `localStorage` para montar o cabeçalho
`Authorization: Bearer`. Isso o expõe a exfiltração caso exista uma falha XSS.

A migração recomendada é substituir esse fluxo por um cookie de sessão
`HttpOnly`, `Secure` e `SameSite`, com token de renovação revogável persistido
no servidor. A mudança exige revisar CORS/CSRF, o contrato de login e os
clientes de API; por isso não foi aplicada parcialmente nesta fase.

## Chave de IA por restaurante

`Restaurant.PhotoImportApiKey` ainda é armazenada no banco para suportar
configuração por restaurante. Os DTOs de leitura não retornam o segredo: só
expõem se existe chave e, quando aplicável, seus quatro últimos caracteres.

A evolução recomendada é cifrar o valor em repouso com chave gerenciada pelo
ambiente (ou secret manager), mantendo-o fora de logs, DTOs genéricos e
respostas de erro.
