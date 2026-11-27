# Apoio — Modelo Ollama local para notebook Ryzen e 20 GB de RAM

## Contexto e seleção sugerida

Hardware informado pelo responsável: Ryzen 5 (classe 5500/5000) e 20 GB RAM, notebook Fedora/Linux de desenvolvimento. Verificar modelo real da CPU, instruções disponíveis, memória livre e versão do Ollama em tempo de execução.

**Candidato principal para testar:** `qwen3.5:9b` (pesos quantizados ~6,6 GB + memória de contexto/execução). Na biblioteca pública do Ollama, é um modelo com suporte a ferramentas e raciocínio, relevante para português e consultas estruturadas. Endereço oficial: https://ollama.com/library/qwen3.5:9b. Ele deve caber em 20 GB no cenário típico com contexto moderado, mas isso **não garante desempenho aceitável em CPU nem ausência de swap**.

**Alternativa se a inferência for lenta ou consumir memória demais:** `qwen3:8b` (https://ollama.com/library/qwen3:8b), ou `qwen3.5:4b` (https://ollama.com/library/qwen3.5:4b). Nenhum modelo local elimina alucinações; segurança e cálculo devem ficar nas ferramentas determinísticas.

## Instalação: regras para o Codex

1. Inspecionar instalação `command -v ollama; ollama --version; systemctl --user status ollama || systemctl status ollama`; não reinstalar se estiver funcional.
2. Consultar documentação oficial antes de instalar; evitar scripts remotos sem inspeção. Solicitar aprovação para comandos com privilégios elevados se o runner exigir, não contornar políticas da máquina.
3. Se Ollama não estiver instalado, instalar via método compatível com Fedora informado pela documentação oficial; registrar método/versão.
4. `ollama pull qwen3.5:9b`; `ollama list`; executar smoke via API `POST /api/chat` com JSON válido, tempo limite realista e sem segredos.
5. Se não passar no benchmark, registrar motivo e testar `qwen3:8b`; se necessário `qwen3.5:4b`.
6. Configurar no `.env` local **sem commit de secrets** a opção de modelo presente no `docker-compose.yml`/backend, o serviço e `ASSISTANT_AI_ENABLED=true` apenas no ambiente de teste autorizado.
7. Para backend em contêiner, conferir `ASSISTANT_OLLAMA_BASE_URL` (snapshot usa `http://host.docker.internal:11434`) e reachability real do host: não supor que `host.docker.internal` funcione em todo Linux sem configuração.

## Configuração conservadora inicial

- Testar janela de contexto 4096 ou 8192 tokens, ajustando ao comprimento real dos contratos. Não reservar janela máxima anunciada.
- Uma inferência simultânea inicial; não carregar múltiplos modelos grandes em paralelo.
- Deixar memória para Docker, MySQL, build e sistema; observar swap e OOM. Não modificar limites globais do sistema sem necessidade.
- O snapshot usa `ASSISTANT_AI_TIMEOUT_SECONDS=20` e isso pode ser insuficiente para inferência 9B na CPU; testar 90–180 segundos em desenvolvimento, com timeouts individuais e feedback visual. Não fixar timeout de produção sem evidência.
- Separar benchmarks de *tool-calling JSON estruturado* e respostas em linguagem natural; medir taxa de sucesso, não só latência.

## Benchmark reproduzível

Rodar no mínimo 12 prompts (3 cada): roteamento estruturado, consulta contextual, recusa de ferramenta proibida, síntese com evidências. Medir cold start, p50/p95, RAM livre mínima, swap, erro de schema e taxa de JSON válido. Repetir em dois ciclos. Resultado aceitável: 100% de autorização (determinística no backend), JSON conforme contratos e sem OOM. LLM lento é razão para ajustar UX/fila ou usar fallback, **não para relaxar as permissões**.

Registrar modelo escolhido, tag/hash, configuração, tempos e resultados em `relatorios/OLLAMA-benchmark.md`.

## Observação de arquitetura

O Ollama deste notebook é infraestrutura de **desenvolvimento e apresentação**, não prova de escalabilidade comercial do SaaS. Para nuvem/API comercial posterior, trocar apenas o provider e preservar as regras determinísticas, o controle de custos, limites de uso e isolamento de dados.
