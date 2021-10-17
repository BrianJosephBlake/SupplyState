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
    public class All_ItemsModel : PageModel
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


            if (HasAccess && !string.IsNullOrWhiteSpace(Sql.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            UserName = Sql.GetUserNameByUserId(UserId);

            Site = Sql.GetSiteByUser(UserId);

            AllSitesList = Sql.GetAllSites();

            HasHistory = Sql.HasNotesHistory(SearchKey);

            

            if(IsSmartSearch)
            {
                ItemList = Sql.SmartSearch_SiteTable(SearchKey, UserId);
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
            return RedirectToPage("/MyItemsReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = 0, DetailDisplayState = DetailDisplayState, ViewState = 1, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId });
        }

        public IActionResult OnPostSearch()
        {
            return RedirectToPage("/All_Items", new { SiteName = SiteName, ItemId = ItemId, DisplayState = 0, DetailDisplayState = DetailDisplayState, ViewState = 1, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId, IsSmartSearch = true, SearchKey = SearchKey });
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
            return RedirectToPage("/MyItemsReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

        public IActionResult OnPostDisplayOpen()
        {
            int allCount = Sql.GetAllCountbyUserFromSiteTable(UserId);
            int resolvedCount = Sql.GetResolvedCountByUserFromItemScopeResolved(UserId);

            if (allCount <= resolvedCount)
            {
                return RedirectToPage("/MyItemsReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
            }

            return RedirectToPage("/MyItemsReadout", new { SiteName = SiteName, DisplayState = 1, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

        public IActionResult OnPostDisplayResolved()
        {

            int resolvedCount = Sql.GetResolvedCountByUserFromItemScopeResolved(UserId);

            if (resolvedCount <= 0)
            {
                return RedirectToPage("/MyItemsReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
            }

            return RedirectToPage("/MyItemsReadout", new { SiteName = SiteName, DisplayState = 2, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

    }

}