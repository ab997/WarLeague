# TODO

- [x] Make notes about team in InfoCommands
- [x] Bot should handle roles and colours for teams
- [x] Add command for Captain to update team color
- [x] Before season starts, validate that all teams have a minimum amount of members
- [x] Disable team creation when season starts (when first week is opened for submissions)
- [x] Number of submissions per week:
  - [x] Not a command parameter
  - [x] Stored as a column on the week
- [x] `admin-transfer-captain`: add message  
      “Please remember to manually change the role”
- [x] Replace error  
      `"Module precondition group Permission failed."`  
      with a clearer message (e.g. “You must be a captain to use this command”)
- [x] Admin must be able to manage week status
- [x] Remove all logic for week start / end / submission  
      Week dates are display-only; rely **solely on week status** for logic
- [x] Match: `no-show` command → no replay
- [x] Week delete command is missing
- [x] Week: add nice errors when week is updated to a status that is prevented by a unique constraint
- [x] Week rules:
  - [x] One week may be `InProgress`
  - [x] One `InProgress` week may coexist with another week that is `Open` **or** `SubmissionClosed`
- [x] Deck submission:
  - [x] Seat number is a parameter
  - [x] Do not randomize pairings
  - [x] Pair strictly by seat number
- [x] Deck submissions:
  - [x] Update commands to explicitly handle seats and players
- [X] Deck submission validation:
  - [x] A player can only be submitted once per week
- [x] Substitution command:
  - [x] Mid-week admin can substitute a player
  - [x] Replacement must be from the same team
  - [x] Replacement must not already be submitted for that week
- [x] No pinging for peep commands
- [x] Peep all results:
  - [x] Remove attachments
- [ ] Decide which commands should be ephemeral (private)
- [ ] Add a system for reporting messages in a separate channel

## BYE WEEKS & PLAYOFFS & CONFERENCES

- [ ] Add TeamWeekBye table (TeamId, WeekId) for explicit bye tracking
- [ ] Add Conference table (Id, SeasonId, Name)
- [ ] Add ConferenceId (nullable) to Team table
- [ ] Add PlayoffMatchup table (SeasonId, Round, BracketPosition, Team1Id, Team2Id, WinnerTeamId, AdvancesToMatchupId, Status)
- [ ] Update Match table: WeekId nullable, add PlayoffMatchupId nullable
- [ ] Update RoundRobin pairing to filter by conference when applicable
- [ ] Implement SingleEliminationPairing strategy for playoffs
- [ ] Update MatchService.GeneratePairingsAsync to handle conference-filtered pairings
- [ ] Playoff bracket creation and advancement logic
- [ ] Database constraints:
  - [ ] Match: Check constraint - exactly one of WeekId or PlayoffMatchupId must be set (not both null, not both set)
  - [x] Match: Check constraint - Player1Id != Player2Id (cannot play yourself)
  - [x] DeckSubmission: Unique index on (PlayerId, WeekId) - player can only submit once per week
  - [ ] Conference: Unique index on (SeasonId, Name)
  - [ ] PlayoffMatchup: Unique index on (SeasonId, Round, BracketPosition)
  - [ ] TeamWeekBye: Unique index on (TeamId, WeekId) if implemented

## TECHNICAL

- [x] Add logging
- [ ] Add audit columns
- [ ] Add deck validation:
  - [ ] Format-specific
  - [ ] Banlist
  - [ ] Card pool
- [x] Discord roles are not unique by name -> we need to migrate from using "admin" and "captain" to integer IDs.
- [ ] When it makes sense (such as for example when chosing between existing finite choices -> team names, players in team, etc) limit the suggestions in UI
- [ ] database daily backup
- [x] Return-null-as-failure is a design smell. return result instead.
- [x] each week transition should check that IN status and OUT status are ok
- [x] a way to add teams in tests so that we can test all week transition (close submissions)