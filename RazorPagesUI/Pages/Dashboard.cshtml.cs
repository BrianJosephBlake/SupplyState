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
    public class DashboardModel : PageModel
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

        [BindProperty]
        public List<string> AllMBOsList { get; set; }

        public void OnGet()
        {
            

            if (HasAccess && !string.IsNullOrWhiteSpace(SQL.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            SiteName = "Dashboard";

            MonitoredUsers = SQL.GetMonitoredUsers();
            AllSitesList = SQL.GetAllSites();
            AllMBOsList = SQL.GetAllMBOs();

            //foreach (var site in AllSitesList)
            //{ Console.WriteLine(site); }
          

            
        }

        public IActionResult OnPostDrillDown()
        {

            return RedirectToPage("/Dashboard_More", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, HasAccess = HasAccess, UserId = UserId });
        }

        public IActionResult OnPostGoToSite()
        {
            Console.WriteLine("Dashboard Goto Site: " + SiteName);
           

            return RedirectToPage("/All_Site_Items", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, HasAccess = HasAccess, UserId = UserId });
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
            int output = 0;

            output = SQL.GetAllCountbyUserFromSiteTable(SQL.GetMasterUserIdBySite(site));

            SQL.OutputToLog(site + " - AllCountBySite: " + output);

            return SQL.GetAllCountbyUserFromSiteTable(SQL.GetMasterUserIdBySite(site));
        }

        public int GetAllCountByScope(string site, string scope)
        {
            int output = 0;

            output = SQL.GetAllCountbyScopeFromSiteTable(site, scope);

            SQL.OutputToLog(site + ", " + scope + " - AllCountByScope: " + output);

            return output;
        }

        public int GetResolvedCountByScope(string site, string scope)
        {
            int output = SQL.GetResolvedCountByScopeFromItemScopeResolved(site, scope);

            SQL.OutputToLog(site + ", " + scope + " - ResolvedCountByScope: " + output);

            return output;
        }

        public int GetSiteResolvedCount(string site)
        {
            int output = SQL.GetResolvedCountByUserFromItemScopeResolved(SQL.GetMasterUserIdBySite(site));

            SQL.OutputToLog(site + " - ResolvedCountBySite: " + output);

            return output;
        }

        public string GetSiteName(string site)
        {
            return SQL.TranslateSiteToName(site);
        }

        public List<string> GetScopesBySite(string site)
        {
            List<string> output = new List<string>();

            output = SQL.GetAllScopesBySite(site);

            return output;
        }
        
    
    }

    
}
