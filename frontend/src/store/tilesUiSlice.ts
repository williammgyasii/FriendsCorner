import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import type { TilesPreview, Unsent } from '../tilesLook.ts'
import type { AppThunk } from './index.ts'
import { stateReceived } from './roomSlice.ts'

const BLANK = '?'

// What this page is doing with its own tiles before anything is sent. The
// server's rack and board stay the truth; unsent tiles are drawn on top.
type TilesUiState = {
  selected: number | null
  unsent: Unsent[]
  askingBlank: { square: number; rackIndex: number } | null
  exchanging: number[] | null
  rack: string
  // Rack indexes in the order this page shows them. Shuffle only changes this.
  order: number[]
  preview: TilesPreview | null
}

const identity = (length: number) => Array.from({ length }, (_, index) => index)

const initialState: TilesUiState = {
  selected: null,
  unsent: [],
  askingBlank: null,
  exchanging: null,
  rack: '',
  order: [],
  preview: null,
}

const sameTiles = (unsent: Unsent[], asked: TilesPreview['tiles']) => {
  const key = (tiles: { square: number; letter: string; blank: boolean }[]) =>
    tiles
      .map(({ square, letter, blank }) => `${square}${letter}${blank ? '?' : ''}`)
      .toSorted()
      .join(',')
  return key(unsent) === key(asked)
}

export const tilesUiSlice = createSlice({
  name: 'tilesUi',
  initialState,
  reducers: {
    rackTapped(state, action: PayloadAction<number>) {
      const index = action.payload
      if (state.exchanging) {
        state.exchanging = state.exchanging.includes(index)
          ? state.exchanging.filter((picked) => picked !== index)
          : [...state.exchanging, index]
        return
      }
      if (state.unsent.some((tile) => tile.rackIndex === index)) {
        return
      }
      state.selected = state.selected === index ? null : index
    },
    placed(state, action: PayloadAction<Unsent>) {
      state.unsent.push(action.payload)
      state.selected = null
      state.preview = null
    },
    takenBack(state, action: PayloadAction<number>) {
      state.unsent = state.unsent.filter((tile) => tile.square !== action.payload)
      state.selected = null
      state.preview = null
    },
    moved(state, action: PayloadAction<{ from: number; to: number }>) {
      const tile = state.unsent.find((unsent) => unsent.square === action.payload.from)
      if (tile) {
        tile.square = action.payload.to
        state.preview = null
      }
    },
    blankAsked(state, action: PayloadAction<{ square: number; rackIndex: number }>) {
      state.askingBlank = action.payload
      state.selected = null
    },
    blankChosen(state, action: PayloadAction<string>) {
      if (state.askingBlank) {
        state.unsent.push({ ...state.askingBlank, letter: action.payload.toUpperCase(), blank: true })
        state.askingBlank = null
        state.preview = null
      }
    },
    blankCancelled(state) {
      state.askingBlank = null
    },
    recalled(state) {
      state.unsent = []
      state.selected = null
      state.askingBlank = null
      state.preview = null
    },
    exchangeStarted(state) {
      state.exchanging = []
      state.selected = null
    },
    exchangeEnded(state) {
      state.exchanging = null
    },
    rackShuffled(state, action: PayloadAction<number[]>) {
      state.order = action.payload
    },
    previewReceived(state, action: PayloadAction<TilesPreview>) {
      if (state.unsent.length > 0 && sameTiles(state.unsent, action.payload.tiles)) {
        state.preview = action.payload
      }
    },
  },
  extraReducers: (builder) => {
    // A new rack means the play or exchange was taken, so the unsent tiles
    // are now real ones. A refusal leaves the rack alone, and them in place.
    builder.addCase(stateReceived, (state, action) => {
      const tiles = action.payload.tiles ?? null
      const rack = tiles?.rack.join('') ?? ''
      if (rack !== state.rack) {
        return { ...initialState, rack, order: identity(tiles?.rack.length ?? 0) }
      }
      if (tiles) {
        const kept = state.unsent.filter((tile) => tiles.board[tile.square] === null)
        if (kept.length !== state.unsent.length) {
          state.unsent = kept
          state.preview = null
        }
      }
    })
  },
})

