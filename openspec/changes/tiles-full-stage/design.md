## Context

`TilesScreen` renders a header (status line and a `Scores` list of badges), a plays aside, the board cell, and the tray. `describeTiles` in `tilesLook.ts` is the pure view model; the screen only draws it. Faces reach the badges through `faceHosts`, which looks for `[data-face]` inside `#tiles-world`. `motion` is already a dependency for drag.

## Goals / Non-Goals

**Goals:** a full stage on phone and desktop, a hint pill, a scoreboard with gains and counting scores, landing and refill motion, a full-screen button where allowed.

**Non-Goals:** powers, back-to-lobby, sound, any server change.

## Decisions

1. **The hint is the status line.** One `role="status"` element, so a screen reader hears one message per change. The text is decided in `describeTiles` (pure, tested) in this order: ending, preview refusal, previewed words and score, "Your turn · the first word must cover the star" on an empty board, "Your turn", "B is thinking". The refusal texts are the ones the server's refusal reasons already map to. Alternative: a second hint element. Rejected: two live regions talk over each other.
2. **The scoreboard keeps one `Scores` list.** Each player stays one list item, so the tests and assistive tech keep one structure; only CSS moves the panels (side by side on a phone, stacked in the left column on a desktop). The right column keeps the last play and the bag and is where powers will go. Alternative: split "you" and "others" into two lists placed in two columns. Rejected: two lists for one scoreboard.
3. **Faces beside the board, not over it.** Video over the board would hide edge squares. The panels are the `[data-face]` hosts, so `faceHosts` does not change.
4. **Gain is data; its timing is the component's.** `PlayerView.gain` is the last play's score for the seat that made it, else null. The component shows "+N" keyed on the last play, and it fades out. Keeping the time out of the view model keeps `describeTiles` pure.
5. **CountUp renders the true value first.** A new score is shown immediately in the accessible text; the digits animate from the old value with `motion`'s `animate`. Reduced motion (`useReducedMotion`) skips the animation. Tests read the final number.
6. **Landing motion rides on mounting.** A placed tile's span mounts when it lands (an unsent tile is a different element), so `initial`/`animate` on mount springs it in with no board diffing. Rack tiles are keyed by index and letter, so a refilled slot remounts and slides in.
7. **Full screen by feature detection.** The button renders only when `document.fullscreenEnabled` is true (iPhone Safari is false for the page). It toggles `requestFullscreen()` on the document element and `exitFullscreen()`, and follows `fullscreenchange` for its label.

## Risks / Trade-offs

- [Motion in jsdom] → tests assert final text and attributes, never mid-animation frames.
- [Desktop height] → the board is sized by its container query, so bigger panels shrink the side columns, not the board.
