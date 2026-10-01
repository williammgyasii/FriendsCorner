import { createApi } from '@reduxjs/toolkit/query/react'
import { baseQuery } from './baseQuery.ts'

export type BillingRequest = {
  action: 'checkout' | 'portal' | 'confirm'
  plan?: string | null
  returnUrl?: string
  sessionId?: string
}

export const billingApi = createApi({
  reducerPath: 'billingApi',
  baseQuery,
  endpoints: (build) => ({
    startBilling: build.mutation<{ url: string }, BillingRequest>({
      query: (body) => ({ url: 'billing', method: 'POST', body }),
    }),
    confirmCheckout: build.mutation<null, { sessionId: string }>({
      query: ({ sessionId }) => ({ url: 'billing', method: 'POST', body: { action: 'confirm', sessionId } }),
    }),
  }),
})
