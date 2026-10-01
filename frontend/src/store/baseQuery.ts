import { fetchBaseQuery } from '@reduxjs/toolkit/query/react'

// Same origin and cookie for every feature API. This is the one fetch.
export const baseQuery = fetchBaseQuery({
  baseUrl: typeof location === 'undefined' ? 'http://localhost' : location.origin,
  credentials: 'include',
})
