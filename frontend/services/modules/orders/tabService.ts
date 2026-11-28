import { apiClientWithAuth } from '~/services/api/client'
export enum BackendPaymentMethod { CreditCard = 1, DebitCard = 2, Pix = 3, Cash = 4, Voucher = 5, Other = 6 }

export interface OpenTab {
  id: number
  tableReference: string
  total: number
  canPay: boolean
  orderIds: number[]
}

export interface TabPayment {
  tabId: number
  paymentMethod: BackendPaymentMethod
  total: number
  paidAtUtc: string
}

export const tabService = {
  listOpen: () => apiClientWithAuth<OpenTab[]>('/api/orders/tabs/open'),
  open: (tableReference: string) => apiClientWithAuth<OpenTab>('/api/orders/tabs/open', { query: { tableReference } }),
  pay: (tabId: number, idempotencyKey: string, paymentMethod: BackendPaymentMethod, total: number) =>
    apiClientWithAuth<TabPayment>(`/api/orders/tabs/${tabId}/payments`, {
      method: 'POST', body: { idempotencyKey, paymentMethod, total }
    })
}
