const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const { transformSync } = require('esbuild');
const vue = require('vue');
const statuses = { Submitted: 0, Preparing: 1, Ready: 2, Completed: 3, Paid: 4, Cancelled: 5 };
const order = (status = 0, version = 0, id = 1) => ({ id, status, version, createdAtUtc: '2026-11-12T12:00:00Z', tableReference: '04', items: [{ dishId: 1, dishName: 'Prato', quantity: 2, observation: 'sem sal' }] });
const deferred = () => { let resolve; const promise = new Promise(r => { resolve = r; }); return { promise, resolve }; };
function setup(file = 'useKitchen') {
  const mounted = [], unmounted = [], listeners = new Map(); let tick; let cleared = false;
  const document = { hidden: false, addEventListener: (name, fn) => listeners.set(name, fn), removeEventListener: name => listeners.delete(name) };
  const service = { list: async () => [order()], updateStatus: async (value, status) => ({ ...value, status, version: value.version + 1 }) };
  const menu = { getDishes: async () => [{ id: 1, name: 'Prato real', categoryId: 2, isActive: false }], getCategories: async () => [{ id: 2, name: 'Principais' }] };
  const exports = {};
  const context = { exports, module: { exports }, require: name => name === 'vue' ? { ...vue, onMounted: fn => mounted.push(fn), onUnmounted: fn => unmounted.push(fn) } : name.includes('menuService') ? { menuService: menu } : { orderService: service, OrderStatus: statuses }, document, setInterval: fn => { tick = fn; return 1; }, clearInterval: () => { cleared = true; } };
  vm.runInNewContext(transformSync(fs.readFileSync(`composables/${file}.ts`, 'utf8'), { loader: 'ts', format: 'cjs' }).code, context);
  return { api: context.module.exports[file](), service, menu, mounted, unmounted, listeners, document, tick: () => tick(), cleared: () => cleared };
}
test('fila contém só recebidos, preparo e prontos, com os mais antigos primeiro', async () => {
  const { api, service } = setup();
  service.list = async () => Array.from({ length: 6 }, (_, status) => ({ ...order(status, 0, status), createdAtUtc: `2026-11-12T1${5-status}:00:00Z` }));
  await api.refresh(); assert.deepEqual(Array.from(api.activeOrders.value, x => x.status), [2, 1, 0]);
  assert.equal(api.orders.value.length, 6); // O histórico conserva os estados encerrados.
});
test('preparo e despacho usam versão atual; chef não avança depois de pronto', async () => {
  const { api, service } = setup(); await api.refresh(); const calls = [];
  service.updateStatus = async (value, status) => { calls.push([value.version, status]); return { ...value, status, version: value.version + 1 }; };
  assert.equal(await api.advance(1), true); assert.equal(await api.advance(1), true); assert.equal(await api.advance(1), false);
  assert.deepEqual(calls, [[0, 1], [1, 2]]); assert.match(api.feedback.value, /garçom/);
});
test('bloqueia clique duplicado e consulta antiga não desfaz o preparo', async () => {
  const { api, service } = setup(); await api.refresh();
  const list = deferred(), update = deferred(); service.list = () => list.promise; const polling = api.refresh();
  let count = 0; service.updateStatus = () => { count++; return update.promise; };
  const first = api.advance(1); assert.equal(await api.advance(1), false);
  update.resolve(order(1, 1)); await first; list.resolve([order()]); await polling;
  assert.equal(count, 1); assert.equal(api.orders.value[0].status, 1);
});
test('conflito recarrega versão e preserva erro; permite nova tentativa válida', async () => {
  const { api, service } = setup(); await api.refresh();
  service.updateStatus = async () => { throw { data: { detail: 'Pedido foi alterado' } }; };
  service.list = async () => [order(1, 2)]; assert.equal(await api.advance(1), false);
  assert.equal(api.orders.value[0].version, 2); assert.equal(api.error.value, 'Pedido foi alterado');
  service.updateStatus = async (value, status) => ({ ...value, status, version: 3 });
  assert.equal(await api.advance(1), true); assert.equal(api.error.value, '');
});
test('polling recebe novos pedidos, remove entregues, respeita aba oculta e desmontagem', async () => {
  const env = setup(); const { api, service } = env; await api.refresh();
  let calls = 0; service.list = async () => { calls++; return [order(3), order(0, 0, 2)]; };
  env.mounted.forEach(fn => fn()); await new Promise(setImmediate);
  assert.equal(api.activeOrders.value[0].id, 2); const before = calls;
  env.document.hidden = true; env.tick(); assert.equal(calls, before);
  env.document.hidden = false; env.listeners.get('visibilitychange')(); await new Promise(setImmediate); assert.equal(calls, before + 1);
  env.unmounted.forEach(fn => fn()); assert.equal(env.cleared(), true); assert.equal(env.listeners.size, 0);
  await api.refresh(); assert.equal(calls, before + 1);
});
test('falha de rede conserva pedidos e recuperação atualiza a fila', async () => {
  const { api, service } = setup(); await api.refresh();
  service.list = async () => { throw new Error('offline'); }; await api.refresh();
  assert.equal(api.orders.value.length, 1); assert.match(api.error.value, /conectar/);
  service.list = async () => []; await api.refresh(); assert.equal(api.error.value, ''); assert.equal(api.orders.value.length, 0);
});
test('consulta do chef carrega cardápio real, disponibilidade e categoria, recupera falha', async () => {
  const { api, menu } = setup('useMenuCatalog'); await api.refresh();
  assert.equal(api.categoryNames.value.get(2), 'Principais'); assert.equal(api.dishes.value[0].isActive, false);
  menu.getDishes = async () => { throw new Error('offline'); }; await api.refresh(); assert.match(api.error.value, /cardápio/);
  assert.equal(api.dishes.value.length, 1);
  menu.getDishes = async () => []; await api.refresh(); assert.equal(api.error.value, ''); assert.equal(api.dishes.value.length, 0);
});
