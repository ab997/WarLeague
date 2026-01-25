using Discord.Interactions;
using System.Globalization;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Preconditions;

namespace WarLeague.Discord.Commands
{
    [Group("week", "Week commands")]
    [RequireRole("Admin")]
    [EnsureSingleActiveFormat]
    [EnsureSingleActiveSeason]
    public class WeekCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly FormatRepository _formatRepository;
        private readonly SeasonRepository _seasonRepository;
        private readonly WeekRepository _weekRepository;
        public WeekCommands(SeasonRepository seasonRepository, FormatRepository formatRepository, WeekRepository weekRepository)
        {
            _seasonRepository = seasonRepository;
            _formatRepository = formatRepository;
            _weekRepository = weekRepository;
        }
        [SlashCommand("create", "Creates a week")]
        public async Task Create(int weekNumber,
            [Summary("start-date", "Start date (YYYY-MM-DD)")] string startDateStr,
            [Summary("end-date", "End date (YYYY-MM-DD)")] string endDateStr,
            [Summary("submissions-close-date", "Submissions close date (YYYY-MM-DD)")] string subCloseDateStr
            )
        {
            await DeferAsync(ephemeral: true);

            Week? week = await _weekRepository.GetByWeekNumberAsync(weekNumber);

            if (week != null)
            {
                await FollowupAsync($"Week with number {weekNumber} already exists.");
                return;
            }

            if (!DateTime.TryParse(startDateStr, out var startDate) ||
                !DateTime.TryParse(endDateStr, out var endDate) ||
                !DateTime.TryParse(subCloseDateStr, out var subCloseDate))
            {
                await RespondAsync("Invalid date format. Use YYYY-MM-DD.", ephemeral: true);
                return;
            }

            Season season = await _seasonRepository.GetSingleActiveSeasonAsync();

            Week weekNew = new Week
            {
                WeekNumber = weekNumber,
                Season = season,
                StartDate = startDate,
                EndDate = endDate,
                SubmissionsClosedDate = subCloseDate,
                Status = WeekStatus.Open,
            };

            await _weekRepository.AddAsync(weekNew);

            await FollowupAsync($"Week created.");
        }
    }
}
