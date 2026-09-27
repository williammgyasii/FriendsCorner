import assert from 'node:assert/strict'
import { test } from 'node:test'
import { pickLayout } from '../src/layout.ts'

test('a phone held upright is portrait', () => {
  assert.equal(pickLayout(390, 844), 'portrait')
  assert.equal(pickLayout(360, 640), 'portrait')
})

test('a phone turned on its side is sideways', () => {
  assert.equal(pickLayout(844, 390), 'sideways')
  assert.equal(pickLayout(640, 360), 'sideways')
})

test('a laptop or monitor is desktop', () => {
  assert.equal(pickLayout(1440, 900), 'desktop')
  assert.equal(pickLayout(1024, 768), 'desktop')
})

test('a tablet held upright keeps the desktop layout', () => {
  assert.equal(pickLayout(820, 1180), 'desktop')
})

test('a wide window that is very short goes sideways', () => {
  assert.equal(pickLayout(1280, 480), 'sideways')
})

test('the edges: 720 wide is desktop, 500 tall is not sideways', () => {
  assert.equal(pickLayout(719, 900), 'portrait')
  assert.equal(pickLayout(720, 900), 'desktop')
  assert.equal(pickLayout(900, 500), 'desktop')
  assert.equal(pickLayout(900, 499), 'sideways')
})
