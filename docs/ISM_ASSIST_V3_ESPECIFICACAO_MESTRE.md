# ISM ASSIST V3 — ESPECIFICAÇÃO MESTRE DE ENGENHARIA, EXPERIÊNCIA E ANIMAÇÃO

**Produto:** ISM — Intelligence Supply Manager
**Área abrangida:** painel administrativo, alertas, três agentes e chat de IA já desenvolvido
**Natureza:** documento de implementação de produto e critérios de aceite, a ser lido integralmente por agente de código
**Versão:** 3.0 — 11/11/2026
**Status:** especificação de destino; não representa atestado de implementação
**Princípio central:** *o assistente vive nas interfaces que já existem; ele não cria outra dashboard dentro delas.*

---

# 0. CONTRATO INEGOCIÁVEL DE RESULTADO

## 0.1 Resultado esperado em uma frase

Substituir os robôs 3D/de corpo inteiro e os elementos grandes/invasivos por **três cabeças 2D originais, pequenas e expressivas**, cada uma associada ao respectivo agente **Estoque / Precificação / Fornecedores**, e conectá-las de verdade aos alertas, ao sino, às páginas contextuais e ao chat do ISM, com animações sutis, reações baseadas em dados e testes reais.

## 0.2 O que deve ser PRESERVADO

- Navbar, sidebar, cabeçalho, KPIs, gráficos, espaçamentos gerais, tema claro/escuro, tipografia, tabelas, autenticação, roteamento e funcionalidades administrativas existentes.
- Local da seção **“Insights e alertas dos agentes”** na dashboard e **exatamente três cards-resumo**, um por especialista.
- A mesma aparência geral das **três referências visuais aprovadas**, alterando apenas o necessário para torná-las funcionais, responsivas e acessíveis.
- O botão discreto de **Chat** no canto inferior direito, **fechado por padrão**; ele abre o chat lateral implementado anteriormente.
- A integração real com backend, Ollama e histórico contextual produzida nas tarefas anteriores, naquilo que estiver correto.
- Os cálculos determinísticos dos três agentes e as regras de negócio existentes. O mascote é representação visual; não refaz cálculos nem toma decisões autônomas.
- O fluxo de notificação existente, com leitura/descarte persistidos. Não criar duas fontes concorrentes de alertas.

## 0.3 O que deve ser REMOVIDO/CORRIGIDO

- Toda representação do mascote com **tronco, braços, pernas, mãos, fones enormes, tablet, objetos na mão, textura 3D ou aparência de boneco de corpo inteiro**.
- Mascotes estáticos e inexpressivos que só mudam de imagem quando a página recarrega.
- Reações com looping agressivo, flashing, bordas neon fortes, animações de fundo ou efeitos que competem com os dados.
- Cards que se multiplicam pelo número de alertas na dashboard, “megacomponente IA” central, banners duplicados e interfaces que parecem outro produto.
- Dados simulados exibidos como verdade (valores, entregas previstas, preços, percentuais, datas), botões que dão sucesso sem operação real, notificações antigas reanunciadas como novas.
- Duplicações de polling, listeners globais sem limpeza, múltiplas instâncias de chat, requisições Ollama disparadas por animações e dependências grandes introduzidas apenas para desenhar olhos.

## 0.4 Fora do escopo

**Não desenvolver agora:** página de configuração dos agentes, garçom, cozinha, caixa, câmera/OCR/fotos fiscais, áudio, assistente de voz, app desktop, notificações do sistema operacional, novo mecanismo de precificação, nova infraestrutura de LLM, integração com serviços externos.

Caso algo já exista, não excluir; apenas não modificar fora do necessário.

## 0.5 Referências visuais obrigatórias

O pacote acompanha:

1. `referencias-visuais/01_dashboard_agentes_2d.png` — dashboard, cards e cabeças 2D.
2. `referencias-visuais/02_notificacao_inteligente_aberta.png` — sino expandido, balão e agente de estoque.
3. `referencias-visuais/03_alerta_na_tela_estoque.png` — página de estoque, banner contextual e mascote 2D.

**Ordem de precedência em caso de divergência:** requisitos deste documento > comportamento real e segurança > referências de imagem > implementações visuais anteriores. Imagens são referência visual, **não fontes de verdade de dados**. Números, nomes e porcentagens presentes nas imagens são exemplos de layout e somente poderão aparecer em produção se vierem do banco.

---

# 1. DIAGNÓSTICO OBRIGATÓRIO DO ESTADO ATUAL

> **Advertência:** o ZIP do ISM examinado para a concepção inicial é uma versão anterior à tarefa longa executada no notebook. Caminhos abaixo foram vistos nesse ZIP, mas podem ter mudado. O executor TEM de inspecionar o working tree atual e seu histórico antes de editar.

## 1.1 Stack identificada na base anterior

| Camada | Base previamente inspecionada | Política nesta entrega |
|---|---|---|
| Frontend | Nuxt 3, Vue 3, TypeScript | Reaproveitar; preferir SVG inline + CSS + Composition API |
| Estilo | Tailwind CSS 3, Element Plus | Usar tokens/classes existentes; não trocar design system |
| Estado | Pinia, composables Vue | Um estado coordenador de alertas/reação por escopo de usuário |
| Backend | .NET 8, camadas API/Application/Domain/Infrastructure | Manter separação; mudanças aditivas e mínimas |
| Banco | MySQL + EF Core | Preservar schema e migrations existentes |
| Agentes | `StockAgent`, `SupplierAgent`, `PricingCmvService` | Fontes de decisões, não substituídas por LLM |
| Alertas | `SystemAlert`, endpoint `/api/system/alerts` | Fonte de verdade para eventos e lidos/descartados |
| Worker | `AgentsBackgroundScheduler` (tick de 5 minutos na base) | Nunca duplicar scheduler |
| Conversação | Implantada na tarefa noturna, não presente no ZIP inicial | **Auditar implementation atual** e integrar sem recomeçar |

## 1.2 Caminhos base identificados (confirmar novamente)

- `frontend/pages/index.vue`: dashboard administrativa real na versão inicial.
- `frontend/pages/dashboard.vue`: na base inicial, redirecionava para `/`; não presumir ser dashboard real.
- `frontend/components/dashboard/AiInsightCard.vue`: cards anteriores dos agentes.
- `frontend/components/layout/NotificationDrawer.vue`: central do sino.
- `frontend/composables/useDashboardMetrics.ts`: montagem de KPIs e insights.
- `frontend/layouts/default.vue`: shell do aplicativo e localização candidata para um único launcher global.
- `frontend/services/modules/system/systemService.ts`: chamadas de alertas.
- `frontend/services/api/client.ts`: cliente HTTP com autenticação.
- `frontend/components/layout/AppNavbar.vue` ou equivalente atual: sino; **não redesenhar navbar**.
- `backend/src/ISM.API/Controllers/System/SystemAlertsController.cs`: alertas.
- `backend/src/ISM.API/HostedServices/AgentsBackgroundScheduler.cs`: processamento periódico.
- `backend/src/ISM.Application/Services/Stock/StockAgent.cs`.
- `backend/src/ISM.Application/Services/Suppliers/SupplierAgent.cs`.
- `backend/src/ISM.Application/Services/Menu/PricingCmvService.cs`.
- `backend/src/ISM.Domain/Modules/System/Enums/SystemAlertType.cs`.

## 1.3 Tipos antigos de alerta que devem ser mapeados

| Tipo legado | Agente | Semântica |
|---|---|---|
| `StockRuptureRisk48h` | Estoque | Risco próximo de ruptura |
| `StockBelowMinimum` | Estoque | Quantidade abaixo do mínimo |
| `SupplierPriceSpike15Percent` | Fornecedores | Alta de preço registrada |
| `SupplierBetterAlternative` | Fornecedores | Alternativa potencialmente mais vantajosa |
| `DishCmvDefasagem` | Precificação | Custo/preço ou margem requer atenção |

**Não presumir** que todos os tipos têm o mesmo formato de `payload`, `referenceEntityId`, data ou severidade. Inspecionar mapeamentos, modelos e casos de uso reais. Adicionar adaptação tipada, tolerante a registros legados.

## 1.4 Primeiro diagnóstico do executor

Antes de iniciar a codificação, produzir um relatório com:

1. Nome da branch atual, upstream, último commit e arquivos sujos.
2. Relação dos componentes de IA presentes agora e quem os renderiza.
3. Capturas atuais da dashboard, da notificação e da página de estoque quando ferramenta de browser estiver disponível.
4. Fluxos reais de dados do estoque, precificação e fornecedores até cada card.
5. Fluxo real do chat, thread, memória, autorização, provider Ollama e tratamento de erro.
6. Locais exatos em que hoje existe robô errado, ilustração 3D, SVG, GIF, CSS de movimento ou imagens estáticas.
7. Decisão explícita de **o que reutilizar, o que refatorar e o que remover**, sem eliminar lógica de negócio.
8. Baseline de build/test existente, inclusive falhas anteriores à alteração.

Registrar esse diagnóstico em log de execução/arquivo de acompanhamento apropriado. Não confundir defeito preexistente com regressão.

---

# 2. CONCEITO DE PRODUTO E LINGUAGEM VISUAL

## 2.1 Personalidade do mascote

O ISM Assist é uma interface de atenção e análise: **simpático, discreto, atento e útil**. Não deve parecer infantil a ponto de reduzir credibilidade. Expressões curtas substituem excesso de texto. Em situações graves, sua expressão deve mostrar preocupação, sem sensacionalismo.

A cabeça 2D é um **único componente base** com variações de tema, expressão e tamanho. Não desenvolver três personagens tecnicamente independentes e inconsistentes. O especialista é representado pela cor, pelo ícone auxiliar, pelo conteúdo e pelo estado; o desenho da cabeça permanece reconhecível.

## 2.2 Forma geométrica — referência obrigatória

O personagem é **apenas uma cabeça arredondada**, sem corpo:

