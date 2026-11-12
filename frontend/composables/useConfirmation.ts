import { onUnmounted, reactive } from 'vue'

export interface ConfirmationOptions {
  title: string
  message: string
  context?: string
  detail?: string
  confirmLabel?: string
  tone?: 'danger' | 'success' | 'default'
}
export interface ConfirmationState extends ConfirmationOptions { open: boolean }

export function useConfirmation() {
  const state = reactive<ConfirmationState>({ open: false, title: '', message: '' })
  let resolvePending: ((accepted: boolean) => void) | undefined
  function finish(accepted: boolean) {
    state.open = false
    const resolve = resolvePending
    resolvePending = undefined
    resolve?.(accepted)
  }
  function ask(options: ConfirmationOptions): Promise<boolean> {
    if (state.open) return Promise.resolve(false)
    Object.assign(state, { context: '', detail: '', confirmLabel: 'Confirmar', tone: 'default' }, options, { open: true })
    return new Promise(resolve => { resolvePending = resolve })
  }
  const cancel = () => finish(false)
  const accept = () => finish(true)
  onUnmounted(cancel)
  return { state, ask, cancel, accept }
}
