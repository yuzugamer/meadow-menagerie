using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RainMeadow;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using RWCustom;
using System.Collections.Generic;
using System.Threading;
using VoidSea;
using JetBrains.Annotations;

namespace StoryMenagerie;

public static class StoryHooks
{
    public static ConditionalWeakTable<OracleBehavior, Creature> oracleCrits = new ConditionalWeakTable<OracleBehavior, Creature>();
    public static ConditionalWeakTable<VoidWorm, Creature> voidWormCrits = new();
    public static void Apply()
    {
        IL.GhostCreatureSedater.Update += IL_GhostCreatureSedator_Update;
        IL.Ghost.Update += IL_Ghost_Update;
        On.Oracle.Collide += On_Oracle_Collide;
        On.OracleBehavior.ctor += On_OracleBehavior_ctor;
        On.OracleBehavior.FindPlayer += On_OracleBehavior_FindPlayer;
        IL.Oracle.OracleArm.Update += IL_OracleArm_Update;
        IL.OracleGraphics.DrawSprites += IL_OracleGraphics_DrawSprites;
        IL.OracleGraphics.Update += IL_OracleGraphics_Update;
        IL.SSOracleBehavior.BasePosScore += ReplaceOraclePlayerHook;
        IL.SSOracleBehavior.CommunicatePosScore += ReplaceOraclePlayerHook;
        //IL.SSOracleBehavior.Move += ReplaceOraclePlayerHook;
        On.SSOracleBehavior.Move += Move;
        IL.SSOracleBehavior.SeePlayer += IL_SSOracleBehavior_SeePlayer;
        IL.SSOracleBehavior.Update += IL_SSOracleBehavior_Update;
        IL.SSOracleBehavior.SSOracleMeetWhite.ShowMediaMovementBehavior += IL_SSSubBehavior_ReplaceTwo;
        IL.SSOracleBehavior.SSOracleMeetWhite.ShowMediaScore += IL_SSSubBehavior_ReplaceTwo;
        IL.SSOracleBehavior.SSOracleMeetWhite.Update += IL_SSOracleMeetWhite_Update;
        IL.SSOracleBehavior.ThrowOutBehavior.Update += IL_SSOracleThrowOut_Update;
        IL.SLOracleBehavior.InitCutsceneObjects += ReplaceOraclePlayerHook;
        IL.SLOracleBehavior.BasePosScore += ReplaceOraclePlayerHook;
        IL.SLOracleBehavior.CommunicatePosScore += ReplaceOraclePlayerHook;
        IL.SLOracleBehavior.Move += ReplaceOraclePlayerHook;
        IL.SLOracleBehavior.ShowMediaScore += ReplaceOraclePlayerHook;
        IL.SLOracleBehavior.Update += ReplaceOraclePlayerHook;
        IL.SLOracleBehaviorHasMark.InitateConversation += ReplaceOraclePlayerHook;
        IL.SLOracleBehaviorHasMark.Update += IL_SLOracleBehaviorHasMark_Update;
        //On.SLOracleBehaviorHasMark.Update += hasmarkUpdate;
        IL.SLOracleBehaviorNoMark.Update += ReplaceOraclePlayerHook;
        On.SLOracleBehavior.Update += On_SLOracleBehavior_Update;
        IL.VoidSea.VoidSeaScene.Update += IL_VoidSeaScene_Update;
        IL.VoidSea.VoidWorm.Update += IL_VoidWorm_Update;
        //IL.VoidSea.VoidWorm.BackgroundWormBehavior.Update += ReplaceVoidWormBehaviorPlayerHook;
        //IL.VoidSea.VoidWorm.MainWormBehavior.Update += IL_MainWormBehavior_Update;
    }

