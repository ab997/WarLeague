using Discord;
using Discord.Interactions;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Repositories;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;

namespace WarLeague.Discord.Commands;

[Group("deck", "Deck submission commands")]
[EnsureChannelIsInFormatCategory]
[EnsureSingleActiveSeason]
public class DeckCommands : InteractionModuleBase<SocketInteractionContext>
{
    private const int MaxDeckFileBytes = 1_000_000; // 1MB safety limit

    private readonly DiscordApiHelperService _helperService;
    private readonly PlayerService _playerService;
    private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
    private readonly TeamRepository _teamRepository;
    private readonly WeekRepository _weekRepository;
    private readonly DeckSubmissionRepository _deckSubmissionRepository;

    public DeckCommands(
        DiscordApiHelperService helperService,
        PlayerService playerService,
        PlayerSeasonTeamRepository playerSeasonTeamRepository,
        TeamRepository teamRepository,
        WeekRepository weekRepository,
        DeckSubmissionRepository deckSubmissionRepository)
    {
        _helperService = helperService;
        _playerService = playerService;
        _playerSeasonTeamRepository = playerSeasonTeamRepository;
        _teamRepository = teamRepository;
        _weekRepository = weekRepository;
        _deckSubmissionRepository = deckSubmissionRepository;
    }

    [SlashCommand("submit", "Submit a .ydk deck file for the currently open week")]
    public async Task SubmitAsync(
        [Summary("player", "The team member whose deck is being submitted")] IUser player,
        [Summary("deck-file", "The .ydk file to submit")] IAttachment deckFile)
    {
        await DeferAsync(ephemeral: false);

        if (!await EnsureInGuildAsync()) return;
        if (!await ValidateDeckAttachmentAsync(deckFile)) return;

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        var (isAdmin, callerCaptainTeam) = await GetCallerCaptainTeamOrFollowupAsync(season, "Only Admins or team captains for the active season can submit decks.");
        if (!isAdmin && callerCaptainTeam is null) return;

        Week? openWeek = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(season.Id, WeekStatus.Open);
        if (openWeek is null)
        {
            await FollowupAsync("There is no open week in the active season.");
            return;
        }
        if (!await EnsureDeckSubmissionsOpenAsync(openWeek)) return;

        var (targetPlayer, targetPst) = await GetTargetPlayerSeasonTeamOrFollowupAsync(player, season);
        if (targetPlayer is null || targetPst is null) return;
        if (!await EnsureTargetPlayerOnCaptainTeamOrFollowupAsync(player, isAdmin, callerCaptainTeam, targetPst.TeamId)) return;

        string deckContent;
        try
        {
            using var http = new HttpClient();
            deckContent = await http.GetStringAsync(deckFile.Url);
        }
        catch (HttpRequestException ex)
        {
            await FollowupAsync($"Failed to download the file: {ex.Message}");
            return;
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Unexpected error while downloading the file: {ex.Message}");
            return;
        }

        if (string.IsNullOrWhiteSpace(deckContent))
        {
            await FollowupAsync("The uploaded `.ydk` file appears to be empty.");
            return;
        }

        // Upsert submission for (player, week).
        var existing = await _deckSubmissionRepository.GetByPlayerAndWeekAsync(targetPlayer.Id, openWeek.Id);
        if (existing != null)
        {
            existing.DeckFile = deckContent;
            existing.SubmittedDate = DateTime.UtcNow;
            existing.IsValidated = false;
            await _deckSubmissionRepository.UpdateAsync(existing);
        }
        else
        {
            await _deckSubmissionRepository.AddAsync(new DeckSubmission
            {
                PlayerId = targetPlayer.Id,
                WeekId = openWeek.Id,
                DeckFile = deckContent,
                SubmittedDate = DateTime.UtcNow,
                IsValidated = false
            });
        }

        await FollowupAsync($"Deck submitted for {player.Mention} for week {openWeek.WeekNumber} (season {season.SeasonNumber}).");
    }

