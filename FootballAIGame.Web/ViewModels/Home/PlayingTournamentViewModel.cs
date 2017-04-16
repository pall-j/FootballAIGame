﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FootballAIGame.DbModel.Models;

namespace FootballAIGame.Web.ViewModels.Home
{
    public class PlayingTournamentViewModel
    {
        public Tournament Tournament { get; set; }

        public Player CurrentPlayer { get; set; }
    }
}