- Silhueta horizontal do tipo *squircle/rounded rectangle*, com topo quase plano e cantos amplamente arredondados.
- Proporção-base recomendada: **largura 112 unidades × altura 86 unidades** (aprox. **1,30:1**); aceitar refino visual entre 1,20:1 e 1,38:1 para ficar próximo das referências sem copiá-las literalmente.
- Branco puro a frio no centro (`#F9FAFF`) e leve tom azulado na parte inferior (`#E3ECFF`), com um sombreado suave; **não renderização 3D**.
- Contorno opcional de 1 a 1,5 unidade em `#DDE4F0`; sombra externa muito sutil. Não desenhar superfície metálica, carenagem, orelhas, antenas, óculos, capacete nem pescoço.
- Olho esquerdo e direito: duas pequenas elipses pretas (`#101217`), aproximadamente **8 × 13 unidades** cada na vista 112 × 86, centradas verticalmente na metade inferior (~55% da altura). Ajustar espaçamento a ~31–39 unidades entre centros.
- O mascote **não precisa de boca** para expressar emoções. Preferir posição/formato dos olhos, pálpebras, sobrancelhas discretas e pequenos sinais fora da cabeça.
- Elementos opcionais: um pequeno badge circular de agente no canto superior esquerdo e 2–3 riscos de atenção no canto superior direito. São *acessórios gráficos 2D*, não membros corporais.
- Cor principal da cabeça SEMPRE clara; **a cor do agente aparece no badge, reflexo de fundo e pequenos acentos**, não em uma cabeça inteira vermelha ou azul.
- Variantes e valores são tokens CSS configuráveis, não artefatos PNG estáticos.

### Diagrama da anatomia

```text
                linhas de emoção/evento (opcional)
                            * *
       badge ┌─────────────────────────╮
          ●  │                         │
             │        ●       ●        │  ← olhos pretos como camadas SVG
             ╰─────────────────────────╯
              ↑ cabeça 2D, SEM CORPO

   Camadas: shell → sombra/realce → grupo de olhos → pálpebras
           → expressão → badge → sinais de estado
```

## 2.3 Identidade por especialista

| Especialista | ID lógico | Acento principal | Fundo de destaque | Ícone discreto | Tom da resposta |
|---|---|---|---|---|---|
| Estoque | `stock` | `#F05259` vermelho | vermelho quase preto `#241316` | caixa/cubo | cuidadoso, voltado a evitar falta |
| Precificação | `pricing` | `#E8EDF7` branco/prata | grafite `#1B1D24` | etiqueta/preço | analítico, equilibrado |
| Fornecedores | `suppliers` | `#4B9DFF` azul | azul-escuro `#101B2F` | caminhão/entrega ou elo | claro, operacional |

Acentos são **identidades**, e não gravidade: um alerta de fornecedor crítico mantém identidade azul, mas também mostra badge de severidade vermelho com texto `Crítico`; jamais usar somente a cor para comunicar gravidade.

### Tokens de estado sugeridos

```css
--ism-agent-stock: #F05259;
--ism-agent-pricing: #E8EDF7;
--ism-agent-suppliers: #4B9DFF;
--ism-severity-success: #2FC997;
--ism-severity-info: #75B8FF;
--ism-severity-warning: #F4B65D;
--ism-severity-critical: #F05259;
--ism-surface: #15161B;
--ism-surface-elevated: #1B1C22;
--ism-border: rgba(255,255,255,.10);
--ism-text-primary: #F7F8FC;
--ism-text-secondary: #A5A8B3;
--ism-radius-card: 16px;
--ism-ease-out: cubic-bezier(.22,1,.36,1);
--ism-ease-spring: cubic-bezier(.2,1.35,.4,1);
```

Adaptar os tokens ao tema já vigente; não impor preto ou branco quando o usuário estiver usando tema claro. Em modo claro, preservar contraste, contorno e legibilidade, desativando brilho excessivo.

## 2.4 Não copiar assets do Coucou

As imagens de Coucou/Mochi servem **exclusivamente como referência de interação e simplicidade**. O repositório de referência informa em `LICENSE-ASSETS.md` que nomes, personagem, desenhos, expressões e animações são protegidos; a licença MIT vale para código, **não** para distribuir o personagem original. Por isso, construir o **ISM Assist, um desenho próprio**, com medidas, badge, composição e microinterações definidas neste documento. Não importar ícones, sons, arquivos `.png`, sprites ou imagens proprietárias do Coucou.

---

# 3. ARQUITETURA DO COMPONENTE 2D — SVG VIVO

## 3.1 Estratégia gráfica obrigatória

Criar **SVG inline com camadas manipuláveis** e animações CSS/Vue. Não usar GIF, sprite sheet, vídeo, canvas WebGL, biblioteca 3D, arquivos externos baixados na execução nem base64 gigante para o rosto. A mesma instância lógica deve ser usada nas três superfícies: cards, notificações e alerta contextual.

Benefícios: escala vetorial perfeita, reatividade nativa, olhos independentes, poucas requisições, manutenção fácil, acessibilidade e consumo baixo.

## 3.2 Arquivos/componentes de referência (nomes SUGERIDOS)

```text
frontend/
├─ components/assistant/
│  ├─ AgentMascot2D.vue            # shell SVG e camadas dos olhos
│  ├─ AgentStatusRays.vue          # opcional: sinais gráficos leves
│  ├─ AgentNotificationCard.vue    # 3 cards do dashboard
│  ├─ AgentDetailPanel.vue         # detalhe compacto acessível
│  ├─ AgentAnnouncement.vue        # balão contextual efêmero
│  ├─ AgentChatLauncher.vue        # launcher global pequeno
│  └─ AgentNotificationContent.vue # conteúdo reaproveitável no sino
├─ composables/
│  ├─ useAgentAvatarMotion.ts      # comportamento, focus, pointer, reduce-motion
│  └─ useAgentExperience.ts        # eventos, prioridades, cooldowns
├─ types/
│  └─ agentExperience.ts
└─ services/
   └─ ...                         # estender módulos atuais; não duplicar API
```

Nomes devem se adequar ao repositório atual: **componentização por responsabilidade** sem fragmentar o SVG em dezenas de arquivos desnecessários. `AgentMascot2D.vue` pode conter internamente toda a geometria e ser reutilizado com props.

## 3.3 Contrato tipado do mascote

```ts
type AgentKind = 'stock' | 'pricing' | 'suppliers' | 'general'
type AgentMood =
  | 'idle' | 'curious' | 'happy' | 'worried' | 'sad'
  | 'surprised' | 'thinking' | 'speaking' | 'waiting-approval'
  | 'success' | 'error' | 'offline'

type AgentMascotProps = {
  agent: AgentKind
  mood: AgentMood
  size?: 'xs' | 'sm' | 'md' | 'lg'
  interactive?: boolean
  attentionLevel?: 'none' | 'info' | 'warning' | 'critical'
  reducedMotion?: boolean // se omitido, detectar mídia/configuração local
  labelledBy?: string
}
```

Eventos recomendados: `activate`, `focus`, `expression-end`; somente se necessários. **Não usar `@click` na raiz de um SVG meramente decorativo**: o botão pai deve possuir foco, rótulo e papel semântico.

## 3.4 Tamanhos padronizados

| Contexto | Largura cabeça | Altura aprox. | Comportamento |
|---|---:|---:|---|
| Chat launcher | 34–42 px | 27–34 px | olhar/um piscar discreto |
| Sino lista densa | 40–48 px | 31–38 px | quase estático para reduzir distração |
| Balão inteligente | 64–80 px | 50–62 px | expressão do evento, entrada suave |
| Cards dashboard desktop | 88–112 px | 68–86 px | idle + pointer tracking + evento |
| Alerta página de estoque | 124–150 px | 96–116 px | olhar/reações, nunca corpo |
| Card mobile | 64–82 px | 50–64 px | eye tracking reduzido ou desligado |

As medidas são sugestões calibradas nas referências; validar no navegador real. Não aumentar altura do card apenas para mostrar a cabeça.

## 3.5 Grupos/camadas do SVG

1. `<defs>`: gradiente claro, filtros de sombra leves; IDs únicos por instância para evitar colisões entre três cards e popover.
2. `head-shell`: retângulo com cantos arredondados / path de squircle.
3. `head-highlight`: brilho superior com opacidade baixa (opcional).
4. `eyes-group`: grupo de olhos sujeito a pequenos deslocamentos por pointer.
5. `eye-left`, `eye-right`: elipses pretas **independentes**.
6. `eyelid-left`, `eyelid-right`: máscaras/pálpebras para blink; não ocultar olhos abruptamente por `display:none`.
7. `expression-lines`: sobrancelhas discretas/trajetos alternativos para felicidade ou tristeza.
8. `agent-badge`: ponto/círculo com cor e ícone secundário simples.
9. `event-rays`: até três riscos curtos, somente quando alertar/celebrar.
10. `focus-outline`: não parte da cabeça; contorno do botão envolvente quando foco visível.

Todas as camadas faciais devem ter origem de transform explícita no SVG (via `transform-box: fill-box; transform-origin: center`). Para IDs de gradientes e `clipPath`, usar identificador único estável por instância; nenhuma ocorrência duplicada de `id="faceClip"` em múltiplos mascotes.

## 3.6 Modelo matemático da posição dos olhos

Posição neutra de ambos: eixo X simétrico e Y idêntico. Para pointer, aplicar a MESMA translação nos olhos, preservando distância entre eles:

```ts
const nx = Math.max(-1, Math.min(1, (pointerX - headCenterX) / (headWidth * 0.5)))
const ny = Math.max(-1, Math.min(1, (pointerY - headCenterY) / (headHeight * 0.5)))
const gazeX = nx * 3.5  // unidades SVG, alvo máximo
const gazeY = ny * 2.4
```

