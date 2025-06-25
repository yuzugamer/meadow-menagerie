using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using RainMeadow;
using System.Diagnostics;
using UnityEngine;
using RWCustom;
using MonoMod.Cil;
using System.Diagnostics.Eventing.Reader;
using Mono.Cecil.Cil;

namespace StoryMenagerie;

public static class PlayerHooks
{
    public static void Apply()
    {
        new Hook(typeof(Player).GetMethod("get_FoodInStomach", BindingFlags.Instance | BindingFlags.Public), On_Player_get_FoodInStomach);
        //new Hook(typeof(Player).GetMethod("get_Adrenaline", BindingFlags.Instance | BindingFlags.Public), On_Player_get_Adrenaline);
    }

    public static int On_Player_get_FoodInStomach(Func<Player, int> orig, Player self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            return menagerie.foodPoints;
        }
        return orig(self);
    }

    public static int On_Player_get_Adrenaline(Func<Player, int> orig, Player self)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MenagerieGameMode menagerie)
        {
            return menagerie.mushroomCounter;
        }
        return orig(self);
    }
}