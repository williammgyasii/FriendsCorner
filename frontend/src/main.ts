import { FaceCall, type SignalPayload } from './faceCall.ts'
import '@fontsource-variable/fredoka'
import confetti from 'canvas-confetti'
import {
  checkedKing,
  describeChess,
  needsPromotion,
  pieceHint,
  piecesFrom,
  squaresInView,
  targetsFrom,
  type ChessCard,
  type ChessState,
} from './chessLook.ts'
import type { ChessView } from './chessView3d.ts'
import { loadIceServers } from './ice.ts'
import { pickLayout } from './layout.ts'
import { describeMarks, describePlayers, type PlayerCard } from './marksLook.ts'
import { placeFigures, wallHeight, type PlacedFigure } from './roomLook.ts'
import './style.css'

const app = document.querySelector<HTMLDivElement>('#app')
if (!app) {
  throw new Error('Missing #app')
}

const roomId = new URLSearchParams(location.search).get('room')
if (!roomId) {
  renderStart(app)
} else {
  renderRoom(app, roomId)
}

function renderStart(root: HTMLDivElement) {
  root.innerHTML = `
    <main class="door">
      <p class="mark">Friends Corner</p>
      <h1>Open a lobby</h1>
      <p>Send the link. You both wait there, then one of you starts the world.</p>
      <button id="start" type="button">Open a lobby</button>
    </main>
  `
  root.querySelector<HTMLButtonElement>('#start')?.addEventListener('click', async () => {
    const response = await fetch('/rooms', { method: 'POST' })
    const body = (await response.json()) as { id: string }
    location.search = `?room=${body.id}`
  })
}

type Player = { x: number; y: number } | null
type BoardState = {
  squares: (string | null)[]
  next: 'A' | 'B'
  winner: 'A' | 'B' | null
  draw: boolean
}

type StateMessage = {
  type: 'state'
  you: 'A' | 'B'
  world: string | null
  board: BoardState | null
  chess: ChessState | null
  players: { A: Player; B: Player }
}

const chessGlyph: Record<string, string> = { k: '♚', q: '♛', r: '♜', b: '♝', n: '♞', p: '♟' }
const promotionChoices = [
  { piece: 'q', name: 'Queen' },
  { piece: 'r', name: 'Rook' },
  { piece: 'b', name: 'Bishop' },
  { piece: 'n', name: 'Knight' },
]

// U+FE0E keeps iOS from drawing the pawn as an emoji.
function pieceHtml(piece: string) {
  const side = piece === piece.toUpperCase() ? 'white' : 'black'
  return `<span class="piece piece-${side}" aria-hidden="true">${chessGlyph[piece.toLowerCase()]}\uFE0E</span>`
}

