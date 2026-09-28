// @vitest-environment jsdom
import { act, cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Provider } from 'react-redux'
import { afterEach, expect, test, vi } from 'vitest'
import { makeStore } from '../src/store/index.ts'
import { stateReceived } from '../src/store/roomSlice.ts'
import { previewReceived } from '../src/store/tilesUiSlice.ts'
import type { TilesState } from '../src/tilesLook.ts'
import { TilesScreen } from '../src/tiles/TilesScreen.tsx'
import { tiles } from './tilesFixture.ts'

afterEach(cleanup)

const show = (extra: Partial<TilesState> = {}, you: 'A' | 'B' = 'A') => {
  const sent: unknown[] = []
  const store = makeStore({ send: (message) => sent.push(message) }, { storage: null })
  store.dispatch(
    stateReceived({
      you,
      world: 'tiles',
      board: null,
      chess: null,
      tiles: tiles(extra),
      players: { A: { x: 1, y: 1 }, B: { x: 2, y: 2 } },
      lobby: null,
    }),
  )
  render(
    <Provider store={store}>
      <TilesScreen />
    </Provider>,
  )
  return { sent, store }
}

const plays = (sent: unknown[]) => sent.filter((message) => (message as { type: string }).type === 'tiles-play')

const square = (index: number) => screen.getByRole('button', { name: new RegExp(`^Square ${index}\\b`) })

test('the board has 225 squares with a star in the middle, and your 7 tiles', () => {
  show()

  expect(screen.getAllByRole('button', { name: /^Square \d+/ })).toHaveLength(225)
  expect(square(112)).toHaveTextContent('★')
  const rack = within(screen.getByRole('group', { name: 'Your tiles' }))
  expect(rack.getAllByRole('button')).toHaveLength(7)
})

test('other players show only as a score and a tile count', () => {
  show()

  const panel = within(screen.getByRole('list', { name: 'Scores' }))
  expect(panel.getByText('B')).toBeInTheDocument()
  expect(panel.getAllByText('7 tiles')).toHaveLength(2)
})

test('tap a tile, tap a square, then Submit sends the play', async () => {
  const { sent } = show()
  const rack = within(screen.getByRole('group', { name: 'Your tiles' }))

  expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled()
  await userEvent.click(rack.getByRole('button', { name: /^C,/ }))
  await userEvent.click(square(112))
  await userEvent.click(rack.getByRole('button', { name: /^A,/ }))
  await userEvent.click(square(113))

  expect(square(112)).toHaveTextContent('C')
  expect(plays(sent)).toEqual([])
  await userEvent.click(screen.getByRole('button', { name: 'Submit' }))

  expect(plays(sent)).toEqual([
    {
      type: 'tiles-play',
      tiles: [
        { square: 112, letter: 'C' },
        { square: 113, letter: 'A' },
      ],
    },
  ])
})

test('every player has a badge with score and tiles, and the seat to move is marked', () => {
  show({
    players: [
      { seat: 'A', score: 10, count: 7 },
      { seat: 'B', score: 0, count: 7 },
    ],
    toMove: 'B',
  })

  const [a, b] = within(screen.getByRole('list', { name: 'Scores' })).getAllByRole('listitem')
  expect(a).toHaveTextContent('10')
  expect(a).toHaveTextContent('7 tiles')
  expect(b).toHaveAttribute('aria-current', 'true')
  expect(a).not.toHaveAttribute('aria-current')
})

test('the status line says whose turn it is', () => {
  show({}, 'B')

  expect(screen.getByRole('status')).toHaveTextContent('A is thinking')
})

test('your badge and your partner badge hold the face videos', () => {
  show()

  const [a, b] = within(screen.getByRole('list', { name: 'Scores' })).getAllByRole('listitem')
  expect(a.querySelector('[data-face="you"]')).not.toBeNull()
  expect(b.querySelector('[data-face="partner"]')).not.toBeNull()
})

test('placing a tile asks for a preview, and the answer shows on the board and on Submit', async () => {
  const { sent, store } = show()
  const rack = within(screen.getByRole('group', { name: 'Your tiles' }))

  await userEvent.click(rack.getByRole('button', { name: /^C,/ }))
  await userEvent.click(square(111))
  await userEvent.click(rack.getByRole('button', { name: /^A,/ }))
  await userEvent.click(square(112))

  expect(sent.at(-1)).toEqual({ type: 'tiles-preview', tiles: [{ square: 111, letter: 'C' }, { square: 112, letter: 'A' }] })
  act(() => {
    store.dispatch(
      previewReceived({
        tiles: [
          { square: 111, letter: 'C', blank: false },
          { square: 112, letter: 'A', blank: false },
        ],
        words: ['CA'],
        score: 8,
      }),
    )
  })

  expect(screen.getByRole('button', { name: 'Submit 8' })).toBeEnabled()
  expect(screen.getByRole('note', { name: 'Preview: 8' })).toHaveTextContent('8')
})

