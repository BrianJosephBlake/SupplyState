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
    public class SiteViewReadoutModel : PageModel
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

        [BindProperty (SupportsGet = true)]
        public bool DidCalculateRunway { get; set; }

        [BindProperty (SupportsGet = true)]
        public int Runway { get; set; }

        [BindProperty]
        public string PrevItem { get; set; }

        [BindProperty (SupportsGet = true)]
        public int DetailDisplayState { get; set; }

        [BindProperty (SupportsGet = true)]
        public int UserId { get; set; }

        [BindProperty]
        public string UserName { get; set; }

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
        public int FromMyItems { get; set; }

        public List<Notes_Model> DisplayNotesList { get; set; }

        [BindProperty]
        public bool UserHasItems { get; set; }

        [BindProperty]
        public List<string> AllSitesList { get; set; }

        [BindProperty]
        public bool HasNotes { get; set; }

        [BindProperty (SupportsGet = true)]
        public string MessageToUser { get; set; }

        [BindProperty]
        public int QOH { get; set; }

        [BindProperty]
        public string Scope { get; set; }

        SQLCrud SQL = new SQLCrud(ConnectionString.GetConnectionString());


        public void OnGet()
        {
            AllSitesList = SQL.GetAllSites();

            if (HasAccess && !string.IsNullOrWhiteSpace(SQL.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            string site = SQL.GetSiteByUser(UserId);

            FromMyItems = 1;

            UserName = SQL.GetUserNameByUserId(UserId);

            Scope = SQL.GetUserScope(UserId);

            SiteName = SQL.TranslateSiteToName(site);

            if (string.IsNullOrWhiteSpace(SiteName))
            { SiteName = "Undefined."; }

            if (!string.IsNullOrWhiteSpace(ItemId))
            {
                IC211 = SQL.GetIC211ByUserFromSiteTable(UserId,ItemId);
            }
            else if (!string.IsNullOrWhiteSpace(PrevItem))
            {
                ErrorState = 1;

                IC211 = SQL.GetIC211ByUserFromSiteTable(UserId, PrevItem);
            }
            else
            {
                IC211 = SQL.GetFirstIC211ByUserDisplayStateFromSiteTable(UserId, DisplayState);

                if(!string.IsNullOrWhiteSpace(IC211.Item_Number))
                { ItemId = IC211.Item_Number; }
                else
                { ItemId = "NullItem"; }
                
            }

            if (!string.IsNullOrEmpty(IC211.Item_Number))
            {
                ResolvedOrOpen = SQL.IsResolvedOrOpen(IC211.Item_Number, UserId);
            }
            else
            {
                ResolvedOrOpen = "No Record";
            }

            AllCount = SQL.GetAllCountbyUserFromSiteTable(UserId);

            //Console.WriteLine(DateTime.Now + " - Get All Count by User & Site");

            //Console.WriteLine("Item: " + IC211.Item_Number + ", userId: " + UserId);

            ResolvedCount = SQL.GetResolvedCountByUserFromItemScopeResolved(UserId);

            //ResolvedCount = SQL.GetResolvedCountByUserFromSiteTable(UserId);
            //Console.WriteLine(DateTime.Now + " - Get Resolved Count by User");

            if (ResolvedCount >= AllCount)
            {
                ResolvedCount = AllCount;
            }

            OpenCount = AllCount - ResolvedCount;


            BackOrderDisplay = new BackOrderItemMaster_Model();

            //Console.WriteLine("Item to retrieve from BIOM: {0}", IC211.Item_Number);

            BackOrderDisplay = SQL.GetBOIMByItemUserFromSiteTable(IC211.Item_Number,UserId);
            //Console.WriteLine(DateTime.Now + " - Retrieve BOIM Record");

            NotesList = new List<Notes_Model>();

            NotesList = SQL.GetNotesByItemSite(ItemId, SQL.TranslateNameToSite(SiteName));
            //Console.WriteLine(DateTime.Now + " - Get Notes by Item & Site");

            SubsList = new List<Subs_Model>();

            SubsList = SQL.GetSubsByItem(ItemId);

            //Console.WriteLine(DateTime.Now + " - Get Subs by Item");

            LocationsList = new List<IC211_Model>();

            LocationsList = SQL.GetLocationsByItemSite(ItemId, SQL.TranslateNameToSite(SiteName));
            //Console.WriteLine(DateTime.Now + " - Get Locations by Item");

            DisplayNotesList = new List<Notes_Model>();

            DisplayNotesList = GetDisplayNotesList(NotesList);
            //Console.WriteLine(DateTime.Now + " - Create Display Notes List");

            //Console.WriteLine(DateTime.Now + " - Finish Get");

            HasNotes = SQL.ItemHasNotes(ItemId);


        }


        public IActionResult OnPost()
        {
            //Console.WriteLine(BackOrderDisplay.Resolved);

            return RedirectToPage("/Index");
        }

        public IActionResult OnPostSearch()
        {
            return RedirectToPage("/SiteViewAll_Items", new { SiteName = SiteName, SearchKey = SearchItem, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 1, IsSmartSearch = true });
        }

        public IActionResult OnPostDisplayAll()
        {
            return RedirectToPage("/SiteViewReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

        public IActionResult OnPostDisplayOpen()
        {
            int allCount = SQL.GetAllCountbyUserFromSiteTable(UserId);
            int resolvedCount = SQL.GetResolvedCountByUserFromItemScopeResolved(UserId);

            if (allCount <= resolvedCount)
            {
                return RedirectToPage("/SiteViewReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
            }
                
            return RedirectToPage("/SiteViewReadout", new { SiteName = SiteName, DisplayState = 1, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

        public IActionResult OnPostDisplayResolved()
        {

            int resolvedCount = SQL.GetResolvedCountByUserFromItemScopeResolved(UserId);

            if (resolvedCount <= 0)
            {
                return RedirectToPage("/SiteViewReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
            }

            return RedirectToPage("/SiteViewReadout", new { SiteName = SiteName, DisplayState = 2, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

        public IActionResult OnPostNext()
        {
            string site = SQL.TranslateNameToSite(SiteName);
            string newItemId = "";

            IC211_Model newIC211 = new IC211_Model();

            if (DisplayState == 0)
            {
                newItemId = SQL.NextAllByUserId(UserId,ItemId);
            }
            else if (DisplayState == 1)
            {
                newItemId = SQL.NextOpenByUserId(UserId,ItemId);
            }
            else if (DisplayState == 2)
            {
                newItemId = SQL.NextResolvedByUserId(UserId, ItemId);
            }

            return RedirectToPage("/SiteViewReadout", new { SiteName = SiteName, ItemId = newItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

        public IActionResult OnPostPrevious()
        {
            string site = SQL.TranslateNameToSite(SiteName);
            string newItemId = "";

            IC211_Model newIC211 = new IC211_Model();

            if (DisplayState == 0)
            {
                newItemId = SQL.PreviousAllByUserId(UserId, ItemId);
            }
            else if (DisplayState == 1)
            {
                newItemId = SQL.PreviousOpenByUserId(UserId,ItemId);
            }
            else if (DisplayState == 2)
            {
                newItemId = SQL.PreviousResolvedByUserId(UserId, ItemId);
            }

            return RedirectToPage("/SiteViewReadout", new { SiteName = SiteName, ItemId = newItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

        public IActionResult OnPostDetailChange()
        {

            return RedirectToPage("/SiteViewReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

        public IActionResult OnPostCalculateRunway()
        {
            string site = SQL.GetSiteByUser(UserId);
            string scope = SQL.GetUserScope(UserId);

            Runway = SQL.ItemRunwayInDAys(QOH, ItemId, site, scope);

            return RedirectToPage("/SiteViewReadout", new { DidCalculateRunway = true, Runway = Runway, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
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

            return RedirectToPage("/SiteViewReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }


        public IActionResult OnPostAllNotes()
        {
            Console.WriteLine(FromMyItems);
            return RedirectToPage("/AllNotes", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }



        public IActionResult OnPostGetNote()
        {

            return RedirectToPage("/ShowNote", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, ViewNoteId = ViewNoteId, HasAccess = HasAccess, UserId = UserId, FromMyItems = 1 });

        }

        public IActionResult OnPostCancelNote()
        {
            return RedirectToPage("/MyItemsReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
        }

        public IActionResult OnPostViewAllItems()
        {
            return RedirectToPage("/All_Items", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 1 });
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
