using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using MonoMod.RuntimeDetour;
using RainMeadow;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Mono.Cecil;

namespace StoryMenagerie;

public static class MeadowHooks
{

    public static void Apply()
    {
        try
        {
            new Hook(typeof(MeadowVoice).GetConstructor(new Type[] { typeof(CreatureController) }), On_MeadowVoice_ctor);
            new Hook(typeof(MeadowVoice).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance), On_MeadowVoice_Update);
            new Hook(typeof(MeadowVoice).GetMethod("Call", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance), On_MeadowVoice_Call);
            //new ILHook(typeof(CreatureController).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance), IL_CreatureController_Update);
            //new Hook(typeof(CreatureController).GetMethod("ConsciousUpdate", BindingFlags.Public | BindingFlags.Instance), On_CreatureController_ConsciousUpdate);
            //new ILHook(typeof(CreatureController).GetMethod("CheckInputs", BindingFlags.Public | BindingFlags.Instance), IL_CreatureController_CheckInputs);
            new Hook(typeof(RainMeadow.RainMeadow).GetMethod("ShelterDoorOnClose", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), On_RainMeadow_ShelterDoorOnClose);
            Debug.Log("all fine and great!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            new Hook(typeof(RainMeadow.RainMeadow).GetMethod("Player_AddFood", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), On_RainMeadow_Player_AddFood);
            new Hook(typeof(RainMeadow.RainMeadow).GetMethod("Player_SubtractFood", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), On_RainMeadow_Player_SubtractFood);
            new ILHook(typeof(PlayerSpecificOnlineHud).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public), IL_PlayerSpecificOnlineHud_Update);
            new ILHook(typeof(OnlinePlayerDisplay).GetConstructor(new Type[] { typeof(PlayerSpecificOnlineHud), typeof(SlugcatCustomization), typeof(OnlinePlayer) }), IL_OnlinePlayerDisplay_ctor);
            new ILHook(typeof(MeadowCreatureData.State).GetConstructor(new Type[] { typeof(MeadowCreatureData) }), IL_MeadowCreatureData_State_ctor);
            new ILHook(typeof(MeadowCreatureData.State).GetMethod("ReadTo", BindingFlags.Instance | BindingFlags.Public), IL_MeadowCreatureData_State_ReadTo);
            new ILHook(typeof(OnlinePlayerDeathBump).GetConstructor(new Type[] { typeof(PlayerSpecificOnlineHud), typeof(SlugcatCustomization) }), IL_OnlinePlayerDeathBump_ctor);
            new Hook(typeof(StoryModeExtensions).GetMethod(nameof(StoryModeExtensions.FriendlyFireSafetyCandidate), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), On_StoryModeExtensions_FriendlyFireSafetyCandidate);
            new Hook(typeof(AbstractPhysicalObjectState).GetMethod(nameof(AbstractPhysicalObjectState.GetRealizedState), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), On_AbstractCreatureState_GetRealizedState);
            new ILHook(typeof(OnlinePlayerDeathBump).GetMethod("Draw", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), IL_OnlinePlayerDeathBump_Draw);
            new Hook(typeof(SpectatorHud).GetMethod(nameof(SpectatorHud.ReturnCameraToPlayer), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), On_SpectatorHud_Update);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    //public delegate void On_CreatureController_orig_ConsciousUpdate(CreatureController self);
    public delegate void On_MeadowVoice_orig_ctor(MeadowVoice self, CreatureController owner);
    public delegate void On_MeadowVoice_orig_Update(MeadowVoice self);
    public delegate void On_RainMeadow_orig_ShelterDoorOnClose(RainMeadow.RainMeadow _, On.ShelterDoor.orig_Close orig, ShelterDoor self);

    public static void On_MeadowVoice_ctor(On_MeadowVoice_orig_ctor orig, MeadowVoice self, CreatureController owner)
    {
        if (OnlineManager.lobby.gameMode is not MenagerieGameMode) orig(self, owner);
    }

    public static void On_MeadowVoice_Update(On_MeadowVoice_orig_Update orig, MeadowVoice self)
    {
        if (OnlineManager.lobby.gameMode is not MenagerieGameMode) orig(self);
    }

    public static void On_MeadowVoice_Call(Action<MeadowVoice> orig, MeadowVoice self)
    {
        if (OnlineManager.lobby.gameMode is not MenagerieGameMode) orig(self);
    }

    public static bool ShouldUpdateVoice() => OnlineManager.lobby.gameMode is not MenagerieGameMode;

    private static void IL_CreatureController_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CreatureController>("voice"),
                x => x.MatchCallvirt<MeadowVoice>("Update")
            );
            var skip = c.DefineLabel();
            c.EmitDelegate(ShouldUpdateVoice);
            c.Emit(OpCodes.Brfalse, skip);
            c.GotoNext(x => x.MatchNop());
            c.Index++;
            c.MarkLabel(skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void On_RainMeadow_ShelterDoorOnClose(On_RainMeadow_orig_ShelterDoorOnClose orig, RainMeadow.RainMeadow _, On.ShelterDoor.orig_Close origorig, ShelterDoor self)
    {
        if (OnlineManager.lobby == null)
        {
            origorig(self);
            return;
        }
        if (OnlineManager.lobby.gameMode is not MenagerieGameMode menagerie)
        {
            orig(_, origorig, self);
            return;
        }
        if (!self.Broken)
        {
            menagerie.storyClientData.readyForWin = true;
            if (!menagerie.readyForWin)
            {
                StoryMenagerie.Debug("not ready for win; shelter door returning");
                return;
            }
        } else
        {
            var crit = menagerie.avatars.First(); //needs to be changed if we want to support Jolly
            var realizedCrit = crit.realizedCreature;
            if (realizedCrit == null || !self.room.PlayersInRoom.Contains(realizedCrit)) return;
            var readyForWin = false;
            if (realizedCrit is Player scug) readyForWin = scug.readyForWin;
            else if (CreatureController.creatureControllers.TryGetValue(realizedCrit, out var controller)) readyForWin = controller.readyForWin;
            else StoryMenagerie.LogError("Player is neither a slugcat nor a controlled creature!");
            if (!readyForWin) return;
        }

        origorig(self);

        // yoinked this
        if (self.IsClosing)
        {
            if (menagerie != null && menagerie.storyClientData.readyForWin)
            {
                menagerie.myLastDenPos = self.room.abstractRoom.name;
                menagerie.myLastWarp = null;
                menagerie.hasSheltered = true;
            }
        }
    }

    public static void On_RainMeadow_Player_AddFood(Action<RainMeadow.RainMeadow, On.Player.orig_AddFood, Player, int> orig, RainMeadow.RainMeadow _, On.Player.orig_AddFood origorig, Player self, int add)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && RainMeadow.RainMeadow.sUpdateFood)
        {
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity))
            {
                StoryMenagerie.LogError("Player doesn't have OnlineEntity counterpart!!");
                return;
            }
            if (!onlineEntity.isMine) return;
            var state = (PlayerState)self.State;
            state.foodInStomach = menagerie.foodPoints;
            state.quarterFoodPoints = menagerie.quarterFoodPoints;
            var origFood = state.foodInStomach * 4 + state.quarterFoodPoints;

            origorig(self, add);
            if (self.isNPC) return;
            var newFood = state.foodInStomach * 4 + state.quarterFoodPoints;
            if (!OnlineManager.lobby.isOwner)
            {
                if (newFood != origFood) OnlineManager.lobby.owner.InvokeRPC(MenagerieGameMode.ChangeFood, (short)(newFood - origFood));
            }
            else
            {
                MenagerieGameMode.ChangeFood((short)(newFood - origFood));
            }
            return;
        }
        orig(_, origorig, self, add);
    }

    public static void On_RainMeadow_Mushroom_BitByPlayer(Action<RainMeadow.RainMeadow, On.Mushroom.orig_BitByPlayer, Mushroom, Creature.Grasp, bool> orig, RainMeadow.RainMeadow _, On.Mushroom.orig_BitByPlayer origorig, Mushroom self, Creature.Grasp grasp, bool eu)
    {
        if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MenagerieGameMode menagerie)
        {
            orig(_, origorig, self, grasp, eu);
        }
        if (!OnlinePhysicalObject.map.TryGetValue((grasp.grabber as Player).abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
        if (!onlineEntity.isMine) return;

        OnlineManager.lobby.owner.InvokeOnceRPC(MenagerieGameMode.AddMushroomCounter);
    }

    // copy of addfood hook
    public static void On_RainMeadow_Player_SubtractFood(Action<RainMeadow.RainMeadow, On.Player.orig_SubtractFood, Player, int> orig, RainMeadow.RainMeadow _, On.Player.orig_SubtractFood origorig, Player self, int add)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && RainMeadow.RainMeadow.sUpdateFood)
        {
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
            if (!onlineEntity.isMine) return;
            var state = (PlayerState)self.State;
            state.foodInStomach = menagerie.foodPoints;
            state.quarterFoodPoints = menagerie.quarterFoodPoints;
            var origFood = state.foodInStomach * 4 + state.quarterFoodPoints;

            origorig(self, add);
            if (self.isNPC) return;
            var newFood = state.foodInStomach * 4 + state.quarterFoodPoints;
            if (!OnlineManager.lobby.isOwner)
            {
                if (newFood != origFood) OnlineManager.lobby.owner.InvokeRPC(MenagerieGameMode.ChangeFood, (short)(newFood - origFood));
            }
            else
            {
                MenagerieGameMode.ChangeFood((short)(newFood - origFood));
            }
            return;
        }
        orig(_, origorig, self, add);
    }

    public static Vector2 PlayerIsCreature(PlayerSpecificOnlineHud self, Vector2 rawPos)
    {
        if (OnlineManager.lobby.gameMode is MenagerieGameMode && self.abstractPlayer.realizedCreature != null && CreatureController.creatureControllers.TryGetValue(self.abstractPlayer.realizedCreature, out var _))
        {
            var crit = self.abstractPlayer.realizedCreature;
            if (crit.room == self.camera.room)
            {
                self.found = true;
                rawPos = /*Vector2.Lerp(crit.bodyChunks[0].pos, crit.bodyChunks[1].pos, 0.33333334f)*/ crit.mainBodyChunk.pos - self.camera.pos;
                self.pointDir = Vector2.down;
                //self.drawpos = rawPos;
            }
            else
            {
                Vector2? shortcutpos = self.camera.game.shortcuts.OnScreenPositionOfInShortCutCreature(self.camera.room, crit);
                if (shortcutpos != null)
                {
                    self.found = true;
                    rawPos = shortcutpos.Value - self.camera.pos;
                    self.pointDir = Vector2.down;
                    //self.drawpos = rawPos;
                }
            }
            //return true;
        }
        return rawPos;
    }

    public static void IL_PlayerSpecificOnlineHud_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip = c.DefineLabel();
            c.GotoNext(
                x => x.MatchNop(),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PlayerSpecificOnlineHud>(nameof(PlayerSpecificOnlineHud.abstractPlayer)),
                x => x.MatchCallvirt<AbstractCreature>("get_realizedCreature"),
                x => x.MatchIsinst<Player>(),
                x => x.MatchStloc(12),
                x => x.MatchLdloc(12),
                x => x.MatchLdnull(),
                x => x.MatchCgtUn(),
                x => x.MatchStloc(13)
            );
            var c2 = c;
            c2.GotoNext(x => x.MatchBrfalse(out skip));
            // if player is a creature, skip past slugcat check
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_1);
            c.EmitDelegate(PlayerIsCreature);
            c.Emit(OpCodes.Stloc_1);
            //c.Emit(OpCodes.Brtrue, skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static string SlugIconSprite(string orig, OnlinePlayerDisplay self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            /*var avatars = menagerie.avatars;
            if (avatars == null && self.owner.clientSettings.avatars != null) avatars = self.owner.clientSettings.avatars.Select(id => id.FindEntity(true)).Where(oe => oe is OnlineCreature).Select(oe => oe as OnlineCreature).ToList();
            if (avatars != null)
            {
                var avi = avatars.First(avi => avi.owner == self.owner.clientSettings.owner);
                if (avi != null)
                {
                    return CreatureSymbol.SpriteNameOfCreature(CreatureSymbol.SymbolDataFromCreature(avi.abstractCreature));
                }
            } else
            {
                StoryMenagerie.LogError("Avatar list is null!");
            }*/
            //if (self.owner.clientSettings.avatars != null && self.owner.clientSettings.avatars[0]?.FindEntity(true) is OnlineCreature oc)
            OnlineCreature crit = null;
            var owner = self.owner.clientSettings.owner;
            foreach (var kvp in OnlineManager.lobby.playerAvatars)
            {
                if (kvp.Key == owner)
                {
                    crit = kvp.Value.FindEntity(true) as OnlineCreature;
                }
            }
            if (crit != null)
            {
                return CreatureSymbol.SpriteNameOfCreature(CreatureSymbol.SymbolDataFromCreature(crit.abstractCreature));
            }
            else
            {
                crit = self.owner.clientSettings.avatars[0]?.FindEntity(true) as OnlineCreature;
                if (crit != null)
                {
                    return CreatureSymbol.SpriteNameOfCreature(CreatureSymbol.SymbolDataFromCreature(crit.abstractCreature));
                }
            }
        }
        return orig;
    }

    public static void IL_OnlinePlayerDisplay_ctor(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdstr("Kill_Slugcat")
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(SlugIconSprite);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    // mostly copy pasted from meadow, but with spec button added
    public static ushort MenagerieInputs(Player.InputPackage i)
    {
        return (ushort)(
                      (i.x == 1 ? 1 << 0 : 0)
                    | (i.x == -1 ? 1 << 1 : 0)
                    | (i.y == 1 ? 1 << 2 : 0)
                    | (i.y == -1 ? 1 << 3 : 0)
                    | (i.downDiagonal == 1 ? 1 << 4 : 0)
                    | (i.downDiagonal == -1 ? 1 << 5 : 0)
                    | (i.pckp ? 1 << 6 : 0)
                    | (i.jmp ? 1 << 7 : 0)
                    | (i.thrw ? 1 << 8 : 0)
                    | (i.mp ? 1 << 9 : 0)
                    | (i.spec ? 1 << 10 : 0));
    }

    public static void IL_MeadowCreatureData_State_ctor(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(0),
                x => x.MatchLdfld<Player.InputPackage>("x")
            );
            FieldReference stateInputs = null;
            var c2 = c;
            c2.GotoNext(
                MoveType.After,
                x => x.MatchStfld(out stateInputs)
            );
            c.MoveAfterLabels();
            // lazy
            c.EmitDelegate(ShouldUpdateVoice);
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Brtrue, skip);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate(MenagerieInputs);
            c.Emit(OpCodes.Stfld, stateInputs);
            var skip2 = c2.DefineLabel();
            c.Emit(OpCodes.Br, skip2);
            c.MarkLabel(skip);
            c2.MarkLabel(skip2);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static bool SetSpec(ushort inputs) => ((inputs >> 10) & 1) != 0;

    //public static AddSpec(ushort inputs)

    public static void IL_MeadowCreatureData_State_ReadTo(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdloca(1),
                x => x.MatchLdcI4(1),
                x => x.MatchStfld<Player.InputPackage>("mp")
            );
            FieldReference mcdinput = null;
            var c2 = c;
            c2.GotoNext(
                MoveType.After,
                x => x.MatchStfld(out mcdinput)
            );
            c.MoveAfterLabels();
            // lazy
            c.EmitDelegate(ShouldUpdateVoice);
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Brtrue, skip);
            //c.Emit(OpCodes.Ldloc, 1)
            c.Emit(OpCodes.Ldloca, 1);
            //c.Emit(OpCodes.Ldfld, mcdinput);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(MeadowCreatureData.State).GetField(nameof(MeadowCreatureData.State.inputs), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            c.EmitDelegate(SetSpec);
            c.Emit(OpCodes.Stfld, typeof(Player.InputPackage).GetField(nameof(Player.InputPackage.spec)));
            c.MarkLabel(skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }


    public static void IL_OnlinePlayerDeathBump_ctor(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            FieldReference abstractPlayer = null;
            FieldReference state = null;
            c.GotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchLdfld(out abstractPlayer),
                x => x.MatchLdfld(out state),
                x => x.MatchIsinst<PlayerState>()
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldc_I4_0);
            // emit false
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldfld, abstractPlayer);
            c.Emit(OpCodes.Ldfld, state);
            c.Emit(OpCodes.Isinst, typeof(PlayerState));
            var skip = c.DefineLabel();
            // skip to stloc, with false still on stack
            c.Emit(OpCodes.Brfalse, skip);
            // remove false
            c.Emit(OpCodes.Pop);
            c.GotoNext(x => x.MatchStloc(out var _));
            c.MarkLabel(skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static bool On_StoryModeExtensions_FriendlyFireSafetyCandidate(Func<PhysicalObject, bool> orig, PhysicalObject creature)
    {
        if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MenagerieGameMode menagerie)
        {
            return orig(creature);
        }
        // slugpups get the spear
        if ((creature is Player p && !p.isNPC) || (creature is Creature crit && CreatureController.creatureControllers.TryGetValue(crit, out var _)))
        {
            return !menagerie.friendlyFire;
        }
        return false;
    }

    public static RealizedCreatureState On_AbstractCreatureState_GetRealizedState(Func<AbstractCreatureState, OnlinePhysicalObject, RealizedCreatureState> orig, AbstractCreatureState self, OnlinePhysicalObject onlineObject)
    {
        if (onlineObject.apo.realizedObject != null && onlineObject.apo.realizedObject is EggBug)
        {
            return new RealizedEggBugState((OnlineCreature)onlineObject);
        }
        return orig(self, onlineObject);
    }

    public static bool OwnerIsPlayer(OnlinePlayerDeathBump self) => self.owner.abstractPlayer is Player;

    public static void IL_OnlinePlayerDeathBump_Draw(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip = c.DefineLabel();
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<OnlinePlayerDeathBump>(nameof(OnlinePlayerDeathBump.owner)),
                x => x.MatchLdfld<PlayerSpecificOnlineHud>(nameof(PlayerSpecificOnlineHud.abstractPlayer)),
                x => x.MatchLdfld<AbstractCreature>(nameof(AbstractCreature.state)),
                x => x.MatchIsinst<PlayerState>(),
                x => x.MatchLdfld<PlayerState>(nameof(PlayerState.slugcatCharacter)),
                x => x.MatchLdsfld(out var _), // survivor
                x => x.MatchCallOrCallvirt(out var _), // op equality
                x => x.MatchBrfalse(out skip)
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(OwnerIsPlayer);
            c.Emit(OpCodes.Brfalse, skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static int hackyspectatorcounter = 0;
    public static void On_SpectatorHud_Update(Action<SpectatorHud> orig, SpectatorHud self)
    {
        orig(self);
        ++hackyspectatorcounter;
        if (hackyspectatorcounter >= 40 * 5)
        {
            self.ReturnCameraToPlayer();
            hackyspectatorcounter = 0;
        }
    }
}
