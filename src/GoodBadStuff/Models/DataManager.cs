﻿using GoodBadStuff.Models;
using GoodBadStuff.Models.Entities;
using GoodBadStuff.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoodBadStuff.Models
{
    public class DataManager
    {
        TrvlrContext _context;
        UserManager<IdentityUser> _userManager;

        public DataManager(TrvlrContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //  const string CON_STR = "Server=tcp:trvlr.database.windows.net,1433;Initial Catalog=TRVLRdb;Persist Security Info=False;User ID=trvlr;Password=Secret123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";
        //const string CON_STR = @"Data Source=trvlr.database.windows.net;Initial Catalog=TRVLRdb;Persist Security Info=True;User ID=trvlr;Password=Secret123";

        public int GetValuesFromAPIs(TravelInfoVM travelInfoVM, string json)
        {
            var travelInfo = new TravelInfo();
            travelInfo.FromAddress = travelInfoVM.FromAddress;
            travelInfo.ToAddress = travelInfoVM.ToAddress;
            travelInfo.Transport = travelInfoVM.Transport;

            JObject o = JObject.Parse(json);

            string tempDist = (string)o.SelectToken("emissions[1].routedDistance");
            travelInfo.Distance = Convert.ToSingle(tempDist);

            switch (travelInfo.Transport)
            {
                case "BICYCLE":
                    GetCo2(o, 0, travelInfo);
                    break;
                case "WALKING":
                    GetCo2(o, 1, travelInfo);
                    break;
                case "TRAIN":
                    GetCo2(o, 2, travelInfo);
                    break;
                case "BUS":
                    GetCo2(o, 3, travelInfo);
                    break;
                case "MOTORCYCLE":
                    GetCo2(o, 4, travelInfo);
                    break;
                case "DRIVING":
                    GetCo2(o, 7, travelInfo);
                    break;
            }
            return AddNewTravel(travelInfo);
        }

        internal async Task<string> GetUserInfoFromdb(string userName)
        {

            var user = await _userManager.FindByNameAsync(userName);
            if (user.Email == null)
                return user.Email = "";
            else
                return user.Email;


        }

        internal async Task SaveTravelToUser(int travelInfoId, string userName)
        {
            // kod för att stoppa in UserID or Name i 
            var userId = await GetIdFromUserName(userName);

            AddUserIdToTravelInfoDBTable(userId, travelInfoId);
        }

        private void AddUserIdToTravelInfoDBTable(string userId, int travelInfoId)
        {
            var travelInfo = _context.TravelInfo.Single(o => o.Id == travelInfoId);
            travelInfo.UserId = userId;
            _context.SaveChanges();
        }

        async Task<string> GetIdFromUserName(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            return user.Id;
        }

        private void GetCo2(JObject o, int i, TravelInfo travelInfo)
        {
            float Co2;
            var distance = (int)o.SelectToken($"emissions[{i}].routedDistance");
            if (i == 7 && distance > 2000000)
            {
                Co2 = Convert.ToSingle(distance / 5.556f);
                travelInfo.Co2 = Co2;
            }
            else
            {
                string tempCo2 = (string)o.SelectToken($"emissions[{i}].totalCo2");
                if (tempCo2.StartsWith("-"))
                    tempCo2 = tempCo2.Substring(1);
                Co2 = Convert.ToSingle(tempCo2);
                travelInfo.Co2 = Co2;
            }
        }

        public int AddNewTravel(TravelInfo travelInfo)
        {
            _context.TravelInfo.Add(travelInfo);
            _context.SaveChanges();
            return travelInfo.Id;

            #region Old ugly SQL solution
            //SqlConnection myConnection = new SqlConnection(CON_STR);
            //SqlCommand myCommand = new SqlCommand();

            ////myCommand.CommandText = $"insert into TravelInfo (Transport, Co2, Date, TreeCount, Distance, FromAddress, ToAddress) values ('{travelInfoDb.Transport}', {travelInfoDb.Co2}, {travelInfoDb.Created}, {travelInfoDb.TreeCount}, {travelInfoDb.Distance}, '{travelInfoDb.FromAddress}', '{travelInfoDb.ToAddress}')";
            //myCommand.CommandText = $"insert into TravelInfo (Transport, Co2, FromAddress, ToAddress, Distance) values ('{travelInfoDb.Transport}', {travelInfoDb.Co2},'{travelInfoDb.FromAddress}', '{travelInfoDb.ToAddress}', {travelInfoDb.Distance}); select @@identity";


            //myCommand.CommandType = System.Data.CommandType.Text;
            //myCommand.Connection = myConnection;

            //try
            //{
            //    myConnection.Open();
            //    //myCommand.ExecuteNonQuery();
            //    ret = Convert.ToInt32(myCommand.ExecuteScalar());
            //}
            //catch (Exception e)
            //{
            //    throw;
            //}
            //finally
            //{
            //    myConnection.Close();
            //}
            //return ret.Value;
            #endregion
        }

        public MyTravelsVM LoadTravels(string userId)
        {
            MyTravelsVM travelsToReturn = new MyTravelsVM();
            travelsToReturn.TravelInfo = _context.TravelInfo.Where(a => a.UserId == userId)
                .Select(c => new Travels { Transport = c.Transport, Co2 = c.Co2, Date = c.Date, Distance = c.Distance, FromAddress = c.FromAddress, ToAddress = c.ToAddress, UserId = c.UserId })
            .ToList();
            travelsToReturn.TravelsByBus = _context.TravelInfo.Where(a => a.UserId == userId && a.Transport == "BUS").Count();
            travelsToReturn.TravelsByWalking = _context.TravelInfo.Where(a => a.UserId == userId && a.Transport == "WALKING").Count();
            travelsToReturn.TravelsByBicycling += _context.TravelInfo.Where(a => a.UserId == userId && a.Transport == "BICYCLING").Count();
            travelsToReturn.TravelsByCar = _context.TravelInfo.Where(a => a.UserId == userId && a.Transport == "DRIVING").Count();
            travelsToReturn.TravelsByTrain = _context.TravelInfo.Where(a => a.UserId == userId && a.Transport == "TRAIN").Count();
            travelsToReturn.TravelsByMotorcycle = _context.TravelInfo.Where(a => a.UserId == userId && a.Transport == "MOTORCYCLE").Count();

            return travelsToReturn;
        }

    }
}
//