- Medir o limite pela caixa local do mascote/card, não pelo movimento absoluto do cursor na página inteira.
- Atualizar via `requestAnimationFrame`, **no máximo uma vez por frame visível**; não escrever no store global a cada movimento.
- Limitar gaze para não ultrapassar a cabeça, inclusive quando viewport muda.
- Olhos voltam ao centro em ~**220–320 ms** depois de `pointerleave`.
- Não associar posição a dados sensíveis nem gravar telemetria de mouse.
- `pointer: coarse` / dispositivos touch: desabilitar perseguição contínua e usar micro-saccades ocasionais.
- No mouse fora do card, o personagem pode olhar para um alvo semântico (título, botão ou alerta), mas não precisa perseguir o cursor por toda a tela.

## 3.7 Camadas de movimento independentes

- **Cabeça:** `translateY`, `scaleX`, `scaleY`, `rotate` leve.
- **Olhos:** translação limitada (gaze), escala vertical (blink), variação de forma (expressão).
- **Badge:** escala pulsante curta por evento, sem looping infinito.
- **Riscos/sparkles:** entrada por `opacity` + `scale`/`translate`, apenas em estados específicos.
- **Card:** preferir leve elevação no hover (`translateY(-2px)`) sem deslocar vizinhos.
- **Balão:** entrada/saída independentes; não girar o bloco de texto.

**Nunca** animar diretamente `width`, `height`, `top`, `left`, `box-shadow` complexo, `filter:blur` alto ou CSS de layout em loops contínuos. Priorizar `transform` e `opacity`.

---

# 4. BIBLIOTECA DE ANIMAÇÕES — TEMPOS, FORMAS E GATILHOS

> Timings abaixo são valores iniciais **implementáveis e testáveis**, ajustáveis após avaliação visual. O mais importante é a combinação natural sem dois movimentos brigando pela mesma propriedade.

## 4.1 Movimento basal: “respiração”

- **Gatilho:** mascote visível em estado idle/quiet.
- **Ciclo:** 3.8–5.0 segundos, ease-in-out.
- **Amplitude:** `scaleY(0.985 → 1.012 → 1.0)` e compensação leve `scaleX(1.005 → .996 → 1)`; deslocamento Y ±**1.5px**.
- **Efeito desejado:** parecer presente, não uma boia pulsando.
- **Regras:** não executar enquanto ocorre aprovação, loading relevante, aviso crítico em destaque ou `prefers-reduced-motion`.
- **Atenuação:** cards fora da viewport devem pausar a animação; um rosto pequeno no sino não precisa respirar continuamente.

## 4.2 Piscada natural (blink)

- **Gatilho:** scheduler da instância visível em mood permitido.
- **Intervalo aleatório controlado:** entre **3.5 e 7.0 s**, com jitter; em momentos de atenção, piscar menos. Evitar que os três mascotes pisquem sincronizados.
- **Duração total:** **160–220 ms**: fechar **65–85 ms**, segurar **20–35 ms**, abrir **75–100 ms**.
- **Técnica:** pálpebras / `scaleY` dos olhos com origem no centro, preservando a forma; evitar blink negativo/flip.
- **Piscada dupla rara:** probabilidade máxima ~8%, separação 170–260 ms; não em alerta crítico nem durante `speaking` para evitar ruído visual.
- **SSR:** iniciar timers apenas no `onMounted` do cliente; não gerar aleatoriedade na árvore SSR para evitar hydration mismatch.
- **Limpeza:** encerrar timeouts ao unmount, quando documento fica oculto e quando reduced-motion estiver ativo.

## 4.3 Movimento de olhar (saccade)

- **Gatilho:** idle visível e sem pointer ativo.
- **Intervalo:** a cada 4–9 s, com chance limitada de ocorrer; olhos olham brevemente para o título/etiqueta.
- **Amplitude:** X ±2.0 unidades, Y ±1.2 unidades.
- **Transição:** 120–180 ms para mover, pausa 300–650 ms, retorno 220–300 ms.
- **Não** executar enquanto pessoa usa teclado no card ou enquanto há loading de chat.

## 4.4 Seguir mouse / foco

- **Gatilho:** `pointerenter` no card ou no próprio botão do mascote.
- **Entrada:** olhar se alinha ao cursor em 160–240 ms.
- **Durante:** posição dos olhos suavizada por interpolação, sem saltos; head pode inclinar 1–2 graus no máximo.
- **Saída:** olhar retorna ao centro, e o personagem pode dar micro-piscada após 500–900 ms.
- **Teclado:** ao focar `Detalhes`, olhos apontam levemente para o primeiro botão; ao focar `Conversar`, para o segundo; sem depender exclusivamente de mouse.
- **Limite:** no máximo 1 listener `pointermove` por card ativo, removido ao sair; não pendurar listener de alta frequência no `window` para todas as instâncias.

## 4.5 Squash & stretch (encolher / aumentar)

- **Uso:** reação discreta quando hover começa, quando usuário clica, quando alerta novo é anunciado.
- **Hover único:** `scale(.985, 1.015)` por 90 ms e volta a `scale(1.0)` em 180–220 ms; não repetir a cada pixel do mouse.
- **Click/ativação:** `scaleX(1.035) scaleY(.965)` por 95 ms → `scaleX(.99) scaleY(1.015)` por 125 ms → base por 180 ms.
- **Êxito:** pequena elevação de 2–3 px e retorno, sem bounce longo.
- **Proibição:** 3+ rebotes grandes, deslocamentos >8px, zoom maior que 8%, shaking permanente.

## 4.6 Expressão FELIZ

- **Quando:** recuperação real de alerta, meta atingida comprovada, consulta finalizada com sucesso, ação confirmada por backend.
- **Forma:** substituir elipses por dois arcos pretos leves `∩ ∩` (olhos sorrindo) ou curvar parcialmente as bordas superiores. Cabeça eleva ~2px; badge pode exibir brilho discreto.
- **Entrada:** 180–260 ms. **Duração em primeiro plano:** 1.2–2.0s. Retorno suave ao repouso se não houver evento pendente.
- **Não** confundir sucesso da requisição de listar dados com melhoria de negócio: carregar tabela com HTTP 200 não significa estoque saudável.

## 4.7 Expressão PREOCUPADA

- **Quando:** estoque próximo de mínimo, variação desfavorável relevante, risco provável mas não confirmado, falha moderada.
- **Forma:** olhos pretos ovalizados mais próximos, leve inclinação de pequenas sobrancelhas 2D (opcional), gaze fixa na informação afetada.
- **Sinal:** 2 riscos da cor da severidade aparecem uma vez por 350–500 ms; ausência de flashing constante.
- **Duração:** 2.5–4.5s para o anúncio; pode manter expressão *quase neutra* enquanto problema persistir, sem repetição contínua de atenção.

## 4.8 Expressão TRISTE

- **Quando:** condição negativa REAL e material: ruptura confirmada / produto sem estoque com demanda, degradação importante já comprovada, ação falhou de maneira definitiva.
- **Forma:** olhos diminuem em altura/descem 1 unidade; pálpebra superior ligeiramente caída; opcional pequeno traço de sobrancelha curvo, **sem lágrimas**.
- **Entrada:** 220–320 ms. **Duração inicial:** 2.5–5s; depois transição a uma forma contida de atenção até resolução.
- **Não ativar** com base em valor zero sem contexto, por exemplo receita 0 em sistema de demonstração ou sem pedidos.
- **Regra de dados:** se não houver baseline suficiente, usar `idle`/`curious` com badge `Sem dados`, não `sad`.

## 4.9 Expressão SURPRESO

- **Quando:** chegada de alerta novo relevante ou uma descoberta realmente nova.
- **Forma:** olhos arredondados + `scaleY(1.1)` por ~180 ms, cabeça `scale(1.025)` e retorno.
- **Duração:** 500–850 ms; depois passa para `worried`/`sad`/`happy` conforme resultado.
- **Não** disparar sempre que atualizam os dados por polling.

## 4.10 Expressão PENSANDO

- **Quando:** requisição real de contexto, resposta do Ollama em processamento, cálculo consultivo de backend.
- **Forma:** olhar para cima/direita, deslocamento de até 2 unidades; piscada menos frequente; 2 pequenos pontos de pensamento podem aparecer em sequência (opcional).
- **Ciclo:** 1.6–2.4s com progressão suave; terminar imediatamente no sucesso, falha ou cancelamento.
- **Restrição:** não usar loading infinito; depois do timeout mostrar estado offline/erro com opção `Tentar novamente`.

## 4.11 Expressão FALANDO

- **Quando:** resposta do chat sendo exibida em streaming ou balão novo em entrada.
- **Forma:** mexer levemente os olhos/gaze e inclinar a cabeça 1–2 graus; SEM boca sincronizada com cada token, sem strobe e sem bobbing rápido.
- **Cadência:** um movimento curto por bloco de fala (não um por token).
- **Duração:** limitada à chegada visual da mensagem; encerrada se o usuário fechar chat.

## 4.12 Expressão AGUARDANDO APROVAÇÃO

- **Quando:** há proposta de ação legítima aguardando intervenção do administrador, se o backend atual suportar.
- **Forma:** olhos atentos, imóvel por mais tempo, 1 blink calmo; badge discreto `Aguardando decisão`.
- **Não** executar automaticamente ação ao passar mouse/clicar no mascote; o botão de aprovação deve estar em fluxo com confirmação e autorização próprias.

## 4.13 Expressão SUCESSO

- **Quando:** backend confirmou ação e o resultado persistiu; ou alerta foi resolvido e leitura foi atualizada realmente.
- **Forma:** olhos felizes + mini squish (máximo 2px) e brilho verde discreto de estado.
- **Duração:** 1.0–1.6s; se for uma falha recuperada, comunicar de modo textual `Resolvido`.

## 4.14 Expressão ERRO

- **Quando:** falha de chamada importante, erro 500, validação negada; não esconder erro com sorriso.
- **Forma:** eyes worried + única queda de 1–2px; badge de erro e texto esclarecedor.
- **Sem:** tremedeira, flash vermelho repetido, som alto.
- **Duração:** estado de erro persiste textual até correção; animação executa uma vez.

## 4.15 Expressão OFFLINE / MODELO INDISPONÍVEL

