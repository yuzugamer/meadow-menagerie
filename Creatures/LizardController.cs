using System;
using RainMeadow;
using UnityEngine;
using RWCustom;
using static MonoMod.InlineRT.MonoModRule;

namespace StoryMenagerie.Creatures
{
    public class LizardController : RainMeadow.LizardController
    {
        public int throwCooldown;
        public int grabHeld;
        public bool lastThrow;
        public int lungeTime;
        public bool lastJump;
        public bool lastSpec;
        public int specCooldown;
        public float jawCounter;
        public bool putDown;
        //public int foodInStomach { get; set; }

        /*public static void On_Lizard_Act(On.Lizard.orig_Act orig, Lizard self)
        {
            if (Input.GetKey(KeyCode.L)) RainMeadow.RainMeadow.Debug($"liz act pre");
            if (creatureControllers.TryGetValue(self, out var c) && c is LizardController l)
            {
                l.ConsciousUpdate();
            }
            orig(self);
            if (Input.GetKey(KeyCode.L)) RainMeadow.RainMeadow.Debug($"liz act post");
        }*/
        public LizardController(Lizard lizard, OnlineCreature oc, int playerNumber, CreatureCustomization customization) : base(lizard, oc, playerNumber, new ExpandedAvatarData(customization))
        {
            lizard.effectColor = customization.bodyColor.SafeColorRange();
        }

