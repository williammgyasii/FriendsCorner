import {
  ACESFilmicToneMapping,
  CircleGeometry,
  DirectionalLight,
  Group,
  Mesh,
  MeshBasicMaterial,
  Object3D,
  PCFSoftShadowMap,
  PerspectiveCamera,
  PlaneGeometry,
  PMREMGenerator,
  Raycaster,
  RingGeometry,
  Scene,
  Vector2,
  Vector3,
  WebGLRenderer,
} from 'three'
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js'
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js'
import { MeshoptDecoder } from 'three/addons/libs/meshopt_decoder.module.js'
import { fitCamera, planMoves, squareToWorld, worldToSquare } from './chessScene.ts'

export type ChessPicture = {
  pieces: Map<string, string>
  youAreWhite: boolean
  selected: string | null
  targets: string[]
  lastMove: { from: string; to: string } | null
  check: string | null
}

export type ChessView = {
  show(picture: ChessPicture): void
  dispose(): void
}

// Model units per square in "A Beautiful Game"; its white side faces -z.
const modelScale = 16
const templates: Record<string, string> = {
  K: 'King_W',
  Q: 'Queen_W',
  R: 'Castle_W1',
  B: 'Bishop_W1',
  N: 'Knight_W1',
  P: 'Pawn_Body_W1',
  k: 'King_B',
  q: 'Queen_B',
  r: 'Castle_B1',
  b: 'Bishop_B1',
  n: 'Knight_B1',
  p: 'Pawn_Body_B1',
}
const slideMs = 380
const liftHeight = 0.35

type Slide = { holder: Object3D; from: Vector3; to: Vector3; started: number }

