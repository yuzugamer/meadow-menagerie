/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RainMeadow;
using RainMeadow.Generics;
using static RainMeadow.OnlineEntity.EntityData;
using static RainMeadow.OnlineState;

namespace StoryMenagerie;

public class StoryControllerData : OnlineEntity.EntityData
{
    public Player.InputPackage input;
    public CreatureController.SpecialInput specialInput;
    public WorldCoordinate destination;
    public float moveSpeed;

    public override EntityDataState MakeState(OnlineEntity onlineEntity, OnlineResource inResource)
    {
        if (inResource is RoomSession)
        {
            RainMeadow.RainMeadow.Trace($"{this} for {onlineEntity} making state in {inResource}");
            return new State(this);
        }
        RainMeadow.RainMeadow.Trace($"{this} for {onlineEntity} skipping state in {inResource}");
        return null;
    }

    public class State : EntityDataState
    {
        [OnlineField(group = "inputs")]
        public ushort inputs;
        [OnlineFieldHalf(group = "inputs")]
        public float analogInputX;
        [OnlineFieldHalf(group = "inputs")]
        public float analogInputY;
        [OnlineField(group = "inputs")]
        internal CreatureController.SpecialInput specialInput; // todo todo todo
        [OnlineField(group = "ai")]
        internal WorldCoordinate destination;
        [OnlineFieldHalf(group = "ai")]
        internal float moveSpeed;

        public State() { }
        public State(StoryControllerData scd)
        {
            RainMeadow.RainMeadow.Trace(scd);

            var i = scd.input;
            inputs = (ushort)(
                  (i.x == 1 ? 1 << 0 : 0)
                | (i.x == -1 ? 1 << 1 : 0)
                | (i.y == 1 ? 1 << 2 : 0)
                | (i.y == -1 ? 1 << 3 : 0)
                | (i.downDiagonal == 1 ? 1 << 4 : 0)
                | (i.downDiagonal == -1 ? 1 << 5 : 0)
                | (i.pckp ? 1 << 6 : 0)
                | (i.jmp ? 1 << 7 : 0)
                | (i.thrw ? 1 << 8 : 0)
                | (i.mp ? 1 << 9 : 0));

            analogInputX = i.analogueDir.x;
            analogInputY = i.analogueDir.y;
            specialInput = scd.specialInput;
            destination = scd.destination;
            moveSpeed = scd.moveSpeed;
        }

        public override Type GetDataType()
        {
            return typeof(StoryControllerData);
        }

        public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
        {
            RainMeadow.RainMeadow.Trace(onlineEntity);
            var mcd = (StoryControllerData)entityData;

            Player.InputPackage i = default;
            if (((inputs >> 0) & 1) != 0) i.x = 1;
            if (((inputs >> 1) & 1) != 0) i.x = -1;
            if (((inputs >> 2) & 1) != 0) i.y = 1;
            if (((inputs >> 3) & 1) != 0) i.y = -1;
            if (((inputs >> 4) & 1) != 0) i.downDiagonal = 1;
            if (((inputs >> 5) & 1) != 0) i.downDiagonal = -1;
            if (((inputs >> 6) & 1) != 0) i.pckp = true;
            if (((inputs >> 7) & 1) != 0) i.jmp = true;
            if (((inputs >> 8) & 1) != 0) i.thrw = true;
            if (((inputs >> 9) & 1) != 0) i.mp = true;
            i.analogueDir.x = analogInputX;
            i.analogueDir.y = analogInputY;
            mcd.input = i;
            mcd.specialInput = specialInput;
            mcd.destination = destination;
            mcd.moveSpeed = moveSpeed;
        }
    }
}
*/