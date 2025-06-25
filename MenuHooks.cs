using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using RainMeadow;
using MonoMod.Cil;
using Menu;
using static RainMeadow.MeadowProgression;

namespace StoryMenagerie
{
    public static class MenuHooks
    {
        internal static FieldInfo reqCampaignSlug;
        internal static MethodInfo RemoveSlugcatList;
        internal static void Apply()
        {
            On.ProcessManager.PostSwitchMainProcess += On_ProcessManager_PostSwitchMainProcess;
            try
            {
                reqCampaignSlug = typeof(StoryOnlineMenu).GetField("reqCampaignSlug", BindingFlags.NonPublic | BindingFlags.Instance);
                RemoveSlugcatList = typeof(StoryOnlineMenu).GetMethod("RemoveSlugcatList", BindingFlags.NonPublic | BindingFlags.Instance);
                On.Menu.SlugcatSelectMenu.GetChecked += On_SlugcatSelectMenu_GetChecked;
                On.Menu.SlugcatSelectMenu.SetChecked += On_SlugcatSelectMenu_SetChecked;
                new ILHook(typeof(StoryOnlineMenu).GetMethod("SetupSlugcatList", BindingFlags.NonPublic | BindingFlags.Instance), IL_StoryOnlineMenu_SetupSlugcatList);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex);
            }
        }

        private static bool On_SlugcatSelectMenu_GetChecked(On.Menu.SlugcatSelectMenu.orig_GetChecked orig, SlugcatSelectMenu self, CheckBox box)
        {
            if(RainMeadow.RainMeadow.isStoryMode(out var story) && story is MenagerieGameMode menagerie && box.IDString == "HOSTCRITONLY")
            {
                return false;
                return menagerie.requiredHostCreature != null;
            }
            return orig(self, box);
        }

        private static void On_SlugcatSelectMenu_SetChecked(On.Menu.SlugcatSelectMenu.orig_SetChecked orig, SlugcatSelectMenu self, CheckBox box, bool c)
        {
            if (RainMeadow.RainMeadow.isStoryMode(out var story) && self is MenagerieOnlineMenu menagerie && box.IDString == "HOSTCRITONLY")
            {
                return;
                (story as MenagerieGameMode).requiredHostCreature = menagerie.PlayerSelectedCreature;
                return;
            }
            orig(self, box, c);
        }

        private static void On_ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if(ID == StoryMenagerie.MenagerieMenu)
            {
                self.currentMainLoop = new MenagerieOnlineMenu(self);
            }
            orig(self, ID);
        }

        private static bool ShouldSetupSlugcatList(StoryOnlineMenu self) => self is not MenagerieOnlineMenu menagerie || menagerie.currentCreature == CreatureTemplate.Type.Slugcat;

        private static float SlugcatListPos(float orig, StoryOnlineMenu self) => self is MenagerieOnlineMenu ? orig + 120f : orig;

        private static void IL_StoryOnlineMenu_SetupSlugcatList(ILContext il)
        {
            var c = new ILCursor(il);
            var skip = c.DefineLabel();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(ShouldSetupSlugcatList);
            c.Emit(OpCodes.Brtrue, skip);
            c.Emit(OpCodes.Ret);
            c.MarkLabel(skip);
            if (c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdcR4(394)
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(SlugcatListPos);
            }
            else UnityEngine.Debug.Log("IL hook failed first part");
        }
    }
}
