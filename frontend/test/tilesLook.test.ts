import assert from 'node:assert/strict'
import { test } from 'vitest'
import { describeTiles, dropTarget, squareLabel, type TilesUi } from '../src/tilesLook.ts'
import { layout, tiles } from './tilesFixture.ts'

const idle: TilesUi = { selected: null, unsent: [], exchanging: null }

test('premium squares are labelled and the centre is a star', () => {
  assert.equal(squareLabel(layout, 0), 'TW')
  assert.equal(squareLabel(layout, 3), 'DL')
  assert.equal(squareLabel(layout, 20), 'TL')
  assert.equal(squareLabel(layout, 16), 'DW')
  assert.equal(squareLabel(layout, 112), '★')
  assert.equal(squareLabel(layout, 1), '')
})

test('on your turn Submit needs a placed tile; Exchange and Pass are open', () => {
  const nothingPlaced = describeTiles(tiles(), 'A', idle)
  const placed = describeTiles(tiles(), 'A', { ...idle, unsent: [{ square: 112, rackIndex: 1, letter: 'A', blank: false }] })

  assert.deepEqual(nothingPlaced.actions, { submit: false, exchange: true, pass: true, rematch: false })
  assert.equal(placed.actions.submit, true)
})

test('off your turn every action is off', () => {
  const view = describeTiles(tiles(), 'B', { ...idle, unsent: [{ square: 112, rackIndex: 1, letter: 'A', blank: false }] })

  assert.deepEqual(view.actions, { submit: false, exchange: false, pass: false, rematch: false })
})

test('with fewer than 7 in the bag Exchange is off but Pass stays on', () => {
  const view = describeTiles(tiles({ bag: 5 }), 'A', idle)

  assert.equal(view.actions.exchange, false)
  assert.equal(view.actions.pass, true)
})

test('the panel shows scores, the seat to move, the bag, and the last play', () => {
  const view = describeTiles(
    tiles({
      players: [
        { seat: 'A', score: 10, count: 7 },
        { seat: 'B', score: 0, count: 7 },
      ],
      toMove: 'B',
      bag: 83,
      lastPlay: { seat: 'A', words: ['CAT'], score: 10 },
    }),
    'A',
    idle,
  )

  assert.deepEqual(view.players, [
    { seat: 'A', score: 10, count: 7, toMove: false, isYou: true, face: 'you', gain: 10 },
    { seat: 'B', score: 0, count: 7, toMove: true, isYou: false, face: 'partner', gain: null },
  ])
  assert.equal(view.bag, 83)
  assert.equal(view.lastPlay, 'A played CAT for 10')
})

test('board tiles show their value; a blank shows its letter and no value', () => {
  const board = tiles().board.slice()
  board[111] = 'C'
  board[113] = 't'
  const view = describeTiles(tiles({ board }), 'A', {
    ...idle,
    unsent: [{ square: 112, rackIndex: 6, letter: 'A', blank: true }],
  })

  assert.deepEqual(view.squares[111], { square: 111, label: '', letter: 'C', value: 3, unsent: false })
  assert.deepEqual(view.squares[112], { square: 112, label: '★', letter: 'A', value: null, unsent: true })
  assert.deepEqual(view.squares[113], { square: 113, label: '', letter: 'T', value: null, unsent: false })
})

test('rack tiles on the board are hidden from the rack, and the selected one is marked', () => {
  const view = describeTiles(tiles(), 'A', {
    ...idle,
    selected: 0,
    unsent: [{ square: 112, rackIndex: 1, letter: 'A', blank: false }],
  })

  assert.deepEqual(
    view.rack.map((tile) => [tile.letter, tile.value, tile.selected, tile.used]),
    [
      ['C', 3, true, false],
      ['A', 1, false, true],
      ['T', 1, false, false],
      ['Q', 10, false, false],
      ['X', 8, false, false],
      ['Z', 10, false, false],
      ['?', 0, false, false],
    ],
  )
})

test('a refused word is named', () => {
  const view = describeTiles(tiles({ refusal: { reason: 'not-a-word', words: ['QXZ'] } }), 'A', idle)

  assert.equal(view.refusal, 'QXZ is not in the word list')
})

test('two refused words are both named', () => {
  const view = describeTiles(tiles({ refusal: { reason: 'not-a-word', words: ['QXZ', 'TX'] } }), 'A', idle)

  assert.equal(view.refusal, 'QXZ and TX are not in the word list')
})

test('other refusals read as a short reason', () => {
  const view = describeTiles(tiles({ refusal: { reason: 'first-must-cover-centre', words: [] } }), 'A', idle)

  assert.equal(view.refusal, 'The first word must cover the star')
})

test('the end names the winner and the scores, and offers a rematch', () => {
  const view = describeTiles(
    tiles({
      players: [
        { seat: 'A', score: 180, count: 2 },
        { seat: 'B', score: 212, count: 0 },
      ],
      outcome: { winners: ['B'], scores: { A: 180, B: 212 } },
    }),
    'A',
    idle,
  )

  assert.equal(view.ending, 'B wins, 212 to 180')
  assert.deepEqual(view.actions, { submit: false, exchange: false, pass: false, rematch: true })
})

