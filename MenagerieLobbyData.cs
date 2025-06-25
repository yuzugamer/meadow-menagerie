using System;
using System.Collections.Generic;
using System.Linq;
using RainMeadow;

namespace StoryMenagerie;

public class MenagerieLobbyData : OnlineResource.ResourceData
{
    public MenagerieLobbyData() { }

    public override ResourceDataState MakeState(OnlineResource resource) => new State(this, resource);

    public class State : ResourceDataState
    {
        [OnlineField]
        public int foodPoints;
        [OnlineField]
        public int quarterFoodPoints;
        public State() { }
        public State(MenagerieLobbyData menagerieLobbyData, OnlineResource onlineResource)
        {
            MenagerieGameMode menagerie = (onlineResource as Lobby).gameMode as MenagerieGameMode;
            foodPoints = menagerie.foodPoints;
            quarterFoodPoints = menagerie.quarterFoodPoints;
        }

        public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
        {
            var lobby = (resource as Lobby);
            (lobby.gameMode as MenagerieGameMode).foodPoints = foodPoints;
            (lobby.gameMode as MenagerieGameMode).quarterFoodPoints = quarterFoodPoints;

        }

        public override Type GetDataType() => typeof(MenagerieLobbyData);
    }
}