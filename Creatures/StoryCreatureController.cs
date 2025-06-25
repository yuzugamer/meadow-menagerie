using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using static RewiredConsts.Layout;
using DevInterface;
using MoreSlugcats;
using Watcher;

namespace StoryMenagerie;

//public interface IStoryCreatureController
//{
    //public Creature GetCreature { get; }
    //public StoryControllerData scd { get; set; }
    //public SlugcatCustomization storyCustomization { get; set; }
    //public int forceSleepCounter { get; set; }
    //public int foodInStomach { get; set; }
    //public abstract bool GrabImpl(PhysicalObject pickUpCandidate);
//}

public static class StoryCreatureController
{
    public static ConditionalWeakTable<CreatureController, StoryCreatureControllerValues> storyControllers = new ConditionalWeakTable<CreatureController, StoryCreatureControllerValues>();
    public static bool isStory(this CreatureController self, out StoryCreatureControllerValues story) => storyControllers.TryGetValue(self, out story);
    public static StoryCreatureControllerValues story(this CreatureController self) => storyControllers.GetOrCreateValue(self);
    public static int foodToHibernate(this CreatureController self) => (self.creature.room.game.session is StoryGameSession session) ? SlugcatStats.SlugcatFoodMeter(session.saveState.saveStateNumber).x : 0;
    public static int maxFood(this CreatureController self) => (self.creature.room.game.session is StoryGameSession session) ? SlugcatStats.SlugcatFoodMeter(session.saveState.saveStateNumber).y : 0;
    //public static bool GrabImpl(this CreatureController self, PhysicalObject pickUpCandidate) => (self as IStoryCreatureController).GrabImpl(pickUpCandidate);

    /*public static void GrabUpdate(this CreatureController self)
    {
        var room = self.creature.room;
        var grasps = self.creature.grasps;
        bool holdingGrab = self.input[0].pckp;
        bool still = (self.inputDir == Vector2.zero && !self.input[0].thrw && !self.input[0].jmp && self.creature.Submersion < 0.5f);
        bool eating = false;
        bool swallow = false;

        // eat popcorn
        if (self.dontEatExternalFoodSourceCounter > 0)
        {
            self.dontEatExternalFoodSourceCounter--;
        }
        if (self.eatExternalFoodSourceCounter > 0)
        {
            self.eatExternalFoodSourceCounter--;
            if (self.eatExternalFoodSourceCounter < 1)
            {
                if (self.creature.grasps[0]?.grabbed is SeedCob) self.creature.grasps[0].Release();
                self.dontEatExternalFoodSourceCounter = 45;
                self.creature.room.PlaySound(SoundID.Slugcat_Bite_Fly, self.creature.mainBodyChunk);
            }
        }

        if (still)
        {
            Creature.Grasp edible = grasps.FirstOrDefault(g => g != null && g.grabbed is IPlayerEdible ipe && ipe.Edible);

            if (edible != null && (holdingGrab || self.eatCounter < 15))
            {
                eating = true;
                if (edible.grabbed is IPlayerEdible ipe)
                {
                    if (ipe.FoodPoints <= 0 || self.CanEat()) // can eat
                    {
                        if (eatCounter < 1)
                        {
                            eatCounter = 15;
                            BiteEdibleObject(edible, creature.evenUpdate);
                        }
                    }
                    else // no can eat
                    {
                        if (eatCounter < 20 && room.game.cameras[0].hud != null)
                        {
                            room.game.cameras[0].hud.foodMeter.RefuseFood();
                        }
                        edible = null;
                    }
                }
            }
        }

        if (eating && eatCounter > 0)
        {
            eatCounter--;
        }
        else if (!eating && eatCounter < 40)
        {
            eatCounter++;
        }

        // self was in vanilla might as well keep it
        foreach (var grasp in grasps) if (grasp != null && grasp.grabbed.slatedForDeletetion) creature.ReleaseGrasp(grasp.graspUsed);

        // pickup updage
        PhysicalObject physicalObject = (dontGrabStuff >= 1) ? null : PickupCandidate(8f);
        if (pickUpCandidate != physicalObject && physicalObject != null && physicalObject is PlayerCarryableItem)
        {
            (physicalObject as PlayerCarryableItem).Blink();
        }
        pickUpCandidate = physicalObject;

        if (wantToPickUp > 0) // pick up
        {
            var dropInstead = true; // grasps.Any(g => g != null);
            for (int i = 0; i < input.Length && i < 5; i++)
            {
                if (input[i].y > -1) dropInstead = false;
            }
            if (dropInstead)
            {
                for (int i = 0; i < grasps.Length; i++)
                {
                    if (grasps[i] != null)
                    {
                        wantToPickUp = 0;
                        room.PlaySound((!(grasps[i].grabbed is Creature)) ? SoundID.Slugcat_Lay_Down_Object : SoundID.Slugcat_Lay_Down_Creature, grasps[i].grabbedChunk, false, 1f, 1f);
                        room.socialEventRecognizer.CreaturePutItemOnGround(grasps[i].grabbed, creature);
                        if (grasps[i].grabbed is PlayerCarryableItem)
                        {
                            (grasps[i].grabbed as PlayerCarryableItem).Forbid();
                        }
                        creature.ReleaseGrasp(i);
                        break;
                    }
                }
            }
            else if (pickUpCandidate != null)
            {
                int freehands = 0;
                for (int i = 0; i < grasps.Length; i++)
                {
                    if (grasps[i] == null)
                    {
                        freehands++;
                    }
                }

                for (int i = 0; i < grasps.Length; i++)
                {
                    if (grasps[i] == null)
                    {
                        if (self.GrabImpl(self.pickUpCandidate))
                        {
                            self.pickUpCandidate = null;
                            self.wantToPickUp = 0;
                        }
                        break;
                    }
                }
            }
        }
    }*/

