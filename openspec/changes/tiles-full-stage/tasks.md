## 1. The look

- [x] 1.1 Write failing tests in `tilesLook.test.ts`:
  - The status reads "Your turn · the first word must cover the star" on an empty board, and "Your turn" once a tile is down.
  - With unsent tiles, a kept preview makes the status "CAT for 10"; a refused preview makes it the refusal text.
  - The ending still wins over everything.
  - `gain` is the last play's score on the seat that made it, and null on every other seat and before any play.
- [x] 1.2 Implement the hint order and `gain` in `describeTiles`.

## 2. The screen

- [x] 2.1 Write failing tests in `tilesScreen.test.tsx`:
  - After a play, the scorer's panel shows "+10".
  - The status line shows the first-move hint.
  - With `document.fullscreenEnabled` true, "Full screen" calls `requestFullscreen`, and after `fullscreenchange` the button reads "Leave full screen" and calls `exitFullscreen`.
  - With it false, there is no full-screen button.
- [x] 2.2 Add `Scoreboard` panels, `CountUp`, the gain chip, `FullScreenButton`, landing and refill motion, and the stage CSS for phone and desktop.

## 3. End to end

- [x] 3.1 In a real browser, check at 390 by 844 and 1440 by 900 that all 225 squares, the panels, the rack, and Submit are inside the viewport with no overlaps, and take a screenshot of each. Play a live game against a word-playing bot and see the gains, the hint, and the counts. Run both suites and `npm run build`.
- [x] 3.2 Give the board the space. Measure first (desktop board 76% of the height, phone board starting 297 pixels down), then make faces small rounded rectangles, move the desktop actions beside the board, and pin the phone board under a compact header. Re-measure against "The board on a desktop" and "The board on a phone".
- [ ] 3.3 Walk William through the code; leave commit and archive for his approval.
