import assert from 'node:assert/strict'
import { test } from 'vitest'
import { piecesFrom } from '../src/chessLook.ts'
import { fitCamera, planMoves, squareToWorld, worldToSquare, type CameraFit } from '../src/chessScene.ts'

type Vec = { x: number; y: number; z: number }

const sub = (a: Vec, b: Vec): Vec => ({ x: a.x - b.x, y: a.y - b.y, z: a.z - b.z })
const dot = (a: Vec, b: Vec) => a.x * b.x + a.y * b.y + a.z * b.z
const cross = (a: Vec, b: Vec): Vec => ({
  x: a.y * b.z - a.z * b.y,
  y: a.z * b.x - a.x * b.z,
  z: a.x * b.y - a.y * b.x,
})
const unit = (a: Vec): Vec => {
  const length = Math.hypot(a.x, a.y, a.z)
  return { x: a.x / length, y: a.y / length, z: a.z / length }
}

// A pinhole camera, the same model a perspective renderer uses.
function onScreen(camera: CameraFit, aspect: number, point: Vec) {
  const forward = unit(sub(camera.target, camera.position))
  const right = unit(cross(forward, { x: 0, y: 1, z: 0 }))
  const up = cross(right, forward)
  const offset = sub(point, camera.position)
  const depth = dot(offset, forward)
  const halfHeight = depth * Math.tan((camera.fov * Math.PI) / 360)
  return {
    x: dot(offset, right) / (halfHeight * aspect),
    y: dot(offset, up) / halfHeight,
  }
}

const corners: Vec[] = [
  { x: -4, y: 0, z: -4 },
  { x: 4, y: 0, z: -4 },
  { x: -4, y: 0, z: 4 },
  { x: 4, y: 0, z: 4 },
]

const screens = [
  { name: 'phone upright', aspect: 9 / 19.5 },
  { name: 'square window', aspect: 1 },
  { name: 'wide monitor', aspect: 21 / 9 },
]

test('squares sit on a unit grid with a1 at white’s near-left corner', () => {
  assert.deepEqual(squareToWorld('a1'), { x: -3.5, z: 3.5 })
  assert.deepEqual(squareToWorld('h8'), { x: 3.5, z: -3.5 })
  assert.deepEqual(squareToWorld('e4'), { x: 0.5, z: 0.5 })
})

test('every square maps back to itself', () => {
  for (const file of 'abcdefgh') {
    for (const rank of '12345678') {
      const square = `${file}${rank}`
      assert.equal(worldToSquare(squareToWorld(square)), square)
    }
  }
})

test('a tap anywhere inside a square picks that square', () => {
  assert.equal(worldToSquare({ x: 0.05, z: 0.95 }), 'e4')
  assert.equal(worldToSquare({ x: 0.95, z: 0.05 }), 'e4')
})

test('a tap off the board picks nothing', () => {
  assert.equal(worldToSquare({ x: 4.1, z: 0 }), null)
  assert.equal(worldToSquare({ x: 0, z: -4.01 }), null)
})

test('the camera sits above your own side, looking at the board', () => {
  const white = fitCamera(1, true)
  const black = fitCamera(1, false)

  assert.ok(white.position.z > 0, 'white looks from behind rank 1')
  assert.ok(black.position.z < 0, 'black looks from behind rank 8')
  assert.ok(white.position.y > 0 && black.position.y > 0, 'both look down on the board')
  for (const { target } of [white, black]) {
    assert.equal(target.x, 0)
    assert.equal(target.z, 0)
    assert.ok(target.y >= 0 && target.y < 2.5, 'aims just above the middle of the board')
  }
})

test('a tall phone looks down more steeply than a wide monitor, so the board fills the height', () => {
  const steepness = (camera: CameraFit) => {
    const drop = camera.position.y - camera.target.y
    return drop / Math.hypot(camera.position.z - camera.target.z, drop)
  }

  assert.ok(steepness(fitCamera(9 / 19.5, true)) - steepness(fitCamera(21 / 9, true)) > 0.05)
})

