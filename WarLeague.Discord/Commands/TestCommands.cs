using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Data;
using WarLeague.Data.Entities;
using WarLeague.Data.Enums;
using WarLeague.Discord.Constants;

namespace WarLeague.Discord.Commands
{
    [Group("test", "Test commands")]
    [RequireRole(DiscordRoleConstants.Admin)]
    public class TestCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly WarLeagueDbContext _dbContext;
        public TestCommands(WarLeagueDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        [SlashCommand("initial-data-seed", "Seeds a format, season, week, teams and submited decks")]
        public async Task InitialSeedDataAsync(string testFormatName)
        {
            await DeferAsync(ephemeral: false);
            Seed.SeedData(_dbContext, testFormatName);
            await FollowupAsync("seeded");
        }
        [SlashCommand("report-all-matches", "Set all matches to status Reported")]
        public async Task ConfirmAsync()
        {
            await DeferAsync(ephemeral: false);
            _dbContext.Matches.ExecuteUpdate(x => x.SetProperty(y => y.Status, MatchStatus.Reported));
            await FollowupAsync("all matches reported");
        }
    }
    public static class Seed
    {
        public static void SeedData(WarLeagueDbContext DbContext, string name)
        {
            // Create Formats
            var testFormat = new Format
            {
                Name = name,
                Rules = "{}",
                SingleFormatMode = false
            };

            DbContext.Formats.AddRange(testFormat);
            DbContext.SaveChanges();

            Random rnd = new Random();

            // Create Players
            var player1 = new Player { DiscordUserId = (ulong)rnd.Next(0, int.MaxValue), UserName = name+"Player1" };
            var player2 = new Player { DiscordUserId = (ulong)rnd.Next(0, int.MaxValue), UserName = name+"Player2" };
            var player3 = new Player { DiscordUserId = (ulong)rnd.Next(0, int.MaxValue), UserName = name+"Player3" };
            var player4 = new Player { DiscordUserId = (ulong)rnd.Next(0, int.MaxValue), UserName = name+"Player4" };
            var player5 = new Player { DiscordUserId = (ulong)rnd.Next(0, int.MaxValue), UserName = name+"Player5" };
            var player6 = new Player { DiscordUserId = (ulong)rnd.Next(0, int.MaxValue), UserName = name+"Player6" };

            DbContext.Players.AddRange(player1, player2, player3, player4, player5, player6);
            DbContext.SaveChanges();

            // Create Season
            var season1 = new Season
            {
                SeasonNumber = 1,
                FormatId = testFormat.Id,
                Active = true,
                DisableTeamModification = false,
                MinimumTeamMembers = 2
            };

            DbContext.Seasons.Add(season1);
            DbContext.SaveChanges();

            // Create Teams
            var team1 = new Team
            {
                Name = name+"A",
                CaptainId = player1.Id,
                SeasonId = season1.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                DiscordRoleId = 123456789012345678
            };

            var team2 = new Team
            {
                Name = name+"B",
                CaptainId = player3.Id,
                SeasonId = season1.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                DiscordRoleId = 234567890123456789
            };

            var team3 = new Team
            {
                Name = name+"C",
                CaptainId = player5.Id,
                SeasonId = season1.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                DiscordRoleId = 234567890123456789
            };

            DbContext.Teams.AddRange(team1, team2, team3);
            DbContext.SaveChanges();

            // Assign Players to Teams for the Season
            var pst1 = new PlayerSeasonTeam { PlayerId = player1.Id, SeasonId = season1.Id, TeamId = team1.Id };
            var pst2 = new PlayerSeasonTeam { PlayerId = player2.Id, SeasonId = season1.Id, TeamId = team1.Id };
            var pst3 = new PlayerSeasonTeam { PlayerId = player3.Id, SeasonId = season1.Id, TeamId = team2.Id };
            var pst4 = new PlayerSeasonTeam { PlayerId = player4.Id, SeasonId = season1.Id, TeamId = team2.Id };
            var pst5 = new PlayerSeasonTeam { PlayerId = player5.Id, SeasonId = season1.Id, TeamId = team3.Id };
            var pst6 = new PlayerSeasonTeam { PlayerId = player6.Id, SeasonId = season1.Id, TeamId = team3.Id };

            DbContext.PlayerSeasonTeams.AddRange(pst1, pst2, pst3, pst4, pst5, pst6);
            DbContext.SaveChanges();

            // Create Week 1
            var week1 = new Week
            {
                WeekNumber = 1,
                SeasonId = season1.Id,
                StartDate = DateTime.UtcNow.AddDays(-14),
                EndDate = DateTime.UtcNow.AddDays(-7),
                Status = WeekStatus.Open,
                SubmissionsClosedDate = DateTime.UtcNow.AddDays(-13),
                SubmissionsRequired = 2
            };

            DbContext.Weeks.Add(week1);
            DbContext.SaveChanges();

            
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

            var deck4 = new DeckSubmission
            {
                WeekId = week1.Id,
                PlayerId = player4.Id,
                DeckFile = "deck4.ydk",
                SubmittedDate = DateTime.UtcNow.AddDays(-13),
                SeatNumber = 1
            };

            var deck5 = new DeckSubmission
            {
                WeekId = week1.Id,
                PlayerId = player5.Id,
                DeckFile = "deck5.ydk",
                SubmittedDate = DateTime.UtcNow.AddDays(-13),
                SeatNumber = 2
            };

            var deck6 = new DeckSubmission
            {
                WeekId = week1.Id,
                PlayerId = player6.Id,
                DeckFile = "deck6.ydk",
                SubmittedDate = DateTime.UtcNow.AddDays(-13),
                SeatNumber = 3
            };

            DbContext.DeckSubmissions.AddRange(deck1, deck2, deck3, deck4, deck5, deck6);
            DbContext.SaveChanges();
        }
    }
}
