using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DataAccessLibrary;
using DataAccessLibrary.Models;
using RazorPagesUI.PublicLibrary;

namespace RazorPagesUI.Pages
{
    public class Dashboard_MoreModel : PageModel
    {


        [BindProperty (SupportsGet =true)]
        public string SiteName { get; set; }

        [BindProperty (SupportsGet = true)]
        public string UserName { get; set; }

        [BindProperty (SupportsGet = true)]
        public int UserId { get; set; }

        [BindProperty (SupportsGet = true)]
        public bool HasAccess { get; set; }

        public int SiteUserId { get; set; }

        [BindProperty (SupportsGet = true)]
        public string ItemId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int DisplayState { get; set; }

        [BindProperty]
        public List<User_Model> MonitoredUsers { get; set; }

        SQLCrud SQL = new SQLCrud(ConnectionString.GetConnectionString());

        [BindProperty]
        public bool UserHasItems { get; set; }

        [BindProperty]
        public List<string> AllSitesList { get; set; }

       

        public void OnGet()
        {
            Console.WriteLine(DateTime.Now + " - Start Get");


            if (HasAccess && !string.IsNullOrWhiteSpace(SQL.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            SiteName = "Dashboard";

            MonitoredUsers = SQL.GetMonitoredUsers();
            AllSitesList = SQL.GetAllSites();

            foreach (var site in AllSitesList)
            { Console.WriteLine(site); }
          

            
        }

        public int GetUserAllCount(int userId)
        {
            return SQL.GetAllCountbyUserFromSiteTable(userId);
            Console.WriteLine(DateTime.Now + " - Get All Count by User " + userId);
        }

        public int GetUserResolvedCount(int userId)
        {
            return SQL.GetResolvedCountByUserFromItemScopeResolved(userId);
            Console.WriteLine(DateTime.Now + " - Get Resolved Count by User " + userId);
        }

        public int GetUserOpenCount(int userId)
        {

            int allCount = this.GetUserAllCount(userId);
            int resolvedCount = this.GetUserResolvedCount(userId);

            if (resolvedCount > allCount)
            { resolvedCount = allCount; }

            Console.WriteLine(DateTime.Now + " - Get Open Count by User " + userId);

            return allCount - resolvedCount;
        }

        public int GetSiteAllCount(string site)
        {
            return SQL.GetAllCountbyUserFromSiteTable(SQL.GetMasterUserIdBySite(site));
        }

        public int GetSiteResolvedCount(string site)
        {
            return SQL.GetResolvedCountByUserFromItemScopeResolved(SQL.GetMasterUserIdBySite(site));
        }

        public string GetSiteName(string site)
        {
            return SQL.TranslateSiteToName(site);
        }

    }

    
}
