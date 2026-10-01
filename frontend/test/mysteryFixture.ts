import type { MysteryLead, MysteryState } from '../src/mysteryLook.ts'

const lead = (id: string, kind: 'clue' | 'statement', about: string, text: string | null = null, by: 'A' | 'B' | null = null): MysteryLead => ({
  id,
  kind,
  about,
  text,
  by,
})

// The server's section mid-investigation on easy, together: A opened c1, B opened t4, 6 leads left, no picks.
export const mystery = (extra: Partial<MysteryState> = {}): MysteryState => ({
  phase: 'investigating',
  level: 'easy',
  mode: 'together',
  mood: 'smoke',
  title: 'Death at the Lantern Inn',
  setting: 'A snowed-in coaching inn on the moors, 1923.',
  victim: { name: 'Edmund Hale', found: 'Found at the foot of the cellar stairs at midnight.' },
  suspects: [
    { id: 's1', name: 'Ada Finch', role: 'Cook', motive: 'Edmund owed her wages.', alibi: 'In the kitchen all evening.' },
    { id: 's2', name: 'Basil Crane', role: 'Guest', motive: 'Edmund ruined his brother.', alibi: 'Asleep in room 4.' },
    { id: 's3', name: 'Clara Voss', role: 'Innkeeper', motive: 'Edmund was selling the inn.', alibi: 'Doing the accounts.' },
    { id: 's4', name: 'Dev Mistry', role: 'Stable hand', motive: 'Edmund had him dismissed.', alibi: 'Tending the horses.' },
  ],
  places: [
    { id: 'p1', name: 'Kitchen' },
    { id: 'p2', name: 'Cellar' },
    { id: 'p3', name: 'Stables' },
  ],
  leads: [
    lead('c1', 'clue', 'p1', 'The bread oven was lit at eleven and never left alone.', 'A'),
    lead('c2', 'clue', 'p1'),
    lead('c3', 'clue', 'p2'),
    lead('c4', 'clue', 'p2'),
    lead('c5', 'clue', 'p3'),
    lead('c6', 'clue', 'p3'),
    lead('t1', 'statement', 's1'),
    lead('t2', 'statement', 's1'),
    lead('t3', 'statement', 's2'),
    lead('t4', 'statement', 's2', 'I heard footsteps on the cellar stairs.', 'B'),
    lead('t5', 'statement', 's3'),
    lead('t6', 'statement', 's3'),
    lead('t7', 'statement', 's4'),
    lead('t8', 'statement', 's4'),
  ],
  leadsLeft: 6,
  picks: { A: null, B: null },
  out: [],
  endsInMs: null,
  locksInMs: null,
  outcome: null,
  ...extra,
})

export const writing = (): MysteryState => ({
  phase: 'writing',
  level: 'easy',
  mode: 'together',
  mood: null,
  title: null,
  setting: null,
  victim: null,
  suspects: [],
  places: [],
  leads: [],
  leadsLeft: 8,
  picks: { A: null, B: null },
  out: [],
  endsInMs: null,
  locksInMs: null,
  outcome: null,
})
