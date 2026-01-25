using Discord;
using Discord.Interactions;
using WarLeague.Discord.Roles;


namespace WarLeague.Discord.Commands;

[Group("example", "Example commands")]
public class ExampleCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordRoleMapper _roleMapper;
    public ExampleCommands(DiscordRoleMapper roleMapper)
    {
        _roleMapper = roleMapper;
    }
    [SlashCommand("test", "Test command")]
    public async Task TestCommand()
    {
        await RespondAsync("Test!");
    }

    [SlashCommand("whoami", "Show the application role mapped from your Discord roles")]
    [RequireRole("Admin")]
    public async Task WhoAmI()
    {
        // This command must run in a guild so we can read the user's guild roles
        if (Context.Guild == null)
        {
            await RespondAsync("This command must be used in a server (guild).", ephemeral: true);
            return;
        }

        // Get the guild user (should be available on the interaction context)
        var guildUser = Context.User as IGuildUser;
        if (guildUser == null)
        {
            await RespondAsync("Could not resolve guild user.", ephemeral: true);
            return;
        }

        // Map to your application Role enum using the mapper
        var appRole = _roleMapper.MapToApplicationRole(guildUser);

        // Optionally show which Discord role names the user currently has
        var roleNames = guildUser.RoleIds
                                 .Select(id => Context.Guild.GetRole(id)?.Name ?? id.ToString())
                                 .Where(n => !string.IsNullOrEmpty(n))
                                 .ToArray();

        var rolesText = roleNames.Length > 0 ? string.Join(", ", roleNames) : "No roles";

        await RespondAsync($"Mapped application role: `{appRole}`\nDiscord roles: {rolesText}", ephemeral: true);
    }

    [SlashCommand("longtask", "Example of long running task (3 seconds)")]
    public async Task LongTask()
    {
        await DeferAsync(ephemeral: true);

        await Task.Delay(3000); // Simulate long task

        await FollowupAsync("Congratulations you have waited for 3 seconds");
    }
}