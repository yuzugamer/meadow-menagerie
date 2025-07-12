using MonoMod.RuntimeDetour;
using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using UnityEngine;
using System.Runtime.CompilerServices;
using static RainMeadow.CreatureController;
using JetBrains.Annotations;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace StoryMenagerie;

public static class CreatureControllerHooks
{

    //public static MethodInfo GetSpecialInput;
    //public static MethodInfo CreatureControllerMoving;
    //public static MethodInfo CreatureControllerLookImpl;
    //public static MethodInfo CreatureControllerResting;
    public static MethodInfo GroundConsciousUpdate;
    public static void Apply()
    {
        Creatures.CentipedeController.ApplyHooks();
        Creatures.LanternMouseController.ApplyHooks();
        Creatures.JetFishController.ApplyHooks();
        Creatures.YeekController.ApplyHooks();
        Creatures.BigEelController.ApplyHooks();
        // Dr meadow
        Creatures.LongLegsController.ApplyHooks();
        Creatures.BarnacleController.ApplyHooks();
        Creatures.DropBugController.ApplyHooks();

        //GetSpecialInput = typeof(CreatureController).GetMethod("GetSpecialInput", BindingFlags.NonPublic | BindingFlags.Instance);
        //CreatureControllerMoving = typeof(CreatureController).GetMethod("Moving", BindingFlags.NonPublic | BindingFlags.Instance);
        //CreatureControllerLookImpl = typeof(CreatureController).GetMethod("LookImpl", BindingFlags.NonPublic | BindingFlags.Instance);
        //CreatureControllerResting = typeof(CreatureController).GetMethod("Resting", BindingFlags.NonPublic | BindingFlags.Instance);
        GroundConsciousUpdate = typeof(GroundCreatureController).GetMethod("ConsciousUpdate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        new Hook(typeof(CreatureController).GetConstructor(new Type[] { typeof(Creature), typeof(OnlineCreature), typeof(int), typeof(MeadowAvatarData) }), On_CreatureController_ctor);
        new Hook(typeof(LizardController).GetMethod("LizardGraphics_ctor", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static), On_LizardController_LizardGraphics_ctor);
        new Hook(typeof(CreatureController).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance), On_CreatureController_Update);
        new Hook(typeof(CreatureController).GetMethod("get_CurrentFood", BindingFlags.Instance | BindingFlags.Public), On_get_CurrentFood);
        new Hook(typeof(CreatureController).GetMethod("CheckInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), On_CreatureController_CheckInput);
        new ILHook(typeof(LizardController).GetConstructor(new Type[] { typeof(Lizard), typeof(OnlineCreature), typeof(int), typeof(MeadowAvatarData) }), IL_LizardController_ctor);
        new ILHook(typeof(LizardController).GetMethod("ConsciousUpdate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), IL_LizardController_ConsciousUpdate);
        new Hook(typeof(NoodleController).GetMethod(nameof(NoodleController.NeedleWormGraphics_ApplyPalette), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), On_NoodleController_NeedleWormGraphics_ApplyPalette);
        new Hook(typeof(EggbugController).GetMethod(nameof(EggbugController.EggBugGraphics_ApplyPalette), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), On_EggBugController_EggBugGraphics_ApplyPalette);
        new Hook(typeof(ScavengerController).GetMethod(nameof(ScavengerController.ScavengerGraphics_ctor), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), On_ScavengerController_ScavengerGraphics_ctor);
        //var scavengerController = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => t.IsClass && t.Namespace == nameof(RainMeadow)).First(t => t.Name == "ScavengerController");
        //new Hook(scavengerController.GetMethod("ConsciousUpdate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic), On_ScavengerController_ConsciousUpdate);
    }

    public delegate void On_LizardController_orig_LizardGraphics_ctor(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow);
    public delegate void On_CreatureController_orig_ctor(CreatureController self, Creature creature, OnlineCreature oc, int playerNumber, MeadowAvatarData customization);
    public delegate void On_ScavengerController_orig_ConsciousUpdate(ScavengerController self);
    public static void On_CreatureController_ctor(On_CreatureController_orig_ctor orig, CreatureController self, Creature creature, OnlineCreature oc, int playerNumber, MeadowAvatarData customization)
    {
        // just in case
        if ((OnlineManager.lobby == null || StoryMenagerie.IsMenagerie) && oc.TryGetData<SlugcatCustomization>(out var data))
        {
            // most of the code is yoinked, but what can 'ya do
            self.creature = creature;
            self.template = creature.Template;
            self.onlineCreature = oc;
            self.effectColor = creature.ShortCutColor();
            self.needsLight = true;
            self.voice = new MeadowVoice(self);
            self.input = new Player.InputPackage[2];
            self.specialInput = new SpecialInput[2];
            var story = self.story();
            self.mcd = oc.GetData<MeadowCreatureData>();
            story.stillInStartShelter = true;
            self.story().storyCustomization = data;
            self.customization = new ExpandedAvatarData(data);
            //if (self.customization == null) self.customization = new ExpandedAvatarData();
            //(self.customization as ExpandedAvatarData).owner = self;
            //story.state = new PlayerState(creature.abstractCreature, playerNumber, creature.room.game.StoryCharacter, false);

            CreatureController.creatureControllers.Add(creature, self);

            self.standStillOnMapButton = creature.abstractCreature.world.game.IsStorySession;
            self.flipDirection = 1;

            RainMeadow.RainMeadow.Debug(self + " added!");

            if (oc.isMine && self.template.AI && creature.abstractCreature.world.GetAbstractRoom(creature.abstractCreature.pos) != null)
            {
                //creature.abstractCreature.abstractAI.RealAI.pathFinder.visualize = true;
                //self.debugDestinationVisualizer = new DebugDestinationVisualizer(creature.abstractCreature.world.game.abstractSpaceVisualizer, creature.abstractCreature.world, creature.abstractCreature.abstractAI.RealAI.pathFinder, Color.green);
            }
            return;
        }
        orig(self, creature, oc, playerNumber, customization);
    }

    public static void StoryLizardSkins(LizardController self)
    {

    }

    public static void On_LizardController_LizardGraphics_ctor(On_LizardController_orig_LizardGraphics_ctor orig, On.LizardGraphics.orig_ctor origorig, LizardGraphics self, PhysicalObject ow)
    {
        StoryMenagerie.Debug("lizard graphics ctor");
        if (ow is Lizard lizard && CreatureController.creatureControllers.TryGetValue(lizard, out var c) && c is LizardController liz)
        {
            StoryMenagerie.Debug("is story lizard");
            origorig(self, ow);
            StoryLizardSkins(liz);
            return;
        }
        StoryMenagerie.Debug("is not story lizard");
        orig(origorig, self, ow);
    }

    public static int On_get_CurrentFood(Func<CreatureController, int> orig, CreatureController self)
    {
        //StoryMenagerie.LogDebug("food being checked" + Environment.StackTrace);
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie) return menagerie.foodPoints;
        return orig(self);
    }

    public static void On_CreatureController_CheckInput(Action<CreatureController> orig, CreatureController self)
    {
        if (self.isStory(out var story))
        {
            for (int i = self.input.Length - 1; i > 0; i--)
            {
                self.input[i] = self.input[i - 1];
            }
            if (self.onlineCreature.isMine)
            {
                if (self.creature.stun == 0 && !self.creature.dead)
                {
                    self.input[0] = RWInput.PlayerInput(0);
                    if (self.forceInputCounter > 0)
                    {
                        self.input[0].x = self.forceInputDir.x;
                        self.input[0].y = self.forceInputDir.y;
                    }
                }
                else
                {
                    self.input[0] = new Player.InputPackage(Custom.rainWorld.options.controls[self.playerNumber].gamePad, Custom.rainWorld.options.controls[self.playerNumber].GetActivePreset(), 0, 0, false, false, false, false, false);
                }
                self.mcd.input = self.input[0];
            }
            else
            {
                self.input[0] = self.mcd.input;
            }
            for (int i = self.specialInput.Length - 1; i > 0; i--)
            {
                self.specialInput[i] = self.specialInput[i - 1];
            }

            if (self.onlineCreature.isMine)
            {
                self.specialInput[0] = self.GetSpecialInput(self.creature.DangerPos - self.creature.room.game.cameras[0].pos, self.playerNumber);

                self.mcd.specialInput = self.specialInput[0];
            }
            else
            {
                self.specialInput[0] = self.mcd.specialInput;
            }
            if (ChatTextBox.blockInput)
            {
                self.input[0].x = 0;
                self.input[0].y = 0;
                self.input[0].analogueDir = default;
                self.input[0].jmp = false;
                self.input[0].thrw = false;
                self.input[0].pckp = false;
                self.input[0].spec = false;
                self.input[0].mp = false;
            }
            self.rawInput = self.input[0];
            if (self.preventInput || (self.standStillOnMapButton && self.input[0].mp) || self.sleepCounter != 0)
            {
                self.input[0].x = 0;
                self.input[0].y = 0;
                self.input[0].analogueDir = default;
                self.input[0].jmp = false;
                self.input[0].thrw = false;
                self.input[0].pckp = false;
                self.blink = Mathf.Max(self.blink, 5);
            }

            // no input
            if (self.input[0].x == 0 && self.input[0].y == 0 && !self.input[0].jmp && !self.input[0].thrw && !self.input[0].pckp)
            {
                self.touchedNoInputCounter++;
            }
            else
            {
                self.touchedNoInputCounter = 0;
            }
            return;
        }
        orig(self);
    }

    public static void On_CreatureController_Update(Action<CreatureController, bool> orig, CreatureController self, bool eu)
    {
        orig(self, eu);
        if (self.isStory(out var scc))
        {
            var local = self.creature.IsLocal();
            var story = self.creature.room.world.game.session as StoryGameSession;

            // prevent pointing with throw, since throw button is used for other things
            // should make right click use lookimpl
            if (self.specialInput[0].direction != Vector2.zero)
            {
                self.pointCounter = 10;
                self.pointing = true;
            }
            else
            {
                self.pointCounter = 0;
                self.pointing = false;
            }

            if (scc.mark == null)
            {
                scc.mark = new MarkSprite(self.creature);
                scc.mark.controller = self;
                self.creature.room.AddObject(scc.mark);
            }

            if (scc.mark.slatedForDeletetion || scc.mark.room != self.creature.room || self.creature.slatedForDeletetion)
            {
                scc.mark.Destroy();
                scc.mark = null;
            }

            self.needsLight = story.saveState.deathPersistentSaveData.theMark;

            //StoryMenagerie.LogDebug("current stun is set to" + self.creature.stun);
            if (local && self.creature.room != null && self.creature.room.game.devToolsActive && self.onlineCreature.isMine)
            {
                // relevant to story
                if (Input.GetKeyDown("q"))
                {
                    self.AddFood(1);
                }
            }
            var devTools = self.creature.room.game.devToolsActive;
            // a lot of things copypasted from from p.update
            // bunch of unimplemented story things
            // relevant to story
            // shelter activation
            if (local && story != null)
            {
                var menagerie = OnlineManager.lobby.gameMode as MenagerieGameMode;
                var clientData = menagerie.storyClientData;
                clientData.readyForWin = false;
                self.readyForWin = false;
                if (self.creature.room.abstractRoom.shelter && self.creature.room.game.IsStorySession && !self.creature.dead && self.sleepCounter == 0 && self.creature.room.shelterDoor != null && !self.creature.room.shelterDoor.Broken)
                {
                    if (devTools) StoryMenagerie.Debug("IN SHELTER FOOD: " + self.FoodInRoom(self.creature.room, false) + "FOOD TO HIBERNATE: " + self.foodToHibernate() + ", MAX FOOD: " + self.maxFood());
                    if (!scc.stillInStartShelter && self.FoodInRoom(self.creature.room, false) >= ((!story.saveState.malnourished) ? self.foodToHibernate() : self.maxFood()))
                    {
                        clientData.readyForWin = true;
                        self.readyForWin = true;
                        scc.forceSleepCounter = 0;
                        StoryMenagerie.Debug("Ready for win!");
                    }
                    else if (self.creature.room.world.rainCycle.timer > self.creature.room.world.rainCycle.cycleLength)
                    {
                        if (self.FoodInRoom(self.creature.room, false) >= (story.saveState.malnourished ? self.maxFood() : self.foodToHibernate()))
                        {
                            clientData.readyForWin = true;
                            self.readyForWin = true;
                            scc.forceSleepCounter = 0;
                        }
                        else
                        {
                            StoryMenagerie.Debug("starving is not currently implemented, so perish!");
                            if (self.creature.State is HealthState state)
                            {
                                state.health -= 0.001f;
                            }
                            else if (UnityEngine.Random.value < 0.001f)
                            {
                                self.creature.Die();
                            }
                        }
                    }
                    else if (self.input[0].y < 0 && !self.input[0].jmp && !self.input[0].thrw && !self.input[0].pckp && self.creature.IsTileSolid(1, 0, -1) && !story.saveState.malnourished && self.FoodInRoom(self.creature.room, false) > 0 && self.FoodInRoom(self.creature.room, false) < self.foodToHibernate() && (self.input[0].x == 0 || ((!self.creature.IsTileSolid(1, -1, -1) || !self.creature.IsTileSolid(1, 1, -1)) && self.creature.IsTileSolid(1, self.input[0].x, 0))))
                    {
                        // foodmeter breaks if starved, and base meadow doesn't currently support starving anyway, so i can't be bothered
                        // update: meadow's starting to support starvation. better get to it!!!
                        //scc.forceSleepCounter++;
                    }
                    else
                    {
                        if (devTools) StoryMenagerie.Debug("not sheltering! left start shelter: " + !scc.stillInStartShelter + " ");
                        scc.forceSleepCounter = 0;
                    }
                    if (Custom.ManhattanDistance(self.creature.abstractCreature.pos.Tile, self.creature.room.shortcuts[0].StartTile) > 6)
                    {
                        if (self.readyForWin && self.touchedNoInputCounter > 20)
                        {
                            //self.sleepCounter = 1;
                            self.creature.room.shelterDoor.Close();
                            StoryMenagerie.Debug("Shelter closed!");
                        }
                        else if (scc.forceSleepCounter > 260)
                        {
                            //self.sleepCounter = -24;
                            menagerie.foodPoints = 0;
                            self.creature.room.shelterDoor.Close();
                            StoryMenagerie.Debug("Shelter forcefullyy closed");
                        }
                        else
                        {
                            if (devTools) StoryMenagerie.Debug("Did not close shelter, no inputs held for " + self.touchedNoInputCounter);
                        }
                        if (!menagerie.readyForWin) StoryMenagerie.Debug("client is ready for win, but not the gamemode!");
                    }
                    else
                    {
                        if (devTools) StoryMenagerie.Debug("too close to shelter entrance!");
                    }
                }
                else
                {
                    if (devTools) StoryMenagerie.Debug("Not sheltering, is local: " + local + " is shelter: " + self.creature.room.abstractRoom.shelter + " is story: " + self.creature.room.game.IsStorySession + " is alive: " + !self.creature.dead + " sleep counter is 0: " + self.sleepCounter == 0 + " shelter door exists:" + (self.creature.room.shelterDoor != null) + " shelter door not broken" + (self.creature.room.shelterDoor == null ? false : !self.creature.room.shelterDoor.Broken));
                }

                clientData.isDead = self.creature == null || self.creature.dead;
            }

            // relevant to story
            // karma flower placement
            if (story != null)
            {
                //if (self.creature.room.game.cameras[0].hud != null && !self.creature.room.game.cameras[0].hud.textPrompt.gameOverMode)
                //{
                //    self.SessionRecord.time++;
                //}
                if (!self.creature.dead && self.creature.grabbedBy.Count == 0 && self.creature.IsTileSolid(self.creature.mainBodyChunkIndex, 0, -1))
                {
                    var pos = self.creature.mainBodyChunk.pos;
                    if (!self.creature.room.GetTile(pos).DeepWater && !self.creature.IsTileSolid(self.creature.mainBodyChunkIndex, 0, 0) && !self.creature.room.GetTile(pos).wormGrass && (self.creature.room == null || !self.creature.room.readyForAI || !self.creature.room.aimap.getAItile(self.creature.room.GetTilePosition(pos)).narrowSpace))
                    {
                        scc.karmaFlowerGrowPos = new WorldCoordinate?(self.creature.room.GetWorldCoordinate(pos));
                    }
                }
            }
            /*
            // relevant to story
            // SHROOMIES
            if (this.mushroomCounter > 0)
            {
                if (!this.inShortcut)
                {
                    this.mushroomCounter--;
                }
                this.mushroomEffect = Custom.LerpAndTick(this.mushroomEffect, 1f, 0.05f, 0.025f);
            }
            else
            {
                this.mushroomEffect = Custom.LerpAndTick(this.mushroomEffect, 0f, 0.025f, 0.014285714f);
            }
            if (this.Adrenaline > 0f)
            {
                if (this.adrenalineEffect == null)
                {
                    this.adrenalineEffect = new AdrenalineEffect(this);
                    this.room.AddObject(this.adrenalineEffect);
                }
                else if (this.adrenalineEffect.slatedForDeletetion)
                {
                    this.adrenalineEffect = null;
                }
            }

            // relevant to story
            // death grasp
             this is from vanilla but could be reworked into a more flexible system.*/
            var dangerGrasp = scc.dangerGrasp;
            if (dangerGrasp == null)
            {
                scc.dangerGraspTime = 0;
                foreach (var grasp in self.creature.grabbedBy)
                {
                    if (grasp.grabber is Lizard || grasp.grabber is Vulture || grasp.grabber is BigSpider || grasp.grabber is DropBug || grasp.pacifying)  //cmon joarge
                    {
                        scc.dangerGrasp = grasp;
                    }
                }
            }
            else if (scc.dangerGrasp.discontinued || (!dangerGrasp.pacifying && self.creature.stun <= 0))
            {
                scc.dangerGrasp = null;
                scc.dangerGraspTime = 0;
            }
            else
            {
                scc.dangerGraspTime++;
                // lizards will eventually drop things they can't eat (with the exception of major statistical anomalies), which means the grasp is not fatal
                if (scc.dangerGraspTime == 60 && local && (dangerGrasp.grabber is not Lizard liz || liz.AI.DynamicRelationship((self.creature as Creature).abstractCreature).type == CreatureTemplate.Relationship.Type.Eats))
                {
                    self.creature.room.game.GameOver(dangerGrasp);
                }
            }

            // relevant to story AND meadow?
            // map progression specifics
            /*if (this.MapDiscoveryActive && this.coord != this.lastCoord)
            {
                if (this.exitsToBeDiscovered == null)
                {
                    if (this.room != null && this.room.shortCutsReady)
                    {
                        this.exitsToBeDiscovered = new List<Vector2>();
                        for (int i = 0; i < this.room.shortcuts.Length; i++)
                        {
                            if (this.room.shortcuts[i].shortCutType == ShortcutData.Type.RoomExit)
                            {
                                this.exitsToBeDiscovered.Add(this.room.MiddleOfTile(this.room.shortcuts[i].StartTile));
                            }
                        }
                    }
                }
                else if (this.exitsToBeDiscovered.Count > 0 && this.room.game.cameras[0].hud != null && this.room.game.cameras[0].hud.map != null && !this.room.CompleteDarkness(this.firstChunk.pos, 0f, 0.95f, false))
                {
                    int index = UnityEngine.Random.Range(0, this.exitsToBeDiscovered.Count);
                    if (this.room.ViewedByAnyCamera(this.exitsToBeDiscovered[index], -10f))
                    {
                        Vector2 vector = this.firstChunk.pos;
                        for (int j = 0; j < 20; j++)
                        {
                            if (Custom.DistLess(vector, this.exitsToBeDiscovered[index], 50f))
                            {
                                this.room.game.cameras[0].hud.map.ExternalExitDiscover((vector + this.exitsToBeDiscovered[index]) / 2f, this.room.abstractRoom.index);
                                this.room.game.cameras[0].hud.map.ExternalOnePixelDiscover(this.exitsToBeDiscovered[index], this.room.abstractRoom.index);
                                this.exitsToBeDiscovered.RemoveAt(index);
                                break;
                            }
                            this.room.game.cameras[0].hud.map.ExternalSmallDiscover(vector, this.room.abstractRoom.index);
                            vector += Custom.DirVec(vector, this.exitsToBeDiscovered[index]) * 50f;
                        }
                    }
                }
            }*/
            if (self.creature.grabbedBy.Count < 1)
            {
                var stuck = true;
                foreach (var chunk in self.creature.bodyChunks)
                {
                    if (!self.creature.room.GetTile(chunk.pos).Solid)
                    {
                        stuck = false;
                        break;
                    }
                }
                if (stuck)
                {
                    Custom.LogWarning(new string[]
                    {
                "WALLSTUCK"
                    });
                    self.creature.bodyChunks[0].HardSetPosition(self.creature.bodyChunks[0].pos + Custom.DirVec(self.creature.bodyChunks[0].pos, self.creature.bodyChunks[1].pos) * 2f);
                    var move = new Vector2(self.input[0].x, self.input[0].y).normalized * 1.5f;
                    foreach (var chunk in self.creature.bodyChunks)
                    {
                        chunk.vel = default(Vector2);
                        var pos = chunk.pos + move;
                        pos.y += self.creature.gravity;
                        chunk.HardSetPosition(pos);
                    }
                }
            }
        }
    }

    public static void IL_LizardController_ctor(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchCallvirt<Lizard>("get_effectColor"),
                x => x.MatchStloc(0)
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Isinst, typeof(LizardController));
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Brtrue, skip);
            c.GotoNext(
                MoveType.After,
                x => x.MatchCallvirt<Lizard>("set_effectColor"),
                x => x.MatchNop()
            );
            c.MoveAfterLabels();
            c.MarkLabel(skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void IL_LizardController_ConsciousUpdate(ILContext il)
    {
        try
        {
            // don't judge me
            var c = new ILCursor(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchCall<GroundCreatureController>(nameof(GroundCreatureController.ConsciousUpdate)),
                x => x.MatchNop()
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Isinst, typeof(LizardController));
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Brfalse, skip);
            c.Emit(OpCodes.Ret);
            c.MarkLabel(skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }


    // this might be one of the worst things i've ever had to do
    public static void On_NoodleController_NeedleWormGraphics_ApplyPalette(Action<ILContext> orig, ILContext il)
    {
        var c = new ILCursor(il);
        c.GotoNext(MoveType.Before,
            i => i.MatchLdarg(1)
            );
        c.GotoPrev(MoveType.After,
            i => i.MatchStfld<NeedleWormGraphics>("highLightColor")
            );

        c.MoveAfterLabels();
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Action<NeedleWormGraphics>>((self) =>
        {
            if (creatureControllers.TryGetValue(self.worm, out var p))
            {
                p.customization.ModifyBodyColor(ref self.detailsColor);
                self.highLightColor = Color.Lerp(self.highLightColor, Color.black, 0.4f);
                p.customization.ModifyBodyColor(ref self.highLightColor);
                self.highLightColor = Color.Lerp(self.highLightColor, Color.white, 0.4f);
                p.customization.ModifyBodyColor(ref self.bodyColor);
            }
        });
    }

    // the sequel
    public static void On_EggBugController_EggBugGraphics_ApplyPalette(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(MoveType.AfterLabel,
            i => i.MatchStfld<EggBugGraphics>("blackColor")
            );

        c.GotoPrev(MoveType.After,
            i => i.MatchStloc(out _));

        c.MoveAfterLabels();
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloca, 0);
        c.Emit(OpCodes.Ldloca, 1);
        c.EmitDelegate((EggBugGraphics self, ref Color c1, ref Color c2) =>
        {
            if (creatureControllers.TryGetValue(self.bug, out var p))
            {
                var diff = c1 - c2;
                p.customization.ModifyBodyColor(ref c1);
                p.customization.ModifyBodyColor(ref c2);
                c2 += diff;
            }
        });

        // egg colors
        c.GotoNext(i => i.MatchCallOrCallvirt<EggBugGraphics>("EggColors"));
        c.GotoNext(MoveType.After, i => i.MatchStfld<EggBugGraphics>("eggColors"));
        c.MoveAfterLabels();
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((EggBugGraphics self) =>
        {
            if (creatureControllers.TryGetValue(self.bug, out var p))
            {
                p.customization.ModifyBodyColor(ref self.eggColors[0]);
                p.customization.ModifyBodyColor(ref self.eggColors[1]);
                p.customization.ModifyBodyColor(ref self.eggColors[2]);
            }
        });
    }

    public static void On_ScavengerController_ScavengerGraphics_ctor(Action<On.ScavengerGraphics.orig_ctor, ScavengerGraphics, PhysicalObject> orig, On.ScavengerGraphics.orig_ctor origorig, ScavengerGraphics self, PhysicalObject ow)
    {
        if (StoryMenagerie.IsMenagerie && ow.abstractPhysicalObject.ID.number == -273819595)
        {
            origorig(self, ow);
            return;
        }
        orig(origorig, self, ow);
    }
}
