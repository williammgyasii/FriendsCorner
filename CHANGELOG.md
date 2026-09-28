# Changelog

What ships in each release. Every merged PR adds its lines under **Unreleased**; a release renames that section to the version and publishes it as the GitHub Release notes.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/).

## Unreleased

### Changed
- Production ships when a GitHub Release is published, not when a tag is pushed. The Release notes come from this file.

## v0.1.1 — 2026-09-28

### Changed
- CI and CD are split per area: `backend-ci`/`backend-cd`, `frontend-ci`/`frontend-cd`, `terraform-ci`/`terraform-cd`. A PR runs only the CI for the areas it touches.
- A release ships only the areas that changed since the last release, in order: backend, then frontend, then Terraform. A failure stops everything after it.

## v0.1.0 — 2026-09-28

### Added
- Letter Tiles: a word game on a 15×15 board, scored by the server against the ENABLE word list.
- A live score preview while placing tiles, answered only to the player who asked.
- A full-screen tiles stage where the board takes most of the window and each player's face fills their panel.
- A game seam, so tic-tac-toe, chess, and Letter Tiles plug into the room the same way.
- Saved tiles games, so a room comes back after a restart.

### Changed
- Backend CI runs the database tests against a throwaway Postgres.
