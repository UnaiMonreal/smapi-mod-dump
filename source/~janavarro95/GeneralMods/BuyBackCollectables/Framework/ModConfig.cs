/*************************************************
**
** You're viewing a file in the SMAPI mod dump, which contains a copy of every open-source SMAPI mod
** for queries and analysis.
**
** This is *not* the original file, and not necessarily the latest version.
** Source repository: https://github.com/janavarro95/Stardew_Valley_Mods
**
*************************************************/

using StardewModdingAPI;

namespace Omegasis.BuyBackCollectables.Framework
{
    /// <summary>The mod configuration.</summary>
    internal class ModConfig
    {
        /// <summary>The key which shows the menu.</summary>
        public SButton KeyBinding { get; set; } = SButton.B;

        /// <summary>The multiplier applied to the cost of buying back a collectable.</summary>
        public double CostMultiplier { get; set; } = 3.0;
    }
}
