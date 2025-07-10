using System;
using System.Collections.Generic;

namespace StoryMenagerie;

public static class BugHooks
{
    public static void Apply()
    {
        On.AbstractCreature.ctor += On_AbstractCreature_ctor;
        On.AbstractCreature.Realize += On_AbstractCreature_Realize;
        On.AbstractCreature.InitiateAI += On_AbstractCreature_InitiateAI;
        On.CreatureTemplate.ctor_Type_CreatureTemplate_List1_List1_Relationship += On_CreatureTemplate_ctor;
    }

    public static void On_AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
    {
        orig(self, world, creatureTemplate, realizedCreature, pos, ID);
        if (creatureTemplate.type == Enums.BackgroundBug)
        {
            self.state = new NoHealthState(self);
        }
    }

    public static void On_AbstractCreature_Realize(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
    {
        if (self.realizedCreature != null)
        {
            if (self.creatureTemplate.type == Enums.BackgroundBug)
            {
                self.realizedCreature = new BackgroundBugWrapper(self, self.world);
                self.InitiateAI();
                for (int i = 0; i < self.stuckObjects.Count; i++)
                {
                    if (self.stuckObjects[i].A.realizedObject == null)
                    {
                        self.stuckObjects[i].A.Realize();
                    }
                    if (self.stuckObjects[i].B.realizedObject == null)
                    {
                        self.stuckObjects[i].B.Realize();
                    }
                }
            }
        }
        else
        {
            orig(self);
        }
    }

    public static void On_AbstractCreature_InitiateAI(On.AbstractCreature.orig_InitiateAI orig, AbstractCreature self)
    {
        if (self.creatureTemplate.type == Enums.BackgroundBug)
        {
            self.abstractAI.RealAI = new BackgroundBugWrapper.DummyAI(self, self.world);
            return;
        }
        orig(self);
    }

    public static void On_CreatureTemplate_ctor(On.CreatureTemplate.orig_ctor_Type_CreatureTemplate_List1_List1_Relationship orig, CreatureTemplate self, CreatureTemplate.Type type, CreatureTemplate ancestor, List<TileTypeResistance> tileResistances, List<TileConnectionResistance> connectionResistances, CreatureTemplate.Relationship defaultRelationship)
    {
        orig(self, type, ancestor, tileResistances, connectionResistances, defaultRelationship);
        if (type == Enums.BackgroundBug)
        {
            self.name = "Background Bug";
        }
    }
}