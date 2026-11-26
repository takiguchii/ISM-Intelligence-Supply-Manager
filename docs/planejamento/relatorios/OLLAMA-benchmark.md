# Benchmark Ollama local

## Resultado

`gemma3:270m` foi a única alternativa medida em 12 prompts reais: 12/12 respostas, sem erro de JSON da API, sem OOM e sem concorrência simultânea. Cold start: 2.121 s; chamadas aquecidas: 448–464 ms; p50 aproximado: 456 ms; p95 observado: 2.121 s.

`qwen3.5:4b` está instalado, mas a amostra de cold start excedeu 90 s em CPU e foi interrompida pelo timeout do cliente. Não foi escolhido para a apresentação. `qwen3.5:9b` não está instalado e não foi baixado para evitar consumo/rede sem necessidade.

## Decisão operacional

Manter o LLM opcional e usar `gemma3:270m` somente para conversa curta/síntese auxiliar em desenvolvimento. Autorização, cálculos e fallback continuam determinísticos no backend. O Ollama foi parado após a medição para liberar memória.
