using Discord;
using Discord.Interactions;
using WarLeague.Core.Data.Entities;
using WarLeague.Core.Data.Enums;
using WarLeague.Core.Domain.Model;
using WarLeague.Core.Domain.Services;
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
    private readonly DeckSubmissionService _deckSubmissionService;

    public DeckCommands(
        DiscordApiHelperService helperService,
        PlayerService playerService,
        PlayerSeasonTeamRepository playerSeasonTeamRepository,
        TeamRepository teamRepository,
        WeekRepository weekRepository,
        DeckSubmissionRepository deckSubmissionRepository,
        DeckSubmissionService deckSubmissionService)
    {
        _helperService = helperService;
        _playerService = playerService;
        _playerSeasonTeamRepository = playerSeasonTeamRepository;
        _teamRepository = teamRepository;
        _weekRepository = weekRepository;
        _deckSubmissionRepository = deckSubmissionRepository;
        _deckSubmissionService = deckSubmissionService;
    }

    [SlashCommand("submit", "Submit a .ydk deck file for the currently open week")]
    public async Task SubmitAsync(
        [Summary("player", "The team member whose deck is being submitted")] IUser player,
        [Summary("deck-file", "The .ydk file to submit")] IAttachment deckFile)
    {
        await DeferAsync(ephemeral: false);

        if (!await ValidateDeckAttachmentAsync(deckFile)) return;

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Player callerPlayer = await _playerService.EnsurePlayerExistsAsync(Context.User);

        bool isAdmin = _helperService.IsUserAdmin(Context);

        var callerCaptainTeam = await _teamRepository.GetByCaptainAndSeasonAsync(callerPlayer.Id, season.Id);
        if (!isAdmin && callerCaptainTeam is null)
        {
            await FollowupAsync("Only Admins or team captains for the active season can submit or modify decks.");
            return;
        }

        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(player);

        var pst = await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(targetPlayer.Id, season.Id);
        if (pst is null)
        {
            await FollowupAsync("Player is not on any team for the active season.");
            return;
        }

        int targetTeamId = pst.TeamId;

        if (!isAdmin && targetTeamId != callerCaptainTeam!.Id)
        {
            await FollowupAsync($"{player.Mention} is not on your team for the active season.");
            return;
        }

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

        Result result = await _deckSubmissionService.SubmitAsync(season.Id, targetPlayer.Id, deckContent);

        await FollowupAsync(result.Message);
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

