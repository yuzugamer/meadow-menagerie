using JetBrains.Annotations;
using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace StoryMenagerie;

public class CreatureCustomization : SlugcatCustomization
{
    public CreatureTemplate.Type selectedCreature;
    public bool[] enabledColors;
    public bool special;
    public int id;
    public override EntityDataState MakeState(OnlineEntity onlineEntity, OnlineResource inResource)
    {
        return new State(this);
    }

    [UsedImplicitly]
    public new class State : SlugcatCustomization.State
    {
        [OnlineField]
        public CreatureTemplate.Type selectedCreature;
        [OnlineField]
        public bool[] enabledColors;
        [OnlineField]
        public bool special;
        public State() { }
        public State(CreatureCustomization customization) : base()
        {
            selectedCreature = customization.selectedCreature;
            enabledColors = customization.enabledColors;
            special = customization.special;
        }

        public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
        {
            base.ReadTo(entityData, onlineEntity);
            var customization = (CreatureCustomization)entityData;
            customization.selectedCreature = selectedCreature;
            customization.enabledColors = enabledColors;
            customization.special = special;
        }
    }
}