function renderRoom(root: HTMLDivElement, id: string) {
  root.innerHTML = `
    <div class="screen" id="screen">
      <div class="face-dock">
        <div class="slot">
          <video id="local-face" autoplay playsinline muted></video>
          <p id="local-note">You</p>
        </div>
        <div class="portrait slot" id="face">
          <video id="remote-face" autoplay playsinline></video>
          <p class="face-note" id="face-note">Waiting for them</p>
        </div>
      </div>
      <section class="lobby" id="lobby">
        <p class="mark">Friends Corner</p>
        <h1>Lobby</h1>
        <p class="status" id="status">Connecting…</p>
        <div class="games">
          <button class="game selected" type="button" data-world="room">
            <p class="kicker">Selected</p>
            <h2>The Room</h2>
            <p>A floor, a window, and the two of you.</p>
          </button>
          <button class="game" type="button" data-world="tictactoe">
            <p class="kicker">Selected</p>
            <h2>Tic-tac-toe</h2>
            <p>Nine squares. You are X, they are O.</p>
          </button>
          <button class="game" type="button" data-world="chess">
            <p class="kicker">Selected</p>
            <h2>Chess</h2>
            <p>The classic. Tap a piece and the board shows where it can go.</p>
          </button>
        </div>
        <div class="actions">
          <button id="launch" type="button">Start</button>
          <button id="copy" class="quiet" type="button">Copy invite</button>
        </div>
      </section>
      <section class="world floor-world" id="floor-world">
        <canvas class="floor" id="floor" width="480" height="320"></canvas>
        <div class="pad" id="pad" aria-label="Move"></div>
      </section>
      <section class="world marks-world" id="marks-world">
        <div class="stage" id="marks-board">
          <header class="versus">
            <div class="player card-left" id="card-left">
              <div class="player-face" id="face-left"></div>
              <div class="player-info">
                <span class="player-mark"></span>
                <strong class="player-label"></strong>
                <span class="player-note"></span>
              </div>
            </div>
            <span class="vs">VS</span>
            <div class="player card-right" id="card-right">
              <div class="player-face" id="face-right"></div>
              <div class="player-info">
                <span class="player-mark"></span>
                <strong class="player-label"></strong>
                <span class="player-note"></span>
              </div>
            </div>
          </header>
          <div class="arena">
            <div class="board">
              <div class="grid" id="grid"></div>
              <svg class="strike" id="strike" viewBox="0 0 300 300" aria-hidden="true">
                <line id="strike-line" x1="0" y1="0" x2="0" y2="0" />
              </svg>
            </div>
          </div>
          <footer class="action-bar">
            <p class="marks-status" id="marks-status"></p>
            <button class="chunky" id="rematch" type="button" hidden>Play again</button>
          </footer>
        </div>
      </section>
      <section class="world chess-world" id="chess-world">
        <div class="stage" id="chess-stage">
          <header class="versus">
            <div class="player card-left" id="chess-card-left">
              <div class="player-face"></div>
              <div class="player-info">
                <span class="player-mark"></span>
                <strong class="player-label"></strong>
                <span class="player-note"></span>
              </div>
            </div>
            <span class="vs">VS</span>
            <div class="player card-right" id="chess-card-right">
              <div class="player-face"></div>
              <div class="player-info">
                <span class="player-mark"></span>
                <strong class="player-label"></strong>
                <span class="player-note"></span>
              </div>
            </div>
          </header>
          <div class="arena">
            <div class="board chess-board" id="chess-board">
              <div class="chess-grid" id="chess-grid"></div>
              <p class="chess-credit">
                Chess set: <a href="https://github.com/KhronosGroup/glTF-Sample-Assets/tree/main/Models/ABeautifulGame" target="_blank" rel="noopener">A Beautiful Game</a>
                by Moeen and Mujtaba Sayed,
                <a href="https://creativecommons.org/licenses/by/4.0/" target="_blank" rel="noopener">CC BY 4.0</a>
              </p>
              <div class="promotion" id="promotion" hidden>
                <p>Your pawn made it across! Pick what it becomes.</p>
                <div class="promotion-choices" id="promotion-choices"></div>
              </div>
            </div>
          </div>
          <footer class="action-bar">
            <div class="chess-words">
              <p class="marks-status" id="chess-status"></p>
              <p class="chess-hint" id="chess-hint"></p>
            </div>
            <button class="chunky" id="chess-rematch" type="button" hidden>Play again</button>
          </footer>
        </div>
      </section>
    </div>
  `

  const status = root.querySelector<HTMLParagraphElement>('#status')!
  const canvas = root.querySelector<HTMLCanvasElement>('#floor')!
  const pad = root.querySelector<HTMLDivElement>('#pad')!
  const portrait = root.querySelector<HTMLDivElement>('#face')!
  const faceNote = root.querySelector<HTMLParagraphElement>('#face-note')!
  const video = root.querySelector<HTMLVideoElement>('#remote-face')!
  const context = canvas.getContext('2d')
  if (!context) {
    throw new Error('Canvas is unavailable')
  }

  const screen = root.querySelector<HTMLDivElement>('#screen')!
  const fitLayout = () => {
    screen.dataset.layout = pickLayout(window.innerWidth, window.innerHeight)
  }
  fitLayout()
  window.addEventListener('resize', fitLayout)
  const localVideo = root.querySelector<HTMLVideoElement>('#local-face')!
  const localNote = root.querySelector<HTMLParagraphElement>('#local-note')!
  const faceNotes = {
    hidden: 'Waiting for them',
    waiting: 'Connecting their camera…',
    unavailable: 'Their camera is off',
    live: 'Them',
  }
  const socket = new WebSocket(`${location.protocol === 'https:' ? 'wss' : 'ws'}://${location.host}/ws/${id}`)
  const face = new FaceCall(
    (payload) => {
      if (socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({ type: 'signal', payload }))
      }
    },
    video,
    (tile) => {
      portrait.className = `slot portrait ${tile}`
      faceNote.textContent = faceNotes[tile]
    },
    () => loadIceServers(id),
  )
  const cameraReady = navigator.mediaDevices
    .getUserMedia({ video: true, audio: true })
    .then((stream) => {
      localVideo.srcObject = stream
      void localVideo.play().catch(() => undefined)
      localNote.textContent = 'You'
      return stream
    })
    .catch(() => {
      localNote.textContent = 'Camera is off'
      return null
    })

  root.querySelector('#copy')?.addEventListener('click', () => {
    void navigator.clipboard.writeText(location.href)
    status.textContent = 'Invite copied.'
  })
  root.querySelector('#launch')?.addEventListener('click', () => {
    const selected = root.querySelector<HTMLButtonElement>('.game.selected')?.dataset.world ?? 'room'
    if (socket.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify({ type: 'launch', world: selected }))
      const names: Record<string, string> = { tictactoe: 'Tic-tac-toe', chess: 'Chess', room: 'The Room' }
      status.textContent = `Opening ${names[selected] ?? 'The Room'}…`
    }
  })
  root.querySelectorAll<HTMLButtonElement>('.game').forEach((card) => {
    card.addEventListener('click', () => {
      root.querySelectorAll('.game').forEach((other) => other.classList.remove('selected'))
      card.classList.add('selected')
    })
  })

  let you: 'A' | 'B' = 'A'
  let worldOpen = false
  let faceStarted = false
  let boardKey = ''
  const held = new Set<string>()
  const grid = root.querySelector<HTMLDivElement>('#grid')!
  const marksBoard = root.querySelector<HTMLDivElement>('#marks-board')!
  const marksStatus = root.querySelector<HTMLParagraphElement>('#marks-status')!
  const rematch = root.querySelector<HTMLButtonElement>('#rematch')!
  const strike = root.querySelector<SVGSVGElement>('#strike')!
  const strikeLine = root.querySelector<SVGLineElement>('#strike-line')!
  const cards = {
    left: root.querySelector<HTMLDivElement>('#card-left')!,
    right: root.querySelector<HTMLDivElement>('#card-right')!,
  }
  const faceDock = root.querySelector<HTMLDivElement>('.face-dock')!
  const localSlot = localVideo.parentElement as HTMLDivElement
  let previousSquares: (string | null)[] = []
  let celebratedKey = ''
  rematch.addEventListener('click', () => {
    if (socket.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify({ type: 'rematch' }))
    }
  })

  const chessCards = {
    left: root.querySelector<HTMLDivElement>('#chess-card-left')!,
    right: root.querySelector<HTMLDivElement>('#chess-card-right')!,
  }
  const chessStage = root.querySelector<HTMLDivElement>('#chess-stage')!
  const chessBoard = root.querySelector<HTMLDivElement>('#chess-board')!
  const chessGrid = root.querySelector<HTMLDivElement>('#chess-grid')!
  const chessStatus = root.querySelector<HTMLParagraphElement>('#chess-status')!
  const chessHint = root.querySelector<HTMLParagraphElement>('#chess-hint')!
  const chessRematch = root.querySelector<HTMLButtonElement>('#chess-rematch')!
  const promotion = root.querySelector<HTMLDivElement>('#promotion')!
  const promotionButtons = root.querySelector<HTMLDivElement>('#promotion-choices')!
  let chessState: ChessState | null = null
  let chessHere = { A: false, B: false }
  let selected: string | null = null
  let inspected: string | null = null
  let pendingPromotion: { from: string; to: string } | null = null
  let chessCelebrated = ''
  let chessView: ChessView | null = null
  let chessViewTried = false

  const openChessView = () => {
    chessViewTried = true
    const fallBack = () => {
      chessView?.dispose()
      chessView = null
      chessBoard.classList.remove('three')
      renderChess()
    }
    import('./chessView3d.ts')
      .then(({ mountChessView }) => mountChessView(chessBoard, chooseChessSquare, fallBack))
      .then((view) => {
        chessView = view
        chessBoard.classList.add('three')
        renderChess()
      })
      .catch((error: unknown) => {
        console.warn('3D chess is unavailable; using the flat board.', error)
      })
  }

  const sendChessMove = (from: string, to: string, piece?: string) => {
    if (socket.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify({ type: 'chess-move', from, to, ...(piece ? { promotion: piece } : {}) }))
    }
  }

  chessRematch.addEventListener('click', () => {
    if (socket.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify({ type: 'chess-rematch' }))
    }
  })

  const paintChessCard = (element: HTMLDivElement, card: ChessCard) => {
    element.style.setProperty('--seat', card.color)
    element.classList.toggle('active', card.active)
    element.classList.toggle('quiet', !card.active && card.note !== 'Waiting')
    element.querySelector('.player-mark')!.innerHTML = pieceHtml(card.side === 'White' ? 'K' : 'k')
    element.querySelector('.player-label')!.textContent = `${card.label} · ${card.side}`
    element.querySelector('.player-note')!.textContent = card.note
  }

  const chooseChessSquare = (square: string) => {
    if (!chessState) {
      return
    }
    const look = describeChess(chessState, you, chessHere)
    const pieces = piecesFrom(chessState.fen)
    const piece = pieces.get(square)
    if (selected && look.canMove && targetsFrom(chessState.legalMoves, selected).includes(square)) {
      if (needsPromotion(chessState.legalMoves, selected, square)) {
        pendingPromotion = { from: selected, to: square }
      } else {
        sendChessMove(selected, square)
      }
      selected = null
      inspected = null
      renderChess()
      return
    }

    inspected = piece ?? null
    const mine = piece !== undefined && (piece === piece.toUpperCase()) === look.youAreWhite
    selected = look.canMove && mine && selected !== square ? square : null
    renderChess()
  }

  const renderChess = () => {
    if (!chessState) {
      return
    }
    const chess = chessState
    const look = describeChess(chess, you, chessHere)
    const pieces = piecesFrom(chess.fen)
    const targets = selected ? targetsFrom(chess.legalMoves, selected) : []
    const inCheck = checkedKing(chess)

    chessStage.classList.toggle('celebrating', look.celebrate !== null)
    screen.style.setProperty('--turn', look.color)
    paintChessCard(chessCards.left, look.left)
    paintChessCard(chessCards.right, look.right)
    chessStatus.textContent = look.celebrate ?? look.status
    chessRematch.hidden = !look.canRematch
    if (look.canRematch) {
      chessHint.textContent = 'Play again and you swap colors.'
    } else if (inspected) {
      chessHint.textContent = pieceHint(inspected)
    } else if (look.canMove) {
      chessHint.textContent = 'Tap one of your pieces. Dots show where it can go.'
    } else {
      chessHint.textContent = 'Tip: tap any piece to learn how it moves.'
    }

    if (!chessViewTried) {
      openChessView()
    }

    if (chessView) {
      chessView.show({
        pieces,
        youAreWhite: look.youAreWhite,
        selected,
        targets,
        lastMove: chess.lastMove,
        check: inCheck,
      })
      chessGrid.replaceChildren()
    } else {
      chessGrid.replaceChildren(
        ...squaresInView(look.youAreWhite).map((square, index) => {
          const button = document.createElement('button')
          button.type = 'button'
          const piece = pieces.get(square)
          const file = square.charCodeAt(0) - 97
          const rank = Number(square[1])
          button.className = (file + rank) % 2 === 0 ? 'sq light' : 'sq dark'
          button.classList.toggle('selected', square === selected)
          button.classList.toggle('target', targets.includes(square))
          button.classList.toggle('capture', targets.includes(square) && piece !== undefined)
          button.classList.toggle('last', chess.lastMove?.from === square || chess.lastMove?.to === square)
          button.classList.toggle('check', square === inCheck)
          button.setAttribute('aria-label', piece ? `${square}, ${piece}` : square)
          const labels = [
            index % 8 === 0 ? `<span class="coord rank">${rank}</span>` : '',
            index >= 56 ? `<span class="coord file">${square[0]}</span>` : '',
          ].join('')
          button.innerHTML = `${labels}${piece ? pieceHtml(piece) : ''}`
          button.addEventListener('click', () => chooseChessSquare(square))
          return button
        }),
      )
    }

    promotion.hidden = pendingPromotion === null
    promotionButtons.replaceChildren(
      ...promotionChoices.map(({ piece, name }) => {
        const button = document.createElement('button')
        button.type = 'button'
        button.innerHTML = `${pieceHtml(look.youAreWhite ? piece.toUpperCase() : piece)}<span>${name}</span>`
        button.addEventListener('click', () => {
          if (pendingPromotion) {
            sendChessMove(pendingPromotion.from, pendingPromotion.to, piece)
          }
          pendingPromotion = null
          renderChess()
        })
        return button
      }),
    )

    if (chess.outcome?.ending === 'checkmate') {
      if (chessCelebrated !== chess.fen) {
        chessCelebrated = chess.fen
        void celebrateWin(look.color)
      }
    } else {
      chessCelebrated = ''
    }
  }

  const showWorld = (world: string | null) => {
    worldOpen = world === 'room'
    screen.classList.remove('world-room', 'world-game', 'world-tictactoe', 'world-chess')
    if (world === 'room') {
      screen.classList.add('world-room')
    }
    if (world === 'tictactoe' || world === 'chess') {
      screen.classList.add('world-game', `world-${world}`)
    }
    placeFaces(world === 'tictactoe' ? cards : world === 'chess' ? chessCards : null)
  }

  const placeFaces = (stage: { left: HTMLDivElement; right: HTMLDivElement } | null) => {
    const home = stage
      ? [stage.left.querySelector<HTMLDivElement>('.player-face')!, stage.right.querySelector<HTMLDivElement>('.player-face')!]
      : [faceDock, faceDock]
    if (localSlot.parentElement === home[0] && portrait.parentElement === home[1]) {
      return
    }
    home[0].append(localSlot)
    home[1].append(portrait)
    void localVideo.play().catch(() => undefined)
    void video.play().catch(() => undefined)
  }

  const paintCard = (element: HTMLDivElement, card: PlayerCard) => {
    element.style.setProperty('--seat', card.color)
    element.classList.toggle('active', card.active)
    element.classList.toggle('quiet', !card.active && card.note !== 'Waiting')
    element.querySelector('.player-mark')!.innerHTML = markSvg(card.mark, false)
    element.querySelector('.player-label')!.textContent = card.label
    element.querySelector('.player-note')!.textContent = card.note
  }

  const renderMarks = (board: BoardState, here: { A: boolean; B: boolean }) => {
    const look = describeMarks(board, you)
    const players = describePlayers(board, you, here)
    marksBoard.classList.toggle('celebrating', look.celebrate !== null)
    screen.style.setProperty('--turn', look.color)
    paintCard(cards.left, players.left)
    paintCard(cards.right, players.right)
    marksStatus.textContent = look.celebrate ?? look.status
    rematch.hidden = !look.canRematch

    const fresh = board.squares.map((mark, index) => mark !== null && previousSquares[index] == null)
    previousSquares = board.squares
    grid.replaceChildren(
      ...board.squares.map((mark, index) => {
        const button = document.createElement('button')
        button.type = 'button'
        button.setAttribute('aria-label', mark ? `Square ${index + 1}, ${mark}` : `Square ${index + 1}`)
        if (mark === 'X' || mark === 'O') {
          button.innerHTML = markSvg(mark, fresh[index])
        }
        button.classList.toggle('won', look.winning.includes(index))
        button.disabled = look.canRematch || mark !== null || board.next !== you
        button.addEventListener('click', () => {
          if (socket.readyState === WebSocket.OPEN) {
            socket.send(JSON.stringify({ type: 'place', square: index }))
          }
        })
        return button
      }),
    )

    strike.classList.toggle('shown', look.winning.length === 3)
    if (look.winning.length === 3) {
      const [from, to] = strikeEnds(look.winning[0], look.winning[2])
      strikeLine.setAttribute('x1', String(from.x))
      strikeLine.setAttribute('y1', String(from.y))
      strikeLine.setAttribute('x2', String(to.x))
      strikeLine.setAttribute('y2', String(to.y))
      strikeLine.style.stroke = look.color
      const key = board.squares.join('')
      if (celebratedKey !== key) {
        celebratedKey = key
        void celebrateWin(look.color)
      }
    } else {
      celebratedKey = ''
    }
  }

  const beginFace = () => {
    if (faceStarted) {
      return
    }
    faceStarted = true
    void cameraReady.then((stream) => face.start(you, stream))
  }

  socket.addEventListener('message', (event) => {
    const message = JSON.parse(String(event.data)) as { type: string; seat?: 'A' | 'B' }
    if (message.type === 'joined' && message.seat) {
      you = message.seat
      status.textContent = 'You are in the lobby. Share the invite.'
      return
    }

    if (message.type === 'signal') {
      face.receive((message as unknown as { payload: SignalPayload }).payload)
      return
    }

    if (message.type === 'state') {
      const state = message as StateMessage
      you = state.you
      draw(context, state)
      showWorld(state.world)
      if (state.world === 'tictactoe' && state.board) {
        const here = { A: state.players.A !== null, B: state.players.B !== null }
        const key = JSON.stringify({ board: state.board, here })
        if (key !== boardKey) {
          boardKey = key
          renderMarks(state.board, here)
        }
      }
      if (state.world === 'chess' && state.chess) {
        const here = { A: state.players.A !== null, B: state.players.B !== null }
        const key = JSON.stringify({ chess: state.chess, here, you })
        if (key !== boardKey) {
          boardKey = key
          if (chessState?.fen !== state.chess.fen) {
            selected = null
            pendingPromotion = null
          }
          chessState = state.chess
          chessHere = here
          renderChess()
        }
      }
      const other = you === 'A' ? state.players.B : state.players.A
      if (other) {
        if (!state.world) {
          status.textContent = 'You are both here. Pick a game and start.'
        }
        beginFace()
      } else if (faceStarted) {
        faceStarted = false
        face.otherLeft()
      }
    }
  })

  socket.addEventListener('close', (event) => {
    face.stop()
    status.textContent = event.reason === 'room is full' ? 'This room is full.' : 'This room is gone.'
  })

  const sendDirection = (x: number, y: number) => {
    if (socket.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify({ type: 'direction', x, y }))
    }
  }

  window.addEventListener('pointerdown', () => {
    void video.play().catch(() => undefined)
  })

  window.addEventListener('keydown', (event) => {
    void video.play().catch(() => undefined)
    if (!worldOpen || !isMoveKey(event.key)) {
      return
    }
    event.preventDefault()
    held.add(event.key)
    const direction = directionFromKeys(held)
    sendDirection(direction.x, direction.y)
  })

  window.addEventListener('keyup', (event) => {
    if (!worldOpen) {
      held.delete(event.key)
      return
    }
    held.delete(event.key)
    const direction = directionFromKeys(held)
    sendDirection(direction.x, direction.y)
  })

  pad.addEventListener('pointerdown', (event) => {
    pad.setPointerCapture(event.pointerId)
    sendDirection(...directionFromPad(pad, event))
  })
  pad.addEventListener('pointermove', (event) => {
    if (!pad.hasPointerCapture(event.pointerId)) {
      return
    }
    sendDirection(...directionFromPad(pad, event))
  })
  const releasePad = () => sendDirection(0, 0)
  pad.addEventListener('pointerup', releasePad)
  pad.addEventListener('pointercancel', releasePad)
}

