using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace SandboxTweaks
{
    /// <summary>
    /// Builds the "all games on floor 1" loot pool: a runtime copy of the Floor 1
    /// loot table whose entries are the de-duplicated union of floors 1-4. The copy
    /// is never written to disk and the original tables are left untouched.
    /// </summary>
    internal static class LootPool
    {
        private const string Floor1Path = "FloorLootTables/Floor 1";
        private static readonly string[] AllFloorPaths =
        {
            "FloorLootTables/Floor 1",
            "FloorLootTables/Floor 2",
            "FloorLootTables/Floor 3",
            "FloorLootTables/Floor 4",
        };

        private static MMLootTableGameObjectSO _floor1Original;
        private static MMLootTableGameObjectSO _combined;

        /// <summary>The unmodified Floor 1 loot table SO — used to recognise the first floor.</summary>
        public static MMLootTableGameObjectSO Floor1Table =>
            _floor1Original != null
                ? _floor1Original
                : (_floor1Original = Resources.Load<MMLootTableGameObjectSO>(Floor1Path));

        /// <summary>
        /// Cached runtime copy of the Floor 1 table whose ObjectsToLoot is the
        /// distinct union of floors 1-4. Null if the tables can't be loaded.
        /// </summary>
        public static MMLootTableGameObjectSO Combined
        {
            get
            {
                if (_combined != null) return _combined;

                var floor1 = Floor1Table;
                if (floor1 == null)
                {
                    Plugin.Log.LogWarning("[SandboxTweaks] Floor 1 loot table not found — cannot build mixed pool");
                    return null;
                }

                var union = new List<MMLootGameObject>();
                var seen = new HashSet<GameObject>();
                foreach (var path in AllFloorPaths)
                {
                    var table = Resources.Load<MMLootTableGameObjectSO>(path);
                    if (table == null || table.LootTable == null || table.LootTable.ObjectsToLoot == null)
                        continue;
                    foreach (var entry in table.LootTable.ObjectsToLoot)
                    {
                        if (entry == null || entry.Loot == null) continue;
                        if (!seen.Add(entry.Loot)) continue;
                        union.Add(entry);
                    }
                }

                if (union.Count == 0)
                {
                    Plugin.Log.LogWarning("[SandboxTweaks] mixed pool union is empty — keeping vanilla Floor 1 table");
                    return null;
                }

                _combined = Object.Instantiate(floor1);
                _combined.name = "SandboxTweaks_MixedFloor1";
                if (_combined.LootTable == null)
                    _combined.LootTable = new MMLootTableGameObject();
                _combined.LootTable.ObjectsToLoot = union;
                Plugin.Log.LogInfo("[SandboxTweaks] mixed floor-1 pool built: " + union.Count + " game prefabs");
                return _combined;
            }
        }
    }
}
