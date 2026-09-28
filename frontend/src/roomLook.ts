export const wallHeight = 64

const floorWidth = 480
const floorHeight = 320
const figureWidth = 36
const figureHeight = 52
const minSeparation = 24

const bodyColors = {
  A: '#1c1915',
  B: '#8a5a44',
  C: '#b7791f',
  D: '#2f7d5b',
} as const

export type SeatName = 'A' | 'B' | 'C' | 'D'

export type FigurePart = {
  kind: 'head' | 'body'
  color: string
}

export type PlacedFigure = {
  seat: SeatName
  centerX: number
  centerY: number
  left: number
  top: number
  width: number
  height: number
  bodyColor: string
  parts: FigurePart[]
}

type SeatPoint = { seat: SeatName; x: number; y: number }

export function placeFigures(seats: SeatPoint[]): PlacedFigure[] {
  const screen = seats.map((seat) => ({
    seat: seat.seat,
    x: seat.x,
    y: floorHeight - seat.y,
  }))

  // Pushing one pair apart can crowd another, so settle over a few passes.
  for (let pass = 0; pass < 24; pass += 1) {
    for (const [index, one] of screen.entries()) {
      for (const [offset, other] of screen.slice(index + 1).entries()) {
        separate(one, other, (index * screen.length + offset) * 2.4)
      }
    }
  }

  return screen.map((point) => toFigure(point.seat, point.x, point.y))
}

function separate(seatA: { x: number; y: number }, seatB: { x: number; y: number }, fallbackAngle: number) {
  let dx = seatB.x - seatA.x
  let dy = seatB.y - seatA.y
  const distance = Math.hypot(dx, dy)
  if (distance >= minSeparation) {
    return
  }

  if (distance === 0) {
    dx = Math.cos(fallbackAngle)
    dy = Math.sin(fallbackAngle)
  } else {
    dx /= distance
    dy /= distance
  }

  const push = (minSeparation - distance) / 2
  seatA.x -= dx * push
  seatA.y -= dy * push
  seatB.x += dx * push
  seatB.y += dy * push
}

function toFigure(seat: SeatName, x: number, y: number): PlacedFigure {
  const halfWidth = figureWidth / 2
  const halfHeight = figureHeight / 2
  const centerX = clamp(x, halfWidth, floorWidth - halfWidth)
  const centerY = clamp(y, halfHeight, floorHeight - halfHeight)
  const bodyColor = bodyColors[seat]

  return {
    seat,
    centerX,
    centerY,
    left: centerX - halfWidth,
    top: centerY - halfHeight,
    width: figureWidth,
    height: figureHeight,
    bodyColor,
    parts: [
      { kind: 'head', color: '#f3d7c4' },
      { kind: 'body', color: bodyColor },
    ],
  }
}

function clamp(value: number, min: number, max: number) {
  return Math.min(max, Math.max(min, value))
}