test('Shuffle reorders your tiles and sends nothing', async () => {
  const random = vi.spyOn(Math, 'random').mockReturnValue(0)
  const { sent } = show()
  const letters = () =>
    within(screen.getByRole('group', { name: 'Your tiles' }))
      .getAllByRole('button')
      .map((tile) => tile.getAttribute('aria-label')?.[0])

  const before = letters()
  await userEvent.click(screen.getByRole('button', { name: 'Shuffle' }))

  expect(letters()).not.toEqual(before)
  expect(letters().toSorted()).toEqual(before.toSorted())
  expect(sent).toEqual([])
  random.mockRestore()
})

test('the last play and the bag are shown', () => {
  show({ lastPlay: { seat: 'A', words: ['CAT'], score: 10 }, bag: 83 })

  expect(screen.getByText('A played CAT for 10')).toBeInTheDocument()
  expect(screen.getByText('83 in the bag')).toBeInTheDocument()
})

test('a blank asks for its letter', async () => {
  show()
  const rack = within(screen.getByRole('group', { name: 'Your tiles' }))

  await userEvent.click(rack.getByRole('button', { name: /^Blank/ }))
  await userEvent.click(square(112))
  const dialog = within(screen.getByRole('dialog', { name: 'Which letter is the blank?' }))
  await userEvent.click(dialog.getByRole('button', { name: 'E' }))

  expect(square(112)).toHaveTextContent('E')
})

test('off your turn the actions are disabled', () => {
  show({}, 'B')

  expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled()
  expect(screen.getByRole('button', { name: 'Exchange' })).toBeDisabled()
  expect(screen.getByRole('button', { name: 'Pass' })).toBeDisabled()
})

test('a refusal is shown to the player', () => {
  show({ refusal: { reason: 'not-a-word', words: ['QXZ'] } })

  expect(screen.getByRole('alert')).toHaveTextContent('QXZ is not in the word list')
})

test('after a play the scorer panel shows the gain', () => {
  show({
    players: [
      { seat: 'A', score: 10, count: 7 },
      { seat: 'B', score: 0, count: 7 },
    ],
    toMove: 'B',
    lastPlay: { seat: 'A', words: ['CAT'], score: 10 },
  })

  const [a, b] = within(screen.getByRole('list', { name: 'Scores' })).getAllByRole('listitem')
  expect(a).toHaveTextContent('+10')
  expect(b).not.toHaveTextContent('+')
})

test('on an empty board the status line hints at the star', () => {
  show()

  expect(screen.getByRole('status')).toHaveTextContent('Your turn · the first word must cover the star')
})

const fullscreen = (enabled: boolean) => {
  const request = vi.fn(() => Promise.resolve())
  const exit = vi.fn(() => Promise.resolve())
  Object.defineProperty(document, 'fullscreenEnabled', { configurable: true, value: enabled })
  Object.defineProperty(document, 'fullscreenElement', { configurable: true, writable: true, value: null })
  Object.defineProperty(document.documentElement, 'requestFullscreen', { configurable: true, value: request })
  Object.defineProperty(document, 'exitFullscreen', { configurable: true, value: exit })
  const change = (element: Element | null) =>
    act(() => {
      ;(document as { fullscreenElement: Element | null }).fullscreenElement = element
      document.dispatchEvent(new Event('fullscreenchange'))
    })
  return { request, exit, change }
}

test('where the browser allows it, Full screen enters and leaves full screen', async () => {
  const { request, exit, change } = fullscreen(true)
  show()

  await userEvent.click(screen.getByRole('button', { name: 'Full screen' }))
  expect(request).toHaveBeenCalledOnce()
  change(document.documentElement)
  await userEvent.click(screen.getByRole('button', { name: 'Leave full screen' }))

  expect(exit).toHaveBeenCalledOnce()
})

test('where the browser does not allow it, there is no full-screen button', () => {
  fullscreen(false)
  show()

  expect(screen.queryByRole('button', { name: /full screen/i })).toBeNull()
})

test('the end shows the result and a Rematch button', async () => {
  const { sent } = show({
    players: [
      { seat: 'A', score: 180, count: 2 },
      { seat: 'B', score: 212, count: 0 },
    ],
    outcome: { winners: ['B'], scores: { A: 180, B: 212 } },
  })

  expect(screen.getByText('B wins, 212 to 180')).toBeInTheDocument()
  await userEvent.click(screen.getByRole('button', { name: 'Rematch' }))

  expect(sent).toEqual([{ type: 'tiles-rematch' }])
})
