import { createSelector } from '@reduxjs/toolkit'
import { checkedKing, describeChess, piecesFrom, targetsFrom } from '../chessLook.ts'
import type { ChessPicture } from '../chessView3d.ts'
import type { RootState } from './index.ts'

export const selectSnapshot = (state: RootState) => state.room.snapshot
export const selectYou = (state: RootState) => state.room.snapshot?.you ?? state.room.seat ?? 'A'
export const selectWorld = (state: RootState) => state.room.snapshot?.world ?? null
export const selectConnection = (state: RootState) => state.room.connection
export const selectChess = (state: RootState) => state.room.snapshot?.chess ?? null
export const selectBoard = (state: RootState) => state.room.snapshot?.board ?? null
export const selectPlayers = (state: RootState) => state.room.snapshot?.players ?? null

// Positions change every tick; presence rarely does. Depend on the booleans.
export const selectHere = createSelector(
  [(state: RootState) => state.room.snapshot?.players.A != null, (state: RootState) => state.room.snapshot?.players.B != null],
  (A, B) => ({ A, B }),
)

export const selectOtherHere = (state: RootState) => {
  const here = selectHere(state)
  return selectYou(state) === 'A' ? here.B : here.A
}

export const selectChessLook = createSelector([selectChess, selectYou, selectHere], (chess, you, here) =>
  chess ? describeChess(chess, you, here) : null,
)

export const selectChessPicture = createSelector(
  [selectChess, selectChessLook, (state: RootState) => state.chessUi.selected],
  (chess, look, selected): ChessPicture | null =>
    chess && look
      ? {
          pieces: piecesFrom(chess.fen),
          youAreWhite: look.youAreWhite,
          selected,
          targets: selected ? targetsFrom(chess.legalMoves, selected) : [],
          lastMove: chess.lastMove,
          check: checkedKing(chess),
        }
      : null,
)
