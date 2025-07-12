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
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine.Rendering;
using System.Security.AccessControl;

namespace StoryMenagerie;

public class StoryJetFishController : CreatureController
{
    public bool forceMove;
    public JetFish fish;
    public bool lastJump;
    public bool waterJet;
    public int jumpTime;
    public bool lastGrab;
    public int biteCounter;
    public bool prevPointing;
    public Vector2? hoverPos;
    public StoryJetFishController(JetFish fish, OnlineCreature oc, int playerNumber, SlugcatCustomization customization) : base(fish, oc, playerNumber, new ExpandedAvatarData(customization))
    {
        this.fish = fish;
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (fish.Consious && input[0].pckp && fish.grasps != null && fish.grasps[0] != null && input[0].x == 0 && input[0].y == 0)
        {
            biteCounter++;
            if (biteCounter > 20)
            {
                BiteEdible(eu);
                biteCounter = 0;
            }
        }
        else
        {
            biteCounter = 0;
        }
    }

    public override void ConsciousUpdate()
    {
        prevPointing = pointing;
        base.ConsciousUpdate();
        if (!pointing) hoverPos = null;
        if (fish.grasps[0] == null)
        {
            PhysicalObject candidate = this.PickupCandidate(0f, 30f);
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

        if (input[0].pckp)
        {
            if (!lastGrab)
            {
                if (fish.grasps[0] == null)
                {
                    if (pickUpCandidate != null && (pickUpCandidate is not Creature crit || crit.Template.smallCreature))
                    {
                        fish.Grab(pickUpCandidate, 0, 0, Creature.Grasp.Shareability.CanNotShare, 1f, true, true);
                    }
                }
                else if (input[0].y < 0)
                {
                    fish.LoseAllGrasps();
                }
            }
            lastGrab = true;
        }
        else
        {
            lastGrab = false;
        }
        Creature grabber = null;
        Player.InputPackage[] grabberInput = input;
        foreach (var grasp in fish.grabbedBy)
        {
            if (grasp != null && grasp.grabber != null)
            {
                if (CreatureController.creatureControllers.TryGetValue(grasp.grabber, out var controller))
                {
                    grabber = grasp.grabber;
                    grabberInput = controller.input;
                    break;
                } else if (grasp.grabber is Player scug)
                {
                    grabber = scug;
                    grabberInput = scug.input;
                    break;
                }
            }
        }
        var connection = (fish.AI.pathFinder as FishPather).FollowPath(fish.room.GetWorldCoordinate(fish.mainBodyChunk.pos), true);
        if (input[0].x != 0 || input[0].y != 0)
        {
            //connection = new MovementConnection(type, this.room.GetWorldCoordinate(base.mainBodyChunk.pos), this.room.GetWorldCoordinate(base.mainBodyChunk.pos + new Vector2((float)this.inputWithDiagonals.Value.x, (float)this.inputWithDiagonals.Value.y) * 40f), 2);
        }
        if (connection != default(MovementConnection))
        {
            var destinationCoord = connection.destinationCoord;
            for (int i = 0; i < 4; i++)
            {
                var connection2 = (fish.AI.pathFinder as FishPather).FollowPath(destinationCoord, false);
                if (connection2 != default(MovementConnection) && connection2.destinationCoord.TileDefined && connection2.destinationCoord.room == fish.room.abstractRoom.index && fish.room.VisualContact(connection.destinationCoord.Tile, connection2.DestTile))
                {
                    destinationCoord = connection2.destinationCoord;
                    if (fish.room.aimap.getAItile(connection2.DestTile).narrowSpace)
                    {
                        fish.slowDownForPrecision += 0.3f;
                        break;
                    }
                }
            }
        }
        fish.slowDownForPrecision = Mathf.Clamp(fish.slowDownForPrecision - 0.1f, 0f, 1f);
        waterJet = false;
        if (jumpTime > 0) jumpTime--;
        if (grabber != null)
        {
            if (grabber.Submersion >= 0.4f)
            {
                grabber.GoThroughFloors = true;
            }
            grabber.GoThroughFloors = true;
            foreach (var chunk in fish.bodyChunks)
            {
                chunk.terrainSqueeze = 0.5f;
            }
            if (grabberInput[0].jmp && !grabberInput[1].jmp && (fish.bodyChunks[0].ContactPoint.y < 0 || fish.bodyChunks[1].ContactPoint.y < 0))
            {
                BodyChunk bodyChunk = fish.bodyChunks[1];
                bodyChunk.vel.y += 8f;
                fish.room.PlaySound(SoundID.Jet_Fish_On_Land_Jump, fish.mainBodyChunk);
            }
            fish.swimDir = (grabberInput[0].analogueDir).normalized;
            float speed = 1.8f;
            var weightDiff = grabber.TotalMass / fish.TotalMass;
            if (weightDiff > 1f)
            {
                speed = 2f;
            }
            else if (weightDiff >= 2f)
            {
                speed = 3f;
            }
            speed = Mathf.Lerp(speed, 2.2f, 1f);
            speed = Mathf.Lerp(1f, speed, fish.Submersion);
            speed = Mathf.Lerp(speed, 1.4f, fish.diveSpeed);
            speed = Mathf.Lerp(speed, 0.9f, fish.slowDownForPrecision);
            fish.Swim(speed);
        }
        else
        {
            fish.swimDir = (input[0].analogueDir).normalized;
            var grounded = fish.Submersion < 0.4f && (fish.bodyChunks[0].ContactPoint.y < 0 || fish.bodyChunks[1].ContactPoint.y < 0);
            if (grounded || fish.bodyChunks[0].contactPoint.x != 0 || fish.bodyChunks[1].contactPoint.x != 0)
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
                    fish.mainBodyChunk.vel += Vector2.Lerp(fish.swimDir, Custom.DegToVec(ang), 0.6f).normalized * 26f;// * Mathf.Lerp(6f, 26f, UnityEngine.Random.value);
                    fish.room.PlaySound(SoundID.Jet_Fish_On_Land_Jump, fish.mainBodyChunk);
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
                fish.Swim(Mathf.Lerp(1.8f, 0.9f, fish.slowDownForPrecision));
            }
            else
            {
                fish.Swim(0.05f);
            }
        }
    }

