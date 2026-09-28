import type { Dispatch } from '@reduxjs/toolkit'
import type { Seat } from '../chessLook.ts'
import type { SignalPayload } from '../faceCall.ts'
import type { RoomMessageOut } from './index.ts'
import { closed, joined, stateReceived, type RoomSnapshot } from './roomSlice.ts'

export type SocketLike = {
  readonly readyState: number
  addEventListener(type: 'message', listener: (event: { data: unknown }) => void): void
  addEventListener(type: 'close', listener: (event: { reason: string }) => void): void
  send(data: string): void
  close(): void
}

type RoomSocketOptions = {
  dispatch: Dispatch
  // Signals are one-shot handshake data for the face call, not room state.
  onSignal: (payload: SignalPayload) => void
  connect?: (url: string) => SocketLike
}

const open = 1

export function openRoomSocket(url: string, { dispatch, onSignal, connect = (to) => new WebSocket(to) }: RoomSocketOptions) {
  const socket = connect(url)

  socket.addEventListener('message', (event) => {
    const message = JSON.parse(String(event.data)) as { type: string; seat?: Seat; payload?: SignalPayload }
    if (message.type === 'joined' && message.seat) {
      dispatch(joined(message.seat))
    } else if (message.type === 'signal' && message.payload) {
      onSignal(message.payload)
    } else if (message.type === 'state') {
      const { you, world, board, chess, players } = message as unknown as RoomSnapshot
      dispatch(stateReceived({ you, world, board, chess, players }))
    }
  })

  socket.addEventListener('close', (event) => {
    dispatch(closed(event.reason))
  })

  return {
    send(message: RoomMessageOut) {
      if (socket.readyState === open) {
        socket.send(JSON.stringify(message))
      }
    },
    close() {
      socket.close()
    },
  }
}

export type RoomSocket = ReturnType<typeof openRoomSocket>
