using Discord;
using Discord.Interactions;
using System.Numerics;
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
[RequireRole("Admin", Group = "Permission")]
[RequireRole("Captain", Group = "Permission")]
public class DeckCommands : InteractionModuleBase<SocketInteractionContext>
{
    private const int MaxDeckFileBytes = 1_000_000; // 1MB safety limit

    private readonly DiscordApiHelperService _helperService;
    private readonly DiscordPlayerService _playerService;
    private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
    private readonly TeamRepository _teamRepository;
    private readonly DeckSubmissionService _deckSubmissionService;

    public DeckCommands(
        DiscordApiHelperService helperService,
        DiscordPlayerService playerService,
        PlayerSeasonTeamRepository playerSeasonTeamRepository,
        TeamRepository teamRepository,
        DeckSubmissionService deckSubmissionService)
    {
        _helperService = helperService;
        _playerService = playerService;
        _playerSeasonTeamRepository = playerSeasonTeamRepository;
        _teamRepository = teamRepository;
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

        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(player);

        bool flowControl = await ValidateDeckSubmissionRequest(season, callerPlayer, targetPlayer);
        if (!flowControl)
        {
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

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);

        Player callerPlayer = await _playerService.EnsurePlayerExistsAsync(Context.User);

        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(player);

        bool flowControl = await ValidateDeckSubmissionRequest(season, callerPlayer, targetPlayer);
        if (!flowControl)
        {
            return;
        }

        Result result = await _deckSubmissionService.DeleteSubmissionAsync(season.Id, targetPlayer.Id);

        await FollowupAsync(result.Message);
    }

    private async Task<bool> ValidateDeckSubmissionRequest(Season season, Player callerPlayer, Player targetPlayer)
    {
        bool isAdmin = _helperService.IsUserAdmin(Context);

        var callerCaptainTeam = await _teamRepository.GetByCaptainAndSeasonAsync(callerPlayer.Id, season.Id);
        if (!isAdmin && callerCaptainTeam is null)
        {
            await FollowupAsync("Only Admins or team captains for the active season can submit or modify decks.");
            return false;
        }

        var pst = await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(targetPlayer.Id, season.Id);
        if (pst is null)
        {
            await FollowupAsync("Player is not on any team for the active season.");
            return false;
        }

        int targetTeamId = pst.TeamId;

        if (!isAdmin && targetTeamId != callerCaptainTeam!.Id)
        {
            await FollowupAsync($"{targetPlayer.UserName} is not on your team for the active season.");
            return false;
        }

        return true;
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
}

