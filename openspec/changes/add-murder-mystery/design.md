## Context

- Games plug in through `IGameEngine` and start in `GameCatalog.TryStart`, which is synchronous and runs inside `RoomEngine.Advance` when the lobby countdown finishes. `RoomManager` holds a lock (`_gate`) around every engine call and broadcasts outside it.
- Every other game is ready the instant it starts. A mystery is not: its case comes from a model call that takes seconds, costs money, can fail, and can return nonsense. That is the one new kind of volatility here.
- Letter Tiles set the pattern this follows: an immutable state in `Core/Engines/<Game>`, a table saved as `jsonb`, a section in the state message written by `StateMessageAccessor`, a `GameQuestion` seam that is not needed here, and a `tiles/` folder on the page with a pure `tilesLook.ts`.
- In production the API container gets its settings from the API Worker's `envVars` (today only `ConnectionStrings__FriendsCorner` from the `DATABASE_URL` secret). OpenAI is not used anywhere yet.
- Players never type anything that is sent to the model, so there is no prompt-injection surface in this change.

## Goals / Non-Goals

**Goals:**

- The engine stays pure: no HTTP, no clock, no await. Everything about "is this case fair" and "what may a seat see" is testable with no network.
- The model call is one accessor that can be replaced by a fake in every test.
- The room keeps ticking and broadcasting while a case is written.
- The case format already fits the later rivals and role-play modes.

**Non-Goals:**

- Other modes, 3–4 players, a tone setting, a case pool, live interrogation, per-room cost caps beyond one write at a time.
- Judging whether a case is *good*. The check guarantees *fair*; quality is the prompt's job and is judged by playing.

## Decisions

### 1. The engine owns the phase; the manager owns the wait

```
Advance / rematch                         RoomManager (outside the gate)
      |                                            |
      v                                            |
MysteryGame: phase = Writing(request n) --------> sees a game waiting for case n
                                                   |
                                        ICaseWriterAccessor.Write()   (<= 60 s)
                                                   |
                                        CaseCheck.Check(case)  -- unfair? ask once more
                                                   |
      +---------------- under the gate -----------+
      v
MysteryGame.TryReceive(n, case)  -> Investigating      or   TryFail(n) -> Failed
      |
      v
Record + Broadcast
```

- `MysteryGame` (Core, Engine) has phases `Writing`, `Investigating`, `Revealed`, `Failed`. Entering `Writing` bumps a request number.
- It implements a small interface, `IAwaitsCase { int? WaitingFor { get; } bool TryReceive(int request, Case c); bool TryFail(int request); }`, in `Core/Engines/Mystery`. `RoomManager` checks `_room.Game is IAwaitsCase { WaitingFor: { } n }` after every `Apply` and `Advance` that changed the game, and starts one write for `n` if none is running for it.
- The request number makes a late answer harmless: a case for request 1 arriving after a rematch started request 2 is refused by the engine, not by manager bookkeeping.
- **Why not start the game only after the case arrives?** The lobby, countdown, and world would need a new "pending" state that every game would see. Keeping `Writing` inside the mystery engine touches no other game.
- **Why the manager and not a new manager?** It is one short sequence (write → check → maybe once more → hand over) that needs the room's gate, recorder, and broadcast. If a pool or background writer arrives later, that sequence moves to its own manager then.

### 2. Fairness is a pure engine: `CaseCheck`

`CaseCheck.Check(Case) -> CaseVerdict` (Ok, or the first rule broken) in `Core/Engines/Mystery`. Shape rules first (counts, ids, lengths), then the five fairness rules from the spec. Rule 4 ("some 6 leads hold a whole proof for every innocent") tries every subset of at most 6 of the 14 leads — 6,476 subsets, microseconds — so it is exact, not a greedy guess. The model is told the same rules in its prompt, so most cases pass the first time; the check is what we *trust*.

**Proofs, not per-lead clears.** A first draft gave each lead a list of suspects it clears. That made rule 4 impossible to break: rule 3 already gives each innocent one clearing lead, so 3 leads always suffice. A proof is a set of 1–3 leads that clear a suspect only together (an alibi plus the witness who backs it), which is how deduction works and gives rule 4 something to check. Proofs sit on the case, not on the leads, so hiding them is one omission in `View()`.

### 3. The secret boundary is in the engine: `MysteryView`

`MysteryGame.View()` returns a `MysteryView` that contains only what a player may see: lead text only when open, no proofs, no lie flags, and the solution only when `Revealed`. `StateMessageAccessor` writes JSON from `MysteryView` and never sees the `Case`. The "never gives the case away" requirement is then a Core test on `View()`, plus one Infrastructure test that the JSON contains nothing the view doesn't.

### 4. The model call: `ICaseWriterAccessor` → OpenAI over `HttpClient`

