---
description: Rules and patterns for player substitution commands and services
applyTo:
  - "**/SubstitutionCommands.cs"
  - "**/SubstitutionService.cs"
---

# Player Substitution Rules

## Week Status Requirement
- Substitutions can ONLY occur during InProgress weeks
- Query week using `WeekStatus.InProgress`

## Validation Layers

### Commands Layer
- Admin-only operation (no captain permission)
- Defer the response before processing

### Service Layer Business Logic
All validations use guard clauses and return BaseResult on failure:

1. **Team Validation**: Team must exist in the active season
2. **Player Membership**: Both playerIn and playerOut must be on the specified team
3. **PlayerIn Availability**: Must NOT already be playing in any match this week
4. **PlayerOut Scheduled**: Must have exactly one Scheduled (unreported) match this week
5. **Deck Submission**: Must exist for playerOut (return error if missing)

## Actions (Service Layer)
When all validations pass, perform BOTH updates in order:
1. Update Match: Replace playerOut with playerIn in the scheduled match
2. Update DeckSubmission: Transfer playerOut's deck to playerIn