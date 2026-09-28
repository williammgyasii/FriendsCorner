import { createApi, fakeBaseQuery } from '@reduxjs/toolkit/query'
import { loadIceServers, type IceServer } from '../ice.ts'
import type { AppThunk } from './index.ts'

// The edge issues TURN credentials for 24 hours; reuse them for an hour.
const turnReuseSeconds = 60 * 60

export const roomApi = createApi({
  reducerPath: 'roomApi',
  baseQuery: fakeBaseQuery<{ status: number }>(),
  endpoints: (build) => ({
    createRoom: build.mutation<string, void>({
      queryFn: async () => {
        const response = await fetch('/rooms', { method: 'POST' })
        if (!response.ok) {
          return { error: { status: response.status } }
        }
        const body = (await response.json()) as { id: string }
        return { data: body.id }
      },
    }),
    iceServers: build.query<IceServer[], string>({
      queryFn: async (roomId) => ({ data: await loadIceServers(roomId) }),
      keepUnusedDataFor: turnReuseSeconds,
    }),
  }),
})

// For code outside React (the face call): read through the cache without
// holding a subscription open.
export const iceServersFor =
  (roomId: string): AppThunk<Promise<IceServer[]>> =>
  (dispatch) =>
    dispatch(roomApi.endpoints.iceServers.initiate(roomId, { subscribe: false })).unwrap()