    public override WorldCoordinate CurrentPathfindingPosition
    {
        get
        {
            if (!forceMove && Custom.DistLess(creature.coord, fish.AI.pathFinder.destination, 3))
            {
                return fish.AI.pathFinder.destination;
            }
            return base.CurrentPathfindingPosition;
        }
    }

    public override void OnCall()
    {
    }

    public override void Resting()
    {
        fish.AI.behavior = JetFishAI.Behavior.Idle;
        forceMove = false;
    }

    public override void Moving(float magnitude)
    {
        fish.AI.behavior = JetFishAI.Behavior.Flee;
        forceMove = true;
    }


    public override void PointImpl(Vector2 dir)
    {
        if (fish.graphicsModule is JetFishGraphics graphics)
        {
            //graphics.bodyParts[0].vel *= 0.6f; // airbreak
            //graphics.bodyParts[0].vel.y += 0.9f; // negate gravity;
            //graphics.bodyParts[0].vel += 5f * dir;
        }
        if (hoverPos == null)
        {
            if (fish.Submersion < 0.4f && (fish.bodyChunks[0].ContactPoint.y < 0 || fish.bodyChunks[1].ContactPoint.y < 0))
            {
                hoverPos = fish.bodyChunks[0].pos + new Vector2(0, 25f);
            }
            else
            {
                hoverPos = fish.bodyChunks[0].pos;
            }
        }
        fish.mainBodyChunk.vel *= Custom.LerpMap(fish.mainBodyChunk.vel.magnitude, 1f, 6f, 0.999f, 0.9f);
        fish.mainBodyChunk.vel += Vector2.ClampMagnitude(hoverPos.Value - fish.mainBodyChunk.pos, 100f) / 100f * 0.4f;
        fish.bodyChunks[1].vel *= 0f;
        //fish.bodyChunks[0].vel += 5f * dir;
        fish.bodyChunks[1].vel -= 5f * dir;
        fish.bodyChunks[0].pos.y += creature.gravity;
        fish.bodyChunks[1].pos.y += creature.gravity;
        fish.swimSpeed = 0f;
    }

    public override void LookImpl(Vector2 pos)
    {
        fish.swimDir = (pos - creature.DangerPos).normalized;
    }

    public void BiteEdible(bool eu)
    {
        if (fish.grasps[0] == null || fish.grasps[0].grabbed == null)
        {
            return;
        }
        var grabbed = fish.grasps[0].grabbed;
        if ((grabbed is IPlayerEdible edible && edible.Edible && fish.abstractCreature.CanEat(grabbed)) || (grabbed is Creature crit && fish.AI.DynamicRelationship((crit).abstractCreature).type == CreatureTemplate.Relationship.Type.Eats))
        {
            fish.mainBodyChunk.vel += Vector2.Lerp(Custom.RNV(), new Vector2(0f, 1f), 0.4f) * Mathf.Lerp(1f, 5f, UnityEngine.Random.value);
            if (grabbed is Creature)
            {
                (grabbed as Creature).SetKillTag(fish.abstractCreature);
            }
            this.FoodBitByPlayer(grabbed, 0, eu);
            return;
        }
    }

