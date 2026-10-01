import assert from 'node:assert/strict'
import { test } from 'node:test'
import { apiRouteFor } from '../api/src/route.ts'

test('opening a lobby and joining a room go to the room container', () => {
  assert.equal(apiRouteFor('POST', '/rooms'), 'room')
  assert.equal(apiRouteFor('GET', '/ws/4d6e61013c5946969d9b359440c7f5ee'), 'room')
})

test('ICE credentials are minted by the API Worker itself', () => {
  assert.equal(apiRouteFor('GET', '/turn'), 'turn')
})

test('billing is forwarded to the room container', () => {
  assert.equal(apiRouteFor('POST', '/billing'), 'billing')
  assert.equal(apiRouteFor('POST', '/billing/webhook'), 'billing')
  assert.equal(apiRouteFor('GET', '/billing'), 'refuse')
  assert.equal(apiRouteFor('DELETE', '/billing/webhook'), 'refuse')
})

test('signing in is forwarded to the room container', () => {
  assert.equal(apiRouteFor('GET', '/account'), 'account')
  assert.equal(apiRouteFor('POST', '/account'), 'account')
  assert.equal(apiRouteFor('DELETE', '/account'), 'refuse')
})

test('only the methods each route uses are allowed through', () => {
  assert.equal(apiRouteFor('GET', '/rooms'), 'refuse')
  assert.equal(apiRouteFor('POST', '/ws/4d6e61013c5946969d9b359440c7f5ee'), 'refuse')
  assert.equal(apiRouteFor('POST', '/turn'), 'refuse')
})

test('the API has no page, so anything else is missing', () => {
  assert.equal(apiRouteFor('GET', '/'), 'missing')
  assert.equal(apiRouteFor('GET', '/rooms/4d6e61013c5946969d9b359440c7f5ee'), 'missing')
})