const played = () => {
  const board = tiles().board.slice()
  board[112] = 'A'
  board[113] = 'T'
  return board
}

test('the status line says whose turn it is', () => {
  assert.equal(describeTiles(tiles({ board: played() }), 'A', idle).status, 'Your turn')
  assert.equal(describeTiles(tiles(), 'B', idle).status, 'A is thinking')
  assert.equal(
    describeTiles(tiles({ outcome: { winners: ['B'], scores: { A: 180, B: 212 } } }), 'A', idle).status,
    'B wins, 212 to 180',
  )
})

test('on an empty board the status line hints at the star', () => {
  assert.equal(describeTiles(tiles(), 'A', idle).status, 'Your turn · the first word must cover the star')
})

test('while placing, the status line reads the kept preview', () => {
  const ok = describeTiles(tiles(), 'A', { ...idle, unsent: cat, preview: { tiles: catAsked, words: ['CAT'], score: 10 } })
  const refused = describeTiles(tiles({ board: played() }), 'A', {
    ...idle,
    unsent: cat,
    preview: { tiles: catAsked, refusal: { reason: 'not-connected', words: [] } },
  })

  assert.equal(ok.status, 'CAT for 10')
  assert.equal(refused.status, 'New tiles must touch tiles on the board')
})

test('the ending wins over a preview in the status line', () => {
  const view = describeTiles(tiles({ outcome: { winners: ['B'], scores: { A: 180, B: 212 } } }), 'A', {
    ...idle,
    unsent: cat,
    preview: { tiles: catAsked, words: ['CAT'], score: 10 },
  })

  assert.equal(view.status, 'B wins, 212 to 180')
})

test('the gain sits on the seat that made the last play, and nowhere before a play', () => {
  const after = describeTiles(tiles({ lastPlay: { seat: 'B', words: ['CAT'], score: 10 } }), 'A', idle)

  assert.deepEqual(after.players.map((player) => player.gain), [null, 10])
  assert.deepEqual(describeTiles(tiles(), 'A', idle).players.map((player) => player.gain), [null, null])
})

test('only the call pair A and B get face slots; C and D show their letter', () => {
  const four = tiles({
    players: [
      { seat: 'A', score: 0, count: 7 },
      { seat: 'B', score: 0, count: 7 },
      { seat: 'C', score: 0, count: 7 },
      { seat: 'D', score: 0, count: 7 },
    ],
  })

  assert.deepEqual(describeTiles(four, 'B', idle).players.map((player) => player.face), ['partner', 'you', null, null])
  assert.deepEqual(describeTiles(four, 'C', idle).players.map((player) => player.face), [null, null, null, null])
})

const cat = [
  { square: 111, rackIndex: 0, letter: 'C', blank: false },
  { square: 112, rackIndex: 1, letter: 'A', blank: false },
  { square: 113, rackIndex: 2, letter: 'T', blank: false },
]
const catAsked = cat.map(({ square, letter, blank }) => ({ square, letter, blank }))

test('a kept preview shows its score by the last unsent tile and on Submit', () => {
  const view = describeTiles(tiles(), 'A', { ...idle, unsent: cat, preview: { tiles: catAsked, words: ['CAT'], score: 10 } })

  assert.deepEqual(view.bubble, { square: 113, text: '10', ok: true })
  assert.equal(view.submitLabel, 'Submit 10')
})

test('a refused preview shows the reason and Submit stays open on your turn', () => {
  const view = describeTiles(tiles(), 'A', {
    ...idle,
    unsent: cat,
    preview: { tiles: catAsked, refusal: { reason: 'not-a-word', words: ['QXZ'] } },
  })

  assert.deepEqual(view.bubble, { square: 113, text: 'QXZ is not in the word list', ok: false })
  assert.equal(view.submitLabel, 'Submit')
  assert.equal(view.actions.submit, true)
})

test('without unsent tiles there is no bubble', () => {
  const view = describeTiles(tiles(), 'A', idle)

  assert.equal(view.bubble, null)
  assert.equal(view.submitLabel, 'Submit')
})

test('the rack is shown in the page order', () => {
  const view = describeTiles(tiles(), 'A', { ...idle, order: [6, 5, 4, 3, 2, 1, 0] })

  assert.deepEqual(view.rack.map((tile) => [tile.index, tile.letter]).slice(0, 2), [[6, '?'], [5, 'Z']])
})

test('a drop lands on the first square or rack under the pointer, skipping the dragged tile', () => {
  const at = (dataset: Record<string, string>) => ({ dataset })

  assert.deepEqual(dropTarget([at({ dragging: '' }), at({ square: '126' }), at({ rack: '' })]), { kind: 'square', square: 126 })
  assert.deepEqual(dropTarget([at({ dragging: '' }), at({ rack: '' })]), { kind: 'rack' })
  assert.deepEqual(dropTarget([at({}), at({})]), { kind: 'nowhere' })
})

test('a tie names every winner', () => {
  const view = describeTiles(
    tiles({ outcome: { winners: ['A', 'B'], scores: { A: 210, B: 210 } } }),
    'A',
    idle,
  )

  assert.equal(view.ending, 'A and B tie, 210 to 210')
})
