import { configureStore, type ThunkAction, type UnknownAction } from '@reduxjs/toolkit'
import type { SignalPayload } from '../faceCall.ts'
import { authApi } from './authApi.ts'
import { authSlice } from './authSlice.ts'
import { billingApi } from './billingApi.ts'
import { chessUiSlice } from './chessUiSlice.ts'
import { devicesSlice, loadDevices, saveDevices, type DeviceStorage } from './devicesSlice.ts'
import { roomApi } from './roomApi.ts'
import { roomSlice } from './roomSlice.ts'
import { tilesUiSlice } from './tilesUiSlice.ts'

export type RoomMessageOut =
  | { type: 'pick'; game: string }
  | { type: 'ready'; ready: boolean }
  | { type: 'capacity'; size: number }
  | { type: 'media'; camera: boolean; mic: boolean }
  | { type: 'start' }
  | { type: 'direction'; x: number; y: number }
  | { type: 'place'; square: number }
  | { type: 'rematch' }
  | { type: 'chess-move'; from: string; to: string; promotion?: string }
  | { type: 'chess-rematch' }
  | { type: 'tiles-play'; tiles: { square: number; letter: string; blank?: true }[] }
  | { type: 'tiles-preview'; tiles: { square: number; letter: string; blank?: true }[] }
  | { type: 'tiles-exchange'; letters: string }
  | { type: 'tiles-pass' }
  | { type: 'tiles-rematch' }
  | { type: 'mystery-open'; lead: string }
  | { type: 'mystery-accuse'; suspect: string }
  | { type: 'mystery-rematch' }
  | { type: 'mystery-withdraw' }
  | { type: 'mystery-settings'; level: 'easy' | 'hard'; mode: 'together' | 'race' }
  | { type: 'signal'; payload: SignalPayload }

// The one way out to the room. The socket code provides it; tests pass a fake.
export type Outbox = { send: (message: RoomMessageOut) => void }

type StoreOptions = { storage?: DeviceStorage | null }

const browserStorage = () => (typeof localStorage === 'undefined' ? null : localStorage)

export function makeStore(outbox: Outbox, { storage = browserStorage() }: StoreOptions = {}) {
  const store = configureStore({
    reducer: {
      auth: authSlice.reducer,
      room: roomSlice.reducer,
      chessUi: chessUiSlice.reducer,
      tilesUi: tilesUiSlice.reducer,
      devices: devicesSlice.reducer,
      [authApi.reducerPath]: authApi.reducer,
      [billingApi.reducerPath]: billingApi.reducer,
      [roomApi.reducerPath]: roomApi.reducer,
    },
    preloadedState: { devices: loadDevices(storage) },
    middleware: (getDefault) =>
      getDefault({ thunk: { extraArgument: outbox } }).concat(authApi.middleware, billingApi.middleware, roomApi.middleware),
  })

  let saved = store.getState().devices
  store.subscribe(() => {
    const devices = store.getState().devices
    if (devices !== saved) {
      saved = devices
      saveDevices(storage, devices)
    }
  })
  return store
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
