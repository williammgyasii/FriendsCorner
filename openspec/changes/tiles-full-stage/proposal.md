## Why

Playing a real game showed the Letter Tiles screen works but feels flat next to the phone game we play with friends. The faces are small badges, the status line says little, nothing moves when a word lands, and the page cannot go full screen. William asked for a full stage: faces large and beside the board, a proper scoreboard, a hint line that guides the turn, animations, and a full-screen button.

## What Changes

- The status line becomes a hint pill that guides the turn: the ending, then the preview's refusal while placing ("The first word must cover the star"), then the previewed words and score ("CAT for 10"), then "Your turn" with a first-move hint on an empty board, then "B is thinking".
- The badges become a scoreboard of large face panels. On a phone the panels sit side by side above the board; on a desktop they stack in the left column beside the board. The seat to move glows.
- A "+N" gain pops on the panel of the seat that just scored, and scores count up to their new value.
- Tiles spring onto the board when they land, and new rack tiles slide in. All motion is dropped when the device asks for reduced motion.
- A full-screen button appears only where the browser allows it (`document.fullscreenEnabled`). Everywhere the page already fills the window.
- No backend change. No new message.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `letter-tiles-screen`: the score panel becomes a scoreboard with gains, the status line becomes a hint pill, and the page gains a full-screen button and motion.

## Impact

- `frontend/src/tilesLook.ts` (hint text, gain per player)
- `frontend/src/tiles/TilesScreen.tsx` (Scoreboard, CountUp, FullScreenButton, motion)
- `frontend/src/style.css` (stage layout)
- Tests: `tilesLook.test.ts`, `tilesScreen.test.tsx`
- Powers (hint, swap, word radar) are a separate change, `add-tiles-powers`.
