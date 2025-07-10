using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryMenagerie;

public static class Enums
{
    public static void Register()
    {
        if (BackgroundBug == null)
        {
            BackgroundBug = new("MenagerieBackgroundBug", true);
        }
    }

    public static void Unregister()
    {
        if (BackgroundBug != null)
        {
            BackgroundBug.Unregister();
            BackgroundBug = null;
        }
    }

    public static CreatureTemplate.Type BackgroundBug = null;
}
