/*************************************************
**
** You're viewing a file in the SMAPI mod dump, which contains a copy of every open-source SMAPI mod
** for queries and analysis.
**
** This is *not* the original file, and not necessarily the latest version.
** Source repository: https://github.com/strobel1ght/Personal-Anvil
**
*************************************************/

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Object = StardewValley.Object;

namespace PersonalAnvil
{
    public class WorkbenchGeodeMenu : MenuWithInventory
    {
        private readonly List<TemporaryAnimatedSprite> _fluffSprites = new List<TemporaryAnimatedSprite>();
        public ClickableComponent GeodeSpot;
        private readonly AnimatedSprite _clint;
        private TemporaryAnimatedSprite _geodeDestructionAnimation;
        private TemporaryAnimatedSprite _sparkle;
        public int GeodeAnimationTimer;
        private int _yPositionOfGem;
        private int _alertTimer;
        private float _delayBeforeShowArtifactTimer;
        private Object _geodeTreasure;

        private readonly IContentHelper _content;

        public WorkbenchGeodeMenu(IContentHelper content)
            : base(null,
                true,
                true,
                12,
                132)
        {
            _content = content;
            if (yPositionOnScreen == borderWidth + spaceToClearTopBorder)
                movePosition(0,
                    -spaceToClearTopBorder);
            inventory.highlightMethod = HighlightGeodes;
            GeodeSpot = new ClickableComponent(new Rectangle(
                    xPositionOnScreen + spaceToClearSideBorder + borderWidth / 2,
                    yPositionOnScreen + spaceToClearTopBorder + 4,
                    560,
                    308),
                "")
            {
                myID = 998,
                downNeighborID = 0
            };
            _clint = new AnimatedSprite(_content.GetActualAssetKey("assets/Empty.png"),
                8,
                32,
                48);
            if (inventory.inventory != null && inventory.inventory.Count >= 12)
                for (var index = 0;
                    index < 12;
                    ++index)
                    if (inventory.inventory[index] != null)
                        inventory.inventory[index]
                            .upNeighborID = 998;

            if (trashCan != null)
                trashCan.myID = 106;
            if (okButton != null)
                okButton.leftNeighborID = 11;
            if (!Game1.options.SnappyMenus)
                return;
            populateClickableComponentList();
            snapToDefaultClickableComponent();
        }

        public sealed override void snapToDefaultClickableComponent()
        {
            currentlySnappedComponent = getComponentWithID(0);
            snapCursorToCurrentSnappedComponent();
        }

        public override bool readyToClose()
        {
            if (base.readyToClose() && GeodeAnimationTimer <= 0)
                return heldItem == null;
            return false;
        }

        private bool HighlightGeodes(Item i)
        {
            if (heldItem != null)
                return true;
            switch (i.ParentSheetIndex)
            {
                case 275:
                case 535:
                case 536:
                case 537:
                case 749:
                    return true;
                default:
                    return false;
            }
        }

        public override void receiveLeftClick(int x,
            int y,
            bool playSound = false)
        {
            base.receiveLeftClick(x,
                y,
                false);
            if (!GeodeSpot.containsPoint(x,
                y))
                return;
            if (heldItem != null &&
                (heldItem.Name.Contains("Geode") || heldItem.ParentSheetIndex == 275) &&
                GeodeAnimationTimer <= 0)
            {
                if (Game1.player.freeSpotsInInventory() > 1 ||
                    Game1.player.freeSpotsInInventory() == 1 && heldItem.Stack == 1)
                {
                    GeodeSpot.item = heldItem.getOne();
                    --heldItem.Stack;
                    if (heldItem.Stack <= 0)
                        heldItem = null;
                    GeodeAnimationTimer = 300;
                    Game1.playSound("stoneStep");
                    _clint.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                    {
                        new FarmerSprite.AnimationFrame(8,
                            24),
                        new FarmerSprite.AnimationFrame(9,
                            24),
                        new FarmerSprite.AnimationFrame(10,
                            24),
                        new FarmerSprite.AnimationFrame(11,
                            24),
                        new FarmerSprite.AnimationFrame(12,
                            24),
                        new FarmerSprite.AnimationFrame(8,
                            24)
                    });
                    _clint.loop = false;
                }
                else
                {
                    descriptionText = Game1.content.LoadString("Strings\\UI:GeodeMenu_InventoryFull");
                    wiggleWordsTimer = 500;
                    _alertTimer = 800;
                }
            }
            else
            {
                if (Game1.player.Money >= 0)
                    return;
                wiggleWordsTimer = 500;
                Game1.dayTimeMoneyBox.moneyShakeTimer = 1000;
            }
        }

