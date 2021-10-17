using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesUI.DataModels;
using RazorPagesUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using DataAccessLibrary.Models;
using DataAccessLibrary;

namespace RazorPagesUI.Pages
{
    public class BackorderManagerModel : PageModel
    {
        [BindProperty (SupportsGet = true)]
        public string SiteName { get; set; }

        [BindProperty]
        public BackOrderDisplayModel BackOrderDisplay { get; set; }

        SQLCrud SQL = new SQLCrud(GetConnectionString());

        public void OnGet()
        {
            if (string.IsNullOrWhiteSpace(SiteName))
            { SiteName = "Undefined."; }
        }

        public IActionResult OnPost()
        {

            IC211_Model IC211 = new IC211_Model
            {
                Item_Number = BackOrderDisplay.LawsonNumber,
                PIV_VEN_ITEM = BackOrderDisplay.MfrNumber,
                Description = BackOrderDisplay.Description,
                Location_Code = "FFMCS",
                Company = "2200",
                ITL_ACTIVE_STATUS_XLT = "ACTIVE"
            };

            SQL.Add_IC211(IC211);
            

            return RedirectToPage("/Index");
        }

        private static string GetConnectionString(string connectionStringName = "Default")
        {
            string output = "";

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            output = config.GetConnectionString(connectionStringName);


            return output;
        }
    }
}
