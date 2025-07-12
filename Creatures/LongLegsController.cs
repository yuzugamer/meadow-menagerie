using MonoMod.Cil;
using RainMeadow;
using RWCustom;
using System;
using UnityEngine;

namespace StoryMenagerie.Creatures
{
    public class LongLegsController : RainMeadow.CreatureController
    {
        private bool forceMove;
        public bool story;

        private readonly DaddyLongLegs longlegs;

        public static void ApplyHooks()
        {
            On.DaddyLongLegs.Update += DaddyLongLegs_Update;
            On.DaddyLongLegs.Act += DaddyLongLegs_Act;
            On.DaddyAI.Update += DaddyAI_Update;
        }

        public override WorldCoordinate CurrentPathfindingPosition
        {
            get
            {
                if (!forceMove && Custom.DistLess(creature.coord, longlegs.AI.pathFinder.destination, 3))
                {
                    return longlegs.AI.pathFinder.destination;
                }
                return base.CurrentPathfindingPosition;
            }
        }

        private static void DaddyAI_Update(On.DaddyAI.orig_Update orig, DaddyAI self)
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

        private static void DaddyLongLegs_Update(On.DaddyLongLegs.orig_Update orig, DaddyLongLegs self, bool eu)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.Update(eu);
                var old = self.AI.daddy.abstractCreature.controlled;
                self.AI.daddy.abstractCreature.controlled = true;//глючное
                orig(self, eu);
                self.AI.daddy.abstractCreature.controlled = old;
            }
            else
            {
                orig(self, eu);
            }
        }

        private static void DaddyLongLegs_Act(On.DaddyLongLegs.orig_Act orig, DaddyLongLegs self, int legsGrabbing)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.ConsciousUpdate();
                var old = self.AI.daddy.abstractCreature.controlled;
                self.AI.daddy.abstractCreature.controlled = true;//глючное
                orig(self, legsGrabbing);
                self.AI.daddy.abstractCreature.controlled = old;
            }
            else
            {
                orig(self, legsGrabbing);
            }
        }

        public LongLegsController(DaddyLongLegs creature, RainMeadow.OnlineCreature oc, int playerNumber, CreatureCustomization customization) : base(creature, oc, playerNumber, new ExpandedAvatarData(customization))
        {
            this.longlegs = creature;
            var c2 = this.longlegs.eyeColor;
            this.longlegs.eyeColor = customization.bodyColor;
            this.longlegs.effectColor = customization.bodyColor;
            story = true;
        }

        public override void LookImpl(Vector2 pos)
        {
            // //longlegs.AI.reactTarget = Custom.MakeWorldCoordinate(new IntVector2((int)(pos.x / 20f), (int)(pos.y / 20f)), this.longlegs.room.abstractRoom.index);
            // longlegs.tentacles[0].SwitchTask(DaddyTentacle.Task.Locomotion);
            // var dir = Custom.DirVec(longlegs.mainBodyChunk.pos, pos);
            // //if (longlegs.graphicsModule is DaddyGraphics graphics) graphics.lookDir = (mouse.mainBodyChunk.pos - pos) / 500f;
            // longlegs.tentacles[0].Tip.vel += 0.5f * dir;
            // longlegs.tentacles[0].Tip.vel -= 0.5f * dir;
        }

        public override void Moving(float magnitude)
        {
            longlegs.AI.behavior = DaddyAI.Behavior.Hunt;
            //longlegs.AI.flySpeed = Custom.LerpAndTick(longlegs.AI.flySpeed, magnitude, 0.2f, 0.05f);
            forceMove = true;
        }

        public override void Resting()
        {
            longlegs.AI.behavior = DaddyAI.Behavior.Idle;
            //longlegs.AI.flySpeed = Custom.LerpAndTick(longlegs.AI.flySpeed, 0, 0.4f, 0.1f);
            forceMove = false;
        }

        public override void OnCall()
        {
            //truly
        }

        public override void PointImpl(Vector2 dir)
        {
            //uh
        }
    }
}
