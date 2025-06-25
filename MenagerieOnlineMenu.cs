using System;
using System.Collections.Generic;
using UnityEngine;
using RainMeadow;
using Menu;
using System.Drawing.Text;
using System.Linq;
using StoryMenagerie;
using Menu.Remix.MixedUI;

namespace StoryMenagerie;

public class MenagerieOnlineMenu : StoryOnlineMenu
{
    public CreatureTemplate.Type currentCreature;
    public CreatureTemplate.Type playerSelectedCreature;
    public CreatureTemplate.Type selectedCreatureGroup;
    public CheckBox reqHostCreature;
    public MenagerieGameMode menagerieGameMode;
    public MenuLabel? creatureLabel;
    public CreatureSelector creatureGroupSelector;
    public CreatureSelector subCreatureSelector;
    public OpUpdown idSelector;
    public Dictionary<CreatureTemplate.Type, List<CreatureTemplate.Type>> creatureGroups = new Dictionary<CreatureTemplate.Type, List<CreatureTemplate.Type>>();
    public SkinSelector skinSelector;
    public MenuLabel? skinLabel;
    public int skinIndex;
    public int secretinput;

    public CreatureTemplate.Type CurrentCreature
    {
        get
        {
            return currentCreature ?? CreatureTemplate.Type.Slugcat;
        }
    }

    public CreatureTemplate.Type PlayerSelectedCreature
    {
        get
        {
            return playerSelectedCreature ?? CreatureTemplate.Type.Slugcat;
        }
        set
        {
            SkinIndex = 0;
            RemoveSkinList();
            playerSelectedCreature = value;
            currentCreature = value;
            menagerieGameMode.selectedCreature = value;
            StoryMenagerie.Debug("Creature has been set to " + value.value);
        }
    }

    public int SkinIndex
    {
        get
        {
            return skinIndex;
        }
        set
        {
            skinIndex = value;
            if (IDManager.Presets != null && currentCreature != null && IDManager.Presets.ContainsKey(currentCreature.value))
            {
                var ids = IDManager.Presets[currentCreature.value];
                if (value < ids.Count)
                {
                    StoryMenagerie.Debug("ID has been set to " + ids[value].Key);
                    menagerieGameMode.critID = ids[value].Key;
                }
            }
            menagerieGameMode.skinIndex = value;
        }
    }

    public MenagerieOnlineMenu(ProcessManager manager) : base(manager)
    {
        menagerieGameMode = (MenagerieGameMode)OnlineManager.lobby.gameMode;
        if (OnlineManager.lobby.isOwner)
        {
            menagerieGameMode.requiredHostCreature = null; // Default option is in remix menu.
        }
        var reqHostCreature = (CheckBox)MenuHooks.reqCampaignSlug.GetValue(this);
        var displayText = Translate("Require same creature");
        reqHostCreature.displayText = displayText;
        reqHostCreature.label.text = displayText;
        reqHostCreature.IDString = "HOSTCRITONLY";
        MenuHooks.reqCampaignSlug.SetValue(this, reqHostCreature);
        this.reqHostCreature = reqHostCreature;
        var playableCreatures = StoryMenagerie.playableCreatures.Keys;
        //List<CreatureTemplate.Type> groups = new List<CreatureTemplate.Type>();
        var uniqueAncestors = new List<CreatureTemplate.Type>();
        foreach (var crit in playableCreatures)
        {
            var ancestor = StoryMenagerie.playableCreatures[crit];
            if (ancestor == null)
            {
                creatureGroups.Add(crit, null);
                continue;
            }
            else if (!uniqueAncestors.Contains(ancestor))
            {
                uniqueAncestors.Add(ancestor);
                var unique = true;
                foreach (var crit2 in playableCreatures)
                {
                    if (crit2 != crit && StoryMenagerie.playableCreatures[crit2] == ancestor)
                    {
                        unique = false;
                        break;
                    }
                }
                if (unique)
                {
                    creatureGroups.Add(crit, null);
                    continue;
                }
            }
            if (!creatureGroups.Keys.Contains(ancestor))
            {
                creatureGroups.Add(ancestor, []);
            }
            creatureGroups[ancestor].Add(crit);
        }
        var oldIndex = menagerieGameMode.skinIndex;
        if (menagerieGameMode.selectedCreature != null)
        {
            PlayerSelectedCreature = menagerieGameMode.selectedCreature;
            foreach (var kvp in creatureGroups)
            {
                if (kvp.Value != null)
                {
                    if (kvp.Value.Contains(playerSelectedCreature))
                    {
                        selectedCreatureGroup = kvp.Key;
                        break;
                    }
                }
                else if (kvp.Key == playerSelectedCreature)
                {
                    selectedCreatureGroup = kvp.Key;
                }
            }
            if (selectedCreatureGroup == null)
            {
                StoryMenagerie.LogError("Already selected creature does not belong to a group!");
            }
        }
        else
        {
            PlayerSelectedCreature = CreatureTemplate.Type.Slugcat;
            selectedCreatureGroup = CreatureTemplate.Type.Slugcat;
            menagerieGameMode.selectedCreature = CreatureTemplate.Type.Slugcat;
        }
        // lazy? perhaps
        skinIndex = oldIndex;
    }

