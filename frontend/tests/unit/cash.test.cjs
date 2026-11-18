const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const { transformSync } = require('esbuild');

const code = transformSync(fs.readFileSync('services/modules/cash/cashService.ts', 'utf8'), { loader: 'ts', format: 'cjs' }).code;
const statuses = { Submitted: 0, Preparing: 1, Ready: 2, Completed: 3, Paid: 4, Cancelled: 5 };
const order = () => ({ id: 10, status: statuses.Completed, version: 4, tableReference: '03', items: [{ dishId: 1, dishName: 'Prato', unitPrice: 12.35, quantity: 2 }] });
const pix = () => [{ method: 1, amount: 24.70, tendered: null }];

async function setup() {
  const storage = new Map();
  const calls = [];
  const orders = { getById: async id => { calls.push(id); return order(); } };
  let queue = Promise.resolve();
  const exports = {};
  const context = {
    exports, module: { exports },
    require: () => ({ orderService: orders, OrderStatus: statuses }),
    localStorage: { getItem: key => storage.get(key) ?? null, setItem: (key, value) => storage.set(key, value) },
    navigator: { locks: { request: (_key, action) => { const pending = queue.then(action); queue = pending.catch(() => {}); return pending; } } }
  };
  vm.runInNewContext(code, context);
  const api = context.module.exports.createCashService(7, 2);
  const shift = await api.open(100.10);
  return { api, shift, orders, calls, storage, CashOrderUnavailableError: context.module.exports.CashOrderUnavailableError };
}

for (const [label, status] of [['cancelado', statuses.Cancelled], ['pago', statuses.Paid], ['em preparo', statuses.Preparing], ['pronto', statuses.Ready]]) {
  test(`não emite recibo para pedido ${label} no servidor depois de abrir o modal`, async () => {
    const { api, shift, orders, storage, CashOrderUnavailableError } = await setup();
    const selected = order();
    orders.getById = async () => ({ ...selected, status, version: selected.version + 1 });
    const before = storage.get('ism-cash-v1:7');
    await assert.rejects(api.receive(shift.shift.id, selected, 'attempt', pix()), CashOrderUnavailableError);
    assert.equal(storage.get('ism-cash-v1:7'), before, 'recibos, saldo e versão do turno devem permanecer intactos');
  });
}

test('bloqueia pedido editado mesmo quando continua entregue e o total é igual', async () => {
  const { api, shift, orders, CashOrderUnavailableError } = await setup();
  orders.getById = async () => ({ ...order(), version: 5 });
  await assert.rejects(api.receive(shift.shift.id, order(), 'edited', pix()), CashOrderUnavailableError);
  assert.equal((await api.get(shift.shift.id)).receipts.length, 0);
});

test('consulta o pedido pelo id e usa o preço confirmado no servidor', async () => {
  const { api, shift, calls } = await setup();
  const stale = order();
  stale.items[0].unitPrice = 1;
  await assert.rejects(api.receive(shift.shift.id, stale, 'tampered', [{ method: 1, amount: 2, tendered: null }]), /soma dos pagamentos/);
  assert.deepEqual(calls, [stale.id]);
  assert.equal((await api.get(shift.shift.id)).receipts.length, 0);
});

test('pedido removido invalida a confirmação sem gravar recibo', async () => {
  const { api, shift, orders, CashOrderUnavailableError } = await setup();
  orders.getById = async () => { throw { response: { status: 404 } }; };
  await assert.rejects(api.receive(shift.shift.id, order(), 'deleted', pix()), CashOrderUnavailableError);
  assert.equal((await api.get(shift.shift.id)).receipts.length, 0);
});

for (const failure of [new Error('offline'), { statusCode: 500 }]) {
  test(`falha na API (${failure.message ?? failure.statusCode}) impede recebimento e permite tentar novamente`, async () => {
    const { api, shift, orders, storage } = await setup();
    const before = storage.get('ism-cash-v1:7');
    orders.getById = async () => { throw failure; };
    await assert.rejects(api.receive(shift.shift.id, order(), 'retry', pix()), /Nenhum recebimento foi registrado/);
    assert.equal(storage.get('ism-cash-v1:7'), before);
    orders.getById = async () => order();
    await api.receive(shift.shift.id, order(), 'retry', pix());
    assert.equal((await api.get(shift.shift.id)).receipts.length, 1);
  });
}

test('pedido atual permite pagamento misto com centavos e troco sem duplicar a repetição', async () => {
  const { api, shift } = await setup();
  const parts = [{ method: 0, amount: 10.20, tendered: 20 }, { method: 1, amount: 14.50, tendered: null }];
  const receipt = await api.receive(shift.shift.id, order(), 'mixed', parts);
  assert.equal(receipt.payments[0].change, 9.80);
  assert.equal((await api.get(shift.shift.id)).expectedCash, 110.30);
  assert.equal((await api.receive(shift.shift.id, order(), 'mixed', parts)).id, receipt.id);
  assert.equal((await api.get(shift.shift.id)).receipts.length, 1);
  await assert.rejects(api.receive(shift.shift.id, order(), 'different', parts), /já tem um recebimento/);
});

test('revalida turno depois da consulta: fechamento em outra aba impede o recebimento', async () => {
  const { api, shift, orders } = await setup();
  let resolve;
  orders.getById = () => new Promise(done => { resolve = done; });
  const receiving = api.receive(shift.shift.id, order(), 'closed', pix());
  await api.close(shift.shift.id, shift.version, 100.10);
  resolve(order());
  await assert.rejects(receiving, /turno está encerrado/);
  assert.equal((await api.get(shift.shift.id)).receipts.length, 0);
});

test('mantém bloqueios de total incorreto e dinheiro insuficiente após validar o pedido', async () => {
  const { api, shift } = await setup();
  await assert.rejects(api.receive(shift.shift.id, order(), 'wrong-total', [{ method: 1, amount: 24.69, tendered: null }]), /soma dos pagamentos/);
  await assert.rejects(api.receive(shift.shift.id, order(), 'short-cash', [{ method: 0, amount: 24.70, tendered: 20 }]), /dinheiro entregue/);
  assert.equal((await api.get(shift.shift.id)).receipts.length, 0);
});
