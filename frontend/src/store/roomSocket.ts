import type { Dispatch } from '@reduxjs/toolkit'
import type { SignalPayload } from '../faceCall.ts'
import type { RoomSeat } from '../lobbyLook.ts'
import type { RoomMessageOut } from './index.ts'
import type { TilesPreview } from '../tilesLook.ts'
import { closed, joined, stateReceived, type RoomSnapshot } from './roomSlice.ts'
import { previewReceived } from './tilesUiSlice.ts'

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
    const message = JSON.parse(String(event.data)) as { type: string; seat?: RoomSeat; payload?: SignalPayload }
    if (message.type === 'joined' && message.seat) {
      dispatch(joined(message.seat))
    } else if (message.type === 'signal' && message.payload) {
      onSignal(message.payload)
    } else if (message.type === 'tiles-preview') {
      const { type: _type, ...answer } = message as unknown as TilesPreview & { type: string }
      dispatch(previewReceived(answer))
    } else if (message.type === 'state') {
      const { you, world, board, chess, players, lobby = null, tiles = null } = message as unknown as RoomSnapshot
      dispatch(stateReceived({ you, world, board, chess, players, lobby, tiles }))
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
