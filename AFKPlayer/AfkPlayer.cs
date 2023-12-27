using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PluginAPI.Core;
using PluginAPI.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SCPSLAfkCheck
{
    public class AfkPlayer : Player
    {
        public AfkPlayer(IGameComponent component) : base(component)
        {
        }

        public void GetAFKComponent(out AFKComponent afkCheckComponent)
        {
            try
            {
                GameObject.TryGetComponent<AFKComponent>(out afkCheckComponent);
            } catch(Exception e)
            {
                Log.Error($"Error getting AFK component: {e}");
                afkCheckComponent = null;
            }               
        }
        public bool playerIs079 => RoleBase is Scp079Role || ReferenceHub.GetRoleId() == RoleTypeId.Scp079;
        public AfkData GetReplacementData()
        {
            if (!playerIs079)
            {
                return new AfkData
                {
                    Inventory = this.Items.Select(item => item.ItemTypeId).ToList(),
                    currentAmmo = new Dictionary<ItemType, ushort>(AmmoBag),
                    replacementPosition = this.Position,
                    currentHP = this.Health,
                    currentAH = this.ArtificialHealth,
                    currentRole = this.Role,
                    Scp079Data = null
                };
            }

            var scp079Role = (Scp079Role)RoleBase;
            scp079Role.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out Scp079TierManager tierManager);
            scp079Role.SubroutineModule.TryGetSubroutine<Scp079AuxManager>(out Scp079AuxManager auxManager);

            return new AfkData
            {
                Inventory = this.Items.Select(item => item.ItemTypeId).ToList(),
                currentAmmo = new Dictionary<ItemType, ushort>(AmmoBag),
                replacementPosition = this.Position,
                currentHP = this.Health,
                currentAH = this.ArtificialHealth,
                currentRole = this.Role,
                Scp079Data = new Scp079Data
                {
                    totalExp = tierManager.TotalExp,
                    currentAux = auxManager.CurrentAux
                }
            };
        }

        public bool SetReplacementData(AfkData dataToGive)
        {
            try
            {
                Timing.CallDelayed(0.2f, () =>
                {
                    foreach (ItemType item in dataToGive.Inventory)
                    {
                        AddItem(item);
                    }

                    foreach (var ammo in dataToGive.currentAmmo)
                    {
                        AddAmmo(ammo.Key, ammo.Value);
                    }

                    foreach (Firearm firearm in Items.Where(i => i is Firearm))
                    {
                        if (AttachmentsServerHandler.PlayerPreferences.TryGetValue(ReferenceHub, out var value))
                            if (value.TryGetValue(firearm.ItemTypeId, out var value2))
                            firearm.ApplyAttachmentsCode(value2, reValidate: true);

                        var firearmStatusFlags = FirearmStatusFlags.MagazineInserted;

                        if (firearm.HasAdvantageFlag(AttachmentDescriptiveAdvantages.Flashlight))
                            firearmStatusFlags |= FirearmStatusFlags.FlashlightEnabled;

                        firearm.Status = new FirearmStatus(firearm.AmmoManagerModule.MaxAmmo, firearmStatusFlags, firearm.GetCurrentAttachmentsCode());
                    }

                    this.Health = dataToGive.currentHP;
                    this.ArtificialHealth = dataToGive.currentAH;
                    this.Position = dataToGive.replacementPosition;

                    if (playerIs079) 
                    {
                        Scp079Role scp079role = this.RoleBase as Scp079Role;
                        scp079role.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out Scp079TierManager tierManager);
                        scp079role.SubroutineModule.TryGetSubroutine<Scp079AuxManager>(out Scp079AuxManager auxManager);
                        
                        tierManager.TotalExp = dataToGive.Scp079Data.totalExp;
                        auxManager.CurrentAux = dataToGive.Scp079Data.currentAux;
                    }
                });

                return true;

            } catch(Exception e)
            {
                Log.Info($"Error giving Replacement Data: {e}");
                return false;
            }
        }

        public static AfkPlayer Get(int PlayerId)
        {
            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.PlayerId == PlayerId)
                    return Get<AfkPlayer>(hub);
            }

            return null;
        }

        public static AfkPlayer Get(ReferenceHub hub)
        {
            return Get<AfkPlayer>(hub);
        }

        public static AfkPlayer Get(GameObject gameObject)
        {
            return Get<AfkPlayer>(gameObject);
        }
    }
}
