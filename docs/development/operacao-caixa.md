# Operação de caixa local

A rota `/caixa` é restrita a administradores e gerentes vinculados a um restaurante. Turnos, movimentações e comprovantes internos são armazenados no navegador por restaurante. Esta etapa não dá baixa no pagamento do pedido no servidor nem substitui a integração financeira.

## Validação antes do recebimento

Ao confirmar o modal, o serviço de caixa consulta `GET /api/orders/{id}`. Apenas pedidos que continuam entregues e com a mesma versão selecionada podem gerar um recebimento. O cálculo usa os preços retornados pelo servidor.

Pedidos cancelados, pagos, removidos ou alterados bloqueiam a confirmação, fecham o modal e provocam uma atualização da fila. É necessário abrir o pedido novamente para conferir alterações. Falhas de rede ou da API mantêm o modal aberto para uma nova tentativa, sem gravar recibo ou movimentar o saldo. O turno e a duplicidade local também são conferidos no momento da gravação.

Essa consulta reduz o uso de dados antigos, mas não cria uma transação entre servidor e armazenamento local: ainda pode haver alteração no servidor depois da consulta. A integração definitiva deve persistir recebimento e quitação no backend de forma atômica, com controle de versão e idempotência. Não há sincronização de turnos/recibos entre dispositivos nesta etapa.

## Verificação

Execute `npm run test:cash` dentro de `frontend`. Os testes cobrem alterações de status/versão durante o modal, exclusão, indisponibilidade da API, nova tentativa, pagamento misto com troco, duplicidade e fechamento do turno durante a consulta. Complete a validação com `npm run build` e os demais testes do frontend.

Na validação em tela, abra um pedido entregue no caixa e altere esse pedido em outra sessão antes de confirmar o recebimento. Repita com cancelamento, edição dos itens, quitação e exclusão. Todos devem bloquear o comprovante. Depois receba um pedido sem alterações e confira que apenas esse recebimento aparece no turno, com saldo e troco corretos.
