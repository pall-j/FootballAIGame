﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace FootballAIGameServer.ApiForWeb
{
    /// <summary>
    /// WCF service contract for application web server.
    /// </summary>
    [ServiceContract]
    public interface IGameServerService
    {
        /// <summary>
        /// Add player with a given name and AI to the queue for random match.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="ai">The AI name.</param>
        /// <returns>"ok" if operation was successful; otherwise, error message</returns>
        [OperationContract]
        string WantsToPlay(string userName, string ai);

        /// <summary>
        /// Starts the game between the given players AIs.
        /// </summary>
        /// <param name="userName1">The player1 name.</param>
        /// <param name="ai1">The player1 AI name.</param>
        /// <param name="userName2">The player2 name.</param>
        /// <param name="ai2">The player2 AI name.</param>
        /// <returns>"ok" if operation was successful; otherwise, error message</returns>
        [OperationContract]
        string StartGame(string userName1, string ai1, string userName2, string ai2);

        /// <summary>
        /// Cancels the game match in which a given player is.
        /// </summary>
        /// <param name="playerName">Name of the player.</param>
        [OperationContract]
        void CancelMatch(string playerName);

        /// <summary>
        /// Removes player from the random match queue.
        /// </summary>
        /// <param name="playerName">The player name.</param>
        [OperationContract]
        void CancelLooking(string playerName);
    }
}
