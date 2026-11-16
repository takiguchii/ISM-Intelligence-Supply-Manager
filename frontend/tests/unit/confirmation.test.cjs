const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const { transformSync } = require('esbuild');
const vue = require('vue');
function setup() {
  let unmount;
  const exports = {};
  const context = { exports, module: { exports }, require: () => ({ ...vue, onUnmounted: fn => { unmount = fn; } }) };
  vm.runInNewContext(transformSync(fs.readFileSync('composables/useConfirmation.ts', 'utf8'), { loader: 'ts', format: 'cjs' }).code, context);
  return { dialog: context.module.exports.useConfirmation(), unmount: () => unmount() };
}
test('voltar ou fechar não executa a operação aguardando confirmação', async () => {
  const { dialog } = setup(); let writes = 0;
  const action = async () => { if (await dialog.ask({ title: 'Entregar', message: 'Confirme a entrega' })) writes++; };
  const pending = action(); assert.equal(dialog.state.open, true); dialog.cancel(); await pending;
  assert.equal(writes, 0); assert.equal(dialog.state.open, false);
});
test('confirmar permite uma única operação e limpa a promessa pendente', async () => {
  const { dialog } = setup(); let writes = 0;
  const action = async () => { if (await dialog.ask({ title: 'Entregar', message: 'Confirme', context: 'Mesa 04', tone: 'success' })) writes++; };
  const pending = action(); assert.equal(dialog.state.context, 'Mesa 04'); dialog.accept(); dialog.accept(); await pending;
  assert.equal(writes, 1); assert.equal(dialog.state.open, false);
});
test('cliques repetidos não substituem a confirmação da primeira ação', async () => {
  const { dialog } = setup(); const first = dialog.ask({ title: 'Pedido 1', message: 'Confirme' });
  assert.equal(await dialog.ask({ title: 'Pedido 2', message: 'Outro' }), false);
  assert.equal(dialog.state.title, 'Pedido 1'); dialog.accept(); assert.equal(await first, true);
  const next = dialog.ask({ title: 'Próxima ação', message: 'Confirme' });
  assert.equal(dialog.state.context, ''); assert.equal(dialog.state.tone, 'default'); dialog.cancel(); assert.equal(await next, false);
});
test('navegar para outra tela cancela a operação que aguardava confirmação', async () => {
  const { dialog, unmount } = setup(); const pending = dialog.ask({ title: 'Excluir', message: 'Confirme' });
  unmount(); assert.equal(await pending, false); assert.equal(dialog.state.open, false);
});
