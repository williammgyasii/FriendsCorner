import assert from 'node:assert/strict'
import { test } from 'node:test'
import { webRouteFor } from '../web/src/route.ts'

test('lobby, room sockets, and ICE credentials are handed to the API Worker', () => {
  assert.equal(webRouteFor('/rooms'), 'api')
  assert.equal(webRouteFor('/account'), 'api')
  assert.equal(webRouteFor('/billing'), 'api')
  assert.equal(webRouteFor('/billing/webhook'), 'api')
  assert.equal(webRouteFor('/ws/4d6e61013c5946969d9b359440c7f5ee'), 'api')
  assert.equal(webRouteFor('/turn'), 'api')
})

test('everything else is the page', () => {
  assert.equal(webRouteFor('/'), 'assets')
  assert.equal(webRouteFor('/login'), 'assets')
  assert.equal(webRouteFor('/register'), 'assets')
  assert.equal(webRouteFor('/assets/index-abc.js'), 'assets')
  assert.equal(webRouteFor('/roomsomething'), 'assets')
  assert.equal(webRouteFor('/ws'), 'assets')
  assert.equal(webRouteFor('/turnstile'), 'assets')
})
