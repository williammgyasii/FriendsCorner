import assert from 'node:assert/strict'
import { test } from 'node:test'
import { describeMarks } from '../src/marksLook.ts'

const empty = {
  squares: Array(9).fill(null),
  next: 'A' as const,
  winner: null,
  draw: false,
}

test('the board takes the color of the seat whose turn it is', () => {
  const yours = describeMarks(empty, 'A')
  const theirs = describeMarks({ ...empty, next: 'B' }, 'A')

  assert.equal(yours.turn, 'A')
  assert.equal(yours.color, '#2f6bff')
  assert.equal(yours.status, 'Your turn. You are X.')
  assert.equal(yours.celebrate, null)
  assert.equal(theirs.turn, 'B')
  assert.equal(theirs.color, '#e23d6b')
  assert.equal(theirs.status, 'Their turn. You are X.')
})

test('a win celebrates that line and offers another round', () => {
  const board = {
    squares: ['X', 'X', 'X', 'O', 'O', null, null, null, null],
    next: 'B' as const,
    winner: 'A' as const,
    draw: false,
  }

  const look = describeMarks(board, 'B')

  assert.deepEqual(look.winning, [0, 1, 2])
  assert.equal(look.turn, 'A')
  assert.equal(look.color, '#2f6bff')
  assert.equal(look.celebrate, 'They won')
  assert.equal(look.canRematch, true)
})

test('a draw offers another round without a winning line', () => {
  const look = describeMarks(
    {
      squares: ['X', 'O', 'X', 'X', 'O', 'O', 'O', 'X', 'X'],
      next: 'A',
      winner: null,
      draw: true,
    },
    'A',
  )

  assert.deepEqual(look.winning, [])
  assert.equal(look.celebrate, 'Draw')
  assert.equal(look.canRematch, true)
  assert.equal(look.color, '#1c1915')
})
