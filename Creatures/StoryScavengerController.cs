using MonoMod.Cil;
using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using static RewiredConsts.Layout;

namespace StoryMenagerie;

public class StoryScavengerController : ScavengerController//, IStoryCreatureController
{
    // all of this code is yoinked because i'm a terrible person
    public bool lastThrow;
    public bool lastGrab;
    public int grabDownTime;
    public int lastGrabPressed;
    public bool lastSpec;
    // poppycorn handled mostly in gamehooks
    public Vector2 handOnExternalFoodSource;
    //public StoryControllerData scd { get; set; }
    //public SlugcatCustomization storyCustomization { get; set; }
    //public int forceSleepCounter { get; set; }
    public bool secretMode;
    public int biteCounter;
    public StoryScavengerController(Scavenger scav, OnlineCreature oc, int playerNumber, SlugcatCustomization customization) : base(scav, oc, playerNumber, new ExpandedAvatarData(customization))
    {
        this.story().storyCustomization = customization;
        secretMode = (OnlineManager.lobby.gameMode as MenagerieGameMode).secretMode;
        // fix: hook onto update, ai update, abstract ai update and temporarily change energy
        // maybe find a way to work around this changing graphics
        //scavenger.abstractCreature.personality.energy = 1f;
    }

    public bool LookForItems()
    {
        // yoinked safari control code
        foreach (var layer in scavenger.room.physicalObjects)
        {
            foreach (var obj in layer)
            {

                var apo = obj.abstractPhysicalObject;
                if (apo.rippleLayer == scavenger.abstractCreature.rippleLayer || apo.rippleBothSides || scavenger.abstractCreature.rippleBothSides)
                {
                    if (obj != null && (apo is not AbstractCreature acrit || acrit.creatureTemplate.smallCreature) && Custom.DistLess(scavenger.mainBodyChunk.pos, obj.firstChunk.pos, 50f) && scavenger.room.VisualContact(scavenger.mainBodyChunk.pos, obj.firstChunk.pos) && obj.grabbedBy.Count < 1 && (((ModManager.DLCShared && scavenger.Elite) || (ModManager.Watcher && (scavenger.Templar || scavenger.Disciple))) || !(apo is AbstractSpear) || !(apo as AbstractSpear).stuckInWall))
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
                        scavenger.PickUpAndPlaceInInventory(obj, false);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public override void ConsciousUpdate()
    {
        base.ConsciousUpdate();

        if (scavenger.grasps.Any(grasp => grasp == null))
        {
            var candidate = this.PickupCandidate(2f, 50f, ModManager.DLCShared && (scavenger.Elite || (ModManager.Watcher && (scavenger.Templar || scavenger.Disciple))));
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
                if (input[0].x < 0)
                {
                    if (scavenger.grasps[0] != null)
                    {
                        scavenger.ReleaseGrasp(0);
                    }
                }
                else
                {
                    var found = LookForItems();
                    //if (grabDownTime == 0)
                    //{
                    //StoryMenagerie.LogDebug("looking for items to pick up");
                    /*// copy pasted this because while LookForItemsToPickUp has this exact code, it has a SafariControlled check before it, and i'm not sure source code without dlc installed contains the code. would just make an il hook for the check otherwise
                    foreach (var layer in scavenger.room.physicalObjects)
                    {
                        foreach (var obj in layer)
                        {
                            if (obj.abstractPhysicalObject.rippleLayer == scavenger.abstractCreature.rippleLayer || obj.abstractPhysicalObject.rippleBothSides || scavenger.abstractCreature.rippleBothSides)
                            {
                                var apo = obj.abstractPhysicalObject;
                                if (obj != null && !(apo is AbstractCreature) && Custom.DistLess(scavenger.mainBodyChunk.pos, obj.firstChunk.pos, 50f) && this.room.VisualContact(scavenger.mainBodyChunk.pos, obj.firstChunk.pos) && obj.grabbedBy.Count < 1 && (!(apo is AbstractSpear) || !(apo as AbstractSpear).stuckInWall))
                                {
                                    if (obj is Weapon)
                                    {
                                        if ((obj as Weapon).mode == Weapon.Mode.Thrown)
                                        {
                                            continue;
                                        }
                                    }
                                    while (obj.grabbedBy.Count > 0)
                                    {
                                        obj.grabbedBy[0].Release();
                                    }
                                    obj.abstractPhysicalObject.LoseAllStuckObjects();
                                    scavenger.PickUpAndPlaceInInventory(obj, false);
                                    break;
                                }
                            }
                        }
                    }*/
                    //}
                    if (!found && grabDownTime != 0)
                    {
                        scavenger.ControlCycleInventory();
                        grabDownTime = 0;
                    }
                    else grabDownTime = 12;
                }
            }
            lastGrab = true;
        }
        else
        {
            lastGrab = false;
        }

        if (grabDownTime > 0) grabDownTime--;

        if (input[0].thrw)
        {
            if (!lastThrow)
            {
                Vector2 aimPosition = this.scavenger.mainBodyChunk.pos + new Vector2(this.scavenger.flip * 250f, (this.scavenger.flip * 125f) * input[0].y);
                var personality = scavenger.abstractCreature.personality;
                scavenger.abstractCreature.personality = StoryMenagerie.GamerPersonality;
                //if (scavenger.animation.id != Scavenger.ScavengerAnimation.ID.ThrowCharge)
                //{
                    //scavenger.animation = new Scavenger.ThrowChargeAnimation(scavenger, null);
                    //scavenger.animation.age = 40;
                    //(scavenger.animation as Scavenger.ThrowChargeAnimation).aimTarget = aimPosition;
                //}
                this.scavenger.TryThrow(null, ScavengerAI.ViolenceType.Lethal, aimPosition);
                //scavenger.Throw(aimPosition);
                scavenger.abstractCreature.personality = personality;
            }
            lastThrow = true;
        }
        else
        {
            lastThrow = false;
        }

        if (input[0].spec)
        {
            (this.scavenger.graphicsModule as ScavengerGraphics).ShockReaction(UnityEngine.Random.Range(0.125f, 1f));
            lastSpec = true;
        }
        else
        {
            lastSpec = false;
        }

        // awful
        if (ModManager.MSC && canGroundJump > 0 && superLaunchJump > 15 && (scavenger.Elite || scavenger.Templar))
        {
            scavenger.abstractCreature.controlled = true;
            scavenger.inputWithDiagonals = input[0];
            scavenger.lastInputWithDiagonals = input[0];
            if (scavenger.controlledJumpFinder != null && scavenger.controlledJumpFinder.startPos != scavenger.abstractCreature.pos.Tile)
            {
                scavenger.controlledJumpFinder.Destroy();
                scavenger.controlledJumpFinder = null;
            }
            if (scavenger.controlledJumpFinder == null)
            {
                scavenger.controlledJumpFinder = new Scavenger.JumpFinder(scavenger.room, scavenger, scavenger.abstractCreature.pos.Tile);
            }
            scavenger.controlledJumpFinder.Update();
            scavenger.controlledJumpFinder.fade = 0;
            if (scavenger.controlledJumpFinder.bestJump != null)
            {
                scavenger.InitiateJump(scavenger.controlledJumpFinder, 30);
            }
            scavenger.abstractCreature.controlled = false;
        }
        if (!this.IsOnPole) scavenger.moveModeChangeCounter = -5;
    }

    public override void Moving(float magnitude)
    {
        base.Moving(magnitude);
        if (scavenger.gravity != 0f && scavenger.room.gravity != 0f)
        {
            for (int i = 0; i < creature.bodyChunks.Length; i++)
            {
                BodyChunk bodyChunk = scavenger.bodyChunks[i];
                if (Mathf.Abs(bodyChunk.vel.x) > runSpeed * 2f)
                {
                    bodyChunk.vel.x *= 0.98f;
                }
            }
        }
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (!scavenger.Consious && !scavenger.dead && scavenger.stun < 35 && scavenger.grabbedBy.Count > 0 && !(scavenger.grabbedBy[0].grabber is Leech))
        {
            CheckInput();
            if (input[0].thrw /*&& !lastThrow*/)
            {
                scavenger.grabbedAttackCounter++;
                if (scavenger.King && scavenger.grabbedAttackCounter == 25)
                {
                    scavenger.MeleeGetFree(scavenger.grabbedBy[0].grabber, scavenger.room.game.evenUpdate);
                    scavenger.grabbedAttackCounter = 0;
                    lastThrow = true;
                }
                else if ((scavenger.grabbedAttackCounter == 25 || (scavenger.Elite && scavenger.grabbedAttackCounter % 75 == 0)) && UnityEngine.Random.value < Mathf.Pow((scavenger.State as HealthState).health, 0.4f) && scavenger.grabbedBy[0].grabber.Template.type != CreatureTemplate.Type.RedLizard && scavenger.grabbedBy[0].grabber.Template.type != CreatureTemplate.Type.KingVulture && (scavenger.grabbedBy[0].grabber.Template.type != CreatureTemplate.Type.Vulture || UnityEngine.Random.value < 0.6f))
                {
                    (this.scavenger.graphicsModule as ScavengerGraphics).ShockReaction(UnityEngine.Random.Range(0.25f, 1f));
                    scavenger.MeleeGetFree(scavenger.grabbedBy[0].grabber, scavenger.room.game.evenUpdate);
                    lastThrow = true;
                }
            }
        }

        if (handOnExternalFoodSource != default(Vector2) && (!scavenger.Consious || !Custom.DistLess(scavenger.mainBodyChunk.pos, handOnExternalFoodSource, 45f) || this.eatExternalFoodSourceCounter < 1))
        {
            handOnExternalFoodSource = default(Vector2);
        }

        if (scavenger.Consious && input[0].pckp && input[0].x == 0 && input[0].y == 0)
        {
            if (scavenger.grasps != null)
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

    public void BiteEdible(bool eu)
    {
        if (scavenger.grasps[0] != null)
        {
            var grabbed = scavenger.grasps[0].grabbed;
            if (grabbed is IPlayerEdible edible && edible.Edible)
            {
                BiteStruggle();
                if (edible.BitesLeft == 1)
                {
                    //this.AddFood(edible.FoodPoints, 1f);
                }
                if (grabbed is Creature creature)
                {
                    creature.SetKillTag(scavenger.abstractCreature);
                }
                if (scavenger.graphicsModule != null)
                {
                    //(mouse.graphicsModule as PlayerGraphics).BiteFly(i);
                }
                this.FoodBitByPlayer(grabbed, 0, eu);
                return;
            }
            else if (grabbed is VultureMask mask && ModManager.DLCShared && scavenger.graphicsModule is ScavengerGraphics graphics && graphics.maskGfx == null && mask.abstractPhysicalObject is VultureMask.AbstractVultureMask amask)
            {
                if (scavenger.King)
                {
                    graphics.maskGfx = new MoreSlugcats.VultureMaskGraphics(this.scavenger, amask, graphics.MaskSprite);
                    graphics.maskGfx.GenerateColor(this.scavenger.abstractCreature.ID.RandomSeed);
                }
                else if (scavenger.Elite)
                {
                    graphics.maskGfx = new MoreSlugcats.VultureMaskGraphics(this.scavenger, amask, graphics.MaskSprite);
                    graphics.maskGfx.GenerateColor(this.scavenger.abstractCreature.ID.RandomSeed);
                }
                else if (scavenger.Templar)
                {
                    graphics.maskGfx = new MoreSlugcats.VultureMaskGraphics(scavenger, amask, graphics.MaskSprite);
                    graphics.maskGfx.GenerateColor(scavenger.abstractCreature.ID.RandomSeed);
                }
                if (ModManager.Watcher && graphics.maskGfx.maskType == VultureMask.MaskType.SCAVTEMPLAR)
                {
                    graphics.maskGfx.ignoreDarkness = true;
                    graphics.maskGfx.glimmer = true;
                }
                if (graphics.maskGfx != null)
                {
                    scavenger.grasps[0].Release();
                    mask.Destroy();
                }
            }
        }
    }

    public void BiteStruggle()
    {
        var graphics = scavenger.graphicsModule as ScavengerGraphics;
        if (graphics != null)
        {
            var dir = Custom.DirVec(scavenger.mainBodyChunk.pos, scavenger.grasps[0].grabbedChunk.pos);
            scavenger.mainBodyChunk.vel += Vector2.Lerp(Custom.RNV(), dir, 0.7f);
            graphics.lookPoint = dir;
            graphics.hands[0].vel += dir;
            if (graphics.blink < 5)
            {
                graphics.blink = 5;
            }
        }
    }
}
