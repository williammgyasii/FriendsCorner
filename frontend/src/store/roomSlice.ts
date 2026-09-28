import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import type { ChessState, Seat } from '../chessLook.ts'
import type { LobbyState, RoomSeat } from '../lobbyLook.ts'
import type { TilesState } from '../tilesLook.ts'

export type Player = { x: number; y: number } | null

export type BoardState = {
  squares: (string | null)[]
  next: Seat
  winner: Seat | null
  draw: boolean
}

// The latest copy of the room the server sent. The server owns it; the
// browser only replaces it when a new message arrives.
export type RoomSnapshot = {
  you: RoomSeat
  world: string | null
  board: BoardState | null
  chess: ChessState | null
  tiles?: TilesState | null
  players: { A: Player; B: Player; C?: Player; D?: Player }
  lobby: LobbyState | null
}

export type Connection = 'connecting' | 'open' | 'full' | 'gone'

type RoomState = {
  seat: RoomSeat | null
  connection: Connection
  snapshot: RoomSnapshot | null
}

const initialState: RoomState = { seat: null, connection: 'connecting', snapshot: null }

const same = (a: unknown, b: unknown) => JSON.stringify(a) === JSON.stringify(b)

export const roomSlice = createSlice({
  name: 'room',
  initialState,
  reducers: {
    joined(state, action: PayloadAction<RoomSeat>) {
      state.seat = action.payload
      state.connection = 'open'
    },
    // Ticks arrive 20 times a second; games that did not change keep their
    // old objects so selectors built on them stay cached.
    stateReceived(state, action: PayloadAction<RoomSnapshot>) {
      const next = action.payload
      state.seat = next.you
      state.connection = 'open'
      if (!state.snapshot) {
        state.snapshot = next
        return
      }

      state.snapshot.you = next.you
      state.snapshot.world = next.world
      state.snapshot.players = next.players
      if (!same(state.snapshot.board, next.board)) {
        state.snapshot.board = next.board
      }
      if (!same(state.snapshot.chess, next.chess)) {
        state.snapshot.chess = next.chess
      }
      if (!same(state.snapshot.tiles ?? null, next.tiles ?? null)) {
        state.snapshot.tiles = next.tiles ?? null
      }
      if (!same(state.snapshot.lobby, next.lobby)) {
        state.snapshot.lobby = next.lobby
      }
    },
    closed(state, action: PayloadAction<string>) {
      state.connection = action.payload === 'room is full' ? 'full' : 'gone'
    },
  },
})

export const { joined, stateReceived, closed } = roomSlice.actions
