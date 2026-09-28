import { describeChess, needsPromotion, piecesFrom, targetsFrom, type ChessState, type Seat } from './chessLook.ts'

export type Tap =
  | { kind: 'pick'; square: string; piece: string }
  | { kind: 'move'; from: string; to: string }
  | { kind: 'promote'; from: string; to: string }
  | { kind: 'look'; piece: string | null }

// What a tap on one square means, given the position and what is already picked up.
export function tapMeaning(
  chess: ChessState,
  you: Seat,
  here: { A: boolean; B: boolean },
  selected: string | null,
  square: string,
): Tap {
  const { canMove, youAreWhite } = describeChess(chess, you, here)

  if (selected && canMove && targetsFrom(chess.legalMoves, selected).includes(square)) {
    return needsPromotion(chess.legalMoves, selected, square)
      ? { kind: 'promote', from: selected, to: square }
      : { kind: 'move', from: selected, to: square }
  }

  const piece = piecesFrom(chess.fen).get(square) ?? null
  const yours = piece !== null && (piece === piece.toUpperCase()) === youAreWhite
  if (canMove && yours && selected !== square) {
    return { kind: 'pick', square, piece }
  }

  return { kind: 'look', piece }
}
