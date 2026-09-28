import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import type { AppThunk } from './index.ts'

// Which camera and mic this browser uses. Only the browser can know its
// devices, so this is the one lobby setting that is not the server's.
export type DevicesState = {
  cameraId: string | null
  micId: string | null
  camera: boolean
  mic: boolean
}

export type DeviceStorage = Pick<Storage, 'getItem' | 'setItem'>

const storageKey = 'friendscorner.devices'
const defaults: DevicesState = { cameraId: null, micId: null, camera: true, mic: true }

export function loadDevices(storage: DeviceStorage | null): DevicesState {
  try {
    const saved = storage?.getItem(storageKey)
    return saved ? { ...defaults, ...(JSON.parse(saved) as Partial<DevicesState>) } : defaults
  } catch {
    return defaults
  }
}

export function saveDevices(storage: DeviceStorage | null, devices: DevicesState) {
  try {
    storage?.setItem(storageKey, JSON.stringify(devices))
  } catch {
    // Private mode or a full disk: the choice just is not remembered.
  }
}

export const devicesSlice = createSlice({
  name: 'devices',
  initialState: defaults,
  reducers: {
    cameraChosen(state, action: PayloadAction<string>) {
      state.cameraId = action.payload
    },
    micChosen(state, action: PayloadAction<string>) {
      state.micId = action.payload
    },
    mediaToggled(state, action: PayloadAction<{ camera?: boolean; mic?: boolean }>) {
      state.camera = action.payload.camera ?? state.camera
      state.mic = action.payload.mic ?? state.mic
    },
  },
})

export const { cameraChosen, micChosen, mediaToggled } = devicesSlice.actions

export const setMedia =
  (change: { camera?: boolean; mic?: boolean }): AppThunk =>
  (dispatch, getState, { send }) => {
    dispatch(mediaToggled(change))
    const { camera, mic } = getState().devices
    send({ type: 'media', camera, mic })
  }
