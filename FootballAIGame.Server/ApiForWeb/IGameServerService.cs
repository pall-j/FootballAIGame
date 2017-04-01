﻿using System.ServiceModel;

namespace FootballAIGame.Server.ApiForWeb
{
    /// <summary>
    /// Represents the WCF service contract for application web server.
    /// </summary>
    [ServiceContract]
    public interface IGameServerService
    {
        /// <summary>
        /// Add player with a specified name and AI to the queue for a random match.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="ai">The AI's name.</param>
        /// <returns>"ok" if the operation was successful; otherwise, an error message.</returns>
        [OperationContract]
        string WantsToPlay(string userName, string ai);

        /// <summary>
        /// Starts the game between specified players' AIs.
        /// </summary>
        /// <param name="userName1">The player1's name.</param>
        /// <param name="ai1">The player1's AI name.</param>
        /// <param name="userName2">The player2's name.</param>
        /// <param name="ai2">The player2's AI name.</param>
        /// <returns>"ok" if the operation was successful; otherwise, an error message.</returns>
        [OperationContract]
        string StartGame(string userName1, string ai1, string userName2, string ai2);

        /// <summary>
        /// Cancels the game match in which the specified player is.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        [OperationContract]
        void CancelMatch(string playerName);

        /// <summary>
        /// Player will leave a running tournament in which he currently is.
        /// If there is not such tournament, then it does nothing.
        /// </summary>
        /// <param name="playerName">The player's name.</param>
        [OperationContract]
        void LeaveRunningTournament(string playerName);

        /// <summary>
        /// Plans the tournament.
        /// </summary>
        /// <param name="tournamentId">The tournament's identifier.</param>
        [OperationContract]
        void PlanTournament(int tournamentId);

        /// <summary>
        /// Removes player from the random match queue.
        /// </summary>
        /// <param name="playerName">The player's name.</param>
        [OperationContract]
        void CancelLooking(string playerName);

        /// <summary>
        /// Gets the current simulation step of the match in which the specified player currently is.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        [OperationContract]
        int GetCurrentMatchStep(string playerName);
    }
}
