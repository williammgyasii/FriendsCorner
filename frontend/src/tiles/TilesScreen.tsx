import { AnimatePresence, animate, MotionConfig, motion, useReducedMotion, type PanInfo } from 'motion/react'
import { useEffect, useRef, useState } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import { seatColor } from '../lobby/seatStyle.ts'
import type { AppDispatch, RootState } from '../store/index.ts'
import { selectTiles, selectTilesView } from '../store/selectors.ts'
import {
  askPreview,
  cancelBlank,
  cancelExchange,
  chooseBlankLetter,
  dropRackTile,
  moveUnsent,
  passTiles,
  recallTiles,
  rematchTiles,
  returnToRack,
  sendExchange,
  shuffleRack,
  startExchange,
  submitTiles,
  tapRackTile,
  tapSquare,
} from '../store/tilesUiSlice.ts'
import { dropTarget, type DropTarget, type PlayerView, type RackTileView, type SquareView, type TilesView } from '../tilesLook.ts'

const premiumName: Record<string, string> = {
  TW: 'triple word',
  DW: 'double word',
  TL: 'triple letter',
  DL: 'double letter',
  '★': 'star',
}

const LETTERS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.split('')

type Pointer = MouseEvent | TouchEvent | PointerEvent

function targetUnder(event: Pointer, info: PanInfo): DropTarget {
  const point = 'changedTouches' in event && event.changedTouches.length > 0 ? event.changedTouches[0] : null
  const x = point ? point.clientX : 'clientX' in event ? event.clientX : info.point.x - window.scrollX
  const y = point ? point.clientY : 'clientY' in event ? event.clientY : info.point.y - window.scrollY
  return dropTarget(document.elementsFromPoint(x, y) as HTMLElement[])
}

// A drag must not also count as a tap when the pointer is let go.
function useDragGuard() {
  const dragged = useRef(false)
  return {
    start: () => {
      dragged.current = true
    },
    tapped: () => {
      if (dragged.current) {
        dragged.current = false
        return false
      }
      return true
    },
  }
}

export function TilesScreen() {
  const dispatch = useDispatch<AppDispatch>()
  const view = useSelector(selectTilesView)
  const tiles = useSelector(selectTiles)
  const board = tiles?.board
  const unsent = useSelector((state: RootState) => state.tilesUi.unsent)
  const askingBlank = useSelector((state: RootState) => state.tilesUi.askingBlank)
  const exchanging = useSelector((state: RootState) => state.tilesUi.exchanging)

  const unsentKey = unsent.map((tile) => `${tile.square}${tile.letter}${tile.blank ? '?' : ''}`).join(',')
  const boardKey = board?.join('') ?? ''
  useEffect(() => {
    dispatch(askPreview())
  }, [dispatch, unsentKey, boardKey])

  if (!view) {
    return null
  }

  const playKey = `${JSON.stringify(tiles?.lastPlay ?? null)}|${boardKey}`

  return (
    <MotionConfig reducedMotion="user">
      <div className="tiles-screen">
        <header className="tiles-top">
          <p role="status" className="tiles-status">
            {view.status}
          </p>
          <FullScreenButton />
        </header>
        <ul aria-label="Scores" className="tiles-players">
          {view.players.map((player) => (
            <Panel key={player.seat} player={player} playKey={playKey} />
          ))}
        </ul>
        <aside className="tiles-plays">
          {view.lastPlay && <p className="tiles-last">{view.lastPlay}</p>}
          <p className="tiles-bag">{view.bag} in the bag</p>
          {view.refusal && (
            <p role="alert" className="tiles-refusal">
              {view.refusal}
            </p>
          )}
        </aside>
        <main className="tiles-board-cell">
          <Board squares={view.squares} bubble={view.bubble} />
        </main>
        <div className="tiles-tray">
          <Rack rack={view.rack} />
          <ActionBar view={view} exchanging={exchanging} />
        </div>
        {askingBlank && <BlankPrompt />}
      </div>
    </MotionConfig>
  )
}

function FullScreenButton() {
  const [full, setFull] = useState(() => Boolean(document.fullscreenElement))

  useEffect(() => {
    const changed = () => setFull(Boolean(document.fullscreenElement))
    document.addEventListener('fullscreenchange', changed)
    return () => document.removeEventListener('fullscreenchange', changed)
  }, [])

  if (!document.fullscreenEnabled) {
    return null
  }

  return (
    <button
      type="button"
      className="tiles-icon-button"
      aria-label={full ? 'Leave full screen' : 'Full screen'}
      onClick={() => (full ? document.exitFullscreen() : document.documentElement.requestFullscreen())}
    >
      {full ? '⤡' : '⛶'}
    </button>
  )
}

