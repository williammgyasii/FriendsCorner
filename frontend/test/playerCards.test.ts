import assert from 'node:assert/strict'
import { test } from 'vitest'
import { describePlayers } from '../src/marksLook.ts'

const empty = {
  squares: Array(9).fill(null),
  next: 'A' as const,
  winner: null,
  draw: false,
}

const bothHere = { A: true, B: true }

test('your card is on the left and glows on your turn', () => {
  const { left, right } = describePlayers(empty, 'A', bothHere)

  assert.deepEqual(left, { seat: 'A', mark: 'X', color: '#2f6bff', label: 'You', active: true, note: 'Your move' })
  assert.deepEqual(right, { seat: 'B', mark: 'O', color: '#e23d6b', label: 'Them', active: false, note: 'Waiting' })
})

test('seat B also sees themselves on the left', () => {
  const { left, right } = describePlayers(empty, 'B', bothHere)

  assert.equal(left.seat, 'B')
  assert.equal(left.mark, 'O')
  assert.equal(left.active, false)
  assert.equal(right.seat, 'A')
  assert.equal(right.active, true)
  assert.equal(right.note, 'Their move')
})

test('the winner card glows and the other goes quiet', () => {
  const { left, right } = describePlayers({ ...empty, next: 'B', winner: 'A' }, 'B', bothHere)

  assert.equal(left.active, false)
  assert.equal(left.note, '')
  assert.equal(right.active, true)
  assert.equal(right.note, 'Winner')
})

test('a draw lights neither card', () => {
  const { left, right } = describePlayers({ ...empty, draw: true }, 'A', bothHere)

  assert.equal(left.active, false)
  assert.equal(right.active, false)
  assert.equal(left.note, 'Draw')
  assert.equal(right.note, 'Draw')
})

test('an empty seat says so instead of glowing', () => {
  const { right } = describePlayers({ ...empty, next: 'B' }, 'A', { A: true, B: false })

  assert.equal(right.active, false)
  assert.equal(right.note, 'Not here')
})
