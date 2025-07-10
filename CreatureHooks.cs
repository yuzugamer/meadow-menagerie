using System;
using System.Collections.Generic;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Reflection;
using RainMeadow;
using RWCustom;
using UnityEngine;
using MonoMod.RuntimeDetour;

namespace StoryMenagerie;

public static class CreatureHooks
{
    public static void Apply()
    {
        On.Creature.Violence += On_Creature_Violence;
        IL.Lizard.SpitOutOfShortCut += IL_Lizard_SpitOutOfShortcut;
        On.Lizard.CarryObject += On_Lizard_CarryObject;
        On.Scavenger.SetUpCombatSkills += On_Scavenger_SetUpCombatSkills;
        new Hook(typeof(LizardGraphics).GetMethod("get_effectColor"), On_LizardGraphics_get_effectColor);
        new ILHook(typeof(LizardGraphics).GetMethod("get_HeadColor1", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), IL_LizardGraphics_get_HeadColor);
        new ILHook(typeof(LizardGraphics).GetMethod("get_HeadColor2", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), IL_LizardGraphics_get_HeadColor);
        IL.ScavengerAI.IUseARelationshipTracker_UpdateDynamicRelationship += IL_ScavengerAI_UpdateDynamicRelationship;
        On.Creature.Die += On_Creature_Die;
        On.UpdatableAndDeletable.Destroy += On_UpdatableAndDeletable_Destroy;
        On.AbstractCreature.IsEnteringDen += On_AbstractCreature_IsEnteringDen;
        On.Creature.Grab += On_Creature_Grab;
        On.LizardAI.LizardSpitTracker.AimPos += On_LizardAI_LizardSpitTracker_AimPos;
        On.Lizard.Collide += On_Lizard_Collide;
        On.Creature.SpitOutOfShortCut += On_Creature_SpitOutOfShortCut;
        IL.ScavengerAI.LikeOfPlayer += IL_ScavengerAI_LikeOfPlayer;
        IL.Lizard.Bite += IL_Lizard_Bite;
        On.AbstractCreature.InDenUpdate += On_AbstractCreature_InDenUpdate;
        On.AbstractCreature.WantToStayInDenUntilEndOfCycle += On_AbstractCreature_WantToStayInDenUntilEndOfCycle;
        On.Creature.ShortCutColor += On_Creature_ShortCutColor;
        On.BigNeedleWorm.Swish += On_BigNeedleWorm_Swish;
        On.AbstractCreature.Update += On_AbstractCreature_Update;
        IL.BigNeedleWorm.Swish += IL_BigNeedleWorm_Swish;
        On.EggBug.ShortCutColor += On_EggBug_ShortCutColor;
        On.JetFish.CarryObject += On_JetFish_CarryObject;
        On.ScavengerGraphics.Update += On_ScavengerGraphics_Update;
        On.Scavenger.Update += On_Scavenger_Update;
    }