function Panel({ player, playKey }: { player: PlayerView; playKey: string }) {
  return (
    <li
      className="tiles-panel"
      aria-current={player.toMove ? 'true' : undefined}
      style={{ ['--seat' as string]: seatColor[player.seat] }}
    >
      {player.face ? (
        <div className="tiles-face" data-face={player.face} />
      ) : (
        <span className="tiles-initial" aria-hidden>
          {player.seat}
        </span>
      )}
      <span className="tiles-panel-text">
        <span className="tiles-seat">
          {player.seat}
          {player.isYou && <small> (you)</small>}
        </span>
        <CountUp value={player.score} />
        <span className="tiles-count">{player.count} tiles</span>
      </span>
      <Gain gain={player.gain} playKey={playKey} />
    </li>
  )
}

// Shows the new score at once, then counts the digits up from the old one.
function CountUp({ value }: { value: number }) {
  const reduce = useReducedMotion()
  const [shown, setShown] = useState(value)
  const last = useRef(value)

  useEffect(() => {
    const from = last.current
    last.current = value
    if (from === value) {
      return
    }
    const controls = animate(from, value, {
      duration: reduce ? 0 : 0.9,
      ease: 'easeOut',
      onUpdate: (latest) => setShown(Math.round(latest)),
    })
    return () => controls.stop()
  }, [value, reduce])

  return <strong className="tiles-score">{shown}</strong>
}

const GAIN_SHOWN_MS = 2200

