using MonoMod.Cil;
using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using Mono.Cecil.Cil;
using Watcher;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using System.Reflection;
using Mono.Cecil;
using MonoMod.RuntimeDetour;

namespace StoryMenagerie;

public static class HudHooks
{
    public static void Apply()
    {
        //IL.RoomCamera.Update += IL_RoomCamera_Update;
        On.HUD.HUD.Update += On_HUD_Update;
        On.HUD.HUD.InitSinglePlayerHud += On_HUD_InitSinglePlayerHud;
        On.RoomCamera.Update += On_RoomCamera_Update;
        IL.HUD.RainMeter.ctor += IL_RainMeter_ctor;
        IL.HUD.RainMeter.Draw += ReplaceAsPlayerHook;
        IL.HUD.RainMeter.Update += IL_RainMeter_Update;
        On.HUD.TextPrompt.ctor += On_TextPrompt_ctor;
        IL.HUD.TextPrompt.Update += IL_TextPrompt_Update;
        On.HUD.SubregionTracker.Update += On_SubregionTracker_Update;
        On.PlayerSessionRecord.AddEat += On_PlayerSessionRecord_AddEat;
        IL.HUD.FoodMeter.Update += IL_FoodMeter_Update;
        On.HUD.KarmaMeter.UpdateGraphic += On_KarmaMeter_UpdateGraphic;
        On.HUD.FoodMeter.ctor += On_FoodMeter_ctor;
        IL.HUD.FoodMeter.GameUpdate += IL_FoodMeter_GameUpdate;
        On.HUD.KarmaMeter.Update += On_KarmaMeter_Update;
        //if (ModManager.MSC)
        //{
            IL.MoreSlugcats.HypothermiaMeter.Update += IL_HypothermiaMeter_Update;
        //}
        // reminds me that threat music needs to be worked on
        new Hook(typeof(ThreatPulser).GetMethod("get_Threat", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), On_ThreatPulser_get_Threat);
        new Hook(typeof(ThreatPulser).GetMethod("get_Show", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), On_ThreatPulser_get_Show);
    }

    private static bool ShouldInitHUD(RoomCamera self) => OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode && self.room != null && self.followAbstractCreature != null && self.followAbstractCreature.creatureTemplate.type != CreatureTemplate.Type.Slugcat && self.followAbstractCreature.realizedCreature != null && self.game.world != null && !self.game.world.singleRoomWorld;

    private static void InitStoryHud(RoomCamera self)
    {
        if (CreatureController.creatureControllers.TryGetValue(self.followAbstractCreature?.realizedObject as Creature, out var crit))
        {
            FireUpMenagerieHud(self, crit);
            StoryMenagerie.Debug("hud initiated");
        }
    }

    public static void FireUpMenagerieHud(RoomCamera self, CreatureController player)
    {
        self.hud = new MenagerieHUD(new FContainer[]
        {
            self.ReturnFContainer("HUD"),
            self.ReturnFContainer("HUD2")
        }, self.game.rainWorld, player);
        self.hud.InitSinglePlayerHud(self);
        if (self.game.session is StoryGameSession story && story.saveState.cycleNumber > 0)
        {
            self.hud.foodMeter.visibleCounter = 200;
            self.hud.karmaMeter.forceVisibleCounter = 200;
            self.hud.rainMeter.remainVisibleCounter = 200;
        }
    }

