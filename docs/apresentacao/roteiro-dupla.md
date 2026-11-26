# ISM — roteiro para Enzo e Alexandre

**Apresentação:** 02/12/2026, às 19h30. **Limite:** 10 minutos, sem perguntas. **Meta de ensaio:** 9min15s, com 45 segundos de margem para transições e imprevistos.

As falas abaixo são texto de palco. As instruções em itálico são ações e não devem ser lidas. Os tempos incluem navegação, pausas e troca de apresentador; são uma previsão, a confirmar em ensaio cronometrado. Não é necessário preencher todo intervalo falando.

## Mapa de tempo

| Tempo | Responsável | Conteúdo | Visual sugerido |
| --- | --- | --- | --- |
| 00:00–00:50 | Enzo | Problema | 1. Operação, estoque e custo desconectados |
| 00:50–01:35 | Enzo | Solução e público | 2. Operação → dados → decisão |
| 01:35–04:20 | Alexandre | Demonstração orientada a um caso | Produto aberto; Telegram preparado |
| 04:20–05:35 | Alexandre | Desenvolvimento técnico e confiança | 3. Interface/canal → aplicação → domínio → infraestrutura |
| 05:35–06:45 | Enzo | B2B e monetização | 4. Assinatura por restaurante; hipótese Pro |
| 06:45–07:45 | Alexandre | Validação e estágio atual | 5. Evidências e limites do MVP |
| 07:45–08:45 | Enzo | Próximos passos | 6. Estabilizar → pilotar → comercializar |
| 08:45–09:15 | Enzo e Alexandre | Fechamento | Marca ISM e proposta de valor |
| 09:15–10:00 | — | Reserva, sem conteúdo novo | — |

## 00:00–00:50 — Enzo: o problema

> Boa noite. Eu sou o Enzo e, junto com o Alexandre e nossa equipe, desenvolvi o ISM, Intelligence Supply Manager.
>
> Imaginem um restaurante em horário de pico: os pedidos estão chegando, a cozinha precisa se organizar e o gerente tem de saber se ainda há ingrediente suficiente. Ao mesmo tempo, o custo de compra pode mudar sem que o preço do prato acompanhe.
>
> Quando essas informações ficam espalhadas, o problema aparece tarde: falta produto, o atendimento perde ritmo ou a margem diminui sem ficar evidente. O desafio que escolhemos enfrentar é transformar os dados da operação em informação que ajude o responsável a agir.

*Pausa breve. Evitar estatísticas de desperdício, tamanho de mercado ou economia sem uma fonte ou medição própria.*

## 00:50–01:35 — Enzo: a solução

> O ISM é uma plataforma de gestão voltada a restaurantes. Ela reúne estoque, cardápio, fornecedores, pedidos e indicadores, com acessos próprios para cada função da equipe.
>
> Sobre essa base, o assistente conecta o gestor a especialistas em estoque, precificação e fornecedores. A ideia é facilitar três perguntas: o que está acontecendo, por que isso merece atenção e qual ação pode ser avaliada.
>
> O gestor também pode consultar informações pelo Telegram, usando a mesma conta e as mesmas permissões do sistema. O Alexandre vai mostrar um exemplo dessa jornada.

## 01:35–04:20 — Alexandre: demonstração

*O ambiente contém dados fictícios de demonstração. Enzo opera a navegação e o cronômetro enquanto Alexandre explica. Deixar as páginas e o chat do bot preparados antes de começar. Não cadastrar conta, gerar vínculo ou instalar modelo durante a apresentação.*

**01:35–02:05 — Indicadores.**

> Estamos em um restaurante de demonstração, com dados fictícios. O painel mostra receita de pedidos pagos nos últimos sete dias, margem bruta dos pratos, quantidade de pedidos pagos e itens em estoque crítico. Essa margem considera o custo dos pratos; não é lucro líquido do restaurante.

*Apontar o período. Não decorar valores: eles mudam com a data e os dados. Não comparar o total mensal do Telegram com o painel semanal como se fossem a mesma janela.*

**02:05–02:45 — Estoque e alerta.**

> Aqui temos um ingrediente abaixo do mínimo cadastrado. O alerta dá visibilidade ao problema, e o responsável pode conferir o saldo e avaliar a reposição. A decisão continua vinculada aos dados e às regras da operação.

*Abrir um alerta real do ambiente e seus detalhes. Se o salmão continuar com 6 kg e mínimo de 15 kg, pode usar esse exemplo; conferir no ensaio. Se não houver alerta, usar um exemplo previamente registrado e identificá-lo como registro.*

**02:45–03:30 — Telegram e orquestrador.**

> Fora dessa tela, o gestor consegue fazer uma pergunta pelo Telegram. Vou perguntar o faturamento. O canal encaminha a consulta ao mesmo assistente do ISM, e o orquestrador usa a ferramenta financeira autorizada. A resposta informa os valores e a origem dos dados.

*Enviar “Qual é o faturamento?”. Mostrar os tópicos em reais e a fonte FinanceOverview. Esperar no máximo 8 segundos; usar a contingência do guia se necessário. Não abrir outros chats pessoais.*

**03:30–04:20 — Consulta e conexão com a operação.**

> Pelo painel do bot também podemos consultar o cardápio e acompanhar pedidos. Aqui está o estado registrado no sistema. Na operação, garçom e cozinha têm áreas próprias: o pedido passa por recebimento, preparo, pronto e entrega, conforme o perfil autorizado.
>
> O valor dessa integração é dar acesso à informação do restaurante por outro canal, preservando os controles da aplicação.