    public override void Update()
    {
        base.Update();
        var hideSelection = menagerieGameMode.requiredHostCreature != null && !OnlineManager.lobby.isOwner;
        // disable dumb thing
        hideSelection = false;
        if (!hideSelection && !menagerieGameMode.secretMode)
        {
            SetupCreatureGroupList();
            currentCreature = PlayerSelectedCreature;
        }
        else
        {
            RemoveCreatureGroupList();
            RemoveSubCreatureList();
            PlayerSelectedCreature = menagerieGameMode.requiredHostCreature;
        }
        if (currentCreature != CreatureTemplate.Type.Slugcat || hideSelection || menagerieGameMode.secretMode)
        {
            RemoveSlugcatList();
        }
        else
        {
            pages[0].ClearMenuObject(ref slugcatLabel);
        }
        if (selectedCreatureGroup != null && creatureGroups[selectedCreatureGroup] != null)
        {
            SetupSubCreatureList();
        }
        else
        {
            RemoveSubCreatureList();
        }
        skinIndex = menagerieGameMode.skinIndex;
        if (!menagerieGameMode.secretMode && currentCreature != CreatureTemplate.Type.Slugcat && IDManager.Presets != null && IDManager.Presets.ContainsKey(currentCreature.value) && IDManager.Presets[currentCreature.value] != null)
        {
            if (skinIndex >= IDManager.Presets[currentCreature.value].Count) SkinIndex = 0;
            SetupSkinList();
        }
        else
        {
            RemoveSkinList();
        }
        if (creatureGroupSelector != null)
        {
            creatureGroupSelector.Crit = selectedCreatureGroup;
        }
        if (subCreatureSelector != null)
        {
            subCreatureSelector.Crit = currentCreature;
        }
        if (skinSelector != null)
        {
            skinSelector.Index = skinIndex;
        }

        if (Input.anyKey)
        {
            if (Input.GetKey(KeyCode.G) || Input.GetKey(KeyCode.U))
            {
                if (((secretinput == 0 || secretinput == 2) && Input.GetKey(KeyCode.G)) || (secretinput == 1 && Input.GetKey(KeyCode.U)))
                {
                    secretinput++;
                }
            }
            else
            {
                secretinput = 0;
            }
            if (secretinput == 3)
            {
                secretinput = 0;
                menagerieGameMode.secretMode = !menagerieGameMode.secretMode;
                if (menagerieGameMode.secretMode)
                {
                    PlayerSelectedCreature = CreatureTemplate.Type.Scavenger;
                    menagerieGameMode.critID = -273819595;
                    skinIndex = 0;
                    var mic = manager.menuMic;
                    if (mic != null)
                    {
                        mic.PlaySound(SoundID.Thunder, 0f, 0.7f, 1f);
                    }
                }
            }
        }
    }

