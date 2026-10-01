## 1. The case and its fairness check (Core, pure)

- [x] 1.1 Write failing tests in `Tests/Core/Mystery/CaseCheckTests.cs` with a `CaseFixture` that builds a fair case (killer `s3`; `c1`+`t2` prove `s1`, `t3` proves `s2`, `c5`+`t8` prove `s4`; `t5` is the killer's lie):
  - The fixture case is Ok.
  - 7 statements, 3 suspects, a clue at an unknown place, a 401-character clue, a proof naming an unknown lead, and a lie on a clue are each rejected as malformed.
  - A proof that clears the killer, an innocent with no proof, a case needing 9 leads to clear everyone, a killer with no lie, and an innocent with a lie are each rejected as unfair, naming the rule.
- [x] 1.2 Add `Case`, `Suspect`, `Location`, `Lead`, `Proof`, `Solution`, `CaseVerdict`, and `CaseCheck` in `Core/Engines/Mystery`.

## 2. Playing a case (Core, pure)

- [x] 2.1 Write failing tests in `Tests/Core/Mystery/MysteryGameTests.cs`:
  - A new game is `Writing` for request 1 and has 8 leads.
  - `TryReceive(1, case)` makes it `Investigating`; `TryReceive(2, case)` and a second `TryReceive(1, ...)` are refused.
  - Opening `c1` spends a lead and opens it; opening it again, an unknown id, and a 9th lead are refused.
  - Different picks don't reveal; the same pick locks and reveals; changing a pick to match locks.
  - Right with 5 opened scores 130; wrong scores 0; after the reveal, open and pick are refused.
  - Rematch is refused while `Writing` and `Investigating`, and after the reveal or `TryFail` returns to `Writing` for request 2 with 8 leads and no picks.
- [x] 2.2 Implement `MysteryGame : IGameEngine, IAwaitsCase` with `OpenLead` and `Accuse` moves.
- [x] 2.3 Write failing tests for `View()`: before the reveal it has no solution, no closed-lead text, no proofs, and no lie flags; after the reveal it has the outcome.
- [x] 2.4 Implement `MysteryView` and `View()`.

## 3. In the room (Core)

- [x] 3.1 Write failing tests: the lobby offers `mystery` for 2; the countdown finishing launches it in `Writing`; the other games' tests still pass.
- [x] 3.2 Add `mystery` to `LobbyEngine.Games` and `GameCatalog`.
- [x] 3.3 Write failing tests in `RoomManagerTests` with a fake `ICaseWriterAccessor` and a fake `TimeProvider`:
  - A fair case arrives → both seats get an `investigating` message; the writer was asked once, with a theme.
  - Unfair then fair → asked twice, `investigating` with the second case.
  - Two unfair, or `null` twice → asked twice, `failed`.
  - No answer for 60 seconds → `failed`.
  - A move from a seat is handled while the writer is still working (the gate is not held).
  - A case for request 1 arriving after a rematch started request 2 is dropped.
- [x] 3.4 Add `ICaseWriterAccessor`, `CaseRequest`, the theme list, and the write sequence in `RoomManager`.

## 4. OpenAI and the page messages (Infrastructure, Api)

- [x] 4.1 Pick the model from OpenAI's current models that support structured outputs; record it in `design.md`.
- [x] 4.2 Write failing tests for `OpenAiCaseWriterAccessor` with a stub `HttpMessageHandler`: the request has the key as a bearer token, the model, and the strict schema; a good reply maps to the fixture case; an HTTP 500, a refusal, and a reply that breaks the schema each return `null`; with no key it returns `null` without calling out.
- [x] 4.3 Implement the accessor, the prompt and schema resources, and registration from `OpenAI:ApiKey` and `OpenAI:Model`.
- [x] 4.4 Write failing tests: the state message has the `mystery` section for a mystery room and contains nothing `View()` hides; chess, tic-tac-toe, and tiles messages gain only `"mystery":null`; `ClientMessageReader` reads `mystery-open`, `mystery-accuse`, `mystery-rematch` and ignores malformed ones.
- [x] 4.5 Implement the JSON writer and reader.

## 5. Saved games (Infrastructure)

- [x] 5.1 Write failing tests against the Postgres test database: a mid-investigation game comes back with the same open leads, leads left, and picks; a game saved while `Writing` comes back `Failed`.
- [x] 5.2 Add the `mystery_games` row, the EF migration, and the recorder and table accessors.

## 6. The page (Frontend)

- [x] 6.1 Write failing tests in `mysteryLook.test.ts`: writing, failed, case board with leads left, closed leads disabled at 0, "You" and partner marks, "Pick the same suspect to accuse", and the reveal text.
- [x] 6.2 Implement `mysteryLook.ts`.
- [x] 6.3 Write failing tests in `mysteryScreen.test.tsx`: tapping a closed lead, Accuse, Try again, and New case send the right messages; the lobby shows a Murder Mystery tile.
- [x] 6.4 Implement `MysteryScreen`, `mount.tsx`, the lobby tile, the store commands, and the CSS.

## 7. Shipping and end to end

- [x] 7.1 Add `OPENAI_API_KEY` to the API Worker's `Env` and `envVars`, and note the one-time `wrangler secret put`.
- [x] 7.2 In a real browser with a real key, play a full case at 390 by 844 and 1440 by 900 with two tabs: writing screen, open leads, disagree, agree, reveal, new case. Run both suites and `npm run build`.
- [ ] 7.3 Walk William through the code; leave commit, push, and archive for his approval.
