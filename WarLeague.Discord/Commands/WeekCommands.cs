using Discord.Interactions;
using System.Globalization;

namespace WarLeague.Discord.Commands
{
    [Group("week", "Week commands")]
    [RequireRole("Admin")]
    public class WeekCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public WeekCommands()
        {
                
        }
        [SlashCommand("create-week", "Creates a week")]
        public async Task CreateWeek(int weekNumber, int seasonNumber, string formatName,
            [Summary("start-date", "Start date (YYYY-MM-DD)")] string startDateStr,
            [Summary("end-date", "End date (YYYY-MM-DD)")] string endDateStr)
        {
            if (!DateTime.TryParse(startDateStr, out var startDate) ||
            !DateTime.TryParse(endDateStr, out var endDate))
            {
                await RespondAsync("Invalid date format. Use YYYY-MM-DD.", ephemeral: true);
                return;
            }
        }
    }
}
