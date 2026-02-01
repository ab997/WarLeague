using Discord.Interactions;
using System.Globalization;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Domain.Model;
using WarLeague.Core.Domain.Services;
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
        private readonly WeekService _weekService;
        private readonly DiscordApiHelperService _helperService;
        public WeekCommands(
            DiscordApiHelperService helperService,
            WeekService weekService)
        {
            _helperService = helperService;
            _weekService = weekService;
        }
        [SlashCommand("create", "Creates a week")]
        public async Task Create(int weekNumber,
            [Summary("start-date", "Start date (YYYY-MM-DD)")] string startDateStr,
            [Summary("end-date", "End date (YYYY-MM-DD)")] string endDateStr,
            [Summary("submissions-close-date", "Submissions close date (YYYY-MM-DD)")] string subCloseDateStr
            )
        {
            await DeferAsync(ephemeral: false);

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

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            Week? week = await _weekService.CreateAsync(season.Id, weekNumber, startDate, endDate, subCloseDate);

            if (week is null)
            {
                await FollowupAsync($"Week with number {weekNumber} already exists.");
                return;
            }

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

            

            // Parse provided dates (only when provided)
            DateTime? startDate = null;
            if (startDateStr is not null)
            {
                if (!DateTime.TryParse(startDateStr, out var parsedStart))
                {
                    await FollowupAsync("Invalid start date format. Use YYYY-MM-DD.", ephemeral: false);
                    return;
                }
                startDate = parsedStart;
            }

            DateTime? endDate = null;
            if (endDateStr is not null)
            {
                if (!DateTime.TryParse(endDateStr, out var parsedEnd))
                {
                    await FollowupAsync("Invalid end date format. Use YYYY-MM-DD.", ephemeral: false);
                    return;
                }
                endDate = parsedEnd;
            }

            DateTime? subCloseDate = null;
            if (subCloseDateStr is not null)
            {
                if (!DateTime.TryParse(subCloseDateStr, out var parsedSubClose))
                {
                    await FollowupAsync("Invalid submissions close date format. Use YYYY-MM-DD.", ephemeral: false);
                    return;
                }
                subCloseDate = parsedSubClose;
            }

            // Validate logical ordering
            if (startDate > endDate)
            {
                await FollowupAsync("Start date must be before end date.", ephemeral: false);
                return;
            }

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            Week? week = await _weekService.UpdateAsync(season.Id, weekNumber, startDate, endDate, subCloseDate, status);

            if (week is null)
            {
                await FollowupAsync($"Week with number {weekNumber} does not exist.");
                return;
            }

            await FollowupAsync($"Week {weekNumber} updated.", ephemeral: false);
        }

        [SlashCommand("start", "Starts the current week by closing submissions")]
        public async Task StartAsync(int requiredDecksByTeams = 5)
        {
            await DeferAsync(ephemeral: false);

            Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

            Result result = await _weekService.StartWeekAsync(season.Id, requiredDecksByTeams);

            await FollowupAsync(result.Message);
        }
    }
}
