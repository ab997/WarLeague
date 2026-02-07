# TODO

- [x] Make notes about team in InfoCommands
- [x] Bot should handle roles and colours for teams
- [x] Add command for Captain to update team color
- [x] Before season starts, validate that all teams have a minimum amount of members
- [x] Disable team creation when season starts (when first week is opened for submissions)
- [ ] Add a system for reporting messages in a separate channel
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
- [ ] Decide which commands should be ephemeral (private)
- [x] Deck submission:
  - [x] Seat number is a parameter
  - [x] Do not randomize pairings
  - [x] Pair strictly by seat number
- [x] Deck submissions:
  - [x] Update commands to explicitly handle seats and players
- [X] Deck submission validation:
  - [x] A player can only be submitted once per week
- [ ] Substitution command:
  - [ ] Mid-week admin can substitute a player
  - [ ] Replacement must be from the same team
  - [ ] Replacement must not already be submitted for that week
- [ ] Write down bye week somehow in the database
- [ ] No pinging for peep commands
- [ ] Peep all results:
  - [ ] Remove attachments

## TECHNICAL

- [ ] Add logging
- [ ] Add deck validation:
  - [ ] Format-specific
  - [ ] Banlist
  - [ ] Card pool
- [ ] Prepare for conferences (e.g. teams grouped by conference)
- [ ] Discord roles are not unique by name -> we need to migrate from using "admin" and "captain" to integer IDs.
- [ ] When it makes sense (such as for example when chosing between existing finite choices -> team names, players in team, etc) limit the suggestions in UI
- [ ] database daily backup
- [ ] Return-null-as-failure is a design smell. return result instead.