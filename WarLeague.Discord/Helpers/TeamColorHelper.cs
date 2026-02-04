using Discord;
using WarLeague.Discord.Enums;

namespace WarLeague.Discord.Helpers;

public static class TeamColorHelper
{
    public static Color ToDiscordColor(this TeamColor teamColor)
    {
        return teamColor switch
        {
            TeamColor.Red => new Color(220, 20, 60),
            TeamColor.Blue => new Color(65, 105, 225),
            TeamColor.Green => new Color(34, 139, 34),
            TeamColor.Yellow => new Color(255, 215, 0),
            TeamColor.Orange => new Color(255, 140, 0),
            TeamColor.Purple => new Color(138, 43, 226),
            TeamColor.Pink => new Color(255, 105, 180),
            TeamColor.Cyan => new Color(0, 206, 209),
            TeamColor.Magenta => new Color(199, 21, 133),
            TeamColor.Lime => new Color(50, 205, 50),
            TeamColor.Navy => new Color(0, 0, 128),
            TeamColor.Teal => new Color(0, 128, 128),
            TeamColor.Gold => new Color(255, 215, 0),
            TeamColor.Silver => new Color(192, 192, 192),
            TeamColor.Maroon => new Color(128, 0, 0),
            TeamColor.Olive => new Color(128, 128, 0),
            TeamColor.Aqua => new Color(127, 255, 212),
            TeamColor.Crimson => new Color(220, 20, 60),
            TeamColor.Indigo => new Color(75, 0, 130),
            TeamColor.White => new Color(255, 255, 255),
            _ => new Color(128, 128, 128)
        };
    }
}
