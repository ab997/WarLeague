---
description: Rules and patterns for deck submission commands and services
applyTo:
  - "**/DeckCommands.cs"
  - "**/DeckSubmission*.cs"
---

# Deck Submission Rules

## SeatNumber Logic
- SeatNumber is REQUIRED for all deck submissions
- SeatNumber must be validated in range [1, Week.SubmissionsRequired]
- When generating pairings, NEVER randomize - pair strictly by SeatNumber (seat 1 vs seat 1, etc.)
- Sort by SeatNumber using `OrderBy(ds => ds.SeatNumber)` in RoundRobin.Run

## Validation Layers

### Authorization (Commands Layer Only)
- Check if caller is Admin OR Captain of target player's team
- Validate target player is on a team for the active season

### Business Logic (Service Layer)
- Check seat conflict: prevent different players from using same seat number
- Allow upsert: same player can update their deck/seat if new seat is available
- Validate Week status is Open for submissions