﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FootballAIGame.MatchSimulation;
using FootballAIGame.MatchSimulation.Messages;
using FootballAIGame.MatchSimulation.Models;
using FootballAIGame.Server.Models;

namespace FootballAIGame.Server
{
    /// <summary>
    /// Provides functionality to manage match simulations. Implemented as singleton.
    /// Serves as the bridge between the server's service and the simulation library.
    /// </summary>
    public class SimulationManager
    {
        /// <summary>
        /// Gets or sets the list of currently running simulations.
        /// </summary>
        /// <value>
        /// The list of currently running simulations.
        /// </value>
        private List<MatchSimulator> RunningSimulations { get; set; } = new List<MatchSimulator>();

        /// <summary>
        /// Gets or sets the list of connections that belong to player that is currently looking
        /// for a new match.
        /// </summary>
        /// <value>
        /// The list of wants to play connections.
        /// </value>
        private List<ClientConnection> WantsToPlayConnections { get; set; } = new List<ClientConnection>();

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static SimulationManager Instance => _instance ?? (_instance = new SimulationManager());

        /// <summary>
        /// The singleton instance.
        /// </summary>
        private static SimulationManager _instance;

        /// <summary>
        /// Prevents a default instance of the <see cref="SimulationManager"/> class from being created.
        /// </summary>
        private SimulationManager()
        {
            Initialize();
        }

        /// <summary>
        /// Starts listening for a new AI connections asynchronously.
        /// </summary>
        /// <returns>The task that represents the asynchronous operation of accepting connections.</returns>
        public async Task StartsAcceptingConnectionsAsync()
        {
            ConnectionManager.Instance.IsVerbose = true;
            var listening = ConnectionManager.Instance.StartListeningAsync();
            Console.WriteLine("Listening has started.");

            await listening;
        }

        /// <summary>
        /// Cancels the game match in which a specified player is.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        public void CancelMatch(string playerName)
        {
            lock (RunningSimulations)
            {
                foreach (var runningSimulation in RunningSimulations)
                {
                    if (runningSimulation.AI1Communicator.PlayerName == playerName)
                        runningSimulation.AI1CancelRequested = true;
                    if (runningSimulation.AI2Communicator.PlayerName == playerName)
                        runningSimulation.AI2CancelRequested = true;
                }
            }
        }

        /// <summary>
        /// Starts the match between specified AIs.
        /// </summary>
        /// <param name="userName1">The player1 name.</param>
        /// <param name="ai1">The player1 AI name.</param>
        /// <param name="userName2">The player2 name.</param>
        /// <param name="ai2">The player2 AI name.</param>
        /// <param name="simulationTask">The task that represents the asynchronous simulate operation.
        /// The value of the task result holds the <see cref="MatchSimulator"/> that simulated the match.</param>
        /// <param name="tournamentId">The tournament Id. (optional)</param>
        /// <returns>
        /// "ok" if operation was successful; otherwise, an error message.
        /// </returns>
        public string StartMatch(string userName1, string ai1, string userName2, string ai2, out Task<MatchSimulator> simulationTask, int? tournamentId = null)
        {
            var manager = ConnectionManager.Instance;
            simulationTask = null;

            lock (manager.ActiveConnections)
            {
                var connection1 = manager.ActiveConnections
                    .FirstOrDefault(c => c.PlayerName == userName1 && c.AiName == ai1);
                var connection2 = manager.ActiveConnections
                    .FirstOrDefault(c => c.PlayerName == userName2 && c.AiName == ai2);

                if (connection1 == null || connection2 == null)
                    return "AI is no longer active.";

                if (connection1.IsInMatch || connection2.IsInMatch)
                    return "Player is already in a match";

                if (userName1 == userName2)
                    Console.WriteLine("User cannot challenge himself.");

                connection1.IsInMatch = true;
                connection2.IsInMatch = true;

                var matchSimulator = new MatchSimulator(connection1, connection2) { IsVerbose = true };

                lock (RunningSimulations)
                {
                    RunningSimulations.Add(matchSimulator);
                }

                var startTime = DateTime.Now;

                simulationTask = Task.Run(async () =>
                {
                    await matchSimulator.SimulateMatchAsync();
                    await OnSimulationEndAsync(startTime, matchSimulator, tournamentId);
                    return matchSimulator;
                });

                return "ok";
            }
        }

