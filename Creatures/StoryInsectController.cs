using MonoMod.RuntimeDetour;
using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RWCustom;
using MoreSlugcats;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace StoryMenagerie;

public class StoryInsectController : CreatureController
{
    public BackgroundBugWrapper wrapper;
    public StoryInsectController(BackgroundBugWrapper bug, OnlineCreature oc, int playerNumber, SlugcatCustomization customization) : base(bug, oc, playerNumber, new ExpandedAvatarData(customization))
    {
        this.wrapper = bug;
        this.story().storyCustomization = customization;
        this.customization = new ExpandedAvatarData(customization);
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (wrapper != null && wrapper.bug != null && wrapper.bug is FireFly bug)
        {
            bug.vel *= 0.95f;
            bug.vel.x = bug.vel.x + bug.dir.x * 0.3f;
            bug.vel.y = bug.vel.y + bug.dir.y * 0.2f;
            bug.dir = Vector2.Lerp(bug.dir, Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Pow(UnityEngine.Random.value, 0.75f), 0.4f).normalized;
            if (bug.wantToBurrow)
            {
                bug.dir = Vector2.Lerp(bug.dir, new Vector2(0f, -1f), 0.1f);
            }
            else if (bug.OutOfBounds)
            {
                bug.dir = Vector2.Lerp(bug.dir, Custom.DirVec(bug.pos, bug.mySwarm.placedObject.pos), Mathf.InverseLerp(bug.mySwarm.insectGroupData.Rad, bug.mySwarm.insectGroupData.Rad + 100f, Vector2.Distance(bug.pos, bug.mySwarm.placedObject.pos)));
            }
            float num = bug.TileScore(bug.room.GetTilePosition(bug.pos));
            IntVector2 intVector = new IntVector2(0, 0);
            bug.vel += input[0].analogueDir.normalized * 0.4f;
            if (bug.room.PointSubmerged(bug.pos))
            {
                bug.pos.y = bug.room.FloatWaterLevel(bug.pos);
            }
            bug.sin += 1f / Mathf.Lerp(20f, 80f, UnityEngine.Random.value);
            if (bug.room.Darkness(bug.pos) > 0f)
            {
                if (bug.light == null)
                {
                    bug.light = new LightSource(bug.pos, false, bug.col, bug);
                    bug.light.noGameplayImpact = ModManager.MMF;
                    bug.room.AddObject(bug.light);
                }
                bug.light.setPos = new Vector2?(bug.pos);
                bug.light.setAlpha = new float?(0.15f - 0.1f * Mathf.Sin(bug.sin * 3.1415927f * 2f));
                bug.light.setRad = new float?(60f + 20f * Mathf.Sin(bug.sin * 3.1415927f * 2f));
            }
            else if (bug.light != null)
            {
                bug.light.Destroy();
                bug.light = null;
            }
            bug.lastLastPos = bug.lastPos;
        }
    }

    public override void ConsciousUpdate()
    {
        base.ConsciousUpdate();
    }

    public override WorldCoordinate CurrentPathfindingPosition => base.CurrentPathfindingPosition;

    public override void OnCall() { }

    public override void Resting() { } 

    public override void Moving(float magnitude) { }

    public override void PointImpl(Vector2 dir) { }

    public override void LookImpl(Vector2 pos) { }

    public static void ApplyHooks()
    {
        IL.FireFly.Update += IL_FireFly_Update;
    }

    public static void On_BigEelAI_Update(On.BigEelAI.orig_Update orig, BigEelAI self)
    {
        if (creatureControllers.TryGetValue(self.creature.realizedCreature, out var cc))
        {
            cc.AIUpdate(self);
        } else
        {
            //StoryMenagerie.Debug("ai update not creature controller!");
            orig(self);
        }
    }

    public static bool IsBugController(CosmeticInsect self)
    {
        if (BackgroundBugWrapper.bugWrappers.TryGetValue(self, out var bug) && CreatureController.creatureControllers.TryGetValue(bug, out var cc))
        {
            cc.Update(!self.evenUpdate);
            return true;
        }
        return false;
    }

    public static void IL_FireFly_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(IsBugController);
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Brtrue, skip);
            c.GotoNext(x => x.MatchCallOrCallvirt<CosmeticInsect>(nameof(CosmeticInsect.Update)));
            c.MarkLabel(skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }
}
