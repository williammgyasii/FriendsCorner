import assert from 'node:assert/strict'
import { test } from 'vitest'
import { describeMystery, suspectPortrait } from '../src/mysteryLook.ts'
import { mystery, writing } from './mysteryFixture.ts'

test('while the case is being written, only the status shows', () => {
  const look = describeMystery(writing(), 'A')

  assert.equal(look.status, 'Writing your case…')
  assert.equal(look.writing, true)
  assert.deepEqual(look.suspects, [])
  assert.deepEqual(look.actions, { tryAgain: false, newCase: false })
})

test('a case that could not be written offers Try again', () => {
  const look = describeMystery({ ...writing(), phase: 'failed' }, 'B')

  assert.equal(look.status, "Couldn't write a case")
  assert.equal(look.writing, false)
  assert.deepEqual(look.actions, { tryAgain: true, newCase: false })
})

test('the case board groups clues by place and statements by suspect, with leads left', () => {
  const look = describeMystery(mystery(), 'A')

  assert.equal(look.status, '6 leads left')
  assert.equal(look.title, 'Death at the Lantern Inn')
  assert.equal(look.victim?.name, 'Edmund Hale')
  assert.deepEqual(
    look.places.map((place) => [place.name, place.clues.map((clue) => clue.id)]),
    [
      ['Kitchen', ['c1', 'c2']],
      ['Cellar', ['c3', 'c4']],
      ['Stables', ['c5', 'c6']],
    ],
  )
  assert.deepEqual(look.suspects[1].statements, [
    { id: 't3', label: 'Basil Crane, statement 1', text: null, canOpen: true, by: null },
    { id: 't4', label: 'Basil Crane, statement 2', text: 'I heard footsteps on the cellar stairs.', canOpen: false, by: 'partner' },
  ])
  assert.deepEqual(look.places[0].clues[0], {
    id: 'c1',
    label: 'Kitchen, clue 1',
    text: 'The bread oven was lit at eleven and never left alone.',
    canOpen: false,
    by: 'you',
  })
})

test('one lead left is singular, and at 0 closed leads cannot be opened', () => {
  assert.equal(describeMystery(mystery({ leadsLeft: 1 }), 'A').status, '1 lead left')

  const spent = describeMystery(mystery({ leadsLeft: 0 }), 'A')

  assert.equal(spent.status, 'No leads left')
  assert.equal(spent.places[0].clues[1].canOpen, false)
  assert.equal(spent.suspects[0].statements[0].canOpen, false)
})

test('your pick and your partner’s pick are marked separately', () => {
  const look = describeMystery(mystery({ picks: { A: 's2', B: 's3' } }), 'B')

  assert.deepEqual(
    look.suspects.map((suspect) => [suspect.id, suspect.yours, suspect.partners]),
    [
      ['s1', false, false],
      ['s2', false, true],
      ['s3', true, false],
      ['s4', false, false],
    ],
  )
  assert.ok(look.suspects.every((suspect) => suspect.canAccuse))
})

test('while the picks differ the hint says to pick the same suspect', () => {
  assert.equal(describeMystery(mystery(), 'A').hint, 'Open leads, then pick the killer together')
  assert.equal(describeMystery(mystery({ picks: { A: 's2', B: null } }), 'A').hint, 'Pick the same suspect to accuse')
  assert.equal(describeMystery(mystery({ picks: { A: 's2', B: 's3' } }), 'A').hint, 'Pick the same suspect to accuse')
})

test('the reveal names the killer, how, why, the story, and the score', () => {
  const look = describeMystery(
    mystery({
      phase: 'revealed',
      picks: { A: 's3', B: 's3' },
      outcome: { killer: 's3', how: 'Pushed him.', why: 'To stop the sale.', story: 'Clara followed Edmund…', accused: 's3', right: true, score: 130, winner: null, timedOut: false },
    }),
    'A',
  )

  assert.equal(look.status, 'Solved!')
  assert.deepEqual(look.reveal, {
    headline: 'Solved!',
    killer: 'Clara Voss',
    how: 'Pushed him.',
    why: 'To stop the sale.',
    story: 'Clara followed Edmund…',
    score: 130,
    right: true,
  })
  assert.ok(look.suspects.every((suspect) => !suspect.canAccuse))
  assert.ok(look.places.every((place) => place.clues.every((clue) => !clue.canOpen)))
  assert.deepEqual(look.actions, { tryAgain: false, newCase: true })
})

test('a wrong accusation is not this time, with no score', () => {
  const look = describeMystery(
    mystery({
      phase: 'revealed',
      picks: { A: 's2', B: 's2' },
      outcome: { killer: 's3', how: 'h', why: 'w', story: 's', accused: 's2', right: false, score: 0, winner: null, timedOut: false },
    }),
    'B',
  )

  assert.equal(look.status, 'Not this time')
  assert.equal(look.reveal?.headline, 'Not this time')
  assert.equal(look.reveal?.killer, 'Clara Voss')
  assert.equal(look.reveal?.score, 0)
})

