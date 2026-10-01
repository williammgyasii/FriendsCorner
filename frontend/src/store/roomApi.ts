import { createApi } from '@reduxjs/toolkit/query/react'
import { fallbackIceServers, iceServersFrom, type IceServer } from '../ice.ts'
import { baseQuery } from './baseQuery.ts'
import type { AppThunk } from './index.ts'

// The edge issues TURN credentials for 24 hours; reuse them for an hour.
const turnReuseSeconds = 60 * 60

export const roomApi = createApi({
  reducerPath: 'roomApi',
  baseQuery,
  endpoints: (build) => ({
    createRoom: build.mutation<string, void>({
      query: () => ({ url: 'rooms', method: 'POST' }),
      transformResponse: (body: { id: string }) => body.id,
    }),
    iceServers: build.query<IceServer[], string>({
      async queryFn(roomId, _api, _extra, query) {
        const result = await query(`turn?room=${encodeURIComponent(roomId)}`)
        if (result.error) return { data: fallbackIceServers }
        return { data: iceServersFrom(result.data) }
      },
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
