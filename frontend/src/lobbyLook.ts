export type RoomSeat = 'A' | 'B' | 'C' | 'D'

export type LobbyMember = {
  seat: RoomSeat
  ready: boolean
  camera: boolean
  mic: boolean
  playing: boolean
}

// The lobby as the server sent it. Members are in join order; the first is
// the game master.
export type LobbyState = {
  host: RoomSeat | null
  capacity: number
  pick: string | null
  canStart: boolean
  countdownMs: number | null
  members: LobbyMember[]
}

export type GameCard = {
  id: string
  name: string
  blurb: string
  players: string
}

export const gameCards: GameCard[] = [
  { id: 'room', name: 'The Room', blurb: 'A floor, a window, and everyone together.', players: '2–4 players' },
  { id: 'tictactoe', name: 'Tic-tac-toe', blurb: 'Nine squares. Three in a row wins.', players: '2 players' },
  { id: 'chess', name: 'Chess', blurb: 'The classic, in 3D. Tap a piece to see its moves.', players: '2 players' },
]

export type Slot =
  | {
      kind: 'member'
      seat: RoomSeat
      label: string
      isYou: boolean
      isHost: boolean
      ready: boolean
      camera: boolean
      mic: boolean
      watching: boolean
    }
  | { kind: 'open'; index: number }

export type LobbyView = {
  role: 'host' | 'guest'
  headline: string
  slots: Slot[]
  pick: GameCard | null
  canPick: boolean
  canResize: boolean
  start: { enabled: boolean; label: string } | null
  ready: { on: boolean; enabled: boolean } | null
  countdown: number | null
}

export function describeLobby(lobby: LobbyState, you: RoomSeat): LobbyView {
  const role = lobby.host === you ? 'host' : 'guest'
  const counting = lobby.countdownMs !== null
  const pick = gameCards.find((game) => game.id === lobby.pick) ?? null
  const me = lobby.members.find((member) => member.seat === you)

  const members: Slot[] = lobby.members.map((member, index) => ({
    kind: 'member',
    seat: member.seat,
    label: member.seat === you ? 'You' : member.seat === lobby.host ? 'Game Master' : `Player ${index + 1}`,
    isYou: member.seat === you,
    isHost: member.seat === lobby.host,
    ready: member.ready,
    camera: member.camera,
    mic: member.mic,
    watching: !member.playing,
  }))
  const open: Slot[] = Array.from({ length: Math.max(0, lobby.capacity - members.length) }, (_, index) => ({
    kind: 'open',
    index: members.length + index,
  }))

  return {
    role,
    headline: role === 'host' ? "You're the Game Master" : guestHeadline(pick, me),
    slots: [...members, ...open],
    pick,
    canPick: role === 'host' && !counting,
    canResize: role === 'host' && !counting,
    start: role === 'host' ? startButton(lobby, pick, members) : null,
    ready: role === 'guest' ? { on: me?.ready ?? false, enabled: pick !== null && !counting } : null,
    countdown: counting ? Math.ceil((lobby.countdownMs ?? 0) / 1000) : null,
  }
}

function guestHeadline(pick: GameCard | null, me: LobbyMember | undefined) {
  if (!pick) {
    return 'The Game Master is picking a game…'
  }
  const watching = me && !me.playing ? ' · you will watch this one' : ''
  return `The Game Master picked ${pick.name}${watching}`
}

function startButton(lobby: LobbyState, pick: GameCard | null, members: Slot[]) {
  if (lobby.countdownMs !== null) {
    return { enabled: false, label: 'Starting…' }
  }
  const missing = lobby.capacity - lobby.members.length
  if (missing > 0) {
    return { enabled: false, label: `Waiting for ${missing} more ${missing === 1 ? 'player' : 'players'}` }
  }
  if (!pick) {
    return { enabled: false, label: 'Pick a game' }
  }
  const notReady = members.filter((slot) => slot.kind === 'member' && !slot.isHost && !slot.ready)
  if (notReady.length > 0) {
    const names = notReady.map((slot) => (slot.kind === 'member' ? slot.label : '')).join(' and ')
    return { enabled: false, label: `Waiting for ${names} to ready up` }
  }
  return { enabled: lobby.canStart, label: `Start ${pick.name}` }
}