        /// <summary>
        /// Starts the game between the specified AIs.
        /// </summary>
        /// <param name="userName1">The player1 name.</param>
        /// <param name="ai1">The player1 AI name.</param>
        /// <param name="userName2">The player2 name.</param>
        /// <param name="ai2">The player2 AI name.</param>
        /// <returns>
        /// "ok" if operation was successful; otherwise, an error message.
        /// </returns>
        public string StartMatch(string userName1, string ai1, string userName2, string ai2)
        {
            Task<MatchSimulator> task;
            return StartMatch(userName1, ai1, userName2, ai2, out task);
        }

        /// <summary>
        /// Adds a new wants to play connection.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <param name="aiName">The AI name.</param>
        /// "ok" if operation was successful; otherwise, an error message.
        public string AddToWantsToPlayConnections(string userName, string aiName)
        {
            ClientConnection connection;

            lock (ConnectionManager.Instance.ActiveConnections)
            {
                connection = ConnectionManager.Instance.ActiveConnections
                    .FirstOrDefault(c => c.PlayerName == userName && c.AiName == aiName);
            }

            if (connection == null)
                return "AI is no longer active.";

            ClientConnection otherPlayerConnection;

            lock (WantsToPlayConnections)
            {
                if (WantsToPlayConnections.Count == 0)
                {
                    WantsToPlayConnections.Add(connection);
                    return "ok";
                }

                otherPlayerConnection = WantsToPlayConnections[0];
                WantsToPlayConnections.Remove(otherPlayerConnection);
            }

            // start match
            using (var context = new ApplicationDbContext())
            {
                var player1 = context.Players.FirstOrDefault(p => p.Name == connection.PlayerName);
                var player2 = context.Players.FirstOrDefault(p => p.Name == otherPlayerConnection.PlayerName);

                if (player1 == null)
                    return $"{connection.PlayerName} is not valid name";
                if (player2 == null)
                    return $"{connection.PlayerName} is not valid name";

                if (otherPlayerConnection == connection)
                    return "Player is already looking for opponent.";

                player1.PlayerState = PlayerState.PlayingMatch;
                player2.PlayerState = PlayerState.PlayingMatch;

                context.SaveChanges();
            }

            StartMatch(connection.PlayerName, connection.AiName,
                otherPlayerConnection.PlayerName, otherPlayerConnection.AiName);

            return "ok";
        }

        /// <summary>
        /// Remove the wants to play connection of the specified user.
        /// </summary>
        /// <param name="userName">The user name.</param>
        public void RemoveFromWantsToPlayConnections(string userName)
        {
            lock (WantsToPlayConnections)
            {
                WantsToPlayConnections.RemoveAll(p => p.PlayerName == userName);
            }
        }

