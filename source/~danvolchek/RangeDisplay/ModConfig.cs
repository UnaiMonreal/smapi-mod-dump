/*************************************************
**
** You're viewing a file in the SMAPI mod dump, which contains a copy of every open-source SMAPI mod
** for queries and analysis.
**
** This is *not* the original file, and not necessarily the latest version.
** Source repository: https://github.com/danvolchek/StardewMods
**
*************************************************/

using StardewModdingAPI;

namespace RangeDisplay
{
    /// <summary>The mod configuration.</summary>
    public class ModConfig
    {
        /*********
        ** Accessors
        *********/

        /// <summary>The key which cycles the active display category.</summary>
        public SButton CycleActiveDisplayKey { get; set; } = SButton.F2;

        /// <summary>Whether to show the range of the item being held or not.</summary>
        public bool ShowRangeOfHeldItem { get; set; } = true;

        /// <summary>Whether to show the range of the item being hovered over not.</summary>
        public bool ShowRangeOfHoveredOverItem { get; set; } = true;

        /// <summary>The hover modifier key.</summary>
        public SButton[] HoverModifierKeys { get; set; } = { SButton.LeftControl, SButton.RightControl };
    }
}
