# Slot Machine (Unity)

A classic 3-reel slot machine built in Unity. Pull the lever, watch the reels
scroll and clunk to a stop, and try to line up three of a kind for the jackpot.

![slot machine](docs/screenshot.png)
<!-- replace with a screenshot or gif of the running game -->

## Game Overview

This is a single-player slot machine game. You start with a balance of coins,
pick a bet, and pull the side lever to spin the three reels. Each reel scrolls a
strip of symbols (Cherry, Bell, BAR, Seven) and eases to a stop one after
another, left to right.

Payouts are tiered:

- **Three of a kind** → full payout (`bet × that symbol's multiplier`)
- **Two of a kind** → half your bet back, as a consolation
- **No match** → you lose the bet

Rarer symbols pay more and show up less often, so a row of Sevens is the
jackpot. A particle burst celebrates a three-of-a-kind win.

## Controls

- **Pull the lever** (right side of the machine) to spin.
- **Bet + / –** buttons to change your bet (10 / 50 / 100).
- Spins are blocked if you can't afford the current bet.

## How to Run the WebGL Build

You don't need Unity to play — the game runs in a browser from the WebGL build
in this repo.

**Option A — play locally:**

1. Clone or download this repository.
2. WebGL builds can't be opened directly as a file (browsers block it); they
   need to be served. From the `Build/WebGL` folder, start a simple local
   server, e.g. with Python:
   ```bash
   cd Build/WebGL
   python -m http.server 8000
   ```
3. Open `http://localhost:8000` in your browser.

**Option B — if hosted:**

If a hosted link is provided (e.g. GitHub Pages / itch.io), just open it:
`<your-hosted-link-here>`

> WebGL note: the first load may take a few seconds while the build decompresses.

## Bonus Features

Beyond the core requirements, this build adds:

- **Real scrolling reels** instead of simple sprite swaps — symbols slide and
  wrap with an ease-out stop for a more authentic feel.
- **Pull-lever spin** — a two-frame lever animation drives the spin, instead of
  a plain button.
- **Tiered payouts** — the two-of-a-kind half-bet refund on top of the standard
  three-of-a-kind win.
- **Win particle effect** — a burst that fires only on a jackpot and stays
  disabled otherwise.
- **Weighted, seedable RNG** — rare symbols are genuinely rarer, and spins can
  be seeded for reproducible testing.

## Thought Process / Approach

My main goal was to keep the four concerns of the game — randomness, animation,
win rules, and UI — completely separate, so each could be reasoned about and
changed on its own.

- **Outcome before animation.** The RNG decides every reel's result *before* the
  reels start spinning, mirroring how real slot machines work. The scrolling is
  purely cosmetic and always lands on the pre-decided symbol. This keeps the
  game fair and makes the animation code dumb and simple.
- **All randomness in one place.** `SlotRng` is the only source of randomness,
  it's weighted (so payouts can be balanced against rarity), and it's seedable
  so I could reproduce spins while debugging.
- **Win rules with no Unity dependency.** `PayoutEvaluator` is plain C# — it
  takes the reel symbols and the bet and returns a result. Because it doesn't
  touch Unity, the rules are trivial to follow and could be unit-tested in
  isolation.
- **Events between logic and UI.** `SlotMachine` exposes events
  (`OnCoinsChanged`, `OnSpinStarted`, `OnSpinCompleted`); `SlotUIManager` and
  `WinEffects` just listen. Neither the UI nor the effects know anything about
  the game rules, and the machine knows nothing about the UI. That separation
  made adding the lever and the particle effect late on painless — they just
  hooked into the existing event.

A spin, end to end: deduct bet → RNG picks results → reels animate and land →
`PayoutEvaluator` scores it → coins update → UI + particles react.

## Project Structure

```
Assets/
├─ Scripts/         # all gameplay C#
├─ Prefabs/         # reel / symbol prefabs
├─ Animations/      # lever / reel animation assets (if used)
├─ UI/              # canvas sprites: cabinet, buttons, popup, background
├─ Sounds/          # spin / win audio (if used)
├─ ScriptableObjects/ # the SymbolData assets
└─ Scenes/
    └─ SlotGame.unity
Build/
└─ WebGL/           # the playable WebGL build
```

### Scripts

| Script | Responsibility |
|--------|----------------|
| `SlotSymbol.cs` | `SymbolType` enum + `SymbolData` ScriptableObject (sprite, multiplier, spawn weight) |
| `SlotRng.cs` | Seedable weighted random selection |
| `Reel.cs` | One reel: scrolling animation + landing on a result |
| `PayoutEvaluator.cs` | Pure win/payout rules (no Unity dependency) |
| `SlotMachine.cs` | Main controller — RNG, reels, betting, payout, events |
| `SlotUIManager.cs` | Bridges UI (coins, bet, messages, win popup) to the machine |
| `LeverAnimator.cs` | The pull-lever; swaps sprites and triggers a spin |
| `WinEffects.cs` | Plays the particle burst on a jackpot |

## Built With

- Unity 6 (6000.0.67f1), 2D URP
- TextMeshPro for UI text