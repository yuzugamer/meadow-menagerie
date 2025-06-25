using MoreSlugcats;
using RainMeadow;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace StoryMenagerie;

public class MarkSprite : CosmeticSprite
{
    public Creature owner;
    public CreatureController controller;
    public float alpha;
    public float baseAlpha = 1f;
    public float lastAlpha;
    public int secondaryChunkIndex;
    public MarkSprite(Creature owner)
    {
        this.owner = owner;
        var story = owner.abstractCreature.world.game.GetStorySession;
        if (story.saveStateNumber == SlugcatStats.Name.Red || (ModManager.MSC && story.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Rivulet))
        baseAlpha = Mathf.Pow(Mathf.InverseLerp(4f, 14f, (float)story.saveState.cycleNumber), 3.5f);
        if (owner.bodyChunks.Length > 1)
        {
            if (owner.bodyChunkConnections != null && owner.bodyChunkConnections.Length > 0)
            {
                var selectedIndex = 0;
                foreach (var connection in owner.bodyChunkConnections)
                {
                    if (connection.type == PhysicalObject.BodyChunkConnection.Type.Normal)
                    {
                        if (connection.chunk1 == owner.mainBodyChunk)
                        {
                            for (int i = 0; i < owner.bodyChunks.Length; i++)
                            {
                                if (i != owner.mainBodyChunkIndex && connection.chunk2 == owner.bodyChunks[i])
                                {
                                    selectedIndex = i;
                                    goto SetIndex;
                                }
                            }
                        }
                        else if (connection.chunk2 == owner.mainBodyChunk)
                        {
                            for (int i = 0; i < owner.bodyChunks.Length; i++)
                            {
                                if (i != owner.mainBodyChunkIndex && connection.chunk1 == owner.bodyChunks[i])
                                {
                                    selectedIndex = i;
                                    goto SetIndex;
                                }
                            }
                        }
                    }
                }
                SetIndex:
                secondaryChunkIndex = selectedIndex;
            }
            else
            {
                if (owner.mainBodyChunkIndex == 0) secondaryChunkIndex = 1;
                else
                {
                    int selectedIndex = 0;
                    var candidateDist = float.MaxValue;
                    for (int i = 0; i < owner.bodyChunks.Length; i++)
                    {
                        if (i != owner.mainBodyChunkIndex)
                        {
                            var dist = Vector2.Distance(owner.bodyChunks[i].pos, owner.mainBodyChunk.pos);
                            if (dist < candidateDist)
                            {
                                candidateDist = dist;
                                selectedIndex = i;
                            }
                        }
                    }
                    secondaryChunkIndex = selectedIndex;
                }
            }
        }
        else secondaryChunkIndex = 0;
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[2];
        sLeaser.sprites[0] = new FSprite("pixel", true);
        sLeaser.sprites[0].scale = 5f;
        sLeaser.sprites[1] = new FSprite("Futile_White", true);
        sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["FlatLight"];
        var container = rCam.ReturnFContainer("Foreground");
        container.AddChild(sLeaser.sprites[0]);
        container.AddChild(sLeaser.sprites[1]);
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        var offset = 30f * (owner.mainBodyChunk.rad / 9f);
        var mainPos = Vector2.Lerp(owner.mainBodyChunk.lastPos, owner.mainBodyChunk.pos, timeStacker);
        var secondaryPos = Vector2.Lerp(owner.bodyChunks[secondaryChunkIndex].lastPos, owner.bodyChunks[secondaryChunkIndex].pos, timeStacker);
        var pos = mainPos + Custom.DirVec(secondaryPos, mainPos) * 30f + new Vector2(0f, offset);
        sLeaser.sprites[0].x = pos.x - camPos.x;
        sLeaser.sprites[0].y = pos.y - camPos.y;
        sLeaser.sprites[0].alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
        sLeaser.sprites[1].x = pos.x - camPos.x;
        sLeaser.sprites[1].y = pos.y - camPos.y;
        sLeaser.sprites[1].alpha = 0.2f * Mathf.Lerp(lastAlpha, alpha, timeStacker);
        sLeaser.sprites[1].scale = 1f + Mathf.Lerp(lastAlpha, alpha, timeStacker);
    }

    public override void Update(bool eu)
    {
        lastAlpha = alpha; ;
        if (!owner.dead && owner.abstractCreature.world.game.session is StoryGameSession story && story.saveState.deathPersistentSaveData.theMark && !(ModManager.MSC && this.room != null && this.room.game.wasAnArtificerDream && this.room.abstractRoom.name != "GW_ARTYNIGHTMARE"))
        {
            alpha = Custom.LerpAndTick(alpha, Mathf.Clamp(Mathf.InverseLerp(30f, 80f, (float)controller.touchedNoInputCounter) - UnityEngine.Random.value * Mathf.InverseLerp(80f, 30f, (float)controller.touchedNoInputCounter), 0f, 1f) * baseAlpha, 0.1f, 0.033333335f);
        } else
        {
            alpha = 0f;
        }
        if (owner == null || owner.slatedForDeletetion || owner.room != this.room)
        {
            Destroy();
        }
    }
}
