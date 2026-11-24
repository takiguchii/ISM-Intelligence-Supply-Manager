# Notificações Telegram/Fors

## Resultado

**BLOCKED — credenciais ausentes no executor.**

As variáveis `FORS_BOT_TOKEN`, `FORS_CHAT_ID`, `AUDREY_BOT_TOKEN`, `AUDREY_CHAT_ID`, `TELEGRAM_BOT_TOKEN` e `TELEGRAM_CHAT_ID` estavam ausentes. Nenhum envio foi tentado e nenhum segredo foi gravado no repositório ou nos relatórios.

O token fornecido anteriormente na conversa deve ser rotacionado antes de qualquer configuração definitiva, pois foi exposto. Após a rotação, configurar somente `FORS_BOT_TOKEN` e `FORS_CHAT_ID` por canal seguro e repetir a verificação de entrega.
