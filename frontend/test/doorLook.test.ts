import assert from 'node:assert/strict'
import { test } from 'vitest'
import { doorLook } from '../src/doorLook.ts'

test('login is email and password, with no Google action', () => {
  const view = doorLook('/login', '', false)

  assert.equal(view.kind, 'login')
  assert.deepEqual(view.kind === 'login' && view.fields, ['email', 'password'])
  assert.equal(view.kind === 'login' && view.action, 'Sign in')
  assert.deepEqual(view.kind === 'login' && view.fonts, { title: 'Bungee', form: 'Fredoka' })
  assert.equal(JSON.stringify(view).includes('Google'), false)
  assert.equal(JSON.stringify(view).includes('name'), false)
})

test('register asks for a name and a game name', () => {
  const view = doorLook('/register', '', false)

  assert.equal(view.kind, 'register')
  assert.deepEqual(view.kind === 'register' && view.fields, ['name', 'gameName', 'email', 'password'])
  assert.equal(view.kind === 'register' && view.action, 'Create account')
  assert.equal(JSON.stringify(view).includes('Google'), false)
})

test('the front door with no session goes to login', () => {
  assert.deepEqual(doorLook('/', '', false), { kind: 'redirect', to: '/login' })
  assert.deepEqual(doorLook('/', '?plan=corner', false), { kind: 'redirect', to: '/login?plan=corner' })
})

test('a room link with no session keeps the room on the register page', () => {
  assert.deepEqual(doorLook('/', '?room=abc', false), { kind: 'redirect', to: '/register?room=abc' })
  assert.equal(doorLook('/register', '?room=abc', false).kind, 'register')
  assert.equal(doorLook('/login', '?room=abc', false).kind, 'login')
})

test('a session with a room enters it, and a session without one is the home', () => {
  assert.deepEqual(doorLook('/login', '?room=abc', true), { kind: 'enter', room: 'abc' })
  assert.deepEqual(doorLook('/', '', true), { kind: 'home' })
  assert.equal(JSON.stringify(doorLook('/', '', true)).includes('Open a lobby'), false)
})
