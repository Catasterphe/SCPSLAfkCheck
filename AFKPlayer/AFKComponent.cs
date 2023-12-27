using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp939;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SCPSLAfkCheck
{
    public class AFKComponent : MonoBehaviour
    {
        private readonly Plugin Plugin = Plugin.Singleton;
        private AfkPlayer checkingPlayer = null;
        private PlayerRoleBase roleBase = null;
        // 939
        public Scp939FocusAbility scp939Focus;

        public Player playerToReplace;
        public Vector3 lastPosition, lastRotation;
        public Quaternion lastCameraRotation;
        public int afkTime = 0;
        public int afkCount = 0;
        // 079
        public Scp079Camera last079Camera;
        public float last079Roll;
        public int totalExp;
        public float currentEnergy;

        private bool checkMovement()
        {
            if (checkingPlayer.playerIs079)
            {
                Scp079Role scp079Base = (Scp079Role)checkingPlayer.RoleBase;
                return scp079Base.CameraPosition != lastPosition ||
                       scp079Base.CurrentCamera != last079Camera ||
                       scp079Base.RollRotation != last079Roll && UpdateLastPositions();
            }
            else
            {
                return checkingPlayer.Position != lastPosition ||
                       checkingPlayer.Rotation != lastRotation ||
                       checkingPlayer.Camera.rotation != lastCameraRotation && UpdateLastPositions();
            }
        }
        private bool UpdateLastPositions()
        {
            if (roleBase is Scp079Role scp079Base)
            {
                lastPosition = scp079Base.CameraPosition;
                last079Camera = scp079Base.CurrentCamera;
                last079Roll = scp079Base.RollRotation;
            }
            else
            {
                lastPosition = checkingPlayer.Position;
                lastRotation = checkingPlayer.Rotation;
                lastCameraRotation = checkingPlayer.Camera.rotation;
            }
            return true;
        }
        private void Awake()
        {
            checkingPlayer = AfkPlayer.Get(gameObject);
        }
        private void Start()
        {
            if (checkingPlayer == null) return;
            CoroutineHandle AFKHandler = Timing.RunCoroutine(AFKCoroutine().CancelWith(gameObject).CancelWith(this));
        }
        private IEnumerator<float> AFKCoroutine()
        {
            while (true)
            {
                try
                {
                    CheckIfAFK();
                }
                catch (Exception e)
                {
                    Log.Error($"Error {e}");
                }

                yield return Timing.WaitForSeconds(1);
            }
        }

        private void CheckIfAFK()
        {
            if (!Round.IsRoundStarted || checkingPlayer?.Role == RoleTypeId.Tutorial) return;
            if (roleBase == null || roleBase != checkingPlayer.RoleBase)
            {
                roleBase = checkingPlayer.RoleBase;
            }   
            switch (roleBase.RoleTypeId)
            {
                case RoleTypeId.Tutorial:
                case RoleTypeId.Spectator:
                case RoleTypeId.None: // what counts as `None`??
                case RoleTypeId.Overwatch:
                    // These are all the classes that should NOT be kicked for AFK ever
                    // since spectators+overwatch are not alive, and Tutorial is usually an admin sit.
                    return;
                case RoleTypeId.Scp096:
                    if (checkMovement() || ((Scp096Role)roleBase).IsAbilityState(Scp096AbilityState.TryingNotToCry))
                    {
                        afkTime = 0;
                        return;
                    }
                    break;
                case RoleTypeId.Scp939:
                    if (checkMovement() || scp939Focus.TargetState == true)
                    {
                        afkTime = 0;
                        return;
                    }
                    break;
                default:
                    if (checkMovement())
                    {
                        afkTime = 0;
                        return;
                    }
                    break;
            }

            afkTime++;
            if (afkTime < Plugin.Config.AfkWarningTime)
            {
                return;
            }
            else
            {
                if (afkTime == Plugin.Config.AfkWarningTime)
                {
                    string AFKWarning = Plugin.Config.AfkWarningMsg.Replace("%x%", Convert.ToString(Plugin.Config.TimeBeforeAction - afkTime));
                    checkingPlayer.SendBroadcast(AFKWarning, 10, shouldClearPrevious: true);
                }
                else if (afkTime > Plugin.Config.TimeBeforeAction)
                {
                    afkTime = 0;
                    afkCount++;
                    if (checkingPlayer.Team == Team.Dead) return;
                    if (Plugin.Config.ReplaceAfks)
                    {
                        AfkData replacementData = checkingPlayer.GetReplacementData();

                        playerToReplace = Player.GetPlayers().FirstOrDefault(plr => plr.Role == RoleTypeId.Spectator && !plr.IsOverwatchEnabled && plr != checkingPlayer);
                        // Despawn current player
                        // playerToReplace = checkingPlayer;
                        if (playerToReplace != null)
                        {
                            checkingPlayer.ClearInventory();
                            checkingPlayer.SendBroadcast(Plugin.Config.AfkReplaceMsg, 30, shouldClearPrevious: true);
                            checkingPlayer.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin);
                            // Spawn the Player that Replaces
                            playerToReplace.ReferenceHub.roleManager.ServerSetRole(replacementData.currentRole, RoleChangeReason.RemoteAdmin);
                            playerToReplace.ClearInventory();
                            AfkPlayer.Get(playerToReplace.ReferenceHub).SetReplacementData(replacementData);
                            playerToReplace.SendBroadcast(Plugin.Config.ReplaceMsg, 10);
                        }
                        else
                        {
                            // could not find a player to replace :(
                            checkingPlayer.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin);
                            checkingPlayer.SendBroadcast(Plugin.Config.AFKSpectatorMsg, 10);
                        }
                    }
                    else
                    {
                        checkingPlayer.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin);
                        checkingPlayer.SendBroadcast(Plugin.Config.AFKSpectatorMsg, 10);
                    }

                    // kick them if theyre past the allowed count
                    if (afkCount >= Plugin.Config.AllowedReplacements)
                    {
                        checkingPlayer.Kick(Plugin.Config.AfkKickMsg);
                    }
                }
            }
        }
    }
}