        public virtual void TryBite()
        {
            if (lizard.grasps[0] != null || !lizard.Consious)
            {
                return;
            }
            if (lizard.JawReadyForBite)
            {

                var lizPos = lizard.mainBodyChunk.pos;
                var dist = lizard.lizardParams.attemptBiteRadius + 5f;
                var compare = dist;
                PhysicalObject target = null;
                /*foreach (var layer in lizard.room.physicalObjects)
                {
                    foreach (var obj in layer)
                    {
                        if (obj != lizard)
                        {
                            var crit = obj as Creature;
                            var isCrit = crit != null;
                            var chunk = isCrit ? crit.mainBodyChunk : obj.firstChunk;
                            var tempDist = Vector2.Distance(lizPos, chunk.pos);
                            if (tempDist < dist)
                            {
                                // prioritize creatures that lizard would normally attack
                                if (isCrit) dist = (!lizard.AI.DynamicRelationship(crit.abstractCreature).GoForKill ? tempDist : tempDist * 1.2f);
                                // heavily priortize creatures over objects
                                else dist = tempDist * 100f;
                                target = obj;
                                StoryMenagerie.Debug("bite target set");
                            } else
                            {
                                StoryMenagerie.Debug("bite target not set");
                            }
                        }
                    }
                }*/
                foreach (var layer in creature.room.physicalObjects)
                {
                    foreach (var obj in layer)
                    {
                        if (obj != null && obj != creature && (obj.abstractPhysicalObject.rippleLayer == creature.abstractPhysicalObject.rippleLayer || obj.abstractPhysicalObject.rippleBothSides || creature.abstractPhysicalObject.rippleBothSides) /*&& (obj is not PlayerCarryableItem carryable || carryable.forbiddenToPlayer < 1)*/ && Custom.DistLess(lizPos, obj.bodyChunks[0].pos, obj.bodyChunks[0].rad + dist))// && (Custom.DistLess(creature.bodyChunks[0].pos, obj.bodyChunks[0].pos, obj.bodyChunks[0].rad + (dist / 2f)) || creature.room.VisualContact(creature.bodyChunks[0].pos, obj.bodyChunks[0].pos)) /*&& creature.CanIPickThisUp(obj)*/)
                        {
                            var tdist = Vector2.Distance(creature.bodyChunks[0].pos, obj.bodyChunks[0].pos);
                            if (obj is Spear spear)
                            {
                                if (spear.abstractSpear.stuckInWall)
                                {
                                    continue;
                                }
                                if (obj is Creature crit)
                                {
                                    if (!lizard.AI.DynamicRelationship(crit.abstractCreature).GoForKill)
                                    {
                                        tdist *= 1.2f;
                                    }
                                }
                                else
                                {
                                    tdist *= 100f;
                                }
                            }
                            if (tdist < compare)
                            {
                                target = obj;
                                compare = tdist;
                            }
                        }
                    }
                }
                bool success = false;
                if (target != null)
                {
                    if ((!ModManager.MSC || lizard.Template.type != MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.TrainLizard) && lizard.tongue != null && lizard.tongue.Out)
                    {
                        if (lizard.tongue.state != LizardTongue.State.StuckInTerrain)
                        {
                            lizard.tongue.Retract();
                        }
                        StoryMenagerie.Debug("tongue out, can't bite");
                        return;
                    }
                    StoryMenagerie.Debug("attempting bite");
                    var headPos = lizard.mainBodyChunk.pos + Custom.DirVec(lizard.bodyChunks[1].pos, lizard.mainBodyChunk.pos) * lizard.lizardParams.biteInFront;
                    var cDist = float.MaxValue;
                    var leniency = 3f * lizard.lizardParams.headSize;
                    BodyChunk bitChunk = null;
                    foreach (var chunk in target.bodyChunks)
                    {
                        var chunkDist = (headPos - chunk.pos).magnitude;
                        if (chunkDist < cDist && chunkDist < (chunk.rad + lizard.lizardParams.biteRadBonus) * leniency)
                        {
                            bitChunk = chunk;
                            cDist = chunkDist;
                        }
                        else
                        {
                            //StoryMenagerie.Debug("distance is " + ((lizard.mainBodyChunk.pos + Custom.DirVec(lizard.bodyChunks[1].pos, lizard.mainBodyChunk.pos) * lizard.lizardParams.biteInFront - chunk.pos).magnitude) + ", under " + ((chunk.rad + lizard.lizardParams.biteRadBonus) * 3f));
                        }
                    }
                    if (bitChunk != null)
                    {
                        success = true;
                        StoryMenagerie.Debug("lizard bite!");
                        lizard.jawOpen = 1f;
                        lizard.Bite(bitChunk);
                        if (bitChunk.owner is KarmaFlower flower)
                        {
                            var game = lizard.room.game;
                            (game.session as StoryGameSession).saveState.deathPersistentSaveData.reinforcedKarma = true;
                            game.cameras[0].hud.karmaMeter.reinforceAnimation = 0;
                            if (!OnlineManager.lobby.isOwner)
                            {
                                OnlineManager.lobby.owner.InvokeRPC(StoryRPCs.ReinforceKarma);
                            }
                            foreach (OnlinePlayer player in OnlineManager.players)
                            {
                                if (!player.isMe)
                                {
                                    player.InvokeRPC(StoryRPCs.PlayReinforceKarmaAnimation);
                                }
                            }
                            // just in case
                            if (lizard.grasps[0] != null) lizard.grasps[0].Release();
                            flower.Destroy();
                        }
                    }
                }
                else
                {
                    StoryMenagerie.Debug("no bite target found");
                    lizard.Bite(null);
                }
                if (lizard.LegsGripping > 0)
                {
                    if (success)
                    {
                        foreach (var chunk in lizard.bodyChunks)
                        {
                            //chunk.vel += Custom.DegToVec(UnityEngine.Random.value * 360f) * 7f;
                        }
                        return;
                    }
                    if (target != null && (lizard.tongue == null || !lizard.tongue.Out))
                    {
                        //lizard.mainBodyChunk.vel += Custom.DirVec(lizard.mainBodyChunk.pos, target.mainBodyChunk.pos) * 3f * lizard.lizardParams.biteHomingSpeed;
                        //lizard.bodyChunks[1].vel -= Custom.DirVec(lizard.mainBodyChunk.pos, target.mainBodyChunk.pos) * lizard.lizardParams.biteHomingSpeed;
                        //lizard.bodyChunks[2].vel -= Custom.DirVec(lizard.mainBodyChunk.pos, target.mainBodyChunk.pos) * lizard.lizardParams.biteHomingSpeed;
                    }
                }
            }
        }

