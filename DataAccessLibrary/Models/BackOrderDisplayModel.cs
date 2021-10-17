using System;
using DataAccessLibrary;
using DataAccessLibrary.Models;
using RazorPagesUI.PublicLibrary;

namespace RazorPagesUI.DataModels

{
    public class BackOrderDisplayModel
    {
        SQLCrud Sql = new SQLCrud(ConnectionString.GetConnectionString());

        IC211_Model Ic211 = new IC211_Model();
        Valuelink_Model Valuelink = new Valuelink_Model();
        Notes_Model Notes = new Notes_Model();

        public BackOrderDisplayModel()
        {
            //Ic211 = Sql.Retrieve_IC211();
            //Valuelink = Sql.Retrieve_Valuelink();
            //Notes = Sql.Retrieve_Notes();

        }

        public BackOrderDisplayModel(string item)
        {
            //Ic211 = Sql.Retrieve_IC211(item);
            //Valuelink = Sql.Retrieve_Valuelink(item);
            //Notes = Sql.Retrieve_Notes(item);
        }

        
        public string LawsonNumber { get; set; }

        public string Description { get; set; }

        public string MfrNumber { get; set; }

        public string StockoutDate { get; set; }

        public string ReleaseDate { get; set; }

        public string GapDays { get; set; }

        public string ReasonCode { get; set; }

        public string StockStatus { get; set; }

        public string Source { get; set; }

       





    }

}