*Clicar em Cardápio e mostrar uma resposta. Se houver tempo, consultar um pedido pelo ID real ensaiado. Para mostrar salão e cozinha, usar duas sessões distintas já autenticadas; no roteiro padrão, bastam a consulta e uma tela preparada. Não executar um ciclo completo adicional se ultrapassar 04:20.*

## 04:20–05:35 — Alexandre: desenvolvimento técnico

> No desenvolvimento, usamos Nuxt 3 com TypeScript na interface, uma API em .NET 8 e MySQL na persistência. O ambiente pode ser executado com Docker Compose.
>
> O backend segue uma separação em camadas: a API recebe a solicitação, a aplicação coordena o caso de uso, o domínio concentra as entidades e regras, e a infraestrutura implementa banco e integrações. Isso permite adicionar um canal como o Telegram reutilizando os serviços existentes.
>
> Um ponto central é a responsabilidade da inteligência artificial. Consultas reconhecidas usam ferramentas autorizadas e cálculos determinísticos. O modelo de linguagem participa da conversa; as regras de negócio e as permissões continuam no sistema. O fluxo de propostas prevê revisão antes de executar alterações permitidas.
>
> Os dados também são separados por restaurante, com autorização por perfil. O Telegram verifica o vínculo com um usuário ativo e seu restaurante. No ambiente demonstrado, o modelo roda localmente via Ollama. A evolução para nuvem faz parte dos próximos passos e depende da implantação e da configuração do provedor.

## 05:35–06:45 — Enzo: modelo de negócio e monetização

> Nosso modelo proposto é B2B: quem contrata é o restaurante, e quem usa são o gestor e a equipe. O consumidor atendido no salão é beneficiado indiretamente; ele não é o assinante da plataforma nesta proposta.
>
> A monetização prevista é uma assinatura recorrente por restaurante, com planos que acompanham o porte e o uso. O protótipo já tem Free, Pro e Enterprise. Como hipótese inicial, o Pro está cadastrado a 149 reais e 90 centavos por mês. Esse preço ainda precisa ser validado com clientes; o sistema de planos atual é demonstrativo e não processa pagamentos reais.
>
> Nossa proposta é começar com uma avaliação limitada e converter restaurantes que percebam valor no acompanhamento de estoque, custos e decisões. Antes da comercialização, precisamos medir disposição a pagar, custo da infraestrutura e da IA, suporte e retenção. A assinatura precisa ser sustentável para o produto e fazer sentido para a operação do cliente.

## 06:45–07:45 — Alexandre: evidências e estágio

> Hoje temos uma implementação funcional em ambiente de desenvolvimento, com testes automatizados no backend e no frontend. A integração com o Telegram foi verificada com mensagens reais: vínculo, consultas, conversa, encaminhamento ao orquestrador, alertas e desconexão.
>
> Também verificamos o isolamento de restaurante, permissões, expiração do vínculo e rejeição de remetentes indevidos. Esses controles são parte da qualidade do fluxo, além do resultado visual.
>
> Ainda há etapas para uma operação comercial: cobrança real, implantação e monitoramento, e integração definitiva do caixa com a quitação no servidor. Conectores que aparecem como indisponíveis, como os de marketplaces, permanecem no planejamento. Nosso compromisso é demonstrar o que já existe e evoluir a partir de validação real.

*Se forem citar números de testes, conferir a versão da entrega: na revisão de 26/11 foram 279 testes de backend e 32 de frontend. Quantidade não equivale a cobertura percentual nem certificação de produção.*

## 07:45–08:45 — Enzo: próximos passos

> Até sexta-feira, 27 de novembro, nossa prioridade é estabilizar o escopo da entrega: revisar o pull request, consolidar a integração e ensaiar os fluxos que vamos demonstrar. Antes da apresentação, vamos manter uma versão conhecida e preparar uma gravação de apoio.
>
> Depois dessa etapa acadêmica, propomos um piloto acompanhado com restaurantes. Queremos observar se os alertas são úteis, se os dados são confiáveis para a rotina e quais recursos justificam a assinatura.
>
> Em paralelo, os passos técnicos são consolidar a implantação em nuvem, medir uso e custo da IA, estruturar monitoramento e concluir os fluxos financeiros transacionais. As integrações comerciais entram conforme prioridade e viabilidade do piloto. O objetivo é aprender com uso real antes de ampliar o produto.

## 08:45–09:15 — fechamento

**Alexandre:**

> O ISM conecta a operação a uma base de dados e a ferramentas que ajudam a interpretar o que exige atenção.

**Enzo:**

> Nossa proposta é que o responsável pelo restaurante consiga entender o cenário e avaliar a próxima ação com mais clareza. É essa ponte entre operação, informação e decisão que estamos construindo. Obrigado.

## Cortes para recuperar tempo

Se o relógio chegar a 04:00 durante a demonstração, pular o detalhe de pedido e concluir a demo. Se chegar a 07:45 antes dos próximos passos, usar: “Agora vamos estabilizar a entrega, conduzir um piloto com restaurantes e validar preço, custo de IA e uso real antes de ampliar a operação comercial.” Preservar sempre problema, solução, B2B/monetização, técnica e próximos passos. Encerrar até 09:30; a reserva não é uma seção extra.

Consultar [preparação e negócio](preparacao-e-negocio.md) para ensaio, contingência, preços, evidências do repositório e respostas à banca.