function draw(context: CanvasRenderingContext2D, state: StateMessage) {
  context.clearRect(0, 0, 480, 320)
  paintRoom(context)

  const occupied = (['A', 'B'] as const).flatMap((seat) => {
    const player = state.players[seat]
    return player ? [{ seat, x: player.x, y: player.y }] : []
  })
  const figures = placeFigures(occupied).toSorted((left, right) => {
    if (left.seat === state.you) {
      return 1
    }
    if (right.seat === state.you) {
      return -1
    }
    return 0
  })
  for (const figure of figures) {
    paintFigure(context, figure, figure.seat === state.you)
  }
}

function paintRoom(context: CanvasRenderingContext2D) {
  context.fillStyle = '#f3e6d6'
  context.fillRect(0, 0, 480, wallHeight)
  context.fillStyle = '#8d6e5c'
  context.fillRect(300, 12, 76, 40)
  context.fillStyle = '#d5e6ef'
  context.fillRect(304, 16, 68, 32)
  context.strokeStyle = '#8d6e5c'
  context.beginPath()
  context.moveTo(338, 16)
  context.lineTo(338, 48)
  context.moveTo(304, 32)
  context.lineTo(372, 32)
  context.stroke()

  context.fillStyle = '#c9b59a'
  context.fillRect(0, wallHeight - 6, 480, 6)
  context.fillStyle = '#e4c99a'
  context.fillRect(0, wallHeight, 480, 320 - wallHeight)
  context.strokeStyle = 'rgba(140, 100, 60, 0.28)'
  for (let y = wallHeight + 28; y < 320; y += 28) {
    context.beginPath()
    context.moveTo(0, y)
    context.lineTo(480, y)
    context.stroke()
  }

  context.fillStyle = '#8a5a44'
  context.beginPath()
  context.ellipse(240, 230, 74, 28, 0, 0, Math.PI * 2)
  context.fill()
  context.fillStyle = '#a56b52'
  context.beginPath()
  context.ellipse(240, 226, 46, 16, 0, 0, Math.PI * 2)
  context.fill()
}

