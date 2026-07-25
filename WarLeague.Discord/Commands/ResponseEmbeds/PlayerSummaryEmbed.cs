using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Model;

namespace WarLeague.Discord.Commands.ResponseEmbeds
{
    public static class PlayerSummaryEmbed
    {
        public static Embed Build(PlayerSummaryResult result)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🏆 Player Summary")
                .WithColor(Color.Gold);

            foreach (var player in result.PlayerResults)
            {
                var indicator = player.Wins >= player.Loses ? ":medal:" : ":small_red_triangle_down:";

                var lines = player.PlayerVsPlayerResults
                    .OrderBy(p => p.WeekNumber)
                    .Select(p =>
                    {
                        var isNoShow = p.GameWins == 0 && p.GameLoses == 0;

                        var gameIndicator = isNoShow
                            ? ":ghost:"
                            : p.GameWins >= p.GameLoses ? ":white_check_mark:" : ":x:";

                        var score = isNoShow ? "No-show" : $"{p.GameWins}-{p.GameLoses}";

                        var line = $"{gameIndicator} Week {p.WeekNumber}: {p.DeckType} vs. {p.OpponentName} ({p.OpposingDeckType}) ({score})";

                        if (!isNoShow && !string.IsNullOrWhiteSpace(p.Replay))
                            line += $" [Replay]({p.Replay})";

                        return line;
                    });

                var fieldValue = string.Join("\n", lines);

                if (string.IsNullOrWhiteSpace(fieldValue))
                    fieldValue = "No matches played.";

                embed.AddField(
                    $"{indicator} {player.Name} ({player.Wins}-{player.Loses})",
                    fieldValue);
            }

            return embed.Build();
        }
    }
}