    public void SetupCreatureGroupList()
    {
        Vector2 pos = new(394, 553);
        if (creatureLabel == null)
        {
            creatureLabel = new(this, pages[0], Translate("Selected Creature"), pos, new(110, 30), true);
            pages[0].subObjects.Add(creatureLabel);
        }
        if (creatureGroupSelector == null)
        {
            //first player button is 30 pos below size of list. and list top part is 30 below the title. Plus
            creatureGroupSelector = new(this, pages[0], new(pos.x, pos.y - (ButtonSize * 2)), MaxVisibleOnList, ButtonSpacingOffset, selectedCreatureGroup, this, false);
            pages[0].subObjects.Add(creatureGroupSelector);
        }
    }
    public void RemoveCreatureGroupList()
    {
        pages[0].ClearMenuObject(ref creatureLabel);
        pages[0].ClearMenuObject(ref creatureGroupSelector);
    }

    public void SetupSubCreatureList()
    {
        Vector2 pos = new(514, 553);
        if (creatureLabel == null)
        {
            //creatureLabel = new(this, pages[0], Translate("Selected Creature"), pos, new(110, 30), true);
            //pages[0].subObjects.Add(creatureLabel);
        }
        if (subCreatureSelector == null)
        {
            //first player button is 30 pos below size of list. and list top part is 30 below the title. Plus
            subCreatureSelector = new(this, pages[0], new(pos.x, pos.y - (ButtonSize * 2)), MaxVisibleOnList, ButtonSpacingOffset, CurrentCreature, this, true);
            pages[0].subObjects.Add(subCreatureSelector);
            //subCreatureSelector.OpenCloseList(true, false, true);
        }
    }

    public void RemoveSubCreatureList()
    {
        //pages[0].ClearMenuObject(ref creatureLabel);
        pages[0].ClearMenuObject(ref subCreatureSelector);
    }

    public void SetupSkinList()
    {
        Vector2 pos = new(1200, 553);
        if (skinLabel == null)
        {
            skinLabel = new(this, pages[0], Translate("Skins"), new(pos.x + 10f, pos.y), new(110, 30), true);
            pages[0].subObjects.Add(skinLabel);
        }
        if (skinSelector == null)
        {
            //first player button is 30 pos below size of list. and list top part is 30 below the title. Plus
            skinSelector = new(this, pages[0], new(pos.x, pos.y - (ButtonSize * 2)), MaxVisibleOnList, ButtonSpacingOffset * 1.4f, skinIndex, this, true);
            pages[0].subObjects.Add(skinSelector);
            skinSelector.OpenCloseList(true, false, true);
        }
    }

    public void RemoveSkinList()
    {
        pages[0].ClearMenuObject(ref skinLabel);
        pages[0].ClearMenuObject(ref skinSelector);
    }

    /*public void SetupSelectableCreatures()
    {
        if (selectableSlugcats == null)
        {
            var SelectableSlugcatsEnumerable = slugcatColorOrder.AsEnumerable();
            if (ModManager.MSC)
            {
                if (!SelectableSlugcatsEnumerable.Contains(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup))
                {
                    SelectableSlugcatsEnumerable = SelectableSlugcatsEnumerable.Append(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup);
                }
            }
            selectableSlugcats = SelectableSlugcatsEnumerable.ToArray();
        }
    }*/

