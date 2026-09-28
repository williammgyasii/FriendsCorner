import assert from 'node:assert/strict'
import { test } from 'vitest'
import type { ChessState } from '../src/chessLook.ts'
import { makeStore } from '../src/store/index.ts'
import { closed, joined, stateReceived, type RoomSnapshot } from '../src/store/roomSlice.ts'
import { chooseChessSquare, choosePromotion } from '../src/store/chessUiSlice.ts'
import { selectChessPicture } from '../src/store/selectors.ts'

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
  ],
  ...overrides,
})

const snapshot = (overrides: Partial<RoomSnapshot> = {}): RoomSnapshot => ({
  you: 'A',
  world: 'chess',
  board: null,
  chess: chess(),
  players: { A: { x: 1, y: 1 }, B: { x: 2, y: 2 } },
  lobby: null,
  ...overrides,
})

const setup = () => {
  const sent: unknown[] = []
  const store = makeStore({ send: (message) => sent.push(message) })
  return { store, sent }
}

test('joining a seat records who you are', () => {
  const { store } = setup()

  store.dispatch(joined('B'))

  assert.equal(store.getState().room.seat, 'B')
  assert.equal(store.getState().room.connection, 'open')
})

test('a state message is kept as the latest copy from the server', () => {
  const { store } = setup()

  store.dispatch(stateReceived(snapshot()))

  assert.equal(store.getState().room.snapshot?.chess?.fen, start)
})

test('a floor tick that leaves the chess game alone keeps the same chess object', () => {
  const { store } = setup()
  store.dispatch(stateReceived(snapshot()))
  const before = store.getState().room.snapshot?.chess

  store.dispatch(stateReceived(snapshot({ players: { A: { x: 5, y: 5 }, B: { x: 2, y: 2 } } })))

  assert.equal(store.getState().room.snapshot?.chess, before)
})

test('a full room and a vanished room close differently', () => {
  const full = setup().store
  full.dispatch(closed('room is full'))
  assert.equal(full.getState().room.connection, 'full')

  const gone = setup().store
  gone.dispatch(closed(''))
  assert.equal(gone.getState().room.connection, 'gone')
})

test('a move is sent to the server, and the board waits for its answer', () => {
  const { store, sent } = setup()
  store.dispatch(stateReceived(snapshot()))
  store.dispatch(chooseChessSquare('e2'))

  store.dispatch(chooseChessSquare('e4'))

  assert.deepEqual(sent, [{ type: 'chess-move', from: 'e2', to: 'e4' }])
  assert.equal(store.getState().chessUi.selected, null)
  assert.equal(store.getState().room.snapshot?.chess?.fen, start)
})

test('a promotion waits for the choice, then sends it', () => {
  const { store, sent } = setup()
  const promoting = chess({
    fen: '4k3/P7/8/8/8/8/8/4K3 w - - 0 1',
    legalMoves: ['q', 'r', 'b', 'n'].map((piece) => ({ from: 'a7', to: 'a8', promotion: piece })),
  })
  store.dispatch(stateReceived(snapshot({ chess: promoting })))
  store.dispatch(chooseChessSquare('a7'))

  store.dispatch(chooseChessSquare('a8'))
  assert.deepEqual(sent, [])
  assert.deepEqual(store.getState().chessUi.pendingPromotion, { from: 'a7', to: 'a8' })

  store.dispatch(choosePromotion('n'))
  assert.deepEqual(sent, [{ type: 'chess-move', from: 'a7', to: 'a8', promotion: 'n' }])
  assert.equal(store.getState().chessUi.pendingPromotion, null)
})

test('a new position from the server drops a stale selection', () => {
  const { store } = setup()
  store.dispatch(stateReceived(snapshot()))
  store.dispatch(chooseChessSquare('e2'))

  const moved = chess({ fen: 'rnbqkbnr/pppppppp/8/8/8/5N2/PPPPPPPP/RNBQKB1R b KQkq - 1 1', toMove: 'B' })
  store.dispatch(stateReceived(snapshot({ chess: moved })))

  assert.equal(store.getState().chessUi.selected, null)
})

test('the chess picture shows where the selected piece can go', () => {
  const { store } = setup()
  store.dispatch(stateReceived(snapshot()))
  store.dispatch(chooseChessSquare('e2'))

  const picture = selectChessPicture(store.getState())

  assert.deepEqual(picture?.targets, ['e3', 'e4'])
  assert.equal(picture?.selected, 'e2')
})

test('a floor tick does not rebuild the chess picture', () => {
  const { store } = setup()
  store.dispatch(stateReceived(snapshot()))
  const before = selectChessPicture(store.getState())

  store.dispatch(stateReceived(snapshot({ players: { A: { x: 9, y: 9 }, B: { x: 2, y: 2 } } })))

  assert.equal(selectChessPicture(store.getState()), before)
})

test('someone leaving does rebuild the chess picture', () => {
  const { store } = setup()
  store.dispatch(stateReceived(snapshot()))
  const before = selectChessPicture(store.getState())

  store.dispatch(stateReceived(snapshot({ players: { A: { x: 1, y: 1 }, B: null } })))

  assert.notEqual(selectChessPicture(store.getState()), before)
})
