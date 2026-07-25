
using WarLeague.Data;
using WarLeague.Data.Entities;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using System.Xml.Linq;
using WarLeague.Data.Enums;

namespace WarLeague.Core.Services
{
    public class TeamService
    {
        private readonly WarLeagueDbContext _context;
        private readonly TeamRepository _teamRepository;
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        private readonly SeasonRepository _seasonRepository;
        private readonly PlayerRepository _playerRepository;
        private readonly ConferenceRepository _conferenceRepository;
        private readonly MatchRepository _matchRepository;
        private readonly DeckSubmissionRepository _deckSubmissionRepository;

        public TeamService(WarLeagueDbContext context, TeamRepository teamRepository, PlayerSeasonTeamRepository playerSeasonTeamRepository, SeasonRepository seasonRepository, PlayerRepository playerRepository, ConferenceRepository conferenceRepository, MatchRepository matchRepository, DeckSubmissionRepository deckSubmissionRepository)
        {
            _context = context;
            _teamRepository = teamRepository;
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
            _seasonRepository = seasonRepository;
            _playerRepository = playerRepository;
            _conferenceRepository = conferenceRepository;
            _matchRepository = matchRepository;
            _deckSubmissionRepository = deckSubmissionRepository;
        }

        public async Task<BaseResult> CreateAsync(int seasonId, string teamName, int captainId, string conferenceName, bool canBypassTeamModificationCheck, ulong? discordRoleId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }

            if (string.IsNullOrWhiteSpace(conferenceName))
            {
                return new BaseResult { Success = false, Message = "Conference name is required." };
            }

            Team? check = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);

            if (check is not null)
            {
                return new BaseResult { Success = false, Message = $"A team with the name '{teamName}' already exists." };
            }

            Player player = await _playerRepository.GetByIdAsync(captainId);

            bool success = await _playerSeasonTeamRepository.EnsurePlayerIsNotMemberOfTeamInSeasonAsync(player.Id, season.Id);

            if (!success)
            {
                return new BaseResult { Success = false, Message = $"Player {player.UserName} already a member of another team." };
            }

            Conference? conference = await _conferenceRepository.GetByNameAndSeasonAsync(conferenceName.Trim(), seasonId);
            if (conference is null)
            {
                return new BaseResult { Success = false, Message = $"Conference '{conferenceName}' does not exist in this season." };
            }

            Team team = new Team
            {
                Name = teamName,
                Captain = player,
                CreatedDate = DateTime.UtcNow,
                Season = season,
                ConferenceId = conference.Id,
                DiscordRoleId = discordRoleId
            };

            await _teamRepository.AddAsync(team);

            PlayerSeasonTeam pst = new PlayerSeasonTeam
            {
                Player = player,
                Season = season,
                Team = team
            };

            await _playerSeasonTeamRepository.AddAsync(pst);

            await transaction.CommitAsync();