        /// <summary>
        /// Gets the match step of the match in which the specified user's AI is.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <returns>The match step of the match in which the specified user's AI is or 1500 if the user
        /// doesn't have any AI in the match.</returns>
        public int GetMatchStep(string userName)
        {
            lock (RunningSimulations)
            {
                var match = RunningSimulations
                    .FirstOrDefault(m => m.AI1Communicator.PlayerName == userName ||
                                         m.AI2Communicator.PlayerName == userName);
                return match?.CurrentStep ?? 1500;
            }
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        private void Initialize()
        {
            ResetPlayers();

            SetSimulationHandlers();
        }

        /// <summary>
        /// Resets players.
        /// Sets all players to idle state.
        /// </summary>
        private static void ResetPlayers()
        {
            // reset player states on the start
            using (var context = new ApplicationDbContext())
            {
                var players = context.Players.ToList();
                foreach (var player in players)
                {
                    player.PlayerState = PlayerState.Idle;
                    player.ActiveAis = null;
                    player.SelectedAi = null;
                }
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Sets the simulation handlers.
        /// </summary>
        private void SetSimulationHandlers()
        {
            ConnectionManager.Instance.AuthenticationHandler = AuthenticateUserAsync;
            ConnectionManager.Instance.ActiveClientDisconnectedHandler = ProcessClientDisconnectionAsync;
        }

        /// <summary>
        /// Handles the simulation end asynchronously. 
        /// </summary>
        /// <param name="startTime">The start time of the simulation.</param>
        /// <param name="simulator">The match simulator.</param>
        /// <param name="tournamentID">If specified then it holds an ID of a tournament to which the simulated match belongs.</param>
        /// <returns>The task that represents the asynchronous handle operation.</returns>
        private async Task OnSimulationEndAsync(DateTime startTime, MatchSimulator simulator, int? tournamentID)
        {
            var matchInfo = simulator.MatchInfo;

            var player1AiConnection = simulator.AI1Communicator as ClientConnection;
            var player2AiConnection = simulator.AI2Communicator as ClientConnection;
            Debug.Assert(player1AiConnection != null, "player1AiConnection != null");
            Debug.Assert(player2AiConnection != null, "player2AiConnection != null");

            using (var context = new ApplicationDbContext())
            {
                var players = await context.Players.Where(
                        p => p.Name == player1AiConnection.PlayerName || p.Name == player2AiConnection.PlayerName).ToListAsync();

                var player1 = players.First(p => p.Name == player1AiConnection.PlayerName);
                var player2 = players.First(p => p.Name == player2AiConnection.PlayerName);

                player1.PlayerState = player1.PlayerState == PlayerState.PlayingTournamentPlaying ?
                    PlayerState.PlayingTournamentWaiting : PlayerState.Idle;

                player2.PlayerState = player2.PlayerState == PlayerState.PlayingTournamentPlaying ?
                    PlayerState.PlayingTournamentWaiting : PlayerState.Idle;

                var match = new Match(matchInfo, player1AiConnection.PlayerName, player2AiConnection.PlayerName)
                {
                    Time = startTime,
                    Player1 = player1,
                    Player2 = player2,
                    Player1Ai = player1AiConnection.AiName,
                    Player2Ai = player2AiConnection.AiName,
                };

                if (tournamentID != null)
                {
                    var tournament = context.Tournaments.FirstOrDefault(t => t.Id == tournamentID);
                    if (tournament != null)
                        match.Tournament = tournament;
                }

                switch (matchInfo.Winner)
                {
                    case Team.FirstPlayer:
                        player1.WonGames++;
                        break;
                    case Team.SecondPlayer:
                        player2.WonGames++;
                        break;
                }

                context.Matches.Add(match);

                await context.SaveChangesAsync();

                RunningSimulations.Remove(simulator);

                player1AiConnection.IsInMatch = false;
                player2AiConnection.IsInMatch = false;
            }

        }

        /// <summary>
        /// Processes a client's disconnection asynchronously.
        /// </summary>
        /// <param name="connection">The client's connection.</param>
        /// <returns>The task that represents the asynchronous process operation.</returns>
        private async Task ProcessClientDisconnectionAsync(ClientConnection connection)
        {
            Debug.Assert(connection != null, "connection != null");

            using (var context = new ApplicationDbContext())
            {
                var player =
                    await context.Players.SingleOrDefaultAsync(p => p.Name == connection.PlayerName);

                if (player != null)
                {
                    var newAis = player.ActiveAis.Split(';').Where(s => s != connection.AiName);
                    player.ActiveAis = string.Join(";", newAis);

                    lock (WantsToPlayConnections)
                    {
                        WantsToPlayConnections.Remove(connection);
                    }

                    if (player.SelectedAi == connection.AiName)
                    {
                        player.SelectedAi = "";
                        player.PlayerState = PlayerState.Idle; // todo error message
                    }

                    if (player.ActiveAis == "")
                        player.ActiveAis = null;
                    connection.Dispose();
                }
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Authenticates the user asynchronously.
        /// </summary>
        /// <param name="message">The login message.</param>
        /// <returns>The task that represents the asynchronous authenticate operation.
        /// The value of the task's result is null if the client has successfully authenticated;
        /// otherwise, an error message.</returns>
        private static async Task<string> AuthenticateUserAsync(LoginMessage message)
        {
            using (var context = new ApplicationDbContext())
            {
                var player = await context.Players
                    .Include(u => u.User)
                    .FirstOrDefaultAsync(p => p.Name == message.PlayerName);

                if (player == null)
                {
                    return "Invalid player name.";
                }

                if (message.AccessKey != player.AccessKey)
                {
                    return "Invalid access key.";
                }

                if (player.ActiveAis == null)
                    player.ActiveAis = message.AIName;
                else
                {
                    if (player.ActiveAis.Split(';').Contains(message.AIName))
                    {
                        return "AI name is already being used.";
                    }

                    player.ActiveAis += ";" + message.AIName;
                }

                await context.SaveChangesAsync();
            }

            return null;
        }
    }
}
