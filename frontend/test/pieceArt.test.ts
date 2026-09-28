import assert from 'node:assert/strict'
import { test } from 'vitest'
import darkKnight from '../src/assets/pieces/nd.svg?url'
import lightKnight from '../src/assets/pieces/nl.svg?url'
import { pieceImage } from '../src/pieceArt.ts'

test('every piece has its own drawing', () => {
  const pieces = ['K', 'Q', 'R', 'B', 'N', 'P', 'k', 'q', 'r', 'b', 'n', 'p']
  const images = pieces.map(pieceImage)

  images.forEach((image, index) => assert.ok(image, pieces[index]))
  assert.equal(new Set(images).size, pieces.length)
})

test('white pieces use the light drawing and black the dark one', () => {
  assert.equal(pieceImage('N'), lightKnight)
  assert.equal(pieceImage('n'), darkKnight)
})