    public static int FoodInRoom(this CreatureController self, Room checkRoom, bool eatAndDestroy)
    {
        if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MenagerieGameMode menagerie) return 0;
        if (checkRoom.game.GetStorySession.saveStateNumber == SlugcatStats.Name.Red)
        {
            return menagerie.foodPoints;
        }
        if (self.creature.room == null)
        {
            return 0;
        }
        if (eatAndDestroy)
        {
            RWCustom.Custom.Log(new string[]
            {
            "Eat edibles in room"
            });
        }
        if (eatAndDestroy && checkRoom.game.session is StoryGameSession story && !story.saveState.deathPersistentSaveData.reinforcedKarma)
        {
            foreach (var entity in checkRoom.abstractRoom.entities)
            {
                if (entity is AbstractPhysicalObject apo && apo.realizedObject != null && apo.type == AbstractPhysicalObject.AbstractObjectType.KarmaFlower)
                {
                    story.saveState.deathPersistentSaveData.reinforcedKarma = true;
                    // gourm stuff
                    //if (this.SessionRecord != null)
                    //{
                    //    this.SessionRecord.AddEat((checkRoom.abstractRoom.entities[i] as AbstractPhysicalObject).realizedObject);
                    //    break;
                    //}
                    break;
                }
            }
        }

