import assert from 'node:assert/strict'
import { test } from 'vitest'
import { hostPlanOffer, hostPlanOffers } from '../src/billingPlansLook.ts'

test('each host plan names the price and the game allowances', () => {
  const plans = hostPlanOffers()

  assert.deepEqual(
    plans.map((plan) => [plan.id, plan.price, plan.perks.filter((line) => line.includes('nights'))]),
    [
      ['corner', 15, ['Tic-tac-toe: 30 nights/mo', 'Chess: 8 nights/mo', 'Letter Tiles: 8 nights/mo', 'Murder Mystery: 4 nights/mo']],
      ['table', 20, ['Tic-tac-toe: 90 nights/mo', 'Chess: 24 nights/mo', 'Letter Tiles: 24 nights/mo', 'Murder Mystery: 12 nights/mo']],
      ['house', 50, ['Tic-tac-toe: unlimited nights', 'Chess: unlimited nights', 'Letter Tiles: unlimited nights', 'Murder Mystery: unlimited nights']],
    ],
  )
  assert.equal(plans.every((plan) => plan.perks[0] === 'Friends on your link play free'), true)
})

test('plans use different colors', () => {
  const colors = hostPlanOffers().map((plan) => plan.color)
  assert.equal(new Set(colors).size, 3)
  assert.notEqual(hostPlanOffer('corner').color, hostPlanOffer('table').color)
})
