# Preparação da apresentação e proposta de negócio

Documento de apoio ao ensaio de 02/12/2026, às 19h30. A apresentação tem dez minutos; perguntas ficam fora desse tempo. Os roteiros propõem 9min15s de conteúdo com 45 segundos de reserva. Tempos são estimativas editoriais, não uma gravação cronometrada.

## Mensagem principal

**ISM conecta a operação do restaurante a dados e ferramentas que ajudam o gestor a avaliar a próxima ação.**

A história da apresentação deve seguir um caso concreto: indicador → ingrediente crítico → consulta com fonte → acesso pelo Telegram. Isso evidencia integração e utilidade sem transformar a apresentação em uma visita a todos os cadastros.

## Produto observado no repositório

| Área | Implementação e interpretação correta | Fonte para conferir |
| --- | --- | --- |
| Estoque | Produtos, saldos, mínimos e alertas; qualidade da análise depende dos registros | `frontend/pages/estoque.vue`; `backend/src/ISM.Application/Services/Stock/` |
| Cardápio e precificação | Pratos, ingredientes, custo/CMV, propostas e revisão; margem dos pratos não é lucro líquido | `backend/src/ISM.Application/Services/Menu/`; `docs/agents/architecture.md` |
| Salão e cozinha | Áreas de garçom e chef conectadas aos serviços de pedidos; transições e acessos por perfil | `docs/development/integracao-garcom.md`; `docs/development/integracao-cozinha.md` |
| Indicadores | Receita, custo e quantidade de pedidos pagos em sete dias; estoque crítico atual; distribuição semanal usa 28 dias | `backend/src/ISM.Application/Services/Dashboard/DashboardService.cs` |
| Financeiro | Registros e consultas próprios; faturamento mensal do assistente usa pedidos pagos | `backend/src/ISM.Application/Services/Finance/FinanceService.cs` |
| Assistente | Contexto persistido, roteamento para ferramentas autorizadas, evidências e fluxo de propostas | `docs/agents/architecture.md`; `AssistantService.cs`; `AgentIntentRouter.cs` |
| Telegram | Vínculo individual de gerente/admin, consultas, conversa e alertas com adesão explícita | `docs/development/telegram.md` |
| Importação | Prévia e revisão antes da confirmação de dados; formatos e extração dependem do fluxo configurado | `docs/mapeamento-recepcao-dados-agentes.md` |
| SaaS e perfis | Dados por restaurante, autenticação, papéis e limites de plano | `docs/security/perfis-e-autorizacao.md`; `docs/development/settings.md` |
| Caixa | Operação local no navegador; ainda não quita pedidos no servidor de maneira transacional | `docs/development/operacao-caixa.md` |
| Marketplaces e adquirência | Ícones e painéis indisponíveis não representam conectores operacionais | `frontend/components/integrations/`; `frontend/pages/integracoes.vue` |
| Cobrança | Alteração demonstrativa de planos e histórico; sem gateway de pagamento real | `backend/src/ISM.Infrastructure/Services/DemoSubscriptionService.cs` |
| Nuvem | A abstração do provedor e as configurações ajudam a evolução; migração completa exige implantação, segurança e validação | `docs/agents/architecture.md`; `docs/security/foundation-hardening-debt.md` |

As telas evoluíram por múltiplos autores. Uma função presente no repositório não significa que foi exaustivamente homologada em todos os ambientes. Não apresentar dados seed como clientes, receita da startup ou prova de resultado operacional.

## Avaliação da monetização

**Recomendação:** manter a assinatura B2B por restaurante como hipótese principal. A estrutura já implementada de planos e limites é coerente com esse modelo. O preço e o conteúdo comercial de cada plano ainda precisam de validação de uso e custo.

Os valores abaixo são os cadastrados no seed, não preços comprovados por pesquisa de mercado ou contratos. Uma base já existente pode ter outros valores, pois o seed só cria planos quando a tabela está vazia.

| Plano do seed | Mensalidade | Limites: usuários / pratos / produtos / categorias | Leitura para a proposta |
| --- | --- | --- | --- |
| Free | R$ 0 | 2 / 10 / 20 / 3 | Avaliação de entrada; definir duração e orçamento de IA antes de oferecer comercialmente |
| Pro | R$ 149,90 | 10 / 100 / 200 / 15 | Hipótese de oferta principal para testar em piloto |
| Enterprise | R$ 499,90 | 100 / 1.000 / 2.000 / 100 | Hipótese para operações maiores; escopo de redes e suporte precisa ser definido |

