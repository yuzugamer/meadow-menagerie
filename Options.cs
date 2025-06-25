using Menu.Remix.MixedUI;
namespace StoryMenagerie;

public sealed class Options : OptionInterface
{
    public static Configurable<bool> BlindMoleLizards;
    public static Configurable<bool> AccurateMovement;

    public Options()
    {
        BlindMoleLizards = config.Bind("cfgMenagerieBlindMoleLizards", true);
        AccurateMovement = config.Bind("cfgMenagerieAccurateMovement", true);
    }

    public override void Initialize()
    {
        base.Initialize();
        var mainTab = new OpTab(this, OptionInterface.Translate("Main"));
        var compatTab = new OpTab(this, OptionInterface.Translate("Compatibility"));
        Tabs = new OpTab[] { mainTab, compatTab };
        var modName = new OpLabel(20, 520, "Story Menagerie", true);
        //var modName2 = new OpLabel(20, 520, "Input Randomizer", true);
        var toggleButton = new OpCheckBox(BlindMoleLizards, 20, 460);
        var toggleLabel = new OpLabel(52, 463, "Blind Mole Lizards (Requires More Slugcats)", false);
        var movementButton = new OpCheckBox(AccurateMovement, 20, 410);
        var movementLabel = new OpLabel(52, 413, "Accurate movement (no exaggerated jumping)", false);
        //var buttonButton = new OpCheckBox(RandomizeButtons, 20, 360);
        //var buttonLabel = new OpLabel(52, 363, "Randomize buttons", false);
        //var movementButton = new OpCheckBox(RandomizeMovement, 20, 310);
        //var movementLabel = new OpLabel(52, 313, "Randomize movement directions", false);
        //var separateButton = new OpCheckBox(SeparateMovement, 20, 260);
        //var separateLabel = new OpLabel(52, 263, "Randomize movement separately from buttons", false);
        //var neutralButton = new OpCheckBox(IncludeNeutral, 20, 210);
        //var neutralLabel = new OpLabel(52, 213, "Include holding neutral into movement randomization", false);
        Tabs[0].AddItems(modName, toggleButton, toggleLabel);
        //var syncButton = new OpCheckBox(MeadowSync, 20, 410);
        //var syncLabel = new OpLabel(52, 413, "Sync randomized inputs in Rain Meadow (Host only)", false);
        //Tabs[1].AddItems(modName2, syncButton, syncLabel);
    }
}