        public override void receiveRightClick(int x,
            int y,
            bool playSound = true)
        {
            base.receiveRightClick(x,
                y);
        }

        public override void performHoverAction(int x,
            int y)
        {
            if (_alertTimer > 0)
                return;
            base.performHoverAction(x,
                y);
            if (!descriptionText.Equals(""))
                return;
            descriptionText =
                "Place geodes or artifact troves in the box on the left to break them! Keep the button pressed to break them continuously.";
        }

        public override void emergencyShutDown()
        {
            base.emergencyShutDown();
            if (heldItem == null)
                return;
            Game1.player.addItemToInventoryBool(heldItem);
        }

        public override void update(GameTime time)
        {
            base.update(time);
            for (var index = _fluffSprites.Count - 1;
                index >= 0;
                --index)
                if (_fluffSprites[index]
                    .update(time))
                    _fluffSprites.RemoveAt(index);

            if (_alertTimer > 0)
                _alertTimer -= time.ElapsedGameTime.Milliseconds;
            if (GeodeAnimationTimer <= 0)
                return;
            GeodeAnimationTimer -= time.ElapsedGameTime.Milliseconds;
            if (GeodeAnimationTimer <= 0)
            {
                _geodeDestructionAnimation = null;
                GeodeSpot.item = null;
                Game1.player.addItemToInventoryBool(_geodeTreasure);
                _geodeTreasure = null;
                _yPositionOfGem = 0;
                _fluffSprites.Clear();
                _delayBeforeShowArtifactTimer = 0.0f;
            }
            else
            {
                var currentFrame = _clint.currentFrame;
                _clint.animateOnce(time);
                if (_clint.currentFrame == 11 && currentFrame != 11)
                {
                    if (GeodeSpot.item != null && GeodeSpot.item.ParentSheetIndex == 275)
                    {
                        Game1.playSound("hammer");
                        Game1.playSound("woodWhack");
                    }
                    else
                    {
                        Game1.playSound("hammer");
                        Game1.playSound("stoneCrack");
                    }

                    Game1.player.gainExperience(Farmer.miningSkill,
                        3);
                    ++Game1.stats.GeodesCracked;
                    var y = 448;
                    if (GeodeSpot.item != null)
                    {
                        switch (((Object) GeodeSpot.item).ParentSheetIndex)
                        {
                            case 536:
                                y += 64;
                                break;
                            case 537:
                                y += 128;
                                break;
                        }

                        _geodeDestructionAnimation = new TemporaryAnimatedSprite("TileSheets\\animations",
                            new Rectangle(0,
                                y,
                                64,
                                64),
                            100f,
                            8,
                            0,
                            new Vector2(GeodeSpot.bounds.X + 285 - 32,
                                GeodeSpot.bounds.Y + 150 - 32),
                            false,
                            false);
                        if (GeodeSpot.item != null && GeodeSpot.item.ParentSheetIndex == 275)
                        {
                            _geodeDestructionAnimation = new TemporaryAnimatedSprite
                            {
                                texture = Game1.temporaryContent.Load<Texture2D>("LooseSprites//temporary_sprites_1"),
                                sourceRect = new Rectangle(388,
                                    123,
                                    18,
                                    21),
                                sourceRectStartingPos = new Vector2(388f,
                                    123f),
                                animationLength = 6,
                                position = new Vector2(GeodeSpot.bounds.X + 273 - 32,
                                    GeodeSpot.bounds.Y + 150 - 32),
                                holdLastFrame = true,
                                interval = 100f,
                                id = 777f,
                                scale = 4f
                            };
                            for (var index = 0;
                                index < 6;
                                ++index)
                            {
                                _fluffSprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors",
                                    new Rectangle(372,
                                        1956,
                                        10,
                                        10),
                                    new Vector2(GeodeSpot.bounds.X + 285 - 32 + Game1.random.Next(21),
                                        GeodeSpot.bounds.Y + 150 - 16),
                                    false,
                                    1f / 500f,
                                    new Color(byte.MaxValue,
                                        222,
                                        198))
                                {
                                    alphaFade = 0.02f,
                                    motion = new Vector2(Game1.random.Next(-20,
                                                             21) /
                                                         10f,
                                        Game1.random.Next(5,
                                            20) /
                                        10f),
                                    interval = 99999f,
                                    layerDepth = 0.9f,
                                    scale = 3f,
                                    scaleChange = 0.01f,
                                    rotationChange = (float) (Game1.random.Next(-5,
                                                                  6) *
                                                              3.14159274101257 /
                                                              256.0),
                                    delayBeforeAnimationStart = index * 20
                                });
                                _fluffSprites.Add(new TemporaryAnimatedSprite
                                {
                                    texture = Game1.temporaryContent.Load<Texture2D>(
                                        "LooseSprites//temporary_sprites_1"),
                                    sourceRect = new Rectangle(499,
                                        132,
                                        5,
                                        5),
                                    sourceRectStartingPos = new Vector2(499f,
                                        132f),
                                    motion = new Vector2(Game1.random.Next(-30,
                                                             31) /
                                                         10f,
                                        Game1.random.Next(-7,
                                            -4)),
                                    acceleration = new Vector2(0.0f,
                                        0.25f),
                                    totalNumberOfLoops = 1,
                                    interval = 1000f,
                                    alphaFade = 0.015f,
                                    animationLength = 1,
                                    layerDepth = 1f,
                                    scale = 4f,
                                    rotationChange = (float) (Game1.random.Next(-5,
                                                                  6) *
                                                              3.14159274101257 /
                                                              256.0),
                                    delayBeforeAnimationStart = index * 10,
                                    position = new Vector2(GeodeSpot.bounds.X + 285 - 32 + Game1.random.Next(21),
                                        GeodeSpot.bounds.Y + 150 - 16)
                                });
                                _delayBeforeShowArtifactTimer = 100f;
                            }
                        }

                        _geodeTreasure = Utility.getTreasureFromGeode(GeodeSpot.item);
                        if (GeodeSpot.item.ParentSheetIndex == 275)
                            Game1.player.foundArtifact(_geodeTreasure.ParentSheetIndex,
                                1);
                        else if (_geodeTreasure.Type.Contains("Mineral"))
                            Game1.player.foundMineral(_geodeTreasure.ParentSheetIndex);
                        else if (_geodeTreasure.Type.Contains("Arch") &&
                                 !Game1.player.hasOrWillReceiveMail("artifactFound"))
                            _geodeTreasure = new Object(390,
                                5);
                    }
                }

