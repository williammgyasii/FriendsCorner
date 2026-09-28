import { configureStore, type ThunkAction, type UnknownAction } from '@reduxjs/toolkit'
import type { SignalPayload } from '../faceCall.ts'
import { chessUiSlice } from './chessUiSlice.ts'
import { roomApi } from './roomApi.ts'
import { roomSlice } from './roomSlice.ts'

export type RoomMessageOut =
  | { type: 'launch'; world: string }
  | { type: 'direction'; x: number; y: number }
  | { type: 'place'; square: number }
  | { type: 'rematch' }
  | { type: 'chess-move'; from: string; to: string; promotion?: string }
  | { type: 'chess-rematch' }
  | { type: 'signal'; payload: SignalPayload }

// The one way out to the room. The socket code provides it; tests pass a fake.
export type Outbox = { send: (message: RoomMessageOut) => void }

export function makeStore(outbox: Outbox) {
  return configureStore({
    reducer: {
      room: roomSlice.reducer,
      chessUi: chessUiSlice.reducer,
      [roomApi.reducerPath]: roomApi.reducer,
    },
    middleware: (getDefault) => getDefault({ thunk: { extraArgument: outbox } }).concat(roomApi.middleware),
  })
}

export type AppStore = ReturnType<typeof makeStore>
export type RootState = ReturnType<AppStore['getState']>
export type AppDispatch = AppStore['dispatch']
export type AppThunk<Result = void> = ThunkAction<Result, RootState, Outbox, UnknownAction>

export const sendToRoom =
  (message: RoomMessageOut): AppThunk =>
  (_dispatch, _getState, { send }) => {
    send(message)
  }
