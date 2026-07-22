using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Model;

namespace WarLeague.Discord.Commands.ResponseEmbeds
{
    public static class ReportWinEmbed
    {
        private static readonly string[] VictoryEmojis =
        {
            "🎉", "🔥", "💪", "😤", "👑", "🐐", "💀", "😎", "🚀",
            "🥇", "🍾", "🎊", "🤯", "😭", "🤡", "💥", "🍿", "🕺",
            "🦾", "🐺", "😈", "🤝", "🎯", "🧊", "🐍", "🥶", "🙏",
            "⚡", "🌪️", "🦍", "🧨", "🏹", "🥋", "🐉", "👹", "🎖️", "🦁",
            "🥂", "📈", "🧟", "🥊", "💯", "🐲", "🦅", "🤙"
        };

        private static readonly Random Rng = new();

        public static Embed Build(ReportWinResult result)
        {
            var emoji = VictoryEmojis[Rng.Next(VictoryEmojis.Length)];

            var embed = new EmbedBuilder()
                .WithTitle("🏆 Match Result")
                .WithDescription($"**{result.Winner}** has won the match! {emoji}")
                .WithColor(Color.Gold)
                .AddField("Winner", $"{result.Winner} — {result.WinnerTeam}", inline: true)
                .AddField("Loser", $"{result.Loser} — {result.LoserTeam}", inline: true)
                ;
            if (!string.IsNullOrWhiteSpace(result.ReplayUrl))
            {
                embed.AddField("Film", $"[Watch Film]({result.ReplayUrl}) 🎥");
            }

            return embed.Build();
        }
    }
}
