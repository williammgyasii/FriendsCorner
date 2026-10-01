## Context

See proposal.md for why. Today `doorLook` sends a signed-in visit with no room to a lobby view whose action is `Open a lobby`. `boot` then asks `billingLook`. A `?plan=` with no stored plan starts checkout. Otherwise `renderLobby` prints that action, the plan title, and `Manage plan` when a plan exists. The button calls `POST /rooms` and sets `?room=<id>`. `GET /account` already returns `gameName` and `plan`.

## Goals / Non-Goals

**Goals:**

- Replace that screen with the home view from the spec.
- Keep room entry and checkout in front of the home.
- Keep room creation on the existing `POST /rooms` call.

**Non-Goals:**

- A billing page, a settings destination, friends, groups, or metrics.
- A new API, table, or engine. There is no new rule for the server to own.

## Decisions

### `homeLook` owns the home view

`homeLook(gameName, plan)` returns the game name, the plan label `Corner`, `Table`, or `House` (or null), the action `Start a session`, and the failure text `Could not open it. Try again`. It does not read the URL, the cookie, or the network.

`billingLook` keeps checkout. `doorLook` keeps the door and `?room=` entry. A signed-in visit with no room returns `{ kind: 'home' }` from `doorLook` instead of the lobby action, so that action cannot be rendered by mistake.

`boot` orders the calls: redirect, enter the room, the door, then checkout, then the home. The home is the last screen.

Alternative: fold the home into `billingLook`. That function already decides checkout, and the home will change again when billing and friends arrive. A separate look keeps those rates apart.

### The page only renders the view

The home markup reads `homeLook`. The button reuses the current create-room handler: success sets `?room=` to the returned id, and failure sets the button text from the view. `boot` reads `gameName` from the account response it already fetches. An unknown plan string is treated as no plan.

### The home uses the existing door screen

The copy changes. The arcade card stays on `/login` and `/register`. A restyle of the home waits until the later dashboard.

### `Manage plan` is not on the home

`billingLook` may still describe `Manage plan`. The home does not render it. The portal call stays for the later billing page.

## Risks / Trade-offs

- [A signed-in person loses the manage button] → Accepted. Billing is the next slice. The plan name is still visible.
- [`doorLook` tests still expect `Open a lobby`] → Those assertions change with the home kind. Room entry stays.
- [Checkout is skipped if the home returns early] → `boot` asks `billingLook` before it renders the home.

## Migration Plan

No data migration. The screen changes with the play app. Rolling back is the previous screen.