            return new BaseResult { Success = true, Message = $"Team created successfully with {player.UserName} as captain." };
        }

        public async Task<BaseResult> DeleteAsync(int seasonId, string teamName)
        {
            Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);
            if (team == null)
            {
                return new BaseResult(false, $"Team with name '{teamName}' not found.");
            }

            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            // no bypass param consciously: not even admins have any reason to delete teams mid season
            if (season.DisableTeamModification)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }

            bool hasMatches = await _matchRepository.AnyMatchReferencesTeamAsync(team.Id);
            if (hasMatches)
            {
                return new BaseResult { Success = false, Message = "Team cannot be deleted because it has matches or matchups. Remove or reassign those first." };
            }

            await _teamRepository.DeleteAsync(team);

            return new BaseResult(true, $"Team '{teamName}' deleted.");
        }
        /// <summary>
        /// add a member to captain's team
        /// </summary>
        /// <param name="seasonId"></param>
        /// <param name="teamName"></param>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public async Task<BaseResult> CaptainAddMemberAsync(int seasonId, int captainId, int playerId, bool canBypassTeamModificationCheck)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }

            Team? team = await _teamRepository.GetByCaptainAndSeasonAsync(captainId, seasonId);

            if (team is null)
            {
                return new BaseResult { Success = false, Message = $"Player with id {captainId} is not captain of any team in this season." };
            }

            return await AddMemberAsync(seasonId, playerId, team.Id, canBypassTeamModificationCheck);
        }

        public async Task<BaseResult> AddMemberAsync(int seasonId, int playerId, string teamName, bool canBypassTeamModificationCheck)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }

            Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);
            if (team is null)
            {
                return new BaseResult { Success = false, Message = $"Team with name '{teamName}' does not exist in this season." };
            }
            return await AddMemberAsync(seasonId, playerId, team.Id, canBypassTeamModificationCheck);
        }

        public async Task<BaseResult> AddMemberAsync(int seasonId, int playerId, int teamId, bool canBypassTeamModificationCheck)
        {
            bool notMember = await _playerSeasonTeamRepository.EnsurePlayerIsNotMemberOfTeamInSeasonAsync(playerId, seasonId);

            if (!notMember)
            {
                return new BaseResult { Success = false, Message = $"Player with id {playerId} is already a member of another team in this season." };
            }

            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }

            PlayerSeasonTeam pst = new PlayerSeasonTeam
            {
                PlayerId = playerId,
                SeasonId = seasonId,
                TeamId = teamId
            };

            await _playerSeasonTeamRepository.AddAsync(pst);

            return new BaseResult { Success = true, Message = $"Added player to team." };
        }

        public async Task<BaseResult> CaptainRemoveMemberAsync(int seasonId, int captainId, int playerId, bool canBypassTeamModificationCheck)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }

            Team? team = await _teamRepository.GetByCaptainAndSeasonAsync(captainId, seasonId);

            if (team is null)
            {
                return new BaseResult { Success = false, Message = $"Player with id {captainId} is not captain of any team in this season." };
            }

            return await RemoveMemberFromTeamAsync(seasonId, playerId, team, canBypassTeamModificationCheck);
        }

        public async Task<BaseResult> RemoveMemberFromTeamAsync(int seasonId, int playerId, Team team, bool canBypassTeamModificationCheck)
        {
            // Prevent dropping the captain
            if (team.CaptainId == playerId)
            {
                return new BaseResult { Success = false, Message = "The team captain cannot be removed. Transfer captainship first if needed." };
            }

            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }

            // Ensure the user is a member of this team in the current season
            PlayerSeasonTeam? pst =
                await _playerSeasonTeamRepository.GetByPlayerSeasonAndTeamAsync(
                    playerId,
                    seasonId,
                    team.Id);

            if (pst == null)
            {
                return new BaseResult { Success = false, Message = $"Player with id {playerId} is not a member of team '{team.Name}'." };
            }

            await _playerSeasonTeamRepository.DeleteAsync(pst);

            return new BaseResult { Success = true, Message = $"Removed player from team '{team.Name}'." };
        }

        public async Task<BaseResult> RemoveMemberAsync(int seasonId, int playerId, bool canBypassTeamModificationCheck)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }

            // Ensure the user is a member of this team in the current season
            PlayerSeasonTeam? pst =
                await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(
                    playerId,
                    seasonId);

            if (pst == null)
            {
                return new BaseResult { Success = false, Message = $"Player with id {playerId} is not a member of any team." };
            }

            Team team = pst.Team;

            // Prevent dropping the captain
            if (team.CaptainId == playerId)
            {
                return new BaseResult { Success = false, Message = "The team captain cannot be removed. Transfer captainship first if needed." };
            }

            await _playerSeasonTeamRepository.DeleteAsync(pst);

            return new BaseResult { Success = true, Message = $"Removed player from team '{team.Name}'." };
        }
        public async Task<BaseResult> TransferMemberAsync(int seasonId, int playerId, string teamName, bool canBypassTeamModificationCheck)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }
            Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);
            if (team == null)
            {
                return new BaseResult { Success = false, Message = $"Team with name '{teamName}' does not exist in this season." };
            }
            return await TransferMemberAsync(seasonId, playerId, team.Id, canBypassTeamModificationCheck);
        }
        public async Task<BaseResult> TransferMemberAsync(int seasonId, int playerId, int teamId, bool canBypassTeamModificationCheck)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }

            // Captains cannot be transferred
            bool canTransfer = await _playerSeasonTeamRepository.EnsurePlayerIsNotCaptainOfTeamInSeasonAsync(playerId, seasonId);
            if (!canTransfer)
            {
                return new BaseResult { Success = false, Message = "Captains cannot be transferred." };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            BaseResult result1 = await RemoveMemberAsync(seasonId, playerId, canBypassTeamModificationCheck);

            if (!result1.Success)
            {
                return result1;
            }

            BaseResult result2 = await AddMemberAsync(seasonId, playerId, teamId, canBypassTeamModificationCheck);

            if (!result2.Success)
            {
                return result2;
            }

            await transaction.CommitAsync();

            return new BaseResult { Success = true, Message = "Player transferred successfully." };
        }

        public async Task<BaseResult> TransferCaptainshipAsync(int seasonId, int newCaptainId, string teamName, bool canBypassTeamModificationCheck)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }
            Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);
            if (team == null)
            {
                return new BaseResult { Success = false, Message = $"Team with name '{teamName}' does not exist in this season." };
            }

            // Ensure the user is already a member of the team
            PlayerSeasonTeam? pst =
                await _playerSeasonTeamRepository.GetByPlayerSeasonAndTeamAsync(
                    newCaptainId,
                    seasonId,
                    team.Id);

            if (pst == null)
            {
                return new BaseResult { Success = false, Message = $"Player with id {newCaptainId} is not a member of team '{team.Name}'. Captainship transfer is supported only among existing members." };
            }

            team.CaptainId = newCaptainId;

            await _teamRepository.UpdateAsync(team);

            return new BaseResult { Success = true, Message = "Captainship transferred successfully." };
        }

        public async Task<BaseResult> AssignDiscordRoleIdAsync(int seasonId, string teamName, ulong discordRoleId, bool canBypassTeamModificationCheck)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }
            Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);
            if (team == null)
            {
                return new BaseResult { Success = false, Message = $"Team with name '{teamName}' does not exist in this season." };
            }

            team.DiscordRoleId = discordRoleId;

            await _teamRepository.UpdateAsync(team);

            return new BaseResult { Success = true, Message = $"Discord role assigned to team '{teamName}' successfully." };
        }

        public async Task<BaseResult> UpdateConferenceAsync(int seasonId, string teamName, string conferenceName, bool canBypassTeamModificationCheck)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new BaseResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }

            if (string.IsNullOrWhiteSpace(conferenceName))
            {
                return new BaseResult { Success = false, Message = "Conference name is required." };
            }

            Team? team = await _teamRepository.GetByNameAndSeasonAsync(teamName, seasonId);
            if (team is null)
            {
                return new BaseResult { Success = false, Message = $"Team with name '{teamName}' does not exist in this season." };
            }

            Conference? conference = await _conferenceRepository.GetByNameAndSeasonAsync(conferenceName.Trim(), seasonId);
            if (conference is null)
            {
                return new BaseResult { Success = false, Message = $"Conference '{conferenceName}' does not exist in this season." };
            }

            if (team.ConferenceId == conference.Id)
            {
                return new BaseResult { Success = true, Message = $"Team '{teamName}' is already in conference '{conference.Name}'." };
            }

            team.ConferenceId = conference.Id;
            await _teamRepository.UpdateAsync(team);

            return new BaseResult { Success = true, Message = $"Team '{teamName}' moved to conference '{conference.Name}'." };
        }

        public async Task<RoleResult> RenameAsync(int seasonId, string oldName, string newName, bool canBypassTeamModificationCheck)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);

            if (season.DisableTeamModification && !canBypassTeamModificationCheck)
            {
                return new RoleResult { Success = false, Message = "Team modifications are currently disabled for this season." };
            }

            Team? team = await _teamRepository.GetByNameAndSeasonAsync(oldName, seasonId);
            if (team is null)
            {
                return new RoleResult { Success = false, Message = $"Team with name '{oldName}' does not exist in this season." };
            }

            team.Name = newName;
            await _teamRepository.UpdateAsync(team);

            return new RoleResult { Success = true, Message = $"Team renamed from '{oldName}' to '{newName}' successfully.", DiscordRoleId = team.DiscordRoleId!.Value };
        }
        public async Task<RoundSummaryResult> GetRoundsSummaryAsync(int seasonId, int teamId)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);
            if (season is null)
            {
                return new RoundSummaryResult { Success = false, Message = "Season not found." };
            }

            var team = await _teamRepository.GetByIdAndSeasonAsync(teamId, seasonId);
            if (team is null)
            {
                return new RoundSummaryResult { Success = false, Message = "Team not found." };
            }

            var weeklyResults = new List<WeeklyResult>();

            List<Match> matches = await _matchRepository.GetBySeasonAndTeamIdAsync(seasonId, teamId);
            matches = matches
                .Where(x => x.Status == MatchStatus.Reported)
                .OrderBy(x => x.Week.WeekNumber).ToList();

            var groupByWeek = matches.GroupBy(x => x.Week.WeekNumber).ToList();
            
            foreach (var grp in groupByWeek)
            {
                int wins = grp.Count(x => x.WinnerTeamId == teamId);
                int loses = grp.Count(x => x.WinnerTeamId != teamId);
                string opposingTeamName = grp.First().Team1Id == teamId ? grp.First().Team2.Name : grp.First().Team1.Name;
                weeklyResults.Add(new WeeklyResult
                {
                    WeekNumber = grp.Key,
                    Wins = wins,
                    Loses = loses,
                    OpposingTeamName = opposingTeamName
                });
            }


            return new RoundSummaryResult
            {
                Success = true,
                Message = $"Rounds summary for team '{team.Name}' retrieved successfully.",
                WeeklyResults = weeklyResults,
                TeamName = team.Name
            };
        }

        public async Task<PlayerSummaryResult> GetPlayersSummaryAsync(int seasonId, int teamId)
        {
            var season = await _seasonRepository.GetSingleActiveSeasonByIdAsync(seasonId);
            if (season is null)
            {
                return new PlayerSummaryResult { Success = false, Message = "Season not found." };
            }

            var team = await _teamRepository.GetByIdAndSeasonAsync(teamId, seasonId);
            if (team is null)
            {
                return new PlayerSummaryResult { Success = false, Message = "Team not found." };
            }

            List<Player> players = await _playerSeasonTeamRepository.GetPlayersByTeamAndSeasonAsync(teamId, seasonId);



            List<PlayerResult> playerResults = [];
            foreach (Player player in players)
            {
                List<DeckSubmission> deckSubmissions = await _deckSubmissionRepository.GetByPlayerAndSeasonAsync(player.Id, seasonId);
                // get reported matches for this player directly from deck submissions
                List<Match> matches = deckSubmissions
                    .Select(x => x.Week)
                    .SelectMany(x => x.Matches)
                    .Where(x => x.Status == MatchStatus.Reported
                        && (x.Player1Id == player.Id || x.Player2Id == player.Id))
                    .OrderBy(x => x.Week.WeekNumber)
                    .ToList();

                // deckSubmission and match both have weekId and playerId we can match them by that
                Dictionary<(int weekId, int playerId), DeckSubmission> deckSubmissionsDict = deckSubmissions.ToDictionary(
                    x => (x.WeekId, x.PlayerId));



                int totalWins = matches.Count(x => x.WinnerId == player.Id);
                int totalLoses = matches.Count(x => x.WinnerId != player.Id);

                PlayerResult playerResult = new PlayerResult
                {
                    Wins = totalWins,
                    Loses = totalLoses,
                    Name = player.UserName,
                    PlayerVsPlayerResults = []
                };
                foreach (Match match in matches)
                {
                    int opponentId = match.Player1Id == player.Id ? match.Player2Id : match.Player1Id;
                    // due to above deckSubmission query it is just easier to get player2 from repo than to
                    // do ef core inlcude thenindlude trickery
                    Player opponent = await _playerRepository.GetByIdAsync(opponentId);
                    string deckType = deckSubmissionsDict[(match.Week.WeekNumber, player.Id)].DeckType;
                    List<DeckSubmission> opponentsDeckSubmissions = await _deckSubmissionRepository.GetByPlayerAndSeasonAsync(opponentId, seasonId);
                    string opponentsDeckType = opponentsDeckSubmissions.Single(x => x.Week.WeekNumber == match.Week.WeekNumber
                        && x.PlayerId == opponentId).DeckType;

                    // 0 in case of no-show
                    int gameWins = match.Player1Id == player.Id ? match.Player1Wins ?? 0 : match.Player2Wins ?? 0;
                    int gameLoses = match.Player1Id == player.Id ? match.Player2Wins ?? 0 : match.Player1Wins ?? 0;
                    PlayerVsPlayerResult playerVsPlayerResult = new PlayerVsPlayerResult
                    {
                        DeckType = deckType,
                        OpposingDeckType = opponentsDeckType,
                        OpponentName = opponent.UserName,
                        Replay = match.ReplayUrl ?? "",
                        WeekNumber = match.Week.WeekNumber,
                        GameWins = gameWins,
                        GameLoses = gameLoses
                    };

                    playerResult.PlayerVsPlayerResults.Add(playerVsPlayerResult);
                }

                playerResults.Add(playerResult);
            }

            return new PlayerSummaryResult
            {
                Success = true,
                PlayerResults = playerResults,
                Message = "Player results generated"
            };
        }
    }
}
