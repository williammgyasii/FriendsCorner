// @vitest-environment jsdom
import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Provider } from 'react-redux'
import { afterEach, expect, test } from 'vitest'
import { MysteryScreen } from '../src/mystery/MysteryScreen.tsx'
import type { MysteryState } from '../src/mysteryLook.ts'
import { makeStore } from '../src/store/index.ts'
import { stateReceived } from '../src/store/roomSlice.ts'
import { mystery, writing } from './mysteryFixture.ts'

afterEach(cleanup)

const show = (section: MysteryState = mystery(), you: 'A' | 'B' = 'A') => {
  const sent: unknown[] = []
  const store = makeStore({ send: (message) => sent.push(message) }, { storage: null })
  store.dispatch(
    stateReceived({
      you,
      world: 'mystery',
      board: null,
      chess: null,
      mystery: section,
      players: { A: { x: 1, y: 1 }, B: { x: 2, y: 2 } },
      lobby: null,
    }),
  )
  const { container } = render(
    <Provider store={store}>
      <MysteryScreen />
    </Provider>,
  )
  return { sent, container }
}

test('while the case is written, the page says so', () => {
  show(writing())

  expect(screen.getByRole('status')).toHaveTextContent('Writing your case…')
})

test('a case that failed offers Try again, which asks for a new case', async () => {
  const { sent } = show({ ...writing(), phase: 'failed' })

  expect(screen.getByRole('status')).toHaveTextContent("Couldn't write a case")
  await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

  expect(sent).toEqual([{ type: 'mystery-rematch' }])
})

test('the case board shows the victim, the suspects, the places, and leads left', () => {
  show()

  expect(screen.getByRole('heading', { name: 'Death at the Lantern Inn' })).toBeInTheDocument()
  expect(screen.getByText(/Edmund Hale/)).toBeInTheDocument()
  expect(screen.getByRole('status')).toHaveTextContent('6 leads left')
  expect(screen.getAllByRole('article', { name: /./ })).toHaveLength(4)
  expect(screen.getByText('The bread oven was lit at eleven and never left alone.')).toBeInTheDocument()
})

test('tapping a closed lead opens it', async () => {
  const { sent } = show()

  await userEvent.click(screen.getByRole('button', { name: 'Kitchen, clue 2' }))

  expect(sent).toEqual([{ type: 'mystery-open', lead: 'c2' }])
})

test('with no leads left the closed leads are disabled', () => {
  show(mystery({ leadsLeft: 0 }))

  expect(screen.getByRole('button', { name: 'Kitchen, clue 2' })).toBeDisabled()
})

test('Accuse picks a suspect, and both picks are marked', async () => {
  const { sent } = show(mystery({ picks: { A: null, B: 's2' } }))

  const basil = within(screen.getByRole('article', { name: 'Basil Crane' }))
  expect(basil.getByText('Partner')).toBeInTheDocument()
  expect(screen.getByText('Pick the same suspect to accuse')).toBeInTheDocument()
  await userEvent.click(basil.getByRole('button', { name: 'Accuse Basil Crane' }))

  expect(sent).toEqual([{ type: 'mystery-accuse', suspect: 's2' }])
})

test('the reveal tells the story and New case asks for another', async () => {
  const { sent } = show(
    mystery({
      phase: 'revealed',
      picks: { A: 's3', B: 's3' },
      outcome: { killer: 's3', how: 'Pushed him.', why: 'To stop the sale.', story: 'Clara followed Edmund…', accused: 's3', right: true, score: 130, winner: null, timedOut: false },
    }),
  )

  const reveal = within(screen.getByRole('region', { name: 'Solved!' }))
  expect(reveal.getByText(/Clara Voss/)).toBeInTheDocument()
  expect(reveal.getByText('Clara followed Edmund…')).toBeInTheDocument()
  expect(reveal.getByText(/130/)).toBeInTheDocument()
  await userEvent.click(screen.getByRole('button', { name: 'New case' }))

  expect(sent).toEqual([{ type: 'mystery-rematch' }])
})

test('the case takes its mood', () => {
  const { container } = show(mystery({ mood: 'frost' }))

  expect(container.querySelector('.mystery-screen')).toHaveAttribute('data-mood', 'frost')
})

test('while the case is written, a line says what the writer is doing', () => {
  show(writing())

  expect(screen.getByText('Choosing a victim…')).toBeInTheDocument()
})

test('the final answer fills the screen with a countdown, and Hold on takes it back', async () => {
  const { sent } = show(mystery({ phase: 'accusing', picks: { A: 's3', B: 's3' }, locksInMs: 3000 }))

  const overlay = within(screen.getByRole('dialog', { name: 'Final answer: Clara Voss' }))
  expect(overlay.getByText('3')).toBeInTheDocument()
  await userEvent.click(overlay.getByRole('button', { name: 'Hold on' }))

  expect(sent).toEqual([{ type: 'mystery-withdraw' }])
})

test('a hard case shows the clock', () => {
  show(mystery({ level: 'hard', endsInMs: 125000 }))

  expect(screen.getByRole('timer')).toHaveTextContent('2:05')
})

test('an easy case has no clock', () => {
  show(mystery())

  expect(screen.queryByRole('timer')).not.toBeInTheDocument()
})

test('a player who is out sees why and cannot accuse', () => {
  show(mystery({ mode: 'race', picks: { A: 's1', B: null }, out: ['A'] }))

  expect(screen.getByText("You're out. Your partner is still looking.")).toBeInTheDocument()
  expect(screen.queryByRole('button', { name: /Accuse/ })).not.toBeInTheDocument()
})

test('each suspect has a round portrait drawn from their name', () => {
  show()

  const face = (name: string) => {
    const portrait = screen.getByRole('article', { name }).querySelector('.mystery-portrait')
    expect(portrait).toHaveStyle({ width: '56px', height: '56px', borderRadius: '50%' })
    const image = portrait?.querySelector('img')
    expect(image).toHaveAttribute('alt', '')
    expect(image?.closest('[aria-hidden="true"]')).toBeTruthy()
    return image?.getAttribute('src')
  }

  const ada = face('Ada Finch')
  expect(ada).toMatch(/^data:image\/svg\+xml/)
  expect(ada).not.toBe(face('Basil Crane'))
})

test('an opened lead says who found it', () => {
  show(mystery())

  expect(screen.getByText('You found this')).toBeInTheDocument()
  expect(screen.getByText('Partner found this')).toBeInTheDocument()
})
