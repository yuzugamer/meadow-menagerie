using MonoMod.Cil;
using RWCustom;
using System;
using UnityEngine;

namespace StoryMenagerie.Creatures
{
    class PoleMimicController : RainMeadow.CreatureController
    {
        private bool forceMove;
        private readonly PoleMimic polemimic;

        internal static void EnablePoleMimic()
        {
            On.PoleMimic.Update += PoleMimic_Update;
            On.PoleMimic.Act += PoleMimic_Act;
        }

        private static void PoleMimic_Update(On.PoleMimic.orig_Update orig, PoleMimic self, bool eu)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.Update(eu);
                var old = self.abstractCreature.controlled;
                self.abstractCreature.controlled = true;//глючное
                orig(self, eu);
                self.abstractCreature.controlled = old;
            }
            else
            {
                orig(self, eu);
            }
        }

        private static void PoleMimic_Act(On.PoleMimic.orig_Act orig, PoleMimic self)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.ConsciousUpdate();
                var old = self.abstractCreature.controlled;
                self.abstractCreature.controlled = true;//глючное
                orig(self);
                self.abstractCreature.controlled = old;
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

        public PoleMimicController(PoleMimic polemimic, RainMeadow.OnlineCreature oc, int playerNumber, CreatureCustomization customization) : base(polemimic, oc, playerNumber, new ExpandedAvatarData(customization))
        {
            this.polemimic = polemimic;
        }

        public override void LookImpl(Vector2 pos)
        {
            //polemimic.AI.reactTarget = Custom.MakeWorldCoordinate(new IntVector2((int)(pos.x / 20f), (int)(pos.y / 20f)), this.polemimic.room.abstractRoom.index);
        }

        public override void Moving(float magnitude)
        {
            //polemimic.AI.behavior = PoleMimicAI.Behavior.Hunt;
            forceMove = true;
        }

        public override void Resting()
        {
            //polemimic.AI.behavior = PoleMimicAI.Behavior.Idle;
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
