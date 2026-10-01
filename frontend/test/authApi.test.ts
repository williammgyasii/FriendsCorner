// @vitest-environment jsdom

import assert from 'node:assert/strict'
import { afterEach, test, vi } from 'vitest'
import { authApi, type Account } from '../src/store/authApi.ts'
import { makeStore } from '../src/store/index.ts'

const ada: Account = { id: '1', email: 'ada@example.com', name: 'Ada', gameName: 'Countess', plan: null }

const requestOf = (fetch: ReturnType<typeof vi.fn>) => fetch.mock.calls[0][0] as Request

const answer = (body: unknown, status = 200) =>
  vi.fn(async () => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }))

afterEach(() => {
  vi.unstubAllGlobals()
})

test('the session is read from GET /account and kept on the auth slice', async () => {
  const fetch = answer(ada)
  vi.stubGlobal('fetch', fetch)
  const store = makeStore({ send: () => undefined })

  const result = await store.dispatch(authApi.endpoints.account.initiate(undefined, { subscribe: false }))

  assert.deepEqual(result.data, ada)
  assert.deepEqual(store.getState().auth.account, ada)
  const request = requestOf(fetch)
  assert.equal(request.method, 'GET')
  assert.equal(new URL(request.url).pathname, '/account')
  assert.equal(request.credentials, 'include')
})

test('signing in posts the account and keeps the cookie', async () => {
  const fetch = answer(ada)
  vi.stubGlobal('fetch', fetch)
  const store = makeStore({ send: () => undefined })
  const body = { action: 'login' as const, name: '', gameName: '', email: ada.email, password: 'correct-horse' }

  const result = await store.dispatch(authApi.endpoints.signIn.initiate(body))

  assert.deepEqual(result.data, ada)
  assert.deepEqual(store.getState().auth.account, ada)
  const request = requestOf(fetch)
  assert.equal(request.method, 'POST')
  assert.equal(new URL(request.url).pathname, '/account')
  assert.equal(request.credentials, 'include')
  assert.deepEqual(await request.json(), body)
})

test('a wrong password comes back as 401 and leaves the slice empty', async () => {
  vi.stubGlobal('fetch', vi.fn(async () => new Response(null, { status: 401 })))
  const store = makeStore({ send: () => undefined })

  const result = await store.dispatch(
    authApi.endpoints.signIn.initiate({ action: 'login', name: '', gameName: '', email: ada.email, password: 'nope' }),
  )

  assert.equal(result.data, undefined)
  assert.equal(result.error && 'status' in result.error ? result.error.status : 0, 401)
  assert.equal(store.getState().auth.account, null)
})

test('logging out posts the account and clears the slice', async () => {
  const fetch = vi.fn(async (input: Request) => {
    if (input.method === 'POST') return new Response(null, { status: 204 })
    return new Response(JSON.stringify(ada), { status: 200, headers: { 'Content-Type': 'application/json' } })
  })
  vi.stubGlobal('fetch', fetch)
  const store = makeStore({ send: () => undefined })

  await store.dispatch(authApi.endpoints.account.initiate(undefined, { subscribe: false }))
  const result = await store.dispatch(authApi.endpoints.signOut.initiate())

  assert.equal(result.error, undefined)
  assert.equal(store.getState().auth.account, null)
  const request = fetch.mock.calls[1][0] as Request
  assert.equal(request.method, 'POST')
  assert.equal(new URL(request.url).pathname, '/account')
  assert.deepEqual(await request.json(), { action: 'logout' })
})