                if (_geodeDestructionAnimation != null &&
                    (_geodeDestructionAnimation.id != 777f && _geodeDestructionAnimation.currentParentTileIndex < 7 ||
                     _geodeDestructionAnimation.id == 777f && _geodeDestructionAnimation.currentParentTileIndex < 5))
                {
                    _geodeDestructionAnimation.update(time);
                    if (_delayBeforeShowArtifactTimer > 0.0)
                    {
                        _delayBeforeShowArtifactTimer -= (float) time.ElapsedGameTime.TotalMilliseconds;
                        if (_delayBeforeShowArtifactTimer <= 0.0)
                        {
                            _fluffSprites.Add(_geodeDestructionAnimation);
                            _fluffSprites.Reverse();
                            _geodeDestructionAnimation = new TemporaryAnimatedSprite
                            {
                                interval = 50f,
                                animationLength = 2,
                                alpha = 1f / 1000f,
                                id = 777f
                            };
                        }
                    }
                    else
                    {
                        if (_geodeDestructionAnimation.currentParentTileIndex < 3)
                            --_yPositionOfGem;
                        --_yPositionOfGem;
                        if ((_geodeDestructionAnimation.currentParentTileIndex == 7 ||
                             _geodeDestructionAnimation.id == 777f &&
                             _geodeDestructionAnimation.currentParentTileIndex == 5) &&
                            _geodeTreasure.Price > 75)
                        {
                            _sparkle = new TemporaryAnimatedSprite("TileSheets\\animations",
                                new Rectangle(0,
                                    640,
                                    64,
                                    64),
                                100f,
                                8,
                                0,
                                new Vector2(GeodeSpot.bounds.X + 285 - 32,
                                    GeodeSpot.bounds.Y + 150 + _yPositionOfGem - 32),
                                false,
                                false);
                            Game1.playSound("discoverMineral");
                        }
                        else if ((_geodeDestructionAnimation.currentParentTileIndex == 7 ||
                                  _geodeDestructionAnimation.id == 777f &&
                                  _geodeDestructionAnimation.currentParentTileIndex == 5) &&
                                 _geodeTreasure.Price <= 75)
                        {
                            Game1.playSound("newArtifact");
                        }
                    }
                }

