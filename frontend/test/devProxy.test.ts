import assert from 'node:assert/strict'
import { test } from 'vitest'
import config from '../vite.config.ts'

test('the dev server forwards rooms and account to the API', () => {
  const proxy = config.server?.proxy

  assert.equal(proxy?.['/rooms'], 'http://localhost:5250')
  assert.equal(proxy?.['/account'], 'http://localhost:5250')
  assert.equal(proxy?.['/billing'], 'http://localhost:5250')
})
