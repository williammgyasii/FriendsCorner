import assert from 'node:assert/strict'
import { test } from 'vitest'
import { describeLobby, type LobbyState } from '../src/lobbyLook.ts'

const member = (seat: 'A' | 'B' | 'C' | 'D', extra: Partial<LobbyState['members'][number]> = {}) => ({
  seat,
  gameName: seat === 'A' ? 'Countess' : 'Bea',
  ready: false,
  camera: true,
  mic: true,
  playing: true,
  ...extra,
})

const lobby = (extra: Partial<LobbyState> = {}): LobbyState => ({
  host: 'A',
  capacity: 2,
  pick: null,
  canStart: false,
  countdownMs: null,
  members: [member('A'), member('B')],
  ...extra,
})

test('the game master sees the controls and a crown on their own slot', () => {
  const view = describeLobby(lobby(), 'A')

  assert.equal(view.role, 'host')
  assert.equal(view.canPick, true)
  assert.equal(view.canResize, true)
  assert.equal(view.ready, null)
  assert.deepEqual(
    view.slots.map((slot) => slot.kind === 'member' && [slot.label, slot.isHost, slot.isYou]),
    [
      ['Countess', true, true],
      ['Bea', false, false],
    ],
  )
})

test('a joining player sees the game master by name and a ready button, not the controls', () => {
  const view = describeLobby(lobby(), 'B')

  assert.equal(view.role, 'guest')
  assert.equal(view.canPick, false)
  assert.equal(view.canResize, false)
  assert.equal(view.start, null)
  assert.deepEqual(view.ready, { on: false, enabled: false })
  assert.equal(view.headline, 'The Game Master is picking a game…')
  assert.deepEqual(
    view.slots.map((slot) => slot.kind === 'member' && slot.label),
    ['Countess', 'Bea'],
  )
})

test('empty seats show as open slots waiting for someone', () => {
  const view = describeLobby(lobby({ capacity: 3 }), 'A')

  assert.deepEqual(
    view.slots.map((slot) => slot.kind),
    ['member', 'member', 'open'],
  )
})

test('start tells the game master who they are waiting for', () => {
  assert.deepEqual(describeLobby(lobby({ capacity: 3 }), 'A').start, {
    enabled: false,
    label: 'Waiting for 1 more player',
  })
  assert.deepEqual(describeLobby(lobby({ members: [member('A')] }), 'A').start, {
    enabled: false,
    label: 'Waiting for 1 more player',
  })
  assert.deepEqual(describeLobby(lobby(), 'A').start, { enabled: false, label: 'Pick a game' })
  assert.deepEqual(describeLobby(lobby({ pick: 'chess' }), 'A').start, {
    enabled: false,
    label: 'Waiting for Bea to ready up',
  })
  assert.deepEqual(
    describeLobby(lobby({ pick: 'chess', canStart: true, members: [member('A'), member('B', { ready: true })] }), 'A').start,
    { enabled: true, label: 'Start Chess' },
  )
})

test('a guest can ready up once a game is picked', () => {
  const view = describeLobby(lobby({ pick: 'chess' }), 'B')

  assert.deepEqual(view.ready, { on: false, enabled: true })
  assert.equal(view.headline, 'The Game Master picked Chess')
  assert.equal(view.pick?.name, 'Chess')
})

test('the countdown shows whole seconds left', () => {
  assert.equal(describeLobby(lobby({ pick: 'chess', countdownMs: 2400 }), 'B').countdown, 3)
  assert.equal(describeLobby(lobby({ pick: 'chess', countdownMs: 900 }), 'B').countdown, 1)
  assert.equal(describeLobby(lobby(), 'B').countdown, null)
})

test('nobody changes the pick or readies during the countdown', () => {
  const host = describeLobby(lobby({ pick: 'chess', countdownMs: 2000 }), 'A')
  const guest = describeLobby(lobby({ pick: 'chess', countdownMs: 2000 }), 'B')

  assert.equal(host.canPick, false)
  assert.deepEqual(host.start, { enabled: false, label: 'Starting…' })
  assert.deepEqual(guest.ready, { on: false, enabled: false })
})

test('a player who will watch a two-player game is told so', () => {
  const view = describeLobby(
    lobby({ capacity: 3, pick: 'chess', members: [member('A'), member('B'), member('C', { playing: false })] }),
    'C',
  )

  const you = view.slots[2]
  assert.equal(you.kind === 'member' && you.watching, true)
  assert.equal(view.headline, 'The Game Master picked Chess · you will watch this one')
})

test('when the game master leaves, the next player in is promoted and sees the controls', () => {
  const view = describeLobby(lobby({ host: 'B', members: [member('B')] }), 'B')

  assert.equal(view.role, 'host')
  assert.equal(view.canPick, true)
})

test('the mystery settings only show once Murder Mystery is picked', () => {
  assert.equal(describeLobby(lobby({ pick: 'chess' }), 'A').mystery, null)
  assert.notEqual(describeLobby(lobby({ pick: 'mystery' }), 'A').mystery, null)
})

test('the game master can change the mystery settings and sees what is chosen', () => {
  const settings = describeLobby(lobby({ pick: 'mystery', mystery: { level: 'easy', mode: 'together' } }), 'A').mystery

  assert.equal(settings?.canChange, true)
  assert.deepEqual(
    settings?.levels.map((level) => [level.id, level.label, level.on]),
    [
      ['easy', 'Easy', true],
      ['hard', 'Hard', false],
    ],
  )
  assert.deepEqual(
    settings?.modes.map((mode) => [mode.id, mode.label, mode.on]),
    [
      ['together', 'Together', true],
      ['race', 'Race', false],
    ],
  )
  assert.match(settings?.levels[1].blurb ?? '', /clock/)
  assert.match(settings?.modes[1].blurb ?? '', /First/)
})

test('a guest sees the mystery settings but cannot change them', () => {
  const settings = describeLobby(lobby({ pick: 'mystery', mystery: { level: 'hard', mode: 'race' } }), 'B').mystery

  assert.equal(settings?.canChange, false)
  assert.deepEqual(settings?.levels.map((level) => level.on), [false, true])
  assert.deepEqual(settings?.modes.map((mode) => mode.on), [false, true])
})

test('the mystery settings lock during the countdown', () => {
  const settings = describeLobby(lobby({ pick: 'mystery', countdownMs: 2000 }), 'A').mystery

  assert.equal(settings?.canChange, false)
})

test('a lobby without settings reads as easy and together', () => {
  const settings = describeLobby(lobby({ pick: 'mystery' }), 'A').mystery

  assert.deepEqual([settings?.levels[0].on, settings?.modes[0].on], [true, true])
})
