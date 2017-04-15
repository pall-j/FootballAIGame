﻿using System;
using System.Collections.Generic;
using System.Linq;
using FootballAIGame.MatchSimulation.Models;

namespace FootballAIGame.Server.Models
{
    public class Match
    {
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the time when the match simulation started.
        /// </summary>
        /// <value>
        /// The time when the match simulation started..
        /// </value>
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the player1.
        /// </summary>
        /// <value>
        /// The player1.
        /// </value>
        public Player Player1 { get; set; }

        /// <summary>
        /// Gets or sets the player2.
        /// </summary>
        /// <value>
        /// The player2.
        /// </value>
        public Player Player2 { get; set; }

        /// <summary>
        /// Gets or sets the player1 AI name.
        /// </summary>
        /// <value>
        /// The player1 AI name.
        /// </value>
        public string Player1Ai { get; set; }

        /// <summary>
        /// Gets or sets the player2 AI name.
        /// </summary>
        /// <value>
        /// The player2 AI name.
        /// </value>
        public string Player2Ai { get; set; }

        /// <summary>
        /// Gets or sets the final match score. <para />
        /// Format: "NumberOfUser1Goals:NumberOfUser2Goals".
        /// </summary>
        /// <value>
        /// The score.
        /// </value>
        public string Score { get; set; }

        /// <summary>
        /// Gets or sets the goals that were scored in the match. <para/>
        /// Format: "GoalTime;UserName;PlayerK|...", where K is number of player from userName team who scored
        /// and '|' is the separator between goals.
        /// </summary>
        /// <value>
        /// The goals.
        /// </value>
        public string Goals { get; set; }

        /// <summary>
        /// Gets or sets the number of player1 team shots.
        /// </summary>
        /// <value>
        /// The number of player1 team shots.
        /// </value>
        public int Shots1 { get; set; }

        /// <summary>
        /// Gets or sets the number of player2 team shots.
        /// </summary>
        /// <value>
        /// The number of player2 team shots.
        /// </value>
        public int Shots2 { get; set; }

        /// <summary>
        /// Gets or sets the number of player1 team shots on target.
        /// </summary>
        /// <value>
        /// The number of player1 team shots on target.
        /// </value>
        public int ShotsOnTarget1 { get; set; }

        /// <summary>
        /// Gets or sets the number of player2 team shots on target.
        /// </summary>
        /// <value>
        /// The number of player2 team shots on target.
        /// </value>
        public int ShotsOnTarget2 { get; set; }

        /// <summary>
        /// Gets or sets the player1 match error log. <para />
        /// Format: "Time - ErrorMessage;...", ';' is errors separator.
        /// </summary>
        /// <value>
        /// The player1 match error log.
        /// </value>
        public string Player1ErrorLog { get; set; }

        /// <summary>
        /// Gets or sets the player2 match error log. <para />
        /// Format: "Time - ErrorMessage;...", ';' is errors separator.
        /// </summary>
        /// <value>
        /// The player1 match error log.
        /// </value>
        public string Player2ErrorLog { get; set; }

        /// <summary>
        /// Gets or sets the player1's average time that the simulator waited for the team's action to be received.
        /// </summary>
        /// <value>
        /// The player1's average time that the simulator waited for the team's action to be received.
        /// </value>
        public int Player1AverageActionLatency { get; set; }

        /// <summary>
        /// Gets or sets the player2's average time that the simulator waited for the team's action to be received.
        /// </summary>
        /// <value>
        /// The player2's average time that the simulator waited for the team's action to be received.
        /// </value>
        public int Player2AverageActionLatency { get; set; }

        /// <summary>
        /// Gets or sets the match data. 
        /// Match data is binary representation of game states from all simulation steps. <para />
        /// For each step there should be a ball position and 22 players positions. <para />
        /// Everything is encoded as float. So each step takes 23*2 = 46 floats = 184 Bytes
        /// </summary>
        /// <value>
        /// If set correctly then there should be the match float data in the following order: 
        /// step0BallX, step0BallY, step0Player0X, step0Player0Y, ...step0Player22Y, step1BallX ... 
        /// converted to bytes.
        /// </value>
        public byte[] MatchData { get; set; }

