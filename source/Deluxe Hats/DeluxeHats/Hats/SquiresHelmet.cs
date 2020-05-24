﻿using StardewValley;

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
