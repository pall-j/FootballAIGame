﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FootballAIGameServer.CustomDataTypes;

namespace FootballAIGameServer.SimulationEntities
{
    /// <summary>
    /// Represents football player action that is received from the AI application.
    /// </summary>
    public class PlayerAction
    {
        /// <summary>
        /// Gets or sets the desired movement vector of the player.
        /// <see cref="MatchSimulator"/> will 
        /// </summary>
        /// <value>
        /// The movement vector.
        /// </value>
        public Vector Movement { get; set; }

        /// <summary>
        /// Gets or sets the desired kick vector of the player.
        /// </summary>
        /// <value>
        /// The kick.
        /// </value>
        public Vector Kick { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerAction"/> class.
        /// </summary>
        public PlayerAction()
        {
            Movement = new Vector();
            Kick = new Vector();
        }
    }
}