    private static void IL_RoomCamera_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip2 = c.DefineLabel();
            if (c.TryGotoNext(
                x => x.MatchBr(out skip2),
                x => x.MatchLdarg(0),
                x => x.MatchCall<RoomCamera>("get_room"),
                x => x.MatchBrfalse(out var _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<RoomCamera>("followAbstractCreature"),
                x => x.MatchBrfalse(out var _)
            ))
            {
                c.Index++;
                c.MoveAfterLabels();
                var skip = c.DefineLabel();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(ShouldInitHUD);
                c.Emit(OpCodes.Brfalse, skip);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(InitStoryHud);
                c.Emit(OpCodes.Br, skip2);
                c.MarkLabel(skip);

            }
            else RainMeadow.RainMeadow.Error("Failed to load RoomCamera IL hook");
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void On_HUD_Update(On.HUD.HUD.orig_Update orig, HUD.HUD self)
    {
        orig(self);
        if (self is MenagerieHUD hud)
        {
            var crit = hud.Creature;
            self.showKarmaFoodRain = self.owner.RevealMap || (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && menagerie.showKarmaFoodRainTime > 0) || (crit.room != null && crit.room.abstractRoom.shelter && crit.room.abstractRoom.realizedRoom != null && crit.room.abstractRoom.realizedRoom.shelterDoor != null && !crit.room.abstractRoom.realizedRoom.shelterDoor.Broken) || (ModManager.MSC && crit.Hypothermia > 0);
            //self.showKarmaFoodRain = true;
            if (crit is not Player && self.karmaMeter.reinforceAnimation > -1)
            {
                self.karmaMeter.reinforceAnimation++;
                if (self.karmaMeter.reinforceAnimation > 135)
                {
                    self.karmaMeter.reinforceAnimation = 135;
                }
            }
        }
    }

    public static void On_HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
    {
        if (self is not MenagerieHUD hud || OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MenagerieGameMode menagerie)
        {
            orig(self, cam);
            return;
        }
        var selectedStory = menagerie.currentCampaign;
        if (ModManager.MSC)
        {
            self.AddPart(new HypothermiaMeter(self, self.fContainers[1]));
        }
        self.AddPart(new TextPrompt(self));
        if (ModManager.Watcher && selectedStory == WatcherEnums.SlugcatStatsName.Watcher)
        {
            self.AddPart(new CamoMeter(self, self.fContainers[1]));
        }
        var foodMeter = SlugcatStats.SlugcatFoodMeter(selectedStory);
        self.AddPart(new FoodMeter(self, foodMeter.x, foodMeter.y, null, 0) { lastCount = self.owner.CurrentFood });
        var data = (hud.Creature.room.game.session as StoryGameSession).saveState.deathPersistentSaveData;
        self.AddPart(new KarmaMeter(self, self.fContainers[1], new IntVector2(data.karma, data.karmaCap), data.reinforcedKarma));
        self.AddPart(new Map(self, new Map.MapData(cam.room.world, cam.room.game.rainWorld)));
        self.AddPart(new RainMeter(self, self.fContainers[1]));
        if (ModManager.HypothermiaModule)
        {
            self.AddPart(new HypothermiaMeter(self, self.fContainers[1]));
        }
        if (ModManager.MSC)
        {
            self.AddPart(new AmmoMeter(self, null, self.fContainers[1]));
            if (selectedStory == MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
            {
                self.AddPart(new GourmandMeter(self, self.fContainers[1]));
            }
        }
        if (ModManager.MMF && MMF.cfgBreathTimeVisualIndicator.Value)
        {
            self.AddPart(new BreathMeter(self, self.fContainers[1]));
            if (ModManager.CoopAvailable && cam.room.game.session != null)
            {
                for (int i = 1; i < cam.room.game.session.Players.Count; i++)
                {
                    self.AddPart(new BreathMeter(self, self.fContainers[1], cam.room.game.session.Players[i]));
                }
            }
        }
        if (ModManager.MMF && MMF.cfgThreatMusicPulse.Value)
        {
            self.AddPart(new ThreatPulser(self, self.fContainers[1]));
        }
        if (ModManager.MMF && MMF.cfgSpeedrunTimer.Value)
        {
            self.AddPart(new SpeedRunTimer(self, null, self.fContainers[1]));
        }
        if (cam.room.abstractRoom.shelter)
        {
            self.karmaMeter.fade = 1f;
            self.rainMeter.fade = 1f;
            self.foodMeter.fade = 1f;
        }
        self.AddPart(new OnlineHUD(self, cam, menagerie));
        self.AddPart(new SpectatorHud(self, cam));
        self.AddPart(new Pointing(self));
        self.AddPart(new ChatHud(self, cam));
    }

    public static void On_RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            if (self.hud == null && self.followAbstractCreature?.realizedObject is Creature owner && (owner is not Player slug || slug.isNPC))
            {
                StoryMenagerie.Debug("followed creature is " + owner);
                if (owner != menagerie.avatars[0].realizedCreature) { StoryMenagerie.LogError($"Camera owner != avatar {owner} {menagerie.avatars[0]}"); }
                if (CreatureController.creatureControllers.TryGetValue(self.followAbstractCreature?.realizedObject as Creature, out var crit))
                {
                    FireUpMenagerieHud(self, crit);
                    StoryMenagerie.Debug("HUD fired up!");
                }
                else
                {
                    StoryMenagerie.LogError("Could not find controller; HUD not fired up");
                }
            }
        }
        orig(self);
    }

