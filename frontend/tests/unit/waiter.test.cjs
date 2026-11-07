const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const { transformSync } = require('esbuild');
const vue = require('vue');
const { randomUUID } = require('node:crypto');
const code = transformSync(fs.readFileSync('composables/useWaiter.ts', 'utf8'), { loader: 'ts', format: 'cjs' }).code;
const statuses = { Submitted: 0, Preparing: 1, Ready: 2, Completed: 3, Paid: 4, Cancelled: 5 };
const order = (status = 2, version = 4) => ({ id: 10, status, version, tableReference: '03', items: [{ dishId: 1, dishName: 'Prato', unitPrice: 40, quantity: 2, observation: '' }] });
const deferred = () => { let resolve; const promise = new Promise(r => { resolve = r }); return { promise, resolve }; };
function setup() {
  const state = new Map(); const mounted = []; const unmounted = []; let tick; let cleared = false;
  const calls = [];
  const service = { list: async () => [order()], create: async body => { calls.push(body); return order(0); }, updateStatus: async (value, status) => order(status, value.version + 1) };
  const menu = { getDishes: async () => [{ id: 1, name: 'Prato', categoryId: 3, price: 40, isActive: true }, { id: 2, name: 'Inativo', categoryId: 3, price: 5, isActive: false }], getCategories: async () => [{ id: 3, name: 'Principais' }] };
  const exports = {};
  const context = { exports, module: { exports }, require: name => name === 'vue' ? { ...vue, onMounted: fn => mounted.push(fn), onUnmounted: fn => unmounted.push(fn) } : name.includes('menuService') ? { menuService: menu } : { orderService: service, OrderStatus: statuses }, useAuthStore: () => ({ currentUser: { id: '1', restaurantId: 7 } }), useState: (key, init) => { if (!state.has(key)) state.set(key, vue.ref(init())); return state.get(key); }, crypto: { randomUUID }, document: { hidden: false }, setInterval: fn => { tick = fn; return 1; }, clearInterval: () => { cleared = true; } };
  vm.runInNewContext(code, context);
  const api = context.module.exports.useWaiter();
  return { api, service, menu, calls, mounted, unmounted, state, tick: () => tick(), cleared: () => cleared };
}
const fill = api => { api.table.value = '03'; api.addDish({ id: 1, name: 'Prato', price: 40, available: true }); };
test('carrega categorias reais, bloqueia inativos e calcula os seis status', async () => {
  const { api, service } = setup(); await api.refreshMenu();
  assert.equal(api.dishes.value[0].category, 'Principais'); api.addDish(api.dishes.value[1]); assert.equal(api.cart.value.length, 0);
  service.list = async () => Array.from({ length: 6 }, (_, status) => ({ ...order(status), id: status }));
  await api.refreshOrders(); assert.equal(api.activeOrders.value.length, 3); assert.equal(api.readyOrders.value.length, 1); assert.equal(api.occupiedTables.value, 1);
  assert.equal(api.orders.value[4].status, 'Pago'); assert.equal(api.orders.value[5].status, 'Cancelado');
});
test('falha conserva o carrinho e reutiliza UUID; sucesso limpa; conteúdo novo usa outro UUID', async () => {
  const { api, service, calls } = setup(); fill(api);
  service.create = async body => { calls.push(JSON.parse(JSON.stringify(body))); throw { data: { detail: 'offline' } }; };
  await assert.rejects(api.submitOrder()); assert.equal(api.cart.value.length, 1);
  await assert.rejects(api.submitOrder()); assert.equal(calls[0].requestId, calls[1].requestId);
  api.cart.value[0].quantity = 2; await assert.rejects(api.submitOrder()); assert.notEqual(calls[1].requestId, calls[2].requestId);
  service.create = async body => { calls.push(body); return order(0); };
  assert.equal(await api.submitOrder(), 10); assert.equal(calls[2].requestId, calls[3].requestId);
  assert.equal(api.cart.value.length, 0); assert.equal(api.table.value, '');
  assert.equal('price' in calls[3].items[0], false);
});
test('bloqueia envio duplicado e alterações enquanto aguarda resposta', async () => {
  const { api, service } = setup(); fill(api); const pending = deferred(); let count = 0;
  service.create = () => { count++; return pending.promise; }; const first = api.submitOrder();
  assert.equal(await api.submitOrder(), null); api.changeQuantity(0, 1); assert.equal(api.cart.value[0].quantity, 1);
  pending.resolve(order(0)); await first; assert.equal(count, 1);
});
test('entrega usa versão atual; conflito recarrega os pedidos', async () => {
  const { api, service } = setup(); await api.refreshOrders();
  service.updateStatus = async (value, status) => { assert.equal(value.version, 4); assert.equal(status, 3); return order(3, 5); };
  assert.equal(await api.confirmDelivery(10), true); assert.equal(api.orders.value[0].status, 'Entregue');
  await api.refreshOrders(); service.updateStatus = async () => { throw { data: { detail: 'versão antiga' } }; }; service.list = async () => [order(3, 6)];
  await assert.rejects(api.confirmDelivery(10)); assert.equal(api.orders.value[0].version, 6);
});
test('consulta antiga não sobrescreve entrega e timer é encerrado ao desmontar', async () => {
  const env = setup(); const { api, service } = env; await api.refreshOrders();
  const pending = deferred(); service.list = () => pending.promise; const refresh = api.refreshOrders();
  await api.confirmDelivery(10); pending.resolve([order()]); await refresh; assert.equal(api.orders.value[0].status, 'Entregue');
  env.mounted.forEach(fn => fn()); env.unmounted.forEach(fn => fn()); assert.equal(env.cleared(), true);
});
test('valida mesa, limite de itens e observações antes da API', async () => {
  const { api, calls } = setup(); await assert.rejects(api.submitOrder()); fill(api);
  api.cart.value[0].observation = 'a'.repeat(501); await assert.rejects(api.submitOrder()); assert.equal(calls.length, 0);
});
