using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;

namespace StoryMenagerie.Creatures
{
    public class NoodleController : RainMeadow.NoodleController
    {
        public bool lastGrab;
        public float attackCharge;
        public float attackReady;
        public bool lastThrow;
        public BigNeedleWorm bigNoodle => creature as BigNeedleWorm;
        public bool isBigNoodle => creature is BigNeedleWorm;
        public NoodleController(Creature creature, OnlineCreature oc, int playerNumber, CreatureCustomization customization) : base(creature, oc, playerNumber, new ExpandedAvatarData(customization))
        {
        }

        // we love copy pasted code
        public void Attack()
        {
            var worm = bigNoodle;
            Vector2 p = worm.bodyChunks[1].pos;
            Vector2 p2 = worm.bodyChunks[1].pos;
            if (worm.controlledCharge == Vector2.zero)
            {
                worm.controlledCharge = Custom.RNV() * 80f;
            }
            if (input[0].AnyDirectionalInput)
            {
                worm.controlledCharge = input[0].analogueDir.normalized * 80f;
            }
            else
            {
                worm.controlledCharge = worm.lookDir * 80f;
            }

            // auto-aim wild creatures
            //if (!this.onlineCreature.isMine || Options.NootAutoAim.Value)
            if (!input[0].AnyDirectionalInput)
            {
                Creature creature = null;
                float num = float.MaxValue;
                float current = Custom.VecToDeg(worm.controlledCharge);
                HashSet<AbstractCreature> nonPlayers = new HashSet<AbstractCreature>(); // I've heard hashsets are faster, so why not
                foreach (var critter in worm.abstractCreature.Room.creatures) // find which creatures in the room are not players
                {
                    // this is oh so simple but sadly only finds slugcat players
                    //if (critter.realizedCreature is not Player)
                    //{
                    //    nonPlayers.Add(critter);
                    //}

                    // juggling types like this to find IDs is slightly less simple but finds non-slugcat players just fine (at least according to my quick 2 minute test)
                    // maybe there's a more elegant way to do this?
                    bool isPlayer = false;
                    if (OnlineManager.lobby.gameMode is not StoryGameMode story || !story.friendlyFire)
                    {
                        foreach (var playerAvatar in OnlineManager.lobby.playerAvatars)
                        {
                            var onlineEntity = playerAvatar.Value.FindEntity(true);
                            if (onlineEntity is OnlinePhysicalObject onlinePhysicalPlayer)
                            {
                                var abstractPhysicalPlayer = onlinePhysicalPlayer.apo;
                                if (abstractPhysicalPlayer.ID == critter.ID)
                                {
                                    isPlayer = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (!isPlayer)
                    {
                        nonPlayers.Add(critter);
                    }
                }
                foreach (var nonPlayer in nonPlayers) // now we can finally aim for the non-players
                {
                    if (worm.abstractCreature != nonPlayer && nonPlayer.realizedCreature != null)
                    {
                        float target = Custom.AimFromOneVectorToAnother(worm.bodyChunks[1].pos, nonPlayer.realizedCreature.mainBodyChunk.pos);
                        float num2 = Custom.Dist(worm.bodyChunks[1].pos, nonPlayer.realizedCreature.mainBodyChunk.pos);
                        if (Mathf.Abs(Mathf.DeltaAngle(current, target)) < 35f && num2 < num)
                        {
                            num = num2;
                            creature = nonPlayer.realizedCreature;
                        }
                    }
                }
                if (creature != null)
                {
                    worm.controlledCharge = Custom.DirVec(worm.bodyChunks[1].pos, creature.mainBodyChunk.pos) * 80f;
                }
            }
            // end of auto-aim wild creatures

            p2 = worm.bodyChunks[1].pos + worm.controlledCharge;
            float num3 = Mathf.InverseLerp(0.5f, 0.95f, attackReady);
            Vector2 vector = Custom.DirVec(p, p2);
            float num4 = Mathf.InverseLerp(0.2f, 0.9f, Vector2.Dot(vector, Custom.DirVec(worm.GetSegmentPos(worm.TotalSegments / 2), worm.mainBodyChunk.pos)));
            num4 *= Mathf.InverseLerp(20f, 50f, Vector2.Distance(worm.BigAI.attackTargetPos, worm.mainBodyChunk.pos));
            if (attackCharge > 0f && num3 == 1f)
            {
                attackCharge = Mathf.Min(1f, attackCharge + 0.018181818f);
            }
            else
            {
                attackCharge = Mathf.Max(0f, attackCharge - 0.033333335f);
            }
            float d = 1f;
            float d2 = 1.9f;
            d = 0f;
            d2 = 0f;
            num3 = 1f;
            worm.SetSegmentVel(0, vector * 5f);
            for (int j = 1; j < worm.TotalSegments; j++)
            {
                worm.SetSegmentVel(j, -vector * 4f);
            }
            Vector2 a = Vector2.ClampMagnitude(worm.BigAI.attackFromPos - vector * attackCharge * 100f - worm.bodyChunks[1].pos, 40f) / 40f * d2 * num3;
            for (int k = 1; k < worm.TotalSegments; k++)
            {
                float num5 = Mathf.InverseLerp(0f, (float)(worm.TotalSegments - 1), (float)k);
                worm.SetSegmentVel(k, worm.GetSegmentVel(k) * Mathf.Lerp(1f, 0.75f, num3 * Mathf.InverseLerp(0.25f, 0.75f, num5)));
                worm.AddSegmentVel(k, a * (1f - num5) + vector * Mathf.Lerp(3f, -6f, num5) * num3 * d * Mathf.InverseLerp(0.75f + 0.6f * attackCharge, 0.5f, num5));
            }
            if (attackCharge > 0.5f)
            {
                worm.crawlSin += 0.8f * attackCharge;
                worm.SinMovementInBody(0f, 1.5f * (worm.small ? 0.6f : 1f) * Mathf.InverseLerp(0.5f, 0.75f, attackCharge), 0.4f, 0.4f);
            }
            if (attackCharge >= 1f && worm.dodgeDelay < 1 && worm.room.VisualContact(worm.mainBodyChunk.pos, worm.FangPos))
            {
                attackCharge = 0f;
                worm.swishDir = new Vector2?(vector);
                worm.swishCounter = 6;
                worm.room.PlaySound(SoundID.Big_Needle_Worm_Attack, worm.mainBodyChunk);
                worm.attackRefresh = true;
            }
        }

        public override void ConsciousUpdate()
        {
            base.ConsciousUpdate();
            if (isBigNoodle)
            {
                bigNoodle.chargingAttack = attackCharge;
                bigNoodle.attackReady = attackReady;
                if (bigNoodle.swishCounter > 0)
                {
                    bigNoodle.Swish();
                }
                if (input[0].pckp)
                {
                    if (!lastGrab && attackReady < 0.05f)
                    {
                        attackReady = 0.05f;
                        bigNoodle.controlledCharge = Vector2.zero;
                    }
                    if (attackReady > 0f && !bigNoodle.attackRefresh)
                    {
                        attackReady = Custom.LerpAndTick(attackReady, 1f, 0f, 0.0375f);
                        if (attackCharge < 0.1f)
                        {
                            attackCharge = 0.1f;
                        }
                    }
                    lastGrab = true;
                }
                else
                {
                    attackCharge = Mathf.Min(0f, attackCharge - 0.006f);
                    attackReady = Mathf.Min(0f, attackReady - 0.006f);
                    bigNoodle.attackRefresh = false;
                    lastGrab = false;
                }
                if (attackCharge > 0f)
                {
                    Attack();
                }
                bigNoodle.chargingAttack = attackCharge;
                bigNoodle.attackReady = attackReady;
            }
            if (input[0].thrw)
            {
                if (!lastThrow)
                {
                    if (isBigNoodle)
                    {
                        bigNoodle.SmallCry();
                    }
                    else if (noodle is SmallNeedleWorm small)
                    {
                        small.SmallScream(true);
                    }
                    lastThrow = true;
                }
                else
                {
                    lastThrow = false;
                }
            }
        }
    }
}
