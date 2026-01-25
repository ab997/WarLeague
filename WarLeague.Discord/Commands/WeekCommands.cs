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
        private readonly DiscordApiHelperService _helperService;
        public WeekCommands(WeekRepository weekRepository, DiscordApiHelperService helperService)
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
            await DeferAsync(ephemeral: false);

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
                await FollowupAsync("Invalid date format. Use YYYY-MM-DD.", ephemeral: false);
                return;
            }

            // Validate logical ordering
            if (startDate > endDate)
            {
                await FollowupAsync("Start date must be before end date.", ephemeral: false);
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


        [SlashCommand("update", "Updates a week")]
        public async Task Update(int weekNumber,
            [Summary("start-date", "Start date (YYYY-MM-DD)")] string? startDateStr = null,
            [Summary("end-date", "End date (YYYY-MM-DD)")] string? endDateStr = null,
            [Summary("submissions-close-date", "Submissions close date (YYYY-MM-DD)")] string? subCloseDateStr = null,
            [Summary("status")] WeekStatus? status = null
            )
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            Week? week = await _weekRepository.GetByWeekNumberAndSeasonAsync(weekNumber, season.Id);

            if (week is null)
            {
                await FollowupAsync($"Week with number {weekNumber} does not exist.");
                return;
            }

            // Parse provided dates (only when provided)
            if (startDateStr is not null)
            {
                if (!DateTime.TryParse(startDateStr, out var parsedStart))
                {
                    await FollowupAsync("Invalid start date format. Use YYYY-MM-DD.", ephemeral: false);
                    return;
                }
                week.StartDate = parsedStart;
            }

            if (endDateStr is not null)
            {
                if (!DateTime.TryParse(endDateStr, out var parsedEnd))
                {
                    await FollowupAsync("Invalid end date format. Use YYYY-MM-DD.", ephemeral: false);
                    return;
                }
                week.EndDate = parsedEnd;
            }

            if (subCloseDateStr is not null)
            {
                if (!DateTime.TryParse(subCloseDateStr, out var parsedSubClose))
                {
                    await FollowupAsync("Invalid submissions close date format. Use YYYY-MM-DD.", ephemeral: false);
                    return;
                }
                week.SubmissionsClosedDate = parsedSubClose;
            }

            // Validate logical ordering
            if (week.StartDate > week.EndDate)
            {
                await FollowupAsync("Start date must be before end date.", ephemeral: false);
                return;
            }

            if (status.HasValue)
            {
                week.Status = status.Value;
            }

            await _weekRepository.UpdateAsync(week);

            await FollowupAsync($"Week {weekNumber} updated.", ephemeral: false);
        }
    }
}
