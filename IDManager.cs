using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IL.MoreSlugcats;
using Kittehface.Framework20;
using MonoMod.Utils;
using Newtonsoft.Json;

namespace StoryMenagerie;

public static class IDManager
{
    public static Dictionary<string, List<KeyValuePair<int, string>>> LoadedIDs;

    public static Dictionary<string, List<KeyValuePair<int, string>>> IDs
    {
        get
        {
            var ids = Presets;
            if (LoadedIDs != null)
            {
                ids.AddRange(LoadedIDs);
            }
            return ids;
        }
    }

    public static Dictionary<string, List<KeyValuePair<int, string>>> Presets
    {
        get
        {
            var presets = new Dictionary<string, List<KeyValuePair<int, string>>>();
            presets.Add("RedLizard", [
                new(0, "Default"),
                new(7943, "Noodle"),
                new(9374, "Nightmare"),
                new(2734, "Shiny"),
                new(6864, "Runt"),
                new(6860, "Lunatic")
                ]);

            presets.Add("YellowLizard", [
                new(0, "Default"),
                new (7805, "The Hearer")
                ]);

            presets.Add("EelLizard", [
                new(0, "Default"),
                new(3299, "Fluffy"),
                new (4624, "Starlight"),
                new (5526, "Dino")
                ]);

            List<KeyValuePair<int, string>> scavyTemplate = [
                new(0, "Default"),
                new(8146, "Terror"),
                new(8790, "Juvenile"),
                new(15523, "Scrungle")
                ];
            var scavenger = scavyTemplate;
            scavenger.Add(new(-273819595, "GUG"));
            presets.Add("Scavenger", scavenger);
            presets.Add("ScavengerElite", scavyTemplate);
            presets.Add("ScavengerKing", scavyTemplate);
            return presets;
;        }
    }

    public static string GetPath()
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
        return Path.Combine(OptionInterface.ConfigHolder.configDirPath, "ids.json");
    }

    public static void LoadIDs()
    {
        var path = GetPath();
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists)
        {
            LoadedIDs = JsonConvert.DeserializeObject<Dictionary<string, List<KeyValuePair<int, string>>>>(File.ReadAllText(path));
        }
        else
        {
            LoadedIDs = new Dictionary<string, List<KeyValuePair<int, string>>>();
        }
    }

    public static void SaveIDs()
    {
        var path = GetPath();
        if (LoadedIDs != null && LoadedIDs.Keys.Count > 0)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(LoadedIDs));
        }
    }

    public static List<KeyValuePair<int, string>> GetIDs(string id, bool includePresets = true)
    {
        if (LoadedIDs == null)
        {
            LoadIDs();
        }
        if (includePresets)
        {
            var ids = Presets[id];
            ids.AddRange(LoadedIDs[id]);
            return ids;
        }
        return LoadedIDs[id];
    }

    public static void AddID(string crit, int id, string name)
    {
        var ids = new List<KeyValuePair<int, string>>();
        var keyExists = false;
        if (LoadedIDs == null)
        {
            LoadIDs();
        }
        if (LoadedIDs.ContainsKey(crit))
        {
            keyExists = true;
            ids = LoadedIDs[crit];
        }
        var kvp = new KeyValuePair<int, string>(id, name);
        if (ids.Count == 0 || !ids.Contains(kvp))
        {
            ids.Add(kvp);
        }
        if (!keyExists)
        {
            LoadedIDs.Add(crit, ids);
        } else
        {
            LoadedIDs[crit] = ids;
        }
        SaveIDs();
    }

    public static void RemoveID(string crit, int id, string name)
    {
        if (LoadedIDs != null && LoadedIDs.ContainsKey(crit))
        {
            var ids = LoadedIDs[crit];
            if (ids != null)
            {
                ids.Remove(new KeyValuePair<int, string>(id, name));
                SaveIDs();
            }
        }
    }
}
