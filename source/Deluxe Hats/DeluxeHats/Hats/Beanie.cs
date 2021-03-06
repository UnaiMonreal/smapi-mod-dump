/*************************************************
**
** You're viewing a file in the SMAPI mod dump, which contains a copy of every open-source SMAPI mod
** for queries and analysis.
**
** This is *not* the original file, and not necessarily the latest version.
** Source repository: https://github.com/domsim1/stardew-valley-deluxe-hats-mod
**
*************************************************/

using Harmony;
using StardewValley;
using System;

namespace DeluxeHats.Hats
{
    public static class Beanie
    {
        public const string Name = "Beanie";
        public const string Description = "Monsters have a greater chance of dropping loot.";
        public static void Activate()
        {
            HatService.Harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.isWearingRing)),
                prefix: new HarmonyMethod(typeof(Beanie), nameof(Beanie.IsWearingRing_Prefix)));
        }

        public static void Disable()
        {
            HatService.Harmony.Unpatch(
               AccessTools.Method(typeof(Farmer), nameof(Farmer.isWearingRing)),
               HarmonyPatchType.Prefix,
               HatService.HarmonyId);
        }

        public static bool IsWearingRing_Prefix(ref bool __result, int ringIndex)
        {
            try
            {
                if (ringIndex == 526) {
                    __result = true;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                HatService.Monitor.Log($"Failed in {nameof(IsWearingRing_Prefix)}:\n{ex}");
                return true;
            }
        }
    }
}
