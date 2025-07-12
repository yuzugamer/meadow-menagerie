using System;
using System.Security;
using System.Security.Permissions;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using RainMeadow;
using MonoMod.RuntimeDetour;
using Menu;
using System.Linq;
using System.Reflection;
using StoryMenagerie;
using System.Runtime.CompilerServices;
using Watcher;
using MoreSlugcats;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: IgnoresAccessChecksTo("RainMeadow")]

namespace StoryMenagerie;

[BepInPlugin("yuzugamer.storymenagerie", "Story Menagerie", "0.5.0")]

public class StoryMenagerie : BaseUnityPlugin
{
    public static MeadowGameMode.OnlineGameModeType MenagerieGameMode;
    public static ProcessManager.ProcessID MenagerieMenu = new("MenagerieMenu", true);
    public static Dictionary<CreatureTemplate.Type, List<Type>> edibleFood = new();
    public static Dictionary<Type, Func<PhysicalObject, Creature.Grasp, int>> biteActions = new();
    public static readonly Dictionary<CreatureTemplate.Type, CreatureTemplate.Type> playableCreatures = new Dictionary<CreatureTemplate.Type, CreatureTemplate.Type>{
        { CreatureTemplate.Type.PinkLizard, CreatureTemplate.Type.LizardTemplate },
        { CreatureTemplate.Type.BlueLizard, CreatureTemplate.Type.LizardTemplate },
        { CreatureTemplate.Type.YellowLizard, CreatureTemplate.Type.LizardTemplate },
        { CreatureTemplate.Type.WhiteLizard, CreatureTemplate.Type.LizardTemplate },
        { CreatureTemplate.Type.RedLizard, CreatureTemplate.Type.LizardTemplate },
        { CreatureTemplate.Type.BlackLizard, CreatureTemplate.Type.LizardTemplate },
        { CreatureTemplate.Type.Salamander, CreatureTemplate.Type.LizardTemplate },
        { CreatureTemplate.Type.CyanLizard, CreatureTemplate.Type.LizardTemplate },
        { CreatureTemplate.Type.GreenLizard, CreatureTemplate.Type.LizardTemplate },
        { CreatureTemplate.Type.Scavenger, CreatureTemplate.Type.Scavenger},
        { CreatureTemplate.Type.Slugcat, null },
        { CreatureTemplate.Type.BigNeedleWorm, CreatureTemplate.Type.BigNeedleWorm },
        { CreatureTemplate.Type.SmallNeedleWorm, CreatureTemplate.Type.BigNeedleWorm },
        { CreatureTemplate.Type.EggBug, CreatureTemplate.Type.EggBug },
        { CreatureTemplate.Type.LanternMouse, CreatureTemplate.Type.LanternMouse },
        { CreatureTemplate.Type.CicadaA, CreatureTemplate.Type.CicadaA },
        { CreatureTemplate.Type.CicadaB, CreatureTemplate.Type.CicadaA },
        //{ CreatureTemplate.Type.Centipede, CreatureTemplate.Type.Centipede },
        { CreatureTemplate.Type.JetFish, CreatureTemplate.Type.JetFish },
        { CreatureTemplate.Type.DaddyLongLegs, CreatureTemplate.Type.DaddyLongLegs },
        { CreatureTemplate.Type.BrotherLongLegs, CreatureTemplate.Type.DaddyLongLegs }
    };

    public static AbstractCreature.Personality GamerPersonality = new AbstractCreature.Personality { sympathy = 0f, energy = 1f, bravery = 1f, nervous = 0f, aggression = 1f, dominance = 1f};
    private static bool init;
    private static StoryMenagerie instance;

    private void OnEnable()
    {
        instance = this;
        On.RainWorld.OnModsInit += On_RainWorld_OnModsInit;
        On.RainWorld.PostModsInit += On_RainWorld_PostModsInit;
        On.RainWorld.OnModsDisabled += On_RainWorld_OnModsDisabled;
    }

    public static void LogError(object error)
    {
        instance.Logger.LogError(error);
        UnityEngine.Debug.LogError("[MENAGERIE ERROR] " + error);
    }

    public static void Debug(object info)
    {
        //instance.Logger.LogInfo(info);
        //instance.Logger.LogDebug(info);
        UnityEngine.Debug.Log("[MENAGERIE DEBUG] " + info);
    }
	
