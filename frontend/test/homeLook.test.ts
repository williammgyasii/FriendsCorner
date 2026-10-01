import assert from 'node:assert/strict'
import { test } from 'vitest'
import { homeLook, signedInScreen } from '../src/homeLook.ts'

test('the home shows the game name, the plan, and start a session', () => {
  const view = homeLook('Countess', 'table')
  const text = JSON.stringify(view)

  assert.equal(view.gameName, 'Countess')
  assert.equal(view.plan, 'Table')
  assert.equal(view.action, 'Start a session')
  assert.equal(view.failure, 'Could not open it. Try again')
  assert.equal(text.includes('Open a lobby'), false)
  assert.equal(text.includes('Manage plan'), false)
})

test('a home with no plan does not invent one', () => {
  const text = JSON.stringify(homeLook('Countess', null))

  assert.equal(text.includes('Countess'), true)
  assert.equal(text.includes('Start a session'), true)
  assert.equal(text.includes('Corner'), false)
  assert.equal(text.includes('Table'), false)
  assert.equal(text.includes('House'), false)
  assert.equal(text.includes('Open a lobby'), false)
})

test('checkout for a plan query comes before the home', () => {
  assert.deepEqual(signedInScreen('Countess', null, 'house'), { kind: 'checkout', plan: 'house' })
  assert.deepEqual(signedInScreen('Countess', 'table', null), homeLook('Countess', 'table'))
})
