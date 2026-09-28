import assert from 'node:assert/strict'
import { test } from 'vitest'
import type { ChessState } from '../src/chessLook.ts'
import { tapMeaning } from '../src/chessTap.ts'

const start = 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1'

const chess = (overrides: Partial<ChessState> = {}): ChessState => ({
  fen: start,
  white: 'A',
  toMove: 'A',
  inCheck: false,
  lastMove: null,
  outcome: null,
  legalMoves: [
    { from: 'e2', to: 'e3', promotion: null },
    { from: 'e2', to: 'e4', promotion: null },
    { from: 'g1', to: 'f3', promotion: null },
  ],
  ...overrides,
})

const bothHere = { A: true, B: true }

test('your own piece on your turn is picked up', () => {
  assert.deepEqual(tapMeaning(chess(), 'A', bothHere, null, 'e2'), { kind: 'pick', square: 'e2', piece: 'P' })
})

test('with a piece picked up, another of yours is picked up instead', () => {
  assert.deepEqual(tapMeaning(chess(), 'A', bothHere, 'e2', 'g1'), { kind: 'pick', square: 'g1', piece: 'N' })
})

test('a square the picked-up piece can reach is a move', () => {
  assert.deepEqual(tapMeaning(chess(), 'A', bothHere, 'e2', 'e4'), { kind: 'move', from: 'e2', to: 'e4' })
})

test('tapping the picked-up piece again puts it down and explains it', () => {
  assert.deepEqual(tapMeaning(chess(), 'A', bothHere, 'e2', 'e2'), { kind: 'look', piece: 'P' })
})

test('their piece is explained, never picked up', () => {
  assert.deepEqual(tapMeaning(chess(), 'A', bothHere, null, 'e7'), { kind: 'look', piece: 'p' })
})

test('an empty square that is not a move clears everything', () => {
  assert.deepEqual(tapMeaning(chess(), 'A', bothHere, 'e2', 'a5'), { kind: 'look', piece: null })
})

test('on their turn your pieces are explained but stay put', () => {
  assert.deepEqual(tapMeaning(chess({ toMove: 'B' }), 'A', bothHere, null, 'e2'), { kind: 'look', piece: 'P' })
})

test('while the other player is away nothing can be picked up', () => {
  assert.deepEqual(tapMeaning(chess(), 'A', { A: true, B: false }, null, 'e2'), { kind: 'look', piece: 'P' })
})

test('after the game ends nothing can be picked up', () => {
  const over = chess({ outcome: { ending: 'checkmate', winner: 'B' } })
  assert.deepEqual(tapMeaning(over, 'A', bothHere, null, 'e2'), { kind: 'look', piece: 'P' })
})

test('black picks up black pieces', () => {
  const blacksTurn = chess({
    white: 'B',
    toMove: 'A',
    legalMoves: [{ from: 'e7', to: 'e5', promotion: null }],
  })
  assert.deepEqual(tapMeaning(blacksTurn, 'A', bothHere, null, 'e7'), { kind: 'pick', square: 'e7', piece: 'p' })
})

test('reaching the last rank asks which piece to become', () => {
  const promoting = chess({
    fen: '4k3/P7/8/8/8/8/8/4K3 w - - 0 1',
    legalMoves: ['q', 'r', 'b', 'n'].map((piece) => ({ from: 'a7', to: 'a8', promotion: piece })),
  })
  assert.deepEqual(tapMeaning(promoting, 'A', bothHere, 'a7', 'a8'), { kind: 'promote', from: 'a7', to: 'a8' })
})
