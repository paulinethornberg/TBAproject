﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoodBadStuff.Models.ViewModels
{
    public class UserMyAccountVM
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        ////make enum??
        //public string TimeZone { get; set; }
    }
}