import assert from 'node:assert/strict'
import { test } from 'node:test'
import { placeFigures, wallHeight } from '../src/roomLook.ts'

const floorWidth = 480
const floorHeight = 320

test('the wall band is 64 pixels', () => {
  assert.equal(wallHeight, 64)
})

test('one occupied seat draws one figure below the wall', () => {
  const figures = placeFigures([{ seat: 'A', x: 240, y: 160 }])

  assert.equal(figures.length, 1)
  const [figure] = figures
  assert.ok(figure.parts.some((part) => part.kind === 'head'))
  assert.ok(figure.parts.some((part) => part.kind === 'body'))
  assert.ok(figure.top >= wallHeight)
})

test('two people on the same spot are drawn at least 24 pixels apart', () => {
  const figures = placeFigures([
    { seat: 'A', x: 240, y: 160 },
    { seat: 'B', x: 240, y: 160 },
  ])

  assert.equal(figures.length, 2)
  const [first, second] = figures
  const distance = Math.hypot(first.centerX - second.centerX, first.centerY - second.centerY)
  assert.ok(distance >= 24)
  assert.notEqual(first.bodyColor, second.bodyColor)
})

test('a lone figure stays on the server x', () => {
  const [figure] = placeFigures([{ seat: 'A', x: 400, y: 160 }])

  assert.equal(figure.centerX, 400)
})

test('a figure on the corner stays inside the floor', () => {
  const [figure] = placeFigures([{ seat: 'A', x: floorWidth, y: 0 }])

  assert.ok(figure.left >= 0)
  assert.ok(figure.top >= 0)
  assert.ok(figure.left + figure.width <= floorWidth)
  assert.ok(figure.top + figure.height <= floorHeight)
  assert.ok(figure.width > 0)
  assert.ok(figure.height > 0)
})
