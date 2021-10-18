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
    public class All_Items_Item_LookupModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public bool HasAccess { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ItemId { get; set; }

        [BindProperty]
        public List<IC211_Model> ItemList { get; set; }

        public string Site { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SiteName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string DisplayState { get; set; }

        public IC211_Model IC211 { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ViewState { get; set; }

        [BindProperty(SupportsGet = true)]
        public string DetailDisplayState { get; set; }

        [BindProperty(SupportsGet = true)]
        public int UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int FromMyItems { get; set; }

        [BindProperty]
        public bool UserHasItems { get; set; }

        [BindProperty]
        public string UserName { get; set; }

        [BindProperty (SupportsGet = true)]
        public bool IsSmartSearch { get; set; }

        [BindProperty (SupportsGet = true)]
        public string SearchKey { get; set; }

        [BindProperty]
        public bool HasHistory { get; set; }

        [BindProperty]
        public List<string> AllSitesList { get; set; }

        [BindProperty]
        public int AllCount { get; set; }

        [BindProperty]
        public int ResolvedCount { get; set; }

        [BindProperty]
        public int OpenCount { get; set; }

        SQLCrud Sql = new SQLCrud(ConnectionString.GetConnectionString());

        public void OnGet()
        {
            Console.WriteLine("Item Lookup: " + SearchKey);

            if (HasAccess && !string.IsNullOrWhiteSpace(Sql.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            UserName = Sql.GetUserNameByUserId(UserId);

            Site = Sql.GetSiteByUser(UserId);

            AllSitesList = Sql.GetAllSites();

            HasHistory = Sql.HasNotesHistory(SearchKey);

            

            if(string.IsNullOrWhiteSpace(SearchKey) || SearchKey == "")
            {
                ItemList = new List<IC211_Model>();
                ItemList.Add(Sql.GetNullIC211());    
            }
            else
            {
                ItemList = Sql.SmartSearch_AllItems(SearchKey);
            }

        }

        public IActionResult OnPostSelectItem()
        {
            return RedirectToPage("/Item_Lookup_Readout", new { SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = 0, DetailDisplayState = DetailDisplayState, ViewState = 1, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId }); ;
        }

        public IActionResult OnPostSearch()
        {
            return RedirectToPage("/All_Items_Item_Lookup", new { SiteName = SiteName, ItemId = ItemId, DisplayState = 0, DetailDisplayState = DetailDisplayState, ViewState = 1, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId, IsSmartSearch = true, SearchKey = SearchKey });
        }

        public string GetSiteName(string site)
        {
            return Sql.TranslateSiteToName(site);
        }

        public string HasNotes(string itemID)
        {
            string output = "";

            if (Sql.ItemHasNotes(itemID))
            {
                int noteCount = Sql.ItemNoteCount(itemID);

                output = noteCount + " NOTE";

                if (noteCount > 1)
                {
                    output += "S";
                }

            }

            return output;
        }

        public string IsCriticalStatus(string itemId)
        {
            if (Sql.IsItemCritical(itemId))
            {
                return "CRITICAL";
            }
            else
            {
                return "";
            }
        }

        public string HasSubs(string itemId)
        {
            string output = "";

            

                if(Sql.ItemHasSubs(itemId))
            {
                int subCount = Sql.ItemSubCount(itemId);

                output = subCount + " SUB";

                if(subCount > 1)
                {
                    output += "S";
                }
                
            }

            return output;
        }

        public string IsActive(string itemId)
        {
            string output = "";

            if(Sql.IsItemActive(itemId))
            {
                int siteCount = Sql.ItemSiteCount(itemId);

                output += siteCount + " SITE";

                if(siteCount > 1)
                {
                    output += "S";
                }
            }

            return output;
        }

    }

}