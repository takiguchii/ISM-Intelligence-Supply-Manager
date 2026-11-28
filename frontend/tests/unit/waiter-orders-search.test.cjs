const { test } = require('node:test')
const assert = require('node:assert/strict')
const fs = require('node:fs')
const vm = require('node:vm')
const { transformSync } = require('esbuild')

const code = transformSync(fs.readFileSync('composables/useWaiter.ts', 'utf8'), { loader: 'ts', format: 'cjs' }).code
const statuses = { Submitted: 0, Preparing: 1, Ready: 2, Completed: 3, Paid: 4, Cancelled: 5 }
const moduleRef = { exports: {} }
const context = {
  exports: moduleRef.exports, module: moduleRef,
  require: name => name === 'vue'
    ? { computed() {}, onMounted() {}, onUnmounted() {}, ref() {} }
    : name.includes('menuService') ? { menuService: {} } : { orderService: {}, OrderStatus: statuses }
}
vm.runInNewContext(code, context)
const { filterWaiterOrders } = moduleRef.exports

const orders = [
  { id: 417, tableReference: 'Mesa 12', status: 'Enviado', items: [{ name: 'Risoto', observation: '' }] },
  { id: 418, tableReference: 'Mesa 3', status: 'Pronto', items: [{ name: 'Hambúrguer', observation: '' }] }
]

test('busca pedido por mesa ao acompanhar pedidos', () => {
  assert.deepEqual(filterWaiterOrders(orders, 'mesa 12').map(order => order.id), [417])
})

test('busca pedido por número sem quebrar o filtro de acompanhamento', () => {
  assert.deepEqual(filterWaiterOrders(orders, '418').map(order => order.id), [418])
})
