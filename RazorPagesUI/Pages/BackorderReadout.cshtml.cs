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
    public class BackorderReadoutModel : PageModel
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

        public List<IC211_Model> LocationsList { get; set; }

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


        public void OnGet()
        {
            Console.WriteLine(DateTime.Now + " - Start Get");


            if (HasAccess && !string.IsNullOrWhiteSpace(SQL.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            if (string.IsNullOrWhiteSpace(SiteName))
            { SiteName = "Undefined."; }

            AllSitesList = SQL.GetAllSites();

            MasterSiteUserId = SQL.GetMasterUserIdBySite(SQL.TranslateNameToSite(SiteName));

            Console.WriteLine(MasterSiteUserId);

            if (!string.IsNullOrWhiteSpace(ItemId))
            {

                IC211 = SQL.GetIC211FromSiteTable(ItemId, SQL.TranslateNameToSite(SiteName));
                Console.WriteLine(DateTime.Now + " - Set IC211 with ItemId");
            }
            else if (!string.IsNullOrWhiteSpace(PrevItem))
            {
                ErrorState = 1;

                IC211 = SQL.GetIC211FromSiteTable(PrevItem, SQL.TranslateNameToSite(SiteName));
                Console.WriteLine(DateTime.Now + " - Set IC211 with PrevItem");
            }
            else
            {

                IC211 = SQL.GetFirstIC211FromSiteTable(SQL.TranslateNameToSite(SiteName), DisplayState);
                ItemId = IC211.Item_Number;

                Console.WriteLine(SQL.TranslateNameToSite(SiteName) + " - " + DisplayState);

                Console.WriteLine(DateTime.Now + " - Set IC211 with Get_First_IC211_BySiteDisplayState");
            }

            if (!string.IsNullOrEmpty(IC211.Item_Number))
            {
                ResolvedOrOpen = SQL.IsResolvedOrOpen(IC211.Item_Number, SQL.TranslateNameToSite(SiteName));
                Console.WriteLine(DateTime.Now + " - Get Resolved or Open State");
            }
            else
            {
                ResolvedOrOpen = "No Record";
                Console.WriteLine(DateTime.Now + " - Set ResolvedOrOpen to No Record");
            }

            AllCount = SQL.GetAllCountbyUserFromSiteTable(MasterSiteUserId);

            Console.WriteLine(DateTime.Now + " - GetAllCountsBySite");

            ResolvedCount = SQL.GetResolvedCountByUserFromItemScopeResolved(MasterSiteUserId);

            //ResolvedCount = 0;

            Console.WriteLine(DateTime.Now + " - GetResolvedCountsBySite, Site: " + SQL.TranslateNameToSite(SiteName) + ", Master UserId: " + SQL.GetMasterUserIdBySite(SQL.TranslateNameToSite(SiteName)));

            OpenCount = AllCount - ResolvedCount;

            Console.WriteLine(DateTime.Now + " - Calculate Open Counts");

            BackOrderDisplay = new BackOrderItemMaster_Model();

            Console.WriteLine(DateTime.Now + " - Instantiate BackOrderItemMaster_Model Object");

            BackOrderDisplay = SQL.GetBOIMByItemUserFromSiteTable(ItemId, MasterSiteUserId);

            Console.WriteLine(DateTime.Now + " - RetrieveBackOrderItemMasterByItem");

            NotesList = new List<Notes_Model>();

            NotesList = SQL.GetNotesByItemSite(ItemId, SQL.TranslateNameToSite(SiteName));

            Console.WriteLine(DateTime.Now + " - GetNotesByItemSite");

            SubsList = new List<Subs_Model>();

            Console.WriteLine(DateTime.Now + " - Instantiate List Object of Type Subs_Model");

            SubsList = SQL.GetSubsByItem(ItemId);

            Console.WriteLine(DateTime.Now + " - GetSubsByItem");

            LocationsList = new List<IC211_Model>();

            Console.WriteLine(DateTime.Now + " - Instantiate List Object of Type IC211_Model");

            LocationsList = SQL.GetLocationsByItemSite(ItemId, SQL.TranslateNameToSite(SiteName));

            Console.WriteLine(DateTime.Now + " - GetLocationsByItemSite");

            DisplayNotesList = new List<Notes_Model>();

            Console.WriteLine(DateTime.Now + " - Instantiate List Object of Type Notes_Model");

            DisplayNotesList = GetDisplayNotesList(NotesList);

            Console.WriteLine(DateTime.Now + " - GetDisplayNotesList");

            HasNotes = SQL.ItemHasNotes(ItemId);
        }


        public IActionResult OnPost()
        {
            //Console.WriteLine(BackOrderDisplay.Resolved);

            return RedirectToPage("/Index");
        }

        public IActionResult OnPostSearch()
        {
            return RedirectToPage("/All_Site_Items",new { SiteName = SiteName, SearchKey = SearchItem, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0, IsSmartSearch = true } );
        }

        public IActionResult OnPostDisplayAll()
        {
            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostDisplayOpen()
        {
            int allCount = SQL.GetAllCountbyUserFromSiteTable(MasterSiteUserId);
            int resolvedCount = SQL.GetResolvedCountByUserFromItemScopeResolved(MasterSiteUserId);

            if (allCount == resolvedCount)
            {
                return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
            }
                
            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, DisplayState = 1, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostDisplayResolved()
        {
            int resolvedCount = SQL.GetResolvedCountByUserFromItemScopeResolved(MasterSiteUserId);

            if (resolvedCount == 0)
            {
                return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
            }

            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, DisplayState = 2, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostNext()
        {
            string site = SQL.TranslateNameToSite(SiteName);

            IC211_Model newIC211 = new IC211_Model();

            if (DisplayState == 0)
            {
                newIC211 = SQL.GetNext_All_IC211ByItemUserFromSiteTable(ItemId, MasterSiteUserId);
            }
            else if (DisplayState == 1)
            {
                newIC211 = SQL.GetNextOpen_IC211_ByUserFromSiteTable(MasterSiteUserId, ItemId);
            }
            else if (DisplayState == 2)
            {
                newIC211 = SQL.GetNextResolved_IC211_ByUserFromSiteTable(MasterSiteUserId, ItemId);
            }
            

            string newItemId = newIC211.Item_Number;

            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, ItemId = newItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostPrevious()
        {
            string site = SQL.TranslateNameToSite(SiteName);

            IC211_Model newIC211 = new IC211_Model();

            if (DisplayState == 0)
            {
                newIC211 = SQL.GetPrevAll_IC211_ByItemUserFromSiteTable(MasterSiteUserId, ItemId);
            }
            else if (DisplayState == 1)
            {
                newIC211 = SQL.GetPrevOpen_IC211_ByUserFromSiteTable(MasterSiteUserId, ItemId);
            }
            else if (DisplayState == 2)
            {
                newIC211 = SQL.GetPrevResolved_IC211_ByUserFromSiteTable(MasterSiteUserId, ItemId);
            }


            string newItemId = newIC211.Item_Number;

            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, ItemId = newItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostDetailChange()
        {

            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostAddNote()
        {
            Notes_Model addNote = new Notes_Model
            {
                ITEM = ItemId,
                NOTE = NewNote,
                Site = SQL.TranslateNameToSite(SiteName),
                UserId = UserId
            };

            SQL.AddNoteByItem(addNote);

            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostChangeResolvedState()
        {
            SQL.ToggleResolvedState(ItemId, SQL.TranslateNameToSite(SiteName), UserId);

            int allCount = SQL.GetAllCountbyUserFromSiteTable(MasterSiteUserId);
            int resolvedCount = SQL.GetResolvedCountByUserFromItemScopeResolved(MasterSiteUserId);
            int openCount = allCount - resolvedCount;

            if (!(SQL.IsResolved(ItemId, SQL.TranslateNameToSite(SiteName))) && resolvedCount == 0)
            {
                DisplayState = 0;
            }
            else if (SQL.IsResolved(ItemId, SQL.TranslateNameToSite(SiteName)) && openCount == 0)
            {
                DisplayState = 0;
            }

            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostAllNotes()
        {
            return RedirectToPage("/AllNotes", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }

        public IActionResult OnPostViewAllItems()
        {
            return RedirectToPage("/All_Site_Items", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
        }



        public IActionResult OnPostGetNote()
        {

            return RedirectToPage("/ShowNote", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, ViewNoteId = ViewNoteId, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });

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
