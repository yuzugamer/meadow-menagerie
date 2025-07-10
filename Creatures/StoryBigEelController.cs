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

public class StoryBigEelController : CreatureController
{
    public bool forceMove;
    public BigEel eel;
    public bool lastJump;
    public bool waterJet;
    public int jumpTime;
    public bool lastGrab;
    public int biteCounter;
    public StoryBigEelController(BigEel eel, OnlineCreature oc, int playerNumber, SlugcatCustomization customization) : base(eel, oc, playerNumber, new ExpandedAvatarData(customization))
    {
        this.eel = eel;
        this.story().storyCustomization = customization;
        this.customization = new ExpandedAvatarData(customization);
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (!eel.Consious)
        {
            biteCounter = 0;
        }
    }

    public override void ConsciousUpdate()
    {
        base.ConsciousUpdate();

        // coppyyyyy pasteeedd from Act
        float charge = eel.jawCharge;
        if (eel.jawCharge == 0f && eel.jawChargeFatigue > 0f)
        {
            eel.jawChargeFatigue = Mathf.Max(eel.jawChargeFatigue - 0.008333334f, 0f);
        }
        else if (eel.jawCharge > 0.3f || (eel.jawChargeFatigue < 1f && input[0].pckp))
        {
            eel.jawCharge += 0.008333334f;
        }
        else
        {
            eel.jawCharge = Mathf.Max(eel.jawCharge - 0.008333334f, 0f);
        }
        if (charge == 0f && eel.jawCharge > 0f)
        {
            eel.room.PlaySound(SoundID.Leviathan_Deploy_Jaws, eel.mainBodyChunk);
        }
        else if (eel.jawCharge >= 0.25f && charge < 0.25f)
        {
            eel.room.PlaySound(SoundID.Leviathan_Jaws_Armed, eel.mainBodyChunk);
        }
        eel.snapFrame = false;
        if (eel.jawCharge > 0.3f && charge <= 0.3f && !input[0].pckp)
        {
            eel.jawCharge = 0.3f;
            eel.jawChargeFatigue += 0.025f;
        }
        if (charge <= 0.35 && eel.jawCharge > 0.35f)
        {
            eel.JawsSnap();
            eel.jawChargeFatigue = 1f;
        }
        if (eel.jawCharge >= 1f)
        {
            eel.jawCharge = 0f;
            eel.Swallow();
        }
        var pos = eel.mainBodyChunk.pos;
        var dir = Custom.DirVec(eel.bodyChunks[1].pos, eel.mainBodyChunk.pos);
        for (int i = 0; i < eel.clampedObjects.Count; i++)
        {
            var moveTo = pos + dir * (eel.clampedObjects[i].distance - Mathf.InverseLerp(0.4f, 0.7f, eel.jawCharge) * 60f - Mathf.InverseLerp(0.7f, 1f, eel.jawCharge) * 40f);
            eel.clampedObjects[i].chunk.MoveFromOutsideMyUpdate(eel.evenUpdate, moveTo);
            if (eel.jawCharge > 0.6f)
            {
                for (int j = 0; j < eel.clampedObjects[i].chunk.owner.bodyChunks.Length; j++)
                {
                    eel.clampedObjects[i].chunk.owner.bodyChunks[j].vel *= 1f - Mathf.InverseLerp(0.6f, 0.8f, eel.jawCharge);
                    eel.clampedObjects[i].chunk.owner.bodyChunks[j].MoveFromOutsideMyUpdate(eel.evenUpdate, Vector2.Lerp(eel.clampedObjects[i].chunk.owner.bodyChunks[j].pos, moveTo, Mathf.InverseLerp(0.6f, 0.8f, eel.jawCharge)));
                }
                if (eel.clampedObjects[i].chunk.owner.graphicsModule != null && eel.clampedObjects[i].chunk.owner.graphicsModule.bodyParts != null)
                {
                    for (int k = 0; k < eel.clampedObjects[i].chunk.owner.graphicsModule.bodyParts.Length; k++)
                    {
                        eel.clampedObjects[i].chunk.owner.graphicsModule.bodyParts[k].vel *= 1f - Mathf.InverseLerp(0.6f, 0.8f, eel.jawCharge);
                        eel.clampedObjects[i].chunk.owner.graphicsModule.bodyParts[k].pos = Vector2.Lerp(eel.clampedObjects[i].chunk.owner.graphicsModule.bodyParts[k].pos, moveTo, Mathf.InverseLerp(0.6f, 0.8f, eel.jawCharge));
                    }
                }
            }
        }

        if (input[0].pckp)
        {
            lastGrab = true;
        }
        else
        {
            biteCounter = 0;
            lastGrab = false;
        }
        //var connection = (eel.AI.pathFinder as FishPather).FollowPath(eel.room.GetWorldCoordinate(eel.mainBodyChunk.pos), true);
        waterJet = false;
        if (jumpTime > 0) jumpTime--;
        eel.swimDir = (input[0].analogueDir).normalized;
        var grounded = eel.Submersion < 0.4f && (eel.bodyChunks[0].ContactPoint.y < 0 || eel.bodyChunks[1].ContactPoint.y < 0);
        if (grounded || eel.bodyChunks[0].contactPoint.x != 0 || eel.bodyChunks[1].contactPoint.x != 0)
        {
            jumpTime = 6;
        }
        if (jumpTime > 0)
        {
            if (input[0].jmp && !lastJump)
            {
                float ang = Mathf.Lerp(-45f, 0f, UnityEngine.Random.value);
                if (input[0].x > 0)
                {
                    ang = Mathf.Lerp(0f, 45f, UnityEngine.Random.value);
                }
                eel.mainBodyChunk.vel += Vector2.Lerp(eel.swimDir, Custom.DegToVec(ang), 0.6f).normalized * 26f;// * Mathf.Lerp(6f, 26f, UnityEngine.Random.value);
                eel.room.PlaySound(SoundID.Jet_Fish_On_Land_Jump, eel.mainBodyChunk);
                jumpTime = 0;
                lastJump = true;
            }
        }
        if (!grounded && input[0].jmp)
        {
            waterJet = true;
        }
        if (!input[0].jmp)
        {
            lastJump = false;
        }
        if (input[0].x != 0 || input[0].y != 0)
        {
            float speed = 3f;
            speed = Mathf.Lerp(speed, 2.2f, 1f);
            speed = Mathf.Lerp(1f, speed, eel.Submersion);
            //speed = Mathf.Lerp(speed, 1.4f, eel.diveSpeed);
            //speed = Mathf.Lerp(speed, 0.9f, eel.slowDownForPrecision);
            eel.swimSpeed = speed;
            eel.Swim();
        }
        else
        {
            //eel.Swim(0.05f);
        }
    }

