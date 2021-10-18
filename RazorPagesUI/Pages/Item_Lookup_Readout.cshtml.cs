using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using DataAccessLibrary.Models;
using DataAccessLibrary;
using RazorPagesUI.PublicLibrary;

namespace RazorPagesUI.Pages
{
    public class Item_Lookup_ReadoutModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public bool HasAccess { get; set; }

        [BindProperty (SupportsGet = true)]
        public string SiteName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchKey { get; set; }

        [BindProperty]
        public IC211_Model IC211 { get; set; }

        [BindProperty (SupportsGet = true)]
        public string ItemId { get; set; }

        [BindProperty (SupportsGet = true)]
        public int DisplayState { get; set; }

        [BindProperty]
        public string SearchItem { get; set; }

        [BindProperty]
        public string PrevItem { get; set; }

        [BindProperty (SupportsGet = true)]
        public int DetailDisplayState { get; set; }

        [BindProperty (SupportsGet = true)]
        public int UserId { get; set; }

        public List<Notes_Model> NotesList { get; set; } 

        public List<Subs_Model> SubsList { get; set; }

        public List<string> LocationsList { get; set; }

        public int ErrorState { get; set; }

        public string ResolvedOrOpen { get; set; }

        [BindProperty]
        public string NewNote { get; set; }

        public int AllCount { get; set; }

        public int ResolvedCount { get; set; }

        public int OpenCount { get; set; }

        [BindProperty]
        public int ViewNoteId { get; set; }

        [BindProperty (SupportsGet = true)]
        public bool FromMyItems { get; set; }

        public List<Notes_Model> DisplayNotesList { get; set; }

        [BindProperty]
        public int MasterSiteUserId { get; set; }

        [BindProperty]
        public bool UserHasItems { get; set; }

        [BindProperty]
        public List<string> AllSitesList { get; set; }

        [BindProperty]
        public bool HasNotes { get; set; }

        SQLCrud SQL = new SQLCrud(ConnectionString.GetConnectionString());

        [BindProperty]
        public BackOrderItemMaster_Model BackOrderDisplay { get; set; }

        [BindProperty]
        public bool IsFromItemLookup { get; set; }

        [BindProperty]
        public bool UserCanEditSubs { get; set; }

        
        public void OnGet()
        {
            Console.WriteLine("Readout: " + SearchKey);


            if (HasAccess && !string.IsNullOrWhiteSpace(SQL.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            if (string.IsNullOrWhiteSpace(SiteName))
            { SiteName = "Undefined."; }

            AllSitesList = SQL.GetAllSites();

            UserCanEditSubs = SQL.CanUserEditSubs(UserId);
            

            IC211 = SQL.GetIC211FromMasterIC211byItem(ItemId);

            BackOrderDisplay = SQL.GetBackOrderDisplayFromMasterBOIMbyItem(ItemId);

           
            NotesList = new List<Notes_Model>();

            NotesList = SQL.GetNotesByItem(ItemId);


            SubsList = new List<Subs_Model>();

            SubsList = SQL.GetSubsByItem(ItemId);


            LocationsList = new List<string>();

            LocationsList = SQL.GetSitesByItem(ItemId);


            DisplayNotesList = new List<Notes_Model>();

            DisplayNotesList = GetDisplayNotesList(NotesList);


            HasNotes = SQL.ItemHasNotes(ItemId);
        }


        public IActionResult OnPost()
        {
            //Console.WriteLine(BackOrderDisplay.Resolved);

            return RedirectToPage("/Index");
        }

        public IActionResult OnPostSearch()
        {
            return RedirectToPage("/All_Items_Item_Lookup",new { SiteName = SiteName, SearchKey = SearchItem, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0, IsSmartSearch = true } );
        }

        public IActionResult OnPostDetailChange()
        {

            return RedirectToPage("/Item_Lookup_Readout", new { SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostAddNote()
        {
            string noteSite;

            if (!string.IsNullOrWhiteSpace(SQL.GetSiteByUser(UserId)) || UserId != 0)
            {
                noteSite = SQL.GetSiteByUser(UserId);
            }
            else
            {
                noteSite = "Global";
            }
            

            Notes_Model addNote = new Notes_Model
            {
                ITEM = ItemId,
                NOTE = NewNote,
                Site = noteSite,
                UserId = UserId
            };

            SQL.AddNoteByItem(addNote);

            return RedirectToPage("/Item_Lookup_Readout", new { SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostEditSubs()
        {
            return RedirectToPage("/SubsLIst", new { SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0, IsFromCriticalItemsReadout = false });

        }

        public IActionResult OnPostAllNotes()
        {
            return RedirectToPage("/ItemLookup_AllNotes", new { SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostBackToSearch()
        {
            return RedirectToPage("/All_Items_Item_Lookup", new { SearchKey = SearchKey, SiteName = SiteName, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostAddSub()
        {

            Console.WriteLine("AddSub: " + IC211.Item_Number);

            return RedirectToPage("/AddSub", new { OEMitemNumber = ItemId, SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }


        public IActionResult OnPostGetNote()
        {

            return RedirectToPage("/ShowNote_Item_Lookup", new { SearchKey = SearchKey, IsFromItemLookup = true, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, ViewNoteId = ViewNoteId, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });

        }


        public int GetCaptionLength(string Note)
        {
            int captionLength = 50;

            if (Note.Length < 50)
            {
                captionLength = Note.Length;
            }

            return captionLength;
        }


        public List<Notes_Model> GetDisplayNotesList(List<Notes_Model> notes)
        {

            List<Notes_Model> DisplayNotesList = new List<Notes_Model>();

            int notesCount = notes.Count;
            if (notesCount > 3)
            { notesCount = 3; }


            for (int i = 0; i < notesCount; i++)
            {
                DisplayNotesList.Add(notes[i]);
            }

            if (DisplayNotesList.Count == 0)
            {
                DisplayNotesList.Add(SQL.GetNullNote());
            }

            return DisplayNotesList;
        }

        public IActionResult OnPostCancelNote()
        {
            return RedirectToPage("/Item_Lookup_Readout", new { SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }


        public string GetUserName(int userId)
        {
            return SQL.GetUserNameByUserId(userId);
        }

        public string GetSiteName(string site)
        {
            return SQL.TranslateSiteToName(site);
        }

        
     
    }
}
