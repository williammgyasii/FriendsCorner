import assert from 'node:assert/strict'
import { test } from 'vitest'
import { billingLook } from '../src/billingLook.ts'

test('a plan link before sign-in is the login view', () => {
  const view = billingLook(false, null, 'corner')

  assert.equal(view.kind, 'login')
  assert.deepEqual(view.kind === 'login' && view.fields, ['email', 'password'])
  assert.deepEqual(view.kind === 'login' && view.actions, ['Create account', 'Sign in'])
  assert.equal(JSON.stringify(view).includes('checkout'), false)
})

test('a plan link after sign-in requests checkout', () => {
  assert.deepEqual(billingLook(true, null, 'house'), { kind: 'checkout', plan: 'house' })
})

test('a stored plan shows its name and manage', () => {
  assert.deepEqual(billingLook(true, 'corner', null), {
    kind: 'lobby',
    action: 'Open a lobby',
    plan: 'Corner',
    manage: 'Manage plan',
  })
})

test('no plan is the lobby door without manage', () => {
  const view = billingLook(true, null, null)

  assert.equal(view.kind, 'lobby')
  assert.equal(view.kind === 'lobby' && view.manage, null)
  assert.equal(JSON.stringify(view).includes('Manage plan'), false)
})
