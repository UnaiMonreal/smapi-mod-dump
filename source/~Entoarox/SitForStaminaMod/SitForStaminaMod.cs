/*************************************************
**
** You're viewing a file in the SMAPI mod dump, which contains a copy of every open-source SMAPI mod
** for queries and analysis.
**
** This is *not* the original file, and not necessarily the latest version.
** Source repository: https://github.com/Entoarox/StardewMods
**
*************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StardewValley;

using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Entoarox.SitForStamina
{
    class SitForStaminaMod : Mod
    {
        private Config Config;
        private uint Ticks = 0;
        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<Config>();
            helper.Events.GameLoop.UpdateTicked += this.OnGameLoopUpdateTicked;
        }

        private void OnGameLoopUpdateTicked(object s, UpdateTickedEventArgs e)
        {
            foreach (var player in Game1.getAllFarmers())
            {
                if (player.IsSitting())
                {
                    player.modData["Entoarox.SitForStamina:Ticks"] += "|";
                    if (player.modData["Entoarox.SitForStamina:Ticks"].Length == this.Config.TicksPerRegen)
                    {
                        player.modData["Entoarox.SitForStamina:Ticks"] = "";
                        if (player.Stamina < player.MaxStamina)
                            player.Stamina++;
                    }
                }
                else
                {
                    player.modData["Entoarox.SitForStamina:Ticks"] = "";
                }
            }
        }
    }
}
