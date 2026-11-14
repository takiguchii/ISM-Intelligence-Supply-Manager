<script setup lang="ts">
import { cashDate, cashMoney, CashMovementKind, paymentLabels, type Shift, type Receipt } from '~/services/modules/cash/cashService'

defineProps<{ details: Shift }>()
defineEmits<{ receipt: [receipt: Receipt] }>()
</script>

<template>
  <div class="space-y-5">
    <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      <div class="rounded-xl border border-zinc-200 p-4 dark:border-zinc-700">
        <p class="text-sm text-zinc-500">Saldo inicial</p>
        <p class="text-lg font-semibold">{{ cashMoney(details.shift.openingAmount) }}</p>
      </div>
      <div class="rounded-xl border border-zinc-200 p-4 dark:border-zinc-700">
        <p class="text-sm text-zinc-500">Dinheiro esperado</p>
        <p class="text-lg font-semibold">{{ cashMoney(details.expectedCash) }}</p>
      </div>
      <div v-if="details.shift.countedAmount !== null" class="rounded-xl border border-zinc-200 p-4 dark:border-zinc-700">
        <p class="text-sm text-zinc-500">Dinheiro contado</p>
        <p class="text-lg font-semibold">{{ cashMoney(details.shift.countedAmount) }}</p>
        <p class="text-sm">Diferença {{ cashMoney(details.shift.countedAmount - details.expectedCash) }}</p>
      </div>
    </div>
    <dl class="grid grid-cols-2 gap-3 rounded-xl bg-zinc-100 p-4 sm:grid-cols-4 dark:bg-zinc-800">
      <div v-for="(label, method) in paymentLabels" :key="method">
        <dt class="text-sm text-zinc-500 dark:text-zinc-400">{{ label }}</dt>
        <dd class="font-semibold">{{ cashMoney(details.totals.find(total => total.method === Number(method))?.amount ?? 0) }}</dd>
      </div>
    </dl>
    <div class="grid gap-5 lg:grid-cols-2">
      <section>
        <h3 class="mb-2 font-semibold">Recebimentos</h3>
        <p v-if="!details.receipts.length" class="text-sm text-zinc-500">Nenhum recebimento no turno.</p>
        <ul v-else class="divide-y divide-zinc-200 dark:divide-zinc-700">
          <li v-for="receipt in details.receipts" :key="receipt.id" class="flex flex-wrap items-center justify-between gap-2 py-3 text-sm">
            <div>
              <p class="font-medium">Pedido #{{ receipt.orderId }} · {{ cashMoney(receipt.payments.reduce((sum, payment) => sum + payment.amount, 0)) }}</p>
              <p class="text-zinc-500">{{ receipt.payments.map(payment => paymentLabels[payment.method]).join(' + ') }} · {{ cashDate(receipt.createdAtUtc) }}</p>
            </div>
            <el-button size="small" @click="$emit('receipt', receipt)">Comprovante</el-button>
          </li>
        </ul>
      </section>
      <section>
        <h3 class="mb-2 font-semibold">Suprimentos e sangrias</h3>
        <p v-if="!details.movements.length" class="text-sm text-zinc-500">Nenhuma movimentação no turno.</p>
        <ul v-else class="divide-y divide-zinc-200 dark:divide-zinc-700">
          <li v-for="movement in details.movements" :key="movement.id" class="py-3 text-sm">
            <p class="font-medium">{{ movement.kind === CashMovementKind.Deposit ? 'Suprimento' : 'Sangria' }} · {{ cashMoney(movement.amount) }}</p>
            <p>{{ movement.reason }}</p>
            <p class="text-zinc-500">{{ cashDate(movement.createdAtUtc) }} · Operador #{{ movement.operatorId }}</p>
          </li>
        </ul>
      </section>
    </div>
  </div>
</template>
