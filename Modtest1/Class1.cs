using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Profiling.Experimental;

namespace MuckModTest {
    [BepInPlugin("me.kikkia.test", "testMod", "1.0.0")]
    public class MainClass : BaseUnityPlugin {
        public static MainClass instance;
        public Harmony harmony;
        public ManualLogSource logSource;
        public Dictionary<int, ChestWrapper> chests;
        public int seed;
        private String seedPath = "maps/seed.txt";
        public int frame = 0; // Counts frames to wait before completing
        public int SCORE_THRESHOLD = 200;

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
            }
            else {
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
            harmony.PatchAll(typeof(ForestGenPatch));

            logSource.LogInfo("Mod loaded");
        }

        private void OnDestroy() {
            harmony.UnpatchSelf();
        }

        public bool parseResults() {
            var score = 0;
            foreach (var chest in chests.Values) {
                foreach (var item in chest.items) {
                    if (weWantThese.ContainsKey(item)) {
                        score += weWantThese[item];
                    }

                    if (item == "Night Blade" || item == "Gronks Sword") {
                        markSword(chest, item);
                    }
                }
            }

            if (score > SCORE_THRESHOLD) {
                String path = "maps/" + score + "." + seed + ".txt";
                StreamWriter writer = new StreamWriter(path, false);
                logSource.LogInfo("=============== Chest Data Seed: " + seed + " ==============");
                foreach (var keyPair in chests) {
                    if (keyPair.Value != null) {
                        Vector3 position = keyPair.Value.transform.position;
                        writer.WriteLine("Chest " + keyPair.Key + ": " + position.x + ":" +
                                         position.y + " " + String.Join(", ", keyPair.Value.items));
                    }
                    else {
                        writer.WriteLine("The fuck, Null value for chest " + keyPair.Key);
                    }
                }

                writer.Close();
            }

            chests.Clear();
            return score > SCORE_THRESHOLD;
        }

        private int getSeed() {
            StreamReader reader = new StreamReader(seedPath);
            int toReturn = Int32.Parse(reader.ReadToEnd());
            reader.Close();
            return toReturn;
        }

        public void incrementSeed() {
            StreamWriter writer = new StreamWriter(seedPath, false);
            writer.WriteLine(seed);
            writer.Close();
            seed++;
        }

        public void generateMap() {
            Boat.Instance.UpdateShipStatus(Boat.BoatPackets.FindShip, 0);
            Boat.Instance.UpdateShipStatus(Boat.BoatPackets.MarkGems, 0);
            Map.Instance.ToggleMap();
            ScreenCapture.CaptureScreenshot(Directory.GetCurrentDirectory() + "/maps/map_" + seed + ".png");
        }

        /**
         * Marks a good sword on the map
         */
        private void markSword(ChestWrapper c, String name) {
            markItem(c, name, Color.black);
        }

        /**
         * Marks a custom mapper on the map
         */
        private void markItem(ChestWrapper c, String name, Color color) {
            Map.Instance.AddMarker(c.transform, Map.MarkerType.Gem, Boat.Instance.gemTexture, color, name);
        }
    }

    class MovementPatch {
        [HarmonyPatch(typeof(PlayerMovement), "Movement")]
        [HarmonyPrefix]
        static bool PrefixMovement() {
            if (MainClass.instance.frame > 3) {
                // If positive match, then generate map image
                if (MainClass.instance.parseResults()) {
                    MainClass.instance.generateMap();
                }

                GameManager.instance.LeaveGame();
                MainClass.instance.frame = 0;
                MainClass.instance.incrementSeed();
            }
            MainClass.instance.frame++;
            return true;
        }
    }

    class ChestAddPatch {
        [HarmonyPatch(typeof(ChestManager), "AddChest")]
        [HarmonyPrefix]
        static bool PrefixChestAdd(Chest c, int id) {
            MainClass.instance.chests.Add(id, new ChestWrapper(c.gameObject.transform, id));
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
            __result = MainClass.instance.seed;
            MainClass.instance.frame = 0;
        }
    }
    
    /**
     * Patches out the tree and rock generation to speed up world gen
     */
    class ForestGenPatch {
        [HarmonyPatch(typeof(ResourceGenerator), "GenerateForest")]
        [HarmonyPrefix]
        static bool PrefixGen() {
            return false;
        }
    }
}