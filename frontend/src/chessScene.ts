export type BoardPoint = { x: number; z: number }
export type Vec3 = { x: number; y: number; z: number }
export type CameraFit = { position: Vec3; target: Vec3; fov: number }

export type Slide = { from: string; to: string }
export type MovePlan = { slides: Slide[]; lifts: string[]; drops: { square: string; piece: string }[] }

const files = 'abcdefgh'
const half = 4
const pieceTop = 2.5
const fov = 40
const margin = 0.92
const aimHeight = 0.8

// White sits at +z. The board never turns; the camera walks to your side.
export function squareToWorld(square: string): BoardPoint {
  const file = files.indexOf(square[0])
  const rank = Number(square[1]) - 1
  return { x: file - 3.5, z: 3.5 - rank }
}

export function worldToSquare(point: BoardPoint): string | null {
  if (Math.abs(point.x) > half || Math.abs(point.z) > half) {
    return null
  }

  const file = Math.min(7, Math.floor(point.x + half))
  const rank = Math.min(7, Math.floor(half - point.z))
  return `${files[file]}${rank + 1}`
}

export function fitCamera(aspect: number, youAreWhite: boolean): CameraFit {
  const side = youAreWhite ? 1 : -1
  const pitch = pitchFor(aspect)
  const at = (distance: number): CameraFit => ({
    position: { x: 0, y: aimHeight + distance * Math.sin(pitch), z: side * distance * Math.cos(pitch) },
    target: { x: 0, y: aimHeight, z: 0 },
    fov,
  })

  let near = 1
  let far = 200
  for (let step = 0; step < 40; step++) {
    const middle = (near + far) / 2
    if (reach(at(middle), aspect) > margin) {
      near = middle
    } else {
      far = middle
    }
  }

  return at(far)
}

// Tall screens are width-bound, so a steeper view makes the board taller on screen.
function pitchFor(aspect: number): number {
  const degrees = Math.min(66, Math.max(52, 52 + (1 - aspect) * 26))
  return (degrees * Math.PI) / 180
}

// Pairs each piece that left a square with the nearest one of its kind that
// arrived, so castling, captures, and a rematch all read as pieces moving.
export function planMoves(before: Map<string, string>, after: Map<string, string>): MovePlan {
  const left = [...before].filter(([square, piece]) => after.get(square) !== piece)
  const arrived = [...after].filter(([square, piece]) => before.get(square) !== piece)

  const pairs = left
    .flatMap(([from, piece]) =>
      arrived.filter(([, other]) => other === piece).map(([to]) => ({ from, to, distance: gap(from, to) })),
    )
    .sort((a, b) => a.distance - b.distance)

  const slides: Slide[] = []
  const used = new Set<string>()
  for (const { from, to } of pairs) {
    if (!used.has(`from:${from}`) && !used.has(`to:${to}`)) {
      used.add(`from:${from}`)
      used.add(`to:${to}`)
      slides.push({ from, to })
    }
  }

  return {
    slides,
    lifts: left.filter(([square]) => !used.has(`from:${square}`)).map(([square]) => square),
    drops: arrived.filter(([square]) => !used.has(`to:${square}`)).map(([square, piece]) => ({ square, piece })),
  }
}

function gap(from: string, to: string): number {
  const a = squareToWorld(from)
  const b = squareToWorld(to)
  return Math.hypot(a.x - b.x, a.z - b.z)
}

// How far toward the screen edge the board lands (1 is the edge), counting the
// tops of pieces standing on the corner squares.
function reach(camera: CameraFit, aspect: number): number {
  const { position, target } = camera
  const forward = unit({ x: target.x - position.x, y: target.y - position.y, z: target.z - position.z })
  const right = unit({ x: -forward.z, y: 0, z: forward.x })
  const up = {
    x: right.y * forward.z - right.z * forward.y,
    y: right.z * forward.x - right.x * forward.z,
    z: right.x * forward.y - right.y * forward.x,
  }
  const tan = Math.tan((camera.fov * Math.PI) / 360)

  const corner = half - 0.5
  const points: Vec3[] = []
  for (const sx of [-1, 1]) {
    for (const sz of [-1, 1]) {
      points.push({ x: sx * half, y: 0, z: sz * half }, { x: sx * corner, y: pieceTop, z: sz * corner })
    }
  }

  let farthest = 0
  for (const point of points) {
    const offset = { x: point.x - position.x, y: point.y - position.y, z: point.z - position.z }
    const depth = dot(offset, forward)
    const screenX = dot(offset, right) / (depth * tan * aspect)
    const screenY = dot(offset, up) / (depth * tan)
    farthest = Math.max(farthest, Math.abs(screenX), Math.abs(screenY))
  }

  return farthest
}

const dot = (a: Vec3, b: Vec3) => a.x * b.x + a.y * b.y + a.z * b.z

function unit(a: Vec3): Vec3 {
  const length = Math.hypot(a.x, a.y, a.z)
  return { x: a.x / length, y: a.y / length, z: a.z / length }
}