const {
  rackTapped,
  placed,
  takenBack,
  moved,
  blankAsked,
  blankChosen,
  blankCancelled,
  recalled,
  exchangeStarted,
  exchangeEnded,
  rackShuffled,
} = tilesUiSlice.actions

export const { previewReceived } = tilesUiSlice.actions
export const tapRackTile = rackTapped
export const chooseBlankLetter = blankChosen
export const cancelBlank = blankCancelled
export const recallTiles = recalled
export const startExchange = exchangeStarted
export const cancelExchange = exchangeEnded
export const returnToRack = takenBack

const wire = (unsent: Unsent[]) =>
  unsent.map(({ square, letter, blank }) => (blank ? { square, letter, blank: true as const } : { square, letter }))

export const dropRackTile =
  (rackIndex: number, square: number): AppThunk =>
  (dispatch, getState) => {
    const state = getState()
    const tiles = state.room.snapshot?.tiles
    const { unsent } = state.tilesUi
    if (!tiles || tiles.board[square] !== null || unsent.some((tile) => tile.square === square || tile.rackIndex === rackIndex)) {
      return
    }
    const letter = tiles.rack[rackIndex]
    if (letter === BLANK) {
      dispatch(blankAsked({ square, rackIndex }))
    } else if (letter !== undefined) {
      dispatch(placed({ square, rackIndex, letter, blank: false }))
    }
  }

export const tapSquare =
  (square: number): AppThunk =>
  (dispatch, getState) => {
    const { selected, unsent } = getState().tilesUi
    if (unsent.some((tile) => tile.square === square)) {
      dispatch(takenBack(square))
      return
    }
    if (selected !== null) {
      dispatch(dropRackTile(selected, square))
    }
  }

export const moveUnsent =
  (from: number, to: number): AppThunk =>
  (dispatch, getState) => {
    const state = getState()
    const board = state.room.snapshot?.tiles?.board
    if (!board || board[to] !== null || state.tilesUi.unsent.some((tile) => tile.square === to)) {
      return
    }
    dispatch(moved({ from, to }))
  }

export const shuffleRack =
  (random: () => number = Math.random): AppThunk =>
  (dispatch, getState) => {
    const order = [...getState().tilesUi.order]
    for (let i = order.length - 1; i > 0; i--) {
      const j = Math.floor(random() * (i + 1))
      ;[order[i], order[j]] = [order[j], order[i]]
    }
    dispatch(rackShuffled(order))
  }

// The screen calls this whenever the unsent tiles or the board change.
export const askPreview = (): AppThunk => (_dispatch, getState, { send }) => {
  const { unsent } = getState().tilesUi
  if (unsent.length > 0) {
    send({ type: 'tiles-preview', tiles: wire(unsent) })
  }
}

export const submitTiles = (): AppThunk => (_dispatch, getState, { send }) => {
  const { unsent } = getState().tilesUi
  if (unsent.length === 0) {
    return
  }
  send({ type: 'tiles-play', tiles: wire(unsent) })
}

export const sendExchange = (): AppThunk => (dispatch, getState, { send }) => {
  const state = getState()
  const rack = state.room.snapshot?.tiles?.rack ?? []
  const picked = state.tilesUi.exchanging ?? []
  if (picked.length > 0) {
    send({ type: 'tiles-exchange', letters: picked.map((index) => rack[index]).join('') })
  }
  dispatch(exchangeEnded())
}

export const passTiles = (): AppThunk => (_dispatch, _getState, { send }) => {
  send({ type: 'tiles-pass' })
}

export const rematchTiles = (): AppThunk => (_dispatch, _getState, { send }) => {
  send({ type: 'tiles-rematch' })
}
