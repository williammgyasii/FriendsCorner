import assert from 'node:assert/strict'
import { afterEach, test, vi } from 'vitest'
import { fallbackIceServers } from '../src/ice.ts'
import { makeStore } from '../src/store/index.ts'
import { iceServersFor, roomApi } from '../src/store/roomApi.ts'

const turnReply = { iceServers: [{ urls: ['turn:turn.example:3478'], username: 'u', credential: 'c' }] }

const answer = (body: unknown, status = 200) =>
  vi.fn(async () => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }))

afterEach(() => {
  vi.unstubAllGlobals()
})

test('TURN servers for a room are fetched once and reused', async () => {
  const fetch = answer(turnReply)
  vi.stubGlobal('fetch', fetch)
  const store = makeStore({ send: () => undefined })

  const first = await store.dispatch(iceServersFor('room-1'))
  const second = await store.dispatch(iceServersFor('room-1'))

  assert.equal(fetch.mock.calls.length, 1)
  assert.deepEqual(first, turnReply.iceServers)
  assert.deepEqual(second, first)
})

test('another room asks for its own TURN servers', async () => {
  const fetch = answer(turnReply)
  vi.stubGlobal('fetch', fetch)
  const store = makeStore({ send: () => undefined })

  await store.dispatch(iceServersFor('room-1'))
  await store.dispatch(iceServersFor('room-2'))

  assert.equal(fetch.mock.calls.length, 2)
})

test('when TURN is unreachable the call still gets public STUN', async () => {
  vi.stubGlobal('fetch', vi.fn(async () => Promise.reject(new Error('offline'))))
  const store = makeStore({ send: () => undefined })

  assert.deepEqual(await store.dispatch(iceServersFor('room-1')), fallbackIceServers)
})

test('opening a lobby returns the new room id', async () => {
  const fetch = answer({ id: 'abc123' })
  vi.stubGlobal('fetch', fetch)
  const store = makeStore({ send: () => undefined })

  const result = await store.dispatch(roomApi.endpoints.createRoom.initiate())

  assert.equal(result.data, 'abc123')
  assert.deepEqual(fetch.mock.calls[0], ['/rooms', { method: 'POST' }])
})

test('a lobby that fails to open is reported, not cached', async () => {
  vi.stubGlobal('fetch', answer({}, 503))
  const store = makeStore({ send: () => undefined })

  const result = await store.dispatch(roomApi.endpoints.createRoom.initiate())

  assert.equal(result.data, undefined)
  assert.deepEqual(result.error, { status: 503 })
})