    public static void On_Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stun)
    {
        // no hurt fren (slugpups are on the menu though)
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode story && !story.friendlyFire && source.owner.abstractPhysicalObject is AbstractCreature asource && story.abstractAvatars.Contains(asource) && story.abstractAvatars.Contains(self.abstractCreature))
        {
            return;
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stun);
    }

    public static bool CreatureIsAvatar(Creature crit) => OnlineManager.lobby != null && CreatureController.creatureControllers.TryGetValue(crit, out var _);

    public static void IL_Lizard_SpitOutOfShortcut(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip = c.DefineLabel();
            c.GotoNext(
                x => x.MatchCall(out var _),
                x => x.MatchLdcR4(0.5f),
                x => x.MatchBgeUn(out skip)
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(CreatureIsAvatar);
            c.Emit(OpCodes.Brtrue, skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void On_Lizard_CarryObject(On.Lizard.orig_CarryObject orig, Lizard self, bool eu)
    {
        //if (CreatureController.creatureControllers.TryGetValue(self, out var liz) && liz is StoryLizardController)
        //{
            //if (UnityEngine.Random.value < 0.025f && (!(self.grasps[0].grabbed is Creature) || self.AI.DynamicRelationship((self.grasps[0].grabbed as Creature).abstractCreature).type != CreatureTemplate.Relationship.Type.Eats))
            //{
                //self.LoseAllGrasps();
                //return;
            //}
        //}
        orig(self, eu);
    }

    public static void On_Scavenger_SetUpCombatSkills(On.Scavenger.orig_SetUpCombatSkills orig, Scavenger self)
    {
        if (CreatureController.creatureControllers.TryGetValue(self, out var _))
        {
            self.blockingSkill = 1f;
            self.dodgeSkill = 1f;
            self.meleeSkill = 1f;
            self.midRangeSkill = 1f;
            self.reactionSkill = 1f;
        }
        orig(self);
    }

    public static Color On_LizardGraphics_get_effectColor(Func<LizardGraphics, Color> orig, LizardGraphics self)
    {
        if (CreatureController.creatureControllers.TryGetValue(self.lizard, out var cc) && cc.isStory(out var _))
        {
            if (self.snowAccCosmetic != null)
            {
                return Color.Lerp(self.lizard.effectColor, Color.Lerp(Color.white, self.whiteCamoColor, 0.5f), Mathf.Min(1f, self.snowAccCosmetic.DebrisSaturation * 1.5f));
            }
            return self.lizard.effectColor;
        }
        return orig(self);
    }

    public static bool UseEffectColor(LizardGraphics self) => CreatureController.creatureControllers.TryGetValue(self.lizard, out var cc) && cc.isStory(out var _);

    public static void IL_LizardGraphics_get_HeadColor(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip = c.DefineLabel();
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld<CreatureTemplate.Type>(nameof(CreatureTemplate.Type.BlackLizard)),
                x => x.MatchCallOrCallvirt(typeof(ExtEnum<CreatureTemplate.Type>).GetMethod("op_Equality")),
                x => x.MatchBrfalse(out skip)
                );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(UseEffectColor);
            c.Emit(OpCodes.Brtrue, skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void UsePlayerRelationship(ScavengerAI self, RelationshipTracker.DynamicRelationship dRelation, CreatureTemplate.Relationship relationship)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && menagerie.abstractAvatars.Contains(dRelation.trackerRep.representedCreature))
        {
            relationship = self.PlayerRelationship(dRelation);
        }
    }

    public static void IL_ScavengerAI_UpdateDynamicRelationship(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip = c.DefineLabel();
            c.GotoNext(
               x => x.MatchLdloc(0),
               x => x.MatchLdfld<CreatureTemplate.Relationship>("type"),
               x => x.MatchLdsfld<CreatureTemplate.Relationship.Type>(nameof(CreatureTemplate.Relationship.type.SocialDependent)),
               x => x.MatchCallOrCallvirt(typeof(ExtEnum<CreatureTemplate.Relationship.Type>).GetMethod("op_Equality")),
               x => x.MatchBrfalse(out skip)
               );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate(UsePlayerRelationship);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void On_Creature_Die(On.Creature.orig_Die orig, Creature self)
    {
        if (OnlineManager.lobby != null && CreatureController.creatureControllers.TryGetValue(self, out var cc) && cc.isStory(out var scc) && OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity) && onlineEntity.isMine)
        {
            var game = Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
            game.GameOver(null);
            if (game.session is StoryGameSession story)
            {
                if ((story.saveState.deathPersistentSaveData.reinforcedKarma || story.saveStateNumber == SlugcatStats.Name.Yellow || (story.saveStateNumber == SlugcatStats.Name.Red && story.RedIsOutOfCycles)) && (!ModManager.MSC || !game.wasAnArtificerDream))
                {
                    if (story.RedIsOutOfCycles && OnlineManager.lobby.gameMode.avatars[0] == onlineEntity)
                    {
                        story.game.manager.rainWorld.progression.miscProgressionData.redsFlower = scc.karmaFlowerGrowPos;
                    }
                    else
                    {
                        story.saveState.deathPersistentSaveData.karmaFlowerPosition = scc.karmaFlowerGrowPos;
                    }
                }
            }
        }
        orig(self);
    }

    public static void On_UpdatableAndDeletable_Destroy(On.UpdatableAndDeletable.orig_Destroy orig, UpdatableAndDeletable self)
    {
        if (OnlineManager.lobby != null && self is Creature crit && CreatureController.creatureControllers.TryGetValue(crit, out var cc) && cc.isStory(out var scc) && (Custom.rainWorld.processManager.currentMainLoop as RainWorldGame).session is StoryGameSession story && OnlinePhysicalObject.map.TryGetValue(crit.abstractPhysicalObject, out var onlineEntity) && onlineEntity.isMine)
        {
            crit.Die();
        }
        orig(self);
    }

    public static void On_AbstractCreature_IsEnteringDen(On.AbstractCreature.orig_IsEnteringDen orig, AbstractCreature self, WorldCoordinate den)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && self.realizedCreature != null && CreatureController.creatureControllers.TryGetValue(self.realizedCreature, out var crit))
        {
            foreach (var stick in self.stuckObjects)
            {
                if (stick is AbstractPhysicalObject.CreatureGripStick)
                {
                    if (stick.B is AbstractCreature acrit)
                    {
                        var relation = self.abstractAI.RealAI.DynamicRelationship(acrit);
                        if (relation.type == CreatureTemplate.Relationship.Type.Eats)
                        {
                            var game = Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
                            if (game.session is StoryGameSession story && menagerie.foodPoints < story.characterStats.maxFood)
                            {
                                if (acrit.creatureTemplate.meatPoints == 0 && acrit.realizedCreature != null && acrit.realizedCreature is IPlayerEdible edible)
                                {
                                    crit.AddFood(edible.FoodPoints, 1f);
                                }
                                else
                                {
                                    crit.AddFood(acrit.creatureTemplate.meatPoints, 1f);
                                }
                            }
                            else if (self.IsLocal())
                            {
                                menagerie.showKarmaFoodRainTime += 300;
                            }
                        }
                    }
                    else if (stick.B.realizedObject != null)
                    {
                        if (self.CanEat(stick.B.realizedObject) && stick.B.realizedObject is IPlayerEdible edible)
                        {
                            crit.AddFood(edible.FoodPoints, 1f);
                        }
                    }
                    else if (crit is LanternMouseController && StoryLanternMouseController.IsEdible(stick.B) && stick.B is IPlayerEdible aEdible)
                    {
                        crit.AddFood(aEdible.FoodPoints, 1f);
                    }
                }
            }
        }
        orig(self, den);
    }

    public static bool On_Creature_Grab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && obj.abstractPhysicalObject is AbstractCreature acrit && menagerie.abstractAvatars.Contains(acrit) && (acrit.realizedCreature == null || (!acrit.realizedCreature.dead && acrit.realizedCreature.stun == 0)) && menagerie.abstractAvatars.Contains(self.abstractCreature)) return false;
        return orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying);
    }

    public static Vector2? On_LizardAI_LizardSpitTracker_AimPos(On.LizardAI.LizardSpitTracker.orig_AimPos orig, LizardAI.LizardSpitTracker self)
    {
        if (OnlineManager.lobby != null && self.lizardAI.lizard != null && CreatureController.creatureControllers.TryGetValue(self.lizardAI.lizard, out var cc) && cc is StoryLizardController liz)
        {
            StoryMenagerie.Debug("spitpos using inputs");
            if (liz.input[0].x != 0 || liz.input[0].y != 0)
            {
                return new Vector2?(liz.lizard.mainBodyChunk.pos + new Vector2((float)liz.input[0].x, (float)liz.input[0].y) * 100f);
            } else
            {
                var dir = Custom.DirVec(liz.lizard.bodyChunks[1].pos, liz.lizard.mainBodyChunk.pos);
                return new Vector2?(liz.lizard.mainBodyChunk.pos + new Vector2(dir.x < 0f ? -1f : 1f, dir.y < 0f ? -1f : 1f) * 100f);
            }
        }
        StoryMenagerie.Debug("spitpos not using inputs");
        return orig(self);
    }

    public static void On_Lizard_Collide(On.Lizard.orig_Collide orig, Lizard self, PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        if (CreatureController.creatureControllers.TryGetValue(self, out var _)) return;
        orig(self, otherObject, myChunk, otherChunk);
    }

    public static void On_Creature_SpitOutOfShortCut(On.Creature.orig_SpitOutOfShortCut orig, Creature self, IntVector2 pos, Room room, bool spitOutAllSticks)
    {
        if (OnlineManager.lobby != null && CreatureController.creatureControllers.TryGetValue(self, out var cc) && cc.isStory(out var scc))
        {
            scc.stillInStartShelter = false;
        }
        orig(self, pos, room, spitOutAllSticks);
    }

    public static bool IsMenagerie() => OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode;

    public static void IL_ScavengerAI_LikeOfPlayer(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<RelationshipTracker.DynamicRelationship>(nameof(RelationshipTracker.DynamicRelationship.trackerRep)),
                x => x.MatchLdfld<Tracker.CreatureRepresentation>(nameof(Tracker.CreatureRepresentation.representedCreature)),
                x => x.MatchLdfld<AbstractCreature>(nameof(AbstractCreature.state)),
                x => x.MatchIsinst<PlayerState>(),
                x => x.MatchLdfld<PlayerState>(nameof(PlayerState.playerNumber))
            );
            c.MoveAfterLabels();
            c.EmitDelegate(IsMenagerie);
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Brfalse, skip);
            c.Emit(OpCodes.Ldc_I4_0);
            var skip2 = c.DefineLabel();
            c.Emit(OpCodes.Br, skip2);
            c.MarkLabel(skip);
            c.GotoNext(x => x.MatchCallvirt<CreatureCommunities>(nameof(CreatureCommunities.LikeOfPlayer)));
            c.MarkLabel(skip2);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static float MassDiscrepancyLeniency(float orig, Lizard self) => OnlineManager.lobby != null && CreatureController.creatureControllers.TryGetValue(self, out var _) ? orig * 2.25f : orig;

    public static void IL_Lizard_Bite(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchCall<PhysicalObject>("get_TotalMass"),
                x => x.MatchLdcR4(1.2f)
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(MassDiscrepancyLeniency);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void On_AbstractCreature_InDenUpdate(On.AbstractCreature.orig_InDenUpdate orig, AbstractCreature self, int time)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && self.remainInDenCounter > -1 && ((self.realizedCreature != null && CreatureController.creatureControllers.TryGetValue(self.realizedCreature, out var _) || (menagerie.abstractAvatars != null && menagerie.abstractAvatars.Contains(self)))))
        {
            self.remainInDenCounter -= time;
            if (self.remainInDenCounter < 0)
            {
                self.remainInDenCounter = -1;
                self.Room.MoveEntityOutOfDen(self);
            }
        }
        orig(self, time);
    }

    public static bool On_AbstractCreature_WantToStayInDenUntilEndOfCycle(On.AbstractCreature.orig_WantToStayInDenUntilEndOfCycle orig, AbstractCreature self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && ((self.realizedCreature != null && CreatureController.creatureControllers.TryGetValue(self.realizedCreature, out var _) || (menagerie.abstractAvatars != null && menagerie.abstractAvatars.Contains(self)))))
        {
            return false;
        }
        return orig(self);
    }

    public static Color On_Creature_ShortCutColor(On.Creature.orig_ShortCutColor orig, Creature self)
    {
        var results = orig(self);
        if (OnlineManager.lobby != null && CreatureController.creatureControllers.TryGetValue(self, out var cc) && cc.isStory(out var scc))
        {
            return scc.storyCustomization.bodyColor;
        }
        return results;
    }

    public static void On_BigNeedleWorm_Swish(On.BigNeedleWorm.orig_Swish orig, BigNeedleWorm self)
    {
        orig(self);
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && CreatureController.creatureControllers.TryGetValue(self, out var crit))
        {
            if (self.impaleChunk != null && self.impaleChunk.owner is Creature target && !target.dead)
            {
                crit.AddFood(1);
            }
            float num = 90f + 90f * Mathf.Sin(Mathf.InverseLerp(1f, 5f, (float)self.swishCounter) * 3.1415927f);
            var value = self.swishDir.Value;
            StoryMenagerie.Debug("Checking for poppycorn");
                var refPos = self.bodyChunks[0].pos + value * (self.fangLength + num);
            var lastFangPos = self.bodyChunks[0].lastPos + value * self.fangLength;
            SharedPhysics.CollisionResult result = SharedPhysics.TraceProjectileAgainstBodyChunks(self, self.room, lastFangPos, ref refPos, 100f, 1, self, true);
                if (result.obj != null) StoryMenagerie.Debug("result is" + result.obj.GetType().Name);
            if (result.obj != null && (result.obj is SeedCob || (ModManager.Watcher && result.obj is Pomegranate)))
            {
                if (result.obj is SeedCob poppycorn)
                {
                    if (poppycorn.open > 0f)
                    {
                        return;
                    }
                    poppycorn.Open();
                }
                else if (ModManager.Watcher && result.obj is Pomegranate pomegranate)
                {
                    if (pomegranate.smashed)
                    {
                        return;
                    }
                    pomegranate.Smash();
                }
                crit.AddFood(10);
                var stabPos = result.collisionPoint - value * self.fangLength * 0.7f;
                self.stuckDir = Vector3.Slerp(self.swishDir.Value, Custom.DirVec(stabPos, result.chunk.pos), 0.4f);
                self.swishCounter = 0;
                self.swishDir = null;
                self.impaleChunk = result.chunk;
                float num2 = -self.fangLength / 4f;
                for (int k = 0; k < self.impaleDistances.GetLength(0); k++)
                {
                    if (k == 1)
                    {
                        num2 += self.fangLength / 4f;
                    }
                    if (k > 0)
                    {
                        num2 += self.GetSegmentRadForRopeLength(k - 1) + self.GetSegmentRadForRopeLength(k) + self.fangLength / 4f;
                    }
                    self.impaleDistances[k, 0] = Vector2.Distance(stabPos - value * num2, self.impaleChunk.pos);
                    if (self.impaleChunk.rotationChunk != null)
                    {
                        self.impaleDistances[k, 1] = Vector2.Distance(stabPos - value * num2, self.impaleChunk.rotationChunk.pos);
                    }
                }
                self.impaleChunk.vel += value * 12f / self.impaleChunk.mass;
                self.impaleChunk.pos += value * 7f / self.impaleChunk.mass;
                for (int l = 0; l < self.TotalSegments; l++)
                {
                    self.SetSegmentVel(l, Vector2.ClampMagnitude(self.GetSegmentVel(l), 6f));
                }
                self.room.PlaySound(SoundID.Big_Needle_Worm_Impale_Creature, stabPos, self.abstractCreature);
                self.stuckTime = 0f;
            }
        }
    }

    public static void On_AbstractCreature_Update(On.AbstractCreature.orig_Update orig, AbstractCreature self, int time)
    {
        orig(self, time);
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && self.IsLocal() && menagerie.abstractAvatars.Contains(self))
        {
            menagerie.storyClientData.isDead = self.state.dead || (self.realizedCreature != null && CreatureController.creatureControllers.TryGetValue(self.realizedCreature, out var crit) && crit.isStory(out var scc) && scc.dangerGraspTime >= 60) || (self.state is PlayerState state && state.permaDead);
        }
    }

    public static bool StabPoppycorn(BigNeedleWorm self, Vector2 lastFangPos, Vector2 fangPos)
    {

        
        return false;
    }

    public static void IL_BigNeedleWorm_Swish(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
                x => x.MatchLdloc(4),
                x => x.MatchLdloca(8),
                x => x.MatchLdcR4(1f),
                x => x.MatchLdcI4(1),
                x => x.MatchLdarg(0)
            );
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 4);
            c.Emit(OpCodes.Ldloc, 5);
            c.EmitDelegate(StabPoppycorn);
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Brtrue, skip);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(3),
                x => x.MatchLdloc(2),
                x => x.MatchCallOrCallvirt(out var _),
                x => x.MatchNewobj(out var _),
                x => x.MatchStfld<BigNeedleWorm>(nameof(BigNeedleWorm.swishAdd))
            );
            c.MarkLabel(skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static Color On_EggBug_ShortCutColor(On.EggBug.orig_ShortCutColor orig, EggBug self)
    {
        if (OnlineManager.lobby != null && CreatureController.creatureControllers.TryGetValue(self, out var cc) && cc.isStory(out var scc))
        {
            return scc.storyCustomization.bodyColor;
        }
        return orig(self);
    }

    public static void On_JetFish_CarryObject(On.JetFish.orig_CarryObject orig, JetFish self, bool eu)
    {
        if (CreatureController.creatureControllers.TryGetValue(self, out var cc))
        {
            self.grasps[0].grabbedChunk.MoveFromOutsideMyUpdate(eu, self.mainBodyChunk.pos + Custom.DirVec(self.bodyChunks[1].pos, self.mainBodyChunk.pos) * 10f);
            self.grasps[0].grabbedChunk.vel = self.mainBodyChunk.vel;
            return;
        }
        orig(self, eu);
    }

    public static void On_ScavengerGraphics_Update(On.ScavengerGraphics.orig_Update orig, ScavengerGraphics self)
    {
        orig(self);
        // mostly copy pasted code from playergraphics
        if (self.scavenger != null && CreatureController.creatureControllers.TryGetValue(self.scavenger, out var cc) && cc is StoryScavengerController scavy && scavy.handOnExternalFoodSource != default(Vector2))
        {
            var hand = (scavy.handOnExternalFoodSource.x < self.scavenger.mainBodyChunk.pos.x) ? 0 : 1;
            if (scavy.eatExternalFoodSourceCounter < 3)
            {
                self.hands[hand].absoluteHuntPos = self.scavenger.mainBodyChunk.pos;
                self.blink = Math.Max(self.blink, 3);
            }
            else
            {
                self.hands[hand].absoluteHuntPos = scavy.handOnExternalFoodSource;
            }
            var drawPos = (Vector2)(Custom.DirVec(self.drawPositions[0, 0], scavy.handOnExternalFoodSource) * 5f);
            self.drawPositions[0, 0].x += drawPos.x;
            self.drawPositions[0, 0].y += drawPos.y;
            self.scavenger.mainBodyChunk.vel += Custom.DirVec(self.drawPositions[0, 0], scavy.handOnExternalFoodSource) * 2f;
        }
    }

    public static void On_Scavenger_Update(On.Scavenger.orig_Update orig, Scavenger self, bool eu)
    {
        var oldEnergy = self.abstractCreature.personality.energy;
        if (CreatureController.creatureControllers.TryGetValue(self, out var cc))
        {
            self.abstractCreature.personality.energy = 1f;
        }
        orig(self, eu);
        if (cc != null)
        {
            self.abstractCreature.personality.energy = oldEnergy;
        }
    }
}