const outcome = (extra: Partial<NonNullable<ReturnType<typeof mystery>['outcome']>> = {}) => ({
  killer: 's3',
  how: 'h',
  why: 'w',
  story: 's',
  accused: 's3',
  right: true,
  score: 180,
  winner: null,
  timedOut: false,
  ...extra,
})

test('the mood colours the case, and a case still being written reads as smoke', () => {
  assert.equal(describeMystery(mystery({ mood: 'frost' }), 'A').mood, 'frost')
  assert.equal(describeMystery(writing(), 'A').mood, 'smoke')
})

test('the tags name the level and the mode', () => {
  assert.deepEqual(describeMystery(mystery(), 'A').tags, ['Easy', 'Together'])
  assert.deepEqual(describeMystery(mystery({ level: 'hard', mode: 'race' }), 'A').tags, ['Hard', 'Race'])
})

test('a hard case shows the clock as m:ss and warns under a minute', () => {
  assert.equal(describeMystery(mystery(), 'A').clock, null)
  assert.deepEqual(describeMystery(mystery({ level: 'hard', endsInMs: 125000 }), 'A').clock, { text: '2:05', warn: false })
  assert.deepEqual(describeMystery(mystery({ level: 'hard', endsInMs: 59001 }), 'A').clock, { text: '1:00', warn: true })
  assert.deepEqual(describeMystery(mystery({ level: 'hard', endsInMs: 0 }), 'A').clock, { text: '0:00', warn: true })
})

test('the clock stops showing once the case is revealed', () => {
  const look = describeMystery(mystery({ level: 'hard', endsInMs: 30000, phase: 'revealed', outcome: outcome() }), 'A')

  assert.equal(look.clock, null)
})

test('while accusing, the final answer names the suspect and counts down whole seconds', () => {
  const look = describeMystery(mystery({ phase: 'accusing', picks: { A: 's3', B: 's3' }, locksInMs: 2100 }), 'A')

  assert.deepEqual(look.finalAnswer, { name: 'Clara Voss', seconds: 3 })
  assert.equal(look.status, 'Final answer…')
  assert.ok(look.suspects.every((suspect) => !suspect.canAccuse))
  assert.equal(describeMystery(mystery(), 'A').finalAnswer, null)
})

test('opened leads say who opened them', () => {
  const asB = describeMystery(mystery(), 'B')

  assert.equal(asB.places[0].clues[0].by, 'partner')
  assert.equal(asB.suspects[1].statements[1].by, 'you')
  assert.equal(asB.places[0].clues[1].by, null)
})

test('a race says you get one guess', () => {
  const look = describeMystery(mystery({ mode: 'race' }), 'A')

  assert.equal(look.hint, 'First to name the killer wins. You get one guess.')
  assert.ok(look.suspects.every((suspect) => suspect.canAccuse))
})

test('a player who guessed wrong in a race is out and can do nothing more', () => {
  const look = describeMystery(mystery({ mode: 'race', picks: { A: 's1', B: null }, out: ['A'] }), 'A')

  assert.equal(look.out, true)
  assert.equal(look.hint, "You're out. Your partner is still looking.")
  assert.ok(look.suspects.every((suspect) => !suspect.canAccuse))
  assert.ok(look.places.every((place) => place.clues.every((clue) => !clue.canOpen)))
  assert.equal(describeMystery(mystery({ mode: 'race', out: ['A'] }), 'B').out, false)
})

test('the race reveal names the winner from your side', () => {
  const won = mystery({ mode: 'race', phase: 'revealed', outcome: outcome({ winner: 'B' }) })

  assert.equal(describeMystery(won, 'A').reveal?.headline, 'Your partner wins')
  assert.equal(describeMystery(won, 'B').reveal?.headline, 'You win!')
  assert.equal(
    describeMystery(mystery({ mode: 'race', phase: 'revealed', outcome: outcome({ right: false, score: 0 }) }), 'A').reveal?.headline,
    'Nobody got it',
  )
})

test('a name always draws the same portrait, and two names draw different ones', () => {
  const ada = suspectPortrait('Ada Finch')

  assert.match(ada, /^data:image\/svg\+xml/)
  assert.equal(suspectPortrait('Ada Finch'), ada)
  assert.notEqual(suspectPortrait('Basil Crane'), ada)
})

test('a case that ran out of time says so', () => {
  const look = describeMystery(
    mystery({ level: 'hard', phase: 'revealed', outcome: outcome({ accused: null, right: false, score: 0, timedOut: true }) }),
    'A',
  )

  assert.equal(look.status, 'Out of time')
  assert.equal(look.reveal?.headline, 'Out of time')
})
