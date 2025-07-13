using RainMeadow;
using MonoMod.Cil;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using System;
using UnityEngine;

namespace StoryMenagerie.Creatures
{
    public class EggbugController : RainMeadow.EggbugController
    {
        public static void ApplyHooks()
        {
            new Hook(typeof(RainMeadow.EggbugController).GetMethod(nameof(RainMeadow.EggbugController.EggBugGraphics_ApplyPalette), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), On_EggBugController_EggBugGraphics_ApplyPalette);
        }

        public EggbugController(EggBug creature, OnlineCreature oc, int playerNumber, CreatureCustomization customization) : base(creature, oc, playerNumber, new ExpandedAvatarData(customization))
        {
        }

        // the sequel
        public static void On_EggBugController_EggBugGraphics_ApplyPalette(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.AfterLabel,
                    i => i.MatchStfld<EggBugGraphics>("blackColor")
                    );

                c.GotoPrev(MoveType.After,
                    i => i.MatchStloc(out _));

                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloca, 0);
                c.Emit(OpCodes.Ldloca, 1);
                c.EmitDelegate((EggBugGraphics self, ref Color c1, ref Color c2) =>
                {
                    if (creatureControllers.TryGetValue(self.bug, out var p))
                    {
                        var diff = c1 - c2;
                        p.customization.ModifyBodyColor(ref c1);
                        p.customization.ModifyBodyColor(ref c2);
                        c2 += diff;
                    }
                });

                // egg colors
                c.GotoNext(i => i.MatchCallOrCallvirt<EggBugGraphics>("EggColors"));
                c.GotoNext(MoveType.After, i => i.MatchStfld<EggBugGraphics>("eggColors"));
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((EggBugGraphics self) =>
                {
                    if (creatureControllers.TryGetValue(self.bug, out var p))
                    {
                        p.customization.ModifyBodyColor(ref self.eggColors[0]);
                        p.customization.ModifyBodyColor(ref self.eggColors[1]);
                        p.customization.ModifyBodyColor(ref self.eggColors[2]);
                    }
                });
            }
            catch (Exception ex)
            {
                StoryMenagerie.LogError(ex);
            }
        }
    }
}
