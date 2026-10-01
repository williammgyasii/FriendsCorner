export type RoomSeat = 'A' | 'B' | 'C' | 'D'

export type LobbyMember = {
  seat: RoomSeat
  gameName: string
  ready: boolean
  camera: boolean
  mic: boolean
  playing: boolean
}

// The lobby as the server sent it. Members are in join order; the first is
// the game master.
export type MysteryLevel = 'easy' | 'hard'
export type MysteryMode = 'together' | 'race'
export type MysterySettings = { level: MysteryLevel; mode: MysteryMode }

export type LobbyState = {
  host: RoomSeat | null
  capacity: number
  pick: string | null
  canStart: boolean
  countdownMs: number | null
  members: LobbyMember[]
  mystery?: MysterySettings
}

export type SettingChoice<T extends string> = { id: T; label: string; blurb: string; on: boolean }

export type MysterySettingsView = {
  canChange: boolean
  chosen: MysterySettings
  levels: SettingChoice<MysteryLevel>[]
  modes: SettingChoice<MysteryMode>[]
}

const levels: Omit<SettingChoice<MysteryLevel>, 'on'>[] = [
  { id: 'easy', label: 'Easy', blurb: '8 leads, no clock. Plain clues.' },
  { id: 'hard', label: 'Hard', blurb: '6 leads and a clock. A sly liar. +50 if you crack it.' },
]

const modes: Omit<SettingChoice<MysteryMode>, 'on'>[] = [
  { id: 'together', label: 'Together', blurb: 'Detectives as a team. Agree on the killer.' },
  { id: 'race', label: 'Race', blurb: 'Your own leads, one guess each. First to the killer wins.' },
]

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
  { id: 'tiles', name: 'Letter Tiles', blurb: 'Build words on one shared board. Only you see your tiles.', players: '2–4 players' },
  { id: 'mystery', name: 'Murder Mystery', blurb: 'A new case every time. Share 8 leads and agree on the killer.', players: '2 players' },
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
  mystery: MysterySettingsView | null
}

export function describeLobby(lobby: LobbyState, you: RoomSeat): LobbyView {
  const role = lobby.host === you ? 'host' : 'guest'
  const counting = lobby.countdownMs !== null
  const pick = gameCards.find((game) => game.id === lobby.pick) ?? null
  const me = lobby.members.find((member) => member.seat === you)

  const members: Slot[] = lobby.members.map((member, index) => ({
    kind: 'member',
    seat: member.seat,
    label: member.gameName,
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
    mystery: lobby.pick === 'mystery' ? mysterySettings(lobby.mystery, role === 'host' && !counting) : null,
  }
}

function mysterySettings(chosen: MysterySettings = { level: 'easy', mode: 'together' }, canChange: boolean): MysterySettingsView {
  return {
    canChange,
    chosen,
    levels: levels.map((level) => ({ ...level, on: level.id === chosen.level })),
    modes: modes.map((mode) => ({ ...mode, on: mode.id === chosen.mode })),
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
