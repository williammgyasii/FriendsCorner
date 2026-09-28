import assert from 'node:assert/strict'
import { test } from 'vitest'
import { fallbackIceServers, iceServersFrom } from '../src/ice.ts'

test('a TURN response becomes the ICE servers, without the port browsers block', () => {
  const servers = iceServersFrom({
    iceServers: [
      { urls: ['stun:stun.cloudflare.com:3478', 'stun:stun.cloudflare.com:53'] },
      {
        urls: ['turn:turn.cloudflare.com:3478?transport=udp', 'turn:turn.cloudflare.com:53?transport=udp'],
        username: 'user',
        credential: 'secret',
      },
    ],
  })

  assert.deepEqual(servers, [
    { urls: ['stun:stun.cloudflare.com:3478'] },
    { urls: ['turn:turn.cloudflare.com:3478?transport=udp'], username: 'user', credential: 'secret' },
  ])
})

test('no usable answer falls back to public STUN', () => {
  assert.deepEqual(iceServersFrom(null), fallbackIceServers)
  assert.deepEqual(iceServersFrom({ error: 'nope' }), fallbackIceServers)
  assert.deepEqual(iceServersFrom({ iceServers: [] }), fallbackIceServers)
  assert.deepEqual(fallbackIceServers, [{ urls: ['stun:stun.cloudflare.com:3478'] }])
})
