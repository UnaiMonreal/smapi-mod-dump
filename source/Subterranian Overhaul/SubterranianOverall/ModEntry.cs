﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace SubterranianOverhaul
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        //remember to set all of this into a config file.
        private const double STONE_BASE_CHANCE_TO_CHANGE = 0.0010; //0.1% chance at level 90.
        private const double STONE_INCREASE_PER_LEVEL = 0.002; //results in a maximum chance at level 119 of 5.9% for each stone to change.
        private const double PURPLE_MUSHROOM_CHANCE_TO_CHANGE = 0.05; //5% chance of each purple mushroom changing.
        private const double PURPLE_MUSHROOM_INCREASE_PER_LEVEL = 0.005; //results in a maximum chance at level 119 of 19.5% for each purple mushroom to change.
        private const double RED_MUSHROOM_CHANCE_TO_CHANGE = 0.20; //10% chance of each red mushroom changing.
        private const double RED_MUSHROOM_INCREASE_PER_LEVEL = 0.010; //results in a maximum chance at level 119 of 59% for each red mushroom to change.

        private static ModEntry mod;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            ModEntry.mod = this;
            if (Game1.IsMasterGame)
            {
                Monitor.Log("Multiplayer ID is: "+this.ModManifest.UniqueID);
            }
            
            IndexManager.monitor = this.Monitor;
            
            helper.Events.Player.Warped += this.OnPlayerWarped;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.Saving += this.OnSave;
            helper.Events.GameLoop.Saved += this.AfterSave;
            helper.Events.GameLoop.SaveLoaded += this.AfterSaveLoad;
            helper.Events.Specialised.LoadStageChanged += this.OnLoadingStageChanged;
            helper.Events.Display.RenderedHud += this.OnRenderedHud;
            helper.Events.Multiplayer.PeerContextReceived += this.OnClientConnected;
            //helper.Events.Multiplayer.ModMessageReceived += this.ProcessModMessage;
        }

        public static IModHelper GetHelper()
        {
            return ModEntry.mod?.Helper;
        }

        public static IMonitor GetMonitor()
        {
            return ModEntry.mod?.Monitor;
        }


        /*********
        ** Private methods
        *********/

        private void OnClientConnected(object sender, PeerContextReceivedEventArgs e)
        {
            this.Helper.Content.AssetEditors.Add(new VoidshroomDataInjector(this.Monitor));
        }

        private void OnLoadingStageChanged(object sender, LoadStageChangedEventArgs e)
        {
            this.Helper.Content.AssetEditors.Add(new VoidshroomDataInjector(this.Monitor));
        }

        

        private static bool savingInProgress = false;

        private void OnSave(object sender, SavingEventArgs e)
        {
            if (!savingInProgress)
            {
                savingInProgress = true;
                VoidshroomTree.RemovalAll();
                ModState.visitedMineshafts.Clear();
                ModState.SaveMod();
                savingInProgress = false;
            }
        }

        private void AfterSave(object sender, SavedEventArgs e)
        {
            VoidshroomTree.ReplaceAll();
        }

        private void AfterSaveLoad(object sender, SaveLoadedEventArgs e)
        {
            ModState.LoadMod();
            VoidshroomTree.ReplaceAll();
        }

        /// <summary>
        /// Processes checks when the player warps so we can try to override some forage spawns.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlayerWarped(object sender, WarpedEventArgs e)
        {   
            if(e.NewLocation.Name.StartsWith("UndergroundMine") || e.NewLocation.Name.StartsWith("Mine"))
            {
                //clean up the list of levels.
                HashSet<String> newListOfVisitedLevels = new HashSet<String>();

                foreach(MineShaft mine in MineShaft.activeMines)
                {
                    if (ModState.visitedMineshafts.Contains(mine.Name))
                    {
                        newListOfVisitedLevels.Add(mine.Name);
                    }
                }

                ModState.visitedMineshafts = newListOfVisitedLevels;
            }

            if (e.NewLocation.Name.StartsWith("UndergroundMine")) {
                
                MineShaft loc = (MineShaft)e.NewLocation;

                SpawnVoidshroomTrees(loc);
            }
        }

        private void SpawnVoidshroomTrees(MineShaft loc)
        {
            this.Monitor.Log("Process voidshroom tree spawning for "+loc.Name);
            String floorText = loc.Name.Substring(15);
            int floorNumber = 0;
            if (int.TryParse(floorText, out floorNumber))
            {
                int floorsAbove90 = floorNumber - 90;
                double extraStoneChance = floorsAbove90 * STONE_INCREASE_PER_LEVEL;
                double stoneChanceToChange = STONE_BASE_CHANCE_TO_CHANGE + extraStoneChance;
                double extraPurpleMushroomChance = floorsAbove90 * PURPLE_MUSHROOM_INCREASE_PER_LEVEL;
                double purpleMushroomChanceToChange = PURPLE_MUSHROOM_CHANCE_TO_CHANGE + extraPurpleMushroomChance;
                double extraRedMushroomChance = floorsAbove90 * RED_MUSHROOM_INCREASE_PER_LEVEL;
                double redMushroomChanceToChange = RED_MUSHROOM_CHANCE_TO_CHANGE + extraRedMushroomChance;

                List<Vector2> toReplace = new List<Vector2>();

                //only proceed if there is a chance of something actually changing into a mushroom tree. For red mushrooms this chance starts at level 70, for purple mushrooms it starts at 80 and for rocks it starts at 90
                if (stoneChanceToChange >= 0 || purpleMushroomChanceToChange >= 0 || redMushroomChanceToChange >= 0)
                {
                    Vector2 origin = new Vector2(0, 0);

                    if (loc.Objects.ContainsKey(origin))
                    {
                        if(loc.Objects[origin] is LoadMarker)
                        {
                            this.Monitor.Log("We've probably already hit this floor ("+loc.Name+"). Skip processing for voidshroom trees.");
                            return;
                        } else
                        {
                            this.Monitor.Log("We didn't have a load marker for (" + loc.Name + "), but something was sitting at 0,0. Figure out what it was!");
                        }
                    } else
                    {
                        //we haven't processed this floor probably, so go ahead and do so and add a marker object so A) we know it's been done and B) when the floor resets we'll process again next time we visit.
                        this.Monitor.Log("We haven't processed before. Add marker to 0,0 on (" + loc.Name + ")");
                        loc.Objects.Add(origin, new LoadMarker());
                    }
                    
                    //if we've already populated this mine level today, don't do so again.
                    /*
                    if (ModState.visitedMineshafts.Contains(loc.Name))
                    {
                        this.Monitor.Log(loc.Name + " was already visited and populated with voidshroom trees today.");
                        return;
                    }
                    */
                    
                    //ModState.visitedMineshafts.Add(loc.Name);

                    foreach (StardewValley.Object o in loc.Objects.Values)
                    {
                        double hit = Game1.random.NextDouble();
                        if (o.Name == "Stone" && hit <= stoneChanceToChange)
                        {
                            toReplace.Add(o.TileLocation);
                        }
                        else if (o.ParentSheetIndex == 420 && hit <= redMushroomChanceToChange)
                        {
                            toReplace.Add(o.TileLocation);
                        }
                        else if (o.ParentSheetIndex == 422 && hit <= purpleMushroomChanceToChange)
                        {
                            toReplace.Add(o.TileLocation);
                        }
                    }

                    this.Monitor.Log(loc.Name + " found " + toReplace.Count + " places to replace with a voidshroom tree.");

                    foreach (Vector2 location in toReplace)
                    {
                        loc.Objects.Remove(location);
                        loc.terrainFeatures.Add(location, (TerrainFeature)new VoidshroomTree(Game1.random.Next(4, 6)));
                    }
                }
            }
        }

        private bool isGameReady()
        {
            return !(!Context.IsWorldReady || Game1.currentLocation == null || Game1.player.CurrentItem == null);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (isGameReady())
            {
                Item currentItem = Game1.player.CurrentItem;

                if (currentItem.ParentSheetIndex == VoidshroomSpore.getIndex() && e.Button.IsActionButton())
                {
                    this.Monitor.Log("Attempt to plant a new voidshroom tree at "+e.Cursor.GrabTile.ToString());
                    bool planted = VoidshroomSpore.AttemptPlanting(e.Cursor.GrabTile, Game1.currentLocation);
                    if (planted)
                    {
                        Game1.player.reduceActiveItemByOne();
                    }
                }
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (isGameReady())
            {
                Item currentItem = Game1.player.CurrentItem;

                if(currentItem.ParentSheetIndex == VoidshroomSpore.getIndex())
                {
                    VoidshroomSpore.drawPlacementBounds((StardewValley.Object)currentItem, e.SpriteBatch, Game1.player.currentLocation);
                }
            }
        }
    }
}