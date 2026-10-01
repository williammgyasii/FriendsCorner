export type Seat = 'A' | 'B'

export type LegalMove = { from: string; to: string; promotion: string | null }

export type ChessState = {
  fen: string
  white: Seat
  toMove: Seat
  inCheck: boolean
  lastMove: { from: string; to: string } | null
  outcome: { ending: 'checkmate' | 'stalemate'; winner: Seat | null } | null
  legalMoves: LegalMove[]
}

export type ChessPicture = {
  pieces: Map<string, string>
  youAreWhite: boolean
  selected: string | null
  targets: string[]
  lastMove: { from: string; to: string } | null
  check: string | null
}

export type ChessCard = {
  seat: Seat
  color: string
  label: 'You' | 'Them'
  side: 'White' | 'Black'
  active: boolean
  note: string
  taken: string[]
}

export type ChessLook = {
  youAreWhite: boolean
  canMove: boolean
  color: string
  status: string
  celebrate: string | null
  canRematch: boolean
  left: ChessCard
  right: ChessCard
}

const seatColor = { A: '#2f6bff', B: '#e23d6b' } as const
const files = 'abcdefgh'

export function piecesFrom(fen: string): Map<string, string> {
  const pieces = new Map<string, string>()
  const rows = fen.split(' ')[0].split('/')
  rows.forEach((row, index) => {
    const rank = 8 - index
    let file = 0
    for (const symbol of row) {
      if (/\d/.test(symbol)) {
        file += Number(symbol)
        continue
      }
      pieces.set(`${files[file]}${rank}`, symbol)
      file += 1
    }
  })
  return pieces
}

const startingCount: Record<string, number> = { q: 1, r: 2, b: 2, n: 2, p: 8 }

function missing(onBoard: Map<string, string>, upper: boolean): string[] {
  const count = (type: string) => [...onBoard.values()].filter((piece) => piece === (upper ? type.toUpperCase() : type)).length
  const officers = ['q', 'r', 'b', 'n']
  const promoted = officers.reduce((sum, type) => sum + Math.max(0, count(type) - startingCount[type]), 0)
  const gone = (type: string) =>
    type === 'p' ? Math.max(0, startingCount.p - count('p') - promoted) : Math.max(0, startingCount[type] - count(type))
  return [...officers, 'p'].flatMap((type) => Array<string>(gone(type)).fill(upper ? type.toUpperCase() : type))
}

export function capturedBy(fen: string): { white: string[]; black: string[] } {
  const onBoard = piecesFrom(fen)
  return { white: missing(onBoard, false), black: missing(onBoard, true) }
}

export function squaresInView(youAreWhite: boolean): string[] {
  const squares: string[] = []
  for (let row = 0; row < 8; row++) {
    for (let column = 0; column < 8; column++) {
      const rank = youAreWhite ? 8 - row : row + 1
      const file = youAreWhite ? column : 7 - column
      squares.push(`${files[file]}${rank}`)
    }
  }
  return squares
}

export function targetsFrom(legalMoves: LegalMove[], from: string): string[] {
  return [...new Set(legalMoves.filter((move) => move.from === from).map((move) => move.to))]
}

export function needsPromotion(legalMoves: LegalMove[], from: string, to: string): boolean {
  return legalMoves.some((move) => move.from === from && move.to === to && move.promotion !== null)
}

export function checkedKing(chess: ChessState): string | null {
  if (!chess.inCheck) {
    return null
  }
  const king = chess.toMove === chess.white ? 'K' : 'k'
  for (const [square, piece] of piecesFrom(chess.fen)) {
    if (piece === king) {
      return square
    }
  }
  return null
}

export function describeChess(chess: ChessState, you: Seat, here: { A: boolean; B: boolean }): ChessLook {
  const other: Seat = you === 'A' ? 'B' : 'A'
  const youAreWhite = chess.white === you
  const bothHere = here.A && here.B
  const captured = capturedBy(chess.fen)

  const card = (seat: Seat): ChessCard => {
    const base = {
      seat,
      color: seatColor[seat],
      label: seat === you ? ('You' as const) : ('Them' as const),
      side: seat === chess.white ? ('White' as const) : ('Black' as const),
      taken: seat === chess.white ? captured.white : captured.black,
    }
    if (!here[seat]) {
      return { ...base, active: false, note: 'Not here' }
    }
    if (chess.outcome) {
      const won = chess.outcome.winner === seat
      return { ...base, active: won, note: won ? 'Winner' : chess.outcome.winner ? '' : 'Draw' }
    }
    if (chess.toMove !== seat) {
      return { ...base, active: false, note: 'Waiting' }
    }
    return { ...base, active: true, note: seat === you ? 'Your move' : 'Their move' }
  }

  const cards = { left: card(you), right: card(other) }

  if (chess.outcome?.ending === 'checkmate' && chess.outcome.winner) {
    const winner = chess.outcome.winner
    return {
      youAreWhite,
      canMove: false,
      color: seatColor[winner],
      status: winner === you ? 'Checkmate. You won.' : 'Checkmate. They won.',
      celebrate: winner === you ? 'Checkmate! You won' : 'Checkmate! They won',
      canRematch: true,
      ...cards,
    }
  }

  if (chess.outcome) {
    return {
      youAreWhite,
      canMove: false,
      color: '#a39a8e',
      status: 'Stalemate. Nobody can move, so it is a draw.',
      celebrate: 'Stalemate: a draw',
      canRematch: true,
      ...cards,
    }
  }

  const yourMove = chess.toMove === you
  const side = youAreWhite ? 'white' : 'black'
  let status: string
  if (!bothHere) {
    status = `Waiting for them to come back. You play ${side}.`
  } else if (yourMove && chess.inCheck) {
    status = 'Check! Your king is under attack. Move it or block.'
  } else if (yourMove) {
    status = `Your move. You play ${side}.`
  } else if (chess.inCheck) {
    status = 'Check on them. Their move.'
  } else {
    status = `Their move. You play ${side}.`
  }

  return {
    youAreWhite,
    canMove: yourMove && bothHere,
    color: seatColor[chess.toMove],
    status,
    celebrate: null,
    canRematch: false,
    ...cards,
  }
}

const hints: Record<string, string> = {
  k: 'King: one square any way. Keep it safe. If it is trapped, the game is over.',
  q: 'Queen: any distance in a straight line or diagonal. Your strongest piece.',
  r: 'Rook: any distance up, down, left, or right.',
  b: 'Bishop: any distance diagonally, always on its own square color.',
  n: 'Knight: jumps in an L shape (two one way, one to the side) and can hop over pieces.',
  p: 'Pawn: one step forward (two on its first move), captures one step diagonally. Reach the far side to become a new piece.',
}

export function pieceHint(piece: string): string {
  return hints[piece.toLowerCase()] ?? ''
}
