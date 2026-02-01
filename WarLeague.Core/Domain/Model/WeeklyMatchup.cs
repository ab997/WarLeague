using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Data.Entities;

namespace WarLeague.Core.Domain.Model
{
    public readonly record struct WeeklyMatchup(
        Team TeamA,
        Team TeamB,
        List<(Player aPlayer, Player bPlayer)> Pairs,
        List<Player> UnpairedA,
        List<Player> UnpairedB);
}