export async function mountChessView(
  host: HTMLElement,
  onSquare: (square: string) => void,
  onLost: () => void,
): Promise<ChessView> {
  const coarse = window.matchMedia('(pointer: coarse)').matches
  const renderer = new WebGLRenderer({ antialias: true, alpha: true, powerPreference: 'high-performance' })
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, coarse ? 1.5 : 2))
  renderer.toneMapping = ACESFilmicToneMapping
  renderer.toneMappingExposure = 1.05
  renderer.shadowMap.enabled = true
  renderer.shadowMap.type = PCFSoftShadowMap

  const scene = new Scene()
  const pmrem = new PMREMGenerator(renderer)
  scene.environment = pmrem.fromScene(new RoomEnvironment(), 0.04).texture
  pmrem.dispose()

  const sun = new DirectionalLight(0xfff1dc, 2.4)
  sun.position.set(5, 14, 7)
  sun.castShadow = true
  sun.shadow.mapSize.setScalar(coarse ? 1024 : 2048)
  sun.shadow.camera.left = -7
  sun.shadow.camera.right = 7
  sun.shadow.camera.top = 7
  sun.shadow.camera.bottom = -7
  sun.shadow.camera.near = 4
  sun.shadow.camera.far = 30
  sun.shadow.bias = -0.0004
  sun.shadow.normalBias = 0.02
  scene.add(sun)

  const loader = new GLTFLoader().setMeshoptDecoder(MeshoptDecoder)
  const gltf = await loader.loadAsync('/chess/chess.glb')
  const model = gltf.scene

  const kinds = new Map<string, Object3D>()
  for (const [piece, name] of Object.entries(templates)) {
    const node = model.getObjectByName(name)
    if (!node) {
      throw new Error(`The chess model has no ${name}.`)
    }
    kinds.set(piece, node)
  }
  for (const child of [...model.children]) {
    if (child.name !== 'Chessboard') {
      model.remove(child)
    }
  }
  model.traverse((node) => {
    if (node instanceof Mesh) {
      node.receiveShadow = true
      node.castShadow = true
    }
  })

  const board = new Group()
  board.rotation.y = Math.PI
  board.scale.setScalar(modelScale)
  board.add(model)
  scene.add(board)

  const surface = surfaceHeight(board)
  const overlays = new Group()
  overlays.position.y = surface + 0.004
  scene.add(overlays)

  const camera = new PerspectiveCamera(40, 1, 0.1, 200)
  const canvas = renderer.domElement
  canvas.className = 'chess-canvas'
  canvas.setAttribute('role', 'img')
  canvas.setAttribute('aria-label', 'Chess board')
  host.prepend(canvas)

  const onScreen = new Map<string, Object3D>()
  let placed = new Map<string, string>()
  let picture: ChessPicture | null = null
  let slides: Slide[] = []
  let frame = 0
  let youAreWhite = true
  let aspect = 1

  const aim = () => {
    const fit = fitCamera(aspect, youAreWhite)
    camera.aspect = aspect
    camera.fov = fit.fov
    camera.position.set(fit.position.x, fit.position.y, fit.position.z)
    camera.lookAt(fit.target.x, fit.target.y, fit.target.z)
    camera.updateProjectionMatrix()
  }

  const makePiece = (piece: string, square: string) => {
    const template = kinds.get(piece)!
    const copy = template.clone(true)
    copy.position.set(0, template.position.y, 0)
    const flip = new Group()
    flip.rotation.y = Math.PI
    flip.scale.setScalar(modelScale)
    flip.add(copy)
    const lifter = new Group()
    lifter.add(flip)
    const holder = new Group()
    holder.add(lifter)
    const spot = squareToWorld(square)
    holder.position.set(spot.x, 0, spot.z)
    holder.userData.square = square
    scene.add(holder)
    return holder
  }

  const place = (next: Map<string, string>) => {
    const plan = planMoves(placed, next)
    for (const square of plan.lifts) {
      scene.remove(onScreen.get(square)!)
      onScreen.delete(square)
    }

    const moving = plan.slides.map(({ from, to }) => ({ holder: onScreen.get(from)!, from, to }))
    for (const { from } of moving) {
      onScreen.delete(from)
    }
    const now = performance.now()
    for (const { holder, from, to } of moving) {
      const start = squareToWorld(from)
      const end = squareToWorld(to)
      holder.userData.square = to
      onScreen.set(to, holder)
      slides.push({ holder, from: new Vector3(start.x, 0, start.z), to: new Vector3(end.x, 0, end.z), started: now })
    }

    for (const { square, piece } of plan.drops) {
      onScreen.set(square, makePiece(piece, square))
    }
    placed = new Map(next)
  }

  const mark = (next: ChessPicture) => {
    overlays.clear()
    const tint = (square: string, color: number, opacity: number) => {
      const spot = squareToWorld(square)
      const tile = new Mesh(squareShape, tintLook(color, opacity))
      tile.position.set(spot.x, 0, spot.z)
      overlays.add(tile)
    }

    if (next.lastMove) {
      tint(next.lastMove.from, 0xf5d76e, 0.28)
      tint(next.lastMove.to, 0xf5d76e, 0.36)
    }
    if (next.check) {
      tint(next.check, 0xff3b30, 0.5)
    }
    if (next.selected) {
      tint(next.selected, 0x4cd964, 0.45)
    }
    for (const target of next.targets) {
      const spot = squareToWorld(target)
      const taking = next.pieces.has(target)
      const dot = new Mesh(taking ? captureShape : dotShape, targetLook)
      dot.position.set(spot.x, 0.002, spot.z)
      overlays.add(dot)
    }
  }

  const draw = (now: number) => {
    frame = 0
    let busy = false

    slides = slides.filter((slide) => {
      const t = Math.min(1, (now - slide.started) / slideMs)
      const eased = t < 0.5 ? 4 * t * t * t : 1 - (-2 * t + 2) ** 3 / 2
      slide.holder.position.lerpVectors(slide.from, slide.to, eased)
      slide.holder.position.y = Math.sin(Math.PI * t) * Math.min(1.1, 0.3 * slide.from.distanceTo(slide.to))
      return t < 1
    })
    busy ||= slides.length > 0

    for (const [square, holder] of onScreen) {
      const lifter = holder.children[0]
      const goal = picture?.selected === square ? liftHeight : 0
      lifter.position.y += (goal - lifter.position.y) * 0.25
      if (Math.abs(goal - lifter.position.y) > 0.002) {
        busy = true
      } else {
        lifter.position.y = goal
      }
    }

    renderer.render(scene, camera)
    if (busy) {
      invalidate()
    }
  }

  const invalidate = () => {
    if (!frame) {
      frame = requestAnimationFrame(draw)
    }
  }

  const resize = () => {
    const { width, height } = host.getBoundingClientRect()
    if (width < 1 || height < 1) {
      return
    }
    renderer.setSize(width, height, false)
    aspect = width / height
    aim()
    invalidate()
  }
  const watcher = new ResizeObserver(resize)
  watcher.observe(host)

  const ray = new Raycaster()
  const pointer = new Vector2()
  const floor = new Mesh(new PlaneGeometry(8, 8).rotateX(-Math.PI / 2), new MeshBasicMaterial())
  floor.position.y = surface
  floor.updateMatrixWorld()

  const pick = (event: MouseEvent) => {
    const bounds = canvas.getBoundingClientRect()
    pointer.set(
      ((event.clientX - bounds.left) / bounds.width) * 2 - 1,
      -((event.clientY - bounds.top) / bounds.height) * 2 + 1,
    )
    ray.setFromCamera(pointer, camera)

    const holders = [...onScreen.values()]
    const hit = ray.intersectObjects(holders, true)[0]
    if (hit) {
      let node: Object3D | null = hit.object
      while (node && node.userData.square === undefined) {
        node = node.parent
      }
      if (node) {
        onSquare(node.userData.square as string)
        return
      }
    }

    const ground = ray.intersectObject(floor)[0]
    const square = ground ? worldToSquare({ x: ground.point.x, z: ground.point.z }) : null
    if (square) {
      onSquare(square)
    }
  }
  canvas.addEventListener('click', pick)

  const lost = (event: Event) => {
    event.preventDefault()
    onLost()
  }
  canvas.addEventListener('webglcontextlost', lost)

  resize()

  return {
    show(next) {
      if (next.youAreWhite !== youAreWhite) {
        youAreWhite = next.youAreWhite
        aim()
      }
      picture = next
      place(next.pieces)
      mark(next)
      invalidate()
    },
    dispose() {
      cancelAnimationFrame(frame)
      watcher.disconnect()
      canvas.removeEventListener('click', pick)
      canvas.removeEventListener('webglcontextlost', lost)
      renderer.dispose()
      canvas.remove()
    },
  }
}

const squareShape = new PlaneGeometry(1, 1).rotateX(-Math.PI / 2)
const dotShape = new CircleGeometry(0.15, 32).rotateX(-Math.PI / 2)
const captureShape = new RingGeometry(0.38, 0.47, 48).rotateX(-Math.PI / 2)
const targetLook = new MeshBasicMaterial({ color: 0x1b1b1b, transparent: true, opacity: 0.45, depthWrite: false })
const tints = new Map<string, MeshBasicMaterial>()

function tintLook(color: number, opacity: number): MeshBasicMaterial {
  const key = `${color}:${opacity}`
  let look = tints.get(key)
  if (!look) {
    look = new MeshBasicMaterial({ color, transparent: true, opacity, depthWrite: false })
    tints.set(key, look)
  }
  return look
}

// The playing surface sits a little below the frame's rim; find it by dropping a ray.
function surfaceHeight(board: Object3D): number {
  board.updateMatrixWorld(true)
  const down = new Raycaster(new Vector3(0.5, 50, 0.5), new Vector3(0, -1, 0))
  return down.intersectObject(board, true)[0]?.point.y ?? 0
}
