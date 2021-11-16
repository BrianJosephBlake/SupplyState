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
    public class SiteViewAll_ItemsModel : PageModel
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
        public int DisplayState { get; set; }

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

        [BindProperty]
        public string Scope { get; set; }

        SQLCrud Sql = new SQLCrud(ConnectionString.GetConnectionString());

        public void OnGet()
        {


            if (HasAccess && !string.IsNullOrWhiteSpace(Sql.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            UserName = Sql.GetUserNameByUserId(UserId);

            Site = Sql.GetSiteByUser(UserId);

            Scope = Sql.GetUserScope(UserId);

            SiteName = Sql.TranslateSiteToName(Site);

            AllSitesList = Sql.GetAllSites();

            HasHistory = Sql.HasNotesHistory(SearchKey);

            if(DisplayState == 1)
            {
                SearchKey = "Open";
            }
            else if(DisplayState == 2)
            {
                SearchKey = "Resolved";
            }

            if(SearchKey == "" || String.IsNullOrEmpty(SearchKey))
            {
                IsSmartSearch = false;
            }

            if(IsSmartSearch)
            {
                ItemList = Sql.SmartSearch_SiteTable(SearchKey, UserId);
            }
            else if(SearchKey == "Open")
            {
                ItemList = Sql.AllOpenItemsByUserId(UserId);
            }
            else if(SearchKey == "Resolved")
            {
                ItemList = Sql.AllResolvedItemsByUserId(UserId);
            }
            else
            {
                ItemList = Sql.GetAllItemsByUserFromSiteTable(UserId);
            }

            AllCount = Sql.GetAllCountbyUserFromSiteTable(UserId);
            ResolvedCount = Sql.GetResolvedCountByUserFromItemScopeResolved(UserId);
            OpenCount = AllCount - ResolvedCount;
            
        }

        public IActionResult OnPostSelectItem()
        {
            return RedirectToPage("/SiteViewReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId });
        }

        public IActionResult OnPostSearch()
        {
            return RedirectToPage("/SiteViewAll_Items", new { SiteName = SiteName, ItemId = ItemId, DisplayState = 0, DetailDisplayState = DetailDisplayState, ViewState = 1, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId, IsSmartSearch = true, SearchKey = SearchKey });
        }

        public IActionResult OnPostViewNotesHistory()
        {
            return RedirectToPage("/Notes_History", new { SiteName = SiteName, ItemId = SearchKey, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

        public string GetSiteName(string site)
        {
            return Sql.TranslateSiteToName(site);
        }

        public IActionResult OnPostDisplayAll()
        {
            return RedirectToPage("/SiteViewAll_Items", new { SiteName = SiteName, ItemId = ItemId, DisplayState = 0, DetailDisplayState = DetailDisplayState, ViewState = 1, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId, IsSmartSearch = true, SearchKey = SearchKey });
        }

        public IActionResult OnPostDisplayOpen()
        {
            int allCount = Sql.GetAllCountbyUserFromSiteTable(UserId);
            int resolvedCount = Sql.GetResolvedCountByUserFromItemScopeResolved(UserId);

            if (allCount <= resolvedCount)
            {
                return RedirectToPage("/SiteViewAll_Items", new { SiteName = SiteName, ItemId = ItemId, DisplayState = 0, DetailDisplayState = DetailDisplayState, ViewState = 1, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId, IsSmartSearch = true, SearchKey = "" });
            }

            return RedirectToPage("/SiteViewAll_Items", new { IsSmartSearch = false, SiteName = SiteName, DisplayState = 1, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems, SearchKey = "Open" }); ;
        }

        public IActionResult OnPostDisplayResolved()
        {

            int resolvedCount = Sql.GetResolvedCountByUserFromItemScopeResolved(UserId);

            if (resolvedCount <= 0)
            {
                return RedirectToPage("/SiteViewAll_Items", new { SiteName = SiteName, ItemId = ItemId, DisplayState = 0, DetailDisplayState = DetailDisplayState, ViewState = 1, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId, IsSmartSearch = true, SearchKey = "" });
            }

            return RedirectToPage("/SiteViewAll_Items", new { SiteName = SiteName, ItemId = ItemId, DisplayState = 2, DetailDisplayState = DetailDisplayState, ViewState = 1, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId, IsSmartSearch = false, SearchKey = "Resolved" });
        }

        public string IsItemResolvedAtUserScope(string itemId)
        {
            string isResolved = "Open";

            if(Sql.IsResolved(itemId, UserId))
            {
                isResolved = "Resolved";
            }

            return isResolved;
        }

    }

}