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
    public class AFKChecker : MonoBehaviour
    {
        private readonly Plugin Plugin = Plugin.Singleton;
        private Player checkingPlayer = null;
        private PlayerRoleBase roleBase = null;
        // 939
        private Scp939FocusAbility scp939Focus;
        // 079
        private bool replacescp079;

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
            bool hasMoved;

            if (roleBase is Scp079Role)
            {
                Scp079Role scp079Base = checkingPlayer.RoleBase as Scp079Role;
                hasMoved = scp079Base.CameraPosition != lastPosition ||
                           scp079Base.CurrentCamera != last079Camera ||
                           scp079Base.RollRotation != last079Roll;

                hasMoved = hasMoved ? UpdateLastPositions(scp079Base) : false;
            }
            else
            {
                hasMoved = checkingPlayer.Position != lastPosition ||
                           checkingPlayer.Rotation != lastRotation ||
                           checkingPlayer.Camera.rotation != lastCameraRotation;

                hasMoved = hasMoved ? UpdateLastPositions() : false;
            }

            return hasMoved;
        }

        // update positions
        private bool UpdateLastPositions()
        {
            lastPosition = checkingPlayer.Position;
            lastRotation = checkingPlayer.Rotation;
            lastCameraRotation = checkingPlayer.Camera.rotation;
            return true;
        }
        private bool UpdateLastPositions(Scp079Role scp079Base)
        {
            lastPosition = scp079Base.CameraPosition;
            last079Camera = scp079Base.CurrentCamera;
            last079Roll = scp079Base.RollRotation;
            return true;
        }
        // end update positions
        private void Awake()
        {
            checkingPlayer = Player.Get(gameObject);
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
            if (!Round.IsRoundStarted || checkingPlayer.Role == RoleTypeId.Tutorial || Round.IsRoundEnded) return;
            if (roleBase == null || roleBase != checkingPlayer.RoleBase)
            {
                roleBase = checkingPlayer.RoleBase;
            }
            switch (roleBase.RoleTypeId)
            {
                case RoleTypeId.Tutorial:
                case RoleTypeId.Spectator:
                case RoleTypeId.None:
                case RoleTypeId.Overwatch:
                    // These are all the classes that should NOT be kicked for AFK ever
                    // since spectators+overwatch are not alive, and Tutorial is usually an admin sit.
                    return;
                case RoleTypeId.Scp096:
                    if (checkMovement() || (roleBase as Scp096Role).IsAbilityState(Scp096AbilityState.TryingNotToCry))
                    {
                        afkTime = 0;
                        return;
                    }
                    break;
                case RoleTypeId.Scp939:
                    (roleBase as Scp939Role).SubroutineModule.TryGetSubroutine<Scp939FocusAbility>(out Scp939FocusAbility scp939Focus);
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
            if (afkTime < Plugin.Config.AfkWarningTime) return;
            if (afkTime == Plugin.Config.AfkWarningTime) {
                string AFKWarning = Plugin.Config.AfkWarningMsg.Replace("%x%", Convert.ToString(Plugin.Config.TimeBeforeAction - afkTime));
                checkingPlayer.SendBroadcast(AFKWarning, 10, shouldClearPrevious:true);
            }
            if (afkTime > Plugin.Config.TimeBeforeAction)
            {
                // Replace the player, check if they should be kicked
                afkTime = 0;
                afkCount++;
                if (checkingPlayer.Team == Team.Dead) return;
                if (Plugin.Config.ReplaceAfks)
                {
                    var inventory = checkingPlayer.Items.Select(item => item.ItemTypeId).ToList();
                    roleBase = checkingPlayer.RoleBase;
                    Vector3 replacementPos = checkingPlayer.Position;
                    float health = checkingPlayer.Health;
                    float ahp = checkingPlayer.ArtificialHealth;
                    if (roleBase is Scp079Role)
                    {
                        replacescp079 = true;
                        Scp079TierManager tierManager;
                        Scp079AuxManager auxManager;
                        (roleBase as Scp079Role).SubroutineModule.TryGetSubroutine<Scp079TierManager>(out tierManager);
                        (roleBase as Scp079Role).SubroutineModule.TryGetSubroutine<Scp079AuxManager>(out auxManager);

                        totalExp = tierManager.TotalExp;
                        currentEnergy = auxManager.CurrentAux;
                    }

                    Dictionary<ItemType, ushort> currentAmmo = checkingPlayer.AmmoBag;

                    playerToReplace = Player.GetPlayers().FirstOrDefault(plr => plr.Role == RoleTypeId.Spectator && !plr.IsOverwatchEnabled && plr != checkingPlayer);
                    if (playerToReplace != null) {
                        checkingPlayer.ClearInventory();
                        checkingPlayer.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin);
                        checkingPlayer.SendBroadcast(Plugin.Config.AfkReplaceMsg, 30, shouldClearPrevious:true);

                        playerToReplace.ReferenceHub.roleManager.ServerSetRole(roleBase.RoleTypeId, RoleChangeReason.RemoteAdmin);

                        foreach (ItemType item in inventory) {
                            playerToReplace.AddItem(item);
                        }
                        playerToReplace.Health = health;
                        playerToReplace.ArtificialHealth = ahp;

                        foreach (var ammoKeyPair in currentAmmo)
                        {
                            ItemType itemAmmo = ammoKeyPair.Key;
                            ushort value = ammoKeyPair.Value;

                            playerToReplace.AddAmmo(itemAmmo, value);
                        }

                        if (replacescp079)
                        {
                            Scp079TierManager tierManager;
                            Scp079AuxManager auxManager;
                            (playerToReplace.RoleBase as Scp079Role).SubroutineModule.TryGetSubroutine<Scp079TierManager>(out tierManager);
                            (playerToReplace.RoleBase as Scp079Role).SubroutineModule.TryGetSubroutine<Scp079AuxManager>(out auxManager);

                            tierManager.ServerGrantExperience(totalExp, Scp079HudTranslation.ExpGainAdminCommand);
                            auxManager.CurrentAux = currentEnergy;
                        }
                        playerToReplace.SendBroadcast(Plugin.Config.ReplaceMsg, 10);

                    } else
                    {
                        // could not find a player to replace :(
                        checkingPlayer.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin);
                        checkingPlayer.SendBroadcast(Plugin.Config.AFKSpectatorMsg, 10);
                    }
                } else
                {
                    checkingPlayer.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.RemoteAdmin);
                    checkingPlayer.SendBroadcast(Plugin.Config.AFKSpectatorMsg, 10);
                }
                if (afkCount >= Plugin.Config.AllowedReplacements)
                {
                    checkingPlayer.Kick(Plugin.Config.AfkKickMsg);
                }
            }
        }
    }
}
