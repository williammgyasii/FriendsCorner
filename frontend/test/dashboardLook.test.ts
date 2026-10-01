import assert from 'node:assert/strict'
import { test } from 'vitest'
import { dashboardNav, sectionTitle } from '../src/home/dashboardLook.ts'

test('the dashboard has five sidebar sections', () => {
  assert.deepEqual(
    dashboardNav.map((item) => item.id),
    ['home', 'friends', 'settings', 'profile', 'billing'],
  )
})

test('each section has a title', () => {
  assert.equal(sectionTitle('home'), 'Home')
  assert.equal(sectionTitle('friends'), 'Friends')
  assert.equal(sectionTitle('billing'), 'Billing')
})
