using PlayerRoles;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SCPSLAfkCheck
{
    public class Scp079Data
    {
        public int totalExp { get; set; }
        public float currentAux { get; set; }
    }
    public class AfkData
    {
        public List<ItemType> Inventory { get; set; }
        public Dictionary<ItemType, ushort> currentAmmo { get; set; }
        public RoleTypeId currentRole { get; set; }
        public Vector3 replacementPosition { get; set; }
        public float currentHP { get; set; }
        public float currentAH { get; set; }
        public Scp079Data Scp079Data { get; set; }
    }
}
