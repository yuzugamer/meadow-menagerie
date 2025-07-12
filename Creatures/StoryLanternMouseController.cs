using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using UnityEngine;
using MoreSlugcats;
using BepInEx.Configuration;

namespace StoryMenagerie;

public class StoryLanternMouseController : LanternMouseController
{
    public bool lastSpec;
    public bool lastThrow;
    public bool lastGrab;
    public int biteCounter;
    public int lastDirection;
    public int sillyCounter;
    public bool silly;
    public StoryLanternMouseController(LanternMouse mouse, OnlineCreature oc, int playerNumber, SlugcatCustomization customization) : base(mouse, oc, playerNumber, new ExpandedAvatarData(customization))
    {
    }

    public static bool IsEdible(AbstractPhysicalObject obj)
    {
        return obj is DangleFruit.AbstractDangleFruit || (ModManager.DLCShared && obj.type == DLCSharedEnums.AbstractObjectType.GooieDuck);
    }

    public static bool IsEdible(PhysicalObject obj)
    {
        return obj is DangleFruit || (ModManager.DLCShared && obj is GooieDuck);
    }

    public void BiteEdible(bool eu)
    {
        var grabbed = mouse.grasps[0].grabbed;
        if (mouse.grasps[0] != null && grabbed is IPlayerEdible edible && edible.Edible)
        {
            BiteStruggle();
            if (edible.BitesLeft == 1)
            {
                //this.AddFood(edible.FoodPoints, 1f);
            }
            if (grabbed is Creature)
            {
                (grabbed as Creature).SetKillTag(mouse.abstractCreature);
            }
            if (mouse.graphicsModule != null)
            {
                //(mouse.graphicsModule as PlayerGraphics).BiteFly(i);
            }
            HandleBitByPlayer(grabbed, eu);
            return;
        }
    }

    public void HandleBitByPlayer(PhysicalObject grabbed, bool eu)
    {
        if (grabbed is Fly fly)
        {
            fly.bites--;
            if (!fly.dead)
            {
                fly.Die();
            }

            fly.room.PlaySound((fly.bites == 0) ? SoundID.Slugcat_Final_Bite_Fly : SoundID.Slugcat_Bite_Fly, fly.mainBodyChunk);
            fly.mainBodyChunk.MoveFromOutsideMyUpdate(eu, mouse.mainBodyChunk.pos);
            if (fly.bites < 1 && fly.eaten == 0)
            {
                this.AddFood(fly.FoodPoints, 1f);
                mouse.grasps[0].Release();
                fly.eaten = 3;
            }
        } else if (grabbed is DangleFruit fruit)
        {
            fruit.bites--;
            fruit.room.PlaySound((fruit.bites == 0) ? SoundID.Slugcat_Eat_Dangle_Fruit : SoundID.Slugcat_Bite_Dangle_Fruit, fruit.firstChunk);
            fruit.firstChunk.MoveFromOutsideMyUpdate(eu, mouse.mainBodyChunk.pos);
            if (fruit.bites < 1)
            {
                this.AddFood(fruit.FoodPoints, 1f);
                mouse.grasps[0].Release();
                fruit.Destroy();
            }
        } else if (ModManager.DLCShared && grabbed is GooieDuck duck)
        {
            if (duck.bites == 6)
            {
                duck.room.PlaySound(DLCSharedEnums.SharedSoundID.Duck_Pop, mouse.mainBodyChunk, false, 1f, 0.5f + UnityEngine.Random.value * 0.5f);
                for (int i = 0; i < 3; i++)
                {
                    duck.room.AddObject(new WaterDrip(duck.firstChunk.pos, Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(4f, 21f, UnityEngine.Random.value), false));
                }
            }
            duck.bites--;
            duck.room.PlaySound((duck.bites != 0) ? SoundID.Slugcat_Bite_Dangle_Fruit : SoundID.Slugcat_Eat_Dangle_Fruit, duck.firstChunk);
            duck.firstChunk.MoveFromOutsideMyUpdate(eu, mouse.mainBodyChunk.pos);
            if (duck.bites < 1)
            {
                this.AddFood(duck.FoodPoints, 1f);
                mouse.grasps[0].Release();
                duck.Destroy();
            }
        } else
        {
            var food = creature.RunBiteAction(0, grabbed);
            this.AddFood(food, 1f);
        }
    }

