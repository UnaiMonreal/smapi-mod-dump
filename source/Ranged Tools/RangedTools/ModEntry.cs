/*************************************************
**
** You're viewing a file in the SMAPI mod dump, which contains a copy of every open-source SMAPI mod
** for queries and analysis.
**
** This is *not* the original file, and not necessarily the latest version.
** Source repository: https://github.com/vgperson/RangedTools
**
*************************************************/

using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace RangedTools
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private static IMonitor myMonitor;
        private static IInputHelper myInput;
        private static ModConfig Config;
        
        public static bool specialClickActive = false;
        public static Vector2 specialClickLocation = Vector2.Zero;
        public static List<SButton> knownToolButtons = new List<SButton>();
        
        public static bool eventUpReset = false;
        public static bool eventUpOld = false;
        
        /***************************
         ** Mod Injection Methods **
         ***************************/
        
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            try
            {
                myMonitor = this.Monitor;
                myInput = this.Helper.Input;
                Config = this.Helper.ReadConfig<ModConfig>();
                
                helper.Events.Input.ButtonPressed += this.OnButtonPressed;
                helper.Events.Input.CursorMoved += this.OnCursorMoved;
                
                HarmonyInstance harmonyInstance = HarmonyInstance.Create(this.ModManifest.UniqueID);
                
                patchPrefix(harmonyInstance, typeof(Farmer), nameof(Farmer.useTool),
                            typeof(ModEntry), nameof(ModEntry.Prefix_useTool));
                
                patchPrefix(harmonyInstance, typeof(Utility), nameof(Utility.isWithinTileWithLeeway),
                            typeof(ModEntry), nameof(ModEntry.Prefix_isWithinTileWithLeeway));
                
                patchPrefix(harmonyInstance, typeof(Game1), nameof(Game1.pressUseToolButton),
                            typeof(ModEntry), nameof(ModEntry.Prefix_pressUseToolButton));
                
                if (Config.ToolHitLocationDisplay > 0)
                {
                    patchPrefix(harmonyInstance, typeof(Farmer), nameof(Farmer.draw),
                                typeof(ModEntry), nameof(ModEntry.Prefix_draw),
                                new Type[] { typeof(SpriteBatch) });
                    
                    patchPostfix(harmonyInstance, typeof(Farmer), nameof(Farmer.draw),
                                 typeof(ModEntry), nameof(ModEntry.Postfix_draw),
                                 new Type[] { typeof(SpriteBatch) });
                }
            }
            catch (Exception e)
            {
                Log("Error in mod setup: " + e.Message + Environment.NewLine + e.StackTrace);
            }
        }
        
        /// <summary>Attempts to patch the given source method with the given prefix method.</summary>
        /// <param name="harmonyInstance">The Harmony instance to patch with.</param>
        /// <param name="sourceClass">The class the source method is part of.</param>
        /// <param name="sourceName">The name of the source method.</param>
        /// <param name="patchClass">The class the patch method is part of.</param>
        /// <param name="patchName">The name of the patch method.</param>
        /// <param name="sourceParameters">The source method's parameter list, when needed for disambiguation.</param>
        void patchPrefix(HarmonyInstance harmonyInstance, System.Type sourceClass, string sourceName, System.Type patchClass, string patchName, Type[] sourceParameters = null)
        {
            try
            {
                MethodBase sourceMethod = AccessTools.Method(sourceClass, sourceName, sourceParameters);
                HarmonyMethod prefixPatch = new HarmonyMethod(patchClass, patchName);
                
                if (sourceMethod != null && prefixPatch != null)
                    harmonyInstance.Patch(sourceMethod, prefixPatch);
                else
                {
                    if (sourceMethod == null)
                        Log("Warning: Source method (" + sourceClass.ToString() + "::" + sourceName + ") not found or ambiguous.");
                    if (prefixPatch == null)
                        Log("Warning: Patch method (" + patchClass.ToString() + "::" + patchName + ") not found.");
                }
            }
            catch (Exception e)
            {
                Log("Error in code patching: " + e.Message + Environment.NewLine + e.StackTrace);
            }
        }
        
        /// <summary>Attempts to patch the given source method with the given postfix method.</summary>
        /// <param name="harmonyInstance">The Harmony instance to patch with.</param>
        /// <param name="sourceClass">The class the source method is part of.</param>
        /// <param name="sourceName">The name of the source method.</param>
        /// <param name="patchClass">The class the patch method is part of.</param>
        /// <param name="patchName">The name of the patch method.</param>
        /// <param name="sourceParameters">The source method's parameter list, when needed for disambiguation.</param>
        void patchPostfix(HarmonyInstance harmonyInstance, Type sourceClass, string sourceName, Type patchClass, string patchName, Type[] sourceParameters = null)
        {
            try
            {
                MethodBase sourceMethod = AccessTools.Method(sourceClass, sourceName, sourceParameters);
                HarmonyMethod postfixPatch = new HarmonyMethod(patchClass, patchName);
                
                if (sourceMethod != null && postfixPatch != null)
                    harmonyInstance.Patch(sourceMethod, postfix: postfixPatch);
                else
                {
                    if (sourceMethod == null)
                        Log("Warning: Source method (" + sourceClass.ToString() + "::" + sourceName + ") not found or ambiguous.");
                    if (postfixPatch == null)
                        Log("Warning: Patch method (" + patchClass.ToString() + "::" + patchName + ") not found.");
                }
            }
            catch (Exception e)
            {
                Log("Error in code patching: " + e.Message + Environment.NewLine + e.StackTrace);
            }
        }
        
        /******************
         ** Input Method **
         ******************/
        
        /// <summary>Checks whether to enable ToolLocation override when a Tool Button is pressed.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            try
            {
                if (!Context.IsWorldReady || Game1.player == null) // If no save is loaded yet or player doesn't exist
                    return;
                
                if (!isAppropriateTimeToUseTool()) // Tools not allowed in various circumstances
                    return;
                
                bool withClick = e.Button.ToString().Contains("Mouse");
                
                // Tool Button was pressed; required to be mouse button if that setting is enabled.
                if (e.Button.IsUseToolButton() && (withClick || !Config.CustomRangeOnClickOnly))
                {
                    Farmer player = Game1.player;
                    if (player.CurrentTool != null && !player.UsingTool) // Have a tool selected, not in the middle of using it
                    {
                        Vector2 mousePosition = convertCursorPosition(e.Cursor);
                        
                        // If setting is enabled, face all mouse clicks when a tool/weapon is equipped.
                        if (withClick && shouldToolTurnToFace(player.CurrentTool))
                            player.faceGeneralDirection(mousePosition);
                        
                        if (positionValidForExtendedRange(player, mousePosition))
                        {
                            // Set this as an override location for when tool is used.
                            specialClickActive = true;
                            specialClickLocation = mousePosition;
                            if (!knownToolButtons.Contains(e.Button)) // Keep a list of Tool Buttons (accounting for click-only option)
                                knownToolButtons.Add(e.Button);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error in button press: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        
        /// <summary>Checks for mouse drag while holding any Tool Button.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnCursorMoved(object sender, CursorMovedEventArgs e)
        {
            try
            {
                if (holdingToolButton()) // Update override location as long as a Tool Button is held (range validity checked later)
                    specialClickLocation = convertCursorPosition(e.NewPosition);
            }
            catch (Exception ex)
            {
                Log("Error in cursor move: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        
        /// <summary>Returns Vector2 global coordinates of mouse position, adjusted for scale.</summary>
        /// <param name="cursor">The cursor position data.</param>
        public static Vector2 convertCursorPosition(ICursorPosition cursor)
        {
            Vector2 mousePosition = Utility.ModifyCoordinatesFromUIScale(cursor.ScreenPixels);
            mousePosition.X += Game1.viewport.X;
            mousePosition.Y += Game1.viewport.Y;
            return mousePosition;
        }
        
        /// <summary>Checks whether the given Farmer and mouse position are within extended range for the current tool.</summary>
        /// <param name="who">The Farmer using the tool.</param>
        /// <param name="mousePosition">The position of the mouse.</param>
        public static bool positionValidForExtendedRange(Farmer who, Vector2 mousePosition)
        {
            Tool currentTool = who.CurrentTool;
            
            if (isToolOverridable(currentTool)) // Only override ToolLocation for applicable tools
            {
                int range = getCustomRange(currentTool);
                
                // Mouse position is within tool's range, or range setting is negative (infinite).
                if (range < 0 || Utility.withinRadiusOfPlayer((int)mousePosition.X, (int)mousePosition.Y, range, who))
                {
                    bool usableOnPlayerTile = getPlayerTileSetting(currentTool);
                    
                    // If not allowed to use on player tile, ensure mouse is not within radius 0 of player.
                    if (usableOnPlayerTile || !Utility.withinRadiusOfPlayer((int)mousePosition.X, (int)mousePosition.Y, 0, who))
                        return true;
                }
            }
            
            return false;
        }
        
        /// <summary>Returns whether a known Tool Button is still being held.</summary>
        /// <param name="clickOnly">Whether to only check for mouse Tool Buttons.</param>
        public static bool holdingToolButton(bool clickOnly = false)
        {
            foreach (SButton button in knownToolButtons)
                if (myInput.IsDown(button)
                 && (!clickOnly || button.ToString().Contains("Mouse"))
                 && button.IsUseToolButton()) // Double-check in case it changed
                    return true;
            
            return false;
        }
        
        /// <summary>Returns whether tools can be used currently. Does not check whether player is in the middle of using tool.</summary>
        public static bool isAppropriateTimeToUseTool()
        {
            return !Game1.fadeToBlack
                && !Game1.dialogueUp
                && !Game1.eventUp
                && !Game1.menuUp
                && Game1.currentMinigame == null
                && !Game1.player.hasMenuOpen
                && !Game1.player.isRidingHorse()
                && (Game1.CurrentEvent == null || Game1.CurrentEvent.canPlayerUseTool());
        }
        
        /// <summary>Returns whether the given tool is a type that supports ToolLocation override.</summary>
        /// <param name="tool">The Tool being checked.</param>
        public static bool isToolOverridable(Tool tool)
        {
            return tool is Axe
                || tool is Pickaxe
                || tool is Hoe
                || tool is WateringCan;
        }
        
        /// <summary>Returns whether the given tool is a type that should face mouse clicks (i.e. sprite won't glitch).</summary>
        /// <param name="tool">The Tool being checked.</param>
        public static bool shouldToolTurnToFace(Tool tool, bool buttonHeld = false)
        {
            return (tool is Axe && Config.ToolAlwaysFaceClick)
                || (tool is Pickaxe && Config.ToolAlwaysFaceClick)
                || (tool is Hoe && Config.ToolAlwaysFaceClick && !buttonHeld)
                || (tool is WateringCan && Config.ToolAlwaysFaceClick && !buttonHeld)
                || (tool is MeleeWeapon && Config.WeaponAlwaysFaceClick && !buttonHeld);
        }
        
        /// <summary>Returns custom range setting for overridable tools (1 for any others).</summary>
        /// <param name="tool">The Tool being checked.</param>
        public static int getCustomRange(Tool tool)
        {
            return tool is Axe? Config.AxeRange
                 : tool is Pickaxe? Config.PickaxeRange
                 : tool is Hoe? Config.HoeRange
                 : tool is WateringCan? Config.WateringCanRange
                 : 1;
        }
       
        /// <summary>Returns "usable on player tile" setting for overridable tools (1 for any others).</summary>
        /// <param name="tool">The Tool being checked.</param>
        public static bool getPlayerTileSetting(Tool tool)
        {
            return tool is Axe? Config.AxeUsableOnPlayerTile
                 : tool is Pickaxe? Config.PickaxeUsableOnPlayerTile
                 : tool is Hoe? Config.HoeUsableOnPlayerTile
                 : true;
        }
        
        /********************
         ** Method Patches **
         ********************/
        
        /// <summary>Prefix to Farmer.useTool that overrides GetToolLocation with click location.</summary>
        /// <param name="who">The Farmer using the tool.</param>
        public static bool Prefix_useTool(Farmer who)
        {
            try
            {
                if (!positionValidForExtendedRange(who, specialClickLocation)) // Disable override if target position is out of range
                    specialClickActive = false;
                else if (holdingToolButton()) // Itherwise, force use of override as long as a Tool Button is being held
                    specialClickActive = true;
                // Note that simply clicking and letting go should leave specialClickActive as true for the next use.
                
                if (specialClickActive)
                {
                    if (who.toolOverrideFunction == null) // Equivalent to "else" branch of original function, only relevant part
                    {
                        if (who.CurrentTool == null) // Check from original function
                            return true; // Go to original function (where it should just terminate due to tool being null, but still)
                        if (who.toolPower > 0 && !Config.AllowRangedChargeEffects) // Don't override charged tool use unless enabled
                            return true; // Go to original function
                        
                        float stamina = who.stamina; // From original function
                        if (who.IsLocalPlayer) // From original function
                        {
                            // Call DoFunction like original function, but on the override location
                            who.CurrentTool.DoFunction(who.currentLocation, (int)specialClickLocation.X, (int)specialClickLocation.Y, 1, who);
                            if (!holdingToolButton()) // Performed action, and button has been let go, so don't use override anymore
                                specialClickActive = false;
                        }
                        
                        // Usual post-DoFunction tasks from original function
                        who.lastClick = Vector2.Zero;
                        who.checkForExhaustion(stamina);
                        Game1.toolHold = 0.0f;
                        return false; // Don't do original function anymore
                    }
                }
                return true; // Go to original function
            }
            catch (Exception e)
            {
                Log("Error in useTool: " + e.Message + Environment.NewLine + e.StackTrace);
                return true; // Go to original function
            }
        }
        
        /// <summary>Rewrite of Utility.isWithinTileWithLeeway that returns true if within object/seed custom range.</summary>
        /// <param name="x">The X location.</param>
        /// <param name="y">The Y location.</param>
        /// <param name="item">The item being placed.</param>
        /// <param name="f">The Farmer placing it.</param>
        /// <param name="__result">The returned result.</param>
        public static bool Prefix_isWithinTileWithLeeway(int x, int y, Item item, Farmer f, ref bool __result)
        {
            try
            {
                bool bigCraftable = (item as StardewValley.Object).bigCraftable;
                
                // Base game relies on short range to prevent placing Crab Pots in unreachable places, so always use default range.
                if (!bigCraftable && item.parentSheetIndex == 710) // Crab Pot
                    return true; // Go to original function
                
                // Though original behavior shows green when placing Tapper as long as highlighted tile is in range,
                // this becomes particularly confusing at longer range settings, so check that there is in fact an empty tree.
                if (bigCraftable && item.parentSheetIndex == 105) // Tapper
                {
                    Vector2 tile = new Vector2(x / 64, y / 64);
                    if (!f.currentLocation.terrainFeatures.ContainsKey(tile) // No special terrain at tile
                     || !(f.currentLocation.terrainFeatures[tile] is Tree) // Terrain at tile is not a tree
                     || f.currentLocation.objects.ContainsKey(tile)) // Tree tile is already occupied
                    {
                        __result = false;
                        return false; // Don't do original function anymore
                    }
                }
                
                int range = item.category == StardewValley.Object.SeedsCategory
                         || item.category == StardewValley.Object.fertilizerCategory? Config.SeedRange
                                                                                    : Config.ObjectPlaceRange;
                
                if (range < 0 || Utility.withinRadiusOfPlayer(x, y, range, f))
                {
                    __result = true;
                    return false; // Don't do original function anymore
                }
                return true; // Go to original function
            }
            catch (Exception e)
            {
                Log("Error in isWithinTileWithLeeway: " + e.Message + Environment.NewLine + e.StackTrace);
                return true; // Go to original function
            }
        }
        
        /// <summary>Turns player to face mouse if Tool Button is being held.</summary>
        public static bool Prefix_pressUseToolButton()
        {
            try
            {
                if (Game1.player == null) // Go to original function
                    return true;
                
                if (specialClickActive && holdingToolButton(true) && shouldToolTurnToFace(Game1.player.CurrentTool, true))
                    Game1.player.faceGeneralDirection(specialClickLocation);
                return true; // Go to original function
            }
            catch (Exception e)
            {
                Log("Error in pressUseToolButton: " + e.Message + Environment.NewLine + e.StackTrace);
                return true; // Go to original function
            }
        }
        
        /// <summary>Prefix to Farmer.draw that draws tool hit location at extended ranges.</summary>
        /// <param name="__instance">The instance of the Farmer.</param>
        /// <param name="b">The sprite batch.</param>
        public static bool Prefix_draw(Farmer __instance, SpriteBatch b)
        {
            try
            {
                // Abort cases from original function
                if (__instance.currentLocation == null
                 || (!__instance.currentLocation.Equals(Game1.currentLocation)
                  && !__instance.IsLocalPlayer
                  && !Game1.currentLocation.Name.Equals("Temp")
                  && !__instance.isFakeEventActor)
                 || ((bool)(NetFieldBase<bool, NetBool>)__instance.hidden
                  && (__instance.currentLocation.currentEvent == null || __instance != __instance.currentLocation.currentEvent.farmer)
                  && (!__instance.IsLocalPlayer || Game1.locationRequest == null))
                 || (__instance.viewingLocation.Value != null
                  && __instance.IsLocalPlayer))
                    return true; // Go to original function
                
                // Conditions for drawing tool hit indicator from original function
                if (Game1.activeClickableMenu == null
                 && !Game1.eventUp
                 && (__instance.IsLocalPlayer && __instance.CurrentTool != null)
                 && (Game1.oldKBState.IsKeyDown(Keys.LeftShift) || Game1.options.alwaysShowToolHitLocation)
                 && __instance.CurrentTool.doesShowTileLocationMarker()
                 && (!Game1.options.hideToolHitLocationWhenInMotion || !__instance.isMoving()))
                {
                    Vector2 mousePosition = Utility.PointToVector2(Game1.getMousePosition()) 
                                          + new Vector2((float)Game1.viewport.X, (float)Game1.viewport.Y);
                    Vector2 limitedLocal = Game1.GlobalToLocal(Game1.viewport, Utility.clampToTile(__instance.GetToolLocation(mousePosition)));
                    Vector2 extendedLocal = Game1.GlobalToLocal(Game1.viewport, Utility.clampToTile(mousePosition));
                    if (!limitedLocal.Equals(extendedLocal)) // Just fall back on original if clamped position is the same
                    {
                        if (positionValidForExtendedRange(__instance, mousePosition))
                        {
                            b.Draw(Game1.mouseCursors, extendedLocal,
                                   new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 29)),
                                   Color.White, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, extendedLocal.Y / 10000f);
                            
                            if (Config.ToolHitLocationDisplay == 1) // 2 shows both, but 1 should hide the default indicator
                            {
                                eventUpOld = Game1.eventUp;
                                Game1.eventUp = true; // Temporarily set this to disable drawing of original indicator
                                eventUpReset = true; // Make sure it's set back to original value in the postfix
                            }
                        }
                    }
                }
                return true; // Go to original function
            }
            catch (Exception e)
            {
                Log("Error in Farmer draw: " + e.Message + Environment.NewLine + e.StackTrace);
                return true; // Go to original function
            }
        }
        
        /// <summary>Postfix to Farmer.draw that resets eventUp to what it was.</summary>
        public static void Postfix_draw()
        {
            if (eventUpReset)
            {
                Game1.eventUp = eventUpOld;
                eventUpReset = false;
            }
        }
        
        /*******************
         ** Debug Methods **
         *******************/
        
        /// <summary>Prints a message to the SMAPI console.</summary>
        /// <param name="message">The log message.</param>
        public static void Log(string message)
        {
            if (myMonitor != null)
                myMonitor.Log(message, LogLevel.Debug);
        }
    }
}