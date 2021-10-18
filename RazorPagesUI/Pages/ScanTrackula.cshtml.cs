using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DataAccessLibrary;
using RazorPagesUI.PublicLibrary;
using DataAccessLibrary.Models;
using System.IO;

namespace RazorPagesUI.Pages
{
    public class ScanTrackulaModel : PageModel
    {

       
        [BindProperty (SupportsGet = true)]
        public int EntryId { get; set; }

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

            [BindProperty]
            public List<Subs_Model> Subs { get; set; }

            [BindProperty]
            public int SelectedSubId { get; set; }

            [BindProperty(SupportsGet = true)]
            public bool IsFromCriticalItemsReadout { get; set; }

        [BindProperty (SupportsGet = true)]
        public ScanTrackula_AddItem_Model ScanEntry { get; set; }



        SQLCrud Sql = new SQLCrud(ConnectionString.GetConnectionString());

       
       
        public void OnGet()
        {
            if (HasAccess && !string.IsNullOrWhiteSpace(Sql.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            AllSitesList = Sql.GetAllSites();
        }

        public IActionResult OnPostDiscard()
        {
            return RedirectToPage("/ScanTrackulaReadout", new { EntryId = EntryId, UserId = UserId, HasAccess = HasAccess });
        }

        public IActionResult OnPostAddScanEntry()
        {
            ScanEntry.UserId = UserId;
            ScanEntry.Created_DateTime = DateTime.Now.ToString();
            ScanEntry.ImageFileName = "";
            ScanEntry.UserName = Sql.GetUserNameByUserId(UserId);

            Sql.ScanTrackula_AddEntry(ScanEntry);

            EntryId = Sql.ScanTrackula_GetLastRecord().Id;
            
            return RedirectToPage("/ScanTrackulaReadout", new { EntryId = EntryId, UserId = UserId, HasAccess = HasAccess });

        }

        public string GetSiteName(string site)
        {
            return Sql.TranslateSiteToName(site);
        }

        

    }
}
