# Evidência Ollama

12 chamadas `POST /api/generate` foram executadas contra `gemma3:270m`, com `temperature=0`, respostas não vazias e encerramento limpo do modelo depois do teste. A amostra de `qwen3.5:4b` atingiu o timeout de 90 segundos em CPU durante o cold start; o processo foi parado sem alteração de arquivos do projeto.
