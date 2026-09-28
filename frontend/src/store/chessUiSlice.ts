import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import { tapMeaning, type Tap } from '../chessTap.ts'
import type { AppThunk } from './index.ts'
import { stateReceived } from './roomSlice.ts'
import { selectHere } from './selectors.ts'

type ChessUiState = {
  selected: string | null
  inspected: string | null
  pendingPromotion: { from: string; to: string } | null
  fen: string | null
}

const initialState: ChessUiState = { selected: null, inspected: null, pendingPromotion: null, fen: null }

export const chessUiSlice = createSlice({
  name: 'chessUi',
  initialState,
  reducers: {
    tapped(state, action: PayloadAction<Tap>) {
      const tap = action.payload
      state.selected = tap.kind === 'pick' ? tap.square : null
      state.inspected = tap.kind === 'pick' || tap.kind === 'look' ? tap.piece : null
      if (tap.kind === 'promote') {
        state.pendingPromotion = { from: tap.from, to: tap.to }
      }
    },
    promotionSettled(state) {
      state.pendingPromotion = null
    },
  },
  extraReducers: (builder) => {
    builder.addCase(stateReceived, (state, action) => {
      const fen = action.payload.chess?.fen ?? null
      if (fen !== state.fen) {
        state.fen = fen
        state.selected = null
        state.pendingPromotion = null
      }
    })
  },
})

const { tapped, promotionSettled } = chessUiSlice.actions

export const chooseChessSquare =
  (square: string): AppThunk =>
  (dispatch, getState, { send }) => {
    const state = getState()
    const snapshot = state.room.snapshot
    if (!snapshot?.chess) {
      return
    }

    const tap = tapMeaning(snapshot.chess, snapshot.you, selectHere(state), state.chessUi.selected, square)
    if (tap.kind === 'move') {
      send({ type: 'chess-move', from: tap.from, to: tap.to })
    }
    dispatch(tapped(tap))
  }

export const choosePromotion =
  (piece: string): AppThunk =>
  (dispatch, getState, { send }) => {
    const pending = getState().chessUi.pendingPromotion
    if (pending) {
      send({ type: 'chess-move', from: pending.from, to: pending.to, promotion: piece })
    }
    dispatch(promotionSettled())
  }

export const cancelPromotion = (): AppThunk => (dispatch) => {
  dispatch(promotionSettled())
}
