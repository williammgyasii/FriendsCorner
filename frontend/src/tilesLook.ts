import type { RoomSeat } from './lobbyLook.ts'

// The Letter Tiles section as the server sent it to this seat. `rack` is only
// ever this seat's own tiles; everyone else is a count.
export type TilesState = {
  layout: string
  board: (string | null)[]
  values: Record<string, number>
  players: { seat: RoomSeat; score: number; count: number }[]
  toMove: RoomSeat
  bag: number
  rack: string[]
  lastPlay: { seat: RoomSeat; words: string[]; score: number } | null
  outcome: { winners: RoomSeat[]; scores: Partial<Record<RoomSeat, number>> } | null
  refusal: { reason: string; words: string[] } | null
}

// A tile moved from the rack to the board on this page only.
export type Unsent = { square: number; rackIndex: number; letter: string; blank: boolean }

// The server's answer to "what would these unsent tiles score?". It echoes the
// tiles it was asked about, so an answer the page has moved past can be dropped.
export type TilesPreview = {
  tiles: { square: number; letter: string; blank: boolean }[]
  words?: string[]
  score?: number
  refusal?: { reason: string; words: string[] }
}

export type TilesUi = {
  selected: number | null
  unsent: Unsent[]
  exchanging: number[] | null
  order?: number[]
  preview?: TilesPreview | null
}

export type SquareView = { square: number; label: string; letter: string | null; value: number | null; unsent: boolean }

export type RackTileView = { index: number; letter: string; value: number; selected: boolean; used: boolean; swapping: boolean }

// Which face video sits in a badge. The call is A with B only.
export type FaceSlot = 'you' | 'partner' | null

// `gain` is the last play's score on the seat that made it.
export type PlayerView = {
  seat: RoomSeat
  score: number
  count: number
  toMove: boolean
  isYou: boolean
  face: FaceSlot
  gain: number | null
}

export type TilesView = {
  squares: SquareView[]
  rack: RackTileView[]
  players: PlayerView[]
  status: string
  bag: number
  lastPlay: string | null
  refusal: string | null
  ending: string | null
  bubble: { square: number; text: string; ok: boolean } | null
  submitLabel: string
  actions: { submit: boolean; exchange: boolean; pass: boolean; rematch: boolean }
}

export type DropTarget = { kind: 'square'; square: number } | { kind: 'rack' } | { kind: 'nowhere' }

// The elements under the pointer, top first. The dragged tile itself is on top
// and marked `data-dragging`, so it is skipped.
export function dropTarget(elements: { dataset: DOMStringMap }[]): DropTarget {
  for (const { dataset } of elements) {
    if (dataset.dragging !== undefined) {
      continue
    }
    if (dataset.square !== undefined) {
      return { kind: 'square', square: Number(dataset.square) }
    }
    if (dataset.rack !== undefined) {
      return { kind: 'rack' }
    }
  }
  return { kind: 'nowhere' }
}

const callPartner: Partial<Record<RoomSeat, RoomSeat>> = { A: 'B', B: 'A' }

function faceOf(seat: RoomSeat, you: RoomSeat): FaceSlot {
  if (callPartner[you] === undefined) {
    return null
  }
  return seat === you ? 'you' : seat === callPartner[you] ? 'partner' : null
}

const EXCHANGE_NEEDS = 7

const labels: Record<string, string> = { T: 'TW', D: 'DW', t: 'TL', d: 'DL', '*': '★' }

export function squareLabel(layout: string, square: number) {
  return labels[layout[square]] ?? ''
}

const reasons: Record<string, string> = {
  'not-your-turn': "It isn't your turn",
  'not-in-rack': "You don't have those tiles",
  'not-in-line': 'Tiles must go in one row or one column',
  gap: 'Tiles must touch with no gaps',
  'first-must-cover-centre': 'The first word must cover the star',
  'first-needs-two-tiles': 'The first word needs at least two tiles',
  'not-connected': 'New tiles must touch tiles on the board',
  'bag-too-small': 'The bag needs 7 tiles to exchange',
  'game-over': 'The game is over',
}

