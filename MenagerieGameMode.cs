using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;
using RainMeadow;
using UnityEngine;

namespace StoryMenagerie;

public class MenagerieGameMode : StoryGameMode
{
    public CreatureTemplate.Type? requiredHostCreature;
    public CreatureTemplate.Type selectedCreature;
    public int critID = 0;
    public int showKarmaFoodRainTime;
    public int foodPoints = 0;
    public int quarterFoodPoints = 0;
    public int mushroomCounter;
    //public List<AbstractCreature> avatars = new List<AbstractCreature>();
    public int skinIndex;
    public bool secretMode;
    public static Action<Creature, OnlineCreature, SlugcatCustomization> AvatarBinds = DefaultAvatarBinds;
    public List<AbstractCreature> abstractAvatars
    {
        get
        {
            var results = new List<AbstractCreature>();
            foreach (var kvp in lobby.playerAvatars)
            {
                var entity = kvp.Value.FindEntity(true);
                if (entity is OnlineCreature oc)
                {
                    results.Add(oc.abstractCreature);
                }
            }
            return results;
        }
    }
    public List<Creature> realizedAvatars
    {
        get
        {
            var results = new List<Creature>();
            foreach (var kvp in lobby.playerAvatars)
            {
                var entity = kvp.Value.FindEntity(true);
                if (entity is OnlineCreature oc && oc.realized)
                {
                    results.Add(oc.realizedCreature);
                }
            }
            return results;
        }
    }
    public List<OnlineCreature> onlineAvatars
    {
        get
        {
            var results = new List<OnlineCreature>();
            foreach (var kvp in lobby.playerAvatars)
            {
                var entity = kvp.Value.FindEntity(true);
                if (entity != null && entity is OnlineCreature oc)
                {
                    results.Add(oc);
                }
            }
            return results;
        }
    }
    public AbstractCreature[] localAvatars
    {
        get
        {
            return clientSettings.avatars.Select(id => (id.FindEntity(true) as OnlineCreature).abstractCreature).ToArray();
        }
    }
    public AbstractCreature firstAliveAvatar
    {
        get
        {
            foreach (var kvp in lobby.playerAvatars)
            {
                var entity = kvp.Value.FindEntity(true);
                if (entity is OnlineCreature oc)
                {
                    if (oc.realized && oc.realizedCreature != null && !oc.realizedCreature.dead)
                    {
                        return oc.abstractCreature;
                    }
                }
            }
            return null;
            //return abstractAvatars.First(avi => avi.realizedCreature != null && !avi.realizedCreature.dead);
        }
    }

    public MenagerieGameMode(Lobby lobby) : base(lobby)
    {
        critID = 0;
        requiredHostCreature = null;
        selectedCreature = CreatureTemplate.Type.Slugcat;
        if (lobby != null)
        {
            lobby.AddData(new MenagerieLobbyData());
        }
    }