    public CreatureButton[] GetCreatureSelectionButtons(CreatureSelector creatureSelector, ButtonScroller buttonScroller, bool subSelector)
    {
        List<CreatureButton> creatureButtons = [];
        var creatures = subSelector ? creatureGroups[selectedCreatureGroup].ToArray() : creatureGroups.Keys.ToArray();
        foreach (var creature in creatures)
        {
            if (creature != creatureSelector.Crit)
            {
                Action<CreatureTemplate.Type> onClick = !subSelector ? (crit) =>
                {
                    selectedCreatureGroup = crit;
                    if (creatureGroups[creature] == null)
                    {
                        PlayerSelectedCreature = crit;
                    }
                    else
                    {
                        PlayerSelectedCreature = creatureGroups[crit][0];
                        SetupSubCreatureList();
                        subCreatureSelector.OpenCloseList(true, false, true);
                    }
                    creatureSelector.OpenCloseList(false, true, true);
                }
                : (crit) =>
                {
                    PlayerSelectedCreature = crit;
                    //selectedCreatureGroup = null;
                    creatureSelector.OpenCloseList(false, true, true);
                };
                CreatureButton storyMenuCreatureButton = new(this, buttonScroller, creature, onClick);
                creatureButtons.Add(storyMenuCreatureButton);
            }
        }
        return [.. creatureButtons];
    }

    public SkinButton[] GetSkinSelectionButtons(SkinSelector selector, ButtonScroller buttonScroller, bool subSelector)
    {
        List<SkinButton> skinButtons = [];
        var ids = IDManager.Presets[currentCreature.value];
        for (int i = 0; i < ids.Count; i++)
        {
            if (i != selector.selectedIndex)
            {
                Action<int> onClick = (int index) =>
                {
                    SkinIndex = index;
                    selector.OpenCloseList(false, true, true);
                };
                SkinButton skinButton = new(this, buttonScroller, currentCreature.value, i, onClick);
                skinButtons.Add(skinButton);
            }
        }
        return [.. skinButtons];
    }
    public class CreatureButton : ButtonScroller.ScrollerButton
    {
        public CreatureButton(Menu.Menu menu, MenuObject owner, CreatureTemplate.Type creature, Action<CreatureTemplate.Type> onReceiveCreature, Vector2 size = default) : base(menu, owner, "", Vector2.zero, size == default ? new(110, 30) : size)
        {
            this.crit = creature;
            name = menuLabel.text = menu.Translate((StaticWorld.creatureTemplates != null && StaticWorld.creatureTemplates.Length > 0) ? StaticWorld.GetCreatureTemplate(crit).name : menu.Translate(new CreatureTemplate(crit, null, new List<TileTypeResistance>() { }, new List<TileConnectionResistance>() { }, new CreatureTemplate.Relationship()).name ?? crit.value));
            OnClick += (_) =>
            {
                onReceiveCreature?.Invoke(this.crit);
            };
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (menuLabel != null)
            {
                menuLabel.text = name;
            }
        }
        public string name;
        public CreatureTemplate.Type crit;
    }

    public class CreatureSelector : ButtonSelector
    {
        public CreatureSelector(Menu.Menu menu, MenuObject owner, Vector2 pos, int amtOfCritsToShow, float spacing, CreatureTemplate.Type currentCreature, MenagerieOnlineMenu storyMenu, bool isSubSelector) : base(menu, owner, "", pos, new(110, 30), amtOfCritsToShow, spacing, menu.Translate("Press on the button to open/close the creature selection list"))
        {
            Crit = currentCreature;
            populateList = (selector, scroller) =>
            {
                return storyMenu.GetCreatureSelectionButtons((CreatureSelector)selector, scroller, isSubSelector);
            };
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
        }
        public CreatureTemplate.Type Crit
        {
            get
            {
                return crit;
            }
            set
            {
                if (value != crit)
                {
                    crit = value;
                    // checks creature templates to see if static world is initialized, gets template name if so, otherwise make new template and get less accurate name from its ctor
                    menuLabel.text = menu.Translate((StaticWorld.creatureTemplates != null && StaticWorld.creatureTemplates.Length > 0) ? StaticWorld.GetCreatureTemplate(crit).name : menu.Translate(new CreatureTemplate(crit, null, new List<TileTypeResistance>() { }, new List<TileConnectionResistance>() { }, new CreatureTemplate.Relationship()).name ?? crit.value));
                    RefreshScrollerList();
                }
            }
        }
        public CreatureTemplate.Type crit;
    }

