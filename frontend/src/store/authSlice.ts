import { createSlice } from '@reduxjs/toolkit'
import { authApi, type Account } from './authApi.ts'

// Who is signed in. The auth API writes it; the door and the home read it.
export type AuthState = {
  account: Account | null
}

const initialState: AuthState = { account: null }

export const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    const signedIn = (state: AuthState, action: { payload: Account }) => {
      state.account = action.payload
    }
    builder
      .addMatcher(authApi.endpoints.account.matchFulfilled, signedIn)
      .addMatcher(authApi.endpoints.signIn.matchFulfilled, signedIn)
      .addMatcher(authApi.endpoints.account.matchRejected, (state) => {
        state.account = null
      })
      .addMatcher(authApi.endpoints.signOut.matchFulfilled, (state) => {
        state.account = null
      })
  },
})