                if (_sparkle == null || !_sparkle.update(time))
                    return;
                _sparkle = null;
            }
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds,
            Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds,
                newBounds);
            GeodeSpot = new ClickableComponent(new Rectangle(
                    xPositionOnScreen + spaceToClearSideBorder + borderWidth / 2,
                    yPositionOnScreen + spaceToClearTopBorder + 4,
                    560,
                    308),
                "Anvil");
            var yPosition = yPositionOnScreen + spaceToClearTopBorder + borderWidth + 192 - 16 + 128 + 4;
            inventory = new InventoryMenu(xPositionOnScreen + spaceToClearSideBorder + borderWidth / 2 + 12,
                yPosition,
                false,
                null,
                inventory.highlightMethod);
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect,
                Game1.graphics.GraphicsDevice.Viewport.Bounds,
                Color.Black * 0.4f);
            draw(b);
            Game1.dayTimeMoneyBox.drawMoneyBox(b);
            b.Draw(_content.Load<Texture2D>("assets/bgpatch.png"),
                new Vector2(GeodeSpot.bounds.X,
                    GeodeSpot.bounds.Y),
                new Rectangle(0,
                    0,
                    140,
                    78),
                Color.White,
                0.0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                0.87f);
            if (GeodeSpot.item != null)
            {
                if (_geodeDestructionAnimation == null)
                    GeodeSpot.item.drawInMenu(b,
                        new Vector2(GeodeSpot.bounds.X +
                                    253 +
                                    (GeodeSpot.item.ParentSheetIndex == 275
                                        ? -8
                                        : 0),
                            GeodeSpot.bounds.Y +
                            118 +
                            (GeodeSpot.item.ParentSheetIndex == 275
                                ? 8
                                : 0)),
                        1f);
                else
                    _geodeDestructionAnimation.draw(b,
                        true);
                foreach (var fluffSprite in _fluffSprites)
                    fluffSprite.draw(b,
                        true);
                if (_geodeTreasure != null && _delayBeforeShowArtifactTimer <= 0.0)
                    _geodeTreasure.drawInMenu(b,
                        new Vector2(GeodeSpot.bounds.X + 253,
                            GeodeSpot.bounds.Y + 118 + _yPositionOfGem),
                        1f);
                _sparkle?.draw(b,
                    true);
            }

            _clint.draw(b,
                new Vector2(GeodeSpot.bounds.X + 384,
                    GeodeSpot.bounds.Y + 64),
                0.877f);
            if (!hoverText.Equals(""))
                drawHoverText(b,
                    hoverText,
                    Game1.smallFont);
            heldItem?.drawInMenu(b,
                new Vector2(Game1.getOldMouseX() + 8,
                    Game1.getOldMouseY() + 8),
                1f);
            if (Game1.options.hardwareCursor)
                return;
            drawMouse(b);
        }
    }
}