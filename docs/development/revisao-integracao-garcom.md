# Revisão local da integração do garçom — 12/11/2026

Branch revisada: `feat/frontend/integracao-garcom`, HEAD inicial `bcc7bca`.
O incremento específico do garçom foi comparado com seu ponto de partida,
`8de3969`: dez arquivos, concentrados no frontend e na documentação. As
correções desta revisão estão descritas abaixo.

## Ambiente recuperado

- O MySQL instalado no host (`mysqld.service`) ocupava a porta 3306. O serviço
  foi parado e o MySQL do Compose passou a usar a própria 3306. Não houve
  alteração permanente de porta nem desativação do serviço no boot.
- A migration de configurações estava parcialmente aplicada, sem registro no
  histórico do EF. Foi salvo um dump antes do reparo. As colunas adicionadas
  parcialmente foram removidas somente nas tabelas `users` e `restaurants`,
  ambas ainda vazias, e a migration foi executada novamente. Nenhuma migration
  versionada foi editada e nenhum volume foi apagado.
- As quinze migrations foram aplicadas e o seed local foi concluído. O banco
  usado é o volume da branch `feat-frontend-integracao-garcom`.

## Resultado dos testes

| Verificação | Resultado |
| --- | --- |
| `dotnet test ISM.sln --no-restore --verbosity quiet` no container backend | 59 unitários, 24 de integração e 1 de arquitetura aprovados |
| `npm run test:waiter` | 6 testes aprovados |
| `npm run test:system-alerts` | 2 testes de regressão aprovados |
| `node --experimental-strip-types tests/unit/navigation.test.ts` | aprovado |
| `npm run build` | build cliente/servidor aprovado novamente após as correções |
| Smoke HTTP contra backend e MySQL reais | 27 verificações aprovadas |
| `git diff --check` | aprovado |

Os testes de integração .NET usam o ambiente de testes da aplicação; o smoke
HTTP foi executado separadamente contra o Compose real. Ele cobriu login dos
quatro perfis, exigência de autenticação, cardápio/categorias, isolamento da
consulta por restaurante, permissões administrativas, bloqueio de criação
pelo chef, validação de mesa, preços oficiais, idempotência, rejeição de UUID
reutilizado com outro conteúdo, conflito de versão, preparo, entrega,
pagamento e cancelamento. Os pedidos exclusivos desse smoke foram removidos
ao final pela API administrativa.

### Navegador com dados reais

- Login de garçom e chef, incluindo redirecionamento de rota incompatível
  com o perfil, funcionou.
- Cardápio exibiu os seis pratos reais, preços e categorias. Busca por
  `salmao` encontrou `Salmão`; filtro de bebidas mostrou apenas as bebidas.
- A comanda `QA-UI-12NOV` foi montada pela interface com duas bruschettas,
  observação `QA - sem cebola` e total de R$ 56,00. Mesa, itens e observação
  foram preservados ao navegar ao cardápio e voltar.
- Com o backend parado propositalmente, o envio mostrou erro e preservou o
  carrinho. Após reiniciá-lo, o reenvio criou o pedido `#363` e limpou a comanda.
  Durante o envio, campos e botões ficaram desabilitados.
- O chef encontrou o pedido com quantidade, observação e valor corretos e o
  avançou pela interface de Enviado para Em preparo e Pronto. O garçom
  encontrou o mesmo pedido pronto ao entrar novamente.
- A ferramenta de navegador travou ao interagir com o `window.confirm` da
  entrega. Portanto, **o clique final dessa confirmação nativa não está
  homologado por esta automação**. A entrega foi executada pela API do
  garçom com a versão atual; o banco confirmou status Completed e versão 3.
  Sem atualização manual, o painel do garçom removeu o pedido dos ativos e
  passou de quatro para três pedidos ativos. O teste do composable também
  validou a chamada de entrega e a recuperação após conflito.
- Em 390 × 844, o carrinho, quantidade, observação e revisão ficaram acessíveis,
  sem transbordamento horizontal. A dimensão temporária foi restaurada.

O pedido `#363` fica identificado como teste no banco local para inspeção.
Atualização posterior: a confirmação nativa foi substituída por
`ConfirmationDialog`. No pedido de teste `#367`, Voltar e Escape preservaram
o pedido, e Confirmar entrega pela interface persistiu Completed com versão 3.
Esse pedido foi removido após a validação. A limitação de automação do clique
final descrita acima foi resolvida por essa substituição.

Esta revisão não representa homologação de todos os navegadores, volumes de
produção ou de todos os módulos que já existiam antes da branch.

## Falhas encontradas e corrigidas

1. **Notificações quebravam ao receber enums numéricos.** A API retorna
   `severity` e `alertType` como números, enquanto componentes usavam
   `toLowerCase` e `includes`. A adaptação passou a acontecer no serviço HTTP,
   inclusive nas respostas de marcar lida e dispensar. `Danger`/2 é traduzido
   para a severidade visual `Critical`. Dois testes cobrem o contrato real e
   as respostas por nome. Após a correção, os alertas reais renderizaram.
2. **Recarregamento de rota autenticada montava a tela de outro perfil antes
   da hidratação.** O layout usava `authReady` sem provedor e assumia `true`.
   Agora servidor e cliente começam com a mesma tela de verificação, e o
   conteúdo é liberado após a montagem, quando a identidade local já foi
   restaurada. O cardápio do garçom foi recarregado e conferido após o ajuste.

Essas falhas estavam em código compartilhado já presente no ponto de partida
da integração; não são alterações introduzidas pelo incremento do garçom.

## Arquitetura

O incremento segue a arquitetura atual do projeto:

- Páginas escolhem o componente apropriado ao perfil; `WaiterPanel` concentra
  apresentação, filtros e interações visuais.
- `useWaiter` coordena estado, consultas, envio, recuperação de erro e ciclo
  de vida do polling. Reutiliza `menuService`/`orderService`, que passam pelo
  cliente HTTP autenticado existente.
- O navegador envia identificadores, quantidades e observações; o backend
  determina preços/custos, permissões, restaurante, idempotência e transições.
- Controllers delegam a interfaces da Application; serviços usam contratos
  de repositório do Domain, implementados na Infrastructure. Application
  referencia Domain, e Domain não referencia as camadas externas.
- O teste de arquitetura existente verifica dependências de construtores de
  controllers. Ele passou, mas sozinho não prova todas as regras de Clean
  Architecture; a cadeia de pedidos também foi inspecionada no código.
- As correções desta revisão preservam esses limites: contrato externo
  normalizado no serviço HTTP e sincronização da apresentação no layout.

Não foi identificada violação bloqueante de arquitetura no incremento do
garçom. Há pontos para evolução: `WaiterPanel` tem cerca de 1.347 linhas e
pode ser dividido em componentes de cardápio, carrinho e acompanhamento;
o polling consulta todo o histórico a cada dez segundos, o que deve virar
consulta incremental/paginada antes de grandes volumes. O rascunho e seu UUID
continuam apenas na memória da navegação: recarregar a página perde o
rascunho e a proteção de reenvio dessa sessão. A tela do garçom não oferece
pagamento/cancelamento, embora a API suporte esses estados; isso já consta
do escopo documentado da branch.
