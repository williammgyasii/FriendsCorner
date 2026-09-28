import assert from 'node:assert/strict'
import { test } from 'vitest'
import { makeStore } from '../src/store/index.ts'
import { stateReceived, type RoomSnapshot } from '../src/store/roomSlice.ts'
import {
  askPreview,
  cancelExchange,
  chooseBlankLetter,
  dropRackTile,
  moveUnsent,
  passTiles,
  previewReceived,
  returnToRack,
  shuffleRack,
  recallTiles,
  rematchTiles,
  sendExchange,
  startExchange,
  submitTiles,
  tapRackTile,
  tapSquare,
} from '../src/store/tilesUiSlice.ts'
import type { TilesState } from '../src/tilesLook.ts'
import { tiles } from './tilesFixture.ts'

const snapshot = (extra: Partial<TilesState> = {}): RoomSnapshot => ({
  you: 'A',
  world: 'tiles',
  board: null,
  chess: null,
  tiles: tiles(extra),
  players: { A: { x: 1, y: 1 }, B: { x: 2, y: 2 } },
  lobby: null,
})

const setup = (extra: Partial<TilesState> = {}) => {
  const sent: unknown[] = []
  const store = makeStore({ send: (message) => sent.push(message) }, { storage: null })
  store.dispatch(stateReceived(snapshot(extra)))
  return { store, sent, ui: () => store.getState().tilesUi }
}

test('a rack tile then a square places it on the page only', () => {
  const { store, sent, ui } = setup()

  store.dispatch(tapRackTile(0))
  store.dispatch(tapSquare(111))

  assert.deepEqual(ui().unsent, [{ square: 111, rackIndex: 0, letter: 'C', blank: false }])
  assert.equal(ui().selected, null)
  assert.deepEqual(sent, [])
})

test('tapping a placed tile takes it back to the rack', () => {
  const { store, sent, ui } = setup()
  store.dispatch(tapRackTile(0))
  store.dispatch(tapSquare(111))

  store.dispatch(tapSquare(111))

  assert.deepEqual(ui().unsent, [])
  assert.deepEqual(sent, [])
})

test('a square that already has a tile takes nothing', () => {
  const board = tiles().board.slice()
  board[111] = 'C'
  const { store, ui } = setup({ board })

  store.dispatch(tapRackTile(1))
  store.dispatch(tapSquare(111))

  assert.deepEqual(ui().unsent, [])
})

test('a rack tile already on the board cannot be picked again', () => {
  const { store, ui } = setup()
  store.dispatch(tapRackTile(0))
  store.dispatch(tapSquare(111))

  store.dispatch(tapRackTile(0))

  assert.equal(ui().selected, null)
})

test('Recall brings every unsent tile back', () => {
  const { store, ui } = setup()
  store.dispatch(tapRackTile(0))
  store.dispatch(tapSquare(111))
  store.dispatch(tapRackTile(1))
  store.dispatch(tapSquare(112))

  store.dispatch(recallTiles())

  assert.deepEqual(ui().unsent, [])
})

test('a blank asks which letter it stands for', () => {
  const { store, ui } = setup()
  store.dispatch(tapRackTile(6))
  store.dispatch(tapSquare(112))

  assert.deepEqual(ui().askingBlank, { square: 112, rackIndex: 6 })
  assert.deepEqual(ui().unsent, [])

  store.dispatch(chooseBlankLetter('a'))

  assert.equal(ui().askingBlank, null)
  assert.deepEqual(ui().unsent, [{ square: 112, rackIndex: 6, letter: 'A', blank: true }])
})

test('Submit sends the unsent tiles as one play, with blank marked', () => {
  const { store, sent } = setup()
  store.dispatch(tapRackTile(0))
  store.dispatch(tapSquare(111))
  store.dispatch(tapRackTile(6))
  store.dispatch(tapSquare(112))
  store.dispatch(chooseBlankLetter('A'))
  store.dispatch(tapRackTile(2))
  store.dispatch(tapSquare(113))

  store.dispatch(submitTiles())

  assert.deepEqual(sent, [
    {
      type: 'tiles-play',
      tiles: [
        { square: 111, letter: 'C' },
        { square: 112, letter: 'A', blank: true },
        { square: 113, letter: 'T' },
      ],
    },
  ])
})

test('after a refusal the unsent tiles stay where they were', () => {
  const { store, ui } = setup()
  store.dispatch(tapRackTile(3))
  store.dispatch(tapSquare(111))
  store.dispatch(submitTiles())

  store.dispatch(stateReceived(snapshot({ refusal: { reason: 'not-a-word', words: ['Q'] } })))

  assert.equal(ui().unsent.length, 1)
})

test('when the play is taken the new rack clears the unsent tiles', () => {
  const { store, ui } = setup()
  store.dispatch(tapRackTile(0))
  store.dispatch(tapSquare(111))
  store.dispatch(submitTiles())

  store.dispatch(stateReceived(snapshot({ rack: ['E', 'A', 'T', 'Q', 'X', 'Z', '?'] })))

  assert.deepEqual(ui().unsent, [])
})

test('Exchange picks rack tiles and sends their letters', () => {
  const { store, sent, ui } = setup()

  store.dispatch(startExchange())
  store.dispatch(tapRackTile(3))
  store.dispatch(tapRackTile(6))
  store.dispatch(tapRackTile(4))
  store.dispatch(tapRackTile(4))
  store.dispatch(sendExchange())

  assert.deepEqual(sent, [{ type: 'tiles-exchange', letters: 'Q?' }])
  assert.equal(ui().exchanging, null)
})

