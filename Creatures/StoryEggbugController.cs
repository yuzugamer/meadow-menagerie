using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryMenagerie
{
    public class StoryEggbugController : EggbugController
    {
        public StoryEggbugController(EggBug creature, OnlineCreature oc, int playerNumber, SlugcatCustomization customization) : base(creature, oc, playerNumber, new ExpandedAvatarData(customization))
        {
            this.story().storyCustomization = customization;
            this.customization = new ExpandedAvatarData(customization);
        }
    }
}
