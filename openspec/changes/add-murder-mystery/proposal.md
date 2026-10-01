## Why

Couples love solving a murder mystery together, and William's partner does in particular. Friends Corner has turn-based games (tic-tac-toe, chess, Letter Tiles) but nothing co-operative or story-driven. An OpenAI model can write a fresh case every game, so the mystery never repeats — as long as the server checks the case is fair before anyone plays it.

## What Changes

- A new lobby game, `mystery`, for exactly 2 players, played **co-operatively**: both players see the same case and win or lose together.
- When the countdown finishes, the room shows **"writing your case…"** while an OpenAI model writes a case as structured JSON: a setting, a victim, 4 suspects, 3 places with 2 clues each, 2 statements per suspect, and a hidden solution.
- A pure **fairness check** runs on every written case before play: exactly one killer, no clue clears the killer, every innocent is cleared by some clue, and at most 6 leads are enough to clear all three innocents. A case that fails is rewritten once; a second failure ends in "couldn't write a case — try again".
- **Play**: the pair has 8 leads to spend on 14 hidden leads (6 clues, 8 statements). Each lead opened is revealed to both.
- **Accusation**: each player picks a suspect; the accusation locks only when both pick the same one. Then the reveal shows the killer, the story, and the score (100 + 10 per unspent lead when right, 0 when wrong).
- **Rematch** writes a new case.
- The whole game, including the case, is saved after every accepted command and comes back after a restart.
- The OpenAI key stays out of the repo and the page: user secrets in development, a Worker secret passed to the API container in production.
- State messages for tic-tac-toe, chess, and Letter Tiles gain one field, `"mystery": null`.

**Included now**: co-op mode, 2 players, one fixed cozy tone, the case format already shaped for later modes.

**Later, not in this change**: rivals and role-play modes (same case format, different rules for who sees what), 3–4 players, a tone setting, a ready pool of pre-written cases, live interrogation of suspects, and a per-room cost cap beyond "one case written at a time".

## Capabilities

### New Capabilities

- `murder-mystery`: the case format, the fairness check, and the co-op rules — leads, joint accusation, reveal, score, rematch.
- `murder-mystery-room`: how the game runs in a room — the lobby entry, the writing phase and its failure, what each state message may and may not contain (never the solution before the reveal), commands from the page, and surviving a restart.
- `murder-mystery-screen`: what the players see — the writing screen, the briefing, suspects and places, leads left, opened leads, both players' picks, and the reveal.

### Modified Capabilities

- `game-seam`: existing rooms' state messages gain `"mystery": null`, alongside the existing `"tiles": null`.

## Impact

- **Core**: new `Engines/Mystery` (case types, fairness check, `MysteryGame : IGameEngine`); a new accessor interface for writing a case; `GameCatalog` and `LobbyEngine` learn `mystery`; `RoomManager` asks for a case when a mystery game is waiting for one.
- **Infrastructure**: an OpenAI case-writer accessor (HTTP, structured output), a `mystery_games` table and EF migration, state-message JSON for the `mystery` section.
- **Api**: reading `mystery-open`, `mystery-accuse`, `mystery-rematch`.
- **Frontend**: a lobby tile and a `MysteryScreen`.
- **Edge / deploy**: a new Worker secret `OPENAI_API_KEY`, passed to the container as `OpenAI__ApiKey`.
- **Cost**: one model call per case (two when a case is rewritten). No calls during play.