O texto do seed descreve Enterprise como “ilimitado”, mas os campos têm limites. Corrigir essa promessa na definição comercial antes de divulgar; não usar “ilimitado” no palco. O sistema também cadastra trial para o restaurante demo, mas isso não define sozinho uma política comercial de avaliação para todos os clientes.

Uma configuração inicial para discutir com o grupo é: avaliação por tempo definido, Pro como oferta principal e Enterprise sob escopo acordado. Os valores atuais podem permanecer como referência interna até o piloto. Não acrescentar compra de créditos, comissões e serviços extras ao pitch sem uma justificativa validada.

### O que validar antes de vender

| Pergunta | Evidência a coletar no piloto | Consequência para o modelo |
| --- | --- | --- |
| O cliente usa e entende o produto? | Ativação, uso semanal, dúvidas recorrentes, conclusão dos fluxos essenciais | Ajustar onboarding e escopo |
| Quais decisões melhoram com o sistema? | Alertas revisados, ações tomadas, erros evitados com registro; comparar uma linha de base | Validar valor, sem atribuir causalidade a qualquer variação |
| O preço é aceitável? | Entrevistas e proposta concreta de contratação | Manter ou revisar oferta e preço |
| Quanto custa atender cada restaurante? | Uso de IA, infraestrutura, armazenamento, suporte e tarifas efetivas | Definir limites e margem de contribuição |
| O cliente continua pagando? | Conversão de avaliação em contrato e retenção de coortes | Avaliar recorrência e priorizar melhorias |

**Conta ilustrativa:** dez restaurantes pagando R$ 149,90 representariam R$ 1.499,00 mensais de receita bruta recorrente. É uma simulação, sem clientes reais implícitos. Não é lucro: ainda há infraestrutura, IA, suporte, tributos e outros custos. Pode ficar nas respostas à banca, sem ocupar o roteiro principal.

O modelo atual não comprova medição de consumo de IA nem cobrança por uso. Quotas, métricas de custo e política de excedentes são trabalho futuro. Para o piloto, uma planilha de custos medidos já ajuda a testar a viabilidade antes de automatizar o billing.

## Sugestões para a tela de indicadores

Priorizar clareza da decisão e significado dos números, mantendo paleta e componentes existentes.

1. **Evitar tendências sem comparação.** A API retorna `delta = "—"` e uma direção fixa, sem calcular variação entre períodos. O cartão foi ajustado para ocultar a seta e o selo quando não existe comparação. Uma evolução posterior pode calcular janelas equivalentes e tratar base anterior zero.
2. **Explicitar os períodos.** Receita e pedidos usam sete dias; estoque é uma posição atual; média por dia da semana usa 28 dias. Mostrar essas referências perto de cada gráfico. Um seletor de período só deve ser apresentado como funcional depois de conectado ao backend.
3. **Ajudar a agir a partir do alerta.** Os cartões já oferecem detalhes e conversa. Uma melhoria posterior é ordenar por prioridade e mostrar “qual evidência”, “o que conferir” e “abrir item”, evitando outro bloco de texto grande na página.
4. **Informar atualização e procedência.** Exibir horário da última consulta bem-sucedida e distinguir erro de atualização de ausência de dados. Não mostrar uma receita de zero quando houve falha na API.
5. **Manter os conceitos corretos.** “Margem bruta dos pratos” deve ter ajuda explicando custos considerados e que não representa lucro líquido. “Pratos mais pedidos” conta unidades; deixar isso claro no eixo ou legenda. Não inventar um indicador de economia gerada pela IA sem medição.

Para a apresentação, a combinação de quatro KPIs, três cartões de agentes e um caso concreto é suficiente. Uma reformulação ampla perto do fechamento traz mais trabalho de regressão do que benefício ao roteiro.

## Preparação da demonstração

### Até sexta-feira, 27/11

- Revisar e integrar a branch conforme o fluxo do grupo; executar a regressão na versão integrada. Ausência de conflito Git não prova compatibilidade funcional.
- Definir uma versão de entrega e evitar novas funções grandes antes da apresentação.
- Escolher um restaurante demo e um alerta verdadeiro desse ambiente; conferir os IDs do produto, prato e pedido usados no ensaio.
- Validar o fluxo salão → cozinha → entrega em duas sessões, sem depender de troca de conta no palco.
- Preparar o bot com um usuário autorizado e confirmar a resposta do assistente. Vínculos temporários de QA são desconectados ao final; conectar a conta de apresentação separadamente antes do evento.

### Entre 28/11 e 01/12

