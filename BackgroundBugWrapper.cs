using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace StoryMenagerie;

public class BackgroundBugWrapper : Creature
{
    public static ConditionalWeakTable<CosmeticInsect, BackgroundBugWrapper> bugWrappers = new();
    public CosmeticInsect bug;
    public CosmeticInsect.Type type = null;
    public BackgroundBugWrapper(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
    {
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (this.room != null)
        {
            if (this.bug == null)
            {
                RealizeBug(this.room, this.abstractCreature.pos.Tile.ToVector2());
            }
            else
            {
                if (this.bug.room != this.room)
                {
                    bug.Destroy();
                    bug = null;
                    RealizeBug(this.room, this.abstractCreature.pos.Tile.ToVector2());
                }
                this.abstractCreature.pos = this.room.GetWorldCoordinate(this.bug.pos);
            }
        }
    }

    public override void PlaceInRoom(Room placeRoom)
    {
        base.PlaceInRoom(placeRoom);
        RealizeBug(placeRoom, this.abstractCreature.pos.Tile.ToVector2());
    }

    public override void NewRoom(Room newRoom)
    {
        base.NewRoom(newRoom);
        bug.Destroy();
        bug = null;
        RealizeBug(newRoom, this.abstractCreature.pos.Tile.ToVector2());
    }

    public override void SpitOutOfShortCut(IntVector2 pos, Room newRoom, bool spitOutAllSticks)
    {
        base.SpitOutOfShortCut(pos, newRoom, spitOutAllSticks);
        bug.Destroy();
        bug = null;
        RealizeBug(newRoom, this.abstractCreature.pos.Tile.ToVector2());
    }

    public void RealizeBug(Room room, Vector2 pos)
    {
        if (type == CosmeticInsect.Type.TinyDragonFly)
        {
            bug = new TinyDragonfly(room, pos);
        }
        else if (type == CosmeticInsect.Type.Moth)
        {
            bug = new Moth(room, pos);
        }
        else if (type == CosmeticInsect.Type.Wasp)
        {
            bug = new Wasp(room, pos);
        }
        else if (type == CosmeticInsect.Type.WaterGlowworm)
        {
            bug = new WaterGlowworm(room, pos);
        }
        else
        {
            bug = new FireFly(room, pos);
        }
        if (!bugWrappers.TryGetValue(bug, out var _))
        {
            bugWrappers.Add(bug, this);
        }
    }

    public class DummyAI : ArtificialIntelligence
    {
        public DummyAI(AbstractCreature creature, World world) : base(creature, world) { }
    }
}