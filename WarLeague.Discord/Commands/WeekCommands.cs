using Discord.Interactions;
using System.Globalization;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands
{
    [Group("week", "Week commands")]
    [RequireRole("Admin")]
    [EnsureSingleActiveSeason]
    [EnsureChannelIsInFormatCategory]
    public class WeekCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly WeekRepository _weekRepository;
        private readonly HelperService _helperService;
        public WeekCommands(WeekRepository weekRepository, HelperService helperService)
        {
            _weekRepository = weekRepository;
            _helperService = helperService;
        }
        [SlashCommand("create", "Creates a week")]
        public async Task Create(int weekNumber,
            [Summary("start-date", "Start date (YYYY-MM-DD)")] string startDateStr,
            [Summary("end-date", "End date (YYYY-MM-DD)")] string endDateStr,
            [Summary("submissions-close-date", "Submissions close date (YYYY-MM-DD)")] string subCloseDateStr
            )
        {
            await DeferAsync(ephemeral: true);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            Week? week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, season.Id);

            if (week != null)
            {
                await FollowupAsync($"Week with number {weekNumber} already exists.");
                return;
            }

            if (!DateTime.TryParse(startDateStr, out var startDate) ||
                !DateTime.TryParse(endDateStr, out var endDate) ||
                !DateTime.TryParse(subCloseDateStr, out var subCloseDate))
            {
                await FollowupAsync("Invalid date format. Use YYYY-MM-DD.", ephemeral: true);
                return;
            }

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
