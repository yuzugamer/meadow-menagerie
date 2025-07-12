using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using UnityEngine;
using UnityEngine.Rendering;

namespace StoryMenagerie;

public class StoryCicadaController : CicadaController
{
    public bool lastThrow;
    public int grabHeld;
    public StoryCicadaController(Cicada creature, OnlineCreature oc, int playerNumber, SlugcatCustomization customization) : base(creature, oc, playerNumber, new ExpandedAvatarData(customization))
    {
    }

    public override void ConsciousUpdate()
    {
        base.ConsciousUpdate();
        if (input[0].pckp)
        {
            // copy pasted
            if (grabHeld == 0 && (creature.grasps == null || creature.grasps[0] == null || creature.grasps[0].grabbed == null))
            {
                foreach (var layer in creature.room.physicalObjects)
                {
                    foreach (var obj in layer)
                    {
                        if ((obj.abstractPhysicalObject.rippleLayer == creature.abstractPhysicalObject.rippleLayer || obj.abstractPhysicalObject.rippleBothSides || creature.abstractPhysicalObject.rippleBothSides) && (obj is Fly || obj is Leech) && Custom.DistLess(creature.mainBodyChunk.pos, (obj as Creature).mainBodyChunk.pos, 50f))
                        {
                            cicada.TryToGrabPrey(obj);
                        }
                    }
                }
            }
            grabHeld++;
        }
        else
        {
            if (grabHeld >= 40 && creature.grasps != null && creature.grasps[0] != null)
            {
                for (int i = 0; i < creature.grasps.Length; i++) {
                    creature.ReleaseGrasp(i);
                }
            }
            grabHeld = 0;
        }

        if (input[0].thrw)
        {
            if (cicada.chargeCounter == 0)
            {
                if (input[0].x != 0 || input[0].y != 0)
                {
                    cicada.Charge(creature.mainBodyChunk.pos + new Vector2(input[0].x, input[0].y) * 40f);
                }
                else
                {
                    cicada.Charge(creature.mainBodyChunk.pos + (creature.graphicsModule as CicadaGraphics).lookDir * 40f);
                }
            }
            lastThrow = true;
        }
        else
        {
            lastThrow = false;
        }
    }
}
