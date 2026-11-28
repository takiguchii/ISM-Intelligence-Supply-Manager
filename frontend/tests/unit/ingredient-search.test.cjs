const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const { transformSync } = require('esbuild');
const vue = require('vue');
const { randomUUID } = require('node:crypto');

const source = fs.readFileSync('composables/useWaiter.ts', 'utf8');
const code = transformSync(source, { loader: 'ts', format: 'cjs' }).code;
const statuses = { Submitted: 0, Preparing: 1, Ready: 2, Completed: 3, Paid: 4, Cancelled: 5 };

function loadWaiter() {
  const exports = {};
  const context = {
    exports,
    module: { exports },
    require: name => name === 'vue' ? vue : name.includes('menuService')
      ? { menuService: { getDishes: async () => [], getCategories: async () => [] } }
      : { orderService: {}, OrderStatus: statuses },
    useAuthStore: () => ({ currentUser: { id: 'test', restaurantId: 1 } }),
    useState: (_key, init) => vue.ref(init()),
    crypto: { randomUUID },
    setInterval: () => 1,
    clearInterval() {},
    document: { hidden: false }
  };
  vm.runInNewContext(code, context);
  return context.module.exports;
}

test('busca pratos por ingrediente preservado, ignorando acentos e caixa', () => {
  const { filterWaiterDishes } = loadWaiter();
  const dishes = [{
    name: 'Risoto da casa',
    description: 'Arroz cremoso',
    category: 'Principais',
    ingredients: ['Cogumelo paris']
  }];

  assert.deepEqual(filterWaiterDishes(dishes, 'COGUMELO PÁRIS'), dishes);
});