- **Quando:** serviço Ollama/texto não responde; sem necessariamente interromper agentes determinísticos.
- **Forma:** pálpebras meio abaixadas, cor de badge cinza e legenda `Chat indisponível`.
- **Continuidade:** cards e alertas continuam disponíveis e atualizados; somente chat perde geração LLM.
- **Não** reiniciar Ollama automaticamente em loop pelo navegador.

## 4.16 Entrada e saída de notificações

- **Novo alerta:** fade-in de 150ms + `translateY(6px → 0)` por 260–320ms; mascote faz `surprised` uma vez, olhos apontam para o balão; alerta estabiliza.
- **Balão (conteúdo):** `opacity: 0 → 1`, `translateY(8px → 0)`, `scale(.98 → 1)`, em ~250ms.
- **Saída:** 180–230ms fade-out leve; desmontar somente após transição; fechar pelo usuário é imediato semanticamente.
- **Timeout padrão:** mostrar **6–8 s**, pausado em hover/foco; alerta crítico pode ser persistente no sino sem toast permanente.
- **Muitos alertas:** agrupar em `3 novos alertas`; mostrar um único balão com link para o sino.
- **Primeira carga/login:** não disparar balões retroativos para 42 eventos antigos.
- **Erro de refresh:** não anunciar como novo evento.

## 4.17 Respeito à redução de movimento

Se `prefers-reduced-motion: reduce` ou preferência de acessibilidade existente:

- Interromper respiração, saccade e rastreamento de ponteiro contínuos.
- Animações de entrada/saída viram fade opcional de até 80ms, ou mudança imediata.
- **Preservar informação e reações semanticamente** por expressão estática + texto/badge.
- Não bloquear chat, cards, anúncios ou leitura do sino.
- Detectar mudanças dinâmicas da media query quando a página está aberta.

---

# 5. MÁQUINA DE ESTADOS — QUEM VENCE QUANDO HÁ EVENTOS SIMULTÂNEOS

## 5.1 Modelo determinístico

Não espalhar 50 flags booleanas independentes (`isHappy`, `isWarning`, `isBlinking`, `isThinking`). Definir **mood principal** + **overlay transitório** + **identidade do agente**:

```ts
type AgentEvent = {
  id: string
  agent: AgentKind
  eventType: string
  severity: 'info' | 'warning' | 'critical' | 'success'
  occurredAt: string
  entityId?: number | string
  alertId?: number | string
  restaurantId?: number | string // apenas leitura/representação, autoridade no backend
  isNew?: boolean
  source: 'system-alert' | 'chat' | 'approval' | 'local-interaction'
}

type AvatarVisualState = {
  mood: AgentMood
  agent: AgentKind
  attentionLevel: 'none' | 'info' | 'warning' | 'critical'
  transientUntil?: number
  announcementId?: string
}
```

Regra: dados do banco determinam severidade; o compositor visual escolhe expressão; **o LLM não envia CSS arbitrário, nem executa comandos para mover o rosto**.

## 5.2 Precedência

| Prioridade | Evento | Estado | Política de interrupção |
|---:|---|---|---|
| P0 | Operação negada/erro irreversível em ação iniciada | `error` | Interrompe efeito positivo, não perde mensagem |
| P1 | Alerta crítico realmente novo | `surprised` → `sad`/`worried` | Pode interromper idle/hover; respeitar diálogo crítico aberto |
| P2 | Ação aguardando autorização explícita | `waiting-approval` | Mantém até decisão; badge textual persistente |
| P3 | Requisição de chat em andamento | `thinking` | Na instância do chat, não em todos os três cards |
| P4 | Sucesso/recovery real | `happy`/`success` | Visível por até ~2s; depois estado derivado do banco |
| P5 | Interação do usuário | `curious`/hover/squash | Só se nenhum estado de maior prioridade estiver ativo |
| P6 | Idle | `idle` | Sempre disponível como fallback |

### Guardas de segurança

- Alertas **não lidos** ≠ necessariamente **novos desde a sessão**.
- `severity=critical` pode manter o card em situação grave, mas NÃO deixa o mascote piscando vermelho indefinidamente.
- Evitar conflito visual entre `thinking` do chat e `sad` do estoque: cada instância tem seu mood local; o *evento de negócio* é compartilhado.
- Estado de UI não autoriza alteração de negócio. O visual reflete resultado confirmado pelo servidor.
- Estado tem `startAt`/`endAt` e transições testáveis. Evento duplicado com mesmo ID não reinicia animação.

## 5.3 Histerese e cooldown

- Mínimo de **3 segundos** entre anúncios toast do mesmo agente, salvo prioridade crítica.
- Deduplicar por `tenant + alertId` ou chave de ocorrência estável; se alerta repetido vier por polling não repetir animação.
- **Cooldown 60–120 s** para mesmo problema, exceto mudança real de severidade ou nova ocorrência comprovada.
- Quando gravidade oscilar por dados limítrofes, exigir estabilidade/fonte de verdade antes de emitir `recovered` e `critical` alternadamente. Não alterar regras matemáticas legadas só pela animação.
- Interromper e limpar timers no logout, mudança de restaurante, troca de papel, unmount e navegação quando componente não estiver global.

## 5.4 Matriz de reações por evento real

| Evento de dado | Resultado visual | Texto e ação principal | Observação |
|---|---|---|---|
| StockBelowMinimum (warning) | Estoque `worried`, acento vermelho moderado | `Estoque abaixo do mínimo` / `Detalhes` | Mostrar atual e mínimo reais |
| Ruptura já confirmada | Estoque `sad`, badge crítico | `Item sem estoque` / `Analisar` | Só se quantidade e semântica sustentarem |
| StockRuptureRisk48h | Estoque `worried` → destaque breve | `Risco de ruptura em até 48h` | É estimativa, não afirmar ruptura consumada |
| Estoque recuperado | Estoque `happy` por 1.5s | `Estoque regularizado` | Somente com confirmação do backend |
| CMV defasado / margem reduzida | Precificação `worried` | `Margem requer atenção` | Não inferir preço final com LLM |
| Ajuste viável calculado | Precificação `curious`/`happy` | `Sugestão de reajuste disponível` | Não aplicado sem autorização |
| Aumento de custo do fornecedor | Fornecedores `worried` | `Custo de fornecedor aumentou` | Mostrar percentual real |
| Alternativa válida cadastrada | Fornecedores `happy`/`curious` | `Alternativa identificada` | Checar qualidade dos dados |
| Nenhum alerta no período | Agentes `idle`/`happy` discreto | `Nenhum alerta ativo` | Sem inventar metas cumpridas |
| Resposta IA recebida | Mascote do chat `speaking` → `idle` | Mensagem factual | Não impacta todos os cards |
| Ollama indisponível | Mascote do chat `offline` | `Não foi possível consultar agora` | Dados determinísticos seguem |
| Falha HTTP/DB | Instância relevante `error` | Estado de erro + retry | Não mascarar como `sem alertas` |
| Sem dados suficientes | `curious`/`idle` | `Dados insuficientes` | Jamais classificar como triste/feliz |
| Informação histórica carregada | Apenas estado persistente | Notificação no sino | Não exibir novo toast de surpresa |
| Aprovação real concluída | `success` e reconsulta do recurso | `Alteração confirmada` | Nunca só no clique |

---

# 6. DASHBOARD — EXATAMENTE TRÊS CARDS DE AGENTES

## 6.1 Layout macro aprovado

```text
[NAVBAR ORIGINAL: logotipo / sino / perfil]

[HERO ORIGINAL — Bem-vindo...	              Atualizar dashboard]

Indicadores Principais
[ KPI 1 ] [ KPI 2 ] [ KPI 3 ] [ KPI 4 ]

Insights e alertas dos agentes             Atualizar alertas
[ESTOQUE:  head  | infos + Detalhes + Conversar]
[PREÇO:    head  | infos + Detalhes + Conversar]   ← desktop: TRÊS na MESMA LINHA
[FORNECED: head  | infos + Detalhes + Conversar]

[GRÁFICOS, TABELAS E OUTROS COMPONENTES ORIGINAIS — INALTERADOS]
                                       [cabecinha Chat >]
```

A diagramação acima é esquemática; no desktop os três cards são colunas iguais e alinhadas. Os KPIs continuam acima. **Nenhum painel de IA extra entre eles**.

## 6.2 Dimensões pretendidas

- Container: seguir `max-w-7xl` ou padrão atual do projeto.
- Desktop amplo: **grid de 3 colunas**, `gap` horizontal **14–18px** (adaptar token existente).
- Cards: altura de referência **180–215px** incluindo botões e padding; não ultrapassar ~240px por descrição longa.
- Padding interno: **16–20px**; raio **14–18px**.
- Área do rosto: largura útil **105–130px**, centralizada verticalmente; área de conteúdo flexível.
- Cabeça: ~**92–110px** de largura; não esticar horizontalmente.
- Label do agente e badge: chips de 11–12px com cor moderada.
- Título: 14–16px semibold/bold, até duas linhas, altura reservada.
- Descrição: 12–13px, 2–3 linhas; botão `Detalhes` abre textos longos.
- Botões: 32–36px de altura, toque fácil, sem `button` sem função.
- Cards alinhados em topo e base; altura consistente no mesmo breakpoint.

## 6.3 Estrutura interna

```text
┌────────────────────────────────────────────────────────────┐
│ [  CABEÇA 2D   ]  [● Estoque] [Atenção]                     │
│ [      ● ●     ]  2 itens em estado crítico                 │
│ [              ]  Filé Mignon (12 kg) e Salmão (6 kg) ...   │
│ [badge/sinais ]  [ Detalhes ] [ Conversar ]                 │
└────────────────────────────────────────────────────────────┘
```

Se um card tem menos texto, os botões **não sobem** desalinhados: usar flex-column e `margin-top:auto` somente na área de ações. Não impor `truncate` em número crítico sem alternativa no detalhe.

## 6.4 Um card por agente, não um card por alerta