    public override WorldCoordinate CurrentPathfindingPosition
    {
        get
        {
            if (!forceMove && Custom.DistLess(creature.coord, eel.AI.pathFinder.destination, 3))
            {
                return eel.AI.pathFinder.destination;
            }
            return base.CurrentPathfindingPosition;
        }
    }

    public override void OnCall()
    {
    }

    public override void Resting()
    {
        eel.AI.behavior = BigEelAI.Behavior.Idle;
        forceMove = false;
    }

    public override void Moving(float magnitude)
    {
        eel.AI.behavior = BigEelAI.Behavior.Hunt;
        forceMove = true;
    }


    public override void PointImpl(Vector2 dir)
    {
        if (eel.graphicsModule is JetFishGraphics graphics)
        {
            graphics.bodyParts[0].vel *= 0.6f; // airbreak
            graphics.bodyParts[0].vel.y += 0.9f; // negate gravity;
            graphics.bodyParts[0].vel += 5f * dir;
        }
    }

    public override void LookImpl(Vector2 pos)
    {
        eel.swimDir = (pos - creature.DangerPos).normalized;
    }

    public void BiteEdible(bool eu)
    {
        if (eel.grasps[0] == null || eel.grasps[0].grabbed == null)
        {
            return;
        }
        var grabbed = eel.grasps[0].grabbed;
        if ((grabbed is IPlayerEdible edible && edible.Edible && eel.abstractCreature.CanEat(grabbed)) || (grabbed is Creature crit && eel.AI.DynamicRelationship((crit).abstractCreature).type == CreatureTemplate.Relationship.Type.Eats))
        {
            eel.mainBodyChunk.vel += Vector2.Lerp(Custom.RNV(), new Vector2(0f, 1f), 0.4f) * Mathf.Lerp(1f, 5f, UnityEngine.Random.value);
            if (grabbed is Creature)
            {
                (grabbed as Creature).SetKillTag(eel.abstractCreature);
            }
            this.FoodBitByPlayer(grabbed, 0, eu);
            return;
        }
    }