    public static Creature HUDOwnerCreature(HudPart self) => (self.hud.owner is CreatureController crit && crit.isStory(out var _)) ? crit.creature : null;

    public static void IL_HypothermiaMeter_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(HUDOwnerCreature);
            c.Emit(OpCodes.Dup);
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Brtrue, skip);
            c.Emit(OpCodes.Pop);
            c.GotoNext(x => x.MatchCallvirt<Creature>("get_Hypothermia"));
            c.MarkLabel(skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError("IL.HypothermiaMeter.Update hook failed! " + ex);
        }
    }

    public static void ReplaceAsPlayerHook(ILContext il)
    {
        var c = new ILCursor(il);
        ReplaceAsPlayer(c);
    }

    public static void ReplaceAsPlayer(ILCursor c)
    {
        if (c.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<HudPart>(nameof(HudPart.hud)),
            x => x.MatchLdfld<HUD.HUD>(nameof(HUD.HUD.owner)),
            x => x.MatchIsinst<Player>()
        ))
        {
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(HUDOwnerCreature);
            c.Emit(OpCodes.Dup);
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Brtrue, skip);
            c.Emit(OpCodes.Pop);
            c.GotoNext(MoveType.After, x => x.MatchIsinst<Player>());
            c.MarkLabel(skip);
            StoryMenagerie.Debug("Player Isinst successfully replaced");
        }
        else
        {
            StoryMenagerie.LogError("Player Isinst was not replaced!");
        }
    }

    public static bool OwnedByCreature(HudPart self) => self.hud.owner.GetOwnerType() == CreatureController.controlledCreatureHudOwner;

    public static void IL_RainMeter_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip = c.DefineLabel();
            ReplaceAsPlayer(c);
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<HudPart>(nameof(HudPart.hud)),
                x => x.MatchLdfld<HUD.HUD>(nameof(HUD.HUD.owner)),
                x => x.MatchIsinst<Player>(),
                x => x.MatchLdfld<Player>(nameof(Player.inVoidSea)),
                x => x.MatchBrfalse(out skip)
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(OwnedByCreature);
            c.Emit(OpCodes.Brtrue, skip);
            for (int i = 0; i < 9; i++)
            {
                ReplaceAsPlayer(c);
            }
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void On_TextPrompt_ctor(On.HUD.TextPrompt.orig_ctor orig, TextPrompt self, HUD.HUD hud)
    {
        orig(self, hud);
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode && self.hud.owner.GetOwnerType() == CreatureController.controlledCreatureHudOwner)
        {
            self.subregionTracker = new SubregionTracker(self);
        }
    }

    public static bool TextPromptCreatureFree(TextPrompt self) => self.hud.owner is CreatureController crit && !crit.creature.dead && self.dependentOnGrasp.discontinued;

    public static void IL_TextPrompt_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchStfld<TextPrompt>(nameof(TextPrompt.gameOverMode))
            );
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<HudPart>(nameof(HudPart.hud)),
                x => x.MatchLdfld<HUD.HUD>(nameof(HUD.HUD.owner)),
                x => x.MatchIsinst<Player>()
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(TextPromptCreatureFree);
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Brtrue, skip);
            // skips to set gameOverMode to false
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(0)
            );
            c.MarkLabel(skip);
            var skip2 = c.DefineLabel();
            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<HudPart>(nameof(HudPart.hud)),
                x => x.MatchLdfld<HUD.HUD>(nameof(HUD.HUD.owner)),
                x => x.MatchIsinst<Player>(),
                x => x.MatchBrfalse(out var _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<HudPart>(nameof(HudPart.hud)),
                x => x.MatchLdfld<HUD.HUD>(nameof(HUD.HUD.owner)),
                x => x.MatchIsinst<Player>(),
                x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
                x => x.MatchLdfld<Room>(nameof(Room.game)),
                x => x.MatchCallvirt<RainWorldGame>("get_IsStorySession"),
                x => x.MatchBrtrue(out skip2)
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(OwnedByCreature);
            c.Emit(OpCodes.Brtrue, skip2);
            ReplaceAsPlayer(c);
            ReplaceAsPlayer(c);
            ReplaceAsPlayer(c);
            ReplaceAsPlayer(c);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void IL_RainMeter_ctor(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<HUD.HUD>(nameof(HUD.HUD.owner)),
                x => x.MatchIsinst<Player>()
            ))
            {
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(HUDOwnerCreature);
                c.Emit(OpCodes.Dup);
                var skip = c.DefineLabel();
                c.Emit(OpCodes.Brtrue, skip);
                c.Emit(OpCodes.Pop);
                c.GotoNext(MoveType.After, x => x.MatchIsinst<Player>());
                c.MarkLabel(skip);
                StoryMenagerie.Debug("Player Isinst successfully replaced");
            }
        } catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void On_SubregionTracker_Update(On.HUD.SubregionTracker.orig_Update orig, SubregionTracker self)
    {
        if (self.textPrompt.hud.owner.GetOwnerType() == CreatureController.controlledCreatureHudOwner)
        {
            // copy pasted from source code to change the player var to be a creature
            var crit = (self.textPrompt.hud.owner as CreatureController).creature;
            int num = 0;
            if (crit.room != null && !crit.room.world.singleRoomWorld && crit.room.world.region != null)
            {
                for (int i = 1; i < crit.room.world.region.subRegions.Count; i++)
                {
                    if (crit.room.abstractRoom.subregionName == crit.room.world.region.subRegions[i])
                    {
                        num = i;
                        break;
                    }
                }
            }
            if (!self.DEVBOOL && num != 0 && crit.room.game.manager.menuSetup.startGameCondition == ProcessManager.MenuSetup.StoryGameInitCondition.Dev)
            {
                self.lastShownRegion = num;
                self.DEVBOOL = true;
            }
            if (num != self.lastShownRegion && crit.room != null && num != 0 && self.lastRegion == num && self.textPrompt.show == 0f)
            {
                bool flag = false;
                for (int j = 0; j < crit.room.warpPoints.Count; j++)
                {
                    if (crit.room.warpPoints[j].timeWarpTearClosed <= 20)
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag || self.counter == 1 || self.counter == 75)
                {
                    self.counter++;
                }
                if (self.counter > 80)
                {
                    if ((num > 1 || self.lastShownRegion == 0 || (crit.room.world.region.name != "SS" && crit.room.world.region.name != "DM")) && num < crit.room.world.region.subRegions.Count)
                    {
                        if (self.showCycleNumber && crit.room.game.IsStorySession && crit.room.game.manager.menuSetup.startGameCondition == ProcessManager.MenuSetup.StoryGameInitCondition.Load)
                        {
                            int num2 = crit.room.game.GetStorySession.saveState.cycleNumber;
                            if (crit.room.game.StoryCharacter == SlugcatStats.Name.Red && !Custom.rainWorld.ExpeditionMode)
                            {
                                num2 = RedsIllness.RedsCycles(crit.room.game.GetStorySession.saveState.redExtraCycles) - num2;
                            }
                            string s = crit.room.world.region.subRegions[num];
                            if (num < crit.room.world.region.altSubRegions.Count && crit.room.world.region.altSubRegions[num] != null)
                            {
                                s = crit.room.world.region.altSubRegions[num];
                            }
                            self.textPrompt.AddMessage(string.Concat(new string[]
                            {
                                self.textPrompt.hud.rainWorld.inGameTranslator.Translate("Cycle"),
                                " ",
                                num2.ToString(),
                                " ~ ",
                                self.textPrompt.hud.rainWorld.inGameTranslator.Translate(s)
                            }), 0, 160, false, true);
                        }
                        else
                        {
                            string s2 = crit.room.world.region.subRegions[num];
                            if (num < crit.room.world.region.altSubRegions.Count && crit.room.world.region.altSubRegions[num] != null)
                            {
                                s2 = crit.room.world.region.altSubRegions[num];
                            }
                            self.textPrompt.AddMessage(self.textPrompt.hud.rainWorld.inGameTranslator.Translate(s2), 0, 160, false, true);
                        }
                    }
                    self.showCycleNumber = false;
                    self.lastShownRegion = num;
                }
            }
            else
            {
                self.counter = 0;
            }
            self.lastRegion = num;
            return;
        }
        orig(self);
    }

    public static void On_PlayerSessionRecord_AddEat(On.PlayerSessionRecord.orig_AddEat orig, PlayerSessionRecord self, PhysicalObject eatenObject)
    {
        orig(self, eatenObject);
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            menagerie.showKarmaFoodRainTime = 300;
        }
    }

    public static void IL_FoodMeter_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip = c.DefineLabel();
            c.GotoNext( x => x.MatchCall<FoodMeter>(nameof(FoodMeter.GameUpdate)));
            c.GotoPrev(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCall<FoodMeter>("get_IsPupFoodMeter"),
                x => x.MatchBrtrue(out skip)
            );
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(HudPart).GetField(nameof(HudPart.hud)));
            c.Emit(OpCodes.Ldfld, typeof(HUD.HUD).GetField(nameof(HUD.HUD.owner)));
            c.Emit(OpCodes.Callvirt, typeof(IOwnAHUD).GetMethod(nameof(IOwnAHUD.GetOwnerType), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
            c.Emit(OpCodes.Ldsfld, typeof(CreatureController).GetField(nameof(CreatureController.controlledCreatureHudOwner)));
            c.Emit(OpCodes.Call, typeof(ExtEnum<HUD.HUD.OwnerType>).GetMethod("op_Equality"));
            c.Emit(OpCodes.Brtrue, skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void On_KarmaMeter_UpdateGraphic(On.HUD.KarmaMeter.orig_UpdateGraphic orig, KarmaMeter self)
    {
        if (self.hud.owner is CreatureController crit && crit.creature.room.game.session is StoryGameSession story)
        {
            var data = story.saveState.deathPersistentSaveData;
            self.showAsReinforced = data.reinforcedKarma;
            if (data.rippleLevel >= 1f)
            {
                self.karmaSprite.element = Futile.atlasManager.GetElementWithName(KarmaMeter.RippleSymbolSprite(true, data.rippleLevel));
                self.baseColor = RainWorld.SaturatedGold;
                self.karmaSprite.color = self.baseColor;
                return;
            }
            self.displayKarma.x = data.karma;
            self.displayKarma.y = data.karmaCap;
        }
        orig(self);
    }

    public static bool UpdateForceSleep(FoodMeter self)
    {
        if (self.hud.owner is CreatureController cc && cc.isStory(out var scc))
        {
            var crit = cc.creature;
            if (scc.forceSleepCounter > 0)
            {
                self.forceSleep = Custom.LerpAndTick(self.forceSleep, Mathf.Pow(Mathf.InverseLerp(10f, 260f, (float)scc.forceSleepCounter), 0.75f), 0.014f, 1f / Mathf.Lerp(180f, 4f, self.forceSleep));
            }
            else if (crit.room != null && crit.room.abstractRoom.shelter && !crit.inShortcut && cc.input[0].y < 0 && !cc.input[0].jmp && !cc.input[0].thrw && !cc.input[0].pckp && crit.IsTileSolid(1, 0, -1) && (cc.input[0].x == 0 || ((!crit.IsTileSolid(1, -1, -1) || !crit.IsTileSolid(1, 1, -1)) && crit.IsTileSolid(1, cc.input[0].x, 0))))
            {
                self.forceSleep = Mathf.Max(-1f, self.forceSleep - 0.016666668f);
            }
            else
            {
                self.forceSleep = Mathf.Max(0f, self.forceSleep - 0.033333335f);
            }
            return true;
        }
        return false;
    }

    public static void IL_FoodMeter_GameUpdate(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<HUD.HudPart>(nameof(HUD.HudPart.hud)),
                x => x.MatchLdfld<HUD.HUD>(nameof(HUD.HUD.owner)),
                x => x.MatchIsinst<Player>(),
                x => x.MatchLdfld<Player>(nameof(Player.forceSleepCounter))
            ))
            {
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(UpdateForceSleep);
                var skip = c.DefineLabel();
                c.Emit(OpCodes.Brtrue, skip);
                c.GotoNext(MoveType.After,
                    x => x.MatchLdcR4(0.033333335f),
                    x => x.MatchSub(),
                    x => x.MatchCall(out var _),
                    x => x.MatchStfld<FoodMeter>(nameof(FoodMeter.forceSleep))
                );
                c.MarkLabel(skip);
            } else
            {
                StoryMenagerie.LogError("IL.FoodMeter.GameUpdate hook failed!");
            }
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void On_KarmaMeter_Update(On.HUD.KarmaMeter.orig_Update orig, KarmaMeter self)
    {
        if (self.hud.owner is CreatureController cc && cc.creature.room != null && cc.creature.room.abstractRoom.gate && cc.creature.room.regionGate != null && cc.creature.room.regionGate.mode == RegionGate.Mode.MiddleClosed)
        {
            self.forceVisibleCounter = Math.Max(self.forceVisibleCounter, 10);
        }
        orig(self);
    }

    public static void On_FoodMeter_ctor(On.HUD.FoodMeter.orig_ctor orig, FoodMeter self, HUD.HUD hud, int maxFood, int survivalLimit, Player associatedPup, int pupNumber)
    {
        if (hud.owner.GetOwnerType() == CreatureController.controlledCreatureHudOwner)
        {
            self.lastCount = hud.owner.CurrentFood;
        }
        orig(self, hud, maxFood, survivalLimit, associatedPup, pupNumber);
    }

    public static float On_ThreatPulser_get_Threat(Func<ThreatPulser, float> orig, ThreatPulser self)
    {
        if (self.hud.owner is CreatureController cc)
        {
            if (cc.creature == null || cc.creature.room == null)
            {
                return 0f;
            }
            var game = cc.creature.room.game;

            if (game.GameOverModeActive)
            {
                return 0f;
            }
            if (game.manager.musicPlayer != null)
            {
                return game.manager.musicPlayer.threatTracker.currentMusicAgnosticThreat;
            }
            if (game.manager.fallbackThreatDetermination == null)
            {
                game.manager.fallbackThreatDetermination = new ThreatDetermination(0);
            }
            return game.manager.fallbackThreatDetermination.currentMusicAgnosticThreat;
        }
        return orig(self);
    }

    public static bool On_ThreatPulser_get_Show(Func<ThreatPulser, bool> orig, ThreatPulser self)
    {
        if (self.hud.owner is CreatureController cc)
        {
            var submerged = (cc.creature is AirBreatherCreature creature && creature.lungs < 1f);
            if (!MMF.cfgBreathTimeVisualIndicator.Value)
            {
                submerged = false;
            }
            return !submerged && self.Threat > 0f;
        }
        return orig(self);
    }
}