- Ensaiar as duas versões com cronômetro. Objetivo: terminar entre 9min e 9min15s, incluindo trocas, cliques e silêncio de espera.
- Conferir nomes e falas com todos os integrantes; testar a troca de microfone na versão de equipe.
- Gravar uma demonstração curta da mesma versão do produto e guardar localmente imagens da resposta do bot e do alerta. Identificar material gravado como tal.
- Conferir o equipamento e a conexão que serão usados. Se o desenvolvimento mudar após a gravação, conferir se o material ainda corresponde à versão apresentada.

### Em 02/12, antes das 19h30

- Iniciar banco, API, frontend e provedor de IA; conferir saúde da aplicação e uma pergunta curta. Não instalar dependências ou baixar modelo no palco.
- Usar apenas um processo de polling para o bot. Desenvolvimento local e servidor em nuvem não devem disputar o mesmo token.
- No Fedora, conferir a ponte local Docker → Ollama se essa for a configuração escolhida; o serviço transitório de desenvolvimento precisa ser iniciado novamente após reinício da máquina.
- Deixar Telegram vinculado, alertas conforme o ensaio e páginas prontas. Ocultar chats pessoais e notificações; usar apenas dados fictícios na projeção.
- Testar tamanho de fonte, contraste e leitura no projetor. Enzo acompanha o cronômetro; Alexandre conduz a demo, conforme combinado.

### Contingência: limite de oito segundos

Se uma consulta não responder em até oito segundos, não insistir nem depurar na frente da banca. Dizer: “Para manter o tempo, vou mostrar o registro desta mesma consulta na versão que ensaiamos.” Exibir o registro ou vídeo local, identificado como gravação, e seguir a explicação.

Sem Telegram ou internet, a demo local pode continuar se o ambiente estiver disponível. Sem Ollama, as consultas determinísticas reconhecidas podem continuar; não prometer conversa livre com IA nesse estado. Sem ambiente local, usar a gravação completa. Um vídeo de reserva ainda precisa ser produzido pela equipe; este roteiro não significa que ele já existe.

## Respostas curtas para a banca

**Qual é o diferencial?** “Nossa proposta é aproximar operação, dados e decisão num fluxo com contexto e evidências. Vamos validar esse valor em piloto; ainda não temos uma comparação de desempenho com concorrentes.”

**É B2B ou B2C?** “B2B: o restaurante contrata e a equipe usa. O consumidor final não é o assinante do modelo proposto.”

**Já há clientes e faturamento?** “Os números mostrados são do restaurante fictício de demonstração. Assinatura e preços ainda são hipóteses; não apresentamos receita comercial validada.”

**A IA pode inventar um valor ou alterar o banco?** “O roteamento reconhecido usa ferramentas e dados determinísticos com autorização. A conversa pode ter limitações do modelo, por isso fonte, validação e revisão continuam necessárias. Alterações seguem os casos de uso e controles da aplicação.”

**Tudo já está na nuvem?** “O ambiente validado aqui usa Ollama local. A evolução para nuvem precisa de implantação e testes; o Telegram reutiliza os contratos do assistente, sem fixar um endereço local de especialista no canal.”

**E segurança e privacidade?** “Há isolamento por restaurante, papéis, autenticação e confirmação do vínculo do Telegram. Ainda precisamos concluir a avaliação operacional de produção e as pendências registradas na documentação. Esses controles não são uma certificação nem uma declaração de conformidade integral.”

**Como controlar o custo da IA?** “Pretendemos medir consumo e custo por restaurante e definir limites sustentáveis. O cadastro de planos existe; a medição e a cobrança desse consumo ainda precisam ser implementadas e validadas.”

**O caixa já alimenta o financeiro?** “A operação atual de caixa é local e valida o pedido antes do recebimento local, mas ainda não quita o pedido no backend. A consolidação transacional está nos próximos passos.”

**Há iFood, 99Food e Stone funcionando?** “Esses painéis estão sinalizados como indisponíveis. A integração demonstrada neste recorte é a do Telegram; as demais dependem de implementação e configuração próprias.”

**O que pretendem medir no piloto?** “Ativação e uso, utilidade dos alertas, qualidade dos dados, tempo observado para executar tarefas, disposição a pagar e custo de atendimento. Ainda não prometemos um percentual de economia.”

## Material visual sugerido

Seis apoios bastam: problema; solução e jornada; arquitetura e confiança; B2B e assinatura; evidências e estágio; próximos passos. A demonstração entra depois da solução. Usar frases curtas e uma imagem ou diagrama por apoio. O roteiro não inclui a criação de um deck; o conteúdo acima serve como guia para montá-lo.
