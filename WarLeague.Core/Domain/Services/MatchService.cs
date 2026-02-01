using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Repositories;

namespace WarLeague.Core.Domain.Services
{
    public class MatchService
    {
        private readonly WeekRepository _weekRepository;
        private readonly TeamRepository _teamRepository;
        private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
        private readonly MatchRepository _matchRepository;
        public MatchService(WeekRepository weekRepository, TeamRepository teamRepository, PlayerSeasonTeamRepository playerSeasonTeamRepository, MatchRepository matchRepository)
        {
            _weekRepository = weekRepository;
            _teamRepository = teamRepository;
            _playerSeasonTeamRepository = playerSeasonTeamRepository;
            _matchRepository = matchRepository;
        }

        public async Task GeneratePairingsAsync()
        {

        }
    }
}
