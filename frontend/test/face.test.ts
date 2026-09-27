import assert from 'node:assert/strict'
import { test } from 'node:test'
import { describeFace } from '../src/face.ts'

test('a denied camera shows unavailable and still allows movement', () => {
  const face = describeFace('unavailable')

  assert.equal(face.tile, 'unavailable')
  assert.equal(face.movementAllowed, true)
})