    public class SkinSelector : ButtonSelector
    {
        public SkinSelector(MenagerieOnlineMenu menu, MenuObject owner, Vector2 pos, int amtOfSkinsToShow, float spacing, int currentIndex, MenagerieOnlineMenu storyMenu, bool isSubSelector) : base(menu, owner, "", pos, new(130f, 42f), amtOfSkinsToShow, spacing, menu.Translate("Press on the button to open/close the creature selection list"))
        {
            //menuLabel.pos += new Vector2(-7f, 2f);
            menuLabel.pos.y += 7f;
            idLabel = new MenuLabel(menu, this, "", menuLabel.pos - new Vector2(0f, 14f), menuLabel.size, false);
            idLabel.label.alignment = FLabelAlignment.Center;
            subObjects.Add(idLabel);
            if ((IDManager.Presets != null && IDManager.Presets.ContainsKey((menu as MenagerieOnlineMenu).currentCreature.value)))
            {
                var ids = IDManager.Presets[(menu as MenagerieOnlineMenu).currentCreature.value][selectedIndex];
                menuLabel.text = menu.Translate(ids.Value);
                idLabel.text = ids.Key.ToString();
            } else
            {
                menuLabel.text = "skin";
                idLabel.text = "id";
            }
            idLabel.label._color = menuLabel.label._color;
            menuLabel.label._color = Color.white;
            Index = currentIndex;
            populateList = (selector, scroller) =>
            {
                return storyMenu.GetSkinSelectionButtons((SkinSelector)selector, scroller, isSubSelector);
            };
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (menuLabel != null && idLabel != null)
            {
                //idLabel.size = menuLabel.size * 0.75f;
                idLabel.pos = menuLabel.pos - new Vector2(0f, 14f);
            }
        }

        public MenuLabel idLabel;
        public int selectedIndex;
        public int Index
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                if (value != selectedIndex)
                {
                    selectedIndex = value;
                    if ((IDManager.Presets != null && IDManager.Presets.ContainsKey((menu as MenagerieOnlineMenu).currentCreature.value)))
                    {
                        var ids = IDManager.Presets[(menu as MenagerieOnlineMenu).currentCreature.value][selectedIndex];
                        menuLabel.text = menu.Translate(ids.Value);
                        idLabel.text = ids.Key.ToString();
                    }
                    else
                    {
                        menuLabel.text = "skin";
                        idLabel.text = "id";
                    }
                    RefreshScrollerList();
                }
            }
        }
    }

    public class SkinButton : ButtonScroller.ScrollerButton
    {
        public SkinButton(Menu.Menu menu, MenuObject owner, string creature, int index, Action<int> onReceiveSkin, Vector2 size = default) : base(menu, owner, "", Vector2.zero, size == default ? new(130, 42f) : size)
        {
            this.crit = creature;
            var kvp = IDManager.Presets[crit][index];
            menuLabel.text = menu.Translate(kvp.Value);
            //menuLabel.pos += new Vector2(-7f, 2f);
            menuLabel.pos.y += 7f;
            idLabel = new MenuLabel(menu, this, kvp.Key.ToString(), menuLabel.pos - new Vector2(0f, 14f), menuLabel.size, false);
            idLabel.label.alignment = FLabelAlignment.Center;
            idLabel.label._color = menuLabel.label._color;
            menuLabel.label._color = Color.white;
            subObjects.Add(idLabel);
            OnClick += (_) =>
            {
                onReceiveSkin?.Invoke(index);
            };
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (menuLabel != null && idLabel != null)
            {
                idLabel.pos = menuLabel.pos - new Vector2(0f, 14f);
                //menuLabel.text = name;
                //menuLabel.size *= 0.75f;
                //menuLabel.pos += new Vector2(27.5f, 7.5f);
            }
        }
        public MenuLabel idLabel;
        public int index;
        public int id;
        public string crit;
    }
}
