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
    public class AllNotesModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public bool HasAccess { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ItemId { get; set; }

        public List<Notes_Model> NotesList { get; set; }

        public string Site { get; set; }

        public List<string> NoteCaptionList { get; set; }

        [BindProperty (SupportsGet = true)]
        public string SiteName { get; set; }

        [BindProperty (SupportsGet = true)]
        public string DisplayState { get; set; }

        public IC211_Model IC211 { get; set; }

        [BindProperty (SupportsGet = true)]
        public int ViewState  { get; set; }

        [BindProperty (SupportsGet = true)]
        public string DetailDisplayState { get; set; }

        [BindProperty]
        public int ViewNoteId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int UserId { get; set; }

        [BindProperty (SupportsGet = true)]
        public int FromMyItems { get; set; }

        [BindProperty]
        public bool UserHasItems { get; set; }

        [BindProperty]
        public List<string> AllSitesList { get; set; }

        SQLCrud Sql = new SQLCrud(ConnectionString.GetConnectionString());

        public void OnGet()
        {


            if (HasAccess && !string.IsNullOrWhiteSpace(Sql.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            //Console.WriteLine(FromMyItems);

            Site = Sql.TranslateNameToSite(SiteName);

            AllSitesList = Sql.GetAllSites();

            if (string.IsNullOrWhiteSpace(ItemId))
            {
                int masterUserId = Sql.GetMasterUserIdBySite(Site)
;               ItemId = Sql.Get_First_IC211_ByUserDisplayState(masterUserId,0).Item_Number;
            }

            NotesList = Sql.GetNotesByItem(ItemId);

            if (NotesList.Count > 0)
            {

            }

            IC211 = new IC211_Model();

            IC211 = Sql.Get_IC211_ByItemSite(ItemId, Site);
            
              


        }

        public IActionResult OnPostGetNote()
        {
            return RedirectToPage("/ShowNote", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, ViewNoteId = ViewNoteId, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId });
        }

        public int GetCaptionLength(string Note)
        {
            int captionLength = 50;

            if(Note.Length < 100)
            {
                captionLength = Note.Length;
            }

            return captionLength;
        }



        public IActionResult OnPostBack()
        {
            Console.WriteLine(SiteName);

            if (FromMyItems != 1)
            {
                return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, ViewNoteId = ViewNoteId, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
            }
            else
            {
                return RedirectToPage("/MyItemsReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, ViewNoteId = ViewNoteId, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
            }
                
        }

        public string GetUserName(int userId)
        {
            return Sql.GetUserNameByUserId(userId);

        }

        public string GetSiteName(string site)
        {
            return Sql.TranslateSiteToName(site);
        }
    }

    
}