export function describeTiles(tiles: TilesState, you: RoomSeat, ui: TilesUi): TilesView {
  const yourTurn = tiles.outcome === null && tiles.toMove === you
  const unsentAt = new Map(ui.unsent.map((tile) => [tile.square, tile]))
  const used = new Set(ui.unsent.map((tile) => tile.rackIndex))
  const valueOf = (letter: string) => tiles.values[letter] ?? 0
  const order = ui.order?.length === tiles.rack.length ? ui.order : tiles.rack.map((_, index) => index)
  const preview = ui.unsent.length > 0 ? (ui.preview ?? null) : null
  const ending = endingText(tiles)
  const scored = preview?.score !== undefined && !preview.refusal ? preview.score : null

  return {
    squares: tiles.board.map((placed, square) => {
      const unsent = unsentAt.get(square)
      const letter = placed ?? unsent?.letter ?? null
      const blank = placed ? placed !== placed.toUpperCase() : (unsent?.blank ?? false)
      return {
        square,
        label: squareLabel(tiles.layout, square),
        letter: letter?.toUpperCase() ?? null,
        value: letter && !blank ? valueOf(letter.toUpperCase()) : null,
        unsent: unsent !== undefined && placed === null,
      }
    }),
    rack: order.map((index) => {
      const letter = tiles.rack[index]
      return {
        index,
        letter,
        value: valueOf(letter),
        selected: ui.selected === index,
        used: used.has(index),
        swapping: ui.exchanging?.includes(index) ?? false,
      }
    }),
    players: tiles.players.map((player) => ({
      ...player,
      toMove: tiles.outcome === null && player.seat === tiles.toMove,
      isYou: player.seat === you,
      face: faceOf(player.seat, you),
      gain: tiles.lastPlay?.seat === player.seat ? tiles.lastPlay.score : null,
    })),
    status: ending ?? previewText(preview) ?? turnText(tiles, yourTurn),
    bag: tiles.bag,
    lastPlay: tiles.lastPlay
      ? `${tiles.lastPlay.seat} played ${tiles.lastPlay.words.join(', ')} for ${tiles.lastPlay.score}`
      : null,
    refusal: refusalText(tiles.refusal),
    ending,
    bubble: preview
      ? {
          square: Math.max(...ui.unsent.map((tile) => tile.square)),
          text: scored !== null ? String(scored) : (refusalText(preview.refusal ?? null) ?? ''),
          ok: scored !== null,
        }
      : null,
    submitLabel: scored !== null ? `Submit ${scored}` : 'Submit',
    actions: {
      submit: yourTurn && ui.unsent.length > 0,
      exchange: yourTurn && tiles.bag >= EXCHANGE_NEEDS,
      pass: yourTurn,
      rematch: tiles.outcome !== null,
    },
  }
}

function previewText(preview: TilesPreview | null) {
  if (!preview) {
    return null
  }
  if (preview.refusal) {
    return refusalText(preview.refusal)
  }
  return preview.words && preview.score !== undefined ? `${preview.words.join(', ')} for ${preview.score}` : null
}

function turnText(tiles: TilesState, yourTurn: boolean) {
  if (!yourTurn) {
    return `${tiles.toMove} is thinking`
  }
  return tiles.board.every((square) => square === null) ? 'Your turn · the first word must cover the star' : 'Your turn'
}

function refusalText(refusal: TilesState['refusal']) {
  if (!refusal) {
    return null
  }
  if (refusal.reason === 'not-a-word') {
    const verb = refusal.words.length === 1 ? 'is' : 'are'
    return `${refusal.words.join(' and ')} ${verb} not in the word list`
  }
  return reasons[refusal.reason] ?? 'That move is not allowed'
}

function endingText(tiles: TilesState) {
  if (!tiles.outcome) {
    return null
  }
  const { winners, scores } = tiles.outcome
  const finals = Object.values(scores)
    .filter((score): score is number => score !== undefined)
    .toSorted((a, b) => b - a)
    .join(' to ')
  return winners.length === 1 ? `${winners[0]} wins, ${finals}` : `${winners.join(' and ')} tie, ${finals}`
}
