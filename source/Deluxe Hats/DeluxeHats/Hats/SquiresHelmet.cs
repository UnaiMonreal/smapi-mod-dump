/*************************************************
**
** You're viewing a file in the SMAPI mod dump, which contains a copy of every open-source SMAPI mod
** for queries and analysis.
**
** This is *not* the original file, and not necessarily the latest version.
** Source repository: https://github.com/domsim1/stardew-valley-deluxe-hats-mod
**
*************************************************/

using StardewValley;

namespace DeluxeHats.Hats
{
    public static class SquiresHelmet
    {
        public const string Name = "Squire's Helmet";
        public const string Description = "Gain +6 armour and +4 attack";
        private const int squiresResilience = 6;
        private const int squiresAttack = 4;
        public static void Activate()
        {
            Game1.player.resilience += squiresResilience;
            Game1.player.attackIncreaseModifier += squiresAttack;
        }

        public static void Disable()
        {
            Game1.player.resilience -= squiresResilience;
            Game1.player.attackIncreaseModifier -= squiresAttack;
        }
    }
}