    public static void ApplyHooks()
    {
        On.BigEel.Update += On_BigEel_Update;
        On.BigEel.Act += On_BigEel_Act;
        On.BigEelAI.Update += On_BigEelAI_Update;
        //On.BigEelAI.WantToSnapJaw += On_BigEelAI_WantToSnapJaw;
        //On.BigEelAI.WantToChargeJaw += On_BigEelAI_WantToChargeJaw;
        IL.BigEel.Swim += IL_BigEel_Swim;
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

    public static void On_BigEel_Act(On.BigEel.orig_Act orig, BigEel self, bool eu)
    {
        if (creatureControllers.TryGetValue(self, out var cc))
        {
            cc.ConsciousUpdate();
            //self.abstractCreature.controlled = true;
            //orig(self);
            //self.abstractCreature.controlled = false;
        }
        else
        {
            //StoryMenagerie.Debug("fish acting!");
            orig(self, eu);
        }
    }

    public static void On_BigEel_Update(On.BigEel.orig_Update orig, BigEel self, bool eu)
    {
        if (creatureControllers.TryGetValue(self, out var cc))
        {
            //StoryMenagerie.Debug("fish update");
            cc.Update(eu);
            //self.abstractCreature.controlled = true;
            orig(self, eu);
            //self.abstractCreature.controlled = false;
        }
        orig(self, eu);
    }

    //public static bool On_BigEelAI_WantToSnapJaw(On.BigEelAI.orig_WantToSnapJaw orig, BigEelAI self) => (CreatureController.creatureControllers.TryGetValue(self.eel, out var cc) && cc.input[0].pckp) ? true : orig(self);

    //public static bool On_BigEelAI_WantToChargeJaw(On.BigEelAI.orig_WantToChargeJaw orig, BigEelAI self) => (CreatureController.creatureControllers.TryGetValue(self.eel, out var cc) && cc.input[0].pckp) ? true : orig(self);

    public static void SwimLeft(BigEel self, ref MovementConnection left)
    {
        if (CreatureController.creatureControllers.TryGetValue(self, out var cc))
        {
            if (cc.input[0].x != 0 || cc.input[0].y != 0)
            {
                left = new MovementConnection(MovementConnection.MovementType.Standard, self.room.GetWorldCoordinate(self.mainBodyChunk.pos), self.room.GetWorldCoordinate(self.mainBodyChunk.pos + new Vector2(cc.input[0].x, cc.input[0].y) * 240f), 2);
                if (self.attackPos == null && self.jawCharge > 0f)
                {
                    self.attackPos = new Vector2?(self.mainBodyChunk.pos + new Vector2(cc.input[0].x, cc.input[0].y) * 240f);
                }
            }
        }
    }

    public static void IL_BigEel_Swim(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var loc = 0;
            c.GotoNext(
                x => x.MatchLdloc(out var loc),
                x => x.MatchLdloca(out var _),
                x => x.MatchInitobj<MovementConnection>(),
                x => x.MatchLdloc(out var _),
                x => x.MatchCallOrCallvirt<MovementConnection>("op_Inequality"),
                x => x.MatchBrfalse(out var _)
            );
            // not super sure, but it seems like this "left" var is what controls swim direction
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, loc);
            c.EmitDelegate(SwimLeft);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }
}
