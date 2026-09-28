import { createSelector } from '@reduxjs/toolkit'
import { checkedKing, describeChess, piecesFrom, targetsFrom, type Seat } from '../chessLook.ts'
import type { ChessPicture } from '../chessLook.ts'
import { describeLobby } from '../lobbyLook.ts'
import { describeTiles } from '../tilesLook.ts'
import type { RootState } from './index.ts'

export const selectSnapshot = (state: RootState) => state.room.snapshot
export const selectYou = (state: RootState) => state.room.snapshot?.you ?? state.room.seat ?? 'A'
export const selectWorld = (state: RootState) => state.room.snapshot?.world ?? null
export const selectConnection = (state: RootState) => state.room.connection
export const selectChess = (state: RootState) => state.room.snapshot?.chess ?? null
export const selectBoard = (state: RootState) => state.room.snapshot?.board ?? null
export const selectPlayers = (state: RootState) => state.room.snapshot?.players ?? null
export const selectLobby = (state: RootState) => state.room.snapshot?.lobby ?? null

// Two-player games are between seats A and B; anyone else watches.
export const selectPlayerSeat = (state: RootState): Seat | null => {
  const you = selectYou(state)
  return you === 'A' || you === 'B' ? you : null
}

// Positions change every tick; presence rarely does. Depend on the booleans.
export const selectHere = createSelector(
  [(state: RootState) => state.room.snapshot?.players.A != null, (state: RootState) => state.room.snapshot?.players.B != null],
  (A, B) => ({ A, B }),
)

export const selectOtherHere = (state: RootState) => {
  const here = selectHere(state)
  const seat = selectPlayerSeat(state)
  return seat === 'A' ? here.B : seat === 'B' ? here.A : false
}

export const selectLobbyView = createSelector([selectLobby, selectYou], (lobby, you) =>
  lobby ? describeLobby(lobby, you) : null,
)

export const selectTiles = (state: RootState) => state.room.snapshot?.tiles ?? null

export const selectTilesView = createSelector([selectTiles, selectYou, (state: RootState) => state.tilesUi], (tiles, you, ui) =>
  tiles ? describeTiles(tiles, you, ui) : null,
)

export const selectChessLook = createSelector([selectChess, selectPlayerSeat, selectHere], (chess, seat, here) => {
  if (!chess) {
    return null
  }
  if (seat) {
    return describeChess(chess, seat, here)
  }
  const look = describeChess(chess, chess.white, here)
  return { ...look, canMove: false, canRematch: false, celebrate: null, status: 'You are watching.' }
})

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
