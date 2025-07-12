using MonoMod.Cil;
using RWCustom;
using System;
using UnityEngine;

namespace StoryMenagerie.Creatures
{
    public class BarnacleController : RainMeadow.GroundCreatureController
    {
        private bool forceMove;
        private readonly Watcher.Barnacle barnacle;

        public BarnacleController(Watcher.Barnacle creature, RainMeadow.OnlineCreature oc, int playerNumber, CreatureCustomization customization) : base(creature, oc, playerNumber, new ExpandedAvatarData(customization))
        {
            this.barnacle = creature;
        }

        public static void ApplyHooks()
        {
            On.Watcher.Barnacle.Update += (On.Watcher.Barnacle.orig_Update orig, Watcher.Barnacle self, bool eu) =>
            {
                if (creatureControllers.TryGetValue(self, out var p))
                {
                    p.Update(eu);
                    var old = self.AI.realizedCreature.abstractCreature.controlled;
                    self.AI.realizedCreature.abstractCreature.controlled = true;//глючное
                    orig(self, eu);
                    self.AI.realizedCreature.abstractCreature.controlled = old;
                }
                else
                {
                    orig(self, eu);
                }
            };
            On.Watcher.Barnacle.Act += (On.Watcher.Barnacle.orig_Act orig, Watcher.Barnacle self) =>
            {
                if (creatureControllers.TryGetValue(self, out var p))
                {
                    p.ConsciousUpdate();
                    var old = self.AI.realizedCreature.abstractCreature.controlled;
                    self.AI.realizedCreature.abstractCreature.controlled = true;//глючное
                    orig(self);
                    self.AI.realizedCreature.abstractCreature.controlled = old;
                }
                else
                {
                    orig(self);
                }
            };
            On.Watcher.BarnacleAI.Update += (On.Watcher.BarnacleAI.orig_Update orig, Watcher.BarnacleAI self) =>
            {
                if (creatureControllers.TryGetValue(self.creature.realizedCreature, out var p))
                {
                    p.AIUpdate(self);
                }
                else
                {
                    orig(self);
                }
            };
        }

        public override WorldCoordinate CurrentPathfindingPosition
        {
            get
            {
                if (barnacle != null)
                {
                    if (!forceMove && Custom.DistLess(creature.coord, barnacle.AI.pathFinder.destination, 3))
                    {
                        return barnacle.AI.pathFinder.destination;
                    }
                }
                return base.CurrentPathfindingPosition;
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

        public override bool HasFooting => barnacle.Footing;
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
            if (barnacle.footingCounter < 10)
            {
                for (int i = 0; i < creature.bodyChunks.Length; i++)
                {
                    creature.bodyChunks[i].vel *= 0.25f;
                }
                creature.mainBodyChunk.vel += 0.2f * (creature.room.MiddleOfTile(tile0.X, tile0.Y) - creature.mainBodyChunk.pos);
                barnacle.footingCounter = 20;
            }
        }

        public override void OnJump()
        {
            barnacle.footingCounter = 0;
        }

        public override void MovementOverride(MovementConnection movementConnection)
        {
            if (barnacle.Footing || barnacle.Submersion > 0f)
            {
                barnacle.specialMoveCounter = 5;
                barnacle.specialMoveDestination = movementConnection.DestTile;
                barnacle.footingCounter = Mathf.Min(barnacle.footingCounter, 20);
            }
        }

        public override void ClearMovementOverride()
        {
            barnacle.specialMoveCounter = 0;
        }

        public override void LookImpl(Vector2 pos)
        {
        }

        public override void Moving(float magnitude)
        {
            if (barnacle != null)
            {
                barnacle.AI.behavior = Watcher.BarnacleAI.Behavior.Flee;
                forceMove = true;
            }
        }

        public override void Resting()
        {
            if (barnacle != null)
            {
                barnacle.AI.behavior = Watcher.BarnacleAI.Behavior.Idle;
                forceMove = false;
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