function paintFigure(context: CanvasRenderingContext2D, figure: PlacedFigure, yours: boolean) {
  const { top, width, height, centerX } = figure
  context.fillStyle = 'rgba(28, 25, 21, 0.18)'
  context.beginPath()
  context.ellipse(centerX, top + height - 2, width / 3, 4, 0, 0, Math.PI * 2)
  context.fill()

  const bodyTop = top + height * 0.4
  context.fillStyle = figure.bodyColor
  context.beginPath()
  context.roundRect(centerX - width * 0.28, bodyTop, width * 0.56, height - (bodyTop - top) - 3, 8)
  context.fill()

  const headRadius = width * 0.28
  const headY = top + headRadius + 2
  const head = figure.parts.find((part) => part.kind === 'head')
  context.fillStyle = head?.color ?? '#f3d7c4'
  context.beginPath()
  context.arc(centerX, headY, headRadius, 0, Math.PI * 2)
  context.fill()

  context.fillStyle = '#1c1915'
  context.beginPath()
  context.arc(centerX - 3, headY - 1, 1.4, 0, Math.PI * 2)
  context.arc(centerX + 3, headY - 1, 1.4, 0, Math.PI * 2)
  context.fill()

  if (yours) {
    context.strokeStyle = '#f4f0e8'
    context.lineWidth = 2
    context.beginPath()
    context.arc(centerX, headY, headRadius + 3, 0, Math.PI * 2)
    context.stroke()
  }
}

