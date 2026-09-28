import assert from 'node:assert/strict'
import { test } from 'vitest'
import { makeStore } from '../src/store/index.ts'
import { openRoomSocket, type SocketLike } from '../src/store/roomSocket.ts'

class FakeSocket implements SocketLike {
  readyState = 0
  sent: string[] = []
  private listeners = new Map<string, ((event: never) => void)[]>()

  constructor(readonly url: string) {}

  addEventListener(type: string, listener: (event: never) => void) {
    this.listeners.set(type, [...(this.listeners.get(type) ?? []), listener])
  }

  send(data: string) {
    this.sent.push(data)
  }

  close() {}

  open() {
    this.readyState = 1
    this.emit('open', {})
  }

  receive(message: unknown) {
    this.emit('message', { data: JSON.stringify(message) })
  }

  shut(reason: string) {
    this.readyState = 3
    this.emit('close', { reason })
  }

  private emit(type: string, event: unknown) {
    for (const listener of this.listeners.get(type) ?? []) {
      listener(event as never)
    }
  }
}

const setup = () => {
  const signals: unknown[] = []
  let fake: FakeSocket | null = null
  const store = makeStore({ send: (message) => room.send(message) })
  const room = openRoomSocket('ws://test/ws/r1', {
    dispatch: store.dispatch,
    onSignal: (payload) => signals.push(payload),
    connect: (url) => (fake = new FakeSocket(url)),
  })
  return { store, room, socket: fake! as FakeSocket, signals }
}

test('the socket opens on the room URL', () => {
  const { socket } = setup()

  assert.equal(socket.url, 'ws://test/ws/r1')
})

test('a joined message records the seat', () => {
  const { store, socket } = setup()

  socket.receive({ type: 'joined', seat: 'B' })

  assert.equal(store.getState().room.seat, 'B')
})

test('a state message becomes the latest server copy', () => {
  const { store, socket } = setup()

  socket.receive({ type: 'state', you: 'A', world: null, board: null, chess: null, players: { A: { x: 1, y: 1 }, B: null } })

  assert.deepEqual(store.getState().room.snapshot, {
    you: 'A',
    world: null,
    board: null,
    chess: null,
    players: { A: { x: 1, y: 1 }, B: null },
  })
})

test('face-call signals go straight to the call and stay out of the store', () => {
  const { store, socket, signals } = setup()
  const before = store.getState()

  socket.receive({ type: 'signal', payload: { kind: 'offer', sdp: 'x' } })

  assert.deepEqual(signals, [{ kind: 'offer', sdp: 'x' }])
  assert.equal(store.getState(), before)
})

test('a full room closes as full', () => {
  const { store, socket } = setup()

  socket.shut('room is full')

  assert.equal(store.getState().room.connection, 'full')
})

test('messages go out as JSON once the socket is open', () => {
  const { room, socket } = setup()
  socket.open()

  room.send({ type: 'chess-rematch' })

  assert.deepEqual(socket.sent, ['{"type":"chess-rematch"}'])
})

test('messages before the socket opens are dropped', () => {
  const { room, socket } = setup()

  room.send({ type: 'chess-rematch' })

  assert.deepEqual(socket.sent, [])
})
