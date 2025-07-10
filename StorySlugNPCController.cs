using MoreSlugcats;
using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace StoryMenagerie;

public class StorySlugNPCController : CreatureController
{
    public Player pup;
    public StorySlugNPCController(Player pup, OnlineCreature oc, int playerNumber, SlugcatCustomization customization) : base(pup, oc, playerNumber, new ExpandedAvatarData(customization))
    {
        this.pup = pup;
        this.story().storyCustomization = customization;
        this.customization = new ExpandedAvatarData(customization);
    }

    public override void OnCall() { }
    public override void Resting() { }
    public override void Moving(float magnitude) { }
    public override void LookImpl(Vector2 pos) { }

    public static void ApplyHooks()
    {
        On.Player.Update += On_Player_Update;
        On.MoreSlugcats.SlugNPCAI.Update += On_SlugNPCAI_Update;
    }

    public static void On_Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (self.isNPC && CreatureController.creatureControllers.TryGetValue(self, out var cc))
        {
            self.abstractCreature.controlled = true;
            cc.Update(eu);
            self.inputWithDiagonals = cc.input[0];
            self.inputWithoutDiagonals = cc.input[0];
            self.lastInputWithDiagonals = cc.input[1];
            self.lastInputWithoutDiagonals = cc.input[1];
            orig(self, eu);
            if (self.Consious)
            {
                cc.ConsciousUpdate();
            }
            self.abstractCreature.controlled = false;
            return;
        }
        orig(self, eu);
    }

    public static void On_SlugNPCAI_Update(On.MoreSlugcats.SlugNPCAI.orig_Update orig, SlugNPCAI self)
    {
        if (CreatureController.creatureControllers.TryGetValue(self.cat, out var cc))
        {
            cc.AIUpdate(self);
            return;
        }
        orig(self);
    }
}
