using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MuckModTest {
    [BepInPlugin("me.kikkia.test", "testMod", "1.0.0")]
    public class MainClass : BaseUnityPlugin {
        public static MainClass instance;
        public Harmony harmony;
        public ManualLogSource logSource;
        public Dictionary<int, ChestWrapper> chests;
        public int seed = Int32.MinValue;
        
        private void Awake() {
            if (instance == null) {
                instance = this;
            } else {
                Destroy(this);
            }

            chests = new Dictionary<int, ChestWrapper>();

            logSource = Logger;
            harmony = new Harmony("me.kikkia.test");

            harmony.PatchAll(typeof(ChestInitPatch));
            harmony.PatchAll(typeof(SetGetSeedPatch));
            harmony.PatchAll(typeof(ChestAddPatch));
            harmony.PatchAll(typeof(MovementPatch));

            logSource.LogInfo("Mod loaded");
        }

        private void OnDestroy() {
            harmony.UnpatchSelf();
        }
    }

    class MovementPatch {
        [HarmonyPatch(typeof(PlayerMovement), "Movement")]
        [HarmonyPrefix]
        static bool PrefixMovement() {
            MainClass.instance.logSource.LogInfo("=============== Chest Data Seed: " + GameManager.instance + " ==============");
            foreach(var keyPair in MainClass.instance.chests) {
                if (keyPair.Value != null) {
                    MainClass.instance.logSource.LogInfo("Chest " + keyPair.Key + ": " + keyPair.Value.x + ":" + 
                                                         keyPair.Value.y + " " + String.Join(", ", keyPair.Value.items));
                } else {
                    MainClass.instance.logSource.LogInfo("The fuck, Null value for chest " + keyPair.Key);
                }
                
            }
            
            SteamLobby.Instance.StartGame();
            return true;
        }
    }

    class ChestAddPatch {
        [HarmonyPatch(typeof(ChestManager), "AddChest")]
        [HarmonyPrefix]
        static bool PrefixChestAdd(Chest c, int id) {
            Vector3 position = c.gameObject.transform.position;
            MainClass.instance.chests.Add(id, new ChestWrapper(position.x, position.y, id));
            return true;
        }
    }

    
    class ChestInitPatch {
        [HarmonyPatch(typeof(Chest), "InitChest")]
        [HarmonyPrefix]
        static bool PrefixChestInit(Chest __instance, List<InventoryItem> items) {
            foreach(InventoryItem i in items) {
                if (i != null) {
                    MainClass.instance.chests[__instance.id].addItem(i.amount + " - " + i.name);
                }
            }

            return true;
        }
    }

    /**
     * Patches into the getSeed the game used to inject what seed we want.
     */
    class SetGetSeedPatch {
        [HarmonyPatch(typeof(SteamLobby), "FindSeed")]
        [HarmonyPostfix]
        static void PostfixFindSeed(ref int __result) {
            __result = MainClass.instance.seed; // TODO: Make seed management function
            MainClass.instance.seed++;
        }
    }

}