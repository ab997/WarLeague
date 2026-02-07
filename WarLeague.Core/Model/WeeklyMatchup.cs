using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Data.Entities;

namespace WarLeague.Core.Model
{
    public readonly record struct WeeklyMatchup(
        Team TeamA,
        Team TeamB,
        List<(Player aPlayer, Player bPlayer)> Pairs,
        List<Player> UnpairedA,
        List<Player> UnpairedB);
}
