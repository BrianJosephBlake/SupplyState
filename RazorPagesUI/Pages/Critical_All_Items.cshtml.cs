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
    public class Critical_All_ItemsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public bool HasAccess { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ItemId { get; set; }

        [BindProperty]
        public List<ItemList_Model> ItemList { get; set; }

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

        [BindProperty]
        public int MasterSiteUserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsSmartSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchKey { get; set; }

        [BindProperty]
        public List<string> AllSitesList { get; set; }

        [BindProperty]
        public int AllCount { get; set; }

        [BindProperty]
        public int ResolvedCount { get; set; }

        [BindProperty]
        public int OpenCount { get; set; }

        [BindProperty]
        public bool HasHistory { get; set; }

        SQLCrud Sql = new SQLCrud(ConnectionString.GetConnectionString());

        public void OnGet()
        {
            if (HasAccess && !string.IsNullOrWhiteSpace(Sql.GetSiteByUser(UserId)))
            { UserHasItems = true; }


           

            AllSitesList = Sql.GetAllSites();

            //UserName = Sql.GetUserNameByUserId(UserId);


            if (string.IsNullOrWhiteSpace(SearchKey) || SearchKey == "")
            {
                ItemList = Sql.GetAllItemListModelFromBOIM();
            }
            else
            {
                ItemList = Sql.SmartSearch_BOIM(SearchKey);
            }


            HasHistory = Sql.HasNotesHistory(SearchKey);


        }

        public IActionResult OnPostSelectItem()
        {
            return RedirectToPage("/AllCriticalReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = 0, DetailDisplayState = DetailDisplayState, ViewState = 1, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId });
        }

        public IActionResult OnPostSearch()
        {
            return RedirectToPage("/Critical_All_Items", new { SiteName = SiteName, ItemId = ItemId, DisplayState = 0, DetailDisplayState = DetailDisplayState, ViewState = 1, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId, MasterSiteUserId = MasterSiteUserId, IsSmartSearch = true, SearchKey = SearchKey });
        }

        public string GetSiteName(string site)
        {
            return Sql.TranslateSiteToName(site);
        }

        public IActionResult OnPostDisplayAll()
        {
            return RedirectToPage("/AllCriticalReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

        public IActionResult OnPostDisplayOpen()
        {
            int allCount = Sql.GetAllCountbyUserFromSiteTable(MasterSiteUserId);
            int resolvedCount = Sql.GetResolvedCountByUserFromItemScopeResolved(MasterSiteUserId);

            if (allCount <= resolvedCount)
            {
                return RedirectToPage("/AllCriticalReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
            }

            return RedirectToPage("/AllCriticalReadout", new { SiteName = SiteName, DisplayState = 1, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

        public IActionResult OnPostDisplayResolved()
        {

            int resolvedCount = Sql.GetResolvedCountByUserFromItemScopeResolved(MasterSiteUserId);

            if (resolvedCount <= 0)
            {
                return RedirectToPage("/AllCriticalReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
            }

            return RedirectToPage("/AllCriticalReadout", new { SiteName = SiteName, DisplayState = 2, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
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



            if (Sql.ItemHasSubs(itemId))
            {
                int subCount = Sql.ItemSubCount(itemId);

                output = subCount + " SUB";

                if (subCount > 1)
                {
                    output += "S";
                }

            }

            return output;
        }

        public string IsActive(string itemId)
        {
            string output = "";

            if (Sql.IsItemActive(itemId))
            {
                int siteCount = Sql.ItemSiteCount(itemId);

                output += siteCount + " SITE";

                if (siteCount > 1)
                {
                    output += "S";
                }
            }

            return output;
        }
    }

}