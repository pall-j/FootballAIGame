﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FootballAIGame.DbModel.Models;
using FootballAIGame.Web.Utilities;
using FootballAIGame.Web.ViewModels.Players;
using Microsoft.AspNet.Identity;

namespace FootballAIGame.Web.Controllers
{
    public class PlayersController : Controller
    {
        /// <summary>
        /// The application database context used for accessing database using entity framework.
        /// </summary>
        private ApplicationDbContext _context;

        /// <summary>
        /// Gets the current connected player.
        /// </summary>
        /// <value>
        /// The current player.
        /// </value>
        private Player CurrentPlayer
        {
            get
            {
                var userId = User.Identity.GetUserId();
                var user = _context.Users
                    .Include(u => u.Player)
                    .SingleOrDefault(u => u.Id == userId);
                return user?.Player;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayersController"/> class.
        /// </summary>
        public PlayersController()
        {
            _context = new ApplicationDbContext();
        }

        /// <summary>
        /// Releases unmanaged resources and optionally releases managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        /// <summary>
        /// Returns the players index view.
        /// </summary>
        /// <returns>The index matches view.</returns>
        public ActionResult Index()
        {
            if (User.IsInRole(RolesNames.MainAdmin))
            {
                var viewModel = _context.Players
                    .Include(p => p.User.Roles)
                    .ToList();

                return View("IndexForAdmin", viewModel);
            }
            else
            {
                var viewModel = _context.Players.ToList();
                return View(viewModel);
            }
        }

        /// <summary>
        /// Returns the player details view.
        /// </summary>
        /// <param name="id">The player name.</param>
        /// <returns>The details view if the player with the specified name exists;
        /// otherwise returns HttpNotFound response.</returns>
        public ActionResult Details(string id)
        {
            var player = _context.Players.SingleOrDefault(p => p.Name == id);
            var currentPlayer = CurrentPlayer;

            if (player == null)
                return HttpNotFound();

            var activeAIs = CurrentPlayer?.ActiveAis?.Split(';').ToList() ?? new List<string>();

            var orderedPlayers = _context.Players.OrderByDescending(p => p.Score).ToList();
            var rank = 1 + orderedPlayers.TakeWhile(orderedPlayer => orderedPlayer.UserId != player.UserId).Count();

            var lastMatches = _context.Matches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .Where(m => m.Player1.UserId == player.UserId || m.Player2.UserId == player.UserId)
                .OrderByDescending(m => m.Time)
                .Take(5).ToList();

            var lastTournaments = _context.Tournaments
                .Include(t => t.Players.Select(tp => tp.Player))
                .AsEnumerable() // to use comparison with player (only allowed in memory!)
                .Where(t => t.TournamentState == TournamentState.Finished && 
                            t.Players.Any(tp => tp.Player == player))
                .ToList();

            lastTournaments.Sort(new JoinedTournamentComparer());

            var viewModel = new PlayerDetailsViewModel()
            {
                Player = player,
                CurrentPlayer = currentPlayer,
                LastMatches = lastMatches,
                LastTournaments = lastTournaments,
                ActiveAIs = activeAIs,
                SelectedAi = currentPlayer?.SelectedAi,
                Rank = rank
            };

            return View("Details", viewModel);
        }
    }
}