    public override void ConsciousUpdate()
    {
        base.ConsciousUpdate();
        if (input[0].spec)
        {
            if (!lastSpec)
            {
                mouse.Squeak(UnityEngine.Random.value);
            }
            lastSpec = true;
        }
        else
        {
            lastSpec = false;
        }

        if (mouse.grasps[0] == null)
        {
            var candidate = this.PickupCandidate(0f, 30f);
            if (candidate != null && candidate != pickUpCandidate && candidate is not Spear)
            {
                if (pickUpCandidate is PlayerCarryableItem carryable)
                {
                    carryable.Blink();
                }
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
                if (mouse.grasps[0] == null)
                {
                    foreach (var layer in mouse.room.physicalObjects)
                    {
                        foreach (var obj in layer)
                        {

                            var apo = obj.abstractPhysicalObject;
                            if (apo.rippleLayer == mouse.abstractCreature.rippleLayer || apo.rippleBothSides || mouse.abstractCreature.rippleBothSides)
                            {
                                if (obj != null && obj is not Spear && (!(apo is AbstractCreature acrit) || mouse.AI.DynamicRelationship(acrit).type == CreatureTemplate.Relationship.Type.Eats) && Custom.DistLess(mouse.mainBodyChunk.pos, obj.firstChunk.pos, 30f) && mouse.room.VisualContact(mouse.mainBodyChunk.pos, obj.firstChunk.pos) && obj.grabbedBy.Count < 1 && (!(apo is AbstractSpear) || !(apo as AbstractSpear).stuckInWall))
                                {
                                    if (obj is Weapon)
                                    {
                                        if (!((obj as Weapon).mode != Weapon.Mode.Thrown))
                                        {
                                            continue;
                                        }
                                    }
                                    while (obj.grabbedBy.Count > 0)
                                    {
                                        obj.grabbedBy[0].Release();
                                    }
                                    apo.LoseAllStuckObjects();
                                    mouse.Grab(obj, 0, 0, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 1f, true, obj.TotalMass < mouse.TotalMass);
                                }
                            }
                        }
                    }
                }
                else if (input[0].y == -1)
                {
                    mouse.LoseAllGrasps();
                }
            }
            lastGrab = true;
        }
        else
        {
            lastGrab = false;
        }

        if (input[0].jmp && mouse.ropeAttatchedPos != null)
        {
            mouse.DetatchRope();
            (mouse.graphicsModule as MouseGraphics).charging = 0f;
        }

        if (input[0].thrw)
        {
            if (!lastThrow)
            {
                int i = 0;
                while (i < 7)
                {
                    if (mouse.room.GetTile(new IntVector2(mouse.abstractCreature.pos.Tile.x, mouse.abstractCreature.pos.Tile.y + i)).horizontalBeam)
                    {
                        mouse.AttatchRope(new IntVector2(mouse.abstractCreature.pos.Tile.x, mouse.abstractCreature.pos.Tile.y + i));
                        break;
                    }
                    if (mouse.room.GetTile(new IntVector2(mouse.abstractCreature.pos.Tile.x, mouse.abstractCreature.pos.Tile.y + i)).Solid)
                    {
                        if (i != 0)
                        {
                            mouse.AttatchRope(new IntVector2(mouse.abstractCreature.pos.Tile.x, mouse.abstractCreature.pos.Tile.y + (i - 1)));
                            break;
                        }
                        break;
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            lastThrow = true;
        }
        else
        {
            lastThrow = false;
        }

        if (mouse.ropeAttatchedPos == null)
        {
            var inAir = false;
            if (!silly)
            {
                inAir = mouse.bodyChunks.All(chunk => chunk.onSlope != 0) && !mouse.IsTileSolid(0, 0, 0) && !mouse.IsTileSolid(0, 0, -1) && !mouse.IsTileSolid(1, 0, 0) && !mouse.IsTileSolid(1, 0, -1);
            }
            if (input[0].x != 0 && input[0].x == -lastDirection && sillyCounter == 2)
            {
                if (!silly)
                {
                    mouse.bodyChunks[1].vel.x = 0;
                    mouse.bodyChunks[0].vel.x = 0;
                }
                silly = true;
            }

            else if (input[0].x == lastDirection && sillyCounter < 3)
            {
                sillyCounter++;
            }
            else
            {
                silly = false;
                sillyCounter = 0;
            }
            if (silly)
            {
                StoryMenagerie.Debug("lampster flying");
                // recalibrate rotation
                var dir = Custom.DirVec(mouse.bodyChunks[0].pos, mouse.bodyChunks[1].pos);
                //mouse.bodyChunks[0].vel.y = mouse.bodyChunks[1].vel.y;
                //mouse.bodyChunks[1].vel.x = -dir.x * 2f;
                //mouse.bodyChunks[1].vel.y = mouse.bodyChunks[0].vel.y + ((1f - dir.y) * 2f);
                //mouse.bodyChunks[1].vel.x += -input[0].x;
                //mouse.bodyChunks[0].vel.x += input[0].x;
                //mouse.bodyChunks[0].vel.y += 5f;
                mouse.bodyChunks[1].vel.x = 0f;
                mouse.bodyChunks[1].vel.y = mouse.bodyChunks[0].vel.y;
                mouse.bodyChunks[0].vel.x = dir.x + input[0].x;
                mouse.bodyChunks[0].vel.y += 5.25f;
                foreach (var chunk in mouse.bodyChunks)
                {
                    //chunk.vel.y += 2.9f;
                }
                //var pos = mouse.mainBodyChunk.pos + new Vector2(input[0].x * 5f, 0f);
                //var dir = Custom.DirVec(mouse.mainBodyChunk.pos, pos);
                //if (mouse.graphicsModule is MouseGraphics mouseGraphics) mouseGraphics.lookDir = (mouse.mainBodyChunk.pos - pos) / 500f;
                //mouse.bodyChunks[0].vel += 0.25f * dir;
                //mouse.bodyChunks[1].vel -= 0.25f * dir;
                mouse.sitting = true;
            }
        }
        else
        {
            silly = false;
            sillyCounter = 0;
        }

        lastDirection = input[0].x;
    }


    public override void Update(bool eu)
    {
        base.Update(eu);
        if (mouse.grasps[0] != null)
        {
            CarryObject(eu);
        }
        if (mouse.Consious && input[0].pckp && input[0].x == 0 && input[0].y == 0)
        {
            if (mouse.grasps != null)
            {
                biteCounter++;
                if (biteCounter > 20)
                {
                    BiteEdible(eu);
                    biteCounter = 0;
                }
            }
        }
        else
        {
            biteCounter = 0;
        }
    }

    public void CarryObject(bool eu)
    {
        if (mouse.grasps[0].discontinued || mouse.grasps[0].grabbed.slatedForDeletetion || mouse.grasps[0].grabbed.room != mouse.room || !Custom.DistLess(mouse.grasps[0].grabbedChunk.pos, mouse.mainBodyChunk.pos, 100f) || !mouse.Consious)
        {
            mouse.LoseAllGrasps();
            return;
        }
        var pos = Vector2.Lerp(mouse.bodyChunks[0].pos, mouse.bodyChunks[1].pos, 0.55f);
        var grabbed = mouse.grasps[0].grabbed;
        var vel = grabbed.bodyChunks[mouse.grasps[0].chunkGrabbed].vel - mouse.mainBodyChunk.vel;
        float num = grabbed.bodyChunks[mouse.grasps[0].chunkGrabbed].mass;
        if (num <= mouse.mainBodyChunk.mass / 2f)
        {
            num /= 2f;
        }
        else if (num <= mouse.mainBodyChunk.mass / 10f)
        {
        }
        grabbed.bodyChunks[mouse.grasps[0].chunkGrabbed].vel = mouse.mainBodyChunk.vel;
        if (grabbed is Weapon weapon && mouse.graphicsModule != null)
        {
            //weapon.setRotation = new Vector2?(Custom.PerpendicularVector(mouse.mainBodyChunk.pos, (mouse.graphicsModule as MouseGraphics).head.pos));
        }
        if (mouse.enteringShortCut == null && (vel.magnitude * grabbed.bodyChunks[mouse.grasps[0].chunkGrabbed].mass > 30f || !Custom.DistLess(pos, grabbed.bodyChunks[mouse.grasps[0].chunkGrabbed].pos, 70f + grabbed.bodyChunks[mouse.grasps[0].chunkGrabbed].rad)))
        {
            mouse.LoseAllGrasps();
        }
        else
        {
            grabbed.bodyChunks[mouse.grasps[0].chunkGrabbed].MoveFromOutsideMyUpdate(eu, pos);
        }
        if (mouse.grasps[0] != null)
        {
            for (int i = 0; i < 2; i++)
            {
                mouse.grasps[0].grabbed.PushOutOf(mouse.bodyChunks[i].pos, mouse.bodyChunks[i].rad, mouse.grasps[0].chunkGrabbed);
            }
        }
    }

    public void BiteStruggle()
    {
        var graphics = mouse.graphicsModule as MouseGraphics;
        if (graphics != null)
        {
            graphics.head.vel += Custom.DirVec(graphics.head.pos, mouse.grasps[0].grabbedChunk.pos);
            graphics.lookDir = Custom.DirVec(graphics.head.pos, mouse.grasps[0].grabbedChunk.pos);
            if (graphics.blink < 5)
            {
                graphics.blink = 5;
            }
        }
    }

    public static void ApplyHooks()
    {

    }


}
