import assert from 'node:assert/strict'
import { test } from 'node:test'
import {
  checkedKing,
  describeChess,
  needsPromotion,
  pieceHint,
  piecesFrom,
  squaresInView,
  targetsFrom,
  type ChessState,
} from '../src/chessLook.ts'

const start = 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1'

const game = (overrides: Partial<ChessState> = {}): ChessState => ({
  fen: start,
  white: 'A',
  toMove: 'A',
  inCheck: false,
  lastMove: null,
  outcome: null,
  legalMoves: [],
  ...overrides,
})

const here = { A: true, B: true }

test('pieces are read from the FEN by square name', () => {
  const pieces = piecesFrom(start)

  assert.equal(pieces.get('e1'), 'K')
  assert.equal(pieces.get('d8'), 'q')
  assert.equal(pieces.get('a2'), 'P')
  assert.equal(pieces.get('e4'), undefined)
  assert.equal(pieces.size, 32)
})

test('white sees rank 8 at the top with a on the left', () => {
  const squares = squaresInView(true)

  assert.equal(squares.length, 64)
  assert.equal(squares[0], 'a8')
  assert.equal(squares[7], 'h8')
  assert.equal(squares[56], 'a1')
  assert.equal(squares[63], 'h1')
})

test('black sees the board turned around so their pieces are at the bottom', () => {
  const squares = squaresInView(false)

  assert.equal(squares[0], 'h1')
  assert.equal(squares[63], 'a8')
})

test('a selected piece lights up each square it can reach once', () => {
  const legalMoves = [
    { from: 'a7', to: 'a8', promotion: 'q' },
    { from: 'a7', to: 'a8', promotion: 'n' },
    { from: 'g1', to: 'f3', promotion: null },
  ]

  assert.deepEqual(targetsFrom(legalMoves, 'a7'), ['a8'])
  assert.deepEqual(targetsFrom(legalMoves, 'g1'), ['f3'])
  assert.deepEqual(targetsFrom(legalMoves, 'e2'), [])
})

test('reaching the last rank with a pawn asks which piece to become', () => {
  const legalMoves = [
    { from: 'a7', to: 'a8', promotion: 'q' },
    { from: 'g1', to: 'f3', promotion: null },
  ]

  assert.equal(needsPromotion(legalMoves, 'a7', 'a8'), true)
  assert.equal(needsPromotion(legalMoves, 'g1', 'f3'), false)
})

test('the king in check is found for the side to move', () => {
  const foolsMate = 'rnb1kbnr/pppp1ppp/8/4p3/6Pq/5P2/PPPPP2P/RNBQKBNR w KQkq - 1 3'

  assert.equal(checkedKing(game({ fen: foolsMate, inCheck: true })), 'e1')
  assert.equal(checkedKing(game({ fen: foolsMate, inCheck: false })), null)
})

test('on your move you are told to pick a piece, in your seat color', () => {
  const look = describeChess(game(), 'A', here)

  assert.equal(look.youAreWhite, true)
  assert.equal(look.canMove, true)
  assert.equal(look.color, '#2f6bff')
  assert.match(look.status, /your move/i)
  assert.equal(look.celebrate, null)
  assert.equal(look.canRematch, false)
})

test('check is called out so a beginner knows to protect the king', () => {
  const look = describeChess(game({ inCheck: true }), 'A', here)

  assert.match(look.status, /check/i)
})

test('while they think, you cannot move', () => {
  const look = describeChess(game({ toMove: 'B' }), 'A', here)

  assert.equal(look.canMove, false)
  assert.equal(look.color, '#e23d6b')
  assert.match(look.status, /their move/i)
})

test('checkmate celebrates the winner and offers a rematch', () => {
  const look = describeChess(game({ toMove: 'A', inCheck: true, outcome: { ending: 'checkmate', winner: 'B' } }), 'B', here)

  assert.equal(look.celebrate, 'Checkmate! You won')
  assert.equal(look.canMove, false)
  assert.equal(look.canRematch, true)
  assert.equal(look.color, '#e23d6b')
})

test('stalemate is a draw and still offers a rematch', () => {
  const look = describeChess(game({ outcome: { ending: 'stalemate', winner: null } }), 'A', here)

  assert.match(look.celebrate ?? '', /stalemate/i)
  assert.equal(look.canRematch, true)
})

test('player cards say who plays white and whose move it is', () => {
  const look = describeChess(game({ white: 'B', toMove: 'B' }), 'A', here)

  assert.equal(look.youAreWhite, false)
  assert.equal(look.left.label, 'You')
  assert.equal(look.left.side, 'Black')
  assert.equal(look.left.active, false)
  assert.equal(look.right.label, 'Them')
  assert.equal(look.right.side, 'White')
  assert.equal(look.right.active, true)
  assert.equal(look.right.note, 'Their move')
})

test('an absent player is shown as not here', () => {
  const look = describeChess(game(), 'A', { A: true, B: false })

  assert.equal(look.right.note, 'Not here')
  assert.equal(look.canMove, false)
})

test('every piece has a plain-English hint for how it moves', () => {
  for (const piece of ['K', 'Q', 'R', 'B', 'N', 'P', 'k', 'q', 'r', 'b', 'n', 'p']) {
    assert.ok(pieceHint(piece).length > 10, piece)
  }
  assert.match(pieceHint('N'), /L/)
})
