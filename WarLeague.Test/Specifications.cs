using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WarLeague.Data;
using WarLeague.Data.Enums;
using WarLeague.Core.Services;
using WarLeague.Data.Entities;
using WarLeague.Core.Model;

namespace WarLeague.Test
{
    /// <summary>
    /// Domain behavior specifications using Arrange-Act-Assert pattern.
    /// Tests ONLY use services - NO direct database context access.
    /// All tests follow "WhenXThenY" naming to make behavior explicit.
    /// </summary>
    public partial class Specifications : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FormatService _formatService;
        private readonly SeasonService _seasonService;
        private readonly WeekService _weekService;
        private readonly TeamService _teamService;
        private readonly MatchService _matchService;
        private readonly DeckSubmissionService _deckSubmissionService;
        private readonly SubstitutionService _substitutionService;
        private readonly ConferenceService _conferenceService;
        private readonly WarLeagueDbContext _context;

        public Specifications()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false)
                .Build();

            var connectionString = configuration.GetConnectionString("TestConnection")
                ?? throw new InvalidOperationException("Test connection string not found in appsettings.Test.json");

            var optionsBuilder = new DbContextOptionsBuilder<WarLeagueDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            _context = new WarLeagueDbContext(optionsBuilder.Options);
            _serviceProvider = TestServiceProvider.CreateServiceProvider(_context);

            _formatService = _serviceProvider.GetRequiredService<FormatService>();
            _seasonService = _serviceProvider.GetRequiredService<SeasonService>();
            _weekService = _serviceProvider.GetRequiredService<WeekService>();
            _teamService = _serviceProvider.GetRequiredService<TeamService>();
            _matchService = _serviceProvider.GetRequiredService<MatchService>();
            _deckSubmissionService = _serviceProvider.GetRequiredService<DeckSubmissionService>();
            _substitutionService = _serviceProvider.GetRequiredService<SubstitutionService>();
            _conferenceService = _serviceProvider.GetRequiredService<ConferenceService>();

            RecreateDatabase();
        }

        private void RecreateDatabase()
        {
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region Helper Methods - Setup Scenarios

        private async Task<(int formatId, int seasonId)> CreateFormatAndSeason()
        {
            var formatName = $"Format{Guid.NewGuid()}";
            await _formatService.CreateFormatAsync(formatName);
            var format = await _formatService.GetFormatAsync(formatName);
            await _seasonService.CreateAsync(format!.Id, 1, 4);
            var season = format.Seasons.First();
            return (format.Id, season.Id);
        }

        private async Task<(int seasonId, int playerId1, int playerId2, int cptId1)> CreateSeasonWithTeamAndOpenWeek(int submissionsRequired = 3)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var (playerId1, cptId1) = await CreateTeamWithPlayer(seasonId, "Team1");
            var (playerId2, cptId2) = await CreateTeamWithPlayer(seasonId, "Team2");
            await CreateOpenWeek(seasonId, submissionsRequired);
            return (seasonId, playerId1, playerId2, cptId1);
        }

        private async Task<(int seasonId, string teamName, int playerInId, int playerOutId)> CreateSubstitutionScenario()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var teamName = "Team1";
            var (player1, player2, teamId) = await CreateTwoPlayersOnSameTeam(seasonId, teamName);
            
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 2);
            await _weekService.UpdateAsync(seasonId, 1, null, null, null, WeekStatus.InProgress, 2);
            
            var opponent = await CreatePlayer(777777);
            var opponentTeamId = await CreateTeam(seasonId, "OpponentTeam", opponent.Id);
            
            await CreateMatch(seasonId, 1, player1.Id, opponent.Id, teamId, opponentTeamId);
            await CreateDeckSubmission(seasonId, 1, player1.Id, 1);
            
            return (seasonId, teamName, player2.Id, player1.Id);
        }

        private async Task<(int seasonId, string teamName, int player1Id, int player2Id, int weekId)> CreateTwoPlayersWithMatchesScenario()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var teamName = "Team1";
            var (player1, player2, teamId) = await CreateTwoPlayersOnSameTeam(seasonId, teamName);
            
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, 2);
            await _weekService.UpdateAsync(seasonId, 1, null, null, null, WeekStatus.InProgress, 2);
            
            var week = await _context.Weeks.FirstAsync(w => w.SeasonId == seasonId && w.WeekNumber == 1);
            
            var opponent1 = await CreatePlayer(888881);
            var opponent2 = await CreatePlayer(888882);
            var opponentTeamId = await CreateTeam(seasonId, "OpponentTeam", opponent1.Id);
            await AddPlayerToTeam(opponent2.Id, seasonId, opponentTeamId);

            await CreateMatch(seasonId, 1, player1.Id, opponent1.Id, teamId, opponentTeamId);
            await CreateMatch(seasonId, 1, player2.Id, opponent2.Id, teamId, opponentTeamId);

            return (seasonId, teamName, player1.Id, player2.Id, week.Id);
        }

        private async Task<(int seasonId, int weekId)> CreateSeasonWithTeamsAndSubmissions(int teamCount, int playersPerTeam)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            
            for (int i = 0; i < teamCount; i++)
            {
                var captain = await CreatePlayer((ulong)(1000 + i * 100));
                var teamId = await CreateTeam(seasonId, $"Team{i + 1}", captain.Id);
                
                for (int j = 1; j <= playersPerTeam; j++)
                {
                    var player = await CreatePlayer((ulong)(1000 + i * 100 + j));
                    await AddPlayerToTeam(player.Id, seasonId, teamId);
                }
            }
            
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, playersPerTeam);
            await _weekService.TransitionToOpenWeekAsync(seasonId, 1);
            
            var teams = await _context.Teams.Where(t => t.SeasonId == seasonId).ToListAsync();
            var week = await _context.Weeks.FirstAsync(w => w.SeasonId == seasonId && w.WeekNumber == 1);
            
            foreach (var team in teams)
            {
                var teamPlayerIds = await GetTeamPlayerIds(seasonId, team.Id);
                for (int seat = 1; seat <= playersPerTeam; seat++)
                {
                    await CreateDeckSubmission(seasonId, week.WeekNumber, teamPlayerIds[seat - 1], seat);
                }
            }
            
            return (seasonId, week.Id);
        }

        /// <summary>
        /// Creates a season with 4 teams in two conferences (Alpha, Beta), with conferences assigned before the week is opened
        /// so that round-robin team matchups are generated per conference. Returns (seasonId, weekId).
        /// </summary>
        private async Task<(int seasonId, int weekId)> CreateSeasonWithTwoConferencesAndSubmissions(int teamsPerConference = 2, int playersPerTeam = 2)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var alphaConference = new Conference { SeasonId = seasonId, Name = "Alpha" };
            var betaConference = new Conference { SeasonId = seasonId, Name = "Beta" };
            _context.Conferences.Add(alphaConference);
            _context.Conferences.Add(betaConference);
            await _context.SaveChangesAsync();

            for (int i = 0; i < teamsPerConference * 2; i++)
            {
                var captain = await CreatePlayer((ulong)(2000 + i * 100));
                var teamId = await CreateTeam(seasonId, $"Team{i + 1}", captain.Id);
                var team = await _context.Teams.FindAsync(teamId);
                team!.ConferenceId = i < teamsPerConference ? alphaConference.Id : betaConference.Id;
                for (int j = 1; j <= playersPerTeam; j++)
                {
                    var player = await CreatePlayer((ulong)(2000 + i * 100 + j));
                    await AddPlayerToTeam(player.Id, seasonId, teamId);
                }
                await _context.SaveChangesAsync();
            }

            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, playersPerTeam);
            await _weekService.TransitionToOpenWeekAsync(seasonId, 1);

            var teams = await _context.Teams.Where(t => t.SeasonId == seasonId).ToListAsync();
            var week = await _context.Weeks.FirstAsync(w => w.SeasonId == seasonId && w.WeekNumber == 1);
            foreach (var team in teams)
            {
                var teamPlayerIds = await GetTeamPlayerIds(seasonId, team.Id);
                for (int seat = 1; seat <= playersPerTeam; seat++)
                {
                    await CreateDeckSubmission(seasonId, week.WeekNumber, teamPlayerIds[seat - 1], seat);
                }
            }
            return (seasonId, week.Id);
        }

        /// <summary>
        /// Creates a season with exactly one team and a week in SubmissionsClosed (via context, since services require 2+ teams).
        /// Used to test that generating pairings with one team returns failure.
        /// </summary>
        private async Task<int> CreateSeasonWithOneTeamAndSubmissionsClosedWeek(int playersPerTeam = 2)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer((ulong)9001);
            var teamId = await CreateTeam(seasonId, "SoloTeam", captain.Id);
            await AddPlayerToTeam(captain.Id, seasonId, teamId);
            for (int j = 1; j < playersPerTeam; j++)
            {
                var player = await CreatePlayer((ulong)(9001 + j));
                await AddPlayerToTeam(player.Id, seasonId, teamId);
            }
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, playersPerTeam);
            var week = await _context.Weeks.FirstAsync(w => w.SeasonId == seasonId && w.WeekNumber == 1);
            week.Status = WeekStatus.SubmissionsClosed;
            await _context.SaveChangesAsync();
            return seasonId;
        }

        private async Task<(int playerId, int captainId)> CreateTeamWithPlayer(int seasonId, string teamName)
        {
            var captain = await CreatePlayer(playerid++);
            var teamId = await CreateTeam(seasonId, teamName, captain.Id);
            var player = await CreatePlayer(playerid++);
            await AddPlayerToTeam(player.Id, seasonId, teamId);
            return (player.Id, captain.Id);
        }
        static ulong playerid = 1;
        private async Task<(Player player1, Player player2, int teamId)> CreateTwoPlayersOnSameTeam(int seasonId, string teamName)
        {
            var captain = await CreatePlayer(playerid++);
            var teamId = await CreateTeam(seasonId, teamName, captain.Id);
            var player1 = await CreatePlayer(playerid++);
            var player2 = await CreatePlayer(playerid++);
            await AddPlayerToTeam(player1.Id, seasonId, teamId);
            await AddPlayerToTeam(player2.Id, seasonId, teamId);
            return (player1, player2, teamId);
        }

        private async Task CreateOpenWeek(int seasonId, int submissionsRequired = 3)
        {
            await _weekService.CreateAsync(seasonId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, submissionsRequired);
            await _weekService.TransitionToOpenWeekAsync(seasonId, 1);
        }

        private async Task CloseSubmissions(int seasonId)
        {
            await _weekService.TransitionToCloseSubmissionsAsync(seasonId);
        }

        private async Task<Player> CreatePlayer(ulong discordUserId)
        {
            var player = new Player { DiscordUserId = discordUserId, UserName = $"Player{discordUserId}" };
            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            return player;
        }

        private async Task<int> CreateTeam(int seasonId, string teamName, int captainId)
        {
            int conferenceId = await GetOrCreateDefaultConferenceId(seasonId);

            var team = new Team
            {
                Name = teamName,
                CaptainId = captainId,
                SeasonId = seasonId,
                ConferenceId = conferenceId,
                DiscordRoleId = (ulong)(new Random().Next(100000, 999999))
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            return team.Id;
        }

        private async Task<int> GetOrCreateDefaultConferenceId(int seasonId)
        {
            var existingConference = await _context.Conferences
                .SingleOrDefaultAsync(c => c.SeasonId == seasonId && c.Name == "Default");

            if (existingConference is not null)
            {
                return existingConference.Id;
            }

            var conference = new Conference
            {
                SeasonId = seasonId,
                Name = "Default"
            };

            _context.Conferences.Add(conference);
            await _context.SaveChangesAsync();

            return conference.Id;
        }

        private async Task AddPlayerToTeam(int playerId, int seasonId, int teamId)
        {
            _context.PlayerSeasonTeams.Add(new PlayerSeasonTeam
            {
                PlayerId = playerId,
                SeasonId = seasonId,
                TeamId = teamId
            });
            await _context.SaveChangesAsync();
        }

        private async Task CreateMatch(int seasonId, int weekNumber, int player1Id, int player2Id, int teamId, int opponentTeamId)
        {
            var week = await _context.Weeks.FirstOrDefaultAsync(w => w.SeasonId == seasonId && w.WeekNumber == weekNumber);
            if (week == null) return;
            
            // Canonical order (Player1Id < Player2Id) for DB unique constraint
            var p1 = Math.Min(player1Id, player2Id);
            var p2 = Math.Max(player1Id, player2Id);
            _context.Matches.Add(new Match
            {
                WeekId = week.Id,
                Player1Id = p1,
                Player2Id = p2,
                Status = MatchStatus.Scheduled,
                Team1Id = teamId,
                Team2Id = opponentTeamId
            });
            await _context.SaveChangesAsync();
        }

        private async Task CreateDeckSubmission(int seasonId, int weekNumber, int playerId, int seatNumber)
        {
            var week = await _context.Weeks.FirstOrDefaultAsync(w => w.SeasonId == seasonId && w.WeekNumber == weekNumber);
            if (week == null) return;
            
            _context.DeckSubmissions.Add(new DeckSubmission
            {
                WeekId = week.Id,
                PlayerId = playerId,
                DeckFile = $"deck{playerId}",
                SeatNumber = seatNumber,
                SubmittedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        private async Task<int> PrepareReadyToCloseWeek()
        {
            int seasonId = await PrepareWeek_ReadyForClosingSubmissions();
            await _weekService.TransitionToCloseSubmissionsAsync(seasonId);
            await _weekService.TransitionToInProgressAsync(seasonId);
            int playerUd = _context.Matches.First().Player1Id;
            await _matchService.ReportLossAsync(seasonId, playerUd, "http://www.example.com");
            return seasonId;
        }
        private async Task<List<int>> GetTeamPlayerIds(int seasonId, int teamId)
        {
            return await _context.PlayerSeasonTeams
                .Where(pst => pst.SeasonId == seasonId && pst.TeamId == teamId)
                .Select(pst => pst.PlayerId)
                .ToListAsync();
        }
        private async Task<int> PrepareWeek_ReadyForClosingSubmissions()
        {
            int weekNumber = 1;
            int submissionRequired = 1;
            var (_, seasonId) = await CreateFormatAndSeason();
            await _weekService.CreateAsync(seasonId, weekNumber, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, submissionRequired);
            var (playerId1, _) = await CreateTeamWithPlayer(seasonId, "Team1");
            var (playerId2, _) = await CreateTeamWithPlayer(seasonId, "Team2");
            await _weekService.TransitionToOpenWeekAsync(seasonId, weekNumber);
            await _deckSubmissionService.SubmitAsync(seasonId, (int)playerId1, "deck content", 1);
            await _deckSubmissionService.SubmitAsync(seasonId, (int)playerId2, "deck content", 1);
            return seasonId;
        }
        #endregion
    }
}
