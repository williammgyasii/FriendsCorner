// @vitest-environment jsdom
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Provider } from 'react-redux'
import { afterEach, expect, test } from 'vitest'
import { createFaces } from '../src/faces.ts'
import type { LobbyState } from '../src/lobbyLook.ts'
import { LobbyScreen } from '../src/lobby/LobbyScreen.tsx'
import { makeStore } from '../src/store/index.ts'
import { stateReceived } from '../src/store/roomSlice.ts'

afterEach(cleanup)

const baseLobby: LobbyState = {
  host: 'A',
  capacity: 2,
  pick: null,
  canStart: false,
  countdownMs: null,
  members: [
    { seat: 'A', ready: false, camera: true, mic: true, playing: true },
    { seat: 'B', ready: false, camera: true, mic: true, playing: true },
  ],
}

const show = (you: 'A' | 'B', lobby: Partial<LobbyState> = {}) => {
  const sent: unknown[] = []
  const store = makeStore({ send: (message) => sent.push(message) }, { storage: null })
  store.dispatch(
    stateReceived({
      you,
      world: null,
      board: null,
      chess: null,
      players: { A: { x: 1, y: 1 }, B: { x: 2, y: 2 } },
      lobby: { ...baseLobby, ...lobby },
    }),
  )
  render(
    <Provider store={store}>
      <LobbyScreen faces={createFaces()} />
    </Provider>,
  )
  return { sent, store }
}

test('the game master is crowned and picks a game from the grid', async () => {
  const { sent } = show('A')

  expect(screen.getByText("You're the Game Master")).toBeInTheDocument()
  await userEvent.click(screen.getByRole('button', { name: /Chess/ }))

  expect(sent).toContainEqual({ type: 'pick', game: 'chess' })
})

test('the game master can pick Letter Tiles for up to four', async () => {
  const { sent } = show('A')

  const card = screen.getByRole('button', { name: /Letter Tiles/ })
  expect(card).toHaveTextContent('2–4 players')
  await userEvent.click(card)

  expect(sent).toContainEqual({ type: 'pick', game: 'tiles' })
})

test('a joining player sees the games but cannot pick one', () => {
  show('B')

  expect(screen.getByText('The Game Master is picking a game…')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: /Chess/ })).toBeDisabled()
  expect(screen.queryByRole('button', { name: /Start|Waiting|Pick a game/ })).not.toBeInTheDocument()
})

test('a joining player readies up once a game is picked', async () => {
  const { sent } = show('B', { pick: 'chess' })

  await userEvent.click(screen.getByRole('button', { name: "I'm ready" }))

  expect(sent).toContainEqual({ type: 'ready', ready: true })
})

test('start stays locked until everyone is in and ready', async () => {
  show('A', { pick: 'chess' })
  expect(screen.getByRole('button', { name: 'Waiting for Player 2 to ready up' })).toBeDisabled()
  cleanup()

  const { sent } = show('A', {
    pick: 'chess',
    canStart: true,
    members: [baseLobby.members[0], { ...baseLobby.members[1], ready: true }],
  })
  await userEvent.click(screen.getByRole('button', { name: 'Start Chess' }))

  expect(sent).toContainEqual({ type: 'start' })
})

test('the game master can make room for more players', async () => {
  const { sent } = show('A')

  await userEvent.click(screen.getByRole('button', { name: 'Add a seat' }))

  expect(sent).toContainEqual({ type: 'capacity', size: 3 })
})

test('an open seat waits for someone to join', () => {
  show('A', { capacity: 3 })

  expect(screen.getByText('Waiting for a player…')).toBeInTheDocument()
})

test('everyone sees the countdown', () => {
  show('B', { pick: 'chess', countdownMs: 2400 })

  expect(screen.getByRole('status', { name: 'Starting in' })).toHaveTextContent('3')
})

test('turning your camera off tells the room', async () => {
  const { sent } = show('B')

  await userEvent.click(screen.getByRole('switch', { name: 'Camera' }))

  expect(sent).toContainEqual({ type: 'media', camera: false, mic: true })
})
