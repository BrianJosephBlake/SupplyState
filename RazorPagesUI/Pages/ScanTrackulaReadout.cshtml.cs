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
    public class ScanTrackulaReadoutModel : PageModel
    {
        [BindProperty]
        public bool HasImage { get; set; }

        [BindProperty]
        public string PdfPath { get; set; }

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

        [BindProperty(SupportsGet = true)]
        public ScanTrackula_AddItem_Model ScanEntry { get; set; }

        [BindProperty (SupportsGet = true)]
        public string SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public int EntryId { get; set; }

        [BindProperty]
        public List<string> FileNames { get; set; }

        SQLCrud Sql = new SQLCrud(ConnectionString.GetConnectionString());
        public void OnGet()
        {
            UserId = 73;

            if (HasAccess && !string.IsNullOrWhiteSpace(Sql.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            AllSitesList = Sql.GetAllSites();

           

            if(EntryId == 0)
            {
                ScanEntry = Sql.ScanTrackula_GetLastRecord();
                EntryId = ScanEntry.Id;
            }
            else
            {
                ScanEntry = Sql.ScanTrackula_RetrieveRecord(EntryId);
            }

            FileNames = Sql.ScanTrackula_ScrapeScansFolder(@"wwwroot\lib\ScanScans");

            HasImage = Sql.ScanTrackula_HasImage(EntryId);

            PdfPath = @"/lib/ScanScans/" + ScanEntry.ImageFileName;

        }

        public IActionResult OnPostGenerateDeliverySheet()
        {
            Sql.ScanTrackula_GenerateDeliverySheetPdf(EntryId);

            return RedirectToPage("/ScanTrackula_ViewDeliverySheet",new { UserId = UserId, HasAccess = HasAccess, EnryId = EntryId, PdfPath = @"/lib/ScanScans/DeliverySheet_" + EntryId + ".pdf"});
        }

        public IActionResult OnPostSearch()
        {
            return RedirectToPage("/ScanTrackula_SearchResults", new { UserId = UserId, HasAccess = HasAccess, SearchString = SearchString, EntryId = EntryId });
        }

        public IActionResult OnPostAddEntry()
        {
            return RedirectToPage("/ScanTrackula", new { UserId = UserId, HasAcess = HasAccess, EntryId = EntryId });
        }

        public string GetSiteName(string site)
        {
            return Sql.TranslateSiteToName(site);
        }

        public string GetUserName(int userId)
        {
            string userName = Sql.GetUserNameByUserId(userId);

            return userName;
        }

    }


}