	// mainly here to be easily hooked in case anyone wants to add compatibility for other gamemodes
	public static bool IsMenagerie => OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode;

    /*public static void RegisterPlayableCreature(CreatureTemplate.Type creature, string group)
    {
        RegisterPlayableCreature(creature, new CreatureTemplate.Type(group, false));
    }*/

    public static void RegisterPlayableCreature(string creatureType, CreatureTemplate.Type group)
    {
        RegisterPlayableCreature(new CreatureTemplate.Type(creatureType, false), group);
    }

    public static void RegisterPlayableCreature(CreatureTemplate.Type creature, CreatureTemplate.Type group)
    {
        if(!playableCreatures.ContainsKey(creature))
        {
            /*if (ancestor == null) {
                var template = StaticWorld.GetCreatureTemplate(creature.value);
                if (template != null)
                {
                    ancestor = template.TopAncestor().type;
                }
            }*/
            playableCreatures.Add(creature, group);
        }
    }

    private void On_RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        if (init) return;
        {
            init = true;
            /*unlocked = ModManager.ActiveMods.Any(mod => mod.id == "pushtomeow" || mod.id == "pushtomeow_vultumast");
            if(unlocked)
            {
                Unlock();
            }*/
            try
            {
                GameHooks.Apply();
                MeadowHooks.Apply();
                CreatureControllerHooks.Apply();
                CreatureHooks.Apply();
                PlayerHooks.Apply();
                MenuHooks.Apply();
                HudHooks.Apply();
                StoryHooks.Apply();
                BugHooks.Apply();
            } catch (Exception ex)
            {
                StoryMenagerie.LogError("Failed to load!");
                StoryMenagerie.LogError(ex);
                return;
            }
            MenagerieGameMode = new("Story Menagerie", true);
            OnlineGameMode.RegisterType(MenagerieGameMode, typeof(MenagerieGameMode), "Story with extra cast variety");
            if (ModManager.DLCShared)
            {
                RegisterPlayableCreature(DLCSharedEnums.CreatureTemplateType.SpitLizard, CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature(DLCSharedEnums.CreatureTemplateType.EelLizard, CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature(DLCSharedEnums.CreatureTemplateType.ZoopLizard, CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature(DLCSharedEnums.CreatureTemplateType.ScavengerElite, CreatureTemplate.Type.Scavenger);
                RegisterPlayableCreature(DLCSharedEnums.CreatureTemplateType.Yeek, DLCSharedEnums.CreatureTemplateType.Yeek);
                RegisterPlayableCreature(DLCSharedEnums.CreatureTemplateType.TerrorLongLegs, CreatureTemplate.Type.DaddyLongLegs);
            }
            if (ModManager.MSC)
            {
                RegisterPlayableCreature(MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.TrainLizard, CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature(MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC, null);
                RegisterPlayableCreature(MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing, CreatureTemplate.Type.Scavenger);
                RegisterPlayableCreature(MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.FireBug, CreatureTemplate.Type.EggBug);
            }
            if (ModManager.Watcher)
            {
                RegisterPlayableCreature(WatcherEnums.CreatureTemplateType.BlizzardLizard, CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature(WatcherEnums.CreatureTemplateType.BasiliskLizard, CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature(WatcherEnums.CreatureTemplateType.IndigoLizard, CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature(WatcherEnums.CreatureTemplateType.ScavengerTemplar, CreatureTemplate.Type.Scavenger);
            }
            if (ModManager.ActiveMods.Any(mod => mod.id == "fruitortreat"))
            {
                RegisterPlayableCreature("LimeLizard", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("RaspberryLizard", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("PumpkinLizard", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("BlueberryLizard", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("ChocoLizard", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("SugarLizard", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("BananaLizard", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("CottonCandyLizard", CreatureTemplate.Type.LizardTemplate);
            }
            if (ModManager.ActiveMods.Any(mod => mod.id == "yuzugamer.CARNAGE"))
            {
                RegisterPlayableCreature("HellLizard", CreatureTemplate.Type.LizardTemplate);
            }
            if (ModManager.ActiveMods.Any(mod => mod.id == "lb-fgf-m4r-ik.modpack"))
            {
                RegisterPlayableCreature("HunterSeeker", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("MoleSalamander", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("NoodleEater", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("Polliwog", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("SilverLizard", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("WaterSpitter", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("CommonEel", CreatureTemplate.Type.LizardTemplate);
                RegisterPlayableCreature("SurfaceSwimmer", CreatureTemplate.Type.EggBug);
            }
            if (ModManager.ActiveMods.Any(mod => mod.id == "myr.moss_fields"))
            {
                RegisterPlayableCreature("SnootShootNoot", CreatureTemplate.Type.BigNeedleWorm);
            }
        }
    }

    public static void RegisterEdible(CreatureTemplate.Type crit, Type edible)
    {
        if (!edibleFood.ContainsKey(crit))
        {
            edibleFood.Add(crit, new List<Type>() { edible });
        }
        else if (!edibleFood[crit].Contains(edible))
        {
            edibleFood[crit].Add(edible);
        }
    }

    public static void RegisterBiteAction(Type type, Func<PhysicalObject, Creature.Grasp, int> func)
    {
        if (!biteActions.ContainsKey(type))
        {
            biteActions.Add(type, func);
        }
        else
        {
            biteActions[type] = func;
        }
    }

    // this needs to be reworked to allow external mods (neither this, nor the mod that adds the object) to mark for consumability and register bite actions, as well as to use apo type enums instead of class types
    // important that mods should not need to add this as a dependency in order to interface with it
    // not sure what the optimal approach for all of the above will be, given that attributes are out of the question, and i need a nice-looking way for methods to mark themselves to be discovered
    // bite action method has to have int return type in order to get its food points, but the method needs to be able to be in any class, and some kind of identifying information is needed in order to register it, but multiple methods would be unreasonable
    // registering for consumability should be easy; just have return type be a kvp with the enum's value, and a list of creature types that can eat it
    // but proper solution for bite action should be sorted before making changes
    public static void On_RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        orig(self);
        Enums.Register();
        edibleFood = new();
        var lampsterFood = new List<Type>()
        {
            typeof(DangleFruit)
        };
        var jetfishFood = new List<Type>()
        {
            typeof(DangleFruit),
            typeof(SwollenWaterNut)
        };
        var scavengerFood = new List<Type>()
        {
            typeof(DangleFruit),
            typeof(JellyFish)
        };
        if (ModManager.DLCShared)
        {
            lampsterFood.Add(typeof(GooieDuck));
            lampsterFood.Add(typeof(GlowWeed));
            jetfishFood.Add(typeof(GooieDuck));
            jetfishFood.Add(typeof(GlowWeed));
            jetfishFood.Add(typeof(LillyPuck));
            scavengerFood.Add(typeof(GooieDuck));
            scavengerFood.Add(typeof(DandelionPeach));
            scavengerFood.Add(typeof(GlowWeed));
        }
        edibleFood.Add(CreatureTemplate.Type.LanternMouse, lampsterFood);
        edibleFood.Add(CreatureTemplate.Type.JetFish, jetfishFood);
        edibleFood.Add(CreatureTemplate.Type.Scavenger, scavengerFood);
        if (ModManager.DLCShared)
        {
            var yeekFood = new List<Type>()
            {
                typeof(DangleFruit),
                typeof(GooieDuck),
                typeof(DandelionPeach),
                typeof(GlowWeed)
            };
            edibleFood.Add(DLCSharedEnums.CreatureTemplateType.Yeek, yeekFood);
        }
        var iEdibleList = new List<Type>();
        // thanks rain meadow, referenced their rpc setup
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly == Assembly.GetExecutingAssembly()) continue;
            try
            {
                foreach (var type in assembly.GetTypesSafely())
                {
                    if (type.GetInterface(nameof(IPlayerEdible)) != null)
                    {
                        iEdibleList.Add(type);
                    }
                }
            }
            catch (Exception ex)
            {
                StoryMenagerie.LogError(ex);
            }
        }
        if (iEdibleList.Count() > 0)
        {
            foreach (var type in iEdibleList)
            {
                var foundEdible = false;
                var foundBite = false;
                foreach (var method in type.GetMethods())
                {
                    if (!foundEdible && method.Name.Contains("MenagerieEdible"))
                    {
                        if (method.ReturnType != null && method.ReturnType.Equals(typeof(CreatureTemplate.Type[])))
                        {
                            if (method.GetParameters().Length == 0)
                            {
                                if (method.IsStatic)
                                {
                                    try
                                    {
                                        var critTemplates = (CreatureTemplate.Type[])method.Invoke(null, null);
                                        var log = "";
                                        foreach (var crit in critTemplates)
                                        {
                                            RegisterEdible(crit, type);
                                            log += " " + crit.value;
                                        }
                                        StoryMenagerie.Debug(type.FullName + " registered to be edible for:" + log);
                                        foundEdible = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        StoryMenagerie.LogError(method.Name + " is marked to edible, but failed to be invoked!");
                                        StoryMenagerie.LogError(ex);
                                    }
                                }
                                else
                                {
                                    StoryMenagerie.LogError(method.Name + " is marked to be edible, but is not static!");
                                }
                            }
                            else
                            {
                                StoryMenagerie.LogError(method.Name + " is marked to be edible, but contains parameters!");
                            }
                        }
                        else
                        {
                            StoryMenagerie.LogError(method.Name + " is marked to be edible, but has the wrong return type!");
                        }
                    }
                    if (!foundBite && method.Name.Contains("MenagerieBite"))
                    {
                        if (method.ReturnType != null && method.ReturnType == typeof(int))
                        {
                            if (method.GetParameters().Length == 2)
                            {
                                if (method.GetParameters()[0].ParameterType == typeof(PhysicalObject) && method.GetParameters()[1].ParameterType == typeof(Creature.Grasp))
                                {
                                    if (method.IsStatic)
                                    {
                                        try
                                        {
                                            var action = (Func<PhysicalObject, Creature.Grasp, int>)method.CreateDelegate(typeof(Func<PhysicalObject, Creature.Grasp, int>));
                                            RegisterBiteAction(type, action);
                                            StoryMenagerie.Debug(type.FullName + "." + method.Name + " registered to be run on bite");
                                            foundBite = true;
                                        }
                                        catch (Exception ex)
                                        {
                                            StoryMenagerie.LogError(method.Name + " is marked to run on bite, but failed to be turned into a delegate!");
                                            StoryMenagerie.LogError(ex);
                                        }
                                    }
                                    else
                                    {
                                        StoryMenagerie.LogError(method.Name + " is marked to be run on bite, but is not static!");
                                    }
                                }
                                else
                                {
                                    StoryMenagerie.LogError(method.Name + " is marked to be run on bite, but parameter types do not match!");
                                }
                            }
                            else
                            {
                                StoryMenagerie.LogError(method.Name + " is marked to be run on bite, but parameter count is not 2!");
                            }
                        }
                        else
                        {
                            StoryMenagerie.LogError(method.Name + " is marked to run on bite, but return type is not int!");
                        }
                    }
                }
                if (!foundEdible && type.GetProperties() != null)
                {
                    foreach (var property in type.GetProperties())
                    {
                        if (property.Name.Contains("MenagerieEdible"))
                        {
                            try
                            {
                                if (property.GetMethod != null)
                                {
                                    var method = property.GetMethod;
                                    if (method.ReturnType != null && method.ReturnType.Equals(typeof(CreatureTemplate.Type[])))
                                    {
                                        if (method.IsStatic)
                                        {
                                            try
                                            {
                                                var critTemplates = (CreatureTemplate.Type[])method.Invoke(null, null);
                                                var log = "";
                                                foreach (var crit in critTemplates)
                                                {
                                                    RegisterEdible(crit, type);
                                                    log += " " + crit.value;
                                                }
                                                StoryMenagerie.Debug(type.FullName + " registered to be edible for:" + log);
                                                break;
                                            }
                                            catch (Exception ex)
                                            {
                                                StoryMenagerie.LogError(method.Name + " is marked to edible, but failed to be invoked!");
                                                StoryMenagerie.LogError(ex);
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            StoryMenagerie.LogError(method.Name + " is marked to be edible, but is not static!");
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        StoryMenagerie.LogError(method.Name + " is marked to be edible, but has the wrong return type!");
                                        break;
                                    }
                                }
                                else
                                {
                                    StoryMenagerie.LogError(property.Name + " is marked to be edible, but does not have a get method!");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                StoryMenagerie.LogError(property.Name + " is marked to be edible, but does not have a get method!");
                                StoryMenagerie.LogError(ex);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    public static void On_RainWorld_OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld self, ModManager.Mod[] newlyDisabledMods)
    {
        orig(self, newlyDisabledMods);
        Enums.Unregister();
    }
}