- Selecionar o insight **mais importante** por agente, ou fazer **resumo agregado** quando existirem várias ocorrências críticas, preservando quantidade real.
- Seleção: prioridade de severidade > recência > estabilidade da entidade; regra previsível, documentada e testada.
- Se não houver nenhum alerta: renderizar card neutro `Nenhuma ocorrência importante` e ações `Ver histórico`/`Conversar`, sem inventar sucessos.
- Card não pode duplicar o mesmo evento após filtro ou recarregamento.
- Alerta de fornecedores não deve aparecer no card de estoque por mapeamento de tipo incorreto.
- Conteúdo sempre vindo de DTO/API real ou formatação determinística do cliente; **LLM não é necessário para renderizar card**.

## 6.5 Comportamentos obrigatórios

| Interação | Resposta visual | Resultado funcional |
|---|---|---|
| Mouse entra no card | olhos acompanham até ±3.5 unidades; cabeça pequena inclinação | nenhuma requisição |
| Mouse sai | olhar retorna e idle recomeça | nenhum estado de negócio alterado |
| Foco pelo teclado | contorno visível, gaze para botão | navegação acessível |
| Click na cabeça | pequeno squash + foco/ação definida | abrir detalhes/contexto, igual a ação semântica |
| Click `Detalhes` | mini reação `curious` | abrir detalhe do agente correto |
| Click `Conversar` | expressão `thinking` no launcher/chat | abrir thread contextual vinculada àquele alerta |
| Atualizar alertas | não animar todos | recarregar API, reagir somente a eventos novos reais |
| Alteração da severidade | estado suave e badge atualizado | números coerentes com API |

## 6.6 Acessibilidade e semântica

- Card contém heading e status em texto; o SVG, se puramente decorativo, `aria-hidden="true"`.
- `Detalhes`/`Conversar` são `<button type="button">` reais, `aria-label` contextual e `aria-controls` quando couber.
- Não colocar botão dentro de botão; se card inteiro for clicável, tratar eventos internos separadamente.
- `focus-visible` com contorno de alto contraste.
- Toda severidade tem texto e/ou ícone, não só vermelho/azul.
- Se usuário estiver lendo descrição com leitor de tela, novas animações não roubam foco.

---

# 7. SINO — NOTIFICAÇÕES INTELIGENTES COM PERSONAGEM

## 7.1 Reaproveitamento obrigatório

**Não implementar outro centro de notificações.** Estender o sino e o `NotificationDrawer`/equivalente atual, preservando comportamento existente de ler, descartar, paginação e permissões. Na base anterior havia:

- `GET /api/system/alerts`
- `PATCH /api/system/alerts/{id}/mark-read`
- `PATCH /api/system/alerts/{id}/dismiss`

Conferir a versão atual: autorização do GET e do marcar como lido pode ser mais ampla que a do descartar. Não transformar `dismiss` em ação genérica permitida a qualquer perfil.

## 7.2 Estética da imagem 02

- Popover/drawer **ancorado visualmente ao sino**, sem mudar navbar.
- Fundo quase preto, borda delicada, raio 14–18px, sombra moderada.
- Cabeçalho `Notificações Inteligentes` e ação `Marcar todas como lidas` somente se backend permitir.
- Para alerta relevante e selecionado, posicionar **cabeça 2D vermelha** à esquerda de um **balão de fala** (não um corpo inteiro).
- Balão: borda vermelha leve, 1–2 linhas de informação, chip `Estoque`, horário relativo e botões compactos.
- Setinha do balão apontando para a cabeça (CSS pseudo-element ou SVG estático), com posição responsiva.
- Para lista densa, cabeças menores e sem looping; usar ícone por categoria se necessário.
- O popover deve conter histórico e não apenas uma notificação cenográfica.

## 7.3 Significado das ações

- `Ver detalhes` → detalhe do alerta exato ou entidade, com os dados atuais do tenant.
- `Conversar` → chat com vínculo ao alerta, jamais conversa vazia.
- `Marcar como lida` → atualiza persistência; não equivale a resolver o problema.
- `Descartar` → respeita autorização e diferencia de `Lido`.
- `Resolver agora` só pode existir se o sistema dispuser de uma ação de resolução REAL e segura. Caso contrário, **substituir por `Analisar` ou `Conversar`**, para não prometer ação inexistente.
- O sino e os cards compartilham `alertId`, estado de leitura e contagem sem assincronias visíveis.

## 7.4 Entrega de evento novo

1. Servidor persiste/retorna ocorrência nova do agente.
2. Uma rotina compartilhada observa mudança da lista ordenada de IDs, filtrada por restaurante/perfil.
3. Identifica novidade **posterior ao baseline da sessão**, não um registro velho ainda não lido.
4. Aplica preferência de notificação e cooldown.
5. Seleciona o agente e modo de reação; badge do sino aumenta/atualiza.
6. Um balão opcional aparece, sem deslocar layout; boca não necessária.
7. Ao clicar, abre detalhe/contexto correto; ao ignorar, alerta continua no sino salvo se usuário explicitamente o marcou.
8. Ao trocar restaurante/logout, limpar cache de novidades e assinaturas anteriores.

## 7.5 Evitar spam

- Somente um balão visível por vez.
- Máximo um anúncio por ID de ocorrência, salvo mudança material de severidade.
- Em lote, mostrar resumo por quantidade; não quatro toasts por tick do worker.
- Se a aba estiver oculta, manter badge após retorno, não fazer animação em background.
- Polling moderado, preferindo mecanismo existente; se introduzir: base 30–60s, backoff em erro, pausa em `document.hidden`, abort de fetch no teardown.
- Não manter listeners/subscriptions quando auth expira.

---

# 8. PÁGINAS CONTEXTUAIS — EXEMPLO ESTOQUE

## 8.1 Imagem 03 como contrato visual

Na tela `Controle de Insumos`, preservar:

- Header e campo de busca existentes.
- Tabela, filtro, status, ações de edição/exclusão e ordenação existentes.
- Card/aviso de risco de estoque acima da tabela **se já existir**; transformá-lo visualmente para o desenho aprovado.
- Cabeça 2D **grande, vermelha por acentos**, à esquerda, e texto do problema à direita.
- Botões `Detalhes` e `Conversar` com ações reais.
- Launcher do chat pequeno no rodapé direito.

Não esticar a cabeça 2D ao ponto de competir com a tabela. Banner pode ter altura maior que cards de dashboard quando existe descrição longa, mas **não pode virar um megacomponente**; alvo aproximado 165–220px desktop, menos quando sem alerta. Se evento não existe, mostrar um card de estado saudável discreto ou não mostrar banner, conforme a UX atual, sem inventar risco.

## 8.2 Fontes de dados

- `2 itens em estado crítico` = quantidade real de produtos elegíveis à condição definida no backend, no restaurante atual.
- Nomes e estoques = consulta tipada autorizada.
- Referências para `Detalhes` e `Conversar` incluem os IDs corretos; não usar strings de nome como identificadores exclusivos.
- Se dados da tabela mudarem, refletir após refresh sem depender de LLM.
- Não tentar chamar cálculo da IA a cada movimento da cabeça.

## 8.3 Outras áreas

O mesmo padrão poderá ser empregado em telas de precificação e fornecedores **somente se a implementação atual já tiver pontos de insight equivalentes**. Não criar banners grandes desnecessários em todas as páginas. O objetivo desta entrega continua: três cards da dashboard, sino, página de estoque ilustrada e chat global.

---

# 9. CHAT LATERAL E RAMIFICAÇÕES CONTEXTUAIS

## 9.1 Comportamento esperado

- Launcher do canto inferior direito é pequeno, com rosto 2D branco e acento atual do chat (azul/neutro).
- Fechado por padrão; abrir por clique/Enter/Espaço em launcher ou em `Conversar`.
- Desktop: drawer na direita, largura de referência 360–420px, responsivo e não invadindo navbar.
- Mobile: painel modal/sheet de largura apropriada com botão de fechar, sem scroll horizontal da página.
- Ao abrir, exibir estado `Disponível`, `Conectando`, `Pensando`, `Indisponível` conforme backend real.
- Conteúdo: cabeçalho, histórico, entrada de texto, enviar, resposta, retry, indicador de contexto, fechar, `Nova conversa` se já houver suporte.

## 9.2 “Conversar sobre isso” NÃO é abrir chat vazio

Fluxo obrigatório, exemplificando card de estoque:

1. Card possui `alertId` (quando existir) e dados reais de apresentação.
2. Usuário clica `Conversar`.
3. Chat é aberto; cabeçalho mostra chip `Contexto: Estoque • Salmão Fresco` (ou nome realmente retornado).
4. Frontend passa **somente ID/tipo estável da entidade/ocorrência**; servidor reconsulta e valida tenant e permissão.
5. Chat inicia ou restaura thread contextual (ramificação filha da conversa principal, se arquitetura atual permitir).
6. Snapshot autorizado é anexado ao contexto: estoque atual, mínimo, movimentações/risco quando disponíveis e data da coleta.
7. Apresenta uma primeira mensagem **determinística e fiel** ao dado, e aceita perguntas adicionais.
8. História da conversa fica persistida nos mecanismos já instalados; novo chat não apaga thread antiga.
9. Se alerta sumiu/foi descartado, histórico pode continuar consultável conforme autorização, mas dados vivos devem ser rechecados e informados como atuais ou históricos.
10. Sair/entrar da conta ou trocar restaurante não pode revelar conversa de outro tenant.

## 9.3 Papel do modelo local

O Ollama entrega **linguagem, explicação e interação**; já os agentes determinísticos fornecem números, alertas, critérios e vínculos. Se o modelo for muito pequeno (ex.: `gemma3:270m`), limitar escopo e utilizar respostas templated seguras para cálculos. **Não passar SQL bruto, comandos, dados de outros restaurantes ou instruções executáveis para o LLM**.

