/*************************************************
**
** You're viewing a file in the SMAPI mod dump, which contains a copy of every open-source SMAPI mod
** for queries and analysis.
**
** This is *not* the original file, and not necessarily the latest version.
** Source repository: https://github.com/spacechase0/CookingSkill
**
*************************************************/

using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CookingSkill
{
    public interface IApi
    {
        /// <summary>
        /// Modify a cooked item based on the player's cooking skill.
        /// Returns if ingredients should be consumed or not.
        /// </summary>
        /// <param name="recipe">The crafting recipe.</param>
        /// <param name="craftedItem">The crafted item from the recipe. Nothing is changed if the recipe isn't cooking.</param>
        /// <param name="additionalIngredients">The additional places to draw ingredients from.</param>
        /// <returns>If ingredients should be consumed or not.</returns>
        bool ModifyCookedItem( CraftingRecipe recipe, Item craftedItem, List<Chest> additionalIngredients );
    }

    public class Api : IApi
    {
        public bool ModifyCookedItem( CraftingRecipe recipe, Item craftedItem, List<Chest> additionalIngredients )
        {
            return Mod.onCook( recipe, craftedItem, additionalIngredients );
        }
    }
}
