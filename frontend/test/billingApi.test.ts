// @vitest-environment jsdom

import assert from 'node:assert/strict'
import { afterEach, test, vi } from 'vitest'
import { billingApi } from '../src/store/billingApi.ts'
import { makeStore } from '../src/store/index.ts'

const answer = (body: unknown, status = 200) =>
  vi.fn(async () => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }))

afterEach(() => {
  vi.unstubAllGlobals()
})

test('checkout posts the plan and returns the Stripe url', async () => {
  const fetch = answer({ url: 'https://checkout.stripe.test/cs' })
  vi.stubGlobal('fetch', fetch)
  const store = makeStore({ send: () => undefined })

  const result = await store.dispatch(billingApi.endpoints.startBilling.initiate({ action: 'checkout', plan: 'house' }))

  assert.deepEqual(result.data, { url: 'https://checkout.stripe.test/cs' })
  const request = fetch.mock.calls[0][0] as Request
  assert.equal(request.method, 'POST')
  assert.equal(new URL(request.url).pathname, '/billing')
  assert.deepEqual(await request.json(), { action: 'checkout', plan: 'house' })
})

test('confirm posts the checkout session id after Stripe returns', async () => {
  const fetch = vi.fn(async () => new Response(null, { status: 204 }))
  vi.stubGlobal('fetch', fetch)
  const store = makeStore({ send: () => undefined })

  const result = await store.dispatch(billingApi.endpoints.confirmCheckout.initiate({ sessionId: 'cs_test_123' }))

  assert.equal(result.error, undefined)
  const request = fetch.mock.calls[0][0] as Request
  assert.equal(request.method, 'POST')
  assert.equal(new URL(request.url).pathname, '/billing')
  assert.deepEqual(await request.json(), { action: 'confirm', sessionId: 'cs_test_123' })
})
