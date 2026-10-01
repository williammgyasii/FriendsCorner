## Context

`add-murder-mystery` shipped one co-op mode: shared 8 leads, the same pick reveals at once, no clock. Moves are pure (`TryPlay(seat, move)`), and the room ticks every 50 ms and broadcasts after every tick. The lobby owns what will be played (`Pick`); the catalog starts a game from the playing seats only.

## Goals / Non-Goals

**Goals:** settings chosen before the game and owned by the server; Hard with a clock; a final answer in Together; a Race mode; a screen that feels like the case; a dark app.

**Non-Goals:** changing settings mid-game (a rematch keeps them; back-to-lobby is its own change); more than two players; sound; a WebGL scene.

## Decisions

### 1. Settings live in the lobby, typed

`LobbyEngine.Mystery` is a `MysterySettings(Level, Mode)` record, set by the host with `SetMysterySettings`. It changes only before the countdown, and like a new pick it clears ready, because guests agreed to something else.

- *Rejected:* a string-to-string settings bag on the lobby. It's generic, but every reader would have to parse and validate strings; a typed record puts the rules in one place.
- *Rejected:* settings on the game. The game doesn't exist until the countdown ends, and guests must see the choice before readying.

The catalog's start function takes a `GameStart(Playing, Mystery)` instead of just the seats. The public `TryStart(id, playing)` stays for callers that have no settings.

### 2. Time enters the game only through the tick

Moves stay pure. `IGameEngine` gets a default `TryAdvance(DateTimeOffset now) => false`, and `RoomEngine.Advance` calls it every tick. A clock starts at the first tick that sees it:

- A Hard case that arrives sets `EndsAt` on the next tick (`now + minutes`).
- Two matching picks in Together set `Phase = Accusing`; the next tick sets `LocksAt = now + 5 s`.
- A tick past `LocksAt` reveals; a tick past `EndsAt` reveals as timed out.

The trade-off is that a clock starts up to 50 ms late, and the first broadcast of `Accusing` has no countdown number yet. In return, no move needs a clock, all the existing move tests keep their shape, and every timer is tested by calling `TryAdvance` with a chosen time.

- *Rejected:* passing `now` into `TryPlay`. That changes the seam for every game to serve one.

### 3. Leads remember who opened them

`Opened` becomes a list of `OpenedLead(By, Lead)`. Together counts all of them against one budget. Race counts each seat's own, and a seat's view only shows the text of leads it opened. This one shape also lets Together show "opened by your partner".

### 4. The view is built for one seat

`View(Seat you)` replaces `View()`. In Race it hides the partner's leads and pick until the reveal, and lists who is out. The message writer already builds one message per seat.

### 5. Race rules

- An accusation is final.
- Right: revealed, and that seat wins.
- Wrong: that seat is out and can't open or accuse. When every seat is out: revealed, no winner.
- The clock running out: revealed, no winner.
- Score: the winner's 100, plus 10 per lead they have left, plus 50 on Hard.

### 6. The writer picks minutes and mood

The schema gains `minutes` (an integer from 5 to 20) and `mood` (an enum of six).

- The engine clamps `minutes` anyway, because the engine owns fairness, not the writer.
- `mood` drives only the look.
- The level goes into the input (`Difficulty: hard`), and the prompt tells the writer what each level means. Easy: plain clues and one obvious contradiction. Hard: every innocent can still be cleared in 6 leads, but with no spare leads, a subtler lie, and more red herrings.

### 7. Saved state

The new fields go into the same jsonb row. A row saved in the old shape reads as nothing, and the room starts fresh. No release has shipped with the old shape, so no migration is needed.

### 8. The look: CSS 3D plus Motion, no WebGL

- The case folder, dossier cards and evidence cards are HTML with `perspective` and `transform-style: preserve-3d`.
- Leads flip with a CSS transition when their text arrives.
- Motion (already a dependency) drives the stamp and the countdown.
- The ambience is one absolutely positioned layer per mood, made of animated gradients. `prefers-reduced-motion` stops it.
- *Rejected:* a Three.js scene. It's heavy on phones and adds nothing for reading text-heavy cards.

The mood maps to a palette in CSS (`[data-mood]`), so the frontend never guesses from the title.

### 9. Dark app

The Tailwind tokens in `index.css` and the old screens' literal colours in `style.css` move to dark values. Seat colours stay the same so faces and marks keep their meaning.

The lobby cards get a 5:7 portrait shape, a 6 px radius, a thin inner frame and corner pips.

## Risks / Trade-offs

- **The case can take 30–60 s to write.** The writing screen cycles through what the writer is doing, so the wait reads as progress. Prefetching during the lobby countdown saves only 3 s.
- **A hard case might be too hard.** The fairness check still guarantees a 6-lead solution; the bonus rewards the risk.
- **Race leaks a little.** A player can see that their partner is out, though not whom they accused.
