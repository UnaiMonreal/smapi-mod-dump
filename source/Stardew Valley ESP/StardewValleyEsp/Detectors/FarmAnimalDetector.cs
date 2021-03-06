/*************************************************
**
** You're viewing a file in the SMAPI mod dump, which contains a copy of every open-source SMAPI mod
** for queries and analysis.
**
** This is *not* the original file, and not necessarily the latest version.
** Source repository: https://github.com/rei2hu/stardewvalley-esp
**
*************************************************/

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValleyEsp.Config;

namespace StardewValleyEsp.Detectors
{
    class FarmAnimalDetector : IDetector
    {
        private GameLocation location;
        private readonly Settings settings;

        public FarmAnimalDetector(Settings settings)
        {
            this.settings = settings;
        }

        public EntityList Detect()
        {
            EntityList e = new EntityList();
            if (location != null && location is Farm)
                foreach (var c in ((Farm)location).getAllFarmAnimals())
                {
                    // bug: if an animal is located in a building, the position is
                    // incorrect
                    if (location.isTileOnMap(c.getTileLocation()))
                        e.Add(new KeyValuePair<Vector2, object>(c.getTileLocation(), c));
                }
            return e;
        }

        public IDetector SetLocation(GameLocation loc)
        {
            location = loc;
            return this;
        }
    }
}
