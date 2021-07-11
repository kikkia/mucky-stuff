using System;
using System.Collections.Generic;
using System.IO;
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
        public int seed;
        private String seedPath = "maps/seed.txt";
        public bool done = false;

        private List<String> weWantThese = new List<string> { "Night Blade", "Gronks Sword"};
        
        private void Awake() {
            if (instance == null) {
                instance = this;
            } else {
                Destroy(this);
            }

            chests = new Dictionary<int, ChestWrapper>();
            seed = getSeed();

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

        public void parseResults() {
            var ladiesAndGentlemenWeGotEm = false;
            foreach (var chest in chests.Values) {
                foreach (var item in chest.items) {
                    if (weWantThese.Contains(item)) {
                        ladiesAndGentlemenWeGotEm = true;
                        break;
                    }
                }
                if (ladiesAndGentlemenWeGotEm) {
                    break;
                }
            }
            logSource.LogInfo("=============== Chest Data Seed: " + seed + " ==============");

            if (ladiesAndGentlemenWeGotEm) {
                String path = "maps/data" + seed + ".json";
                StreamWriter writer = new StreamWriter(path, false);
                logSource.LogInfo("=============== Chest Data Seed: " + seed + " ==============");
                foreach(var keyPair in chests) {
                    if (keyPair.Value != null) {
                        writer.WriteLine("Chest " + keyPair.Key + ": " + keyPair.Value.x + ":" + 
                                         keyPair.Value.y + " " + String.Join(", ", keyPair.Value.items));
                    } else {
                        writer.WriteLine("The fuck, Null value for chest " + keyPair.Key);
                    }
                }
                writer.Close();
            }
            
            chests.Clear();
            incrementSeed();
        }

        private int getSeed() {
            StreamReader reader = new StreamReader(seedPath);
            int toReturn = Int32.Parse(reader.ReadToEnd());
            reader.Close();
            return toReturn;
        }

        private void incrementSeed() {
            StreamWriter writer = new StreamWriter(seedPath, false);
            writer.WriteLine(seed);
            writer.Close();
            seed++;
        }
    }

    class MovementPatch {
        [HarmonyPatch(typeof(PlayerMovement), "Movement")]
        [HarmonyPrefix]
        static bool PrefixMovement() {
            if (!MainClass.instance.done) {
                MainClass.instance.parseResults();
                GameManager.instance.LeaveGame();
                MainClass.instance.done = true;
            }
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
                    MainClass.instance.chests[__instance.id].addItem(i.name);
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
            MainClass.instance.done = false;
        }
    }
}