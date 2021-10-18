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
    public class AllCriticalReadoutModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public bool HasAccess { get; set; }

        [BindProperty (SupportsGet = true)]
        public string SiteName { get; set; }

        [BindProperty]
        public BackOrderItemMaster_Model BackOrderDisplay { get; set; }

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

        [BindProperty]
        public bool UserCanEditSubs { get; set; }

        SQLCrud SQL = new SQLCrud(ConnectionString.GetConnectionString());


        public void OnGet()
        {
            if (HasAccess && !string.IsNullOrWhiteSpace(SQL.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            if (string.IsNullOrWhiteSpace(SiteName))
            { SiteName = "Undefined."; }

            AllSitesList = SQL.GetAllSites();

            if (!string.IsNullOrWhiteSpace(ItemId))
            {

                IC211 = SQL.GetIC211FromMasterIC211byItem(ItemId);
       
            }

            UserCanEditSubs = SQL.CanUserEditSubs(UserId);

            BackOrderDisplay = new BackOrderItemMaster_Model();

            BackOrderDisplay = SQL.GetBackOrderDisplayFromMasterBOIMbyItem(ItemId);

            Console.WriteLine(BackOrderDisplay.Item);

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
            return RedirectToPage("/Critical_All_Items",new { SiteName = SiteName, SearchKey = SearchItem, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0, IsSmartSearch = true } );
        }


        public IActionResult OnPostAddSub()
        {

            Console.WriteLine("AddSub: " + IC211.Item_Number);

            return RedirectToPage("/AddSub", new { OEMitemNumber = ItemId, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0, IsFromCriticalItemsReadout = true });
        }

        public IActionResult OnPostEditSubs()
        {
            return RedirectToPage("/SubsLIst", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0, IsFromCriticalItemsReadout = true });

        }


        public IActionResult OnPostDetailChange()
        {

            return RedirectToPage("/AllCriticalReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostAddNote()
        {
            Notes_Model addNote = new Notes_Model
            {
                ITEM = ItemId,
                NOTE = NewNote,
                Site = SQL.GetSiteByUser(UserId),
                UserId = UserId
            };

            SQL.AddNoteByItem(addNote);

            return RedirectToPage("/AllCriticalReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }


        public IActionResult OnPostAllNotes()
        {
            return RedirectToPage("/Critical_Items_AllNotesModel", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostViewAllItems()
        {
            return RedirectToPage("/Critical_All_Items", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }



        public IActionResult OnPostGetNote()
        {

            return RedirectToPage("/ShowNote_Critical_Items", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, ViewNoteId = ViewNoteId, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });

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
