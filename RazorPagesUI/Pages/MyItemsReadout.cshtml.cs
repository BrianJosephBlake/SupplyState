using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesUI.DataModels;
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
        public string UserId { get; set; }

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

        public List<Notes_Model> DisplayNotesList { get; set; }


        SQLCrud SQL = new SQLCrud(ConnectionString.GetConnectionString());


        public void OnGet()
        {

            

            if (string.IsNullOrWhiteSpace(SiteName))
            { SiteName = "Undefined."; }

           
            if (!string.IsNullOrWhiteSpace(ItemId))
            {

                IC211 = SQL.Get_IC211_ByItemSite(ItemId, SQL.TranslateNameToSite(SiteName));
            }
            else if (!string.IsNullOrWhiteSpace(PrevItem))
            {
                ErrorState = 1;

                IC211 = SQL.Get_IC211_ByItemSite(PrevItem, SQL.TranslateNameToSite(SiteName));
            }
            else
            {

                IC211 = SQL.Get_First_IC211_BySiteDisplayState(SQL.TranslateNameToSite(SiteName), DisplayState);
                ItemId = IC211.Item_Number;
            }

            if (!string.IsNullOrEmpty(IC211.Item_Number))
            {
                ResolvedOrOpen = SQL.IsResolvedOrOpen(IC211.Item_Number, SQL.TranslateNameToSite(SiteName));
            }
            else
            {
                ResolvedOrOpen = "No Record";
            }

            AllCount = SQL.GetAllCountBySite(SQL.TranslateNameToSite(SiteName));

            ResolvedCount = SQL.GetResolvedCountsBySite(SQL.TranslateNameToSite(SiteName));

            OpenCount = AllCount - ResolvedCount;

            BackOrderDisplay = new BackOrderItemMaster_Model();

            BackOrderDisplay = SQL.RetrieveBackOrderItemMasterByItem(IC211.Item_Number);

            NotesList = new List<Notes_Model>();

            NotesList = SQL.GetNotesByItemSite(ItemId, SQL.TranslateNameToSite(SiteName));

            SubsList = new List<Subs_Model>();

            SubsList = SQL.GetSubsByItem(ItemId);

            LocationsList = new List<IC211_Model>();

            LocationsList = SQL.GetLocationsByItemSite(ItemId, SQL.TranslateNameToSite(SiteName));

            DisplayNotesList = new List<Notes_Model>();

            DisplayNotesList = GetDisplayNotesList(NotesList);

            
        }


        public IActionResult OnPost()
        {
            Console.WriteLine(BackOrderDisplay.Resolved);

            return RedirectToPage("/Index");
        }

        public IActionResult OnPostSearch()
        {

            return RedirectToPage("/BackorderReadout",new { SiteName = SiteName, ItemId = SearchItem, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess } );
        }

        public IActionResult OnPostDisplayAll()
        {
            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess });
        }

        public IActionResult OnPostDisplayOpen()
        {
            int allCount = SQL.GetAllCountBySite(SQL.TranslateNameToSite(SiteName));
            int resolvedCount = SQL.GetResolvedCountsBySite(SQL.TranslateNameToSite(SiteName));

            if (allCount == resolvedCount)
            {
                return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess });
            }
                
            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, DisplayState = 1, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess });
        }

        public IActionResult OnPostDisplayResolved()
        {

            int resolvedCount = SQL.GetResolvedCountsBySite(SQL.TranslateNameToSite(SiteName));

            if (resolvedCount == 0)
            {
                return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, DisplayState = 0, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess });
            }

            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, DisplayState = 2, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess });
        }

        public IActionResult OnPostNext()
        {
            string site = SQL.TranslateNameToSite(SiteName);

            IC211_Model newIC211 = new IC211_Model();

            if (DisplayState == 0)
            {
                newIC211 = SQL.GetNext_IC211_All(ItemId, site);
            }
            else if (DisplayState == 1)
            {
                newIC211 = SQL.GetNext_IC211_Open(ItemId, site);
            }
            else if (DisplayState == 2)
            {
                newIC211 = SQL.GetNext_IC211_Resolved(ItemId, site);
            }
            

            string newItemId = newIC211.Item_Number;

            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, ItemId = newItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess });
        }

        public IActionResult OnPostPrevious()
        {
            string site = SQL.TranslateNameToSite(SiteName);

            IC211_Model newIC211 = new IC211_Model();

            if (DisplayState == 0)
            {
                newIC211 = SQL.GetPrevious_IC211_All(ItemId, site);
            }
            else if (DisplayState == 1)
            {
                newIC211 = SQL.GetPrevious_IC211_Open(ItemId, site);
            }
            else if (DisplayState == 2)
            {
                newIC211 = SQL.GetPrevious_IC211_Resolved(ItemId, site);
            }


            string newItemId = newIC211.Item_Number;

            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, ItemId = newItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess });
        }

        public IActionResult OnPostDetailChange()
        {

            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess });
        }

        public IActionResult OnPostAddNote()
        {
            Notes_Model addNote = new Notes_Model
            {
                ITEM = ItemId,
                NOTE = NewNote,
                Site = SQL.TranslateNameToSite(SiteName)
            };

            SQL.AddNoteByItem(addNote);

            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess });
        }

        public IActionResult OnPostChangeResolvedState()
        {
            SQL.ToggleResolvedState(ItemId, SQL.TranslateNameToSite(SiteName));

            int allCount = SQL.GetAllCountBySite(SQL.TranslateNameToSite(SiteName));
            int resolvedCount = SQL.GetResolvedCountsBySite(SQL.TranslateNameToSite(SiteName));
            int openCount = allCount - resolvedCount;

            if (!(SQL.IsResolved(ItemId, SQL.TranslateNameToSite(SiteName))) && resolvedCount == 0)
            {
                DisplayState = 0;
            }
            else if (SQL.IsResolved(ItemId, SQL.TranslateNameToSite(SiteName)) && openCount == 0)
            {
                DisplayState = 0;
            }

            return RedirectToPage("/BackorderReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess });
        }

        public IActionResult OnPostAllNotes()
        {
            return RedirectToPage("/AllNotes", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess });
        }



        public IActionResult OnPostGetNote()
        {

            return RedirectToPage("/ShowNote", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, ViewNoteId = ViewNoteId, HasAccess = HasAccess });

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
    }
}