test('cancelling an exchange sends nothing', () => {
  const { store, sent, ui } = setup()
  store.dispatch(startExchange())
  store.dispatch(tapRackTile(3))

  store.dispatch(cancelExchange())

  assert.equal(ui().exchanging, null)
  assert.deepEqual(sent, [])
})

test('Pass and Rematch are sent as they are', () => {
  const { store, sent } = setup()

  store.dispatch(passTiles())
  store.dispatch(rematchTiles())

  assert.deepEqual(sent, [{ type: 'tiles-pass' }, { type: 'tiles-rematch' }])
})

const place = (store: ReturnType<typeof setup>['store'], rackIndex: number, square: number) => {
  store.dispatch(tapRackTile(rackIndex))
  store.dispatch(tapSquare(square))
}

test('asking for a preview sends the unsent tiles', () => {
  const { store, sent } = setup()
  place(store, 0, 111)
  place(store, 1, 112)

  store.dispatch(askPreview())

  assert.deepEqual(sent, [
    { type: 'tiles-preview', tiles: [{ square: 111, letter: 'C' }, { square: 112, letter: 'A' }] },
  ])
})

test('with no unsent tiles a preview is not asked for and the old one is gone', () => {
  const { store, sent, ui } = setup()
  place(store, 0, 111)
  store.dispatch(previewReceived({ tiles: [{ square: 111, letter: 'C', blank: false }], words: ['C'], score: 3 }))

  store.dispatch(recallTiles())
  store.dispatch(askPreview())

  assert.deepEqual(sent, [])
  assert.equal(ui().preview, null)
})

test('an answer for the tiles on the page is kept', () => {
  const { store, ui } = setup()
  place(store, 0, 111)
  place(store, 1, 112)

  const answer = {
    tiles: [
      { square: 111, letter: 'C', blank: false },
      { square: 112, letter: 'A', blank: false },
    ],
    words: ['CA'],
    score: 4,
  }
  store.dispatch(previewReceived(answer))

  assert.deepEqual(ui().preview, answer)
})

test('an answer for tiles the page has moved on from is ignored', () => {
  const { store, ui } = setup()
  place(store, 0, 111)
  place(store, 1, 112)

  store.dispatch(previewReceived({ tiles: [{ square: 111, letter: 'C', blank: false }], words: ['C'], score: 3 }))

  assert.equal(ui().preview, null)
})

test('changing the unsent tiles drops the preview for the old ones', () => {
  const { store, ui } = setup()
  place(store, 0, 111)
  store.dispatch(previewReceived({ tiles: [{ square: 111, letter: 'C', blank: false }], refusal: { reason: 'first-needs-two-tiles', words: [] } }))

  place(store, 1, 112)

  assert.equal(ui().preview, null)
})

test('an unsent tile can be moved to another empty square', () => {
  const { store, sent, ui } = setup()
  place(store, 0, 111)

  store.dispatch(moveUnsent(111, 126))

  assert.deepEqual(ui().unsent, [{ square: 126, rackIndex: 0, letter: 'C', blank: false }])
  assert.deepEqual(sent, [])
})

test('an unsent tile cannot be moved onto a taken square', () => {
  const board = tiles().board.slice()
  board[126] = 'E'
  const { store, ui } = setup({ board })
  place(store, 0, 111)

  store.dispatch(moveUnsent(111, 126))

  assert.equal(ui().unsent[0].square, 111)
})

test('a rack tile dropped on a square is placed there', () => {
  const { store, ui } = setup()

  store.dispatch(dropRackTile(2, 113))

  assert.deepEqual(ui().unsent, [{ square: 113, rackIndex: 2, letter: 'T', blank: false }])
})

test('a blank dropped on a square asks for its letter', () => {
  const { store, ui } = setup()

  store.dispatch(dropRackTile(6, 112))

  assert.deepEqual(ui().askingBlank, { square: 112, rackIndex: 6 })
})

test('an unsent tile dragged to the rack goes back', () => {
  const { store, ui } = setup()
  place(store, 0, 111)

  store.dispatch(returnToRack(111))

  assert.deepEqual(ui().unsent, [])
})

test('Shuffle reorders the rack on this page and sends nothing', () => {
  const { store, sent, ui } = setup()
  const picks = [0.9, 0.1, 0.5, 0.3, 0.7, 0.2]
  let i = 0

  store.dispatch(shuffleRack(() => picks[i++ % picks.length]))

  assert.notDeepEqual(ui().order, [0, 1, 2, 3, 4, 5, 6])
  assert.deepEqual(ui().order.toSorted(), [0, 1, 2, 3, 4, 5, 6])
  assert.deepEqual(sent, [])
})

test('a new rack starts in its own order', () => {
  const { store, ui } = setup()
  store.dispatch(shuffleRack(() => 0))

  store.dispatch(stateReceived(snapshot({ rack: ['E', 'A', 'T', 'Q', 'X', 'Z', '?'] })))

  assert.deepEqual(ui().order, [0, 1, 2, 3, 4, 5, 6])
})

test('a tick that leaves the game alone keeps the same tiles object', () => {
  const { store } = setup()
  const before = store.getState().room.snapshot?.tiles

  store.dispatch(stateReceived({ ...snapshot(), players: { A: { x: 9, y: 9 }, B: { x: 2, y: 2 } } }))

  assert.equal(store.getState().room.snapshot?.tiles, before)
})
