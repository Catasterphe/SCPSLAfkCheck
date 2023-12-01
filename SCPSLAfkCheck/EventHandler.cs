using Achievements;
using CommandSystem;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using System;

namespace SCPSLAfkCheck
{
    public class EventHandler
    {
        public void ResetAFKTime(Player player)
        {
            try
            {
                AFKChecker AFKCheck = player?.GameObject.GetComponent<AFKChecker>();

                if (AFKCheck == null) return;
                AFKCheck.afkTime = 0;

            }
            catch (Exception e)
            {
                Log.Error($"Error resetting afk time: {e}");
            }
        }

        [PluginEvent]
        void PlayerJoin(PlayerJoinedEvent ev)
        {
            try
            {
                ICommandSender test = ev.Player as ICommandSender;
                BanPlayer.GlobalBanUser(ev.Player.ReferenceHub, test);
                ev.Player.GameObject.AddComponent<AFKChecker>();
            }
            catch(Exception e)
            {
                Log.Error($"Error giving component: {e}");
            }
        }

        [PluginEvent]
        void RoleChangeEvent(PlayerChangeRoleEvent ev)
        {
            if (ev.Player == null) return;
            if (ev.NewRole == RoleTypeId.Spectator || ev.NewRole == RoleTypeId.Tutorial) return;
            AFKChecker AFKCheck = ev.Player.GameObject.GetComponent<AFKChecker>();
            AFKCheck.lastPosition = ev.Player.Position;
            AFKCheck.lastRotation = ev.Player.Rotation;
            AFKCheck.lastCameraRotation = ev.Player.Camera.rotation;
            if (ev.Player.RoleBase is Scp079Role)
            {
                AFKCheck.last079Camera = (ev.Player.RoleBase as Scp079Role).CurrentCamera;
            }
        }

        // Events to reset AFK time
        [PluginEvent]
        void PlayerAim(PlayerAimWeaponEvent ev)
        {
            ResetAFKTime(ev.Player);
        }

        [PluginEvent]
        void PlayerUseItem(PlayerUseItemEvent ev)
        {
            ResetAFKTime(ev.Player);
        }

        [PluginEvent]
        void PlayerInteractDoor(PlayerInteractDoorEvent ev)
        {
            ResetAFKTime(ev.Player);
        }

        [PluginEvent]
        void PlayerInteractLocker(PlayerInteractLockerEvent ev)
        {
            ResetAFKTime(ev.Player);
        }
        [PluginEvent]
        void SCP079GainEXP(Scp079GainExperienceEvent ev)
        {
            ResetAFKTime(ev.Player);
        }
        [PluginEvent]
        void SCP079Blackout(Scp079BlackoutRoomEvent ev)
        {
            ResetAFKTime(ev.Player);
        }
    }
}
