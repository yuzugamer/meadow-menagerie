/*using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IL.MoreSlugcats;
using Kittehface.Framework20;
using MonoMod.Utils;
using Newtonsoft.Json;
using RainMeadow;
using UnityEngine;

namespace StoryMenagerie;

public static class CustomizationManager
{
    public static Dictionary<string, KeyValuePair<bool, Color>[]> Customization;

    public static string FilePath
    {
        get
        {
            if (string.IsNullOrEmpty(OptionInterface.ConfigHolder.configDirPath))
            {
                OptionInterface.ConfigHolder.configDirPath = Path.Combine(Path.GetFullPath(UserData.GetPersistentDataPath()), "ModConfigs");
                DirectoryInfo directoryInfo = new DirectoryInfo(OptionInterface.ConfigHolder.configDirPath);
                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }
            }
            return Path.Combine(OptionInterface.ConfigHolder.configDirPath, "customization.json");
        }
    }

    public static void Load()
    {
        var path = FilePath;
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists)
        {
            Customization = JsonConvert.DeserializeObject<Dictionary<string, KeyValuePair<bool, Color>[]>>(File.ReadAllText(path));
        }
        else
        {
            Customization = new Dictionary<string, KeyValuePair<bool, Color>[]>();
        }
    }

    public static void Save()
    {
        if (Customization != null && Customization.Keys.Count > 0)
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(Customization));
        }
    }

    public static KeyValuePair<bool, Color>[] GetCustomization(string id)
    {
        if (Customization == null)
        {
            Load();
        }
        return Customization[id];
    }

    public static void SaveCustomization(CreatureCustomization customization)
    {
        if (customization.selectedCreature == null)
        {
            StoryMenagerie.LogError("tried to save customization, but the creature template type is null!");
            return;
        }
        if (customization.enabledColors == null || customization.currentColors == null)
        {
            StoryMenagerie.LogError("object reference not set to an instance of an object!");
        }
        if (customization.enabledColors.Length != customization.currentColors.Count)
        {
            StoryMenagerie.LogError("lengths of enabled colors array and current colors list do not match!");
            return;
        }
        if (!customization.enabledColors.Any(c => c))
        {
            return;
        }
        if (Customization == null)
        {
            Load();
        }
        var crit = customization.selectedCreature.value;
        List<KeyValuePair<bool, Color>> save = new();
        for (int i = 0; i < customization.currentColors.Count; i++)
        {
            save.Add(new KeyValuePair<bool, Color>(customization.enabledColors[i], customization.currentColors[i]));
        }
        if (Customization.ContainsKey(crit))
        {
            
            Customization[crit] = save.ToArray();
        }
        else
        {
            Customization.Add(crit, save.ToArray());
        }
        Save();
    }
 }*/