for (const { name, aspect } of screens) {
  for (const youAreWhite of [true, false]) {
    test(`the whole board is in view on a ${name} as ${youAreWhite ? 'white' : 'black'}`, () => {
      const camera = fitCamera(aspect, youAreWhite)
      for (const corner of corners) {
        const point = onScreen(camera, aspect, corner)
        assert.ok(Math.abs(point.x) <= 1 && Math.abs(point.y) <= 1, `corner ${JSON.stringify(corner)} is cut off`)
      }
    })

    test(`a king on the far rank keeps its head in view on a ${name} as ${youAreWhite ? 'white' : 'black'}`, () => {
      const camera = fitCamera(aspect, youAreWhite)
      const farZ = youAreWhite ? -3.5 : 3.5
      for (const x of [-3.5, 3.5]) {
        const point = onScreen(camera, aspect, { x, y: 2.5, z: farZ })
        assert.ok(Math.abs(point.x) <= 1 && Math.abs(point.y) <= 1, `a far piece at x=${x} is cut off`)
      }
    })

    test(`the board fills the ${name} as ${youAreWhite ? 'white' : 'black'} instead of shrinking into the distance`, () => {
      const camera = fitCamera(aspect, youAreWhite)
      const reach = Math.max(
        ...corners.map((corner) => {
          const point = onScreen(camera, aspect, corner)
          return Math.max(Math.abs(point.x), Math.abs(point.y))
        }),
      )
      assert.ok(reach >= 0.8, `the board only reaches ${reach.toFixed(2)} of the screen`)
    })
  }
}

const start = piecesFrom('rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1')

test('nothing changes when the position is the same', () => {
  assert.deepEqual(planMoves(start, start), { slides: [], lifts: [], drops: [] })
})

test('a quiet move slides one piece', () => {
  const after = piecesFrom('rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1')

  assert.deepEqual(planMoves(start, after), { slides: [{ from: 'e2', to: 'e4' }], lifts: [], drops: [] })
})

test('a capture lifts the taken piece and slides the taker onto its square', () => {
  const before = piecesFrom('rnbqkbnr/ppp1pppp/8/3p4/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 2')
  const after = piecesFrom('rnbqkbnr/ppp1pppp/8/3P4/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 2')

  assert.deepEqual(planMoves(before, after), { slides: [{ from: 'e4', to: 'd5' }], lifts: ['d5'], drops: [] })
})

test('castling slides the king and the rook', () => {
  const before = piecesFrom('r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1')
  const after = piecesFrom('r3k2r/8/8/8/8/8/8/R4RK1 b kq - 1 1')
  const plan = planMoves(before, after)

  assert.deepEqual(
    [...plan.slides].sort((a, b) => a.from.localeCompare(b.from)),
    [
      { from: 'e1', to: 'g1' },
      { from: 'h1', to: 'f1' },
    ],
  )
  assert.deepEqual(plan.lifts, [])
  assert.deepEqual(plan.drops, [])
})

test('a promotion lifts the pawn and drops the new piece', () => {
  const before = piecesFrom('4k3/P7/8/8/8/8/8/4K3 w - - 0 1')
  const after = piecesFrom('Q3k3/8/8/8/8/8/8/4K3 b - - 0 1')

  assert.deepEqual(planMoves(before, after), { slides: [], lifts: ['a7'], drops: [{ square: 'a8', piece: 'Q' }] })
})

test('a rematch sends each piece to the nearest home square', () => {
  const before = piecesFrom('4k3/8/8/8/8/2N2N2/8/4K3 w - - 0 1')
  const after = piecesFrom('4k3/8/8/8/8/8/8/1N2K1N1 w - - 0 1')
  const plan = planMoves(before, after)

  assert.deepEqual(
    [...plan.slides].sort((a, b) => a.from.localeCompare(b.from)),
    [
      { from: 'c3', to: 'b1' },
      { from: 'f3', to: 'g1' },
    ],
  )
})
