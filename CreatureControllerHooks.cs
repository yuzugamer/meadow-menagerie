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
        Creatures.EggbugController.ApplyHooks();
        Creatures.LizardController.ApplyHooks();
        // Dr meadow
        Creatures.LongLegsController.ApplyHooks();
        Creatures.BarnacleController.ApplyHooks();
        Creatures.DropBugController.ApplyHooks();

        On.Creature.SafariControlInputUpdate += (On.Creature.orig_SafariControlInputUpdate orig, Creature self, int index) =>
        {
            // Non local? Don't own!
            if (OnlineManager.lobby != null && !self.IsLocal())
            {
                self.inputWithoutDiagonals = null;
                self.lastInputWithoutDiagonals = null;
                self.inputWithDiagonals = null;
                self.lastInputWithDiagonals = null;
            }
            else
            {
                orig(self, index);
            }
        };

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
        new Hook(typeof(NoodleController).GetMethod(nameof(NoodleController.NeedleWormGraphics_ApplyPalette), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), On_NoodleController_NeedleWormGraphics_ApplyPalette);
        new Hook(typeof(ScavengerController).GetMethod(nameof(ScavengerController.ScavengerGraphics_ctor), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), On_ScavengerController_ScavengerGraphics_ctor);
        //var scavengerController = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => t.IsClass && t.Namespace == nameof(RainMeadow)).First(t => t.Name == "ScavengerController");
        //new Hook(scavengerController.GetMethod("ConsciousUpdate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic), On_ScavengerController_ConsciousUpdate);
        new Hook(typeof(CreatureController).GetMethod("Call", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance), On_CreatureController_Call);
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
            if (self.creature.IsLocal() && self.creature.room != null && self.creature.room.game.devToolsActive && self.onlineCreature.isMine)
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
            if (self.creature.IsLocal() && story != null)
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
                    if (devTools) StoryMenagerie.Debug("Not sheltering, is local: " + self.creature.IsLocal() + " is shelter: " + self.creature.room.abstractRoom.shelter + " is story: " + self.creature.room.game.IsStorySession + " is alive: " + !self.creature.dead + " sleep counter is 0: " + self.sleepCounter == 0 + " shelter door exists:" + (self.creature.room.shelterDoor != null) + " shelter door not broken" + (self.creature.room.shelterDoor == null ? false : !self.creature.room.shelterDoor.Broken));
                }

                clientData.isDead = self.creature == null || self.creature.dead;
            }

            // relevant to story - karma flower placement
            if (story != null)
            {
                if (!self.creature.dead && self.creature.grabbedBy.Count == 0 && self.creature.IsTileSolid(self.creature.mainBodyChunkIndex, 0, -1))
                {
                    var pos = self.creature.mainBodyChunk.pos;
                    if (!self.creature.room.GetTile(pos).DeepWater && !self.creature.IsTileSolid(self.creature.mainBodyChunkIndex, 0, 0) && !self.creature.room.GetTile(pos).wormGrass && (self.creature.room == null || !self.creature.room.readyForAI || !self.creature.room.aimap.getAItile(self.creature.room.GetTilePosition(pos)).narrowSpace))
                    {
                        scc.karmaFlowerGrowPos = new WorldCoordinate?(self.creature.room.GetWorldCoordinate(pos));
                    }
                }
            }
            /* this is from vanilla but could be reworked into a more flexible system.*/
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
                if (scc.dangerGraspTime == 60 && self.creature.IsLocal() && (dangerGrasp.grabber is not Lizard liz || liz.AI.DynamicRelationship((self.creature as Creature).abstractCreature).type == CreatureTemplate.Relationship.Type.Eats))
                {
                    self.creature.room.game.GameOver(dangerGrasp);
                }
            }

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

    public static void On_ScavengerController_ScavengerGraphics_ctor(Action<On.ScavengerGraphics.orig_ctor, ScavengerGraphics, PhysicalObject> orig, On.ScavengerGraphics.orig_ctor origorig, ScavengerGraphics self, PhysicalObject ow)
    {
        if (StoryMenagerie.IsMenagerie && ow.abstractPhysicalObject.ID.number == -273819595)
        {
            origorig(self, ow);
            return;
        }
        orig(origorig, self, ow);
    }

    public static void On_CreatureController_Call(Action<CreatureController> orig, CreatureController self)
    {
        if (!StoryMenagerie.IsMenagerie) orig(self);
    }
}