function markSvg(mark: 'X' | 'O', fresh: boolean) {
  const drawn = fresh ? ' drawn' : ''
  if (mark === 'X') {
    return `<svg class="mark-x${drawn}" viewBox="0 0 100 100" aria-hidden="true"><path d="M24 24 L76 76" /><path d="M76 24 L24 76" /></svg>`
  }
  return `<svg class="mark-o${drawn}" viewBox="0 0 100 100" aria-hidden="true"><circle cx="50" cy="50" r="28" /></svg>`
}

function strikeEnds(first: number, last: number) {
  const center = (square: number) => ({ x: (square % 3) * 100 + 50, y: Math.floor(square / 3) * 100 + 50 })
  const from = center(first)
  const to = center(last)
  const length = Math.hypot(to.x - from.x, to.y - from.y)
  const reach = 34 / length
  return [
    { x: from.x - (to.x - from.x) * reach, y: from.y - (to.y - from.y) * reach },
    { x: to.x + (to.x - from.x) * reach, y: to.y + (to.y - from.y) * reach },
  ]
}

async function celebrateWin(color: string) {
  if (matchMedia('(prefers-reduced-motion: reduce)').matches) {
    return
  }
  const colors = [color, '#ffd23f', '#ffffff']
  await confetti({ particleCount: 90, spread: 70, startVelocity: 42, origin: { x: 0.2, y: 0.7 }, angle: 60, colors })
  await confetti({ particleCount: 90, spread: 70, startVelocity: 42, origin: { x: 0.8, y: 0.7 }, angle: 120, colors })
}

function isMoveKey(key: string) {
  return ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'w', 'a', 's', 'd', 'W', 'A', 'S', 'D'].includes(key)
}

function directionFromKeys(held: Set<string>) {
  const left = held.has('ArrowLeft') || held.has('a') || held.has('A')
  const right = held.has('ArrowRight') || held.has('d') || held.has('D')
  const down = held.has('ArrowDown') || held.has('s') || held.has('S')
  const up = held.has('ArrowUp') || held.has('w') || held.has('W')
  return {
    x: Number(right) - Number(left),
    y: Number(up) - Number(down),
  }
}

function directionFromPad(pad: HTMLDivElement, event: PointerEvent): [number, number] {
  const rect = pad.getBoundingClientRect()
  const radius = rect.width / 2
  const x = (event.clientX - rect.left - radius) / radius
  const y = -(event.clientY - rect.top - radius) / radius
  const length = Math.hypot(x, y)
  if (length <= 1) {
    return [x, y]
  }
  return [x / length, y / length]
}
