# ISM — roteiro com participação dos seis integrantes

**Equipe:** Enzo, Alexandre, Raul, Flávio, Augusto e Eduardo. **Data:** 02/12/2026, 19h30. **Limite:** 10 minutos, sem perguntas. **Meta:** 9min15s e 45 segundos de reserva.

Esta versão substitui o roteiro da dupla; não deve ser acrescentada ao final dele. As quatro participações curtas somam 1min30s. Enzo e Alexandre conduzem os outros 7min45s, incluindo a demonstração. As falas curtas foram associadas a contribuições identificadas no Git, sem tratar autoria de commit como medida de esforço total. Confirmar a divisão com a equipe no ensaio.

## Mapa de tempo

| Tempo | Pessoa | Conteúdo |
| --- | --- | --- |
| 00:00–00:45 | Enzo | Problema |
| 00:45–01:20 | Enzo | Solução e público |
| 01:20–01:45 | Raul | Dashboard e área do garçom — 25s |
| 01:45–02:10 | Augusto | Estoque — 25s |
| 02:10–02:35 | Flávio | Cardápio, precificação e CMV — 25s |
| 02:35–02:50 | Eduardo | Funcionários — 15s |
| 02:50–05:00 | Alexandre | Demonstração |
| 05:00–06:10 | Alexandre | Desenvolvimento técnico |
| 06:10–07:20 | Enzo | B2B e monetização |
| 07:20–08:00 | Alexandre | Validação e estágio |
| 08:00–08:55 | Enzo | Próximos passos |
| 08:55–09:15 | Enzo | Fechamento |
| 09:15–10:00 | — | Reserva |

*Manter a ordem dos participantes e o microfone preparado. Evitar agradecimentos e reapresentações a cada troca. Nos quatro blocos curtos, Enzo navega para a tela indicada, sem executar cadastros.*

## 00:00–00:45 — Enzo

> Boa noite. Somos a equipe do ISM, Intelligence Supply Manager.
>
> Em um restaurante, o atendimento precisa acompanhar a cozinha, o estoque precisa sustentar o cardápio e os preços precisam acompanhar os custos. Quando essas informações ficam espalhadas, o gestor pode perceber tarde uma falta de ingrediente ou uma redução na margem.
>
> Nosso projeto parte desse problema: reunir os dados da operação e ajudar a identificar o que exige atenção, para que o responsável consiga avaliar a próxima ação.

## 00:45–01:20 — Enzo

> O ISM é uma plataforma de gestão para restaurantes, com estoque, cardápio, fornecedores, pedidos e indicadores. O assistente conecta o gestor a especialistas que consultam os dados e as regras do sistema. Pelo Telegram, ele também consegue conversar e consultar informações com suas permissões.
>
> Antes da demonstração, nossa equipe vai apresentar brevemente algumas das áreas que ajudou a desenvolver. Raul.

## 01:20–01:45 — Raul

> Contribuí com o dashboard e com as telas iniciais da área do garçom. No painel, o objetivo é facilitar a leitura dos indicadores. Na operação do salão, é organizar o atendimento e a consulta a pedidos e cardápio. Essas interfaces foram evoluindo junto com a integração aos serviços do sistema.

*Visual: dashboard e uma imagem ou aba já aberta do garçom. Não afirmar que todas as versões iniciais já consumiam a API.*

## 01:45–02:10 — Augusto

> Minha contribuição foi na tela de estoque. Ela organiza os produtos e seus saldos, facilitando a identificação dos itens que precisam de atenção. Para o restaurante, isso aproxima a informação cadastrada da rotina de conferência e reposição. Essa base também é consultada pelos alertas e pelo assistente.

*Visual: estoque, destacando saldo e mínimo sem alterar dados.*

## 02:10–02:35 — Flávio

> Trabalhei no cadastro de cardápio e na base de precificação com CMV, que é o custo da mercadoria vendida. Relacionar o custo dos ingredientes ao prato permite avaliar a margem e simular preços com critérios definidos. A proposta é apoiar a decisão do gestor com dados verificáveis.

*Visual: prato com custo e preço. Não chamar margem bruta de lucro líquido.*

## 02:35–02:50 — Eduardo

> Contribuí com a tela de funcionários e suas operações de cadastro. Essa área organiza a equipe do restaurante e dá suporte à gestão dos acessos, junto com as permissões verificadas pelo backend.

*Visual: funcionários. Alexandre assume sem uma nova introdução ao projeto.*

## 02:50–05:00 — Alexandre: demonstração

**02:50–03:15 — Indicadores.**

> Neste restaurante de demonstração, os dados são fictícios. O painel reúne receita de pedidos pagos dos últimos sete dias, margem bruta dos pratos e itens críticos. Assim, o gestor começa por uma visão do cenário.

**03:15–03:45 — Alerta.**

> Este ingrediente está abaixo do mínimo cadastrado. O alerta evidencia o problema, e o responsável pode conferir os dados para avaliar a reposição.

*Abrir o alerta e seus detalhes. Se não houver um alerta atual, usar um registro identificado.*

**03:45–04:30 — Telegram.**

> No Telegram, vou perguntar o faturamento. A consulta passa pelo assistente do ISM e pela ferramenta financeira autorizada. A resposta apresenta os valores e a origem dos dados, preservando o contexto do restaurante e do usuário.

