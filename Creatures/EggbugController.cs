using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryMenagerie.Creatures
{
    public class EggbugController : RainMeadow.EggbugController
    {
        public EggbugController(EggBug creature, OnlineCreature oc, int playerNumber, CreatureCustomization customization) : base(creature, oc, playerNumber, new ExpandedAvatarData(customization))
        {
        }
    }
}
