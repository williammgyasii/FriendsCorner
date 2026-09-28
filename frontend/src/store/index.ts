import { configureStore, type ThunkAction, type UnknownAction } from '@reduxjs/toolkit'
import type { SignalPayload } from '../faceCall.ts'
import { chessUiSlice } from './chessUiSlice.ts'
import { devicesSlice, loadDevices, saveDevices, type DeviceStorage } from './devicesSlice.ts'
import { roomApi } from './roomApi.ts'
import { roomSlice } from './roomSlice.ts'

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
  | { type: 'signal'; payload: SignalPayload }

// The one way out to the room. The socket code provides it; tests pass a fake.
export type Outbox = { send: (message: RoomMessageOut) => void }

type StoreOptions = { storage?: DeviceStorage | null }

const browserStorage = () => (typeof localStorage === 'undefined' ? null : localStorage)

export function makeStore(outbox: Outbox, { storage = browserStorage() }: StoreOptions = {}) {
  const store = configureStore({
    reducer: {
      room: roomSlice.reducer,
      chessUi: chessUiSlice.reducer,
      devices: devicesSlice.reducer,
      [roomApi.reducerPath]: roomApi.reducer,
    },
    preloadedState: { devices: loadDevices(storage) },
    middleware: (getDefault) => getDefault({ thunk: { extraArgument: outbox } }).concat(roomApi.middleware),
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
