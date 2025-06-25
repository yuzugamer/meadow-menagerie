using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using RainMeadow;

namespace StoryMenagerie;

public class MenagerieHUD : HUD.HUD
{
    public Creature Creature
    {
        get
        {
            return (owner as CreatureController).creature;
        }
    }
    public MenagerieHUD(FContainer[] fContainers, RainWorld rainWorld, IOwnAHUD owner) : base(fContainers, rainWorld, owner)
    {

    }
}
