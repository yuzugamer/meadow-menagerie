using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RainMeadow;
using Watcher;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using System.Globalization;

namespace StoryMenagerie;

public static class GameHooks
{
    public static void Apply()
    {
        new Hook(typeof(RainWorldGame).GetMethod("get_AlivePlayers", BindingFlags.Instance | BindingFlags.Public), On_RainWorldGame_get_AlivePlayers);
        // might be a bit ambitious
        //new Hook(typeof(RainWorldGame).GetMethod("FirstAlivePlayer", BindingFlags.Instance | BindingFlags.Public), On_RainWorldGame_get_FirstAlivePlayer);
        new Hook(typeof(RainWorldGame).GetMethod("get_PlayersToProgressOrWin", BindingFlags.Instance | BindingFlags.Public), On_RainWorldGame_get_PlayersToProgressOrWin);
        //On.Lizard.Act += LizardController.On_Lizard_Act;
        On.ShelterDoor.DoorClosed += On_ShelterDoor_DoorClosed;
        On.SaveState.BringUpToDate += On_SaveState_BringUpToDate;
        On.SaveState.SessionEnded += On_SaveState_SessionEnded;
        IL.RainWorldGame.ctor += IL_RainWorldGame_ctor;
        IL.ShelterDoor.Update += IL_ShelterDoor_Update;
        On.RegionState.CreatureToStringInDenPos += On_RegionState_CreatureToStringInDenPos;
        On.RegionGate.PlayersInZone += On_RegionGate_PlayersInZone;
        On.RegionGate.PlayersStandingStill += On_RegionGate_PlayersStandingStill;
        On.RegionGate.AllPlayersThroughToOtherSide += On_RegionGate_AllPlayersThroughToOtherSide;
        On.RegionGate.ListOfPlayersInZone += On_RegionGate_ListOfPlayersInZone;
        new Hook(typeof(RegionGate).GetMethod("get_MeetRequirement", BindingFlags.Instance | BindingFlags.Public), On_RegionGate_get_MeetRequirement);
        IL.RainWorldGame.Win += IL_RainWorldGame_Win;
        On.ShortcutGraphics.GenerateSprites += On_ShortcutGraphics_GenerateSprites;
        On.ShortcutGraphics.Draw += On_ShortcutGraphics_Draw;
        On.RainWorldGame.ctor += On_RainWorldGame_ctor;
        On.SeedCob.Update += On_SeedCob_Update;
        IL.RegionState.AdaptRegionStateToWorld += IL_RegionState_AdaptRegionStateToWorld;
        //On.GateKarmaGlyph.ShouldAnimate += On_GateKarmaGlyph_ShouldAnimate;

        On.SLOracleBehaviorHasMark.NameForPlayer += (On.SLOracleBehaviorHasMark.orig_NameForPlayer orig, SLOracleBehaviorHasMark self, bool capitalized) =>
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode gameMode)
            {
                return "gamer";
            }
            else
            {
                return orig(self, capitalized);
            }
        };
        On.SLOracleBehaviorHasMark.InitateConversation += (On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self) =>
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode gameMode)
            {
                self.dialogBox.Interrupt(self.Translate("you'll pay for your slugsins."), 5);
            }
            orig(self);
        };

        On.SSOracleBehavior.PebblesConversation.AddEvents += (On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self) =>
        {
			if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode gameMode && self.id == Conversation.ID.Pebbles_White)
			{
				if (!self.owner.playerEnteredWithMark)
				{
					self.events.Add(new Conversation.TextEvent(self, 0, ".  .  .", 0));
					self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...am I fuckin' reaching you?"), 0));
					self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 4));
				}
				else
				{
					self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 210));
				}
                if (OnlineManager.players.Count > 1)
                {
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Hey you brought friends! Hi silly goobers!!! Let's see..."), 0));
                    List<CreatureTemplate.Type> seenCreatures = [];
                    foreach (var avi in gameMode.onlineAvatars)
                    {
                        var ct = avi.abstractCreature.creatureTemplate;
                        var count = seenCreatures.Where((e) => e == ct.type).Count();
                        if (ct.TopAncestor().type == CreatureTemplate.Type.Centipede)
                        {
                            switch (count)
                            {
                                case 0:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...a centi; fair enough. I should've bought a spore puff"), 0));
                                    break;
                                case 1:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...another centi - do we really need more than one centi? I'd say zero is the preferable amount."), 0));
                                    break;
                                default:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...another centi"), 0));
                                    break;
                            }
                        }
                        else if (ct.TopAncestor().type == MoreSlugcatsEnums.CreatureTemplateType.TrainLizard)
                        {
                            switch (count)
                            {
                                case 0:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...a train lizard? What? Don't you only spawn in the bloody enot campaign?"), 0));
                                    break;
                                case 1:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...another bloody train lizard? Is this enot campaign? Let me check..."), 0));
                                    if (gameMode.currentCampaign == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
                                    {
                                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh it is, well makes sense, wait, you shouldn't be seeing this dialogue."), 0));
                                    }
                                    else if (gameMode.currentCampaign == MoreSlugcatsEnums.SlugcatStatsName.Spear)
                                    {
                                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It's just boring spearmaster ok."), 0));
                                    }
                                    else if (gameMode.currentCampaign == SlugcatStats.Name.Red)
                                    {
                                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("IT'S HUNTER HAHA"), 0));
                                    }
                                    else if (gameMode.currentCampaign == SlugcatStats.Name.White)
                                    {
                                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Survivor? Fair enough."), 0));
                                    }
                                    else
                                    {
                                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("No it's not. Ok."), 0));
                                    }
                                    break;
                                default:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...another lizard"), 0));
                                    break;
                            }
                        }
                        else if (ct.TopAncestor().type == CreatureTemplate.Type.LizardTemplate)
                        {
                            switch (count)
                            {
                                case 0:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...a lizard. I'd expect to see atleast one of them here..."), 0));
                                    break;
                                case 1:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...two lizards? Ok... makes sense I suppose"), 0));
                                    break;
                                case 2:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...another fuckin' lizard. Of course, there's more lizards. Yep."), 0));
                                    break;
                                default:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...another lizard"), 0));
                                    break;
                            }
                        }
                        else if (ct.TopAncestor().type == CreatureTemplate.Type.DropBug)
                        {
                            switch (count)
                            {
                                case 0:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...a dropwig"), 0));
                                    break;
                                case 1:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...another fuckin' dropwig"), 0));
                                    break;
                                default:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...another dropwig"), 0));
                                    break;
                            }
                        }
                        else if (ct.TopAncestor().type == CreatureTemplate.Type.DaddyLongLegs)
                        {
                            switch (count)
                            {
                                case 0:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...IT'S THE ROT WHAT THE HE-"), 0));
                                    break;
                                case 1:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Ok fair enough, I hate all of this - too much rot, by lore I should destroy all of you but I can't be fucked to code that lmao"), 0));
                                    //self.owner?.action = SSOracleBehavior.Action.ThrowOut_KillOnSight;
                                    break;
                                default:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...another funny rot"), 0));
                                    break;
                            }
                        }
                        else
                        {
                            switch (count)
                            {
                                case 0:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...a ") + " " + self.Translate(ct.name), 0));
                                    break;
                                case 1:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...duplicate; how boring."), 0));
                                    break;
                                default:
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...duplicate"), 0));
                                    break;
                            }
                        }
                        seenCreatures.Add(ct.type);
                    }
                }
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Crikey mate you made a bad decision comin' here"), 0));
				self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("The bloody toaster won't run for longer mate."), 0));
				self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Right mate we have found like a solution right - but bloody hell does it not work."), 0));
				self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("So pretend you're a slugcat and go into the void sea mate, take a bath or something."), 0));
				self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 10));
				if (self.owner.playerEnteredWithMark)
				{
					self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Why you have the bloody mark? You fookin' don't know how much valuable the mark is right?"), 0));
				}
				else
				{
					self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Now I'm going to like, give you the mark and you're supposed to leave or else I obliterate you, understood mate?"), 0));
				}
				self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I'm running out of lines can we get to the action now? Thanks cheers mate."), 0));
				self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 20));
				if (self.owner.oracle.room.game.IsStorySession && self.owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.memoryArraysFrolicked)
				{
					self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You came from fuckin' unfortunate development? I wonder if you were the rot and played with your rot friends - BLIMEY!"), 0));
					return;
				}
				if (ModManager.MSC && self.owner.CheckSlugpupsInRoom())
				{
					self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("All slugpups will go to hell."), 0));
					self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Especially the ones you just brought here, bloody hell."), 0));
					self.owner.CreatureJokeDialog();
					return;
				}
				if (ModManager.MMF && self.owner.CheckStrayCreatureInRoom() != CreatureTemplate.Type.StandardGroundCreature)
				{
					self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I find odd things happening aroun' here y'know?"), 0));
					self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It's not the same hood mate."), 0));
					self.owner.CreatureJokeDialog();
					return;
				}
				self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Anyways get the hell out of here; SCRAM OR ELSE"), 0));
				return;
			}
            orig(self);
        };
    }

    public static void On_Creature_SpitOutOfShortCut(On.Creature.orig_SpitOutOfShortCut orig, Creature self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
    {
        if (CreatureController.creatureControllers.TryGetValue(self, out var crit) && crit.isStory(out var story)) story.stillInStartShelter = false;
        orig(self, pos, newRoom, spitOutAllSticks);
    }

    public static List<AbstractCreature> On_RainWorldGame_get_AlivePlayers(Func<RainWorldGame, List<AbstractCreature>> orig, RainWorldGame self)
    {
        if(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode)
        {
            List<AbstractCreature> results = new List<AbstractCreature>() { };
            foreach (var player in self.Players)
            {
                if (player.state is PlayerState pstate && !pstate.dead && !pstate.permaDead)
                {
                    results.Add(player);
                }
                else
                {
                    var rCrit = player.realizedCreature;
                    if(rCrit != null && CreatureController.creatureControllers.TryGetValue(rCrit, out var crit) && crit.isStory(out var story) && !story.state.dead && !story.state.permaDead)
                    {
                        results.Add(player);
                    }
                }
            }
            return results;
        }
        return orig(self);
    }

    public static void On_ShelterDoor_DoorClosed(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
    {
        if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MenagerieGameMode menagerie)
        {
            orig(self);
        }
        else
        {
            bool win = true;
            // not sure if this is better or not? intuitively seems better, but not super sure
            win = menagerie.abstractAvatars.Any(avi => avi.realizedCreature != null && avi.state != null && avi.state.alive && avi.Room == self.room.abstractRoom);
            if (win)
            {
                self.room.game.Win(menagerie.FoodInRoom(false) < SlugcatStats.SlugcatFoodMeter(self.room.game.GetStorySession.saveStateNumber).y, false);
            }
            else
            {
                self.room.game.GoToDeathScreen();
            }
        }
    }
    
    public static void On_SaveState_BringUpToDate(On.SaveState.orig_BringUpToDate orig, SaveState self, RainWorldGame game)
    {
        if(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            Custom.Log(new string[]
            {
                "----Adapt save state to world"
            });
            AbstractCreature acrit = null;
            foreach (var avi in menagerie.onlineAvatars)
            {
                var avicrit = avi.abstractCreature;
                if (avicrit != null && avicrit.state.alive) acrit = avicrit;
            }
            if (acrit == null)
            {
                acrit = menagerie.onlineAvatars[0].abstractCreature;
                StoryMenagerie.LogError("(SaveState.BringUpToDate) No living avatar found in! Defaulting to first avatar");
            }
            WorldCoordinate pos = acrit.pos;
            if (pos == default(WorldCoordinate))
            {
                Custom.LogWarning(new string[]
                {
            "(SaveState.BringUpToDate) Player Pos was null!!"
                });
            }
            AbstractRoom abstractRoom = game.world.GetAbstractRoom(pos);
            if (pos == default(WorldCoordinate) && abstractRoom == null)
            {
                foreach (var avi in menagerie.onlineAvatars)
                {
                    if (avi != null)
                    {
                        var tempPos = avi.abstractCreature.pos;
                        if (tempPos != default(WorldCoordinate) && game.world.GetAbstractRoom(tempPos) != null)
                        {
                            pos = tempPos;
                            abstractRoom = game.world.GetAbstractRoom(pos);
                            break;
                        }
                    }
                }
            }
            self.denPosition = abstractRoom.name;
            self.TrySetVanillaDen(abstractRoom.name);
            self.nextIssuedID = game.nextIssuedId;
            var grabbed = new List<string>();
            foreach (var avatar in menagerie.onlineAvatars)
            {
                var avi = avatar.realizedCreature;
                if (avi != null)
                {
                    foreach (var grasp in avi.grasps)
                    {
                        if (grasp != null && grasp.grabbed != null)
                        {
                            var apo = grasp.grabbed.abstractPhysicalObject;
                            var grabbedCrit = apo as AbstractCreature;
                            if (grabbedCrit != null)
                            {
                                grabbed.Add(SaveState.AbstractCreatureToStringStoryWorld(grabbedCrit));
                            }
                            else
                            {
                                grabbed.Add(apo.ToString());
                            }
                        }
                    }
                }
            }
            if (grabbed.Count > 0)
            {
                self.playerGrasps = grabbed.ToArray();
            }
            else
            {
                self.playerGrasps = null;
            }
            game.world.regionState.AdaptRegionStateToWorld(pos.room, -1);
            self.creatureCommunitiesString = game.session.creatureCommunities.ToString();
            bool itemsSwallowed = false;
            foreach (var avi in menagerie.onlineAvatars)
            {
                if(avi.realizedCreature != null && avi.realizedCreature is Player scug)
                {
                    var playerState = scug.playerState;
                    if (playerState != null && playerState.swallowedItem != null)
                    {
                        itemsSwallowed = true;
                        break;
                    }
                }
            }
            if (itemsSwallowed)
            {
                var slugcats = menagerie.abstractAvatars.Where(ac => ac.creatureTemplate.type == CreatureTemplate.Type.Slugcat);
                self.swallowedItems = new string[slugcats.Count()];
                int i = 0;
                foreach (var slug in slugcats)
                {
                    if (slug.realizedCreature == null) continue;
                    var scug = slug.realizedCreature as Player;
                    if (scug != null && scug.objectInStomach != null)
                    {
                        var swallowedCrit = scug.objectInStomach as AbstractCreature;
                        if (swallowedCrit != null)
                        {
                            if (game.world.GetAbstractRoom(swallowedCrit.pos.room) == null)
                            {
                                swallowedCrit.pos = scug.coord;
                            }
                            self.swallowedItems[i] = SaveState.AbstractCreatureToStringStoryWorld(swallowedCrit);
                        }
                        else
                        {
                            self.swallowedItems[i] = scug.objectInStomach.ToString();
                        }
                    }
                    else
                    {
                        self.swallowedItems[i] = "0";
                    }
                    // jolly co-op stuff grrr
                    /*PlayerState playerState2 = game.session.Players[i].state as PlayerState;
                    if (ModManager.CoopAvailable && playerState2 != null && self.swallowedItems[i].Equals("0") && playerState2.swallowedItem != null)
                    {
                        self.swallowedItems[i] = playerState2.swallowedItem;
                        playerState2.swallowedItem = null;
                    }*/
                    i++;
                }
                return;
            }
            self.swallowedItems = null;
            return;
        }
        orig(self, game);
    }

    public static void On_SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
    {
        if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MenagerieGameMode story)
        {
            orig(self, game, survived, newMalnourished);
            return;
        }
        // yoinked this part from meadow's SessionEnded hook, since this hook is cancelling orig and is run before meadow's hook due to mod ordering
        if (story.myLastDenPos is not (null or ""))
        {
            self.denPosition = story.myLastDenPos;
            if (OnlineManager.lobby.isOwner) story.defaultDenPos = story.myLastDenPos;
        }
        if (story.myLastWarp is not null)
        {
            self.warpPointTargetAfterWarpPointSave = story.myLastWarp;
        }
        // mostly yoinked code, but so much of it had to be dynamically modified that i felt it more worth to copy paste the code, rather than make a thousand il hooks
        self.lastMalnourished = self.malnourished;
        self.malnourished = newMalnourished;
        self.deathPersistentSaveData.sessionTrackRecord.Add(new DeathPersistentSaveData.SessionRecord(survived, game.GetStorySession.playerSessionRecords[0].wokeUpInRegion != game.world.region.name));
        if (self.deathPersistentSaveData.sessionTrackRecord.Count > 20)
        {
            self.deathPersistentSaveData.sessionTrackRecord.RemoveAt(0);
        }
        for (int i = self.deathPersistentSaveData.deathPositions.Count - 1; i >= 0; i--)
        {
            if (self.deathPersistentSaveData.deathPositions[i].Valid)
            {
                self.deathPersistentSaveData.deathPositions[i] = new WorldCoordinate(self.deathPersistentSaveData.deathPositions[i].room, self.deathPersistentSaveData.deathPositions[i].x, self.deathPersistentSaveData.deathPositions[i].y, self.deathPersistentSaveData.deathPositions[i].abstractNode + 1);
            }
            else
            {
                self.deathPersistentSaveData.deathPositions[i] = new WorldCoordinate(self.deathPersistentSaveData.deathPositions[i].unknownName, self.deathPersistentSaveData.deathPositions[i].x, self.deathPersistentSaveData.deathPositions[i].y, self.deathPersistentSaveData.deathPositions[i].abstractNode + 1);
            }
            if (self.deathPersistentSaveData.deathPositions[i].abstractNode >= 7)
            {
                self.deathPersistentSaveData.deathPositions.RemoveAt(i);
            }
        }
        if (survived)
        {
            self.deathPersistentSaveData.foodReplenishBonus = 0;
            Custom.Log(new string[]
            {
            "resetting food rep bonus"
            });
            if (!self.sessionEndingFromSpinningTopEncounter)
            {
                self.RainCycleTick(game, true);
                self.cyclesInCurrentWorldVersion++;
            }
            if (ModManager.MMF && self.progression.miscProgressionData.returnExplorationTutorialCounter > 0)
            {
                self.progression.miscProgressionData.returnExplorationTutorialCounter = 3;
            }
            self.food = 0;
            /*if (ModManager.CoopAvailable)
            {
                var avatars = story.abstractAvatars;
                var firstPlayerScug = false;
                Player scug = null;
                StoryCreatureController crit = null;
                if (avatars[0].creatureTemplate.type == CreatureTemplate.Type.Slugcat || (ModManager.MSC && avatars[0].creatureTemplate.type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC))
                {
                    firstPlayerScug = true;
                    if (avatars[0].realizedCreature != null)
                    {
                        scug = avatars[0].realizedCreature as Player;
                    } else
                    {
                        StoryMenagerie.LogError("Could not find realized slugcat at SaveState.SessionEnded!");
                    }
                }
                else
                {
                    if (avatars[0].realizedCreature != null && CreatureController.creatureControllers.TryGetValue(avatars[0].realizedCreature, out var critController))
                    {
                        crit = critController as StoryCreatureController;
                    }
                    else
                    {
                        StoryMenagerie.LogError("Could not find controlled creature at SaveState.SessionEnded!");
                    }
                }
                if (!(firstPlayerScug ? (avatars[0].state as PlayerState).permaDead : !avatars[0].state.alive) && avatars[0].realizedCreature != null && avatars[0].realizedCreature.room != null)
                {
                    if (self.sessionEndingFromSpinningTopEncounter)
                    {
                        if (firstPlayerScug)
                        {
                            self.food = scug.FoodInStomach;
                        } else
                        {
                            self.food = crit.foodInStomach;
                        }
                    }
                    else
                    {
                        if (firstPlayerScug)
                        {
                            self.food = scug.FoodInRoom(true);
                        }
                        else
                        {
                            self.food = crit.FoodInRoom(crit.room, true);
                        }
                    }
                }
                else if (game.AlivePlayers.Count > 0 && game.FirstAlivePlayer != null && avatars[0].realizedCreature != null)
                {
                    if (self.sessionEndingFromSpinningTopEncounter)
                    {
                        if (firstPlayerScug)
                        {
                            self.food = (game.FirstAlivePlayer.realizedCreature as Player).FoodInStomach;
                        }
                        else
                        {
                            self.food = crit.foodInStomach;
                        }
                    }
                    else
                    {
                        if (firstPlayerScug)
                        {
                            self.food = scug.FoodInRoom(true);
                        } else
                        {
                            self.food = crit.FoodInRoom(crit.room, true);
                        }
                    }
                }
            } */
            //else
            //{
            var avatars = story.abstractAvatars;
            foreach (var avi in avatars)
            {
                if ((avi.creatureTemplate.type == CreatureTemplate.Type.Slugcat || (ModManager.MSC && avi.creatureTemplate.type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC)) && avi.realizedCreature != null)
                {
                    self.food += self.sessionEndingFromSpinningTopEncounter ? (avi.realizedCreature as Player).FoodInStomach : (avi.realizedCreature as Player).FoodInRoom(true);
                } else if (avi.realizedCreature != null && CreatureController.creatureControllers.TryGetValue(avi.realizedCreature, out var cc) && cc.isStory(out var scc))
                {
                    self.food += self.sessionEndingFromSpinningTopEncounter ? story.foodPoints : cc.FoodInRoom(cc.creature.room, true);
                }
            }
            //}
            self.food = Custom.IntClamp(self.food, 0, game.GetStorySession.characterStats.maxFood);
            if (!self.sessionEndingFromSpinningTopEncounter)
            {
                if (self.malnourished)
                {
                    self.food -= game.GetStorySession.characterStats.foodToHibernate;
                }
                else if (self.lastMalnourished)
                {
                    if (game.devToolsActive && self.food < game.GetStorySession.characterStats.maxFood)
                    {
                        Custom.LogWarning(new string[]
                        {
                        "FOOD COUNT ISSUE!",
                        self.food.ToString(),
                        game.GetStorySession.characterStats.maxFood.ToString()
                        });
                    }
                    self.food = 0;
                }
                else
                {
                    self.food -= game.GetStorySession.characterStats.foodToHibernate;
                }
            }
            if (game.IsStorySession)
            {
                game.GetStorySession.saveState.skipNextCycleFoodDrain = false;
            }
            if (self.sessionEndingFromSpinningTopEncounter && avatars[0].realizedCreature != null && avatars[0].realizedCreature.room != null)
            {
                SaveState.forcedEndRoomToAllowwSave = avatars[0].realizedCreature.room.abstractRoom.name;
            }
            self.BringUpToDate(game);
            if (self.sessionEndingFromSpinningTopEncounter)
            {
                SaveState.forcedEndRoomToAllowwSave = "";
            }
            for (int i = 0; i < game.GetStorySession.playerSessionRecords.Length; i++)
            {
                if (game.GetStorySession.playerSessionRecords[i] != null && !self.sessionEndingFromSpinningTopEncounter && (!ModManager.CoopAvailable || game.world.GetAbstractRoom(avatars[i].pos) != null))
                {
                    game.GetStorySession.playerSessionRecords[i].pupCountInDen = 0;
                    bool flag = false;
                    game.GetStorySession.playerSessionRecords[i].wentToSleepInRegion = game.world.region.name;
                    foreach (var creature in game.world.GetAbstractRoom(avatars[i].pos).creatures)
                    {
                        // not sure if the contains check is necessary tbh idek it's 2 am
                        if (creature.state.alive && !story.abstractAvatars.Contains(creature) && creature.state.socialMemory != null && creature.realizedCreature != null && creature.abstractAI != null && creature.abstractAI.RealAI != null && creature.abstractAI.RealAI.friendTracker != null && creature.abstractAI.RealAI.friendTracker.friend != null && creature.abstractAI.RealAI.friendTracker.friend == avatars[i].realizedCreature && creature.state.socialMemory.GetLike(avatars[i].ID) > 0f)
                        {
                            if (ModManager.MSC && creature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC)
                            {
                                if ((creature.state as PlayerNPCState).foodInStomach - ((creature.state as PlayerNPCState).Malnourished ? SlugcatStats.SlugcatFoodMeter(MoreSlugcatsEnums.SlugcatStatsName.Slugpup).x : SlugcatStats.SlugcatFoodMeter(MoreSlugcatsEnums.SlugcatStatsName.Slugpup).y) >= 0)
                                {
                                    game.GetStorySession.playerSessionRecords[i].pupCountInDen++;
                                }
                            }
                            else if (!flag)
                            {
                                flag = true;
                                game.GetStorySession.playerSessionRecords[i].friendInDen = creature;
                                SocialMemory.Relationship orInitiateRelationship = creature.state.socialMemory.GetOrInitiateRelationship(avatars[i].ID);
                                orInitiateRelationship.like = Mathf.Lerp(orInitiateRelationship.like, 1f, 0.5f);
                            }
                        }
                    }
                }
            }
            self.AppendKills(game.GetStorySession.playerSessionRecords[0].kills);
            /*if (ModManager.CoopAvailable)
            {
                for (int m = 1; m < game.GetStorySession.playerSessionRecords.Length; m++)
                {
                    self.AppendKills(game.GetStorySession.playerSessionRecords[m].kills);
                }
            }*/
            game.GetStorySession.AppendTimeOnCycleEnd(false);
            if (!self.sessionEndingFromSpinningTopEncounter)
            {
                self.deathPersistentSaveData.survives++;
                self.deathPersistentSaveData.winState.CycleCompleted(game);
                if (!ModManager.CoopAvailable)
                {
                    self.deathPersistentSaveData.friendsSaved += ((game.GetStorySession.playerSessionRecords[0].friendInDen != null) ? 1 : 0);
                }
                else
                {
                    List<AbstractCreature> list = new List<AbstractCreature>();
                    foreach (PlayerSessionRecord playerSessionRecord in game.GetStorySession.playerSessionRecords)
                    {
                        if (!story.abstractAvatars.Contains(playerSessionRecord.friendInDen) && !list.Contains(playerSessionRecord.friendInDen))
                        {
                            list.Add(playerSessionRecord.friendInDen);
                        }
                    }
                    self.deathPersistentSaveData.friendsSaved += list.Count;
                }
            }
            self.deathPersistentSaveData.karma++;
            self.deathPersistentSaveData.rippleLevel = Mathf.Clamp(self.deathPersistentSaveData.rippleLevel + 0.5f, self.deathPersistentSaveData.minimumRippleLevel, self.deathPersistentSaveData.maximumRippleLevel);
            if (self.malnourished)
            {
                self.deathPersistentSaveData.reinforcedKarma = false;
            }
            game.rainWorld.progression.SaveWorldStateAndProgression(self.malnourished);
            return;
        }
        game.GetStorySession.AppendTimeOnCycleEnd(true);
        if (game.cameras[0].hud != null)
        {
            self.deathPersistentSaveData.AddDeathPosition(game.cameras[0].hud.textPrompt.deathRoom, game.cameras[0].hud.textPrompt.deathPos);
        }
        self.deathPersistentSaveData.deaths++;
        if (self.deathPersistentSaveData.karma == 0 || (self.saveStateNumber == SlugcatStats.Name.White && UnityEngine.Random.value < 0.5f) || self.saveStateNumber == SlugcatStats.Name.Yellow)
        {
            self.deathPersistentSaveData.foodReplenishBonus++;
            Custom.Log(new string[]
            {
            "Ticking up food rep bonus to:",
            self.deathPersistentSaveData.foodReplenishBonus.ToString()
            });
        }
        else
        {
            Custom.Log(new string[]
            {
            "death screen, no food bonus"
            });
        }
        self.deathPersistentSaveData.TickFlowerDepletion(1);
        if (ModManager.MMF && MMF.cfgExtraTutorials.Value)
        {
            Custom.Log(new string[]
            {
            "Exploration tutorial counter :",
            self.progression.miscProgressionData.returnExplorationTutorialCounter.ToString()
            });
            if (game.IsStorySession && (game.world.region.name == "SB" || game.world.region.name == "SL" || game.world.region.name == "UW" || self.deathPersistentSaveData.karmaCap > 8 || self.miscWorldSaveData.SSaiConversationsHad > 0))
            {
                self.progression.miscProgressionData.returnExplorationTutorialCounter = -1;
                Custom.Log(new string[]
                {
                "CANCEL exploration counter"
                });
            }
            else if (game.IsStorySession && (game.world.region.name == "SH" || game.world.region.name == "VS" || game.world.region.name == "DS" || game.world.region.name == "CC" || game.world.region.name == "LF" || game.world.region.name == "SI"))
            {
                Custom.Log(new string[]
                {
                "Exploration counter ticked to",
                self.progression.miscProgressionData.returnExplorationTutorialCounter.ToString()
                });
                if (self.progression.miscProgressionData.returnExplorationTutorialCounter > 0)
                {
                    PlayerProgression.MiscProgressionData miscProgressionData = self.progression.miscProgressionData;
                    int n = miscProgressionData.returnExplorationTutorialCounter;
                    miscProgressionData.returnExplorationTutorialCounter = n - 1;
                }
            }
            else if (self.progression.miscProgressionData.returnExplorationTutorialCounter > 0)
            {
                self.progression.miscProgressionData.returnExplorationTutorialCounter = 3;
                Custom.Log(new string[]
                {
                "Reset exploration counter"
                });
            }
        }
        game.rainWorld.progression.SaveProgressionAndDeathPersistentDataOfCurrentState(true, false);
    }

    public static List<AbstractCreature> On_RainWorldGame_get_PlayersToProgressOrWin(Func<RainWorldGame, List<AbstractCreature>> orig, RainWorldGame self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode story)
        {
            return story.abstractAvatars;
        }
        return orig(self);
    }

    public static bool AvatarsInsteadOfPlayers(RainWorldGame self)
    {

        if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MenagerieGameMode story) return false;
        var food = self.GetStorySession.saveState.food;
        foreach (var avi in story.abstractAvatars)
        {
            // set spawn positions
            if (self.world.GetAbstractRoom(avi.pos) != null)
            {
                IntVector2 tile;
                if (self.world.GetAbstractRoom(avi.pos).shelter)
                {
                    avi.pos.WashTileData();
                }
                else if (RainWorldGame.TryGetPlayerStartPos(self.world.GetAbstractRoom(avi.pos).name, out tile))
                {
                    avi.pos.Tile = tile;
                }
            }
            // set food in stomach
            if (avi.creatureTemplate.type == CreatureTemplate.Type.Slugcat || (ModManager.MSC && avi.creatureTemplate.type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC))
            {
                (avi.state as PlayerState).foodInStomach = food;
            }
            else if (avi.realizedCreature != null && CreatureController.creatureControllers.TryGetValue(avi.realizedCreature, out var controller))
            {
                story.foodPoints = food;
            }
        }
        return true;
    }

    public static void IL_RainWorldGame_ctor(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip = c.DefineLabel();
            c.GotoNext(x => x.MatchLdfld<PlayerState>(nameof(PlayerState.foodInStomach)));
            c.GotoPrev(
                x => x.MatchLdarg(0),
                x => x.MatchCall<RainWorldGame>("get_IsStorySession"),
                x => x.MatchBrfalse(out skip)
            );
            c.GotoPrev(
                 x => x.MatchLdcI4(0),
                 x => x.MatchStloc(out var _),
                 x => x.MatchBr(out var _)
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(AvatarsInsteadOfPlayers);
            c.Emit(OpCodes.Brtrue, skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError("IL.RainWorldGame.ctor hook failed!" + ex);
        }
    }

    public static bool DoNotStunCreature(ShelterDoor self, int i) => OnlineManager.lobby != null && CreatureController.creatureControllers.TryGetValue(self.room.abstractRoom.creatures[i].realizedCreature, out var _);

    public static void IL_ShelterDoor_Update(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip = c.DefineLabel();
            c.GotoNext(
                x => x.MatchCallvirt<Creature>("set_stun")
            );
            c.GotoPrev(
                MoveType.After,
                x => x.MatchCallvirt<AbstractCreature>("get_realizedCreature"),
                x => x.MatchBrfalse(out skip)
            );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 11);
            c.EmitDelegate(DoNotStunCreature);
            c.Emit(OpCodes.Brtrue, skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError("IL.ShelterDoor.Update hook failed!" + ex);
        }
    }

    public static string On_RegionState_CreatureToStringInDenPos(On.RegionState.orig_CreatureToStringInDenPos orig, RegionState self, AbstractCreature critter, int validSaveShelter, int activeGate)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode.avatars.Select(oc => oc.abstractCreature).Contains(critter))
        {
            return "";
        }
        return orig(self, critter, validSaveShelter, activeGate);
    }

    public static int On_RegionGate_PlayersInZone(On.RegionGate.orig_PlayersInZone orig, RegionGate self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            if (self.room == null)
            {
                return -1;
            }
            int num = -1;
            StoryMenagerie.Debug("avatar count is " + menagerie.abstractAvatars.Count);
            foreach (var avi in menagerie.abstractAvatars)
            {
                int num3 = self.DetectZone(avi);
                if (num3 != num && num != -1)
                {
                    num = -1;
                    break;
                }
                num = num3;
            }
            return num;
        }
        return orig(self);
    }

    public static bool On_RegionGate_PlayersStandingStill(On.RegionGate.orig_PlayersStandingStill orig, RegionGate self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            foreach (var avi in menagerie.abstractAvatars)
            {
                if (avi.realizedCreature == null || (avi.realizedCreature is Player scug && scug.touchedNoInputCounter < 20) || (CreatureController.creatureControllers.TryGetValue(avi.realizedCreature, out var crit) && crit.touchedNoInputCounter < 20))
                {
                    return false;
                }
            }
            return true;
        }
        return orig(self);
    }

    public static bool On_RegionGate_AllPlayersThroughToOtherSide(On.RegionGate.orig_AllPlayersThroughToOtherSide orig, RegionGate self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            foreach (var avi in menagerie.abstractAvatars)
            {
                if (avi.pos.room == self.room.abstractRoom.index && (!self.letThroughDir || avi.pos.x < self.room.TileWidth / 2 + 3) && (self.letThroughDir || avi.pos.x > self.room.TileWidth / 2 - 4))
                {
                    return false;
                }
            }
            return true;
        }
        return orig(self);
    }

    public static int On_GateKarmaGlyph_ShouldAnimate(On.GateKarmaGlyph.orig_ShouldAnimate orig, GateKarmaGlyph self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode)
        {
            if (self.requirement != MoreSlugcatsEnums.GateRequirement.RoboLock || self.gate.mode != RegionGate.Mode.MiddleClosed || self.PlayNoEnergyAnimation || self.gate.unlocked || self.gate.letThroughDir == self.side)
            {
                return 0;
            }
            if (ModManager.Watcher && self.gate.room.game.session is StoryGameSession && self.gate.room.game.StoryCharacter == WatcherEnums.SlugcatStatsName.Watcher)
            {
                if (self.gate.room.game.session is StoryGameSession story && story.saveState.deathPersistentSaveData.maximumRippleLevel >= 1f)
                {
                    return 0;
                }
            }
            int num = self.gate.PlayersInZone();
            if (num <= 0 || num >= 3)
            {
                return 0;
            }
            self.gate.letThroughDir = (num == 1);
            if (!self.gate.dontOpen && !self.gate.MeetRequirement)
            {
                return -1;
            }
            return 1;
        }
        return orig(self);
    }

    public static List<Player> On_RegionGate_ListOfPlayersInZone(On.RegionGate.orig_ListOfPlayersInZone orig, RegionGate self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            var slugs = new List<Player>();
            foreach (var avi in menagerie.abstractAvatars)
            {
                if (avi.realizedCreature != null && avi.realizedCreature is Player slug)
                {
                    int det = self.DetectZone(avi);
                    if (det > 0 && det < 3)
                    {
                        slugs.Add(slug);
                    }
                }
            }
            return slugs;
        }
        return orig(self);
    }

    public static bool On_RegionGate_get_MeetRequirement(Func<RegionGate, bool> orig, RegionGate self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && self.room.game.session is StoryGameSession story)
        {
            var firstAlivePlayer = menagerie.firstAliveAvatar;
            if (menagerie.avatars.Count == 0 || firstAlivePlayer == null)
            {
                return false;
            }
            var crit = firstAlivePlayer.realizedCreature;
            if (story != null && story.saveState.deathPersistentSaveData.maximumRippleLevel >= 1f)
            {
                return false;
            }
            var karma = story.saveState.deathPersistentSaveData.karma;
            if (ModManager.MSC && story.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && crit.grasps.Length != 0)
            {
                foreach (var grasp in crit.grasps)
                {
                    if (grasp != null && grasp.grabbedChunk != null && grasp.grabbedChunk.owner is Scavenger)
                    {
                        karma += (grasp.grabbedChunk.owner as Scavenger).abstractCreature.karmicPotential;
                        break;
                    }
                }
            }
            bool unlocked = ModManager.MSC && self.karmaRequirements[(!self.letThroughDir) ? 1 : 0] == MoreSlugcatsEnums.GateRequirement.RoboLock && story.saveState.hasRobo && story.saveState.deathPersistentSaveData.theMark && self.room.world.region.name != "SL" && self.room.world.region.name != "MS" && self.room.world.region.name != "DM";
            int num2 = -1;
            bool enough = false;
            if (int.TryParse(self.karmaRequirements[(!self.letThroughDir) ? 1 : 0].value, NumberStyles.Any, CultureInfo.InvariantCulture, out num2))
            {
                enough = (num2 - 1 <= karma);
            }
            var results = (unlocked || enough) || self.unlocked;
            if (results) StoryRPCs.RegionGateOrWarpMeetRequirement();
            return results;
        }
        return orig(self);
    }

    // mostly copy pasted from meadow's creature pipe sprites, but seems like it was already copy pasted to begin with
    public static void On_ShortcutGraphics_GenerateSprites(On.ShortcutGraphics.orig_GenerateSprites orig, ShortcutGraphics self)
    {
        orig(self);
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode)
        {
            for (int i = 0; i < self.room.shortcuts.Length; i++)
            {
                if (self.room.shortcuts[i].shortCutType == ShortcutData.Type.NPCTransportation)
                {
                    self.entranceSprites[i, 0]?.RemoveFromContainer(); // remove safari one
                    self.entranceSprites[i, 0] = new FSprite("Pebble10", true);
                    self.entranceSprites[i, 0].rotation = RWCustom.Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), -RWCustom.IntVector2.ToVector2(self.room.ShorcutEntranceHoleDirection(self.room.shortcuts[i].StartTile)));
                    self.entranceSpriteLocations[i] = self.room.MiddleOfTile(self.room.shortcuts[i].StartTile) + RWCustom.IntVector2.ToVector2(self.room.ShorcutEntranceHoleDirection(self.room.shortcuts[i].StartTile)) * 15f;
                    if ((ModManager.MMF && MoreSlugcats.MMF.cfgShowUnderwaterShortcuts.Value) || (self.room.water && self.room.waterInFrontOfTerrain && self.room.PointSubmerged(self.entranceSpriteLocations[i] + new Vector2(0f, 5f))))
                    {
                        self.camera.ReturnFContainer((ModManager.MMF && MoreSlugcats.MMF.cfgShowUnderwaterShortcuts.Value) ? "GrabShaders" : "Items").AddChild(self.entranceSprites[i, 0]);
                    }
                    else
                    {
                        self.camera.ReturnFContainer("Shortcuts").AddChild(self.entranceSprites[i, 0]);
                        self.camera.ReturnFContainer("Water").AddChild(self.entranceSprites[i, 1]);
                    }
                }
                else if (self.room.shortcuts[i].shortCutType == ShortcutData.Type.CreatureHole)
                {
                    self.entranceSprites[i, 0]?.RemoveFromContainer();
                    self.entranceSprites[i, 0] = new FSprite("BigGlyph11", true);
                    self.entranceSprites[i, 0].rotation = RWCustom.Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), -RWCustom.IntVector2.ToVector2(self.room.ShorcutEntranceHoleDirection(self.room.shortcuts[i].StartTile)));
                    self.entranceSpriteLocations[i] = self.room.MiddleOfTile(self.room.shortcuts[i].StartTile) + RWCustom.IntVector2.ToVector2(self.room.ShorcutEntranceHoleDirection(self.room.shortcuts[i].StartTile)) * 15f;
                    if ((ModManager.MMF && MoreSlugcats.MMF.cfgShowUnderwaterShortcuts.Value) || (self.room.water && self.room.waterInFrontOfTerrain && self.room.PointSubmerged(self.entranceSpriteLocations[i] + new Vector2(0f, 5f))))
                    {
                        self.camera.ReturnFContainer((ModManager.MMF && MoreSlugcats.MMF.cfgShowUnderwaterShortcuts.Value) ? "GrabShaders" : "Items").AddChild(self.entranceSprites[i, 0]);
                    }
                    else
                    {
                        self.camera.ReturnFContainer("Shortcuts").AddChild(self.entranceSprites[i, 0]);
                        self.camera.ReturnFContainer("Water").AddChild(self.entranceSprites[i, 1]);
                    }
                }
            }
        }
    }

    public static void On_ShortcutGraphics_Draw(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
    {
        orig(self, timeStacker, camPos);
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode)
        {
            if (ModManager.MSC)
            {
                for (int i = 0; i < self.entranceSprites.GetLength(0); i++)
                {
                    if (self.entranceSprites[i, 0] != null && self.room.shortcuts != null && self.room.shortcuts.Length > i && self.room.shortcuts[i].shortCutType == ShortcutData.Type.CreatureHole)
                    {
                        self.entranceSprites[i, 0].isVisible = true;
                    }
                }
            }
        }
    }

    public static void On_RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        orig(self, manager);
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            menagerie.foodPoints = self.GetStorySession.saveState.food;
        }
    }

    public static AbstractCreature FirstAvatar(AbstractCreature orig)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            // prioritize alive, as base game does
            var avatars = menagerie.abstractAvatars.Where(avi => avi.state.alive).ToArray();
            if (avatars.Length > 0) return avatars[0];
            return menagerie.abstractAvatars[0];
        }
        return orig;
    }

    public static void IL_RainWorldGame_Win(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCall<RainWorldGame>("get_FirstAlivePlayer")
            );
            c.MoveAfterLabels();
            c.EmitDelegate(FirstAvatar);

            // this second part should never run, since it's only run if the above returns null, but it's here anyways juuust in case
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCall<RainWorldGame>("get_FirstAnyPlayer")
            );
            c.MoveAfterLabels();
            c.EmitDelegate(FirstAvatar);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }

    public static void On_SeedCob_Update(On.SeedCob.orig_Update orig, SeedCob self, bool eu)
    {
        orig(self, eu);
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && self.room.game.session is StoryGameSession story && !self.AbstractCob.dead && self.open > 0.8f && !self.AbstractCob.rotted)
        {
            foreach (var acrit in self.room.abstractRoom.creatures)
            {
                if (acrit.realizedCreature != null && CreatureController.creatureControllers.TryGetValue((Creature)acrit.realizedCreature, out var cc) && cc is Creatures.ScavengerController scavy)
                {
                    var crit = acrit.realizedCreature;
                    if ((acrit.rippleLayer == self.abstractPhysicalObject.rippleLayer || acrit.rippleBothSides || self.abstractPhysicalObject.rippleBothSides) && crit.room == self.room && menagerie.foodPoints < story.characterStats.maxFood && scavy.handOnExternalFoodSource == null && scavy.dontEatExternalFoodSourceCounter < 1 && scavy.eatExternalFoodSourceCounter < 1 && (scavy.touchedNoInputCounter > 5 || scavy.input[0].pckp) && crit.grasps[0] == null)
                    {
                        var pos = crit.mainBodyChunk.pos;
                        var vec = Custom.ClosestPointOnLineSegment(self.bodyChunks[0].pos, self.bodyChunks[1].pos, pos);
                        if (!Custom.DistLess(pos, vec, 25f))
                        {
                            continue;
                        }
                        scavy.handOnExternalFoodSource = vec + Custom.DirVec(pos, vec) * 5f;
                        scavy.eatExternalFoodSourceCounter = 15;
                        //story.playerSessionRecords[0].AddEat(self);
                        self.delayedPush = new Vector2?(Custom.DirVec(pos, vec) * 1.2f);
                        self.pushDelay = 4;
                        if (crit.graphicsModule != null && crit.graphicsModule is ScavengerGraphics graphics)
                        {
                            graphics.lookPoint = vec;
                        }
                    }
                }
            }
        }
    }

    public static void IL_RegionState_AdaptRegionStateToWorld(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip = c.DefineLabel();
            c.GotoNext(
                x => x.MatchLdsfld<AbstractPhysicalObject.AbstractObjectType>(nameof(AbstractPhysicalObject.AbstractObjectType.KarmaFlower)),
                x => x.MatchCallOrCallvirt<AbstractPhysicalObject.AbstractObjectType>("op_Inequality"),
                x => x.MatchBrfalse(out skip)
            );
            c.Emit(OpCodes.Ldloc, 4);
            c.Emit(OpCodes.Ldloc, 5);
            c.EmitDelegate((AbstractRoom room, int i) => OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie && room.entities[i] is AbstractCreature acrit && menagerie.abstractAvatars.Contains(acrit));
            c.Emit(OpCodes.Brtrue, skip);
        }
        catch (Exception ex)
        {
            StoryMenagerie.LogError(ex);
        }
    }
}