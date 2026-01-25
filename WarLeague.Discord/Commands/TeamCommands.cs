using Discord;
using Discord.Interactions;

namespace WarLeague.Discord.Commands;

[Group("team", "Team management commands")]
[RequireRole("Admin", Group = "Permission")]
[RequireRole("Captain", Group = "Permission")]
public class TeamCommands : InteractionModuleBase<SocketInteractionContext>
{

    public TeamCommands()
    {
    }


    [SlashCommand("create", "Creates team with you as captain")]
    public async Task CreateAsync(
        [Summary("team-name", "Name of the team")] string teamName)
    {
        await DeferAsync(ephemeral: false);
        // Implementation goes here
        await FollowupAsync($"Team '{teamName}' created with you as captain.");
    }

}