function Gain({ gain, playKey }: { gain: number | null; playKey: string }) {
  const [hiddenFor, setHiddenFor] = useState<string | null>(null)

  useEffect(() => {
    const timer = setTimeout(() => setHiddenFor(playKey), GAIN_SHOWN_MS)
    return () => clearTimeout(timer)
  }, [playKey])

  return (
    <AnimatePresence>
      {gain !== null && hiddenFor !== playKey && (
        <motion.span
          key={playKey}
          className="tiles-gain"
          initial={{ opacity: 0, y: 12, scale: 0.6 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          exit={{ opacity: 0, y: -18 }}
          transition={{ type: 'spring', stiffness: 420, damping: 22 }}
        >
          +{gain}
        </motion.span>
      )}
    </AnimatePresence>
  )
}

function Board({ squares, bubble }: { squares: SquareView[]; bubble: TilesView['bubble'] }) {
  return (
    <div className="tiles-board">
      {squares.map((square) => (
        <Square key={square.square} square={square} bubble={bubble?.square === square.square ? bubble : null} />
      ))}
    </div>
  )
}

function Square({ square, bubble }: { square: SquareView; bubble: TilesView['bubble'] }) {
  const dispatch = useDispatch<AppDispatch>()
  const guard = useDragGuard()
  const [dragging, setDragging] = useState(false)
  const name = [`Square ${square.square}`, premiumName[square.label], square.letter].filter(Boolean).join(', ')
  const premium = square.label ? `premium-${square.label === '★' ? 'star' : square.label}` : ''

  const dropped = (event: Pointer, info: PanInfo) => {
    setDragging(false)
    const target = targetUnder(event, info)
    if (target.kind === 'square') {
      dispatch(moveUnsent(square.square, target.square))
    } else if (target.kind === 'rack') {
      dispatch(returnToRack(square.square))
    }
  }

  return (
    <button
      type="button"
      aria-label={name}
      data-square={square.square}
      className={`tiles-square ${square.letter ? '' : premium}`}
      onClick={() => guard.tapped() && dispatch(tapSquare(square.square))}
    >
      {square.letter ? (
        square.unsent ? (
          <motion.span
            key="unsent"
            className="tiles-tile unsent"
            data-dragging={dragging ? '' : undefined}
            drag
            dragSnapToOrigin
            dragMomentum={false}
            whileDrag={{ scale: 1.15, zIndex: 30 }}
            onDragStart={() => {
              guard.start()
              setDragging(true)
            }}
            onDragEnd={dropped}
          >
            <Face letter={square.letter} value={square.value} />
          </motion.span>
        ) : (
          <motion.span
            key="placed"
            className="tiles-tile"
            initial={{ scale: 0.4, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            transition={{ type: 'spring', stiffness: 520, damping: 26 }}
          >
            <Face letter={square.letter} value={square.value} />
          </motion.span>
        )
      ) : (
        <span className="tiles-label">{square.label}</span>
      )}
      {bubble && (
        <span role="note" aria-label={`Preview: ${bubble.text}`} className={`tiles-bubble ${bubble.ok ? 'ok' : 'bad'}`}>
          {bubble.text}
        </span>
      )}
    </button>
  )
}

function Face({ letter, value }: { letter: string; value: number | null }) {
  return (
    <>
      <span className="tiles-letter">{letter}</span>
      {value !== null && <sub className="tiles-value">{value}</sub>}
    </>
  )
}

function Rack({ rack }: { rack: RackTileView[] }) {
  return (
    <div role="group" aria-label="Your tiles" className="tiles-rack" data-rack="">
      {rack.map((tile) => (
        <RackTile key={`${tile.index}${tile.letter}`} tile={tile} />
      ))}
    </div>
  )
}

function RackTile({ tile }: { tile: RackTileView }) {
  const dispatch = useDispatch<AppDispatch>()
  const guard = useDragGuard()
  const [dragging, setDragging] = useState(false)
  const blank = tile.letter === '?'
  const points = `${tile.value} ${tile.value === 1 ? 'point' : 'points'}`
  const lifted = tile.selected || tile.swapping

  const dropped = (event: Pointer, info: PanInfo) => {
    setDragging(false)
    const target = targetUnder(event, info)
    if (target.kind === 'square') {
      dispatch(dropRackTile(tile.index, target.square))
    }
  }

  return (
    <motion.button
      type="button"
      aria-label={`${blank ? 'Blank' : tile.letter}, ${points}`}
      aria-pressed={lifted}
      disabled={tile.used}
      data-dragging={dragging ? '' : undefined}
      drag={!tile.used}
      dragSnapToOrigin
      dragMomentum={false}
      whileDrag={{ scale: 1.1, zIndex: 30 }}
      onDragStart={() => {
        guard.start()
        setDragging(true)
      }}
      onDragEnd={dropped}
      onClick={() => guard.tapped() && dispatch(tapRackTile(tile.index))}
      layout
      initial={{ y: 40, opacity: 0 }}
      animate={{ y: lifted ? -10 : 0, opacity: tile.used ? 0.25 : 1 }}
      className={`tiles-rack-tile ${tile.selected ? 'selected' : ''} ${tile.swapping ? 'swapping' : ''}`}
    >
      {!blank && <Face letter={tile.letter} value={tile.value} />}
    </motion.button>
  )
}

function ActionBar({ view, exchanging }: { view: TilesView; exchanging: number[] | null }) {
  const dispatch = useDispatch<AppDispatch>()

  if (view.actions.rematch) {
    return (
      <div className="tiles-actions">
        <button type="button" className="tiles-button primary" onClick={() => dispatch(rematchTiles())}>
          Rematch
        </button>
      </div>
    )
  }

  if (exchanging) {
    return (
      <div className="tiles-actions">
        <button type="button" className="tiles-button primary" disabled={exchanging.length === 0} onClick={() => dispatch(sendExchange())}>
          Send back {exchanging.length}
        </button>
        <button type="button" className="tiles-button" onClick={() => dispatch(cancelExchange())}>
          Cancel
        </button>
      </div>
    )
  }

  return (
    <div className="tiles-actions">
      <button type="button" className="tiles-button" onClick={() => dispatch(recallTiles())}>
        Recall
      </button>
      <button type="button" className="tiles-button" onClick={() => dispatch(shuffleRack())}>
        Shuffle
      </button>
      <button
        type="button"
        className="tiles-button primary submit"
        disabled={!view.actions.submit}
        onClick={() => dispatch(submitTiles())}
      >
        {view.submitLabel}
      </button>
      <button type="button" className="tiles-button" disabled={!view.actions.exchange} onClick={() => dispatch(startExchange())}>
        Exchange
      </button>
      <button type="button" className="tiles-button" disabled={!view.actions.pass} onClick={() => dispatch(passTiles())}>
        Pass
      </button>
    </div>
  )
}

function BlankPrompt() {
  const dispatch = useDispatch<AppDispatch>()

  return (
    <div className="tiles-scrim">
      <div role="dialog" aria-label="Which letter is the blank?" className="tiles-dialog">
        <p className="tiles-dialog-title">Which letter is the blank?</p>
        <div className="tiles-letters">
          {LETTERS.map((letter) => (
            <button key={letter} type="button" onClick={() => dispatch(chooseBlankLetter(letter))} className="tiles-letter-choice">
              {letter}
            </button>
          ))}
        </div>
        <button type="button" className="tiles-dialog-cancel" onClick={() => dispatch(cancelBlank())}>
          Cancel
        </button>
      </div>
    </div>
  )
}
