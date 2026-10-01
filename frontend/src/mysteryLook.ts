import { createAvatar } from '@dicebear/core'
import { create, meta, schema } from '@dicebear/avataaars'
import type { MysteryLevel, MysteryMode, RoomSeat } from './lobbyLook.ts'

export type MysteryPhase = 'writing' | 'investigating' | 'accusing' | 'revealed' | 'failed'

export type MysteryMood = 'frost' | 'storm' | 'velvet' | 'garden' | 'smoke' | 'gilded'

type Seat = 'A' | 'B'

// Text and by are null while the lead is closed (or, in a race, opened by your partner).
export type MysteryLead = { id: string; kind: 'clue' | 'statement'; about: string; text: string | null; by: Seat | null }

// The murder mystery section as the server sent it for your seat. Nothing
// secret is in it until the outcome.
export type MysteryState = {
  phase: MysteryPhase
  level: MysteryLevel
  mode: MysteryMode
  mood: MysteryMood | null
  title: string | null
  setting: string | null
  victim: { name: string; found: string } | null
  suspects: { id: string; name: string; role: string; motive: string; alibi: string }[]
  places: { id: string; name: string }[]
  leads: MysteryLead[]
  leadsLeft: number
  picks: { A: string | null; B: string | null }
  out: Seat[]
  endsInMs: number | null
  locksInMs: number | null
  outcome: {
    killer: string
    how: string
    why: string
    story: string
    accused: string | null
    right: boolean
    score: number
    winner: Seat | null
    timedOut: boolean
  } | null
}

export type LeadLook = { id: string; label: string; text: string | null; canOpen: boolean; by: 'you' | 'partner' | null }

export type SuspectLook = MysteryState['suspects'][number] & {
  statements: LeadLook[]
  yours: boolean
  partners: boolean
  canAccuse: boolean
}

export type PlaceLook = { id: string; name: string; clues: LeadLook[] }

export type MysteryLook = {
  status: string
  writing: boolean
  mood: MysteryMood
  tags: string[]
  clock: { text: string; warn: boolean } | null
  finalAnswer: { name: string; seconds: number } | null
  out: boolean
  title: string | null
  setting: string | null
  victim: MysteryState['victim']
  suspects: SuspectLook[]
  places: PlaceLook[]
  hint: string | null
  reveal: { headline: string; killer: string; how: string; why: string; story: string; score: number; right: boolean } | null
  actions: { tryAgain: boolean; newCase: boolean }
}

export const writingLines = [
  'Choosing a victim…',
  'Inviting the suspects…',
  'Hiding the clues…',
  'Teaching someone to lie…',
  'Sealing the envelope…',
]

const avataaars = { create, meta, schema }

// The same name always draws the same face, so both players see one person.
export function suspectPortrait(name: string) {
  return createAvatar(avataaars, { seed: name, size: 56 }).toDataUri()
}

const partnerOf: Partial<Record<RoomSeat, Seat>> = { A: 'B', B: 'A' }

const words = { easy: 'Easy', hard: 'Hard', together: 'Together', race: 'Race' } as const

export function describeMystery(mystery: MysteryState, you: RoomSeat): MysteryLook {
  const investigating = mystery.phase === 'investigating'
  const partner = partnerOf[you]
  const seat = you === 'A' || you === 'B' ? you : null
  const yourPick = seat ? mystery.picks[seat] : null
  const partnerPick = partner ? mystery.picks[partner] : null
  const out = seat !== null && mystery.out.includes(seat)
  const acting = investigating && !out

  const leadsAbout = (about: string, label: string): LeadLook[] =>
    mystery.leads
      .filter((lead) => lead.about === about)
      .map((lead, index) => ({
        id: lead.id,
        label: `${label}, ${lead.kind} ${index + 1}`,
        text: lead.text,
        canOpen: acting && lead.text === null && mystery.leadsLeft > 0,
        by: lead.by === null ? null : lead.by === seat ? 'you' : 'partner',
      }))

  return {
    status: status(mystery, seat),
    writing: mystery.phase === 'writing',
    mood: mystery.mood ?? 'smoke',
    tags: [words[mystery.level], words[mystery.mode]],
    clock: clock(mystery),
    finalAnswer: finalAnswer(mystery),
    out,
    title: mystery.title,
    setting: mystery.setting,
    victim: mystery.victim,
    suspects: mystery.suspects.map((suspect) => ({
      ...suspect,
      statements: leadsAbout(suspect.id, suspect.name),
      yours: yourPick === suspect.id,
      partners: partnerPick === suspect.id,
      canAccuse: acting && partner !== undefined,
    })),
    places: mystery.places.map((place) => ({ ...place, clues: leadsAbout(place.id, place.name) })),
    hint: investigating ? hint(mystery, out, yourPick, partnerPick) : null,
    reveal: reveal(mystery, seat),
    actions: { tryAgain: mystery.phase === 'failed', newCase: mystery.phase === 'revealed' },
  }
}

function status(mystery: MysteryState, seat: Seat | null) {
  switch (mystery.phase) {
    case 'writing':
      return 'Writing your case…'
    case 'failed':
      return "Couldn't write a case"
    case 'accusing':
      return 'Final answer…'
    case 'revealed':
      return headline(mystery, seat)
    default:
      return mystery.leadsLeft === 0 ? 'No leads left' : `${mystery.leadsLeft} ${mystery.leadsLeft === 1 ? 'lead' : 'leads'} left`
  }
}

function clock(mystery: MysteryState): MysteryLook['clock'] {
  const playing = mystery.phase === 'investigating' || mystery.phase === 'accusing'
  if (!playing || mystery.endsInMs === null) {
    return null
  }
  const seconds = Math.max(0, Math.ceil(mystery.endsInMs / 1000))
  return { text: `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`, warn: mystery.endsInMs < 60_000 }
}

function finalAnswer(mystery: MysteryState): MysteryLook['finalAnswer'] {
  const accused = mystery.picks.A
  if (mystery.phase !== 'accusing' || accused === null) {
    return null
  }
  const name = mystery.suspects.find((suspect) => suspect.id === accused)?.name ?? accused
  return { name, seconds: Math.max(0, Math.ceil((mystery.locksInMs ?? 0) / 1000)) }
}

function hint(mystery: MysteryState, out: boolean, yours: string | null, partners: string | null) {
  if (out) {
    return "You're out. Your partner is still looking."
  }
  if (mystery.mode === 'race') {
    return 'First to name the killer wins. You get one guess.'
  }
  if (yours === null && partners === null) {
    return 'Open leads, then pick the killer together'
  }
  return yours === partners ? null : 'Pick the same suspect to accuse'
}

function headline(mystery: MysteryState, seat: Seat | null) {
  const outcome = mystery.outcome
  if (outcome?.timedOut) {
    return 'Out of time'
  }
  if (mystery.mode === 'race') {
    if (!outcome?.winner) {
      return 'Nobody got it'
    }
    return outcome.winner === seat ? 'You win!' : 'Your partner wins'
  }
  return outcome?.right ? 'Solved!' : 'Not this time'
}

function reveal(mystery: MysteryState, seat: Seat | null): MysteryLook['reveal'] {
  const outcome = mystery.outcome
  if (mystery.phase !== 'revealed' || !outcome) {
    return null
  }
  const killer = mystery.suspects.find((suspect) => suspect.id === outcome.killer)?.name ?? outcome.killer
  return {
    headline: headline(mystery, seat),
    killer,
    how: outcome.how,
    why: outcome.why,
    story: outcome.story,
    score: outcome.score,
    right: outcome.right,
  }
}
