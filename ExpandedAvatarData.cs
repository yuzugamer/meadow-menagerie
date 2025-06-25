using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace StoryMenagerie;

public class ExpandedAvatarData : MeadowAvatarData
{
    public SlugcatCustomization customization;
    public ExpandedAvatarData(SlugcatCustomization customization)
    {
        this.customization = customization;
    }

    public override void ModifyBodyColor(ref Color originalBodyColor)
    {
        customization.ModifyBodyColor(ref originalBodyColor);
    }

    public override void ModifyEyeColor(ref Color originalEyeColor)
    {
        customization.ModifyEyeColor(ref originalEyeColor);
    }
}
