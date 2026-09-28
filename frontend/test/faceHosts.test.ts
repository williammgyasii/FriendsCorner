// @vitest-environment jsdom
import assert from 'node:assert/strict'
import { beforeEach, test } from 'vitest'
import { faceHosts } from '../src/faceHosts.ts'

beforeEach(() => {
  document.body.innerHTML = `
    <div id="card-left"><div class="player-face" id="marks-you"></div></div>
    <div id="card-right"><div class="player-face" id="marks-them"></div></div>
    <div id="chess-card-left"><div class="player-face" id="chess-you"></div></div>
    <div id="chess-card-right"><div class="player-face" id="chess-them"></div></div>
    <section id="tiles-world">
      <div data-face="partner" id="tiles-them"></div>
      <div data-face="you" id="tiles-you"></div>
    </section>
  `
})

const ids = (hosts: ReturnType<typeof faceHosts>) => hosts?.map((host) => host.id) ?? null

test('Letter Tiles puts your face in your badge and theirs in your partner badge', () => {
  assert.deepEqual(ids(faceHosts('tiles', document.body)), ['tiles-you', 'tiles-them'])
})

test('Letter Tiles without a partner badge keeps the faces in the dock', () => {
  document.querySelector('#tiles-them')!.remove()

  assert.equal(faceHosts('tiles', document.body), null)
})

test('chess and tic-tac-toe use their player cards', () => {
  assert.deepEqual(ids(faceHosts('chess', document.body)), ['chess-you', 'chess-them'])
  assert.deepEqual(ids(faceHosts('tictactoe', document.body)), ['marks-you', 'marks-them'])
})

test('the lobby and the room floor have no face hosts', () => {
  assert.equal(faceHosts(null, document.body), null)
  assert.equal(faceHosts('room', document.body), null)
})