    [SlashCommand("delete", "Delete a player's deck submission for the currently open week")]
    public async Task DeleteDeckSubmissionAsync(
        [Summary("player", "The team member whose deck submission should be deleted")] IUser player)
    {
        await DeferAsync(ephemeral: false);

        if (!await EnsureInGuildAsync()) return;

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        var (isAdmin, callerCaptainTeam) = await GetCallerCaptainTeamOrFollowupAsync(season, "Only Admins or team captains for the active season can delete deck submissions.");
        if (!isAdmin && callerCaptainTeam is null) return;

        Week? openWeek = await _weekRepository.GetSingleWeekBySeasonAndStatusOrDefaultAsync(season.Id, WeekStatus.Open);
        if (openWeek is null)
        {
            await FollowupAsync("There is no open week in the active season.");
            return;
        }

        var (targetPlayer, targetPst) = await GetTargetPlayerSeasonTeamOrFollowupAsync(player, season);
        if (targetPlayer is null || targetPst is null) return;
        if (!await EnsureTargetPlayerOnCaptainTeamOrFollowupAsync(player, isAdmin, callerCaptainTeam, targetPst.TeamId)) return;

        bool deleted = await _deckSubmissionRepository.DeleteByPlayerAndWeekAsync(targetPlayer.Id, openWeek.Id);
        if (!deleted)
        {
            await FollowupAsync($"No deck submission found for {player.Mention} for week {openWeek.WeekNumber} (season {season.SeasonNumber}).");
            return;
        }

        await FollowupAsync($"Deleted deck submission for {player.Mention} for week {openWeek.WeekNumber} (season {season.SeasonNumber}).");
    }

    private async Task<bool> EnsureInGuildAsync()
    {
        if (Context.Guild != null) return true;
        await FollowupAsync("This command can only be used inside a guild.");
        return false;
    }

    private async Task<bool> ValidateDeckAttachmentAsync(IAttachment? deckFile)
    {
        if (deckFile is null)
        {
            await FollowupAsync("No file provided. Please attach a `.ydk` file.");
            return false;
        }

        if (!deckFile.Filename.EndsWith(".ydk", StringComparison.OrdinalIgnoreCase))
        {
            await FollowupAsync("Please upload a file with a `.ydk` extension.");
            return false;
        }

        if (deckFile.Size > MaxDeckFileBytes)
        {
            await FollowupAsync($"That file is too large. Max size is {MaxDeckFileBytes / 1_000_000.0:0.#}MB.");
            return false;
        }

        return true;
    }

    private async Task<(bool isAdmin, Team? callerCaptainTeam)> GetCallerCaptainTeamOrFollowupAsync(Season season, string nonAdminErrorMessage)
    {
        // Permissions: Admins can act for any team; otherwise require caller to be a captain in the active season.
        bool isAdmin = _helperService.IsUserAdmin(Context);
        if (isAdmin)
        {
            return (true, null);
        }

        Player callerPlayer = await _playerService.EnsurePlayerExistsAsync(Context.User);

        var callerCaptainTeam = await _teamRepository.GetByCaptainAndSeasonAsync(callerPlayer.Id, season.Id);
        if (callerCaptainTeam is null)
        {
            await FollowupAsync(nonAdminErrorMessage);
            return (false, null);
        }

        return (false, callerCaptainTeam);
    }

    

    private async Task<bool> EnsureDeckSubmissionsOpenAsync(Week openWeek)
    {
        if (openWeek.Status != WeekStatus.Open)
        {
            await FollowupAsync("Deck submissions are not open for the current week.");
            return false;
        }

        var now = DateTime.UtcNow;
        if (openWeek.SubmissionsClosedDate.HasValue && openWeek.SubmissionsClosedDate.Value <= now)
        {
            await FollowupAsync("Deck submissions are closed for the current week.");
            return false;
        }

        return true;
    }

    private async Task<(Player? player, PlayerSeasonTeam? pst)> GetTargetPlayerSeasonTeamOrFollowupAsync(IUser player, Season season)
    {
        // Ensure target player exists and is on a team for the active season.
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(player);
        var targetPst = await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(targetPlayer.Id, season.Id);
        if (targetPst is null)
        {
            await FollowupAsync($"{player.Mention} is not on any team for the active season.");
            return (null, null);
        }

        return (targetPlayer, targetPst);
    }

    private async Task<bool> EnsureTargetPlayerOnCaptainTeamOrFollowupAsync(IUser targetUser, bool isAdmin, Team? callerCaptainTeam, int targetTeamId)
    {
        if (isAdmin) return true;
        if (callerCaptainTeam is null) return false;

        if (targetTeamId != callerCaptainTeam.Id)
        {
            await FollowupAsync($"{targetUser.Mention} is not on your team for the active season.");
            return false;
        }

        return true;
    }
}

