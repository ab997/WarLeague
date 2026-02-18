using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Data;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;

namespace WarLeague.Test
{
    public static class Seed
    {
        public static void SeedData(WarLeagueDbContext DbContext)
        {
            // Create Formats
            var hatFormat = new Format
            {
                Name = "HAT",
                Rules = "{}",
                SingleFormatMode = true
            };

            var goatFormat = new Format
            {
                Name = "GOAT",
                Rules = "{}",
                SingleFormatMode = false
            };

            var edisonFormat = new Format
            {
                Name = "Edison",
                Rules = "{}",
                SingleFormatMode = false
            };

            DbContext.Formats.AddRange(hatFormat, goatFormat, edisonFormat);
            DbContext.SaveChanges();

            // Create Players
            var player1 = new Player { DiscordUserId = 111111111111111111, UserName = "Player1" };
            var player2 = new Player { DiscordUserId = 222222222222222222, UserName = "Player2" };
            var player3 = new Player { DiscordUserId = 333333333333333333, UserName = "Player3" };
            var player4 = new Player { DiscordUserId = 444444444444444444, UserName = "Player4" };
            var player5 = new Player { DiscordUserId = 555555555555555555, UserName = "Player5" };
            var player6 = new Player { DiscordUserId = 666666666666666666, UserName = "Player6" };
            var player7 = new Player { DiscordUserId = 777777777777777777, UserName = "Player7" };
            var player8 = new Player { DiscordUserId = 888888888888888888, UserName = "Player8" };

            DbContext.Players.AddRange(player1, player2, player3, player4, player5, player6, player7, player8);
            DbContext.SaveChanges();

            // Create Season
            var season1 = new Season
            {
                SeasonNumber = 1,
                FormatId = hatFormat.Id,
                Active = true,
                DisableTeamModification = false,
                MinimumTeamMembers = 3
            };

            DbContext.Seasons.Add(season1);
            DbContext.SaveChanges();

            var defaultConference = new Conference
            {
                SeasonId = season1.Id,
                Name = "Default"
            };

            DbContext.Conferences.Add(defaultConference);
            DbContext.SaveChanges();

            // Create Teams
            var team1 = new Team
            {
                Name = "Team Alpha",
                CaptainId = player1.Id,
                SeasonId = season1.Id,
                ConferenceId = defaultConference.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                DiscordRoleId = 123456789012345678
            };

            var team2 = new Team
            {
                Name = "Team Beta",
                CaptainId = player5.Id,
                SeasonId = season1.Id,
                ConferenceId = defaultConference.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                DiscordRoleId = 234567890123456789
            };

            DbContext.Teams.AddRange(team1, team2);
            DbContext.SaveChanges();

            // Assign Players to Teams for the Season
            var pst1 = new PlayerSeasonTeam { PlayerId = player1.Id, SeasonId = season1.Id, TeamId = team1.Id };
            var pst2 = new PlayerSeasonTeam { PlayerId = player2.Id, SeasonId = season1.Id, TeamId = team1.Id };
            var pst3 = new PlayerSeasonTeam { PlayerId = player3.Id, SeasonId = season1.Id, TeamId = team1.Id };
            var pst4 = new PlayerSeasonTeam { PlayerId = player4.Id, SeasonId = season1.Id, TeamId = team1.Id };
            var pst5 = new PlayerSeasonTeam { PlayerId = player5.Id, SeasonId = season1.Id, TeamId = team2.Id };
            var pst6 = new PlayerSeasonTeam { PlayerId = player6.Id, SeasonId = season1.Id, TeamId = team2.Id };
            var pst7 = new PlayerSeasonTeam { PlayerId = player7.Id, SeasonId = season1.Id, TeamId = team2.Id };
            var pst8 = new PlayerSeasonTeam { PlayerId = player8.Id, SeasonId = season1.Id, TeamId = team2.Id };

            DbContext.PlayerSeasonTeams.AddRange(pst1, pst2, pst3, pst4, pst5, pst6, pst7, pst8);
            DbContext.SaveChanges();

            // Create Week 1
            var week1 = new Week
            {
                WeekNumber = 1,
                SeasonId = season1.Id,
                StartDate = DateTime.UtcNow.AddDays(-14),
                EndDate = DateTime.UtcNow.AddDays(-7),
                Status = WeekStatus.Completed,
                SubmissionsClosedDate = DateTime.UtcNow.AddDays(-13),
                SubmissionsRequired = 3
            };

            DbContext.Weeks.Add(week1);
            DbContext.SaveChanges();

            // Create 3 Matches for Week 1 (all completed)
            var match1 = new Match
            {
                WeekId = week1.Id,
                Player1Id = player1.Id,
                Player2Id = player5.Id,
                WinnerId = player1.Id,
                Status = MatchStatus.Reported,
                ReportedDate = DateTime.UtcNow.AddDays(-10),
                ReplayUrl = "https://example.com/replay1",
                MatchResultType = MatchResultType.Normal
            };

            var match2 = new Match
            {
                WeekId = week1.Id,
                Player1Id = player2.Id,
                Player2Id = player6.Id,
                WinnerId = player6.Id,
                Status = MatchStatus.Reported,
                ReportedDate = DateTime.UtcNow.AddDays(-10),
                ReplayUrl = "https://example.com/replay2",
                MatchResultType = MatchResultType.Normal
            };

            var match3 = new Match
            {
                WeekId = week1.Id,
                Player1Id = player3.Id,
                Player2Id = player7.Id,
                WinnerId = player3.Id,
                Status = MatchStatus.Reported,
                ReportedDate = DateTime.UtcNow.AddDays(-10),
                ReplayUrl = "https://example.com/replay3",
                MatchResultType = MatchResultType.Normal
            };

            DbContext.Matches.AddRange(match1, match2, match3);
            DbContext.SaveChanges();

            // Create 3 Deck Submissions for Team Alpha (Week 1)
            var deck1 = new DeckSubmission
            {
                WeekId = week1.Id,
                PlayerId = player1.Id,
                DeckFile = "deck1.ydk",
                SubmittedDate = DateTime.UtcNow.AddDays(-13),
                SeatNumber = 1
            };

            var deck2 = new DeckSubmission
            {
                WeekId = week1.Id,
                PlayerId = player2.Id,
                DeckFile = "deck2.ydk",
                SubmittedDate = DateTime.UtcNow.AddDays(-13),
                SeatNumber = 2
            };

            var deck3 = new DeckSubmission
            {
                WeekId = week1.Id,
                PlayerId = player3.Id,
                DeckFile = "deck3.ydk",
                SubmittedDate = DateTime.UtcNow.AddDays(-13),
                SeatNumber = 3
            };

            // Create 3 Deck Submissions for Team Beta (Week 1)
            var deck4 = new DeckSubmission
            {
                WeekId = week1.Id,
                PlayerId = player5.Id,
                DeckFile = "deck4.ydk",
                SubmittedDate = DateTime.UtcNow.AddDays(-13),
                SeatNumber = 1
            };

            var deck5 = new DeckSubmission
            {
                WeekId = week1.Id,
                PlayerId = player6.Id,
                DeckFile = "deck5.ydk",
                SubmittedDate = DateTime.UtcNow.AddDays(-13),
                SeatNumber = 2
            };

            var deck6 = new DeckSubmission
            {
                WeekId = week1.Id,
                PlayerId = player7.Id,
                DeckFile = "deck6.ydk",
                SubmittedDate = DateTime.UtcNow.AddDays(-13),
                SeatNumber = 3
            };

            DbContext.DeckSubmissions.AddRange(deck1, deck2, deck3, deck4, deck5, deck6);
            DbContext.SaveChanges();
        }
    }
}
