using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Model;

namespace WarLeague.Discord.Commands.ResponseEmbeds
{
    public static class RoundSummaryEmbed
    {
        public static Embed Build(RoundSummaryResult result)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🏆 Round Summary")
                .WithColor(Color.Gold);

            var lines = result.WeeklyResults
                .OrderBy(w => w.WeekNumber)
                .Select(w =>
                {
                    var indicator = w.Wins >= w.Loses ? ":medal:" : ":small_red_triangle_down:";
                    return $"{indicator} ({w.Wins}-{w.Loses}) Week {w.WeekNumber} vs. {w.OpposingTeamName}";
                });

            embed.WithDescription(string.Join("\n", lines));

            return embed.Build();
        }
    }
}
