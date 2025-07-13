using MonoMod.Cil;
using RWCustom;
using System;
using UnityEngine;

namespace StoryMenagerie.Creatures
{
    class BigMothController : RainMeadow.GroundCreatureController
    {
        private bool forceMove;
        private bool forceFly;
        private readonly Watcher.BigMoth bigmoth;

        public BigMothController(Watcher.BigMoth bigmoth, RainMeadow.OnlineCreature oc, int playerNumber, CreatureCustomization customization) : base(bigmoth, oc, playerNumber, new ExpandedAvatarData(customization))
        {
            this.bigmoth = bigmoth;
        }

        internal static void EnableBigMoth()
        {
            On.Watcher.BigMoth.Update += BigMoth_Update;
            On.Watcher.BigMoth.Act += BigMoth_Act;
            On.Watcher.BigMothAI.Update += BigMothAI_Update;
        }

        public override WorldCoordinate CurrentPathfindingPosition
        {
            get
            {
                if (bigmoth != null)
                {
                    if (!forceMove && Custom.DistLess(creature.coord, bigmoth.AI.pathFinder.destination, 3))
                    {
                        return bigmoth.AI.pathFinder.destination;
                    }
                }
                return base.CurrentPathfindingPosition;
            }
        }

        private static void BigMothAI_Update(On.Watcher.BigMothAI.orig_Update orig, Watcher.BigMothAI self)
        {
            if (creatureControllers.TryGetValue(self.creature.realizedCreature, out var p))
            {
                p.AIUpdate(self);
            }
            else
            {
                orig(self);
            }
        }

        private static void BigMoth_Update(On.Watcher.BigMoth.orig_Update orig, Watcher.BigMoth self, bool eu)
        {
            if (creatureControllers.TryGetValue(self, out var q) && q is BigMothController p)
            {
                p.bigmoth.wantToFlyCounter = p.forceFly ? 0 : 100;
                p.Update(eu);
                var old = self.AI.bug.abstractCreature.controlled;
                self.AI.bug.abstractCreature.controlled = true;//глючное
                orig(self, eu);
                self.AI.bug.abstractCreature.controlled = old;
            }
            else
            {
                orig(self, eu);
            }
        }

        private static void BigMoth_Act(On.Watcher.BigMoth.orig_Act orig, Watcher.BigMoth self)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.ConsciousUpdate();
                var old = self.AI.bug.abstractCreature.controlled;
                self.AI.bug.abstractCreature.controlled = true;//глючное
                orig(self);
                self.AI.bug.abstractCreature.controlled = old;
            }
            else
            {
                orig(self);
            }
        }

        internal void ModifyBodyColor(RainMeadow.MeadowAvatarData self, ref Color ogColor)
        {
            if (self.skinData.baseColor.HasValue)
            {
                ogColor = self.skinData.baseColor.Value;
            }
            if (self.effectiveTintAmount > 0f)
            {
                var hslTint = RainMeadow.Extensions.ToHSL(self.tint);
                var hslOgColor = RainMeadow.Extensions.ToHSL(ogColor);
                ogColor = Color.Lerp(HSLColor.Lerp(hslOgColor, hslTint, self.effectiveTintAmount).rgb, Color.Lerp(ogColor, self.tint, self.effectiveTintAmount), 0.5f); // lerp in average of hsl and rgb, neither is good on its own
            }
        }

        public override bool HasFooting => bigmoth.legsOnGround >= 2;
        public override bool IsOnGround => IsTileGround(0, 0, -1) || IsTileGround(1, 0, -1);
        public override bool IsOnPole => !IsOnGround && GetTile(0).AnyBeam;
        public override bool IsOnCorridor => GetAITile(0).narrowSpace;
        public override bool IsOnClimb
        {
            get
            {
                var acc = GetAITile(0).acc;
                if (acc == AItile.Accessibility.Climb || acc == AItile.Accessibility.Wall)
                {
                    return true;
                }
                return false;
            }
        }

        public override void GripPole(Room.Tile tile0)
        {
            forceFly = false;
            for (int i = 0; i < creature.bodyChunks.Length; i++)
            {
                creature.bodyChunks[i].vel *= 0.25f;
            }
            creature.mainBodyChunk.vel += 0.2f * (creature.room.MiddleOfTile(tile0.X, tile0.Y) - creature.mainBodyChunk.pos);
        }

        public override void OnJump()
        {
            forceFly = true;
        }

        public override void MovementOverride(MovementConnection movementConnection)
        {
        }

        public override void ClearMovementOverride()
        {
        }

        public override void LookImpl(Vector2 pos)
        {
        }

        public override void Moving(float magnitude)
        {
            if (bigmoth != null)
            {
                bigmoth.AI.behavior = Watcher.BigMothAI.Behavior.Attack;
                forceMove = true;
                forceFly = false;
            }
        }

        public override void Resting()
        {
            if (bigmoth != null)
            {
                bigmoth.AI.behavior = Watcher.BigMothAI.Behavior.Idle;
                forceMove = false;
                forceFly = false;
            }
        }

        public override void OnCall()
        {
        }

        public override void PointImpl(Vector2 dir)
        {
        }
    }
}
