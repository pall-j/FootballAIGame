﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballAIGameServer.SimulationEntities
{
    public class GameState
    {
        public FootballPlayer[] FootballPlayers { get; set; }

        public Ball Ball { get; set; }
    }
}