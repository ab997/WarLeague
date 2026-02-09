using Discord.Interactions;
using Discord;
using WarLeague.Discord.Preconditions;
using WarLeague.Data.Data.Enums;


namespace WarLeague.Discord.Commands
{
    [Group("info", "Information commands")]
    [RequireAppPermission(PermissionType.Admin)]
    public class InfoCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("admin-help", "Administrator operational guide")]
        public async Task AdminHelpAsync()
        {
            await DeferAsync(ephemeral: false);

            var eb = new EmbedBuilder()
                .WithTitle("Administrator Operational Guide")
                .WithColor(new Color(88, 101, 242));

            eb.AddField("Scope",
                "- Admin-only actions\n- Multiple formats supported\n- Actions executed inside a format category are implicitly scoped to that format",
                inline: false);

            eb.AddField("Formats",
                "- Create a format first\n- Creating a format automatically provisions a category named after the format and an initial channel inside it\n- Perform all actions (seasons, weeks, matches) inside the format’s category; actions are scoped to that format" +
                "\n- Alternatively, use a special command to designate a single format per server. Every command will be assume to reference only that format.",
                inline: false);

            eb.AddField("Seasons",
                "- Seasons are created manually and identified by a number\n- A new season starts inactive\n- An admin must explicitly activate a season\n- Only one season per format can be active at a time",
                inline: false);

            eb.AddField("Weeks",
                "- Weeks are created manually inside a season\n- A season may contain multiple weeks\n- Exactly one active week per season is allowed at any time\n- Active = any status except NotOpenYet\n- Multiple Completed weeks may exist; only one working week (Open, SubmissionsClosed, or InProgress) is allowed\n- Application validations enforce this rule",
                inline: false);

            eb.AddField(
                "Week Statuses and Allowed Actions",
                @"- NotOpenYet
  • Week exists; no actions are allowed yet

- Open
  • Teams may submit decks
  • Players and captains can prepare lineups; deck submissions are permitted

- SubmissionsClosed
  • All teams have submitted
  • Deck changes are no longer allowed

- InProgress
  • Pairings have been generated
  • Players must report match results
  • Reporting is only allowed during InProgress
  • After pairings are generated, each player may report results only for their own single open match
  • Admins may report on behalf of any player with an open match
  • Players report their own losses

- Completed
  • Week is finished",
                inline: false);

            eb.AddField("Recommended Lifecycle Example",
                "1) Create format\n2) Create season\n3) Activate season\n4) Create weeks\n5) Open a week (enable deck submissions)\n6) Close submissions (lock decks)\n7) Generate pairings (move week to InProgress)\n8) Players report matches (only their own, only losses)\n9) Complete the week\n10) Repeat for the next week",
                inline: false);

            await FollowupAsync(embeds: new[] { eb.Build() });
        }
    }
}