        public override void ConsciousUpdate()
        {
            lockInPlace = input[0].thrw && (((!ModManager.MSC || lizard.Template.type != MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.TrainLizard) && lizard.tongue != null) || lizard.AI.redSpitAI != null);
            base.ConsciousUpdate();
            if (input[0].pckp)
            {
                if (!putDown)
                {
                    if (lizard.grasps[0] == null)
                    {
                        // who even knows
                        //lizard.jawOpen = Mathf.Clamp(lizard.jawOpen + Mathf.Lerp((1f / (lizard.jawOpen > 0.75f ? (lizard.lizardParams.biteDelay + 2) : ((lizard.lizardParams.biteDelay + 1) / 2))), (1f - lizard.jawOpen) * 0.04f, Mathf.Pow(lizard.jawOpen, 1.6f)), 0f, 1f);
                        jawCounter = Mathf.Clamp(jawCounter + Mathf.Lerp((1f / (jawCounter > 0.75f ? (lizard.lizardParams.biteDelay + 3) : ((lizard.lizardParams.biteDelay + 2) / 2))), (1f - jawCounter) * 0.075f, (jawCounter / 1.35f)), 0f, 1f);
                        lizard.jawOpen = jawCounter;
                        grabHeld++;
                    }
                    else if (input[0].y < 0)
                    {
                        lizard.ReleaseGrasp(0);
                        lizard.jawOpen += 0.2f;
                        grabHeld = 0;
                        putDown = true;
                    }
                }
            }
            else
            {
                putDown = false;
                lizard.jawOpen -= 0.01f;
                jawCounter = Mathf.Min(0f, jawCounter - 0.01f);
                if (grabHeld > 0)
                {
                    StoryMenagerie.Debug("held for " + grabHeld + ", bite delay is " + lizard.lizardParams.biteDelay);
                    var pos = lizard.mainBodyChunk.pos;
                    if (lizard.grasps[0] == null)
                    {
                        if ((lizard.jawOpen > 0.25f || grabHeld >= (lizard.lizardParams.biteDelay / 10)))
                        {
                            lizard.jawOpen = 1f;
                            TryBite();
                            lizard.biteDelay = 0;
                        }
                        lizard.jawOpen = 0f;
                    }
                }
                grabHeld = 0;
            }
            if (false && lizard.animation == Lizard.Animation.Spit)
            {
                lizard.EnterAnimation(Lizard.Animation.Standard, true);
                lizard.AI.redSpitAI.spitting = false;
                lizard.timeToRemainInAnimation = 0;
            }
            if (input[0].thrw)
            {
                if (throwCooldown == 0)
                {
                    StoryMenagerie.Debug("trying throw ability");
                    if (lizard.AI.redSpitAI != null)
                    {
                        StoryMenagerie.Debug("spitting");
                        lizard.EnterAnimation(Lizard.Animation.Spit, true);
                        lizard.AI.redSpitAI.spitting = true;
                        lizard.AI.redSpitAI.delay = 0;
                        lizard.ActAnimation();
                        lizard.EnterAnimation(Lizard.Animation.Standard, false);
                        lizard.AI.redSpitAI.spitting = false;
                        lizard.timeToRemainInAnimation = 0;
                        throwCooldown = 12;
                        lastThrow = true;
                    }
                    else if (lizard.lizardParams.tongue && !lastThrow)
                    {
                        StoryMenagerie.Debug("tongue lash out");
                        //lizard.EnterAnimation(Lizard.Animation.ShootTongue, true);
                        //lizard.ActAnimation();
                        //lizard.EnterAnimation(Lizard.Animation.Standard, false);
                        lizard.jawOpen = 1f;
                        lizard.tongue.LashOut(lizard.mainBodyChunk.pos + new Vector2(input[0].x, input[0].y) * lizard.lizardParams.tongueAttackRange);
                        throwCooldown = (int)(lizard.lizardParams.tongueWarmUp);
                        lastThrow = true;
                    }
                    else
                    {
                        StoryMenagerie.Debug("no ability used, last throw is" + lastThrow);
                    }
                }
            }
            else lastThrow = false;
            if (throwCooldown > 0) throwCooldown--;
            if (lizard.jumpModule is LizardJumpModule jumpModule)
            {
                if (this.superLaunchJump > 10)
                {
                    if (input[0].jmp)
                    {
                        if (jumpModule.actOnJump == null)
                        {
                            // start a new jump
                            RainMeadow.RainMeadow.Debug("JumpModule init");
                            var jumpFinder = new LizardJumpModule.JumpFinder(creature.room, jumpModule, lizard.coord.Tile, false);
                            jumpFinder.currentJump.power = 0.5f;
                            jumpFinder.bestJump = jumpFinder.currentJump;
                            jumpFinder.bestJump.goalCell = jumpFinder.startCell;
                            jumpFinder.bestJump.tick = 20;

                            jumpModule.spin = 1;
                            jumpModule.InitiateJump(jumpFinder, false);
                        }
                        jumpModule.actOnJump.vel = (creature.bodyChunks[0].pos - creature.bodyChunks[1].pos).normalized * 4f + (inputDir.magnitude > 0.5f ? inputDir * 14 + new Vector2(0, 2) : new Vector2(12f * flipDirection, 9f));
                        jumpModule.actOnJump.bestJump.initVel = jumpModule.actOnJump.vel;
                        jumpModule.actOnJump.bestJump.goalCell = lizard.AI.pathFinder.PathingCellAtWorldCoordinate(creature.room.GetWorldCoordinate(creature.bodyChunks[0].pos + jumpModule.actOnJump.vel * 20));
                        canGroundJump = 5; // doesn't interrupt
                        superLaunchJump = 12; // never completes
                        lockInPlace = true;
                        Moving(1f);
                    }
                    else
                    {
                        if (lizard.animation != Lizard.Animation.Jumping)
                        {
                            jumpModule.actOnJump = null;
                        }
                    }
                }
            }
            else if (this.superLaunchJump > 10)
            {
                superLaunchJump = 0;
                //lizard.EnterAnimation(Lizard.Animation.PrepareToLounge, true);
                //lizard.Update(true);
                lizard.EnterAnimation(Lizard.Animation.Lounge, true);
                lungeTime = lizard.lizardParams.loungeMaximumFrames + 5;
                canGroundJump = 5;
                Moving(1f);
                lastJump = true;
                /*lizard.loungeDelay = lizard.lizardParams.loungeDelay * ((UnityEngine.Random.value < lizard.lizardParams.riskOfDoubleLoungeDelay) ? 2 : 1) + lizard.lizardParams.loungeMaximumFrames;
                if (lizard.AI.focusCreature != null && lizard.AI.focusCreature.representedCreature != null && lizard.AI.focusCreature.representedCreature.realizedCreature != null)
                {
                    lizard.loungeDir = Vector3.Slerp(lizard.loungeDir, Custom.DirVec(lizard.mainBodyChunk.pos, lizard.AI.focusCreature.representedCreature.realizedCreature.mainBodyChunk.pos), lizard.lizardParams.findLoungeDirection);
                }
                if (Vector2.Dot(Custom.DirVec(lizard.bodyChunks[1].pos, lizard.bodyChunks[0].pos), lizard.loungeDir) < 0f)
                {
                    if ((lizard.lizardParams.canExitLounge || lizard.lizardParams.canExitLoungeWarmUp) && !lizard.safariControlled)
                    {
                        lizard.EnterAnimation(Lizard.Animation.Standard, true);
                    }
                    else
                    {
                        lizard.loungeDir = Custom.DirVec(lizard.bodyChunks[1].pos, lizard.bodyChunks[0].pos);
                    }
                }
                if (lizard.loungeDir.y > 0f)
                {
                    lizard.loungeDir.y = lizard.loungeDir.y + lizard.lizardParams.loungeJumpyness;
                }
                lizard.loungeDir.y = lizard.loungeDir.y * 0.5f;
                lizard.timeToRemainInAnimation = lizard.lizardParams.loungeMaximumFrames + 5;
                if (ModManager.Watcher && lizard.rotModule != null)
                {
                    lizard.rotModule.Hiss(60);
                    return;
                }
                lizard.room.PlaySound(SoundID.Lizard_Lunge_Attack_Init, lizard.mainBodyChunk);
                canGroundJump = 0;
                canPoleJump = 0;*/
            }
            if (input[0].jmp)
            {
                if (!lastJump)
                {
                    lizard.EnterAnimation(Lizard.Animation.Standard, true);
                    lastJump = true;
                }
            }
            else
            {
                lastJump = false;
            }

            if (specCooldown > 0) specCooldown--;
            if (input[0].spec && !lastSpec && specCooldown == 0)
            {
                if (lizard.voice != null)
                {
                    /*var module = lizard.AI.utilityComparer.HighestUtilityModule();
                    var emotion = LizardVoice.Emotion.Boredom;
                    if (module is PreyTracker)
                    {
                        emotion = UnityEngine.Random.value > 0.5f ? LizardVoice.Emotion.BloodLust : (UnityEngine.Random.value > 0.6f ? LizardVoice.Emotion.SpottedPreyFirstTime : LizardVoice.Emotion.ReSpottedPrey);
                    }
                    else if (module is ThreatTracker || module is RainTracker)
                    {
                        emotion = LizardVoice.Emotion.Fear;
                    }
                    else if (module is AgressionTracker)
                    {
                        emotion = UnityEngine.Random.value > 0.5f ? LizardVoice.Emotion.Dominance : LizardVoice.Emotion.BloodLust;
                    }
                    else if (module is NoiseTracker)
                    {
                        emotion = LizardVoice.Emotion.Curious;
                    }
                    else if (module is InjuryTracker)
                    {
                        emotion = LizardVoice.Emotion.PainIdle;
                    }
                    else if (module is FriendTracker)
                    {
                        emotion = LizardVoice.Emotion.Frustration;
                    }
                    else if (module is MissionTracker)
                    {
                        emotion = LizardVoice.Emotion.OutOfShortcut;
                    }
                    lizard.voice.MakeSound(emotion, 1f);*/
                    // yes this is copy pasted safari code idec at this point
                    if (input[0].y == 0 && input[0].x == 0)
                    {
                        lizard.voice.MakeSound(LizardVoice.Emotion.GeneralSmallNoise);
                        lizard.bubble = 5;
                        lizard.bubbleIntensity = UnityEngine.Random.value * 0.5f;
                    }
                    else if (Mathf.Abs(input[0].y) > Mathf.Abs(input[0].x))
                    {
                        lizard.voice.MakeSound(LizardVoice.Emotion.Dominance);
                        lizard.bubble = 30;
                        lizard.bubbleIntensity = UnityEngine.Random.value * 0.5f + 0.5f;
                    }
                    else
                    {
                        lizard.voice.MakeSound(LizardVoice.Emotion.Frustration);
                        lizard.bubble = 20;
                        lizard.bubbleIntensity = UnityEngine.Random.value * 0.5f;
                    }
                }
                if (lizard.AI.yellowAI != null)
                {
                    var yellowAI = lizard.AI.yellowAI;
                    StoryMenagerie.Debug("attempting communication");
                    yellowAI.communicating = 16;
                    foreach (var member in yellowAI.pack.members)
                    {
                        if (member.lizard.abstractAI.RealAI != null)
                        {
                            for (int i = 0; i < lizard.AI.tracker.CreaturesCount; i++)
                            {
                                if (member.lizard.realizedCreature != null && member.lizard.realizedCreature.Consious)
                                {
                                    var packMember = member.lizard.realizedCreature as Lizard;
                                    yellowAI.PackMemberIsSeeingCreature(packMember, member.lizard.abstractAI.RealAI.tracker.GetRep(i));
                                    //packMember.AI.excitement = 1f;
                                    //packMember.AI.runSpeed = 1f;
                                    //member.lizard.abstractAI.SetDestination(lizard.abstractCreature.pos);
                                }
                            }
                        }
                    }
                }
                specCooldown = 5;
                lastSpec = true;
            }
            else lastSpec = false;

            // lost footing doesn't auto-recover
            if (lizard.inAllowedTerrainCounter < 10)
            {
                if (!(WallClimber && input[0].y == 1)
                    && lizard.gripPoint == null
                    && creature.bodyChunks[0].contactPoint.y != -1
                    && creature.bodyChunks[1].contactPoint.y != -1
                    && !creature.IsTileSolid(1, 0, -1)
                    && !creature.IsTileSolid(0, 0, -1))
                {
                    lizard.inAllowedTerrainCounter = 0;
                }
            }
            // footing recovers faster on climbing ledges etc
            if (forceJump <= 0 && lizard.inAllowedTerrainCounter < 20 && input[0].x != 0 && (creature.bodyChunks[0].contactPoint.x == input[0].x || creature.bodyChunks[1].contactPoint.x == input[0].x))
            {
                if (lizard.inAllowedTerrainCounter > 0) lizard.inAllowedTerrainCounter = Mathf.Max(lizard.inAllowedTerrainCounter + 1, 10);
            }

            // body points to input
            if (inputDir.magnitude > 0f && !lockInPlace)
            {
                creature.bodyChunks[0].vel += inputDir * 0.4f;
                creature.bodyChunks[2].vel -= inputDir * 0.4f;
            }

            // climb that damn ledge
            if (input[0].x != 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (creature.bodyChunks[i].contactPoint.x == input[0].x
                    && creature.bodyChunks[i].vel.y < 4f
                    && GetTile(i, input[0].x, 0).Solid
                    && !GetTile(i, 0, 1).Solid
                    && !GetTile(i, input[0].x, 1).Solid
                    )
                    {
                        creature.bodyChunks[0].vel += new Vector2(0f, 2f);
                    }
                }
            }

            if (lizard.timeSpentTryingThisMove < 20) // don't panic
            {
                lizard.desperationSmoother = 0f;
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (lungeTime > 0)
            {
                lizard.animation = Lizard.Animation.Lounge;
                lungeTime--;
            }
            if (!lizard.Consious)
            {
                jawCounter = Mathf.Min(0f, jawCounter - 0.01f);
                putDown = false;
            }
        }
    }
}
