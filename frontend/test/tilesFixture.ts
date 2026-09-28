import type { TilesState } from '../src/tilesLook.ts'

// The layout the server sends, row 0 first.
export const layout = [
  'T..d...T...d..T',
  '.D...t...t...D.',
  '..D...d.d...D..',
  'd..D...d...D..d',
  '....D.....D....',
  '.t...t...t...t.',
  '..d...d.d...d..',
  'T..d...*...d..T',
  '..d...d.d...d..',
  '.t...t...t...t.',
  '....D.....D....',
  'd..D...d...D..d',
  '..D...d.d...D..',
  '.D...t...t...D.',
  'T..d...T...d..T',
].join('')

const values: Record<string, number> = { '?': 0 }
for (const [letters, value] of [
  ['AEILNORSTU', 1],
  ['DG', 2],
  ['BCMP', 3],
  ['FHVWY', 4],
  ['K', 5],
  ['JX', 8],
  ['QZ', 10],
] as const) {
  for (const letter of letters) {
    values[letter] = value
  }
}

export const tiles = (extra: Partial<TilesState> = {}): TilesState => ({
  layout,
  board: Array.from({ length: 225 }, () => null),
  values,
  players: [
    { seat: 'A', score: 0, count: 7 },
    { seat: 'B', score: 0, count: 7 },
  ],
  toMove: 'A',
  bag: 86,
  rack: ['C', 'A', 'T', 'Q', 'X', 'Z', '?'],
  lastPlay: null,
  outcome: null,
  refusal: null,
  ...extra,
})