        return menagerie.foodPoints;
        /*
        if (this.FoodInStomach >= this.MaxFoodInStomach)
        {
            return this.FoodInStomach;
        }
        if (ModManager.MSC && this.slugcatStats.name == MoreSlugcatsEnums.SlugcatStatsName.Spear)
        {
            return this.FoodInStomach;
        }
        int num = this.FoodInStomach;
        int j = 0;
        for (int k = 0; k < 2; k++)
        {
            if (tardigrade.grasps[k] != null && tardigrade.grasps[k].grabbed is Fly)
            {
                PhysicalObject grabbed = tardigrade.grasps[k].grabbed;
                int num2 = SlugcatStats.NourishmentOfObjectEaten(this.SlugCatClass, grabbed as IPlayerEdible);
                if (num2 != -1)
                {
                    for (j += num2; j >= 4; j -= 4)
                    {
                        num++;
                    }
                    if (eatAndDestroy)
                    {
                        for (int l = 0; l < tardigrade.abstractCreature.stuckObjects.Count; l++)
                        {
                            if (tardigrade.abstractCreature.stuckObjects[l].A == tardigrade.abstractCreature && tardigrade.abstractCreature.stuckObjects[l].B == grabbed.abstractPhysicalObject)
                            {
                                tardigrade.abstractCreature.stuckObjects[l].Deactivate();
                                break;
                            }
                        }
                        if (this.SessionRecord != null)
                        {
                            this.SessionRecord.AddEat(grabbed);
                        }
                        grabbed.Destroy();
                        checkRoom.RemoveObject(grabbed);
                        checkRoom.abstractRoom.RemoveEntity(grabbed.abstractPhysicalObject);
                        this.ReleaseGrasp(k);
                    }
                }
                if (num >= this.MaxFoodInStomach)
                {
                    return num;
                }
            }
        }
        if (num >= this.slugcatStats.foodToHibernate)
        {
            return num;
        }
        if (ModManager.MMF && !eatAndDestroy && !MMF.cfgVanillaExploits.Value)
        {
            num = this.FoodInStomach;
            j = 0;
        }
        for (int m = checkRoom.abstractRoom.entities.Count - 1; m >= 0; m--)
        {
            if (checkRoom.abstractRoom.entities[m] is AbstractPhysicalObject && this.ObjectCountsAsFood((checkRoom.abstractRoom.entities[m] as AbstractPhysicalObject).realizedObject))
            {
                PhysicalObject realizedObject = (checkRoom.abstractRoom.entities[m] as AbstractPhysicalObject).realizedObject;
                int num3 = SlugcatStats.NourishmentOfObjectEaten(this.SlugCatClass, realizedObject as IPlayerEdible);
                if (num3 != -1)
                {
                    for (j += num3; j >= 4; j -= 4)
                    {
                        num++;
                    }
                    if (eatAndDestroy)
                    {
                        for (int n = 0; n < tardigrade.abstractCreature.stuckObjects.Count; n++)
                        {
                            if (tardigrade.abstractCreature.stuckObjects[n].A == tardigrade.abstractCreature && tardigrade.abstractCreature.stuckObjects[n].B == realizedObject.abstractPhysicalObject)
                            {
                                tardigrade.abstractCreature.stuckObjects[n].Deactivate();
                                break;
                            }
                        }
                        if (this.SessionRecord != null)
                        {
                            this.SessionRecord.AddEat(realizedObject);
                        }
                        realizedObject.Destroy();
                        checkRoom.RemoveObject(realizedObject);
                        checkRoom.abstractRoom.RemoveEntity(realizedObject.abstractPhysicalObject);
                    }
                }
                if (num >= this.slugcatStats.foodToHibernate)
                {
                    return num;
                }
            }
        }
        return num;*/
    }

    public static void AddFood(this CreatureController self, int food, float divisor = 1f)
    {
        if (!OnlinePhysicalObject.map.TryGetValue(self.creature.abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
        if (!onlineEntity.isMine) return;
        if (!self.isStory(out var scc)) return;
        if (food == 0) return;
        /*var state = self.creature.room.game.Players[0].state as PlayerState;
        var origFood = state.foodInStomach;
        var raw = food / divisor;
        var pips = (short)Mathf.Round(raw);
        var quarters = (byte)Mathf.RoundToInt((raw - pips) * 4);
        state.foodInStomach = state.foodInStomach + pips;
        //state.quarterFoodPoints = quarters;
        if (!OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
        {
            var newFood = state.foodInStomach;// * 4 + state.quarterFoodPoints;
            if (newFood != origFood) OnlineManager.lobby.owner.InvokeRPC(StoryRPCs.ChangeFood, (short)(newFood - origFood));
        }
        */if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MenagerieGameMode menagerie) return;
        var origFood = menagerie.foodPoints * 4 + menagerie.quarterFoodPoints;


        //var raw = (origFood / 4) + food / divisor;
        //var pips = (short)Mathf.Round(raw);
        //var quarters = (byte)Mathf.RoundToInt((raw - pips) * 4);

        //orig(self, add);
        //if (scc.foodInStomach < self.creature.room.game.GetStorySession.characterStats.maxFood) scc.foodInStomach++;
        //var newFood = pips * 4 + quarters;
        var newFood = origFood + (food * 4f);
        if (!OnlineManager.lobby.isOwner )
        {
            if (newFood != origFood) OnlineManager.lobby.owner.InvokeRPC(MenagerieGameMode.ChangeFood, (short)(newFood - origFood));
        } else
        {
            MenagerieGameMode.ChangeFood((short)(newFood - origFood));
        }

    }
    /// <summary>
    /// distance for slugcats is 40f, dist is from first body chunk
    /// </summary>
    public static PhysicalObject PickupCandidate(this CreatureController self, float favorSpears, float dist, bool includeLodged = false)
    {
        var crit = self.creature;
        PhysicalObject result = null;
        var compare = float.MaxValue;
        List<PhysicalObject> grabbed = [];
        if (crit.grasps != null)
        {
            foreach (var grasp in crit.grasps)
            {
                if (grasp != null && grasp.grabbed != null) grabbed.Add(grasp.grabbed);
            }
        }
        foreach (var layer in crit.room.physicalObjects)
        {
            foreach (var obj in layer)
            {
                if (obj != null && obj != crit && !grabbed.Contains(obj) && (obj.abstractPhysicalObject.rippleLayer == crit.abstractPhysicalObject.rippleLayer || obj.abstractPhysicalObject.rippleBothSides || crit.abstractPhysicalObject.rippleBothSides) && (obj is not Weapon weapon || weapon.mode != Weapon.Mode.Thrown) /*&& (obj is not PlayerCarryableItem carryable || carryable.forbiddenToPlayer < 1)*/ && Custom.DistLess(crit.bodyChunks[0].pos, obj.bodyChunks[0].pos, obj.bodyChunks[0].rad + dist))// && (Custom.DistLess(crit.bodyChunks[0].pos, obj.bodyChunks[0].pos, obj.bodyChunks[0].rad + (dist / 2f)) || crit.room.VisualContact(crit.bodyChunks[0].pos, obj.bodyChunks[0].pos)) /*&& crit.CanIPickThisUp(obj)*/)
                {
                    var tdist = Vector2.Distance(crit.bodyChunks[0].pos, obj.bodyChunks[0].pos);
                    if (obj is Spear spear)
                    {
                        if (!includeLodged && spear.abstractSpear.stuckInWall)
                        {
                            continue;
                        }
                        tdist -= favorSpears;
                    }
                    if (tdist < compare)
                    {
                        result = obj;
                        compare = tdist;
                    }
                }
            }
        }
        return result;
    }
    public static bool CanEat(this AbstractCreature self, PhysicalObject obj)
    {
        return obj is KarmaFlower || StoryMenagerie.edibleFood[self.creatureTemplate.type].Contains(obj.GetType()) || (ModManager.DLCShared && self.creatureTemplate.type == CreatureTemplate.Type.LanternMouse && obj.abstractPhysicalObject.type == DLCSharedEnums.AbstractObjectType.Seed);
    }

    public static int RunBiteAction(this Creature self, int grasp, PhysicalObject obj)
    {
        var type = obj.GetType();
        if (StoryMenagerie.biteActions.ContainsKey(type))
        {
            return StoryMenagerie.biteActions[type].Invoke(obj, self.grasps[grasp]);
        }
        return 0;
    }
    public static void FoodBitByPlayer(this CreatureController self, PhysicalObject grabbed, int grasp, bool eu)
    {
        if (grabbed is Creature)
        {
            (grabbed as Creature).SetKillTag(self.creature.abstractCreature);
        }
        // please don't judge me
        if (grabbed is Fly fly)
        {
            fly.bites--;
            if (!fly.dead)
            {
                fly.Die();
            }

            fly.room.PlaySound((fly.bites == 0) ? SoundID.Slugcat_Final_Bite_Fly : SoundID.Slugcat_Bite_Fly, fly.mainBodyChunk);
            fly.mainBodyChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (fly.bites < 1 && fly.eaten == 0)
            {
                self.AddFood(fly.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                fly.eaten = 3;
            }
        }
        else if (grabbed is Centipede centi)
        {
            centi.bites--;
            centi.Die();
            centi.room.PlaySound((centi.bites == 0) ? SoundID.Slugcat_Eat_Centipede : SoundID.Slugcat_Bite_Centipede, centi.mainBodyChunk);
            centi.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (centi.bites < 1)
            {
                self.AddFood(centi.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                centi.Destroy();
            }
        }
        else if (grabbed is Hazer hazer)
        {
            hazer.bites--;
            hazer.room.PlaySound(SoundID.Slugcat_Eat_Centipede, hazer.firstChunk);
            hazer.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (hazer.bites < 1)
            {
                self.AddFood(hazer.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                hazer.Destroy();
            }
        }
        else if (grabbed is SmallNeedleWorm worm)
        {
            worm.Scream();
            worm.Die();
            for (int i = 0; i < worm.bodyChunks.Length; i++)
            {
                worm.bodyChunks[i].MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            }
            worm.bites--;
            if (worm.bites < 1)
            {
                self.AddFood(worm.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                worm.Destroy();
            }
        }
        else if (grabbed is VultureGrub grub)
        {
            grub.bites--;
            grub.room.PlaySound(SoundID.Slugcat_Eat_Centipede, grub.firstChunk);
            grub.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (grub.bites < 1)
            {
                self.AddFood(grub.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                grub.Destroy();
            }
        }
        else if (grabbed is JellyFish jelly)
        {
            jelly.bites--;
            jelly.room.PlaySound((jelly.bites == 0) ? SoundID.Slugcat_Eat_Jelly_Fish : SoundID.Slugcat_Bite_Jelly_Fish, jelly.firstChunk);
            jelly.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (!jelly.AbstrConsumable.isConsumed)
            {
                jelly.AbstrConsumable.Consume();
            }
            for (int i = 0; i < jelly.tentacles.Length; i++)
            {
                for (int j = 0; j < jelly.tentacles[i].GetLength(0); j++)
                {
                    jelly.tentacles[i][j, 0] = Vector2.Lerp(jelly.tentacles[i][j, 0], jelly.firstChunk.pos, 0.2f);
                }
            }
            if (jelly.bites < 1)
            {
                self.AddFood(jelly.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                jelly.Destroy();
            }
        }
        else if (grabbed is SLOracleSwarmer neuron)
        {
            neuron.bites--;
            neuron.room.PlaySound((neuron.bites == 0) ? SoundID.Slugcat_Eat_Swarmer : SoundID.Slugcat_Bite_Swarmer, neuron.firstChunk);
            neuron.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (neuron.bites < 1)
            {
                self.AddFood(neuron.FoodPoints, 1f);
                if (neuron.room.game.session is StoryGameSession story)
                {
                    story.saveState.theGlow = true;
                }
                // prob not needed?
                // ((grasp.grabber as Player).State as PlayerNPCState).Glowing = true;
                // (grasp.grabber as Player).glowing = true;
                self.creature.grasps[grasp].Release();
                neuron.Destroy();
            }
        }
        else if (grabbed is OracleSwarmer swarmer)
        {
            swarmer.bites--;
            swarmer.room.PlaySound((swarmer.bites == 0) ? SoundID.Slugcat_Eat_Swarmer : SoundID.Slugcat_Bite_Swarmer, swarmer.firstChunk);
            swarmer.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (swarmer.bites < 1)
            {
                self.AddFood(swarmer.FoodPoints, 1f);
                if (swarmer.room.game.session is StoryGameSession story)
                {
                    story.saveState.theGlow = true;
                }
                self.creature.grasps[grasp].Release();
                swarmer.Destroy();
            }
        }
        else if (grabbed is DangleFruit fruit)
        {
            fruit.bites--;
            fruit.room.PlaySound((fruit.bites == 0) ? SoundID.Slugcat_Eat_Dangle_Fruit : SoundID.Slugcat_Bite_Dangle_Fruit, fruit.firstChunk);
            fruit.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (fruit.bites < 1)
            {
                self.AddFood(fruit.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                fruit.Destroy();
            }
        }
        else if (ModManager.DLCShared && grabbed is MoreSlugcats.GooieDuck duck)
        {
            if (duck.bites == 6)
            {
                duck.room.PlaySound(DLCSharedEnums.SharedSoundID.Duck_Pop, self.creature.mainBodyChunk, false, 1f, 0.5f + UnityEngine.Random.value * 0.5f);
                for (int i = 0; i < 3; i++)
                {
                    duck.room.AddObject(new WaterDrip(duck.firstChunk.pos, Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(4f, 21f, UnityEngine.Random.value), false));
                }
            }
            duck.bites--;
            duck.room.PlaySound((duck.bites != 0) ? SoundID.Slugcat_Bite_Dangle_Fruit : SoundID.Slugcat_Eat_Dangle_Fruit, duck.firstChunk);
            duck.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (duck.bites < 1)
            {
                self.AddFood(duck.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                duck.Destroy();
            }
        }
        else if (grabbed is SwollenWaterNut nut)
        {
            nut.bites--;
            nut.room.PlaySound((nut.bites == 0) ? SoundID.Slugcat_Eat_Water_Nut : SoundID.Slugcat_Bite_Water_Nut, nut.firstChunk);
            nut.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (nut.bites < 1)
            {
                self.AddFood(nut.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                nut.Destroy();
            }
            nut.propSpeed += Mathf.Lerp(-1f, 1f, UnityEngine.Random.value) * 7f;
            nut.bodyChunks[0].rad = Mathf.InverseLerp(3f, 0f, (float)nut.bites) * 9.5f;
        }
        else if (grabbed is SlimeMold mold)
        {
            mold.BitByCreature(eu);
        }
        else if (grabbed is EggBugEgg egg)
        {
            egg.bites--;
            egg.room.PlaySound((egg.bites == 0) ? SoundID.Slugcat_Eat_Dangle_Fruit : SoundID.Slugcat_Bite_Dangle_Fruit, egg.firstChunk);
            egg.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            egg.liquid = 1f;
            if (egg.bites < 1)
            {
                self.AddFood(egg.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                egg.Destroy();
            }
        }
        else if (grabbed is Mushroom)
        {
            // uuhhhhhhhh eventually
        }
        else if (grabbed is KarmaFlower flower)
        {
            flower.bites--;
            flower.room.PlaySound((flower.bites == 0) ? SoundID.Slugcat_Eat_Karma_Flower : SoundID.Slugcat_Bite_Karma_Flower, flower.firstChunk);
            flower.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (flower.bites < 1)
            {
                var game = self.creature.room.game;
                if (game.session is StoryGameSession story)
                {
                    story.saveState.deathPersistentSaveData.reinforcedKarma = true;
                    game.cameras[0].hud.karmaMeter.reinforceAnimation = 0;
                    if (!OnlineManager.lobby.isOwner)
                    {
                        OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.ReinforceKarma);
                    }
                    foreach (OnlinePlayer player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeOnceRPC(StoryRPCs.PlayReinforceKarmaAnimation);
                        }
                    }
                }
                self.creature.grasps[grasp].Release();
                flower.Destroy();
            }
        }
        else if (ModManager.DLCShared && grabbed is GlowWeed weed)
        {
            weed.bites--;
            weed.room.PlaySound((weed.bites != 0) ? SoundID.Slugcat_Bite_Dangle_Fruit : SoundID.Slugcat_Eat_Dangle_Fruit, weed.firstChunk);
            weed.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (weed.bites < 1)
            {
                self.AddFood(weed.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                weed.Destroy();
            }
        }
        else if (ModManager.DLCShared && grabbed is LillyPuck lilly)
        {
            lilly.AbstrLillyPuck.bites--;
            lilly.room.PlaySound((lilly.AbstrLillyPuck.bites != 0) ? SoundID.Slugcat_Bite_Dangle_Fruit : SoundID.Slugcat_Eat_Dangle_Fruit, lilly.firstChunk);
            lilly.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (lilly.AbstrLillyPuck.bites < 1)
            {
                self.AddFood(lilly.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                lilly.Destroy();
            }
        }
        else if (ModManager.DLCShared && grabbed is DandelionPeach peach)
        {
            peach.bites--;
            peach.room.PlaySound((peach.bites != 0) ? SoundID.Slugcat_Bite_Water_Nut : SoundID.Slugcat_Eat_Water_Nut, peach.firstChunk);
            peach.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (peach.bites < 1)
            {
                self.AddFood(peach.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                peach.Destroy();
            }
            peach.bodyChunks[0].rad = Mathf.InverseLerp(3f, 0f, (float)peach.bites) * 9.5f;
        }
        else if (ModManager.MSC && grabbed is FireEgg fegg)
        {
            fegg.bites--;
            fegg.room.PlaySound((fegg.bites != 0) ? SoundID.Slugcat_Bite_Dangle_Fruit : SoundID.Slugcat_Eat_Dangle_Fruit, fegg.firstChunk);
            fegg.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            fegg.liquid = 1f;
            if (fegg.bites < 1)
            {
                self.AddFood(fegg.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                fegg.Destroy();
            }
        }
        else if (ModManager.Watcher && grabbed is Barnacle crab)
        {
            crab.bites--;
            crab.Die();
            crab.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (crab.bites < 1)
            {
                self.AddFood(crab.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                crab.Destroy();
            }
        }
        else if (ModManager.Watcher && grabbed is Tardigrade bunny)
        {
            var hsl = Custom.RGB2HSL((bunny.BitesLeft == 3) ? bunny.iVars.secondaryColor : bunny.iVars.bodyColor);
            bunny.room.AddObject(new PoisonInjecter(self.creature, 0.22f, (10f + UnityEngine.Random.value * 8f) * ((bunny.BitesLeft == 3) ? 1f : 4.4f), new HSLColor(hsl.x, Mathf.Lerp(hsl.y, 1f, 0.5f), 0.5f).rgb));
            (bunny.State as Tardigrade.TardigradeState).bites--;
            bunny.room.PlaySound((bunny.BitesLeft == 0) ? SoundID.Slugcat_Eat_Slime_Mold : SoundID.Slugcat_Bite_Slime_Mold, bunny.firstChunk);
            bunny.firstChunk.MoveFromOutsideMyUpdate(eu, self.creature.mainBodyChunk.pos);
            if (bunny.BitesLeft <= 1 && !bunny.dead)
            {
                bunny.Die();
            }
            if (bunny.BitesLeft < 1)
            {
                self.AddFood(bunny.FoodPoints, 1f);
                self.creature.grasps[grasp].Release();
                bunny.Destroy();
            }
        }
        else
        {
            var food = self.creature.RunBiteAction(grasp, grabbed);
            self.AddFood(food, 1f);
        }
        // i've yet to get far enough into watcher to add the others don't blame me pls
    }
}

public class StoryCreatureControllerValues
{
    //public StoryControllerData scd;
    public SlugcatCustomization storyCustomization;
    public int forceSleepCounter;
    public bool stillInStartShelter;
    public PlayerState state;
    public Creature.Grasp dangerGrasp;
    public int dangerGraspTime;
    public WorldCoordinate? karmaFlowerGrowPos;
    public MarkSprite mark;
    public bool inVoidSea;
}