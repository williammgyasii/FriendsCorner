import { createApi } from '@reduxjs/toolkit/query/react'
import { baseQuery } from './baseQuery.ts'

export type Account = {
  id: string
  email: string
  name: string
  gameName: string
  plan: string | null
}

export type AccountSignIn = {
  action: 'register' | 'login'
  name: string
  gameName: string
  email: string
  password: string
}

export const authApi = createApi({
  reducerPath: 'authApi',
  baseQuery,
  endpoints: (build) => ({
    account: build.query<Account, void>({
      query: () => 'account',
    }),
    signIn: build.mutation<Account, AccountSignIn>({
      query: (body) => ({ url: 'account', method: 'POST', body }),
    }),
    signOut: build.mutation<null, void>({
      query: () => ({ url: 'account', method: 'POST', body: { action: 'logout' } }),
    }),
  }),
})