*Enviar “Qual é o faturamento?”. Mostrar resposta e fonte. Não comparar o resultado mensal com o painel semanal. Após 8 segundos sem resposta, usar a contingência.*

**04:30–05:00 — Cardápio e acompanhamento.**

> Pelo painel do bot também consultamos o cardápio e acompanhamos pedidos. O gestor acessa a informação da operação sem precisar permanecer na tela administrativa. Garçom e cozinha continuam usando suas áreas e seus perfis próprios.

*Clicar em Cardápio. Só consultar um pedido se sobrar tempo. Enzo mantém a navegação e o cronômetro.*

## 05:00–06:10 — Alexandre: técnica

> A interface usa Nuxt 3 e TypeScript. O backend usa .NET 8, com MySQL e execução do ambiente por Docker Compose.
>
> Organizamos o backend em API, aplicação, domínio e infraestrutura. Essa separação permite reutilizar os casos de uso no Telegram, em vez de duplicar as regras no bot.
>
> O assistente combina conversa com ferramentas autorizadas. Consultas reconhecidas usam dados e cálculos determinísticos; o modelo de linguagem não recebe liberdade para modificar o banco por texto. Propostas de alteração seguem o fluxo de revisão previsto na aplicação.
>
> O acesso é controlado por perfil e restaurante. O vínculo do Telegram é individual e confirmado pela conta esperada. Hoje demonstramos o modelo local via Ollama; a implantação em nuvem é uma evolução planejada, que precisa preservar esses contratos e controles.

## 06:10–07:20 — Enzo: negócio

> O modelo proposto é B2B: o restaurante contrata a plataforma para uso do gestor e da equipe. A monetização prevista é uma assinatura por restaurante, com limites adequados ao porte e ao uso.
>
> O protótipo tem os planos Free, Pro e Enterprise. O Pro está cadastrado a 149 reais e 90 centavos por mês como hipótese inicial. Ainda vamos validar esse valor com clientes; a assinatura implementada é demonstrativa e não faz cobranças reais.
>
> Nossa recomendação é começar com uma avaliação limitada e medir o valor percebido antes da conversão. Para sustentar o negócio, precisamos acompanhar custo da IA, infraestrutura, suporte, aquisição e retenção. Queremos que a assinatura seja justificada pela utilidade do sistema na rotina do restaurante.

## 07:20–08:00 — Alexandre: validação

> Os fluxos têm testes automatizados e validação local. No Telegram, verificamos mensagens reais, consultas, orquestrador, alertas e desconexão, além de controles de identidade e isolamento.
>
> Temos uma implementação funcional de desenvolvimento. Cobrança real, consolidação da nuvem e quitação transacional do caixa ainda são etapas da evolução comercial. Os conectores sinalizados como indisponíveis também não são apresentados como integrações prontas.

## 08:00–08:55 — Enzo: próximos passos

> Até sexta-feira, 27 de novembro, vamos priorizar estabilização, revisão e fechamento do escopo. Para a apresentação, teremos uma versão ensaiada e uma gravação de apoio dos fluxos reais.
>
> Depois, propomos um piloto acompanhado com restaurantes para avaliar utilidade dos alertas, qualidade dos dados e disposição a pagar. A evolução técnica inclui implantação em nuvem, monitoramento, controle do custo da IA e conclusão dos fluxos financeiros no servidor.
>
> A prioridade das integrações comerciais deve vir desse aprendizado. Assim, o projeto avança com evidências de uso e com um escopo viável para a equipe.

## 08:55–09:15 — Enzo: fechamento

> O ISM conecta operação, informação e decisão. Nossa proposta é ajudar o restaurante a perceber o que merece atenção e a avaliar a próxima ação com dados mais claros. Esse é o produto que nossa equipe está construindo. Obrigado.

## Critério para a divisão de falas

| Integrante | Evidência no histórico consultado | Tema atribuído |
| --- | --- | --- |
| Enzo | Cozinha, notificações, validações e integração Telegram: `eb34ae7`, `970270c`, `c01a243`, `9a0dd2e` | Produto, integração, negócio e condução |
| Alexandre | Especialistas, orquestração, assistente e evolução da interface: `85e06dc`, `e8a8d24`, `6705cc6`, `20e2676` | Demonstração, arquitetura e IA |
| Raul | Dashboard e telas do garçom: `9f4bef0`, `973e028`, `0ff3755`, `9411b2e` | Indicadores e operação do salão |
| Augusto | Tela de estoque: `80860a5`, `339bb08`, `712427d` | Estoque |
| Flávio | Cardápio e base de CMV: `21064c1`, `9431816` | Cardápio e precificação |
| Eduardo | Tela e serviço de funcionários: `d4312bf` | Funcionários |

Identificação baseada nos autores e arquivos dos commits. Refatorações posteriores e trabalho em conjunto podem alterar a distribuição real de responsabilidades. Não medir participação por quantidade de commits. Os nomes de palco são os seis nomes informados pela equipe.

## Recuperação de tempo

Se a sequência dos quatro participantes passar de 02:50, reduzir a demonstração a um alerta e uma resposta do Telegram. Ao chegar a 08:30, concluir os próximos passos em uma frase e passar ao fechamento. Não eliminar a fala de um integrante para compensar navegação lenta. Consultar [preparação e negócio](preparacao-e-negocio.md).
