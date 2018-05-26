﻿using CitizenFX.Core;
using CitizenFX.Core.Native;
using CorruptSnail.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CorruptSnail.Spawners
{
    class ZombieSpawner : BaseScript
    {
        private const int ZOMBIE_AMOUNT = 30;
        private const double ZOMBIE_ATTR_CHANCE = 0.5;
        private const int ZOMBIE_MAX_HEALTH = 500;
        private const int ZOMBIE_MAX_ARMOR = 500; 

        private List<Ped> zombies;
        public static RelationshipGroup ZombieGroup { get; private set; }

        public ZombieSpawner()
        {
            zombies = new List<Ped>();
            ZombieGroup = World.AddRelationshipGroup("zombies");

            EventHandlers["corruptsnail:client:newZombie"] += new Action<int>(OnNewZombie);
            Tick += OnTick;
        }

        private async Task OnTick()
        {
            await Delay(SpawnerHost.SPAWN_TICK_RATE);

            if (SpawnerHost.IsHost && zombies.Count < ZOMBIE_AMOUNT)
                SpawnRandomZombie();
            else if (zombies.Count > 0)
                foreach (Ped zombie in zombies.ToArray())
                    if (!Utils.IsPosShitSpawn(Players, zombie.Position, SpawnerHost.SPAWN_DESPAWN_DISTANCE)
                        || zombie.IsDead)
                    {
                        zombie.MarkAsNoLongerNeeded();
                        zombies.Remove(zombie);
                    }
        }

        private async void SpawnRandomZombie()
        {
            Vector3 spawnPos = Utils.GetRandomSpawnPosFromPlayer(Game.Player, SpawnerHost.SPAWN_MIN_DISTANCE, SpawnerHost.SPAWN_DESPAWN_DISTANCE);

            if (!Utils.IsPosShitSpawn(Players, spawnPos, SpawnerHost.SPAWN_MIN_DISTANCE))
            {
                Ped zombie = await World.CreatePed(PedHash.Zombie01, spawnPos);
                int zombieHandle = zombie.Handle;
                API.SetPedCombatRange(zombieHandle, 2);
                API.SetPedHearingRange(zombieHandle, float.MaxValue);
                API.SetPedCombatAttributes(zombieHandle, 46, true);
                API.SetPedCombatAttributes(zombieHandle, 5, true);
                API.SetPedCombatAttributes(zombieHandle, 1, false);
                API.SetPedCombatAttributes(zombieHandle, 0, false);
                API.SetPedCombatAbility(zombieHandle, 0);
                API.SetAiMeleeWeaponDamageModifier(float.MaxValue);
                API.SetPedRagdollBlockingFlags(zombieHandle, 4);
                API.SetPedCanPlayAmbientAnims(zombieHandle, false);

                int randHealth = Utils.GetRandomInt(1, ZOMBIE_MAX_HEALTH);
                zombie.MaxHealth = randHealth;
                zombie.Health = randHealth;
                zombie.Armor = Utils.GetRandomInt(ZOMBIE_MAX_ARMOR);
                zombie.RelationshipGroup = ZombieGroup;
                ZombieGroup.SetRelationshipBetweenGroups(Game.PlayerPed.RelationshipGroup, Relationship.Hate, true);
                ZombieAttrChances(zombie);

                zombie.Task.WanderAround();
                TriggerServerEvent("corruptsnail:newZombie", API.PedToNet(zombieHandle));

                zombies.Add(zombie);
            }
        }

        private void OnNewZombie(int zombieNetHandle)
        {
            int zombieHandle = API.NetToPed(zombieNetHandle);
            Ped zombie = new Ped(zombieHandle)
            {
                Voice = "ALIENS",
                IsPainAudioEnabled = false
            };

            API.RequestAnimSet("move_m@drunk@verydrunk");
            API.SetPedMovementClipset(zombieHandle, "move_m@drunk@verydrunk", 1f);
        }

        private void ZombieAttrChances(Ped zombie)
        {
            if (AttrChance())
                API.SetPedRagdollOnCollision(zombie.Handle, true);
            if (AttrChance())
                API.SetPedHelmet(zombie.Handle, true);
            if (AttrChance())
                API.SetPedRagdollBlockingFlags(zombie.Handle, 1);
        }

        private bool AttrChance()
        {
            return Utils.GetRandomFloat(1f) <= ZOMBIE_ATTR_CHANCE;
        }
    }
}
