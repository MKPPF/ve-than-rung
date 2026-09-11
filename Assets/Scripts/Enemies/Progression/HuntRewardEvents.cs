using System;
using TopDownGame.Equipment;
using UnityEngine;

namespace TopDownGame.Enemies.Progression
{
    public readonly struct HuntRewardNotice
    {
        public HuntRewardNotice(int experience, int gold,
            EquipmentData loot, Vector3 worldPosition, int hunterEssence = 0,
            EquipmentData salvagedLoot = null)
        {
            Experience = experience;
            Gold = gold;
            Loot = loot;
            WorldPosition = worldPosition;
            HunterEssence = hunterEssence;
            SalvagedLoot = salvagedLoot;
        }

        public int Experience { get; }
        public int Gold { get; }
        public EquipmentData Loot { get; }
        public Vector3 WorldPosition { get; }
        public int HunterEssence { get; }
        public EquipmentData SalvagedLoot { get; }
    }

    public static class HuntRewardEvents
    {
        public static event Action<HuntRewardNotice> RewardGranted;
        public static event Action<EnemyRuntimeProfile> EliteSpawned;

        public static void PublishReward(HuntRewardNotice notice)
        {
            RewardGranted?.Invoke(notice);
        }

        public static void PublishElite(EnemyRuntimeProfile profile)
        {
            if (profile != null) EliteSpawned?.Invoke(profile);
        }
    }
}
