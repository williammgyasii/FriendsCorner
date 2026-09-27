export type MarksBoard = {
  squares: (string | null)[]
  next: 'A' | 'B'
  winner: 'A' | 'B' | null
  draw: boolean
}

export type MarksLook = {
  turn: 'A' | 'B' | null
  color: string
  status: string
  celebrate: string | null
  winning: number[]
  canRematch: boolean
}

const seatColor = {
  A: '#2f6bff',
  B: '#e23d6b',
} as const

const lines = [
  [0, 1, 2],
  [3, 4, 5],
  [6, 7, 8],
  [0, 3, 6],
  [1, 4, 7],
  [2, 5, 8],
  [0, 4, 8],
  [2, 4, 6],
]

export function describeMarks(board: MarksBoard, you: 'A' | 'B'): MarksLook {
  const yours = you === 'A' ? 'X' : 'O'
  const winning = winningSquares(board.squares)
  if (board.winner) {
    return {
      turn: board.winner,
      color: seatColor[board.winner],
      status: board.winner === you ? 'You won.' : 'They won.',
      celebrate: board.winner === you ? 'You won' : 'They won',
      winning,
      canRematch: true,
    }
  }

  if (board.draw) {
    return {
      turn: null,
      color: '#1c1915',
      status: 'Draw.',
      celebrate: 'Draw',
      winning: [],
      canRematch: true,
    }
  }

  return {
    turn: board.next,
    color: seatColor[board.next],
    status: board.next === you ? `Your turn. You are ${yours}.` : `Their turn. You are ${yours}.`,
    celebrate: null,
    winning: [],
    canRematch: false,
  }
}

export type PlayerCard = {
  seat: 'A' | 'B'
  mark: 'X' | 'O'
  color: string
  label: 'You' | 'Them'
  active: boolean
  note: string
}

export function describePlayers(
  board: MarksBoard,
  you: 'A' | 'B',
  here: { A: boolean; B: boolean },
): { left: PlayerCard; right: PlayerCard } {
  const other = you === 'A' ? 'B' : 'A'
  const card = (seat: 'A' | 'B'): PlayerCard => {
    const base = {
      seat,
      mark: seat === 'A' ? ('X' as const) : ('O' as const),
      color: seatColor[seat],
      label: seat === you ? ('You' as const) : ('Them' as const),
    }
    if (!here[seat]) {
      return { ...base, active: false, note: 'Not here' }
    }
    if (board.winner) {
      const won = board.winner === seat
      return { ...base, active: won, note: won ? 'Winner' : '' }
    }
    if (board.draw) {
      return { ...base, active: false, note: 'Draw' }
    }
    const moving = board.next === seat
    if (!moving) {
      return { ...base, active: false, note: 'Waiting' }
    }
    return { ...base, active: true, note: seat === you ? 'Your move' : 'Their move' }
  }

  return { left: card(you), right: card(other) }
}

function winningSquares(squares: (string | null)[]): number[] {
  for (const line of lines) {
    const mark = squares[line[0]]
    if (mark && mark === squares[line[1]] && mark === squares[line[2]]) {
      return line
    }
  }

  return []
}