    private static float BasePosScore(On.SSOracleBehavior.orig_BasePosScore orig, SSOracleBehavior self, Vector2 tryPos)
    {
        if (oracleCrits.TryGetValue(self, out var crit))
        {
            if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Meditate || crit == null)
            {
                return Vector2.Distance(tryPos, self.oracle.room.MiddleOfTile(24, 5));
            }
            if (self.movementBehavior == SSOracleBehavior.MovementBehavior.ShowMedia)
            {
                return -Vector2.Distance(crit.DangerPos, tryPos);
            }
            return Mathf.Abs(Vector2.Distance(self.nextPos, tryPos) - 200f) + RWCustom.Custom.LerpMap(Vector2.Distance(crit.DangerPos, tryPos), 40f, 300f, 800f, 0f);
        }
        return orig(self, tryPos);
    }


    public static bool CreatureIsAvatar(GhostCreatureSedater self, int i)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            var creature = self.room.abstractRoom.creatures[i];
            if (CreatureController.creatureControllers.TryGetValue(creature.realizedCreature, out var _) || menagerie.abstractAvatars.Contains(creature))
            {
                return true;
            }
        }
        return false;
    }

    public static void IL_GhostCreatureSedator_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<GhostCreatureSedater>(nameof(GhostCreatureSedater.den)),
                x => x.MatchCallOrCallvirt(out var _),
                x => x.MatchBrfalse(out var _)
            );
            // continue if creature is avatar
            c.MoveAfterLabels();
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 2);
            c.EmitDelegate(CreatureIsAvatar);
            c.Emit(OpCodes.Brtrue, skip);
            c.GotoNext(
                x => x.MatchLdloc(out var _),
                x => x.MatchLdcI4(1),
                x => x.MatchAdd(),
                x => x.MatchStloc(out var _),
                x => x.MatchLdloc(out var _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
                x => x.MatchCallOrCallvirt<Room>("get_abstractRoom"),
                x => x.MatchLdfld<AbstractRoom>(nameof(AbstractRoom.creatures)),
                x => x.MatchCallOrCallvirt(out var _),
                x => x.MatchBlt(out var _)
            );
            c.MarkLabel(skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static AbstractCreature FirstAliveAvatar(AbstractCreature orig)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            var avatars = menagerie.clientSettings.avatars;
            if (avatars != null && avatars.Count > 0 && avatars[0].FindEntity() is OnlineCreature oc) {
                return oc.abstractCreature;
            }
        }
        return orig;
    }

    public static void IL_Ghost_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
                x => x.MatchLdfld<Room>(nameof(Room.game)),
                x => x.MatchCallOrCallvirt<RainWorldGame>("get_FirstAlivePlayer")
            );
            c.MoveAfterLabels();
            c.EmitDelegate(FirstAliveAvatar);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    // copy pasted code, with player isinsts replaced
    public static void On_Oracle_Collide(On.Oracle.orig_Collide orig, Oracle self, PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MenagerieGameMode)
        {
            orig(self, otherObject, myChunk, otherChunk);
            return;
        }
        if (self.Consious && otherObject is Creature crit && CreatureController.creatureControllers.TryGetValue(crit, out var _) && self.oracleBehavior is SLOracleBehaviorHasMark behavior)
        {
            bool annoying = true;
            if (behavior.State.SpeakingTerms)
            {
                for (int i = crit.grasps.Length - 1; i >= 0; i--)
                {
                    if (crit.grasps[i] != null && behavior.currentConversation == null && behavior.WillingToInspectItem(crit.grasps[i].grabbed))
                    {
                        bool alreadyRead = false;
                        for (int j = 0; j < behavior.pickedUpItemsThisRealization.Count; j++)
                        {
                            if (behavior.pickedUpItemsThisRealization[j] == crit.grasps[i].grabbed.abstractPhysicalObject.ID)
                            {
                                alreadyRead = true;
                                break;
                            }
                        }
                        if (!alreadyRead)
                        {
                            behavior.GrabObject(crit.grasps[i].grabbed);
                            crit.ReleaseGrasp(i);
                        }
                        annoying = false;
                        break;
                    }
                }
            }
            if (annoying)
            {
                behavior.playerAnnoyingCounter++;
            }
        }
    }

    public static void On_OracleBehavior_ctor(On.OracleBehavior.orig_ctor orig, OracleBehavior self, Oracle oracle)
    {
        orig(self, oracle);
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            Creature crit = null;
            // not really sure what the point of these checks are, but they're in the base game so might as well keep them
            if (self.oracle != null && self.oracle.room != null && self.oracle.room.game != null)
            {
                foreach (var acrit in self.oracle.room.abstractRoom.creatures)
                {
                    if (acrit.realizedCreature != null && (CreatureController.creatureControllers.TryGetValue(acrit.realizedCreature, out var _) || acrit.realizedCreature is Player) && acrit.state.alive)
                    {
                        crit = acrit.realizedCreature;
                        break;
                    }
                }
            }
            oracleCrits.Add(self, crit);
        }
    }

    public static void On_OracleBehavior_FindPlayer(On.OracleBehavior.orig_FindPlayer orig, OracleBehavior self)
    {
        if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MenagerieGameMode menagerie)
        {
            orig(self);
            return;
        }
        if (self.oracle.room != null  && menagerie.clientSettings.avatars != null && menagerie.clientSettings.avatars.Count > 0 && menagerie.clientSettings.avatars[0].FindEntity() is OnlineCreature oc && oc.realizedCreature != null)
        {
            bool flag = false;
            if (self.oracle.ID == Oracle.OracleID.SS && self.oracle.room.game.StoryCharacter == SlugcatStats.Name.Red && !self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.pebblesSeenGreenNeuron && self.PlayerWithNeuronInStomach != null)
            {
                return;
            }
            var avi = oc.realizedCreature;
            if (oracleCrits.TryGetValue(self, out var _))
            {
                oracleCrits.Remove(self);
                oracleCrits.Add(self, avi);
            } 
            var inRoom = self.oracle.room.abstractRoom.creatures.Where(acrit => menagerie.abstractAvatars.Contains(acrit) && acrit.realizedCreature != null).Select(acrit => acrit.realizedCreature).ToArray();
            if (avi == null || avi.room != self.oracle.room || avi.inShortcut)
            {
                avi = ((inRoom.Length > 0) ? inRoom[0] : null);
                if (avi != null)
                {
                    int num = 1;
                    while (!flag && avi.inShortcut && num < inRoom.Length)
                    {
                        avi = inRoom[num];
                        num++;
                    }
                }
            }
            if (inRoom.Length > 0 && avi.dead && avi == inRoom[0])
            {
                avi = null;
            }
            if (avi != null)
            {
                self.oracle.room.game.cameras[0].EnterCutsceneMode(avi.abstractCreature, RoomCamera.CameraCutsceneType.Oracle);
            }
        }
    }

    public static Creature CritOrSlug(Creature orig, OracleBehavior self) => OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode ? (oracleCrits.TryGetValue(self, out var crit) ? crit : null) : orig;
    public static bool ReplaceOraclePlayer(ILCursor c)
    {
        if (c.TryGotoNext(
            MoveType.After,
            x => x.MatchLdfld<OracleBehavior>(nameof(OracleBehavior.player))
        ))
        {
        c.MoveAfterLabels();
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate(CritOrSlug);
            StoryMenagerie.Debug("Oracle player field successfully replaced");
            return true;
        }
        StoryMenagerie.LogError("Oracle player was not replaced!");
        return false;
    }

    public static void ReplaceOraclePlayerHook(ILContext il)
    {
        var c = new ILCursor(il);
        while (true)
        {
            if (!ReplaceOraclePlayer(c)) break;
        }
    }

    public static void IL_OracleArm_Update(ILContext il)
    {
        var c = new ILCursor(il);
            ReplaceOraclePlayer(c);
            ReplaceOraclePlayer(c);
    }

    public static void IL_OracleGraphics_DrawSprites(ILContext il)
    {
        var c = new ILCursor(il);
        while (true)
        {
            if (!ReplaceOraclePlayer(c)) break;
        }
    }

    public static void IL_OracleGraphics_Update(ILContext il)
    {
        var c = new ILCursor(il);
            for (int i = 0; i < 17; i++)
            {
                ReplaceOraclePlayer(c);
        }
    }

    public static bool CritIsNotSlug(OracleBehavior self) => OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode && (!oracleCrits.TryGetValue(self, out var crit) || crit is not Player);

    public static void IL_SSOracleBehavior_SeePlayer(ILContext il)
    {
        var c = new ILCursor(il);
        var skip = c.DefineLabel();
        c.GotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<SSOracleBehavior>(nameof(SSOracleBehavior.greenNeuron)),
            x => x.MatchBrtrue(out var _),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<OracleBehavior>(nameof(OracleBehavior.player)),
            x => x.MatchLdfld<Player>(nameof(Player.objectInStomach)),
            x => x.MatchBrfalse(out skip)
        );
        c.MoveAfterLabels();
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate(CritIsNotSlug);
        c.Emit(OpCodes.Brtrue, skip);
        for (int i = 0; i < 2; i++) {
            ReplaceOraclePlayer(c);
            // irresponsible
            // "irresponsible" ??
            c.Emit(OpCodes.Isinst, typeof(Player));
        }
    }

    public static bool SpearGiveMark(SSOracleBehavior self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode && oracleCrits.TryGetValue(self, out var crit))
        {
            if (self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear && self.oracle.ID == Oracle.OracleID.SS)
            {
                var slug = crit as Player;
                if (slug != null && crit.graphicsModule is PlayerGraphics graphics)
                {
                    if (graphics.bodyPearl != null)
                    {
                        graphics.bodyPearl.visible = false;
                        graphics.bodyPearl.scarVisible = true;
                    }
                    slug.Regurgitate();
                    slug.aerobicLevel = 1.1f;
                    slug.exhausted = true;
                    slug.SetMalnourished(true);
                }
                if (self.SMCorePearl == null)
                {
                    int n = 0;
                    while (n < self.oracle.room.updateList.Count)
                    {
                        if (self.oracle.room.updateList[n] is SpearMasterPearl)
                        {
                            self.SMCorePearl = (self.oracle.room.updateList[n] as SpearMasterPearl);
                            if (AbstractPhysicalObject.UsesAPersistantTracker(self.SMCorePearl.abstractPhysicalObject))
                            {
                                (self.oracle.room.game.session as StoryGameSession).AddNewPersistentTracker(self.SMCorePearl.abstractPhysicalObject);
                                break;
                            }
                            break;
                        }
                        else
                        {
                            n++;
                        }
                    }
                }
                if (self.SMCorePearl != null)
                {
                    self.SMCorePearl.firstChunk.vel *= 0f;
                    self.SMCorePearl.DisableGravity();
                    self.afterGiveMarkAction = MoreSlugcatsEnums.SSOracleBehaviorAction.MeetPurple_GetPearl;
                }
                else
                {
                    self.afterGiveMarkAction = SSOracleBehavior.Action.General_Idle;
                }
                if (ModManager.CoopAvailable)
                {
                    self.StunCoopPlayers(60);
                }
                else
                {
                    crit.Stun(60);
                }
            }
            return true;
        }
        return false;
    }

    public static bool OracleAddFood(SSOracleBehavior self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            if (OnlineManager.lobby.isOwner)
            {
                menagerie.foodPoints += 10;
            }
            else
            {
                MenagerieGameMode.ChangeFood(10);
            }
            return true;
        }
        return false;
    }

    // i might cry
    public static void IL_SSOracleBehavior_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            // 21
            for (int i = 0; i < 20; i++)
            {
                ReplaceOraclePlayer(c);
            }
            var skip = c.DefineLabel();
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.MSC)),
                x => x.MatchBrfalse(out skip)
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(SpearGiveMark);
            c.Emit(OpCodes.Brtrue, skip);

            for (int i = 0; i < 3; i++)
            {
                ReplaceOraclePlayer(c);
            }

            // pain
            for (int i = 0; i < 3; i++)
            {
                c.GotoNext(x => x.MatchLdfld<OracleBehavior>(nameof(OracleBehavior.player)));
            }

            for (int i = 0; i < 2; i++)
            {
                ReplaceOraclePlayer(c);
            }



            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<OracleBehavior>(nameof(OracleBehavior.player)),
                x => x.MatchLdcI4(10),
                x => x.MatchCallOrCallvirt<Player>(nameof(Player.AddFood))
            );
            c.MoveAfterLabels();
            var skip2 = c.DefineLabel();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(OracleAddFood);
            c.Emit(OpCodes.Brtrue, skip2);
            c.Index += 5;
            c.MarkLabel(skip2);

            ReplaceOraclePlayer(c);
            for (int i = 0; i < 2; i++)
            {
                c.GotoNext(x => x.MatchLdfld<OracleBehavior>(nameof(OracleBehavior.player)));
            }
            ReplaceOraclePlayer(c);

            var skip3 = c.DefineLabel();
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdfld<OracleBehavior>(nameof(OracleBehavior.inActionCounter)),
                x => x.MatchLdcI4(0x12C),
                x => x.MatchBle(out skip3)
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(CritIsNotSlug);
            c.Emit(OpCodes.Brtrue, skip3);
            for (int i = 0; i < 3; i++)
            {
                c.GotoNext(x => x.MatchLdfld<OracleBehavior>(nameof(OracleBehavior.player)));
            }
            
            for (int i = 0; i < 2; i++)
            {
                ReplaceOraclePlayer(c);
            }
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static Creature SSSubCritOrSlug(Creature orig, SSOracleBehavior.SubBehavior self) => OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode && oracleCrits.TryGetValue(self.owner, out var crit) ? crit : orig;

    public static void ReplaceOracleSubGetPlayer(ILCursor c)
    {
        c.TryGotoNext(
            MoveType.After,
            x => x.MatchCallOrCallvirt<SSOracleBehavior.SubBehavior>("get_player")
        );
        c.MoveAfterLabels();
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate(SSSubCritOrSlug);
        StoryMenagerie.Debug("Oracle player field successfully replaced");
    }

    public static void IL_SSSubBehavior_ReplaceTwo(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            ReplaceOracleSubGetPlayer(c);
            ReplaceOracleSubGetPlayer(c);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void IL_SSOracleMeetWhite_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            for (int i = 0; i < 12; i++)
            {
                ReplaceOracleSubGetPlayer(c);
            }

            // move past player graphics stuff
            for (int i = 0; i < 3; i++)
            {
                c.GotoNext(x => x.MatchCallOrCallvirt<SSOracleBehavior.SubBehavior>("get_player"));
            }

            for (int i = 0; i < 2; i++)
            {
                ReplaceOracleSubGetPlayer(c);
            }

            // why
            for (int i = 0; i < 2; i++)
            {
                c.GotoNext(x => x.MatchCallOrCallvirt<SSOracleBehavior.SubBehavior>("get_player"));
            }

            for (int i = 0; i < 2; i++)
            {
                ReplaceOracleSubGetPlayer(c);
            }
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    // literally why
    public static void IL_SSOracleThrowOut_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            for (int i = 0; i < 11; i++)
            {
                ReplaceOracleSubGetPlayer(c);
            }

            FieldReference owner = null;
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<SSOracleBehavior.SubBehavior>("get_player"),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(out owner),
                x => x.MatchLdfld<SSOracleBehavior>(nameof(SSOracleBehavior.greenNeuron)),
                x => x.MatchLdcI4(1),
                x => x.MatchCallOrCallvirt<Player>(nameof(Player.SlugcatGrab))
            );
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, owner);
            c.EmitDelegate(CritIsNotSlug);
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Brtrue, skip);
            c.Index += 8;
            c.MarkLabel(skip);

            //22
            //49
            //59
            for (int i = 0; i < 59; i++)
            {
                ReplaceOracleSubGetPlayer(c);
            }

            var skip2 = c.DefineLabel();
            c.GotoNext(
                x => x.MatchCallOrCallvirt<Vector2>(nameof(Vector2.Distance)),
                x => x.MatchLdcR4(10),
                x => x.MatchBgeUn(out skip2)
            );
            c.GotoPrev(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<SSOracleBehavior.SubBehavior>("get_oracle"),
                x => x.MatchLdfld<Oracle>(nameof(Oracle.oracleBehavior))
            );
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, owner);
            c.EmitDelegate(CritIsNotSlug);
            c.Emit(OpCodes.Brtrue, skip2);
            ReplaceOracleSubGetPlayer(c);
            for (int i = 0; i < 4; i++)
            {
                ReplaceOracleSubGetPlayer(c);
                c.Emit(OpCodes.Isinst, typeof(Player));
            }

            for (int i = 0; i < 15; i++)
            {
                ReplaceOracleSubGetPlayer(c);
            }

            // repeated
            var skip3 = c.DefineLabel();
            c.GotoNext(
                x => x.MatchCallOrCallvirt<Vector2>(nameof(Vector2.Distance)),
                x => x.MatchLdcR4(10),
                x => x.MatchBgeUn(out skip3)
            );
            c.GotoPrev(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<SSOracleBehavior.SubBehavior>("get_oracle"),
                x => x.MatchLdfld<Oracle>(nameof(Oracle.oracleBehavior))
            );
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, owner);
            c.EmitDelegate(CritIsNotSlug);
            c.Emit(OpCodes.Brtrue, skip3);
            ReplaceOracleSubGetPlayer(c);
            for (int i = 0; i < 4; i++)
            {
                ReplaceOracleSubGetPlayer(c);
                c.Emit(OpCodes.Isinst, typeof(Player));
            }

            for (int i = 0; i < 9; i++)
            {
                ReplaceOracleSubGetPlayer(c);
            }
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    // what even
    // lazy, should replace with il hook once i get around to figuring out why il hook didn't work for this
    // fix pebbles floating in the corner like an idiot
    public static void Move(On.SSOracleBehavior.orig_Move orig, SSOracleBehavior self)
    {
        if (!oracleCrits.TryGetValue(self, out var crit))
        {
            orig(self);
        }
        if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Idle)
        {
            self.invstAngSpeed = 1f;
            if (self.investigateMarble == null && self.oracle.marbles.Count > 0)
            {
                self.investigateMarble = self.oracle.marbles[UnityEngine.Random.Range(0, self.oracle.marbles.Count)];
            }
            if (self.investigateMarble != null && (self.investigateMarble.orbitObj == self.oracle || Custom.DistLess(new Vector2(250f, 150f), self.investigateMarble.firstChunk.pos, 100f)))
            {
                self.investigateMarble = null;
            }
            if (self.investigateMarble != null)
            {
                self.lookPoint = self.investigateMarble.firstChunk.pos;
                if (Custom.DistLess(self.nextPos, self.investigateMarble.firstChunk.pos, 100f))
                {
                    self.floatyMovement = true;
                    self.nextPos = self.investigateMarble.firstChunk.pos - Custom.DegToVec(self.investigateAngle) * 50f;
                }
                else
                {
                    self.SetNewDestination(self.investigateMarble.firstChunk.pos - Custom.DegToVec(self.investigateAngle) * 50f);
                }
                if (self.pathProgression == 1f && UnityEngine.Random.value < 0.005f)
                {
                    self.investigateMarble = null;
                }
            }
            if (ModManager.MSC && self.oracle.ID == MoreSlugcatsEnums.OracleID.DM && UnityEngine.Random.value < 0.001f)
            {
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
            }
        }
        else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Meditate)
        {
            if (self.nextPos != self.oracle.room.MiddleOfTile(24, 17))
            {
                self.SetNewDestination(self.oracle.room.MiddleOfTile(24, 17));
            }
            self.investigateAngle = 0f;
            self.lookPoint = self.oracle.firstChunk.pos + new Vector2(0f, -40f);
            if (ModManager.MMF && UnityEngine.Random.value < 0.001f)
            {
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
            }
        }
        else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.KeepDistance)
        {
            if (crit == null)
            {
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
            }
            else
            {
                self.lookPoint = crit.DangerPos;
                Vector2 vector = new Vector2(UnityEngine.Random.value * self.oracle.room.PixelWidth, UnityEngine.Random.value * self.oracle.room.PixelHeight);
                if (!self.oracle.room.GetTile(vector).Solid && self.oracle.room.aimap.getTerrainProximity(vector) > 2 && Vector2.Distance(vector, crit.DangerPos) > Vector2.Distance(self.nextPos, crit.DangerPos) + 100f)
                {
                    self.SetNewDestination(vector);
                }
            }
        }
        else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Investigate)
        {
            if (crit == null)
            {
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
            }
            else
            {
                self.lookPoint = crit.DangerPos;
                if (self.investigateAngle < -90f || self.investigateAngle > 90f || (float)self.oracle.room.aimap.getTerrainProximity(self.nextPos) < 2f)
                {
                    self.investigateAngle = Mathf.Lerp(-70f, 70f, UnityEngine.Random.value);
                    self.invstAngSpeed = Mathf.Lerp(0.4f, 0.8f, UnityEngine.Random.value) * ((UnityEngine.Random.value < 0.5f) ? -1f : 1f);
                }
                Vector2 vector = crit.DangerPos + Custom.DegToVec(self.investigateAngle) * 150f;
                if ((float)self.oracle.room.aimap.getTerrainProximity(vector) >= 2f)
                {
                    if (self.pathProgression > 0.9f)
                    {
                        if (Custom.DistLess(self.oracle.firstChunk.pos, vector, 30f))
                        {
                            self.floatyMovement = true;
                        }
                        else if (!Custom.DistLess(self.nextPos, vector, 30f))
                        {
                            self.SetNewDestination(vector);
                        }
                    }
                    self.nextPos = vector;
                }
            }
        }
        else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.Talk)
        {
            if (crit == null)
            {
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
            }
            else
            {
                self.lookPoint = crit.DangerPos;
                Vector2 vector = new Vector2(UnityEngine.Random.value * self.oracle.room.PixelWidth, UnityEngine.Random.value * self.oracle.room.PixelHeight);
                if (self.CommunicatePosScore(vector) + 40f < self.CommunicatePosScore(self.nextPos) && !Custom.DistLess(vector, self.nextPos, 30f))
                {
                    self.SetNewDestination(vector);
                }
            }
        }
        else if (self.movementBehavior == SSOracleBehavior.MovementBehavior.ShowMedia)
        {
            if (self.currSubBehavior is SSOracleBehavior.SSOracleMeetWhite)
            {
                (self.currSubBehavior as SSOracleBehavior.SSOracleMeetWhite).ShowMediaMovementBehavior();
            }
            else if (ModManager.MSC && self.currSubBehavior is SSOracleBehavior.SSOracleMeetGourmand)
            {
                (self.currSubBehavior as SSOracleBehavior.SSOracleMeetGourmand).ShowMediaMovementBehavior();
            }
        }
        if (self.currSubBehavior != null && self.currSubBehavior.LookPoint != null)
        {
            self.lookPoint = self.currSubBehavior.LookPoint.Value;
        }
        self.consistentBasePosCounter++;
        if (self.oracle.room.readyForAI)
        {
            Vector2 vector = new Vector2(UnityEngine.Random.value * self.oracle.room.PixelWidth, UnityEngine.Random.value * self.oracle.room.PixelHeight);
            if (!self.oracle.room.GetTile(vector).Solid && self.BasePosScore(vector) + 40f < self.BasePosScore(self.baseIdeal))
            {
                self.baseIdeal = vector;
                self.consistentBasePosCounter = 0;
                return;
            }
        }
        else
        {
            self.baseIdeal = self.nextPos;
        }
    }

    public static void IL_SLOracleBehaviorHasMark_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            ReplaceOraclePlayer(c);
            ReplaceOraclePlayer(c);
            var skip = c.DefineLabel();
            c.GotoNext(
                MoveType.After,
                x => x.MatchBrfalse(out skip)
            );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(CritIsNotSlug);
            c.Emit(OpCodes.Brtrue, skip);
            for (int i = 0; i < 5; i++)
            {
                ReplaceOraclePlayer(c);
                c.Emit(OpCodes.Isinst, typeof(Player));
            }
            while (true)
            {
                if (!ReplaceOraclePlayer(c)) break;
            }
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void SLOracleBehavior_BaseUpdate(SLOracleBehavior self, bool eu)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && self.oracle.room != null)
        {
            var playersInRoom = menagerie.realizedAvatars.Where(avi => avi.room == self.oracle.room).OrderBy(crit => crit.mainBodyChunk.pos.x).ToArray();
            if (playersInRoom.Length > 0)
            {
                if (oracleCrits.TryGetValue(self, out var _))
                {
                    oracleCrits.Remove(self);
                }
                oracleCrits.Add(self, playersInRoom[0]);
            }
        }
        if (self.voice != null)
        {
            self.voice.alive = true;
            if (self.voice.slatedForDeletetion)
            {
                self.voice = null;
            }
        }
        if (ModManager.MSC && self.oracle.room != null && self.oracle.room.game.rainWorld.safariMode)
        {
            self.safariCreature = null;
            float num = float.MaxValue;
            for (int i = 0; i < self.oracle.room.abstractRoom.creatures.Count; i++)
            {
                if (self.oracle.room.abstractRoom.creatures[i].realizedCreature != null)
                {
                    Creature realizedCreature = self.oracle.room.abstractRoom.creatures[i].realizedCreature;
                    float num2 = Custom.Dist(self.oracle.firstChunk.pos, realizedCreature.mainBodyChunk.pos);
                    if (num2 < num)
                    {
                        num = num2;
                        self.safariCreature = realizedCreature;
                    }
                }
            }
        }
        self.FindPlayer();
        oracleCrits.TryGetValue(self, out var crit);
        self.InitCutsceneObjects();
        StoryMenagerie.Debug("sloraclebehavior is " + crit);
        if (ModManager.MSC)
        {
            if (!self.initiated)
            {
                if (self.oracle.myScreen == null)
                {
                    self.oracle.myScreen = new OracleProjectionScreen(self.oracle.room, self);
                }
                self.initiated = true;
            }
            if (self.oracle.room.game.IsMoonActive())
            {
                self.oracle.room.gravity = 0.2f;
            }
        }
        if (self.stillWakingUp)
        {
            self.dontHoldKnees = Math.Max(self.dontHoldKnees, 620);
        }
        else
        {
            self.oracle.health = Mathf.InverseLerp(0f, 5f, (float)self.State.neuronsLeft);
        }
        if (self.dontHoldKnees > 0)
        {
            self.dontHoldKnees--;
        }
        if (self.InSitPosition && self.oracle.arm != null)
        {
            for (int i = 0; i < self.oracle.arm.joints.Length; i++)
            {
                if (self.oracle.arm.joints[i].vel.magnitude > 0.05f)
                {
                    self.oracle.arm.joints[i].vel *= 0.98f;
                }
            }
        }
        self.moonActive = self.oracle.room.game.IsMoonActive();
        if (!self.oracle.Consious)
        {
            self.forceFlightMode = false;
            if (ModManager.MSC)
            {
                self.oracle.SetLocalGravity(1f);
            }
            return;
        }
        if (ModManager.MSC)
        {
            if (!self.forceFlightMode)
            {
                if (self.oracle.room.game.IsMoonActive() && crit != null && crit.mainBodyChunk.pos.x >= 1430f && crit.mainBodyChunk.pos.x <= 1560f && self.oracle.firstChunk.pos.x > crit.mainBodyChunk.pos.x)
                {
                    self.timeOutOfSitZone = 0;
                }
                else if (!self.oracle.room.game.IsMoonActive() && crit != null && crit.mainBodyChunk.pos.x >= 1430f && crit.mainBodyChunk.pos.x <= 1660f && self.oracle.firstChunk.pos.x > crit.mainBodyChunk.pos.x)
                {
                    self.timeOutOfSitZone = 0;
                }
                else
                {
                    self.timeOutOfSitZone++;
                }
                if (self.WantsToSit && crit != null)
                {
                    self.moonActive = false;
                    self.setMovementBehavior(SLOracleBehavior.MovementBehavior.InvestigateSlugcat);
                    self.invstAngSpeed = 0.2;
                    self.lookPoint = ((crit.mainBodyChunk.pos.x <= self.oracle.room.PixelWidth * 0.85f) ? new Vector2(crit.mainBodyChunk.pos.x + 100f, crit.mainBodyChunk.pos.y + 150f) : new Vector2(crit.mainBodyChunk.pos.x - 100f, crit.mainBodyChunk.pos.y + 150f));
                    if (self.oracle.room.game.IsMoonActive())
                    {
                        self.oracle.SetLocalGravity(Mathf.Lerp(self.oracle.gravity, 0f, 0.04f));
                    }
                    else
                    {
                        self.oracle.SetLocalGravity(Mathf.Lerp(self.oracle.gravity, 0.9f, 0.04f));
                    }
                }
            }
            if ((self.movementBehavior == SLOracleBehavior.MovementBehavior.ShowMedia || self.movementBehavior == SLOracleBehavior.MovementBehavior.KeepDistance) && self.oracle.room.game.IsMoonActive())
            {
                self.moonActive = true;
                self.oracle.SetLocalGravity(Mathf.Lerp(self.oracle.gravity, 1f, 0.2f));
            }
        }
        for (int j = 0; j < self.oracle.room.abstractRoom.entities.Count; j++)
        {
            if (self.oracle.room.abstractRoom.entities[j] is AbstractPhysicalObject && (self.oracle.room.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject != null && Custom.DistLess((self.oracle.room.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject.firstChunk.pos, self.oracle.firstChunk.pos, 500f) && (self.oracle.room.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject.grabbedBy.Count == 0 && (self.oracle.room.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject is OracleSwarmer)
            {
                OracleSwarmer oracleSwarmer = (self.oracle.room.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject as OracleSwarmer;
                oracleSwarmer.affectedByGravity = Mathf.InverseLerp(300f, 500f, Vector2.Distance(oracleSwarmer.firstChunk.pos, self.oracle.firstChunk.pos));
                if (self.reelInSwarmer == null && oracleSwarmer is SSOracleSwarmer && self.holdingObject == null)
                {
                    self.reelInSwarmer = (oracleSwarmer as SSOracleSwarmer);
                }
            }
        }
        if (self.reelInSwarmer != null && self.holdingObject == null)
        {
            self.swarmerReelIn = Mathf.Min(1f, self.swarmerReelIn + 0.016666668f);
            self.reelInSwarmer.firstChunk.vel *= Custom.LerpMap(self.swarmerReelIn, 0.4f, 1f, 1f, 0.3f, 6f);
            self.reelInSwarmer.firstChunk.vel += Custom.DirVec(self.reelInSwarmer.firstChunk.pos, self.oracle.firstChunk.pos) * 3.2f * self.swarmerReelIn;
            if (Custom.DistLess(self.reelInSwarmer.firstChunk.pos, self.oracle.firstChunk.pos, 30f))
            {
                self.GrabObject(self.reelInSwarmer);
                self.reelInSwarmer = null;
            }
        }
        else
        {
            self.swarmerReelIn = 0f;
        }
        self.dehabilitateTime--;
        if (!self.hasNoticedPlayer)
        {
            if (self.safariCreature != null)
            {
                self.lookPoint = self.safariCreature.mainBodyChunk.pos;
            }
            else if (self.InSitPosition)
            {
                self.lookPoint = self.oracle.firstChunk.pos + new Vector2(-145f, -45f);
            }
            else
            {
                self.lookPoint = self.OracleGetToPos;
            }
            if (crit != null && crit.room == self.oracle.room && crit.mainBodyChunk.pos.x > 1160f)
            {
                self.hasNoticedPlayer = true;
                self.oracle.firstChunk.vel += Custom.DegToVec(45f) * 3f;
                self.oracle.bodyChunks[1].vel += Custom.DegToVec(-90f) * 2f;
            }
        }
        else if (ModManager.MSC && crit != null && crit.room == self.oracle.room && crit.mainBodyChunk.pos.x < 1125f && self.moonActive)
        {
            self.hasNoticedPlayer = false;
            self.idleCounter = 0.0;
        }
        if (ModManager.MSC && UnityEngine.Random.value < 0.033333335f)
        {
            self.idealShowMediaPos += Custom.RNV() * UnityEngine.Random.value * 30f;
            self.showMediaPos += Custom.RNV() * UnityEngine.Random.value * 30f;
            if (self.forcedShowMediaPos == null)
            {
                self.idealShowMediaPos = self.ClampMediaPos(self.idealShowMediaPos);
                self.showMediaPos = self.ClampMediaPos(self.showMediaPos);
            }
        }
        if (self.holdingObject != null)
        {
            if (!self.oracle.Consious || self.holdingObject.grabbedBy.Count > 0)
            {
                if (self is SLOracleBehaviorHasMark && self.holdingObject.grabbedBy.Count > 0)
                {
                    (self as SLOracleBehaviorHasMark).PlayerInterruptByTakingItem();
                }
                self.holdingObject = null;
            }
            else
            {
                self.holdingObject.firstChunk.MoveFromOutsideMyUpdate(eu, self.oracle.firstChunk.pos + new Vector2(-18f, -7f));
                self.holdingObject.firstChunk.vel *= 0f;
                if (self.holdingObject is SSOracleSwarmer && (self.oracle.room.game.cameras[0].hud.dialogBox == null || self.oracle.room.game.cameras[0].hud.dialogBox.messages.Count < 1))
                {
                    self.convertSwarmerCounter++;
                    if (self.convertSwarmerCounter > 40)
                    {
                        Vector2 pos = self.holdingObject.firstChunk.pos;
                        self.holdingObject.Destroy();
                        self.holdingObject = null;
                        SLOracleSwarmer sloracleSwarmer = new SLOracleSwarmer(new AbstractPhysicalObject(self.oracle.room.world, AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer, null, self.oracle.room.GetWorldCoordinate(pos), self.oracle.room.game.GetNewID()), self.oracle.room.world);
                        self.oracle.room.abstractRoom.entities.Add(sloracleSwarmer.abstractPhysicalObject);
                        sloracleSwarmer.firstChunk.HardSetPosition(pos);
                        self.oracle.room.AddObject(sloracleSwarmer);
                        self.ConvertingSSSwarmer();
                    }
                }
            }
        }
        if (self.moonActive)
        {
            self.UpdateActive(eu);
            self.oracle.arm.isActive = true;
            return;
        }
        if (self.InSitPosition)
        {
            if (self.holdingObject == null && self.dontHoldKnees < 1 && UnityEngine.Random.value < 0.025f && (crit == null || !Custom.DistLess(self.oracle.firstChunk.pos, crit.DangerPos, 50f)) && !self.protest && self.oracle.health >= 1f)
            {
                self.holdKnees = true;
            }
        }
        else
        {
            BodyChunk firstChunk = self.oracle.firstChunk;
            firstChunk.vel.x = firstChunk.vel.x + ((self.oracle.firstChunk.pos.x < self.OracleGetToPos.x) ? 1f : -1f) * 0.6f * self.CrawlSpeed;
            if (crit != null && crit.DangerPos.x < self.oracle.firstChunk.pos.x)
            {
                if (self.oracle.firstChunk.ContactPoint.x != 0)
                {
                    self.oracle.firstChunk.vel.y = Mathf.Lerp(self.oracle.firstChunk.vel.y, 1.2f, 0.5f) + 1.2f;
                }
                if (self.oracle.bodyChunks[1].ContactPoint.x != 0)
                {
                    self.oracle.firstChunk.vel.y = Mathf.Lerp(self.oracle.firstChunk.vel.y, 1.2f, 0.5f) + 1.2f;
                }
            }
            if (crit != null && !Custom.DistLess(self.oracle.firstChunk.pos, crit.DangerPos, 50f) && (self.oracle.bodyChunks[1].pos.y > 140f || crit.DangerPos.x < self.oracle.firstChunk.pos.x || Mathf.Abs(self.oracle.firstChunk.pos.x - self.oracle.firstChunk.lastPos.x) > 2f))
            {
                self.crawlCounter += 0.04f;
            }
            self.holdKnees = false;
        }
        if (self.oracle.arm.joints[2].pos.y < 140f)
        {
            self.oracle.arm.joints[2].pos.y = 140f;
            self.oracle.arm.joints[2].vel.y = Mathf.Abs(self.oracle.arm.joints[1].vel.y) * 0.2f;
        }
        self.oracle.WeightedPush(0, 1, new Vector2(0f, 1f), 4f * Mathf.InverseLerp(60f, 20f, Mathf.Abs(self.OracleGetToPos.x - self.oracle.firstChunk.pos.x)));
        self.oracle.arm.isActive = false;
        //orig(self, eu);
        StoryMenagerie.Debug("made it to the endof sloraclebehavior.update");
    }

    public static void On_SLOracleBehavior_Update(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
    {
        //SLOracleBehavior_BaseUpdate(self, eu);
        //return;
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && self.oracle.room != null)
        {
            var playersInRoom = menagerie.realizedAvatars.Where(avi => avi.room == self.oracle.room).OrderBy(crit => crit.mainBodyChunk.pos.x).ToArray();
            if (playersInRoom.Length > 0)
            {
                if (oracleCrits.TryGetValue(self, out var _))
                {
                    oracleCrits.Remove(self);
                }
                oracleCrits.Add(self, playersInRoom[0]);
            }
        }
        orig(self, eu);
        //SLOracleBehavior_BaseUpdate(self, eu);
    }

    public static void hasmarkUpdate(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
    {
        if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MenagerieGameMode)
        {
            orig(self, eu);
            return;
        }
        if (ModManager.MSC && self.SingularityProtest())
        {
            if (self.currentConversation != null)
            {
                self.currentConversation.Destroy();
            }
        }
        else
        {
            self.protest = false;
        }
        SLOracleBehavior_BaseUpdate(self, eu);
        oracleCrits.TryGetValue(self, out var crit);
        var slug = crit as Player;
        if (!self.oracle.Consious || self.stillWakingUp)
        {
            self.oracle.room.socialEventRecognizer.ownedItemsOnGround.Clear();
            self.holdingObject = null;
            self.moveToAndPickUpItem = null;
            return;
        }
        if (ModManager.MSC)
        {
            if (self.rivEnding != null && self.currentConversation != null)
            {
                self.currentConversation.Update();
                self.oracle.room.socialEventRecognizer.ownedItemsOnGround.Clear();
                self.holdingObject = null;
                self.moveToAndPickUpItem = null;
                return;
            }
            if (self.rivEnding != null)
            {
                return;
            }
            if (crit != null && self.oracle.room.game.GetStorySession.saveState.denPosition == "SL_AI" && self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding && self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Rivulet && !self.holdPlayerAsleep)
            {
                self.holdPlayerAsleep = true;
                self.oracle.room.game.GetStorySession.saveState.denPosition = "SL_S06";
                self.sayHelloDelay = 0;
                self.forceFlightMode = true;
                if (self.currentConversation != null)
                {
                    self.currentConversation.Destroy();
                    self.currentConversation = null;
                }
                self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(MoreSlugcatsEnums.ConversationID.Moon_RivuletPostgame, self.oracle.oracleBehavior, SLOracleBehaviorHasMark.MiscItemType.NA);
            }
            if (slug != null && self.holdPlayerAsleep)
            {
                slug.sleepCounter = 99;
                slug.standing = false;
                slug.flipDirection = 1;
                slug.touchedNoInputCounter = 10;
                slug.sleepCurlUp = 1f;
            }
            if (self.currentConversation == null)
            {
                self.forceFlightMode = false;
            }
        }
        if (crit != null && self.hasNoticedPlayer)
        {
            if (ModManager.MMF && crit.dead)
            {
                self.TalkToDeadPlayer();
            }
            if (self.movementBehavior != SLOracleBehavior.MovementBehavior.Meditate && self.movementBehavior != SLOracleBehavior.MovementBehavior.ShowMedia)
            {
                self.lookPoint = crit.DangerPos;
            }
            if (self.sayHelloDelay < 0 && ((ModManager.MSC && self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint) || self.oracle.room.world.rainCycle.TimeUntilRain + self.oracle.room.world.rainCycle.pause > 2000))
            {
                self.sayHelloDelay = 30;
            }
            else
            {
                if (self.sayHelloDelay > 0)
                {
                    self.sayHelloDelay--;
                }
                if (self.sayHelloDelay == 1)
                {
                    self.InitateConversation();
                    if (!self.conversationAdded && self.oracle.room.game.session is StoryGameSession)
                    {
                        SLOrcacleState sloracleState = (self.oracle.room.game.session as StoryGameSession).saveState.miscWorldSaveData.SLOracleState;
                        int num = sloracleState.playerEncounters;
                        sloracleState.playerEncounters = num + 1;
                        SLOrcacleState sloracleState2 = (self.oracle.room.game.session as StoryGameSession).saveState.miscWorldSaveData.SLOracleState;
                        num = sloracleState2.playerEncountersWithMark;
                        sloracleState2.playerEncountersWithMark = num + 1;
                        if (ModManager.MSC && self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Rivulet && (self.oracle.room.game.session as StoryGameSession).saveState.miscWorldSaveData.SLOracleState.playerEncounters == 1 && self.oracle.room.world.overseersWorldAI != null)
                        {
                            self.oracle.room.world.overseersWorldAI.DitchDirectionGuidance();
                        }
                        Custom.Log(new string[]
                        {
                        "player encounter with SL AI logged"
                        });
                        self.conversationAdded = true;
                    }
                }
            }
            if (crit.room != self.oracle.room || crit.DangerPos.x < 1016f)
            {
                self.playerLeavingCounter++;
            }
            else
            {
                self.playerLeavingCounter = 0;
            }
            if (crit.room == self.oracle.room && Custom.DistLess(crit.mainBodyChunk.pos, self.oracle.firstChunk.pos, 100f) && !Custom.DistLess(crit.mainBodyChunk.lastPos, crit.mainBodyChunk.pos, 1f))
            {
                self.playerAnnoyingCounter++;
            }
            else
            {
                self.playerAnnoyingCounter--;
            }
            self.playerAnnoyingCounter = Custom.IntClamp(self.playerAnnoyingCounter, 0, 150);
            bool flag = false;
            for (int i = 0; i < crit.grasps.Length; i++)
            {
                if (crit.grasps[i] != null && crit.grasps[i].grabbed is SLOracleSwarmer)
                {
                    flag = true;
                }
            }
            if (!self.State.SpeakingTerms && self.currentConversation != null)
            {
                self.currentConversation.Destroy();
            }
            if (!self.rainInterrupt && crit.room == self.oracle.room && self.oracle.room.world.rainCycle.TimeUntilRain < 1600 && self.oracle.room.world.rainCycle.pause < 1)
            {
                if (ModManager.MSC && self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint)
                {
                    if (self.currentConversation == null)
                    {
                        self.InterruptRain();
                        self.rainInterrupt = true;
                    }
                }
                else
                {
                    self.InterruptRain();
                    self.rainInterrupt = true;
                    if (self.currentConversation != null)
                    {
                        self.currentConversation.Destroy();
                    }
                }
            }
            if (flag)
            {
                if (self.currentConversation != null)
                {
                    if (!self.currentConversation.paused || self.pauseReason != SLOracleBehaviorHasMark.PauseReason.GrabNeuron)
                    {
                        self.currentConversation.paused = true;
                        self.pauseReason = SLOracleBehaviorHasMark.PauseReason.GrabNeuron;
                        self.InterruptPlayerHoldNeuron();
                    }
                }
                else if (!self.playerHoldingNeuronNoConvo)
                {
                    self.playerHoldingNeuronNoConvo = true;
                    self.InterruptPlayerHoldNeuron();
                }
            }
            if (self.currentConversation != null)
            {
                self.playerHoldingNeuronNoConvo = false;
                self.playerIsAnnoyingWhenNoConversation = false;
                if (self.currentConversation.slatedForDeletion)
                {
                    self.currentConversation = null;
                }
                else
                {
                    if (self.playerLeavingCounter > 10)
                    {
                        if (!self.currentConversation.paused)
                        {
                            self.currentConversation.paused = true;
                            self.pauseReason = SLOracleBehaviorHasMark.PauseReason.Leave;
                            self.InterruptPlayerLeavingMessage();
                        }
                    }
                    else if (self.playerAnnoyingCounter > 80 && !self.oracle.room.game.IsMoonActive())
                    {
                        if (!self.currentConversation.paused)
                        {
                            self.currentConversation.paused = true;
                            self.pauseReason = SLOracleBehaviorHasMark.PauseReason.Annoyance;
                            self.InterruptPlayerAnnoyingMessage();
                        }
                    }
                    else if (self.currentConversation.paused)
                    {
                        if (self.resumeConversationAfterCurrentDialoge)
                        {
                            if (self.dialogBox.messages.Count == 0)
                            {
                                self.currentConversation.paused = false;
                                self.resumeConversationAfterCurrentDialoge = false;
                                self.currentConversation.RestartCurrent();
                            }
                        }
                        else if ((self.pauseReason == SLOracleBehaviorHasMark.PauseReason.Leave && crit.room == self.oracle.room && crit.DangerPos.x > 1036f) || (self.pauseReason == SLOracleBehaviorHasMark.PauseReason.Annoyance && self.playerAnnoyingCounter == 0) || (self.pauseReason == SLOracleBehaviorHasMark.PauseReason.GrabNeuron && !flag))
                        {
                            self.resumeConversationAfterCurrentDialoge = true;
                            self.ResumePausedConversation();
                        }
                    }
                    self.currentConversation.Update();
                }
            }
            else if (self.State.SpeakingTerms)
            {
                if (self.playerHoldingNeuronNoConvo && !flag)
                {
                    self.playerHoldingNeuronNoConvo = false;
                    self.PlayerReleaseNeuron();
                }
                else if (self.playerAnnoyingCounter > 80 && !self.playerIsAnnoyingWhenNoConversation && !self.oracle.room.game.IsMoonActive())
                {
                    self.playerIsAnnoyingWhenNoConversation = true;
                    self.PlayerAnnoyingWhenNotTalking();
                }
                else if (self.playerAnnoyingCounter < 10 && self.playerIsAnnoyingWhenNoConversation)
                {
                    self.playerIsAnnoyingWhenNoConversation = false;
                    if (self.State.annoyances == 1)
                    {
                        if (self.State.neuronsLeft == 3)
                        {
                            self.dialogBox.Interrupt("...thank you.", 7);
                        }
                        else if (self.State.neuronsLeft > 3)
                        {
                            self.dialogBox.Interrupt(self.Translate("Thank you."), 7);
                        }
                    }
                }
            }
        }
        if ((ModManager.MSC || (!self.DamagedMode && self.State.SpeakingTerms)) && self.holdingObject == null && self.reelInSwarmer == null && self.moveToAndPickUpItem == null)
        {
            for (int j = 0; j < self.oracle.room.socialEventRecognizer.ownedItemsOnGround.Count; j++)
            {
                if (Custom.DistLess(self.oracle.room.socialEventRecognizer.ownedItemsOnGround[j].item.firstChunk.pos, self.oracle.firstChunk.pos, 100f) && self.WillingToInspectItem(self.oracle.room.socialEventRecognizer.ownedItemsOnGround[j].item))
                {
                    bool flag2 = true;
                    for (int k = 0; k < self.pickedUpItemsThisRealization.Count; k++)
                    {
                        if (self.pickedUpItemsThisRealization[k] == self.oracle.room.socialEventRecognizer.ownedItemsOnGround[j].item.abstractPhysicalObject.ID)
                        {
                            flag2 = false;
                            break;
                        }
                    }
                    if (flag2)
                    {
                        self.moveToAndPickUpItem = self.oracle.room.socialEventRecognizer.ownedItemsOnGround[j].item;
                        if (self.currentConversation != null)
                        {
                            self.currentConversation.Destroy();
                        }
                        self.currentConversation = null;
                        self.PlayerPutItemOnGround();
                        break;
                    }
                }
            }
        }
        if (self.moveToAndPickUpItem != null)
        {
            self.moveToItemDelay++;
            if (!self.WillingToInspectItem(self.moveToAndPickUpItem) || self.moveToAndPickUpItem.grabbedBy.Count > 0)
            {
                self.moveToAndPickUpItem = null;
            }
            else if ((self.moveToItemDelay > 40 && Custom.DistLess(self.moveToAndPickUpItem.firstChunk.pos, self.oracle.firstChunk.pos, 40f)) || (self.moveToItemDelay < 20 && !Custom.DistLess(self.moveToAndPickUpItem.firstChunk.lastPos, self.moveToAndPickUpItem.firstChunk.pos, 5f) && Custom.DistLess(self.moveToAndPickUpItem.firstChunk.pos, self.oracle.firstChunk.pos, 20f)))
            {
                self.GrabObject(self.moveToAndPickUpItem);
                self.moveToAndPickUpItem = null;
            }
        }
        else
        {
            self.moveToItemDelay = 0;
        }
        if (crit != null)
        {
            int l = 0;
            while (l < crit.grasps.Length)
            {
                if (crit.grasps[l] != null && crit.grasps[l].grabbed is SLOracleSwarmer)
                {
                    self.protest = true;
                    self.holdKnees = false;
                    self.oracle.bodyChunks[0].vel += Custom.RNV() * self.oracle.health * UnityEngine.Random.value;
                    self.oracle.bodyChunks[1].vel += Custom.RNV() * self.oracle.health * UnityEngine.Random.value * 2f;
                    self.protestCounter += 0.045454547f;
                    self.lookPoint = self.oracle.bodyChunks[0].pos + Custom.PerpendicularVector(self.oracle.bodyChunks[1].pos, self.oracle.bodyChunks[0].pos) * Mathf.Sin(self.protestCounter * 3.1415927f * 2f) * 145f;
                    if (UnityEngine.Random.value < 0.033333335f)
                    {
                        self.armsProtest = !self.armsProtest;
                        break;
                    }
                    break;
                }
                else
                {
                    l++;
                }
            }
        }
        if (!self.protest)
        {
            self.armsProtest = false;
        }
        if (self.holdingObject != null)
        {
            self.describeItemCounter++;
            if (!self.protest && (self.currentConversation == null || !self.currentConversation.paused) && self.movementBehavior != SLOracleBehavior.MovementBehavior.Meditate && self.movementBehavior != SLOracleBehavior.MovementBehavior.ShowMedia)
            {
                self.lookPoint = self.holdingObject.firstChunk.pos + Custom.DirVec(self.oracle.firstChunk.pos, self.holdingObject.firstChunk.pos) * 100f;
            }
            if (!(self.holdingObject is SSOracleSwarmer) && self.describeItemCounter > 40 && self.currentConversation == null)
            {
                if (ModManager.MMF && self.throwAwayObjects)
                {
                    self.holdingObject.firstChunk.vel = new Vector2(-5f + (float)UnityEngine.Random.Range(-8, -11), 8f + (float)UnityEngine.Random.Range(1, 3));
                    self.oracle.room.PlaySound(SoundID.Slugcat_Throw_Rock, self.oracle.firstChunk);
                }
                self.holdingObject = null;
                return;
            }
        }
        else
        {
            self.describeItemCounter = 0;
        }
        StoryMenagerie.Debug("end of withmark update");
    }

    // this is actually so stupid why do they call game.players like 20 individual times
    public static void IL_VoidSeaScene_Update(ILContext il)
    {
        try
        {
            int loc = 0;
            var c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<UpdatableAndDeletable>("room"),
                x => x.MatchLdfld<Room>("game"),
                x => x.MatchCallvirt<RainWorldGame>("get_FirstRealizedPlayer"),
                x => x.MatchStloc(out loc) //stloc.s V_5
            );
            c.Emit(OpCodes.Ldloc, loc);
            c.EmitDelegate(VoidWormPlayer);
            c.Emit(OpCodes.Stloc, loc);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void UpdateCreaturesInVoidSea(VoidSeaScene self, Creature voidSeaCrit)
    {
        VoidSeaTreatment(self, voidSeaCrit, 0.95f);
        bool flag = voidSeaCrit.mainBodyChunk.pos.y < 240f && voidSeaCrit.mainBodyChunk.pos.y > -40f;
        if (self.Inverted)
        {
            flag = (voidSeaCrit.mainBodyChunk.pos.y > self.room.PixelHeight + 100f && voidSeaCrit.mainBodyChunk.pos.y < self.room.PixelHeight + 380f);
        }
        if (!self.secondSpace && flag)
        {
            float num = 2200f;
            float num2 = 2900f;
            if (self.Inverted)
            {
                num = 0f;
                num2 = self.room.PixelWidth;
            }
            if (voidSeaCrit.mainBodyChunk.pos.x < num)
            {
                MoveCreature(self, voidSeaCrit, new Vector2((num + num2) * 0.5f - voidSeaCrit.mainBodyChunk.pos.x, 0f), true);
            }
            else if (voidSeaCrit.mainBodyChunk.pos.x > num2)
            {
                MoveCreature(self, voidSeaCrit, new Vector2((num + num2) * 0.5f - voidSeaCrit.mainBodyChunk.pos.x, 0f), true);
            }
        }
        if ((int)self.deepDivePhase >= (int)VoidSeaScene.DeepDivePhase.EggScenario && voidSeaCrit.mainBodyChunk.pos.y > -10000f)
        {
            Custom.Log(new string[]
            {
            "second space mov"
            });
            Vector2 b = self.theEgg.pos - voidSeaCrit.mainBodyChunk.pos;
            MoveCreature(self, voidSeaCrit, new Vector2(0f, -11000f - voidSeaCrit.mainBodyChunk.pos.x), true);
            self.theEgg.pos = voidSeaCrit.mainBodyChunk.pos + b;
        }
        for (int i = 0; i < self.room.game.cameras.Length; i++)
        {
            if (self.room.game.cameras[i].followAbstractCreature == voidSeaCrit.abstractCreature)
            {
                self.room.game.cameras[i].voidSeaMode = ((voidSeaCrit.mainBodyChunk.pos.y < 240f && !self.Inverted) || (self.Inverted && voidSeaCrit.mainBodyChunk.pos.y > self.room.PixelHeight + 100f));
                if (self.room.game.cameras[i].voidSeaMode && !self.lastVoidSeaModes[i])
                {
                    self.cameraOffset *= 0f;
                    self.room.game.cameras[i].pos = voidSeaCrit.mainBodyChunk.pos - new Vector2(700f, 400f);
                    self.room.game.cameras[i].lastPos = self.room.game.cameras[i].pos;
                }
                self.lastVoidSeaModes[i] = self.room.game.cameras[i].voidSeaMode;
            }
        }
        var input = default(Player.InputPackage);
        if (voidSeaCrit is Player slug)
        {
            input = slug.input[0];
        }
        else if (CreatureController.creatureControllers.TryGetValue(voidSeaCrit, out var cc))
        {
            input = cc.input[0];
        }
        if (self.deepDivePhase == VoidSeaScene.DeepDivePhase.EggScenario && self.eggScenarioTimer < 2800 && (input.x != 0 || input.y != 0))
        {
            self.eggScenarioTimer++;
        }
    }

    public static void MoveCreature(VoidSeaScene self, Creature crit, Vector2 move, bool moveCamera)
    {
        if (moveCamera)
        {
            self.cameraOffset -= move;
        }
        for (int i = 0; i < crit.bodyChunks.Length; i++)
        {
            crit.bodyChunks[i].pos += move;
            crit.bodyChunks[i].lastPos += move;
            crit.bodyChunks[i].lastLastPos += move;
        }
        if (crit.graphicsModule != null)
        {
            for (int j = 0; j < crit.graphicsModule.bodyParts.Length; j++)
            {
                crit.graphicsModule.bodyParts[j].pos += move;
                crit.graphicsModule.bodyParts[j].lastPos += move;
            }
            if (crit.graphicsModule is PlayerGraphics)
            {
                if ((crit.graphicsModule as PlayerGraphics).lightSource != null)
                {
                    (crit.graphicsModule as PlayerGraphics).lightSource.HardSetPos((crit.graphicsModule as PlayerGraphics).lightSource.Pos + move);
                }
                for (int k = 0; k < (crit.graphicsModule as PlayerGraphics).drawPositions.GetLength(0); k++)
                {
                    (crit.graphicsModule as PlayerGraphics).drawPositions[k, 0] += move;
                    (crit.graphicsModule as PlayerGraphics).drawPositions[k, 1] += move;
                }
            }
        }
        if (moveCamera)
        {
            for (int l = 0; l < self.room.game.cameras.Length; l++)
            {
                if (self.room.game.cameras[l].followAbstractCreature == crit.abstractCreature)
                {
                    self.room.game.cameras[l].pos += move;
                    self.room.game.cameras[l].lastPos += move;
                }
            }
        }
    }

    public static void VoidSeaTreatment(VoidSeaScene self, Creature crit, float swimSpeed)
    {
        if (crit.room != self.room)
        {
            return;
        }
        var slug = crit as Player;
        for (int i = 0; i < crit.bodyChunks.Length; i++)
        {
            crit.bodyChunks[i].restrictInRoomRange = float.MaxValue;
            crit.bodyChunks[i].vel *= Mathf.Lerp(swimSpeed, 1f, self.room.game.cameras[0].voidSeaGoldFilter);
            if (self.Inverted)
            {
                BodyChunk bodyChunk = crit.bodyChunks[i];
                bodyChunk.vel.y = bodyChunk.vel.y + crit.buoyancy;
                BodyChunk bodyChunk2 = crit.bodyChunks[i];
                bodyChunk2.vel.y = bodyChunk2.vel.y - crit.gravity;
            }
            else
            {
                BodyChunk bodyChunk3 = crit.bodyChunks[i];
                bodyChunk3.vel.y = bodyChunk3.vel.y - crit.buoyancy;
                BodyChunk bodyChunk4 = crit.bodyChunks[i];
                bodyChunk4.vel.y = bodyChunk4.vel.y + crit.gravity;
            }
        }
        if ((!ModManager.MSC || self.saintEndPhase != VoidSeaScene.SaintEndingPhase.EchoTransform))
        {
            if (slug != null)
            {
                slug.airInLungs = 1f;
                slug.lungsExhausted = false;
            }
            else if (crit is AirBreatherCreature breather)
            {
                breather.lungs = 1f;
            }
        }
        if (crit.graphicsModule is PlayerGraphics && (crit.graphicsModule as PlayerGraphics).lightSource != null)
        {
            if (self.Inverted)
            {
                (crit.graphicsModule as PlayerGraphics).lightSource.setAlpha = new float?(Custom.LerpMap(crit.mainBodyChunk.pos.y, 2000f, 8000f, 1f, 0.2f) * (1f - self.eggProximity));
                (crit.graphicsModule as PlayerGraphics).lightSource.setRad = new float?(Custom.LerpMap(crit.mainBodyChunk.pos.y, 2000f, 8000f, 300f, 200f) * (0.5f + 0.5f * (1f - self.eggProximity)));
            }
            else
            {
                (crit.graphicsModule as PlayerGraphics).lightSource.setAlpha = new float?(Custom.LerpMap(crit.mainBodyChunk.pos.y, -2000f, -8000f, 1f, 0.2f) * (1f - self.eggProximity));
                (crit.graphicsModule as PlayerGraphics).lightSource.setRad = new float?(Custom.LerpMap(crit.mainBodyChunk.pos.y, -2000f, -8000f, 300f, 200f) * (0.5f + 0.5f * (1f - self.eggProximity)));
            }
        }
        else if (CreatureController.creatureControllers.TryGetValue(crit, out var cc) && cc != null)
        {
            if (self.Inverted)
            {
                cc.lightSource.setAlpha = new float?(Custom.LerpMap(crit.mainBodyChunk.pos.y, 2000f, 8000f, 1f, 0.2f) * (1f - self.eggProximity));
                cc.lightSource.setRad = new float?(Custom.LerpMap(crit.mainBodyChunk.pos.y, 2000f, 8000f, 300f, 200f) * (0.5f + 0.5f * (1f - self.eggProximity)));
            }
            else
            {
                cc.lightSource.setAlpha = new float?(Custom.LerpMap(crit.mainBodyChunk.pos.y, -2000f, -8000f, 1f, 0.2f) * (1f - self.eggProximity));
                cc.lightSource.setRad = new float?(Custom.LerpMap(crit.mainBodyChunk.pos.y, -2000f, -8000f, 300f, 200f) * (0.5f + 0.5f * (1f - self.eggProximity)));
            }
        }
        if (self.deepDivePhase == VoidSeaScene.DeepDivePhase.EggScenario && UnityEngine.Random.value < 0.1f)
        {
            crit.mainBodyChunk.vel += Custom.DirVec(crit.mainBodyChunk.pos, self.theEgg.pos) * 0.02f * UnityEngine.Random.value;
        }
        if (ModManager.MMF && crit.Submersion > 0.5f)
        {
            for (int i = 0; i < crit.grasps.Length; i++)
            {
                if (crit.grasps[i] != null && !(crit.grasps[i].grabbed is Creature))
                {
                    self.AddMeltObject(crit.grasps[1].grabbed);
                }
            }
            if (slug != null && slug.spearOnBack != null && slug.spearOnBack.HasASpear)
            {
                slug.spearOnBack.DropSpear();
                self.AddMeltObject(slug.spearOnBack.spear);
            }
        }
    }

    public static void FindVoidWormPlayer(VoidWorm self)
    {
        if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MenagerieGameMode menagerie)
        {
            return;
        }

        var crit = menagerie.localAvatars.Where(ac => ac.realizedCreature != null).ToArray()[0].realizedCreature;
        if (voidWormCrits.TryGetValue(self, out var _))
        {
            voidWormCrits.Remove(self);
        }
        voidWormCrits.Add(self, crit);
    }

    public static Creature VoidWormPlayer(VoidWorm self)
    {
        if (StoryMenagerie.IsMenagerie)// && self != null)
        {
            if (voidWormCrits.TryGetValue(self, out var crit))
            {
                return crit;
            }
            else
            {
                FindVoidWormPlayer(self);
                if (voidWormCrits.TryGetValue(self, out crit))
                {
                    return crit;
                }
                else
                {
                    return null;
                }
            }
        }
        return self.voidSea.room.game.FirstRealizedPlayer;
    }

    public static bool ReplaceVoidWormBehaviorPlayer(ILCursor c, int ldloc)
    {
        if (c.TryGotoNext(
            MoveType.After,
            x => x.MatchLdloc(ldloc)
        ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(VoidWorm.VoidWormBehavior).GetField(nameof(VoidWorm.VoidWormBehavior.worm)));
            c.EmitDelegate(VoidWormPlayer);
            return true;
        }
        return false;
    }

    public static bool ReplaceVoidWormArmPlayer(ILCursor c, int ldloc)
    {
        if (c.TryGotoNext(
            MoveType.After,
            x => x.MatchLdloc(ldloc)
        ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(VoidWorm.Arm).GetField(nameof(VoidWorm.Arm.owner)));
            c.EmitDelegate(VoidWormPlayer);
            return true;
        }
        return false;
    }

    public static void ReplaceVoidWormBehaviorPlayerHook(ILContext il)
    {
        var c = new ILCursor(il);
        while (true)
        {
            if (!ReplaceVoidWormBehaviorPlayer(c, 0)) break;
        }
    }

    public static void IL_VoidWorm_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<VoidSea.VoidWorm>("voidSea"),
                x => x.MatchLdfld<UpdatableAndDeletable>("room"),
                x => x.MatchLdfld<Room>("game"),
                x => x.MatchCallvirt<RainWorldGame>("get_FirstRealizedPlayer")
            );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(VoidWormPlayer);
            c.Emit(OpCodes.Stloc_0);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static bool MenagerieLobby() => StoryMenagerie.IsMenagerie;

    public static SlugcatStats.Name GameCharacter(VoidWorm.MainWormBehavior self) => self.worm.room.game.StoryCharacter;

    public static void IL_MainWormBehavior_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            for (int i = 0; i < 18; i++)
            {
                ReplaceVoidWormBehaviorPlayer(c, 0);
            }

            for (int i = 0; i < 2; i++)
            {
                c.GotoNext(
                    x => x.MatchLdloc(0),
                    x => x.MatchCallOrCallvirt<Player>("get_playerState"),
                    x => x.MatchLdfld<PlayerState>(nameof(PlayerState.slugcatCharacter))
                );
                c.EmitDelegate(MenagerieLobby);
                var skip = c.DefineLabel();
                c.Emit(OpCodes.Brfalse, skip);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(GameCharacter);
                var skip2 = c.DefineLabel();
                c.Emit(OpCodes.Br, skip2);
                c.MarkLabel(skip);
                c.Index += 3;
                c.MarkLabel(skip2);
            }
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }
}