- Core declares `ICaseWriterAccessor { Task<Case?> Write(CaseRequest request, CancellationToken ct); }`. `CaseRequest` carries a `theme` picked by the manager from a fixed list with the injected `Random`, so cases vary without the model choosing the same manor house every time.
- Infrastructure's `OpenAiCaseWriterAccessor` calls the Responses API with a strict JSON schema (structured outputs), maps the reply into Core's `Case`, and returns `null` on any HTTP error, refusal, or schema mismatch. It never throws into the room.
- **Plain `HttpClient` + `System.Text.Json`, not the OpenAI .NET SDK**: one endpoint, one schema, and a stub `HttpMessageHandler` tests it end to end without the network. The SDK is the alternative if we add streaming or more endpoints later.
- The prompt and schema live as embedded resources next to the accessor (`Resources/mystery-prompt.md`, `Resources/mystery-case.schema.json`), like the word list.
- Settings: `OpenAI:ApiKey` (secret) and `OpenAI:Model` (in `appsettings.json`). The model is `gpt-5.5-2026-04-23`, picked on 2026-09-28 from the key's `/v1/models` list (which ran from `gpt-5` to `gpt-6.1-sol`): documented for structured outputs, a strong enough reasoner to build a fair puzzle, and a dated snapshot so it doesn't drift under us. It is a config value, not code; change `OpenAI:Model` to try another.
- Reasoning effort is `low` (`OpenAI:Effort`), from a live probe of 3 themes each, every case run through `CaseCheck`:

  | Model, effort | Seconds | Fair |
  |---|---|---|
  | gpt-5.5, default (medium) | 55–75 | 3/3 |
  | gpt-5.5, low | 22–25 | 3/3 |
  | gpt-5.4-mini, medium | 46–82 | 3/3 |
  | gpt-5.4-mini, low | 27–28 | 3/3 |

  Medium effort regularly crosses the 60-second limit; low effort is fair every time at well under half of it.
- With no key, registration wires a writer that returns `null` at once, so the game fails cleanly and nothing else changes.

### 5. Timeouts and retries belong to the manager

60 seconds per attempt via a `CancellationTokenSource` on the injected `TimeProvider`, so tests don't wait 60 seconds. The token goes to the writer (so the HTTP call is cancelled, not just abandoned) and to `WaitAsync` (so a writer that ignores it still can't hold the room). A `null`, unfair, or throwing answer is retried once; a timeout fails at once, because a second 60-second wait is too long for players staring at "writing".

### 6. Saved like Letter Tiles

A `mystery_games` table (`room_id` primary key, `state` jsonb, the same two columns as `letter_tiles`) with an EF migration; `GameRecorderAccessor`, `GameTableAccessor`, and the shutdown flush learn the mystery row. Restore turns `Writing` into `Failed` (the write that was in flight died with the process).

### 7. Secrets in production

`edge/api`: add `OPENAI_API_KEY` to the Worker's `Env` and `envVars = { ..., OpenAI__ApiKey: this.env.OPENAI_API_KEY }`; set it once with `wrangler secret put OPENAI_API_KEY -c api/wrangler.jsonc`. Development uses `dotnet user-secrets set OpenAI:ApiKey`, which already exists.

### 8. The page mirrors Letter Tiles

`frontend/src/mysteryLook.ts` (pure: message → what to draw, including "Pick the same suspect to accuse"), `frontend/src/mystery/MysteryScreen.tsx`, `frontend/src/mystery/mount.tsx`, a lobby tile in `GameGrid` via `lobbyLook.ts`, and the socket commands in the store.

### Who owns each step

| Step | Owner | Layer |
|---|---|---|
| Lobby offers `mystery` for 2 | `LobbyEngine.Games` | Engine |
| Start in `Writing` | `GameCatalog` → `MysteryGame.Start` | Engine |
| Notice a game waiting for a case | `RoomManager` | Manager |
| Write a case | `ICaseWriterAccessor` / `OpenAiCaseWriterAccessor` | Access |
| Is it fair? | `CaseCheck` | Engine |
| Retry, timeout | `RoomManager` | Manager |
| Open, accuse, reveal, score, rematch | `MysteryGame` | Engine |
| What a seat may see | `MysteryGame.View()` | Engine |
| JSON out / commands in | `StateMessageAccessor` / `ClientMessageReader` | Access / Client |
| Save / restore | mystery table accessors | Access |

## Risks / Trade-offs

- **Fair but dull cases** → judged by playing; the prompt and theme list are resources we can tune without code changes.
- **Latency (10–30 s)** → a clear writing screen; a case pool is the known next step if the wait feels bad.
- **Cost** → one call per case, two at most; no calls during play; one write per room at a time.
- **Model or API changes** → the model is config; the schema is strict and a mismatch becomes `failed`, never a crash.
- **Content** → the prompt asks for a cozy tone, no graphic violence, no sexual content, no real people; the reveal text is capped at 1,200 characters.
- **A second lock-holder**: the write runs outside the gate and only re-enters it to hand the case over, so a slow model never blocks moves, ticks, or other rooms.