    public static void ApplyHooks()
    {
        On.JetFish.Update += On_JetFish_Update;
        On.JetFish.Act += On_JetFish_Act;
        On.JetFishAI.Update += On_JetFishAI_Update;
        On.JetFish.Collide += On_JetFish_Collide;
        IL.JetFish.Swim += IL_JetFish_Swim;
    }

    public static void On_JetFishAI_Update(On.JetFishAI.orig_Update orig, JetFishAI self)
    {
        if (creatureControllers.TryGetValue(self.creature.realizedCreature, out var cc))
        {
            //cc.AIUpdate(self);
        } else
        {
            //StoryMenagerie.Debug("ai update not creature controller!");
            orig(self);
        }
    }

    public static void On_JetFish_Act(On.JetFish.orig_Act orig, JetFish self)
    {
        if (creatureControllers.TryGetValue(self, out var cc))
        {
            //cc.ConsciousUpdate();
            //self.abstractCreature.controlled = true;
            //orig(self);
            //self.abstractCreature.controlled = false;
        }
        else
        {
            //StoryMenagerie.Debug("fish acting!");
            orig(self);
        }
    }

    public static void On_JetFish_Update(On.JetFish.orig_Update orig, JetFish self, bool eu)
    {
        if (creatureControllers.TryGetValue(self, out var cc))
        {
            //StoryMenagerie.Debug("fish update");
            cc.Update(eu);
            //self.abstractCreature.controlled = true;
            orig(self, eu);
            if (self.Consious)
            {
                //StoryMenagerie.Debug("fish conscious update");
                cc.ConsciousUpdate();
            } else if (cc is StoryJetFishController fish)
            {
                fish.jumpTime = 0;
                if (self.grasps[0] != null)
                {
                    self.LoseAllGrasps();
                }
            }
            return;
            //self.abstractCreature.controlled = false;
        }
        orig(self, eu);
    }

    public static void On_JetFish_Collide(On.JetFish.orig_Collide orig, JetFish self, PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        if (creatureControllers.TryGetValue(self, out var cc))
        {
            // copy pasted, no shame
            if (cc.input[0].pckp)
            {
                var chunk = self.bodyChunks[myChunk];
                var hitChunk = otherObject.bodyChunks[otherChunk];
                var dist = Vector2.Distance(chunk.vel, hitChunk.vel);
                if (dist > 12f && otherObject is Creature crit)
                {
                    crit.Violence(chunk, new Vector2?(chunk.vel * chunk.mass), hitChunk, null, Creature.DamageType.Blunt, 0.1f, 10f);
                    self.room.PlaySound(SoundID.Jet_Fish_Ram_Creature, self.mainBodyChunk);
                    var pos = chunk.pos + Custom.DirVec(chunk.pos, hitChunk.pos) * chunk.rad;
                    for (int i = 0; i < 5; i++)
                    {
                        self.room.AddObject(new Bubble(pos, Custom.RNV() * 18f * UnityEngine.Random.value, false, false));
                    }
                }
            }
            // risky?
            return;
        }
        orig(self, otherObject, myChunk, otherChunk);
    }

    public static bool JetFishControllerSwim(JetFish self)
    {
        if (creatureControllers.TryGetValue(self, out var cc) && cc is StoryJetFishController fish)
        {
            if (self.bodyChunks[1].submersion >= 0.5)
            {
                self.availableWater = 1f;
            }
            if (fish.waterJet && self.availableWater > 0f)
            {
                self.jetWater = Mathf.Clamp(self.jetWater + 0.033333335f, 0f, 1f);
                self.availableWater -= 0.005f;
            }
            else
            {
                self.jetWater = Mathf.Clamp(self.jetWater - 0.04f, 0f, 1f);
            }
            return true;
        }
        return false;
    }

    public static void IL_JetFish_Swim(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt<Mathf>(nameof(Mathf.Pow)),
                x => x.MatchCallOrCallvirt<Mathf>(nameof(Mathf.Lerp)),
                x => x.MatchAdd(),
                x => x.MatchLdcR4(out var _),
                x => x.MatchLdcR4(out var _),
                x => x.MatchCallOrCallvirt<Mathf>(nameof(Mathf.Clamp)),
                x => x.MatchStfld<JetFish>(nameof(JetFish.jetWater))
            );
            var skip = c.DefineLabel();
            c.MarkLabel(skip);
            c.GotoPrev(
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0)
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(JetFishControllerSwim);
            c.Emit(OpCodes.Brtrue, skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }
}
