<script setup lang="ts">
import { apiClientWithAuth } from "~/services/api/client";
import { settingsError } from "~/services/modules/system/settingsService";
const props = defineProps<{ restaurantId: number | null; superAdmin: boolean }>();
interface Plan { id: number; name: string; description: string; monthlyPrice: number; maxUsers: number; maxProducts: number; maxDishes: number; maxCategories: number }
interface Invoice { id: number; planName: string; amount: number; createdAtUtc: string }
interface SubscriptionResponse { demoEnabled: boolean; subscription: { planId: number | null; isActive: boolean; trialEndAtUtc: string | null; plans: Plan[]; invoices: Invoice[] } }
const data = ref<SubscriptionResponse | null>(null);
const loading = ref(false);
const saving = ref(false);
const error = ref("");
const success = ref("");
const selectedPlan = ref<number | null>(null);
const confirmed = ref(false);
const url = computed(() => props.superAdmin ? `/api/restaurants/${props.restaurantId}/subscription` : "/api/me/restaurant/subscription");
let version = 0;
async function load() {
  const current = ++version;
  data.value = null; selectedPlan.value = null; confirmed.value = false; error.value = "";
  if (!props.restaurantId) return;
  loading.value = true;
  try {
    const result = await apiClientWithAuth<SubscriptionResponse>(url.value);
    if (current === version) { data.value = result; selectedPlan.value = result.subscription.planId; }
  } catch (e) { if (current === version) error.value = settingsError(e); }
  finally { if (current === version) loading.value = false; }
}
watch(() => [props.restaurantId, props.superAdmin], () => { success.value = ""; load(); }, { immediate: true });
async function changePlan() {
  if (!selectedPlan.value || !confirmed.value || saving.value) return;
  const current = version;
  saving.value = true; error.value = ""; success.value = "";
  try {
    await apiClientWithAuth(`${url.value}/plan`, { method: "POST", body: { planId: selectedPlan.value } });
    if (current === version) { await load(); success.value = "Plano atualizado e cobrança demonstrativa registrada."; }
  } catch (e) { if (current === version) error.value = settingsError(e); }
  finally { saving.value = false; }
}
const money = (amount: number) => new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(amount);
</script>
<template>
  <div class="space-y-6">
    <div class="settings-card">
      <h2>Assinatura &amp; pagamento</h2>
      <p class="settings-hint">Ambiente acadêmico: as cobranças abaixo são simulações. Nenhum pagamento é processado e nenhum dado de cartão é solicitado.</p>
      <p v-if="!restaurantId" class="settings-hint">Selecione um restaurante para consultar sua assinatura.</p>
      <p v-if="loading" role="status">Carregando assinatura…</p>
      <p v-if="error" role="alert" class="text-red-600 dark:text-red-400">{{ error }}</p>
      <p v-if="success" role="status" class="text-emerald-600 dark:text-emerald-400">{{ success }}</p>
      <template v-if="data">
        <p class="font-medium">Plano atual: {{ data.subscription.plans.find(p => p.id === data!.subscription.planId)?.name || 'Sem plano' }} · {{ data.subscription.isActive ? 'Ativo' : 'Inativo' }}</p>
        <p v-if="data.subscription.trialEndAtUtc" class="settings-hint">Fim do período de avaliação: {{ new Date(data.subscription.trialEndAtUtc).toLocaleDateString('pt-BR') }}</p>
        <form v-if="data.demoEnabled" @submit.prevent="changePlan">
          <fieldset :disabled="saving" class="space-y-5">
            <label for="subscription-plan">Plano<select id="subscription-plan" v-model="selectedPlan" required><option v-for="plan in data.subscription.plans" :key="plan.id" :value="plan.id">{{ plan.name }} — {{ money(plan.monthlyPrice) }}/mês · {{ plan.maxUsers }} usuários · {{ plan.maxDishes }} pratos · {{ plan.maxProducts }} produtos</option></select></label>
            <label class="!flex-row items-center gap-3"><input v-model="confirmed" type="checkbox" required class="h-5 w-5 accent-indigo-600" /><span>Confirmo a alteração de plano e o registro de uma cobrança simulada.</span></label>
            <button type="submit" class="settings-primary" :disabled="saving || !confirmed || (selectedPlan === data.subscription.planId && data.subscription.isActive)">{{ saving ? 'Atualizando…' : 'Aplicar plano demonstrativo' }}</button>
          </fieldset>
        </form>
        <p v-else class="settings-hint">Alterações demonstrativas estão desativadas neste ambiente.</p>
      </template>
    </div>
    <div v-if="data" class="settings-card">
      <h2>Histórico de cobranças demonstrativas</h2>
      <p v-if="!data.subscription.invoices.length" class="settings-hint">Nenhuma cobrança demonstrativa registrada.</p>
      <div v-else class="overflow-x-auto"><table class="w-full text-sm text-left"><thead><tr><th class="p-3">Data</th><th class="p-3">Plano</th><th class="p-3">Valor</th><th class="p-3">Status</th></tr></thead><tbody><tr v-for="invoice in data.subscription.invoices" :key="invoice.id" class="border-t border-zinc-200 dark:border-zinc-800"><td class="p-3">{{ new Date(invoice.createdAtUtc).toLocaleString('pt-BR') }}</td><td class="p-3">{{ invoice.planName }}</td><td class="p-3">{{ money(invoice.amount) }}</td><td class="p-3">Simulada</td></tr></tbody></table></div>
    </div>
  </div>
</template>
