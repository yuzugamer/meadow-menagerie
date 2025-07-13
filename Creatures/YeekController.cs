using MonoMod.RuntimeDetour;
using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;
using RWCustom;
using MoreSlugcats;
using static RewiredConsts.Layout;

namespace StoryMenagerie.Creatures
{
    public class YeekController : RainMeadow.GroundCreatureController
    {
        public Yeek yeek;
        public bool lastGrab;
        public int biteCounter;
        public bool lastJump;
        public StuckTracker stuckTracker;
        public int downTimer;
        public YeekController(Yeek yeek, OnlineCreature oc, int playerNumber, CreatureCustomization customization) : base(yeek, oc, playerNumber, new ExpandedAvatarData(customization))
        {
            this.yeek = yeek;
            this.story().storyCustomization = customization;
            this.customization = new ExpandedAvatarData(customization);
            if (yeek.AI.stuckTracker != null)
            {
                stuckTracker = yeek.AI.stuckTracker;
            }
            else
            {
                stuckTracker = new StuckTracker(yeek.AI, true, true);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (yeek.Consious && input[0].pckp && input[0].x == 0 && input[0].y == 0)
            {
                if (yeek.grasps != null)
                {
                    biteCounter++;
                    if (biteCounter > 20)
                    {
                        BiteEdible(eu);
                        biteCounter = 0;
                    }
                }
            }
            else
            {
                biteCounter = 0;
            }
        }

        public override void ConsciousUpdate()
        {
            base.ConsciousUpdate();
            stuckTracker.Update();

            if (yeek.grasps[0] == null)
            {
                var candidate = this.PickupCandidate(0f, 40f);
                if (candidate != null && candidate != pickUpCandidate && candidate is PlayerCarryableItem carryable)
                {
                    carryable.Blink();
                }
                pickUpCandidate = candidate;
            }
            else
            {
                pickUpCandidate = null;
            }

            if (input[0].x != 0)
            {
                yeek.firstChunk.vel.x += input[0].x * 0.25f;
            }

            if (yeek.climbingMode)
            {
                yeek.interestInClimbingPoles = 1f;
                if (input[0].y < 0 && input[1].y >= 0)
                {
                    if (downTimer > 0)
                    {
                        yeek.EndClimb();
                    }
                    downTimer = 30;
                }
                else if (downTimer > 0)
                {
                    downTimer--;
                }
            }
            else
            {
                downTimer = 0;
            }

            if (input[0].pckp)
            {
                if (!lastGrab)
                {
                    if (yeek.grasps[0] == null)
                    {
                        if (pickUpCandidate != null)
                        {
                            yeek.Grab(pickUpCandidate, 0, 0, Creature.Grasp.Shareability.CanNotShare, 1f, true, true);
                        }
                    }
                    else if (input[0].y < 0)
                    {
                        yeek.LoseAllGrasps();
                    }
                }
                lastGrab = true;
            }
            else
            {
                lastGrab = false;
            }

            if (input[0].jmp)
            {
                if (yeek.climbingMode)
                {
                    if (input[0].x != 0)
                    {
                        yeek.EndClimb();
                    }
                }
                else
                {
                    var connection = (yeek.AI.pathFinder as StandardPather).FollowPath(yeek.room.GetWorldCoordinate(yeek.mainBodyChunk.pos), true);
                    if (!lastJump || (stuckTracker.Utility() >= 0.75f || yeek.firstChunk.contactPoint.y < 0 || (connection.type == MovementConnection.MovementType.Standard || connection.type == MovementConnection.MovementType.Slope || connection.type == MovementConnection.MovementType.CeilingSlope || connection.type == MovementConnection.MovementType.OpenDiagonal || connection.type == MovementConnection.MovementType.SemiDiagonalReach || connection.type == MovementConnection.MovementType.ReachUp || (connection.type == MovementConnection.MovementType.ShortCut && yeek.shortcutDelay > 0))))
                    {
                        yeek.timeSinceHop = 0;
                        var dir = yeek.firstChunk.pos + (input[0].AnyDirectionalInput ? new Vector2(input[0].x, input[0].y) * 3.5f : Vector2.up);
                        yeek.Hop(yeek.firstChunk.pos, dir, true, true, true);
                    }
                }
                lastJump = true;
            }
            else
            {
                lastJump = false;
            }

            if (yeek.climbingMode || input[0].y > 0)
            {
                yeek.interestInClimbingPoles = 1f;
            }
        }

        //public override void Jump()
        //{
        //    yeek.timeSinceHop = 0;
        //    var dir = yeek.firstChunk.pos + (input[0].AnyDirectionalInput ? new Vector2(input[0].x, input[0].y) * 5f : Vector2.up);
        //    yeek.Hop(yeek.firstChunk.pos, dir, true, true, true);
        //}

        public override bool HasFooting => yeek.OnGround;

        public override bool IsOnGround => IsTileGround(0, 0, -1) || IsTileGround(1, 0, -1);
        public override bool IsOnPole => !IsOnGround && GetTile(0).AnyBeam;
        public override bool IsOnCorridor => GetAITile(0).narrowSpace;

        public override bool IsOnClimb
        {
            get
            {
                if (WallClimber)
                {
                    var acc = GetAITile(0).acc;
                    if ((acc == AItile.Accessibility.Climb && !GetTile(0).AnyBeam) || acc == AItile.Accessibility.Wall)
                    {
                        return true;
                    }
                    acc = GetAITile(1).acc;
                    if ((acc == AItile.Accessibility.Climb && !GetTile(1).AnyBeam) || acc == AItile.Accessibility.Wall)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override void GripPole(Room.Tile tile0)
        {
            if (!yeek.OnGround)
            {
                RainMeadow.RainMeadow.Debug("gripped");
                for (int i = 0; i < creature.bodyChunks.Length; i++)
                {
                    creature.bodyChunks[i].vel *= 0.25f;
                }
                creature.mainBodyChunk.vel += 0.2f * (creature.room.MiddleOfTile(tile0.X, tile0.Y) - creature.mainBodyChunk.pos);
            }
        }

        public override void OnJump()
        {
        }

        public override void MovementOverride(MovementConnection movementConnection)
        {
        }

        public override void ClearMovementOverride()
        {
        }

        public override bool CanPounce => false;

        public override WorldCoordinate CurrentPathfindingPosition
        {
            get
            {
                return base.CurrentPathfindingPosition;
            }
        }

        public override void OnCall()
        {
        }

        public override void Resting()
        {
            yeek.AI.behavior = YeekAI.Behavior.Idle;
        }

        public override void Moving(float magnitude)
        {
            yeek.AI.behavior = YeekAI.Behavior.Fear;
        }

        public override void LookImpl(Vector2 pos)
        {
        }

        public void BiteEdible(bool eu)
        {
            var grabbed = yeek.grasps[0].grabbed;
            if (yeek.grasps[0] != null && grabbed is IPlayerEdible edible && edible.Edible)
            {
                var graphics = yeek.graphicsModule as YeekGraphics;
                if (graphics != null)
                {
                    var headPos = yeek.bodyChunks[0].pos;
                    yeek.bodyChunks[0].vel += Custom.DirVec(headPos, yeek.grasps[0].grabbedChunk.pos);
                    graphics.headDrawDirection = Custom.DirVec(headPos, yeek.grasps[0].grabbedChunk.pos);
                    graphics.blinkStartCounter = 0;
                }
                this.FoodBitByPlayer(grabbed, 0, eu);
            }
        }

        public static void ApplyHooks()
        {
            On.MoreSlugcats.YeekAI.Update += On_YeekAI_Update;
            On.MoreSlugcats.Yeek.Act += On_Yeek_Act;
            On.MoreSlugcats.Yeek.Update += On_Yeek_Update;
            On.MoreSlugcats.Yeek.Collide += On_Yeek_Collide;
            On.MoreSlugcats.YeekGraphics.CreateCosmeticAppearance += On_YeekGraphics_CreateCosmeticAppearance;
            On.MoreSlugcats.Yeek.Climb += On_Yeek_Climb;
        }

        public static void On_YeekAI_Update(On.MoreSlugcats.YeekAI.orig_Update orig, YeekAI self)
        {
            if (CreatureController.creatureControllers.TryGetValue(self.creature.realizedCreature, out var cc))
            {
                cc.AIUpdate(self);
            }
            else
            {
                orig(self);
            }
        }

        public static void On_Yeek_Act(On.MoreSlugcats.Yeek.orig_Act orig, Yeek self)
        {
            if (CreatureController.creatureControllers.TryGetValue(self, out var p))
            {
                p.ConsciousUpdate();
                orig(self);
            }
            else
            {
                orig(self);
            }
        }

        public static void On_Yeek_Update(On.MoreSlugcats.Yeek.orig_Update orig, Yeek self, bool eu)
        {
            if (CreatureController.creatureControllers.TryGetValue(self, out var cc))
            {
                cc.Update(eu);
                self.abstractCreature.controlled = true;
                orig(self, eu);
                self.abstractCreature.controlled = false;
                if (!self.Consious)
                {
                    cc.pickUpCandidate = null;
                }
            }
            else orig(self, eu);
        }

        public static void On_Yeek_Collide(On.MoreSlugcats.Yeek.orig_Collide orig, Yeek self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (CreatureController.creatureControllers.TryGetValue(self, out var _))
            {
                // bad?
                return;
            }
            orig(self, otherObject, myChunk, otherChunk);
        }

        public static void On_YeekGraphics_CreateCosmeticAppearance(On.MoreSlugcats.YeekGraphics.orig_CreateCosmeticAppearance orig, YeekGraphics self)
        {
            orig(self);
            if (CreatureController.creatureControllers.TryGetValue(self.myYeek, out var cc) && cc.isStory(out var scc))
            {
                self.tailHighlightColor = scc.storyCustomization.bodyColor;
                self.furColor = scc.storyCustomization.eyeColor;
            }
        }

        public static void On_Yeek_Climb(On.MoreSlugcats.Yeek.orig_Climb orig, Yeek self, IntVector2 climbTile)
        {
            if (OnlineManager.lobby != null && CreatureController.creatureControllers.TryGetValue(self, out var cc))
            {
                climbTile = self.abstractCreature.pos.Tile + new IntVector2(cc.input[0].x * 2, cc.input[0].y * 2);
                // This prevents softlocks
                if (self.room.GetTile(climbTile).Terrain == Room.Tile.TerrainType.Slope)
                    climbTile = self.abstractCreature.pos.Tile;
            }
            orig(self, climbTile);
        }
    }
}

