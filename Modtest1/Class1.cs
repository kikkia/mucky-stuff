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

        private Dictionary<String, int> weWantThese = new Dictionary<string, int>() {
            {"Night Blade", 250},
            {"Gronks Sword", 200},
            {"Adamantite Ore", 30},
            {"Oak Wood", 30},
            {"Mithril Ore", 20},
            {"Adamantite Axe", 30},
            {"Obamium Ore", 30},
            {"rope", 5}
        };
        
        private void Awake() {
            if (instance == null) {
                instance = this;
            } else {
                Destroy(this);
            }
            
            weWantThese["Adamantite Ore"] = 30;
            weWantThese["Oak Wood"] = 30;
            weWantThese["Mithril Ore"] = 20;
            weWantThese["Adamantite Axe"] = 50;

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
            var score = 0;
            foreach (var chest in chests.Values) {
                foreach (var item in chest.items) {
                    if (weWantThese.ContainsKey(item)) {
                        score += weWantThese[item];
                    }
                }
            }

            if (score > 200) {
                String path = "maps/" + score + "." + seed + ".txt";
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