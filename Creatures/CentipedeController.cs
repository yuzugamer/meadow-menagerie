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

namespace StoryMenagerie.Creatures
{
    public class CentipedeController : CreatureController
    {
        public Centipede centi;
        public CentipedeController(Centipede centi, OnlineCreature oc, int playerNumber, CreatureCustomization customization) : base(centi, oc, playerNumber, new ExpandedAvatarData(customization))
        {
            this.centi = centi;
        }

        // most of this stuff is just stolen from dr's more creatures, so that i could get an idea of how this stuff works. all of this desperately needs to be redone

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
            //centi.AI.behavior = CentipedeAI.Behavior.Idle;
        }

        public override void Moving(float magnitude)
        {
            centi.AI.behavior = CentipedeAI.Behavior.Flee;
        }

        public override void LookImpl(Vector2 pos)
        {
        }

        public static void ApplyHooks()
        {
            On.CentipedeAI.Update += On_CentipedeAI_Update;
            On.Centipede.Act += On_Centipede_Act;
            On.Centipede.Update += On_Centipede_Update;
            new Hook(typeof(CentipedeGraphics).GetMethod("get_ShellColor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), On_CentipedeGraphics_get_ShellColor);
            new Hook(typeof(CentipedeGraphics).GetMethod("get_SecondaryShellColor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), On_CentipedeGraphics_get_SecondaryShellColor);
            On.CentipedeGraphics.DrawSprites += On_CentipedeGraphics_DrawSprites;
        }

        public static void On_CentipedeAI_Update(On.CentipedeAI.orig_Update orig, CentipedeAI self)
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

        public static void On_Centipede_Act(On.Centipede.orig_Act orig, Centipede self)
        {
            if (CreatureController.creatureControllers.TryGetValue(self, out var cc))
            {
                cc.ConsciousUpdate();
                self.abstractCreature.controlled = true;
                orig(self);
                self.abstractCreature.controlled = false;
            }
            else
            {
                orig(self);
            }
        }

        public static void On_Centipede_Update(On.Centipede.orig_Update orig, Centipede self, bool eu)
        {
            if (CreatureController.creatureControllers.TryGetValue(self, out var cc))
            {
                cc.Update(eu);
                self.abstractCreature.controlled = true;
                orig(self, eu);
                self.abstractCreature.controlled = false;
            }
            else orig(self, eu);
        }

        public static Color On_CentipedeGraphics_get_ShellColor(Func<CentipedeGraphics, Color> orig, CentipedeGraphics self)
        {
            if (CreatureController.creatureControllers.TryGetValue(self.centipede, out var cc) && cc.isStory(out var scc))
            {
                return Color.Lerp(scc.storyCustomization.bodyColor, self.blackColor, self.darkness);
            }
            return orig(self);
        }

        public static Color On_CentipedeGraphics_get_SecondaryShellColor(Func<CentipedeGraphics, Color> orig, CentipedeGraphics self)
        {
            if (CreatureController.creatureControllers.TryGetValue(self.centipede, out var cc) && cc.isStory(out var scc))
            {
                return Color.Lerp(scc.storyCustomization.bodyColor, self.blackColor, 0.3f + 0.7f * self.darkness);
            }
            return orig(self);
        }

        public static void On_CentipedeGraphics_DrawSprites(On.CentipedeGraphics.orig_DrawSprites orig, CentipedeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (CreatureController.creatureControllers.TryGetValue(self.centipede, out var cc) && cc.isStory(out var scc))
            {
                var hsl = Custom.RGB2HSL(self.ShellColor);
                var color = Custom.HSL2RGB(hsl.x, hsl.y, hsl.z * self.darkness);
                for (int i = 0; i < self.owner.bodyChunks.Length; i++)
                {
                    for (int j = 0; j < (self.centipede.AquaCenti ? 2 : 1); j++)
                    {
                        sLeaser.sprites[self.ShellSprite(i, j)].color = color;
                    }
                }
            }
        }
    }
}