        /// <summary>
        /// Returns 1 if Player1 won, 2 if Player2 won, otherwise (draw) returns 0.
        /// </summary>
        /// <value>1 if Player1 won, 2 if Player2 won, otherwise (draw) returns 0.</value>
        public int Winner { get; set; }

        /// <summary>
        /// Gets or sets the tournament identifier to which this match belongs.
        /// If the match is not part of a tournament it is equal to null.
        /// </summary>
        public int? TournamentId { get; set; }

        /// <summary>
        /// Gets or sets the tournament to which this match belongs.
        /// If the match is not part of a tournament it is equal to null.
        /// </summary>
        public Tournament Tournament { get; set; }

        public Match() { }

        public Match(MatchInfo matchInfo, string player1Name, string player2Name)
        {

            Shots1 = matchInfo.Team1Statistics.Shots;
            Shots2 = matchInfo.Team2Statistics.Shots;
            ShotsOnTarget1 = matchInfo.Team1Statistics.ShotsOnTarget;
            ShotsOnTarget2 = matchInfo.Team2Statistics.ShotsOnTarget;
            Player1AverageActionLatency = matchInfo.Team1Statistics.AverageActionLatency;
            Player2AverageActionLatency = matchInfo.Team2Statistics.AverageActionLatency;
            Score = $"{matchInfo.Team1Statistics.Goals}:{matchInfo.Team2Statistics.Goals}";
            Winner = matchInfo.Winner == Team.FirstPlayer ? 1 : matchInfo.Winner == Team.SecondPlayer ? 2 : 0;

            var goalsEnumerable = from goal in matchInfo.Goals
                                  let userName = goal.TeamThatScored == Team.FirstPlayer ? player1Name : player2Name
                                  select $"{goal.ScoreTime};{userName};Player{goal.ScorerNumber}";
            Goals = string.Join("|", goalsEnumerable);

            SetErrorsLogs(matchInfo.Errors);

            // convert match data to byte array
            var matchByteData = new byte[matchInfo.MatchData.Count * 4];
            Buffer.BlockCopy(matchInfo.MatchData.ToArray(), 0, matchByteData, 0, matchInfo.MatchData.Count * 4);
            MatchData = matchByteData;
        }

        private void SetErrorsLogs(IEnumerable<SimulationError> errors)
        {
            var player1Errors = new List<string>();
            var player2Errors = new List<string>();

            foreach (var error in errors)
            {
                string errorMessage;

                switch (error.Reason)
                {
                    case SimulationErrorReason.TooHighSpeed:
                        errorMessage = $"{error.Time} - Player{error.AffectedPlayerNumber} has too high speed.";
                        break;
                    case SimulationErrorReason.TooHighAcceleration:
                        errorMessage = $"{error.Time} - Player{error.AffectedPlayerNumber} has too high acceleration.";
                        break;
                    case SimulationErrorReason.TooStrongKick:
                        errorMessage = $"{error.Time} - Player{error.AffectedPlayerNumber} has made too strong kick.";
                        break;
                    case SimulationErrorReason.InvalidMovementVector:
                        errorMessage = $"{error.Time} - Player{error.AffectedPlayerNumber} has invalid movement vector set.";
                        break;
                    case SimulationErrorReason.InvalidKickVector:
                        errorMessage = $"{error.Time} - Player{error.AffectedPlayerNumber} has invalid kick vector set.";
                        break;
                    case SimulationErrorReason.Disconnection:
                        errorMessage = $"{error.Time} - Player has disconnected.";
                        break;
                    case SimulationErrorReason.Cancellation:
                        errorMessage = $"{error.Time} - Player has left the match.";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (error.Team == Team.FirstPlayer)
                    player1Errors.Add(errorMessage);
                else
                    player2Errors.Add(errorMessage);
            }

            Player1ErrorLog = string.Join(";", player1Errors);
            Player2ErrorLog = string.Join(";", player2Errors);
        }
    }
}