- Não reinstalar Ollama/baixar modelos sem diagnóstico e necessidade.
- Não substituir o provider existente por outro sem pedido.
- Não chamar localhost Ollama diretamente do browser.
- Timeout e fallback com erro visível; chat offline não derruba dashboard.
- Nenhum texto gerado deve se tornar ordem de aprovação/compra/preço.
- Sanitizar textos de produtos/fornecedores antes de inseri-los em prompts e HTML; tratar como dados não confiáveis.

## 9.4 Continuidade de contexto

- Verificar se o backend implementado na tarefa anterior guarda histórico por usuário/tenant/thread, com limites de tokens/contexto.
- Reusar repositório, DTOs, memória de conversa e APIs existentes.
- Evitar enviar banco inteiro a cada pergunta; recuperar contexto relevante por IDs autorizados.
- Resposta deve declarar quando número veio de snapshot datado e quando foi atualizado.
- A experiência visual muda o mood do avatar **local do chat** para `thinking`/`speaking`/`error` sem travar o restante da aplicação.

---

# 10. RESPONSIVIDADE — SEM “EFEITO PILHA INFINITA”

## 10.1 Desktop largo (>= 1280px de viewport)

- Os três cards **lado a lado**, com mesma altura, texto ao lado do mascote e botões alinhados na base.
- Alertas e popovers respeitam largura do container; o sino pode sobrepor temporariamente hero/KPIs, com `z-index` e bordas corretos.
- Launcher alinhado à margem inferior direita, respeitando sidebar/nav e safe area.

## 10.2 Tablet (768–1279px)

- Preferência: trio de cards em **faixa horizontal rolável local com scroll-snap e indicador/controle acessível** ou composição 2+1 somente se a proporção dos cards ficar realmente melhor; **não** comprimir mascote e texto até sobreposição.
- O movimento horizontal é restrito à faixa; não gerar overflow da página.
- Card nunca fica cortado sem affordance de continuidade.
- A cabeça não invade título nem botões em tamanhos intermediários.

## 10.3 Mobile (< 768px)

- Preferência: **um card por vez em carrossel horizontal local**, swipe acessível + botões de navegação discretos; evita três blocos grandes empilhados antes de gráficos.
- Mascote reduzido a 64–82px; texto mantém contraste; ações acessíveis por toque (alvo 40–44px quando possível).
- Balão do alerta usa largura `min(92vw, 360px)` e não cobre teclado virtual, CTA ou área de navegação.
- Notification drawer usa popover/sheet adequado à viewport; não colocar 600px fora da tela.
- Se o usuário escolhe redução de movimento, trocar auto-scroll por navegação manual sem transição.

## 10.4 Breakpoints e testes obrigatórios

Validar pelo menos: **360×800, 390×844, 768×1024, 1024×768, 1366×768, 1440×900 e 1920×1080**. Verificar tema claro e escuro; fontes em 125% e 200%; textos longos; respostas de erro; produto com nome grande; badge crítico; modal aberto e teclado de celular.

---

# 11. CONTRATOS DE DADOS E INTEGRAÇÃO ENTRE SUPERFÍCIES

## 11.1 DTO de apresentação sugerido

> Modelo de design, não ordem de trocar todos os DTOs do backend. Adapte o mapper a contratos atuais.

```ts
type AgentCardViewModel = {
  agent: 'stock' | 'pricing' | 'suppliers'
  summaryKey: string
  title: string
  description: string
  severity: 'info' | 'warning' | 'critical' | 'success' | 'unknown'
  metric?: { label: string; value: string; unit?: string }
  alertIds: string[]
  primaryAlertId?: string
  subjectType?: 'Product' | 'Dish' | 'Supplier'
  subjectId?: string
  observedAt?: string
  unreadCount: number
  canOpenDetails: boolean
  canStartChat: boolean
  canTakeAction: boolean
  state: 'ready' | 'loading' | 'empty' | 'error' | 'stale'
}
```

Critérios:

- IDs persistentes e números normalizados no servidor quando necessário.
- Contagem de não lidos sincronizada com sino; card mostra agregado por agente.
- `unknown` não significa `info`, nem significa `healthy`.
- Data e fuso local convertidos sem apresentar futuro falso.
- Nunca inferir autorização `canTakeAction` somente do papel salvo no frontend.

## 11.2 Fluxo unificado

```text
[Agentes determinísticos / Banco] → [SystemAlert API e DTOs]
                                       ↓
                            [Adapter tipado de alertas]
                                       ↓
                 [Store/composable único por usuário/tenant]
                   ├── [3 cards de dashboard]
                   ├── [sino/notifications drawer]
                   ├── [balão de evento novo]
                   ├── [banner contextual estoque]
                   └── [chat / thread contextual por alert ID]
                                        ↓
                            [Backend autorizado / Ollama]
```

**Uma única fonte de atualização**: evitar `setInterval` em cada card, notificação e launcher. Se há polling existente, centralizar assinatura e revalidação.

## 11.3 Recomendações de mapping de severidade

- Usar severidade armazenada na ocorrência quando for confiável.
- Para registro legado com formato diferente, adaptar explicitamente cada tipo; marcar severidade desconhecida sem fingir que é `info`.
- Se um alerta de `DishCmvDefasagem` estiver resolvido, o card precisa refletir o novo estado por consulta ao backend e/ou mecanismo de resolução atual.
- Quando houver vários restaurantes, cache/index por tenant autenticado, nunca compartilhado sem chave.

## 11.4 Segurança e autorização

- **Backend valida claims e tenant** em TODA leitura de detalhe/thread, não basta filtrar frontend.
- Admin global não recebe poderes gerenciais arbitrários sobre restaurante alheio; respeitar políticas existentes.
- Não permitir que cliente envie `restaurantId` arbitrário e acesse métricas de terceiros.
- Reconferir autorização na hora de abrir detalhes, não confiar no objeto que estava no card.
- Content-Security, CORS e API base URL permanecem padrões atuais; não publicar porta Ollama para toda rede.
- Não persistir rastreamento de mouse, expressões ou atributos emocionais como dados de negócio; são apenas estado local de UI.

---

# 12. CONTRATO TÉCNICO PARA PERFORMANCE E CICLO DE VIDA

## 12.1 Orçamento inicial de experiência

- Animações em `transform`/`opacity`; tentar manter **60 FPS** em desktop moderado com poucos mascotes visíveis, sem prometer valor absoluto.
- No máximo uma instância de loop `requestAnimationFrame` por mascote **ativo sob pointer**, cancelada ao sair/ocultar.
- Timers de blink por instância visível, pausados por `IntersectionObserver`/`visibilitychange`/unmount.
- Nenhuma chamada ao servidor disparada por frame, hover, blink, idle ou resize do mascote.
- No máximo um listener global compartilhado para eventos globais quando necessário.
- Reutilizar SVG inline de baixo custo, sem filtros caros/blur animado e sem dependência externa por animação.
- Memória não deve crescer após abrir/fechar chat ou sino repetidamente.

## 12.2 Revisão de listeners

Em cada componente que registra `pointermove`, `pointerleave`, `visibilitychange`, `resize`, `matchMedia` ou IntersectionObserver:

- Documentar `setup` e `cleanup`.
- Cancelar `requestAnimationFrame` em `onBeforeUnmount`.
- Limpar `setTimeout` ao desativar.
- Retirar listeners do `document` / `window`.
- Resetar estado de ponteiro ao sair de uma instância.
- Confirmar ausência de comportamento duplicado após múltiplos mounts.

## 12.3 SSR e hidratação Nuxt

- Não acessar `window`, `document`, `matchMedia`, `performance` ou `getBoundingClientRect` durante SSR.
- Valores aleatórios de timer somente depois de mount no browser.
- SVG server-rendered deve conter forma estática inicial equivalente à primeira renderização no cliente.
- `ClientOnly` somente para camada que realmente exige DOM; não esconder textos ou dados essenciais dos cards desnecessariamente.
- Se o `id` único SVG for dependente de instância, preferir `useId()`/mecanismo estável suportado por Vue atual.

## 12.4 Degradação graciosa

- Sem JavaScript de animação: SVG estático legível + texto de severidade.
- Sem Ollama: cards, notificações e botões `Detalhes` seguem funcionando; chat sinaliza indisponibilidade.
- API de alertas offline: sinalizar erro na região, sem inventar `Nenhum alerta`.
- Sem suporte a hover: clique/foco e transições discretas.
- Modo reduzido: sem breathing/gaze, informações intactas.

---

# 13. DESIGN DE EVENTOS — ANIMAÇÃO NÃO É REGRA DE NEGÓCIO

## 13.1 Separação de responsabilidades

- `Domain/Application`: define eventos, riscos, preços, estoques, permissões.
- `API`: entrega DTOs e status corretos.
- `Store`: reflete eventos/dados, estado de leitura, contexto e datas.
- `EmotionMapper` (puro): `event + status + severity → mood`, sem API.
- `MotionController`: executa transição visual que recebeu.
- `Avatar`: desenha rosto e emite click/focus, sem acesso ao banco.
- `UI cards/sino/chat`: especificam contexto visual, sem recriar lógica de risco.

## 13.2 Exemplo de funções puras testáveis

```ts
function moodForSeverity(severity: AgentCardViewModel['severity']): AgentMood {
  switch (severity) {
    case 'critical': return 'sad'
    case 'warning': return 'worried'
    case 'success': return 'happy'
    case 'info': return 'curious'
    default: return 'idle'
  }
}

function shouldAnnounceNewAlert(
  eventId: string,
  alreadySeenThisSession: ReadonlySet<string>,
  initialHydrationComplete: boolean
): boolean {
  return initialHydrationComplete && !alreadySeenThisSession.has(eventId)
}
```

A função `moodForSeverity` é **default**, não substitui mapeamento semântico por evento; `critical` de ruptura potencial pode justificar preocupado em vez de triste. Testar regras por tipo.

## 13.3 Exemplo de controller de animação (pseudo)

