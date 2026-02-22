using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Linq;
using WarLeague.Data;
using WarLeague.Data.Data.Entities;
using WarLeague.Data.Data.Enums;
using WarLeague.Data.Enums;
using WarLeague.Core.Services;
using WarLeague.Core.Repositories;
using WarLeague.Data.Entities;
using WarLeague.Core.Model;

namespace WarLeague.Test
{
    /// <summary>
    /// Domain behavior specifications using Arrange-Act-Assert pattern.
    /// Tests use services and repositories; DbContext is used only for DB lifecycle (recreate/dispose).
    /// Helpers are incremental so scenarios can be built from ground up.
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
        private readonly SeasonRepository _seasonRepository;
        private readonly WeekRepository _weekRepository;
        private readonly TeamRepository _teamRepository;
        private readonly ConferenceRepository _conferenceRepository;
        private readonly PlayerRepository _playerRepository;
        private readonly MatchRepository _matchRepository;
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        private readonly DeckSubmissionRepository _deckSubmissionRepository;
        private readonly TeamStandingsService _teamStandingsService;
        private readonly PlayoffService _playoffService;
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
            _seasonRepository = _serviceProvider.GetRequiredService<SeasonRepository>();
            _weekRepository = _serviceProvider.GetRequiredService<WeekRepository>();
            _teamRepository = _serviceProvider.GetRequiredService<TeamRepository>();
            _conferenceRepository = _serviceProvider.GetRequiredService<ConferenceRepository>();
            _playerRepository = _serviceProvider.GetRequiredService<PlayerRepository>();
            _matchRepository = _serviceProvider.GetRequiredService<MatchRepository>();
            _playerSeasonTeamRepository = _serviceProvider.GetRequiredService<PlayerSeasonTeamRepository>();
            _deckSubmissionRepository = _serviceProvider.GetRequiredService<DeckSubmissionRepository>();
            _teamStandingsService = _serviceProvider.GetRequiredService<TeamStandingsService>();
            _playoffService = _serviceProvider.GetRequiredService<PlayoffService>();

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

        #region Incremental building blocks (use these to build scenarios)

        private async Task<(int formatId, int seasonId)> CreateFormatAndSeason()
        {
            var formatName = $"Format{Guid.NewGuid()}";
            await _formatService.CreateFormatAsync(formatName);
            var format = await _formatService.GetFormatAsync(formatName);
            await _seasonService.CreateAsync(format!.Id, 1, 4);
            await _seasonService.SetActiveAsync(format!.Id, 1);
            var season = format.Seasons.First();
            return (format.Id, season.Id);
        }

        private async Task EnsureConferenceAsync(int seasonId, string name)
        {
            if (await _conferenceRepository.GetByNameAndSeasonAsync(name, seasonId) != null) return;
            (await _conferenceService.CreateAsync(seasonId, name, 1)).Success.ShouldBeTrue();
        }

        private async Task<Player> CreatePlayer(ulong discordUserId)
        {
            var player = new Player { DiscordUserId = discordUserId, UserName = $"Player{discordUserId}" };
            await _playerRepository.AddAsync(player);
            return player;
        }

