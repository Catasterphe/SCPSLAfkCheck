using System.ComponentModel;

namespace SCPSLAfkCheck
{
    public class Config
    {
        [Description("After not moving for this time, a warning message will appear telling them they are AFK.")]
        public short AfkWarningTime { get; private set; } = 30;
        [Description("How long can player not move?")]
        public short TimeBeforeAction { get; private set; } = 60;
        [Description("How many times can a player be AFK before getting kicked?")]
        public int AllowedReplacements { get; private set; } = 2;
        [Description("Replace AFK players with spectators?")]
        public bool ReplaceAfks { get; private set; } = true;
        [Description("Messages to display to the user - name is explanatory")]

        public string AFKSpectatorMsg { get; private set; } = "You have been set to spectator for being afk.";
        public string AfkWarningMsg { get; private set; } = "You will be replaced in <color=red>%x% seconds</color> if you do not move!";
        public string AfkReplaceMsg { get; private set; } = "You were detected as AFK and automatically replaced.";
        public string AfkKickMsg { get; private set; } = "You were kicked for being AFK.";
        public string ReplaceMsg { get; private set; } = "You have replaced an AFK player.";
    }
}