    public override AbstractCreature SpawnAvatar(RainWorldGame game, WorldCoordinate location)
    {
        RainMeadow.RainMeadow.DebugMe();
        RainMeadow.RainMeadow.sSpawningAvatar = true;
        AbstractCreature abstractCreature;
        var id = new EntityID(-1, critID);
        // alt seed only works for non-negative numbers, due to "RandomSeed" - which is the primary way of getting the id number - only returning altSeed if it's above -1
        // might as well have it anyways though
        id.altSeed = critID;
        abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(selectedCreature), null, location, id);
        if (selectedCreature == CreatureTemplate.Type.Slugcat)
        {
            abstractCreature.state = new PlayerState(abstractCreature, 0, avatarSettings.playingAs, false);
            game.session.AddPlayer(abstractCreature);
        }
        else
        {
            game.GetStorySession.playerSessionRecords[0] = new PlayerSessionRecord(0);
            game.GetStorySession.playerSessionRecords[0].wokeUpInRegion = game.world.region.name;
            //StoryCreatureController.AddPlayer(game.session, abstractCreature);
            if (game.session.Players == null || game.session.Players.Count == 0)
            {
                RainMeadow.RainMeadow.sSpawningAvatar = false;
                var player = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, location, new EntityID(-1, 0));
                player.state = new PlayerState(player, 0, game.GetStorySession.saveStateNumber, false);
                game.session.AddPlayer(player);
                RainMeadow.RainMeadow.sSpawningAvatar = true;
            }
        }
        //avatars.Add(abstractCreature.GetOnlineCreature());
        game.world.GetAbstractRoom(abstractCreature.pos.room).AddEntity(abstractCreature);
        RainMeadow.RainMeadow.Debug("spawned avatar is " + abstractCreature);
        return abstractCreature;
    }

    public override void ConfigureAvatar(OnlineCreature onlineCreature)
    {
		var type = onlineCreature.abstractCreature.creatureTemplate.type;
        if (type != CreatureTemplate.Type.Slugcat && (!ModManager.MSC || type != MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC)) avatarSettings.wearingCape = false;
        base.ConfigureAvatar(onlineCreature);
    }

    public override void NewEntity(OnlineEntity oe, OnlineResource inResource)
    {
        RainMeadow.RainMeadow.Debug(oe);
        if (oe is OnlineCreature onlineCreature)
        {
            // shrug
            oe.AddData<MeadowCreatureData>(new MeadowCreatureData());
            //oe.AddData<StoryControllerData>(new StoryControllerData());

            //if (RainMeadow.RainMeadow.sSpawningAvatar)
            //{
            //    RainMeadow.RainMeadow.Debug("Registring avatar: " + onlineCreature);
            //    this.avatars.Add(onlineCreature);
            //    ConfigureAvatar(onlineCreature);
            //}
            if (RainMeadow.RainMeadow.sSpawningAvatar || oe.TryGetData<SlugcatCustomization>(out var _))
            {
                var prevSpawning = RainMeadow.RainMeadow.sSpawningAvatar;
                RainMeadow.RainMeadow.sSpawningAvatar = true;
                StoryMenagerie.Debug("Registering avatar: " + onlineCreature);
                this.avatars.Add(onlineCreature);
                ConfigureAvatar(onlineCreature);
                RainMeadow.RainMeadow.sSpawningAvatar = prevSpawning;
            }
        }
    }

    public override void LobbyTick(uint tick)
    {
        clientSettings.avatars = avatars.Select(a => a.id).ToList();
        //storyClientData.isDead = avatars.All(a => a.abstractCreature.state.dead || (a.realizedCreature != null && CreatureController.creatureControllers.TryGetValue(a.realizedCreature, out var crit) && crit.isStory(out var scc) && scc.dangerGraspTime >= 60) || (a.abstractCreature.state is PlayerState state && state.permaDead));
        var devTools = RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.devToolsActive;

        if (lobby.isOwner && lobby.clientSettings.Values.Where(cs => cs.inGame) is var inGameClients && inGameClients.Any())
        {
            var inGameClientsData = inGameClients.Select(cs => cs.GetData<StoryClientSettingsData>());
            var inGameAvatarOPOs = inGameClients.SelectMany(cs => cs.avatars.Select(id => id.FindEntity(true))).OfType<OnlinePhysicalObject>();

            if (!readyForWin && inGameClientsData.Any(scs => scs.readyForWin) && inGameClientsData.All(scs => scs.readyForWin || scs.isDead))
            {
                RainMeadow.RainMeadow.Debug("ready for win!");
                StoryMenagerie.Debug("ready for win!!! yippee!!!");
                readyForWin = true;
            }
            else
            {
                if (devTools) StoryMenagerie.Debug("not ready for win, " + inGameClientsData.Count() + " in game clients with data");
                foreach (var cs in inGameClients)
                {
                    var scs = cs.GetData<StoryClientSettingsData>();
                    StoryMenagerie.Debug($"{cs.owner} ready for win: {scs.readyForWin}");
                }
            }

            if (readyForTransition == ReadyForTransition.MeetRequirement)
            {
                gateRoom = null;
                if (inGameClientsData.All(scs => scs.readyForTransition))
                {
                    // make sure they're at the same region gate
                    var rooms = inGameAvatarOPOs.Select(opo => opo.apo.pos.room);
                    if (rooms.Distinct().Count() == 1)
                    {
                        RainWorld.roomIndexToName.TryGetValue(rooms.First(), out gateRoom);
                        RainMeadow.RainMeadow.Debug($"ready for gate {gateRoom}!");
                        readyForTransition = ReadyForTransition.Opening;
                    }
                }
            }
            else if (readyForTransition == ReadyForTransition.Crossed)
            {
                // wait for all players to pass through OR leave the gate room
                if (inGameClientsData.All(scs => !scs.readyForTransition)
                    || (gateRoom is not null && !inGameAvatarOPOs.Select(opo => opo.apo.Room?.name).Contains(gateRoom))  // HACK: AllPlayersThroughToOtherSide may not get called if warp, which softlocks gates
                    )
                {
                    RainMeadow.RainMeadow.Debug($"all through gate {gateRoom}!");
                    readyForTransition = ReadyForTransition.Closed;
                }
            }
        }
        if (showKarmaFoodRainTime > 0) showKarmaFoodRainTime--;
    }

    public override void Customize(Creature creature, OnlineCreature oc)
    {
        base.Customize(creature, oc);
        if (oc.TryGetData<SlugcatCustomization>(out var data))
        {
            DefaultAvatarBinds(creature, oc, data);
        }
    }

    public static void BindStoryAvatar(Creature creature, OnlineCreature oc, SlugcatCustomization customization)
    {
        if (AvatarBinds != null) AvatarBinds(creature, oc, customization);
    }

    public static void DefaultAvatarBinds(Creature creature, OnlineCreature oc, SlugcatCustomization customization)
    {
        if (creature is Lizard liz)
        {
            new StoryLizardController(liz, oc, 0, customization);
        }
        else if (creature is Scavenger scavy)
        {
            new StoryScavengerController(scavy, oc, 0, customization);
        }
        else if (creature is NeedleWorm)
        {
            new StoryNoodleController(creature, oc, 0, customization);
        }
        else if (creature is EggBug bug)
        {
            new StoryEggbugController(bug, oc, 0, customization);
        }
        else if (creature is LanternMouse mouse)
        {
            new StoryLanternMouseController(mouse, oc, 0, customization);
        }
        else if (creature is Cicada cicada)
        {
            new StoryCicadaController(cicada, oc, 0, customization);
        }
        else if (creature is Centipede centi)
        {
            new StoryCentipedeController(centi, oc, 0, customization);
        }
        else if (ModManager.DLCShared && creature is MoreSlugcats.Yeek yeek)
        {
            new StoryYeekController(yeek, oc, 0, customization);
        }
        else if (ModManager.MSC && creature is Player player && player.isNPC)
        {
            new StorySlugNPCController(player, oc, 0, customization);
        }
        else if (creature is JetFish fish)
        {
            new StoryJetFishController(fish, oc, 0, customization);
        }
        else if (creature is BigEel eel)
        {
            new StoryBigEelController(eel, oc, 0, customization);
        }
        else if (creature is DaddyLongLegs dll)
        {
            new LongLegsController(dll, oc, 0, customization);
        }
        else if (creature is not Player)
        {
            throw new InvalidProgrammerException("You need to implement " + creature.ToString());
        }
    }

    public override ProcessManager.ProcessID MenuProcessId()
    {
        return StoryMenagerie.MenagerieMenu;
    }

    public int FoodInRoom(bool eatAndDestroy)
    {
        var avatar = avatars[0];
        if (avatar.realizedCreature != null)
        {
            if (avatar.realizedCreature is Player scug)
            {
                return scug.FoodInRoom(scug.room, eatAndDestroy);
            }
            else if (CreatureController.creatureControllers.TryGetValue(avatar.realizedCreature, out var crit))
            {
                return crit.FoodInRoom(crit.creature.room, eatAndDestroy);
            }
        }
        return 0;
    }

    [RPCMethod]
    public static void ChangeFood(short amt)
    {
        if(OnlineManager.lobby.gameMode is not MenagerieGameMode menagerie) throw new InvalidProgrammerException("lobby gamemode is not menagerie");
        if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game)
        {
            StoryMenagerie.Debug("ChangeFood RPC sent, but client is not in-game");
            return;
        }
        var newFood = Math.Max(0, Math.Min(menagerie.foodPoints * 4 + menagerie.quarterFoodPoints + amt, game.session.characterStats.maxFood * 4));
        menagerie.foodPoints = newFood / 4;
        menagerie.quarterFoodPoints = newFood % 4;
    }
    [RPCMethod]
    public static void AddMushroomCounter()
    {
        if (OnlineManager.lobby.gameMode is not MenagerieGameMode menagerie) throw new InvalidProgrammerException("lobby gamemode is not menagerie");
        menagerie.mushroomCounter += 320;
    }

    /*[RPCMethod]
    public static void ForceSyncCustomization(RPCEvent rpc, OnlineEntity avatar, Color[] colors, bool[] enabled)
    {
        if (avatar.owner == rpc.from && avatar is OnlineCreature oc && oc.realizedCreature != null && CreatureController.creatureControllers.TryGetValue(oc.realizedCreature, out var cc) && cc.isStory(out var scc))
        {
            if (oc.TryGetData<SlugcatCustomization>(out var customization))
            {
                customization.currentColors = colors.ToList();
            }
        }
    }*/
}