        private async Task<int> CreateTeam(int seasonId, string teamName, int captainId, string conferenceName = "Default")
        {
            await EnsureConferenceAsync(seasonId, conferenceName);
            var result = await _teamService.CreateAsync(seasonId, teamName, captainId, conferenceName, canBypassTeamModificationCheck: true);
            result.Success.ShouldBeTrue(result.Message);
            var team = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);
            return team!.Id;
        }

        private async Task AddPlayerToTeam(int playerId, int seasonId, int teamId)
        {
            (await _teamService.AddMemberAsync(seasonId, playerId, teamId, canBypassTeamModificationCheck: true)).Success.ShouldBeTrue();
        }

        private async Task CreateWeekAsync(int seasonId, int weekNumber, int submissionsRequired)
        {
            (await _weekService.CreateAsync(seasonId, weekNumber, DateTime.UtcNow, DateTime.UtcNow.AddDays(7), null, submissionsRequired)).Success.ShouldBeTrue();
        }

        private async Task OpenWeekAsync(int seasonId, int weekNumber)
        {
            (await _weekService.TransitionToOpenWeekAsync(seasonId, weekNumber)).Success.ShouldBeTrue();
        }

        private async Task CloseSubmissionsAsync(int seasonId)
        {
            (await _weekService.TransitionToCloseSubmissionsAsync(seasonId)).Success.ShouldBeTrue();
        }

        private async Task SetWeekStatusAsync(int seasonId, int weekNumber, WeekStatus status)
        {
            (await _weekService.UpdateAsync(seasonId, weekNumber, null, null, null, status, null)).Success.ShouldBeTrue();
        }

        private async Task SubmitDeckAsync(int seasonId, int playerId, int seatNumber, string content = "deck content")
        {
            (await _deckSubmissionService.SubmitAsync(seasonId, playerId, content, seatNumber)).Success.ShouldBeTrue();
        }

        private async Task CreateMatchAsync(int seasonId, int weekNumber, int player1Id, int player2Id, int teamId, int opponentTeamId)
        {
            var week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, seasonId);
            if (week == null) return;
            var p1 = Math.Min(player1Id, player2Id);
            var p2 = Math.Max(player1Id, player2Id);
            await _matchRepository.AddRangeAsync(new[]
            {
                new Match
                {
                    WeekId = week.Id,
                    Player1Id = p1,
                    Player2Id = p2,
                    Status = MatchStatus.Scheduled,
                    Team1Id = teamId,
                    Team2Id = opponentTeamId
                }
            });
        }

        /// <summary>Adds a deck submission via repository (use when week is not Open, e.g. SubmissionsClosed or InProgress).</summary>
        private async Task AddDeckSubmissionForWeekAsync(int seasonId, int weekNumber, int playerId, int seatNumber)
        {
            var week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, seasonId);
            if (week == null) return;
            await _deckSubmissionRepository.AddAsync(new DeckSubmission
            {
                WeekId = week.Id,
                PlayerId = playerId,
                DeckFile = $"deck{playerId}",
                SeatNumber = seatNumber,
                SubmittedDate = DateTime.UtcNow
            });
        }

        private async Task<Week?> GetWeekWithSubmissionsAsync(int seasonId, int weekNumber)
        {
            return await _weekRepository.GetByWeekNumberAndSeasonWithSubmissionsAsync(weekNumber, seasonId);
        }

        private async Task<List<Team>> GetTeamsAsync(int seasonId)
        {
            return await _teamRepository.GetBySeasonAsync(seasonId);
        }

        private async Task<List<int>> GetTeamPlayerIds(int seasonId, int teamId)
        {
            return await _playerSeasonTeamRepository.GetPlayerIdsByTeamAndSeasonAsync(teamId, seasonId);
        }

        #endregion

        #region Composite helpers (built from building blocks)

        private async Task<(int playerId, int captainId)> CreateTeamWithPlayer(int seasonId, string teamName, string conferenceName = "Default")
        {
            var captain = await CreatePlayer(_nextPlayerId++);
            var teamId = await CreateTeam(seasonId, teamName, captain.Id, conferenceName);
            var player = await CreatePlayer(_nextPlayerId++);
            await AddPlayerToTeam(player.Id, seasonId, teamId);
            return (player.Id, captain.Id);
        }

        private static ulong _nextPlayerId = 1;

        private async Task<(Player player1, Player player2, int teamId)> CreateTwoPlayersOnSameTeam(int seasonId, string teamName, string conferenceName = "Default")
        {
            var captain = await CreatePlayer(_nextPlayerId++);
            var teamId = await CreateTeam(seasonId, teamName, captain.Id, conferenceName);
            var player1 = await CreatePlayer(_nextPlayerId++);
            var player2 = await CreatePlayer(_nextPlayerId++);
            await AddPlayerToTeam(player1.Id, seasonId, teamId);
            await AddPlayerToTeam(player2.Id, seasonId, teamId);
            return (player1, player2, teamId);
        }

        private async Task CreateOpenWeek(int seasonId, int submissionsRequired = 3)
        {
            await CreateWeekAsync(seasonId, 1, submissionsRequired);
            await OpenWeekAsync(seasonId, 1);
        }

        private async Task<(int seasonId, int playerId1, int playerId2, int cptId1)> CreateSeasonWithTeamAndOpenWeek(int submissionsRequired = 3)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var (playerId1, cptId1) = await CreateTeamWithPlayer(seasonId, "Team1");
            var (playerId2, _) = await CreateTeamWithPlayer(seasonId, "Team2");
            await CreateOpenWeek(seasonId, submissionsRequired);
            return (seasonId, playerId1, playerId2, cptId1);
        }

        private async Task<(int seasonId, string teamName, int playerInId, int playerOutId)> CreateSubstitutionScenario()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var teamName = "Team1";
            var (player1, player2, teamId) = await CreateTwoPlayersOnSameTeam(seasonId, teamName);
            await CreateWeekAsync(seasonId, 1, 2);
            await SetWeekStatusAsync(seasonId, 1, WeekStatus.InProgress);
            var opponent = await CreatePlayer(777777);
            var opponentTeamId = await CreateTeam(seasonId, "OpponentTeam", opponent.Id);
            await CreateMatchAsync(seasonId, 1, player1.Id, opponent.Id, teamId, opponentTeamId);
            await AddDeckSubmissionForWeekAsync(seasonId, 1, player1.Id, 1);
            return (seasonId, teamName, player2.Id, player1.Id);
        }

        private async Task<(int seasonId, string teamName, int player1Id, int player2Id, int weekId)> CreateTwoPlayersWithMatchesScenario()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var teamName = "Team1";
            var (player1, player2, teamId) = await CreateTwoPlayersOnSameTeam(seasonId, teamName);
            await CreateWeekAsync(seasonId, 1, 2);
            await SetWeekStatusAsync(seasonId, 1, WeekStatus.InProgress);
            var week = await _weekRepository.GetByWeekNumberAndSeasonAsync(1, seasonId);
            var opponent1 = await CreatePlayer(888881);
            var opponent2 = await CreatePlayer(888882);
            var opponentTeamId = await CreateTeam(seasonId, "OpponentTeam", opponent1.Id);
            await AddPlayerToTeam(opponent2.Id, seasonId, opponentTeamId);
            await CreateMatchAsync(seasonId, 1, player1.Id, opponent1.Id, teamId, opponentTeamId);
            await CreateMatchAsync(seasonId, 1, player2.Id, opponent2.Id, teamId, opponentTeamId);
            return (seasonId, teamName, player1.Id, player2.Id, week!.Id);
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
            await CreateWeekAsync(seasonId, 1, playersPerTeam);
            await OpenWeekAsync(seasonId, 1);
            var teams = await GetTeamsAsync(seasonId);
            var week = await _weekRepository.GetByWeekNumberAndSeasonAsync(1, seasonId);
            foreach (var team in teams)
            {
                var teamPlayerIds = await GetTeamPlayerIds(seasonId, team.Id);
                for (int seat = 1; seat <= playersPerTeam; seat++)
                    await SubmitDeckAsync(seasonId, teamPlayerIds[seat - 1], seat);
            }
            return (seasonId, week!.Id);
        }

        private async Task<(int seasonId, int weekId)> CreateSeasonWithTwoConferencesAndSubmissions(int teamsPerConference = 2, int playersPerTeam = 2)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            await EnsureConferenceAsync(seasonId, "Alpha");
            await EnsureConferenceAsync(seasonId, "Beta");
            for (int i = 0; i < teamsPerConference * 2; i++)
            {
                var conferenceName = i < teamsPerConference ? "Alpha" : "Beta";
                var captain = await CreatePlayer((ulong)(2000 + i * 100));
                var teamId = await CreateTeam(seasonId, $"Team{i + 1}", captain.Id, conferenceName);
                for (int j = 1; j <= playersPerTeam; j++)
                {
                    var player = await CreatePlayer((ulong)(2000 + i * 100 + j));
                    await AddPlayerToTeam(player.Id, seasonId, teamId);
                }
            }
            await CreateWeekAsync(seasonId, 1, playersPerTeam);
            await OpenWeekAsync(seasonId, 1);
            var teams = await GetTeamsAsync(seasonId);
            var week = await _weekRepository.GetByWeekNumberAndSeasonAsync(1, seasonId);
            foreach (var team in teams)
            {
                var teamPlayerIds = await GetTeamPlayerIds(seasonId, team.Id);
                for (int seat = 1; seat <= playersPerTeam; seat++)
                    await SubmitDeckAsync(seasonId, teamPlayerIds[seat - 1], seat);
            }
            return (seasonId, week!.Id);
        }

        private async Task<int> CreateSeasonWithOneTeamAndSubmissionsClosedWeek(int playersPerTeam = 2)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            var captain = await CreatePlayer((ulong)9001);
            var teamId = await CreateTeam(seasonId, "SoloTeam", captain.Id); // captain is already added by CreateTeam
            for (int j = 1; j < playersPerTeam; j++)
            {
                var player = await CreatePlayer((ulong)(9001 + j));
                await AddPlayerToTeam(player.Id, seasonId, teamId);
            }
            await CreateWeekAsync(seasonId, 1, playersPerTeam);
            await SetWeekStatusAsync(seasonId, 1, WeekStatus.SubmissionsClosed);
            return seasonId;
        }

        private async Task<int> PrepareWeek_ReadyForClosingSubmissions()
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            await CreateWeekAsync(seasonId, 1, 1);
            var (playerId1, _) = await CreateTeamWithPlayer(seasonId, "Team1");
            var (playerId2, _) = await CreateTeamWithPlayer(seasonId, "Team2");
            await OpenWeekAsync(seasonId, 1);
            await SubmitDeckAsync(seasonId, (int)playerId1, 1);
            await SubmitDeckAsync(seasonId, (int)playerId2, 1);
            return seasonId;
        }

        private async Task<int> PrepareReadyToCloseWeek()
        {
            int seasonId = await PrepareWeek_ReadyForClosingSubmissions();
            await CloseSubmissionsAsync(seasonId);
            (await _weekService.TransitionToInProgressAsync(seasonId)).Success.ShouldBeTrue();
            var week = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(seasonId, WeekStatus.InProgress);
            var matches = await _matchRepository.GetByWeekIdAsync(week!.Id);
            await _matchService.ReportLossAsync(seasonId, matches.First().Player1Id, "http://www.example.com");
            return seasonId;
        }

        #endregion

        #region Pairing scenario helpers (for MatchGenerationSpecifications)

        private async Task<(int seasonId, Week week, List<Team> teams)> GetSeasonWeekAndTeamsForPairingsAsync(int teamCount, int playersPerTeam)
        {
            var (seasonId, _) = await CreateSeasonWithTeamsAndSubmissions(teamCount, playersPerTeam);
            await CloseSubmissionsAsync(seasonId);
            var week = await GetWeekWithSubmissionsAsync(seasonId, 1);
            var teams = await GetTeamsAsync(seasonId);
            return (seasonId, week!, teams);
        }

        private async Task<(int seasonId, Week week, List<Team> teams)> GetSeasonWeekAndTeamsTwoConferencesForPairingsAsync(int teamsPerConference = 2, int playersPerTeam = 2)
        {
            var (seasonId, _) = await CreateSeasonWithTwoConferencesAndSubmissions(teamsPerConference, playersPerTeam);
            await CloseSubmissionsAsync(seasonId);
            var week = await GetWeekWithSubmissionsAsync(seasonId, 1);
            var teams = (await GetTeamsAsync(seasonId)).OrderBy(t => t.Id).ToList();
            return (seasonId, week!, teams);
        }

        private async Task<(int seasonId, Week week, List<Team> teams)> GetSeasonWeekAndTeamsOneTeamForPairingsAsync()
        {
            var seasonId = await CreateSeasonWithOneTeamAndSubmissionsClosedWeek();
            var week = await GetWeekWithSubmissionsAsync(seasonId, 1);
            var teams = await GetTeamsAsync(seasonId);
            return (seasonId, week!, teams);
        }

        private async Task<(int seasonId, Week week, List<Team> teams)> GetSeasonWeekAndTeamsNoTeamMatchupsForPairingsAsync(int teamCount = 2, int playersPerTeam = 2)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            for (int i = 0; i < teamCount; i++)
            {
                var captain = await CreatePlayer((ulong)(3000 + i * 100));
                var teamId = await CreateTeam(seasonId, $"Team{i + 1}", captain.Id);
                for (int j = 1; j <= playersPerTeam; j++)
                {
                    var player = await CreatePlayer((ulong)(3000 + i * 100 + j));
                    await AddPlayerToTeam(player.Id, seasonId, teamId);
                }
            }
            await CreateWeekAsync(seasonId, 1, playersPerTeam);
            await SetWeekStatusAsync(seasonId, 1, WeekStatus.SubmissionsClosed);
            var teams = await GetTeamsAsync(seasonId);
            foreach (var team in teams)
            {
                var teamPlayerIds = await GetTeamPlayerIds(seasonId, team.Id);
                for (int seat = 1; seat <= playersPerTeam; seat++)
                    await AddDeckSubmissionForWeekAsync(seasonId, 1, teamPlayerIds[seat - 1], seat);
            }
            var week = await GetWeekWithSubmissionsAsync(seasonId, 1);
            return (seasonId, week!, teams);
        }

        private async Task SetWeekStatusInProgress(int seasonId, int weekNumber)
        {
            await SetWeekStatusAsync(seasonId, weekNumber, WeekStatus.InProgress);
        }

        private async Task SetWeekStatusCompleted(int seasonId, int weekNumber)
        {
            await SetWeekStatusAsync(seasonId, weekNumber, WeekStatus.Completed);
        }

        private async Task CloseSubmissions(int seasonId)
        {
            await CloseSubmissionsAsync(seasonId);
        }

        /// <summary>
        /// Prepares a season in Playoffs phase with one conference, N playoff teams, week 1 completed
        /// (round-robin with reported results), week 2 created as the first playoff week.
        /// Use for testing EnsureTeamMatchupsForWeekAsync with different playoff team counts (e.g. 4 vs 5).
        /// </summary>
        private async Task<(int seasonId, Week week2, List<Team> teams)> GetSeasonWeekAndTeamsForPlayoffsFirstWeekSingleConferenceAsync(int playoffTeamCount, int playersPerTeam = 2)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            (await _conferenceService.CreateAsync(seasonId, "Default", playoffTeamCount)).Success.ShouldBeTrue();
            var playerIdBase = 5000u;
            for (int i = 0; i < playoffTeamCount; i++)
            {
                var captain = await CreatePlayer(playerIdBase + (ulong)(i * 100));
                var teamId = await CreateTeam(seasonId, $"Team{i + 1}", captain.Id);
                for (int j = 1; j <= playersPerTeam - 1; j++)
                {
                    var player = await CreatePlayer(playerIdBase + (ulong)(i * 100 + j));
                    await AddPlayerToTeam(player.Id, seasonId, teamId);
                }
            }
            await CreateWeekAsync(seasonId, 1, playersPerTeam);
            await OpenWeekAsync(seasonId, 1);
            var teams = await GetTeamsAsync(seasonId);
            foreach (var team in teams)
            {
                var teamPlayerIds = await GetTeamPlayerIds(seasonId, team.Id);
                for (int seat = 1; seat <= playersPerTeam; seat++)
                    await SubmitDeckAsync(seasonId, teamPlayerIds[seat - 1], seat);
            }
            await CloseSubmissionsAsync(seasonId);
            (await _weekService.TransitionToInProgressAsync(seasonId)).Success.ShouldBeTrue();
            var week1 = await _weekRepository.GetByWeekNumberAndSeasonAsync(1, seasonId);
            var matches = await _matchRepository.GetByWeekIdAsync(week1!.Id);
            foreach (var group in matches.GroupBy(m => new { m.Team1Id, m.Team2Id }))
            {
                var loserTeamId = group.Key.Team2Id;
                var loserPlayerIds = await GetTeamPlayerIds(seasonId, loserTeamId);
                foreach (var match in group)
                {
                    var loserId = loserPlayerIds.Contains(match.Player1Id) ? match.Player1Id : match.Player2Id;
                    (await _matchService.ReportLossAsync(seasonId, loserId, "https://example.com/seed")).Success.ShouldBeTrue();
                }
            }
            (await _weekService.TransitionToCompletedAsync(seasonId)).Success.ShouldBeTrue();
            (await _seasonService.SetPhaseToPlayoffsAsync(seasonId)).Success.ShouldBeTrue();
            await CreateWeekAsync(seasonId, 2, playersPerTeam);
            var week2 = await _weekRepository.GetByWeekNumberAndSeasonAsync(2, seasonId);
            teams = await GetTeamsAsync(seasonId);
            return (seasonId, week2!, teams);
        }

        /// <summary>
        /// Prepares a season with 4 teams in one conference, PlayoffTeamsCount = 3 (only 3 make playoffs),
        /// one round-robin week where Team1 and Team3 win, Team2 and Team4 lose (2 tied at 0 wins).
        /// Tiebreaker (lower Team.Id) decides which of the two tied teams gets the last playoff spot.
        /// Returns (seasonId, week2, teams) with teams ordered by Id so teams[0]=Team1, teams[1]=Team2, etc.
        /// </summary>
        private async Task<(int seasonId, Week week2, List<Team> teams)> GetSeasonFourTeamsThreePlayoffSlotsTiebreakerScenarioAsync(int playersPerTeam = 2)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            (await _conferenceService.CreateAsync(seasonId, "Default", 3)).Success.ShouldBeTrue(); // only 3 playoff slots
            var playerIdBase = 7000u;
            for (int i = 0; i < 4; i++)
            {
                var captain = await CreatePlayer(playerIdBase + (ulong)(i * 100));
                var teamId = await CreateTeam(seasonId, $"Team{i + 1}", captain.Id);
                for (int j = 1; j <= playersPerTeam - 1; j++)
                {
                    var player = await CreatePlayer(playerIdBase + (ulong)(i * 100 + j));
                    await AddPlayerToTeam(player.Id, seasonId, teamId);
                }
            }
            await CreateWeekAsync(seasonId, 1, playersPerTeam);
            await OpenWeekAsync(seasonId, 1);
            var teams = (await GetTeamsAsync(seasonId)).OrderBy(t => t.Id).ToList();
            foreach (var team in teams)
            {
                var teamPlayerIds = await GetTeamPlayerIds(seasonId, team.Id);
                for (int seat = 1; seat <= playersPerTeam; seat++)
                    await SubmitDeckAsync(seasonId, teamPlayerIds[seat - 1], seat);
            }
            await CloseSubmissionsAsync(seasonId);
            (await _weekService.TransitionToInProgressAsync(seasonId)).Success.ShouldBeTrue();
            var week1 = await _weekRepository.GetByWeekNumberAndSeasonAsync(1, seasonId);
            var matches = await _matchRepository.GetByWeekIdAsync(week1!.Id);
            // Report so Team2 and Team4 lose each matchup → Team1 and Team3 win (2 winners, 2 losers)
            foreach (var group in matches.GroupBy(m => new { m.Team1Id, m.Team2Id }))
            {
                var loserTeamId = group.Key.Team2Id;
                var loserPlayerIds = await GetTeamPlayerIds(seasonId, loserTeamId);
                foreach (var match in group)
                {
                    var loserId = loserPlayerIds.Contains(match.Player1Id) ? match.Player1Id : match.Player2Id;
                    (await _matchService.ReportLossAsync(seasonId, loserId, "https://example.com/tiebreaker")).Success.ShouldBeTrue();
                }
            }
            (await _weekService.TransitionToCompletedAsync(seasonId)).Success.ShouldBeTrue();
            (await _seasonService.SetPhaseToPlayoffsAsync(seasonId)).Success.ShouldBeTrue();
            await CreateWeekAsync(seasonId, 2, playersPerTeam);
            var week2 = await _weekRepository.GetByWeekNumberAndSeasonAsync(2, seasonId);
            teams = (await GetTeamsAsync(seasonId)).OrderBy(t => t.Id).ToList();
            return (seasonId, week2!, teams);
        }

        /// <summary>
        /// Prepares a season with two conferences, week 1 completed (round-robin with reported results),
        /// still in RoundRobin phase (no phase switch). Use for testing guards that require Playoffs (e.g. UpdateSeed).
        /// </summary>
        private async Task<(int seasonId, List<Team> teams)> GetSeasonWithRoundRobinWeekCompleted_NotPlayoffsAsync(int teamsPerConference = 2, int playersPerTeam = 2)
        {
            var (seasonId, _) = await CreateSeasonWithTwoConferencesAndSubmissions(teamsPerConference, playersPerTeam);
            await CloseSubmissionsAsync(seasonId);
            (await _weekService.TransitionToInProgressAsync(seasonId)).Success.ShouldBeTrue();
            var week1 = await _weekRepository.GetByWeekNumberAndSeasonAsync(1, seasonId);
            var teams = (await GetTeamsAsync(seasonId)).OrderBy(t => t.Id).ToList();
            var matches = await _matchRepository.GetByWeekIdAsync(week1!.Id);
            var byTeamMatchup = matches.GroupBy(m => new { m.Team1Id, m.Team2Id }).ToList();
            foreach (var group in byTeamMatchup)
            {
                var loserTeamId = group.Key.Team2Id;
                var loserPlayerIds = await GetTeamPlayerIds(seasonId, loserTeamId);
                foreach (var match in group)
                {
                    var loserId = loserPlayerIds.Contains(match.Player1Id) ? match.Player1Id : match.Player2Id;
                    (await _matchService.ReportLossAsync(seasonId, loserId, "https://example.com/rr")).Success.ShouldBeTrue();
                }
            }
            (await _weekService.TransitionToCompletedAsync(seasonId)).Success.ShouldBeTrue();
            return (seasonId, teams);
        }

        /// <summary>
        /// Prepares a season in Playoffs phase with week 1 completed (round-robin with winners),
        /// week 2 created as the first playoff week. Use for testing EnsureTeamMatchupsForWeekAsync
        /// and GeneratePairingsAsync with PlayoffService (MatchupServiceFactory resolves to PlayoffService).
        /// </summary>
        private async Task<(int seasonId, Week week2, List<Team> teams)> GetSeasonWeekAndTeamsForPlayoffsFirstWeekAsync(int teamsPerConference = 2, int playersPerTeam = 2)
        {
            var (seasonId, _) = await CreateSeasonWithTwoConferencesAndSubmissions(teamsPerConference, playersPerTeam);
            await CloseSubmissionsAsync(seasonId);
            (await _weekService.TransitionToInProgressAsync(seasonId)).Success.ShouldBeTrue();
            var week1 = await _weekRepository.GetByWeekNumberAndSeasonAsync(1, seasonId);
            var teams = (await GetTeamsAsync(seasonId)).OrderBy(t => t.Id).ToList();
            var matches = await _matchRepository.GetByWeekIdAsync(week1!.Id);
            // Report results so each round-robin team matchup has a clear winner (one team loses per matchup)
            var byTeamMatchup = matches.GroupBy(m => new { m.Team1Id, m.Team2Id }).ToList();
            foreach (var group in byTeamMatchup)
            {
                var loserTeamId = group.Key.Team2Id;
                var loserPlayerIds = await GetTeamPlayerIds(seasonId, loserTeamId);
                foreach (var match in group)
                {
                    var loserId = loserPlayerIds.Contains(match.Player1Id) ? match.Player1Id : match.Player2Id;
                    (await _matchService.ReportLossAsync(seasonId, loserId, "https://example.com/playoffs-seed")).Success.ShouldBeTrue();
                }
            }
            (await _weekService.TransitionToCompletedAsync(seasonId)).Success.ShouldBeTrue();
            (await _seasonService.SetPhaseToPlayoffsAsync(seasonId)).Success.ShouldBeTrue();
            await CreateWeekAsync(seasonId, 2, playersPerTeam);
            var week2 = await _weekRepository.GetByWeekNumberAndSeasonAsync(2, seasonId);
            return (seasonId, week2!, teams);
        }

        /// <summary>
        /// Prepares a season in Playoffs phase with two conferences, each with a configurable playoff team count,
        /// one week of round-robin completed, week 2 created as the first playoff week.
        /// Use for testing that PlayoffTeamsCount per conference determines how many teams qualify (e.g. 3 per conference = 6 total).
        /// </summary>
        private async Task<(int seasonId, Week week2, List<Team> teams)> GetSeasonWeekAndTeamsForPlayoffsFirstWeekWithConferencePlayoffCountAsync(
            int teamsPerConference = 4, int playoffTeamsPerConference = 3, int playersPerTeam = 2)
        {
            var (_, seasonId) = await CreateFormatAndSeason();
            (await _conferenceService.CreateAsync(seasonId, "Alpha", playoffTeamsPerConference)).Success.ShouldBeTrue();
            (await _conferenceService.CreateAsync(seasonId, "Beta", playoffTeamsPerConference)).Success.ShouldBeTrue();
            var playerIdBase = 6000u;
            for (int i = 0; i < teamsPerConference * 2; i++)
            {
                var conferenceName = i < teamsPerConference ? "Alpha" : "Beta";
                var captain = await CreatePlayer(playerIdBase + (ulong)(i * 100));
                var teamId = await CreateTeam(seasonId, $"Team{i + 1}", captain.Id, conferenceName);
                for (int j = 1; j <= playersPerTeam; j++)
                {
                    var player = await CreatePlayer(playerIdBase + (ulong)(i * 100 + j));
                    await AddPlayerToTeam(player.Id, seasonId, teamId);
                }
            }
            await CreateWeekAsync(seasonId, 1, playersPerTeam);
            await OpenWeekAsync(seasonId, 1);
            var teams = (await GetTeamsAsync(seasonId)).OrderBy(t => t.Id).ToList();
            foreach (var team in teams)
            {
                var teamPlayerIds = await GetTeamPlayerIds(seasonId, team.Id);
                for (int seat = 1; seat <= playersPerTeam; seat++)
                    await SubmitDeckAsync(seasonId, teamPlayerIds[seat - 1], seat);
            }
            await CloseSubmissionsAsync(seasonId);
            (await _weekService.TransitionToInProgressAsync(seasonId)).Success.ShouldBeTrue();
            var week1 = await _weekRepository.GetByWeekNumberAndSeasonAsync(1, seasonId);
            var matches = await _matchRepository.GetByWeekIdAsync(week1!.Id);
            var byTeamMatchup = matches.GroupBy(m => new { m.Team1Id, m.Team2Id }).ToList();
            foreach (var group in byTeamMatchup)
            {
                var loserTeamId = group.Key.Team2Id;
                var loserPlayerIds = await GetTeamPlayerIds(seasonId, loserTeamId);
                foreach (var match in group)
                {
                    var loserId = loserPlayerIds.Contains(match.Player1Id) ? match.Player1Id : match.Player2Id;
                    (await _matchService.ReportLossAsync(seasonId, loserId, "https://example.com/playoffs-seed")).Success.ShouldBeTrue();
                }
            }
            (await _weekService.TransitionToCompletedAsync(seasonId)).Success.ShouldBeTrue();
            (await _seasonService.SetPhaseToPlayoffsAsync(seasonId)).Success.ShouldBeTrue();
            await CreateWeekAsync(seasonId, 2, playersPerTeam);
            var week2 = await _weekRepository.GetByWeekNumberAndSeasonAsync(2, seasonId);
            return (seasonId, week2!, teams);
        }

        /// <summary>
        /// Completes a playoff week: open week, add submissions for teams in matchups, close submissions,
        /// transition to InProgress (generates pairings), report losers for each normal matchup (in bracket order), then complete.
        /// </summary>
        private async Task CompletePlayoffWeekAsync(int seasonId, int weekNumber, List<Team> teams, int[] loserTeamIdsPerMatchup)
        {
            var week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, seasonId);
            week.ShouldNotBeNull();
            (await _weekService.TransitionToOpenWeekAsync(seasonId, weekNumber)).Success.ShouldBeTrue();
            var playoffMatchups = _context.PlayoffMatchups.Where(pm => pm.WeekId == week!.Id).OrderBy(pm => pm.BracketPosition).ToList();
            var requiredTeamIds = playoffMatchups.SelectMany(pm => new[] { pm.Team1Id, pm.Team2Id }).Distinct().ToList();
            foreach (var teamId in requiredTeamIds)
            {
                var playerIds = await GetTeamPlayerIds(seasonId, teamId);
                for (int seat = 1; seat <= playerIds.Count; seat++)
                    await AddDeckSubmissionForWeekAsync(seasonId, weekNumber, playerIds[seat - 1], seat);
            }
            (await _weekService.TransitionToCloseSubmissionsAsync(seasonId)).Success.ShouldBeTrue();
            (await _weekService.TransitionToInProgressAsync(seasonId)).Success.ShouldBeTrue();
            var matches = await _matchRepository.GetByWeekIdAsync(week!.Id);
            int loserIndex = 0;
            foreach (var pm in playoffMatchups.Where(pm => pm.MatchupType == MatchupType.Normal))
            {
                var teamMatchupMatches = matches.Where(m =>
                    (m.Team1Id == pm.Team1Id && m.Team2Id == pm.Team2Id) || (m.Team1Id == pm.Team2Id && m.Team2Id == pm.Team1Id)).ToList();
                var loserTeamId = loserTeamIdsPerMatchup[loserIndex++];
                var loserPlayerIds = await GetTeamPlayerIds(seasonId, loserTeamId);
                foreach (var match in teamMatchupMatches)
                {
                    var loserId = loserPlayerIds.Contains(match.Player1Id) ? match.Player1Id : match.Player2Id;
                    (await _matchService.ReportLossAsync(seasonId, loserId, "https://example.com/playoff")).Success.ShouldBeTrue();
                }
            }
            (await _weekService.TransitionToCompletedAsync(seasonId)).Success.ShouldBeTrue();
        }

        #endregion
    }
}