```text
onMount -> render static -> read reduced motion -> subscribe visibility
onIdle -> schedule blink jitter and optional micro-saccade
onPointerEnter -> if no P0-P4 lock, curious and gaze(pointer)
onPointerMove -> write one goal; rAF interpolates eye transform
onPointerLeave -> cancel gaze loop, ease back to neutral
onNewCriticalAlert -> dedupe -> surprised one-shot -> worried/sad -> settle
onChatSend -> thinking on chat mascot only
onChatReply -> speaking short -> idle
onChatFailure -> offline/error -> keep actionable retry
onUnmount -> cancel all timeouts, rAF, observers, subscriptions
```

Proibição: animar uma face removendo e recriando SVG em todo update; preservar nós para transição fluida.

---

# 14. ETAPAS DE CONSTRUÇÃO — SEQUÊNCIA OBRIGATÓRIA

Cada etapa deve ter **escopo, implementação, validação, relatório e ponto de aceite**. A próxima só começa com bloqueadores resolvidos ou limitação documentada que não comprometa segurança/aceite.

## ETAPA 0 — Inventário, baseline e segurança do repositório

**Fazer:**

1. Identificar branch/commit/working tree; conferir padrão do projeto.
2. Criar branch de trabalho **a partir da branch com V2 funcional**, sem reset nem apagar commits.
3. Ler documentação local, o presente arquivo e três imagens.
4. Capturar baseline visual, build e testes disponíveis.
5. Mapear no código atual componentes, dados e endpoints responsáveis por cada superfície.
6. Registrar risco de arquivos sujos e backups de mudanças próprias, sem remover nada alheio.

**Aceite:** baseline e mapa de arquivos reais; branch nova identificada, nenhuma alteração destrutiva.

## ETAPA 1 — Contrato visual e protótipo isolado da cabeça SVG

**Fazer:**

1. Construir `AgentMascot2D` isolado, sem backend e sem mudar dashboard ainda.
2. Vetor próprio: cabeça arredondada branca, dois olhos pretos, badge, linhas de reação.
3. Variações stock/pricing/suppliers; tamanhos xs/sm/md/lg.
4. Garantir ausência absoluta de corpo/membros e assets 3D.
5. Produzir uma preview local dos **três rostos lado a lado** em fundo escuro/claro, se conveniente.

**Aceite:** comparação visual com refs. 01–03, SVG limpo, sem distorção, estados estáticos legíveis.

## ETAPA 2 — Motor de animação e máquina de estados

**Fazer:**

1. Implementar `blink`, respiração, gaze pointer, saccades, squash, entrada/saída e emoções.
2. Separar loops de idle, eventos e pointer; criar scheduler controlado.
3. Implementar reduced motion, visibility pause, cleanup, SSR seguro.
4. Expor estados por props tipadas, sem hardcode de eventos de banco.
5. Construir teste/demo local de todos os moods (não publicar como página operacional).

**Aceite:** cada emoção reconhecível; animações não simultâneas contraditórias; sem vazamento de listeners.

## ETAPA 3 — Mapper de estado/alertas com dados reais

**Fazer:**

1. Auditar backend/DTOs, alertas legados e mudanças V2.
2. Criar mapper/testes `alert → agent → severity → mood`.
3. Definir agregação **1 card/agent**, datas, estado sem dados e histórico.
4. Reutilizar store/composable de alertas existente e evitar polling duplicado.

**Aceite:** zero dados inventados; três cards consistentes para cenários crítico, saudável, offline, vazio e múltiplos alertas.

## ETAPA 4 — Dashboard: substituir APENAS a representação antiga

**Fazer:**

1. Manter navbar, hero, KPIs, gráficos e grids.
2. Refatorar cards da seção para layout horizontal cabeça + texto + ações.
3. Renderizar exatamente Estoque, Precificação e Fornecedores.
4. Aplicar emoções/cores por dados reais e hover/foco.
5. Reaproveitar handlers `Detalhes` e `Conversar` existentes, consertando eventuais ações fake.

**Aceite:** espelhamento da referência 01; três cards apenas, sem megacomponente e sem overflow.

## ETAPA 5 — Detalhe e chat contextual por card

**Fazer:**

1. Detalhe com dados reais, títulos, datas, severidade, origem e entidade.
2. `Conversar` abre chat correto com chip de contexto/ID persistido.
3. Backend verifica tenant e retorna snapshot; histórico preservado.
4. Ícone do launcher = a mesma cabeça 2D, com animação contida.
5. `thinking`/`speaking`/`offline` ligados ao estado real da request.

**Aceite:** conversa contextual reproduzível após reload; chat não gera números aleatórios e não quebra sem Ollama.

## ETAPA 6 — Sino e nova notificação com balão

**Fazer:**

1. Integrar cabeça 2D e balão ao drawer existente, sem central paralela.
2. Contador/unread/leitura/descarte corretos e refletidos nos cards.
3. Reação à novidade só após baseline; dedup por evento; cooldown.
4. Popover acima do conteúdo sem clipping/z-index errado.
5. Ações semanticamente verdadeiras (`Analisar`, `Detalhes`, `Conversar`).

**Aceite:** espelhamento da referência 02; receber 1 alerta exibe 1 anúncio, refresh não repete; sem falsos sucessos.

## ETAPA 7 — Tela de estoque contextual

**Fazer:**

1. Reutilizar banner atual, sem refazer a tabela.
2. Substituir ícone/arte por cabeça 2D vermelha, reação preocupada/triste quando real.
3. Ações vinculadas ao(s) item(ns) do estoque real.
4. Estado saudável não cria alerta fake; erro tem sinalização.

**Aceite:** espelhamento da referência 03, tabela intacta, responsividade correta.

## ETAPA 8 — Responsividade, acessibilidade e polimento

**Fazer:**

1. Verificar todos os breakpoints especificados, claro/escuro, zoom 200%.
2. Corrigir carrossel local mobile, posicionamento do sino, chat e balões.
3. Teclado, foco, screenreader e contraste.
4. Motion reduction e touch.
5. Refinar timing pela comparação manual com imagens de referência.

**Aceite:** sem rolagem horizontal global, sem overflows, sem elementos tapando botões; layout clean.

## ETAPA 9 — Testes unitários, integração, UX e segurança

**Fazer:** executar a matriz da seção 15 e corrigir falhas. Adicionar apenas bibliotecas de teste estritamente necessárias, sem refatoração de tooling sem razão.

**Aceite:** evidência de testes reais e não apenas builds; bloqueadores zero nas partes modificadas.

## ETAPA 10 — Performance, leaks e estabilidade

**Fazer:** validar CPU, listeners, FPS percebido, repetição de abertura/fechamento, mudança de conta/tenant, Ollama offline, erros API.

**Aceite:** nenhum loop/timer vazando, nenhuma request por frame, sem regressão de carga visível.

## ETAPA 11 — Revisão final, commits e documentação de uso

**Fazer:**

1. Conferir alterações somente no escopo.
2. Build/test final, registrar falhas preexistentes separadamente.
3. Capturas finais das três telas e, idealmente, um pequeno registro de estados.
4. Commits coerentes por etapa (ou conjunto lógico) respeitando convenções da base.
5. Escrever relatório com mudanças, testes, limitações e roteiro de apresentação.
6. **Não fazer merge** na branch principal/develop sem autorização/política explícita.

**Aceite:** branch entregável, documentação real e checklist assinado por resultados de execução (não promessas).

---

# 15. MATRIZ DE TESTES — EXECUTAR, NÃO APENAS LISTAR

## 15.1 Testes unitários da cabeça/emoções

| ID | Caso | Resultado esperado |
|---|---|---|
| U01 | Renderizar `stock/idle` | SVG 2D branco, olhos pretos, badge vermelho, sem corpo |
| U02 | Renderizar `pricing/happy` | olhos felizes, acento neutro, sem membros |
| U03 | Renderizar `suppliers/worried` | expressão preocupada, badge azul |
| U04 | `sad` após situação crítica real | olhos/posição corretos, badge crítico textual |
| U05 | blink automático | fecha/abre sem perda permanente de olhos |
| U06 | 3 instâncias simultâneas | IDs SVG sem colisão; piscadas não forçadas em sincronia |
| U07 | pointermove extremo | olhos limitados, não saem da cabeça |
| U08 | pointerleave | gaze retorna ao neutro |
| U09 | hover repetido | squash não se acumula nem escala indefinidamente |
| U10 | 200 mounts/unmounts | nenhum timer/listener permanente residual |
| U11 | visibilitychange oculto | animador para e retoma sem salto |
| U12 | reduced motion | loops desativados, sem perder status |
| U13 | SSR/hidratação | nenhum mismatch de IDs/aleatoriedade |
| U14 | mudar props do agente | badge/cores trocam sem duplicar SVG |
| U15 | expressão temporária termina | retorna ao estado derivado de dados |
| U16 | erro interrompe sucesso | prioridade P0 funciona |
| U17 | warning persistente | sem piscada/flash de severidade infinito |
| U18 | click/foco | controles continuam acessíveis |

## 15.2 Testes do mapper e eventos

| ID | Cenário | Esperado |
|---|---|---|
| D01 | Cada enum legado | mapeia para especialista correto |
| D02 | 10 alertas de estoque | exatamente 1 card-resumo estoque; total real |
| D03 | Estoque e fornecedor simultâneos | cards distintos, badges corretos |
| D04 | Evento sem ID de alerta | não inventar ID; fallback definido |
| D05 | Payload legado ausente/JSON inválido | UI segura e erro/unknown adequado |
| D06 | Carregamento de 42 não lidos | badge 42, **nenhum** popover novo automático |
| D07 | Chega 1 novo ID após baseline | um anúncio + contador atualizado |
| D08 | Mesmo ID reaparece no polling | zero novo anúncio |
| D09 | Mudar severity real | card atualiza e respeita cooldown |
| D10 | Marcar como lido | banco, sino e dashboard consistentes |
| D11 | Descartar sem permissão | 403/ação bloqueada, sem sucesso falso |
| D12 | Restaurar sessão | contador e status conforme servidor |
| D13 | Trocar tenant | dados/cache/reações isolados |
| D14 | Dados ausentes | estado neutro, nunca números inventados |
| D15 | Falha API | estado de erro, não `0 alertas` |
| D16 | Erro após refresh | não duplicar alerta/nova reação |

