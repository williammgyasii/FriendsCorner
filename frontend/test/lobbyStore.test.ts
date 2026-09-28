import assert from 'node:assert/strict'
import { test } from 'vitest'
import type { LobbyState } from '../src/lobbyLook.ts'
import { cameraChosen, setMedia } from '../src/store/devicesSlice.ts'
import { makeStore } from '../src/store/index.ts'
import { stateReceived, type RoomSnapshot } from '../src/store/roomSlice.ts'
import { selectLobbyView } from '../src/store/selectors.ts'

const lobby: LobbyState = {
  host: 'A',
  capacity: 2,
  pick: 'chess',
  canStart: false,
  countdownMs: null,
  members: [
    { seat: 'A', ready: false, camera: true, mic: true, playing: true },
    { seat: 'B', ready: false, camera: true, mic: true, playing: true },
  ],
}

const snapshot = (x: number): RoomSnapshot => ({
  you: 'B',
  world: null,
  board: null,
  chess: null,
  players: { A: { x, y: 1 }, B: { x: 2, y: 2 } },
  lobby: structuredClone(lobby),
})

const memoryStorage = () => {
  const items = new Map<string, string>()
  return {
    getItem: (key: string) => items.get(key) ?? null,
    setItem: (key: string, value: string) => void items.set(key, value),
  }
}

const setup = (storage = memoryStorage()) => {
  const sent: unknown[] = []
  const store = makeStore({ send: (message) => sent.push(message) }, { storage })
  return { store, sent, storage }
}

test('a floor tick keeps the same lobby view so the lobby does not repaint', () => {
  const { store } = setup()
  store.dispatch(stateReceived(snapshot(1)))
  const before = selectLobbyView(store.getState())

  store.dispatch(stateReceived(snapshot(9)))

  assert.equal(selectLobbyView(store.getState()), before)
  assert.equal(before?.role, 'guest')
})

test('turning the camera off tells the room and keeps the mic as it was', () => {
  const { store, sent } = setup()

  store.dispatch(setMedia({ camera: false }))

  assert.deepEqual(sent, [{ type: 'media', camera: false, mic: true }])
  assert.equal(store.getState().devices.camera, false)
})

test('camera and mic choices are remembered for the next visit', () => {
  const storage = memoryStorage()
  const first = setup(storage).store
  first.dispatch(cameraChosen('cam-2'))
  first.dispatch(setMedia({ mic: false }))

  const next = setup(storage).store.getState().devices

  assert.equal(next.cameraId, 'cam-2')
  assert.equal(next.mic, false)
  assert.equal(next.camera, true)
})

test('a broken saved choice falls back to the defaults', () => {
  const storage = memoryStorage()
  storage.setItem('friendscorner.devices', '{not json')

  assert.deepEqual(setup(storage).store.getState().devices, { cameraId: null, micId: null, camera: true, mic: true })
})
