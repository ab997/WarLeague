using Discord;
using Discord.Interactions;
using WarLeague.Data.Entities;
using WarLeague.Core.Model;
using WarLeague.Core.Repositories;
using WarLeague.Core.Services;
using WarLeague.Discord.Preconditions;
using WarLeague.Discord.Services;
using static WarLeague.Discord.Helpers.ResultHelper;
using WarLeague.Data.Data.Enums;

namespace WarLeague.Discord.Commands;

[Group("deck", "Deck submission commands")]
[EnsureChannelIsInFormatCategory]
[EnsureSingleActiveSeason]
[EnsureSingleValidOpenWeek]
[RequireAppPermission(PermissionType.Admin, Group = "Permission")]
[RequireAppPermission(PermissionType.Captain, Group = "Permission")]
[InitializeGuildContext]
public class DeckCommands : InteractionModuleBase<SocketInteractionContext>
{
    private const int MaxDeckFileBytes = 1_000_000; // 1MB safety limit

    private readonly DiscordApiHelperService _helperService;
    private readonly DiscordPlayerService _playerService;
    private readonly PlayerSeasonTeamRepository _playerSeasonTeamRepository;
    private readonly TeamRepository _teamRepository;
    private readonly DeckSubmissionService _deckSubmissionService;
    private readonly HttpClient _httpClient;

    public DeckCommands(
        DiscordApiHelperService helperService,
        DiscordPlayerService playerService,
        PlayerSeasonTeamRepository playerSeasonTeamRepository,
        TeamRepository teamRepository,
        DeckSubmissionService deckSubmissionService,
        HttpClient httpClient)
    {
        _helperService = helperService;
        _playerService = playerService;
        _playerSeasonTeamRepository = playerSeasonTeamRepository;
        _teamRepository = teamRepository;
        _deckSubmissionService = deckSubmissionService;
        _httpClient = httpClient;
    }

    [SlashCommand("submit", "Submit a .ydk file")]
    public async Task SubmitAsync(
        [Summary("player", "The team member whose deck is being submitted")] IUser player,
        [Summary("deck-file", "The .ydk file to submit")] IAttachment deckFile,
        [Summary("seat-number", "The seat number for this deck submission")] int seatNumber)
    {
        await DeferAsync(ephemeral: false);

        string? attachmentError = ValidateDeckAttachment(deckFile);
        if (attachmentError is not null)
        {
            await FollowupAsync(attachmentError);
            return;
        }

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Player callerPlayer = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(player);

        string? permissionError = await ValidateDeckSubmissionPermission(season, callerPlayer, targetPlayer);
        if (permissionError is not null)
        {
            await FollowupAsync(permissionError);
            return;
        }

        string deckContent;
        try
        {
            deckContent = await _httpClient.GetStringAsync(deckFile.Url);
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

        BaseResult result = await _deckSubmissionService.SubmitAsync(season.Id, targetPlayer.Id, deckContent, seatNumber);

        await FollowupAsync(Stringify(result));
    }

    [SlashCommand("delete", "Delete a player's deck submission")]
    public async Task DeleteDeckSubmissionAsync(
        [Summary("player", "The team member whose deck submission should be deleted")] IUser player)
    {
        await DeferAsync(ephemeral: false);

        Season season = await _helperService.GetSeasonByCategoryNameAsync(Context);
        Player callerPlayer = await _playerService.EnsurePlayerExistsAsync(Context.User);
        Player targetPlayer = await _playerService.EnsurePlayerExistsAsync(player);

        string? permissionError = await ValidateDeckSubmissionPermission(season, callerPlayer, targetPlayer);
        if (permissionError is not null)
        {
            await FollowupAsync(permissionError);
            return;
        }

        BaseResult result = await _deckSubmissionService.DeleteSubmissionAsync(season.Id, targetPlayer.Id);

        await FollowupAsync(Stringify(result));
    }

    private async Task<string?> ValidateDeckSubmissionPermission(Season season, Player callerPlayer, Player targetPlayer)
    {
        bool isAdmin = _helperService.IsUserAdmin(Context);

        // Admins can submit for anyone, let service handle business validation
        if (isAdmin)
        {
            return null;
        }

        // Non-admins must be captains
        var callerCaptainTeam = await _teamRepository.GetByCaptainAndSeasonAsync(callerPlayer.Id, season.Id);
        if (callerCaptainTeam is null)
        {
            return "Only Admins or team captains for the active season can submit or modify decks.";
        }

        // Captains can only submit for players on their team
        var pst = await _playerSeasonTeamRepository.GetByPlayerAndSeasonAsync(targetPlayer.Id, season.Id);
        
        if (pst is null || pst.TeamId != callerCaptainTeam.Id)
        {
            return $"{targetPlayer.UserName} is not on your team for the active season.";
        }

        return null;
    }

    private string? ValidateDeckAttachment(IAttachment? deckFile)
    {
        if (deckFile is null)
        {
            return "No file provided. Please attach a `.ydk` file.";
        }

        if (!deckFile.Filename.EndsWith(".ydk", StringComparison.OrdinalIgnoreCase))
        {
            return "Please upload a file with a `.ydk` extension.";
        }

        if (deckFile.Size > MaxDeckFileBytes)
        {
            return $"That file is too large. Max size is {MaxDeckFileBytes / 1_000_000.0:0.#}MB.";
        }

        return null;
    }
}

