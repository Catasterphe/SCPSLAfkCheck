using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp939;
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
                if (player == null) return;
                AfkPlayer.Get(player.ReferenceHub).GetAFKComponent(out AFKComponent afkComponent);

                if (afkComponent == null) return;
                afkComponent.afkTime = 0;

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
                ev.Player.GameObject.AddComponent<AFKComponent>();
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
            AfkPlayer afkPlayer = AfkPlayer.Get(ev.Player.ReferenceHub);
            afkPlayer.GetAFKComponent(out AFKComponent AFKCheck);
            AFKCheck.lastPosition = ev.Player.Position;
            AFKCheck.lastRotation = ev.Player.Rotation;
            AFKCheck.lastCameraRotation = ev.Player.Camera.rotation;
            if (afkPlayer.playerIs079)
            {
                AFKCheck.last079Camera = (ev.Player.RoleBase as Scp079Role).CurrentCamera;
            }
            else if (afkPlayer.RoleBase is Scp939Role)
            {
                (ev.Player.RoleBase as Scp939Role).SubroutineModule.TryGetSubroutine<Scp939FocusAbility>(out AFKCheck.scp939Focus);
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
