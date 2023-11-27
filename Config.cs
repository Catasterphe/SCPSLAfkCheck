﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCPSLAfkCheck
{
    public class Config
    {
        [Description("After not moving for this time, a warning message will appear telling them they are AFK.")]
        public short AfkWarningTime = 5;
        [Description("How long can player not move?")]
        public short TimeBeforeAction = 10;
        [Description("How many times can a player be AFK before getting kicked?")]
        public int AllowedReplacements { get; private set; } = 2;
        [Description("Replace AFK players with spectators?")]
        public bool ReplaceAfks { get; private set; } = true;

        public string AfkWarningMsg { get; private set; } = "You will be replaced in <color=red>%x% seconds</color> if you do not move!";
        public string AfkReplaceMsg { get; private set; } = "You were detected as AFK and automatically replaced.";
        public string AfkKickMsg { get; private set; } = "You were kicked for being AFK.";
        public string ReplaceMsg { get; private set; } = "You have replaced an AFK player.";
    }
}