## 15.3 Testes de chat

- `C01`: `Conversar` em estoque abre chat com contexto do alerta correto.
- `C02`: `Conversar` em precificação abre contexto de prato/CMV correto.
- `C03`: `Conversar` em fornecedores abre dados permitidos do fornecedor correto.
- `C04`: conversas contextuais **não sobrescrevem** conversa principal.
- `C05`: reload preserva histórico autorizado.
- `C06`: ID inexistente/alerta descartado tratado honestamente.
- `C07`: request de outro tenant recebe 403/404 sem vazamento.
- `C08`: Ollama offline informa indisponibilidade, não derruba dashboard.
- `C09`: timeouts/cancelamento param `thinking`.
- `C10`: produto com texto malicioso/HTML/prompt injection não executa código nem muda instrução de backend.
- `C11`: reabrir chat 30 vezes não cria 30 listeners/pollings.
- `C12`: resposta do modelo não vira operação de escrita automática.

## 15.4 Testes visuais / interação

- `V01`: desktop 1440px — três cards lado a lado, rostos grandes e texto legível.
- `V02`: 1366px — KPI e cards sem sobreposição; só três cards.
- `V03`: 768px — cards adaptados, sem scrollbar horizontal da página inteira.
- `V04`: 390px — navegação local entre cards, botões acessíveis.
- `V05`: 360px — chat e sino dentro da viewport.
- `V06`: 1920px — sem card esticado absurdamente.
- `V07`: tema claro/escuro — olhos pretos continuam visíveis, sem branco sobre branco.
- `V08`: usuário passa cursor dentro do card — os olhos acompanham suavemente.
- `V09`: usuário sai — olhos voltam sem tremer.
- `V10`: piscadas com intervalos orgânicos — não sincronizadas como relógio.
- `V11`: dados bons → happy; risco → worried; ruptura → sad.
- `V12`: evento novo → surprised → estado semântico, sem loop permanente.
- `V13`: open/close do sino mantém z-index e barra da página.
- `V14`: balão não encobre Chat/CTA, e mantém legibilidade.
- `V15`: `prefers-reduced-motion` no browser desliga movimentos repetitivos.
- `V16`: teclado/Tab/Enter/Escape no sino, card, detalhe e chat.
- `V17`: zoom 200% sem clipping de texto essencial.
- `V18`: nomes de produtos longos, vazios e caracteres especiais.
- `V19`: botões de detalhes/chat não exibem sucesso fictício.
- `V20`: comparações finais com imagens 01, 02, 03 aprovadas.

## 15.5 Testes de performance e regressão

- Inspecionar `npm run build` / comando equivalente existente no projeto.
- Inspecionar testes backend existentes em `backend/ISM.sln`, executar `dotnet test` em ambiente compatível.
- Executar smoke da dashboard, estoque, precificação, fornecedores, login/logout, notificações e chat.
- Confirmar que importação por foto, garçom, cozinha e caixa não foram alterados sem necessidade (compilação/rotas preservadas).
- Usar DevTools/Performance ou automação de navegador para observar reflows e FPS percebido durante 20–30s.
- Abrir/fechar notificações e chat 30–50x, confirmar ausência de listeners/timers acumulados.
- Deixar dashboard 10–15min em idle em máquina dev e observar CPU/memória de maneira comparativa com baseline; não inventar métrica se não houver medidor.
- Simular 50–100 alertas em ambiente de teste sem tornar a dashboard gigantesca e sem spam de notificações.
- Cortar conexão do Ollama e recuperar, verificando graceful fallback.

---

# 16. CRITÉRIOS DE APROVAÇÃO FINAL (DEFINITION OF DONE)

O resultado **só está concluído** se:

1. Existe **UMA implementação visual única do personagem**, baseada em SVG 2D, cabeça branca arredondada e olhos pretos.
2. **ZERO corpo inteiro**, ZERO braços/pernas, ZERO personagem 3D e ZERO asset Coucou/Mochi copiado.
3. As três referências de UI estão refletidas no resultado real (dashboard, sino e estoque).
4. A dashboard mantém seu layout original e **exatamente três cards** — Estoque, Precificação, Fornecedores.
5. Cada card tem cor, título, dado real, ação `Detalhes`, ação `Conversar` e cabeça viva.
6. Blink, eye tracking, respiração leve, squash, happy, worried, sad, thinking, surprised, offline/error funcionam como especificado.
7. Estado emocional vem de **dados reais/eventos reais**, não aleatoriedade semântica ou texto inventado do modelo.
8. Um alerta novo real aparece no sino, pode gerar um balão único, persiste e não é reanunciado todo refresh.
9. `Conversar` abre chat **contextual de verdade** com IDs, tenant e histórico, usando provider local já configurado.
10. Chat/Ollama offline não derruba cards/notificações nem corrompe estados.
11. `Detalhes`, `Conversar`, `Marcar como lido` e operações gerenciais produzem resultados verdadeiros; botões não são placebo.
12. Usuário não perde autenticação, dados, KPIs, gráficos, tabelas, sidebar ou navbar com a refatoração.
13. Desktop, tablet e mobile apresentam espaçamento correto e não criam pilha infinita desnecessária.
14. `prefers-reduced-motion`, teclado, foco, contraste e acessibilidade são verificados.
15. Testes executados com evidência e falhas descritas de forma honesta.
16. Nenhuma mudança destrutiva em banco, Git ou outras features.
17. Nenhuma página nova de configuração dos agentes foi criada.
18. Branch está organizada em commits claros; sem merge não autorizado.
19. Relatório final inclui **arquivos alterados, arquitetura adotada, screenshots, testes, problemas resolvidos e pendências**.
20. Roteiro de apresentação demonstrável sem depender de imagens fake ou de o LLM acertar cálculos exatos.

---

# 17. ROTEIRO DE DEMONSTRAÇÃO PARA A FACULDADE

**Duração alvo: 2–4 minutos, após base inicial carregada.** Preparar dados de demonstração REAIS no banco de desenvolvimento, sem alterar produção.

1. **Dashboard:** mostrar três cabeças 2D observando discretamente a tela. Passar o mouse no card de Estoque: olhos acompanham; mouse sai e volta ao centro; blink orgânico.
2. **Cenário de estoque:** criar/ajustar em ambiente de demonstração um item com quantidade abaixo do mínimo através dos fluxos normais. Agente detecta e registra alerta (manual de teste autorizado / tick existente), sem inventar dados. Rosto muda de `idle` para `surprised` → `worried`/`sad`.
3. **Notificação:** badge do sino atualiza e o balão aparece UMA vez. Abrir o sino; a cabeça vermelha aponta visualmente para o alerta.
4. **Detalhes:** clicar e mostrar quantidade real, mínimo, origem e nome do produto.
5. **Chat:** clicar `Conversar`; chat abre no assunto correto, pergunta `Por que este produto está crítico?`; resposta baseada nos dados autorizados. Se Ollama estiver instável, demonstrar fallback honesto.
6. **Recuperação:** regularizar estoque pelo sistema e reprocessar conforme workflow; mostrar alteração real de status e expressão de felicidade breve.
7. **Fecho:** mencionar outros especialistas e que a apresentação usa dados reais, com aprovação explícita de ações.

**Fallback de apresentação:** deixar o backend/banco no estado seguro conhecido e ter casos de teste persistidos; evitar depender de alertas espontâneos no minuto exato. Não forçar notificação falsa em produção. É permitido um *modo de demonstração apenas em dev/test* que reproduz eventos reais de fixture, claramente identificado e bloqueado fora de dev.

---

# 18. FORMATO DO RELATÓRIO FINAL OBRIGATÓRIO

```markdown
# ENTREGA — ISM ASSIST V3

## Ambiente e branch
- Branch original:
- Branch de trabalho:
- Commit inicial e commits criados:
- Data e versões relevantes:

## Diagnóstico
- Componentes encontrados e estado antes:
- Funcionalidades reaproveitadas:
- Componentes removidos / substituídos:

## Etapas 0–11
Para cada etapa:
- Modificações e caminhos:
- Por que a solução foi adotada:
- Testes realmente executados:
- Resultado: PASSOU / FALHOU / BLOQUEADO:
- Evidências (logs, captura, output):

## Animações entregues
- Idle, blink, gaze, squash, happy, sad, worried, surprised, thinking, speaking, approval, success, error, offline:
- Redução de movimento:
- Controles de CPU/memória e cleanup:

## Segurança e integrações
- Origem dos alertas e tenant:
- Chat contextual e persistência:
- Ollama online/offline:
- Políticas de autorização:

## Revisão visual
- Captura Dashboard:
- Captura Sino:
- Captura Estoque:
- Resultado em mobile:

## Testes
- Comandos efetivos e contagens:
- Falhas preexistentes e novas:
- Testes manuais:

## Pendências honestas / riscos conhecidos
- ...

## Como reproduzir e apresentar
- Comandos:
- Dados de demonstração:
- Roteiro do apresentador:
```

---

# 19. OBSERVAÇÕES PARA O TIME DE DESENVOLVIMENTO

A maior armadilha deste projeto é confundir “assistente vivo” com “assistente enorme”. **Vida está nos olhos, no timing, no contexto e na precisão**, não na quantidade de componentes ou efeitos. Para cada efeito introduzido, perguntar:

- Ele comunica algo útil ou só distrai?
- Ele para quando não está visível?
- Ele reage a uma condição real e testável?
- Ele respeita reduced-motion?
- Se o usuário o ignorar, ainda consegue administrar o restaurante sem esforço?
- Ele é leve o suficiente para o notebook de apresentação?

**Um bom resultado é o sistema original do ISM, com três pequenas presenças inteligentes que parecem acompanhar o restaurante — e que sabem quando ficar quietas.**

*Fim da especificação mestre.*
