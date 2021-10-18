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
    public class ShowNote_Critical_ItemsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public bool HasAccess { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ItemId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int DisplayState { get; set; }

        [BindProperty(SupportsGet = true)]
        public int DetailDisplayState { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ViewState { get; set; }

        [BindProperty (SupportsGet = true)]
        public int ViewNoteId { get; set; }

        [BindProperty(SupportsGet =true)]
        public string SiteName { get; set; }

        [BindProperty(SupportsGet = true)]
        public int UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int FromMyItems { get; set; }

        public Notes_Model Note { get; set; }

        [BindProperty (SupportsGet = true)]
        public bool IsFromBrowse { get; set; }

        [BindProperty]
        public bool UserHasItems { get; set; }

        [BindProperty (SupportsGet = true)]
        public string SearchKey { get; set; }

        SQLCrud Sql = new SQLCrud(ConnectionString.GetConnectionString());

        [BindProperty]
        public List<string> AllSitesList { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsFromItemLookup { get; set; }


        public void OnGet()
        {
            Console.WriteLine(IsFromBrowse);

            AllSitesList = Sql.GetAllSites();

            List<Notes_Model> notesList = new List<Notes_Model>();

            notesList = Sql.GetNotesByItem(ItemId);

            SetNote(notesList, ViewNoteId);

            Console.WriteLine("DisplayState: " + DisplayState);


            if (HasAccess && !string.IsNullOrWhiteSpace(Sql.GetSiteByUser(UserId)))
            { UserHasItems = true; }

        }


        public IActionResult OnPostBack()
        {
            
            if(IsFromItemLookup)
            {
                return RedirectToPage("/AllCriticalReadout", new { SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewNoteId = ViewNoteId, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId });
            }
            else
            {
                return RedirectToPage("/Critical_Items_AllNotesModel", new { SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewNoteId = ViewNoteId, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId });
            }
            
            
            
        }


        public IActionResult OnPostNext()
        {
            List<Notes_Model> notesList = new List<Notes_Model>();

            notesList = Sql.GetNotesByItem(ItemId);

            GetNextNote(notesList, ViewNoteId);


            return RedirectToPage("/ShowNote_Critical_Items", new { SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, ViewNoteId = Note.Id, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId });
        }

        public IActionResult OnPostPrevious()
        {
            List<Notes_Model> notesList = new List<Notes_Model>();

            notesList = Sql.GetNotesByItem(ItemId);

            GetPreviousNote(notesList, ViewNoteId);


            return RedirectToPage("/ShowNote_Critical_Items", new { SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, ViewNoteId = Note.Id, HasAccess = HasAccess, FromMyItems = FromMyItems, UserId = UserId });
        }




        public void SetNote(List<Notes_Model> notesList, int id)
        {
            bool foundIt = false;

            int i = 0;
            int foundItIndex = 0;

            while (!foundIt && (i < notesList.Count))
            {
                if (notesList[i].Id == ViewNoteId)
                {
                    foundItIndex = i;
                    foundIt = true;
                }
                else
                {
                    i++;
                }
            }

            if (!foundIt)
            {
                Note = Sql.GetNullNote();
            }
            else
            {
                Note = notesList[i];
            }

        }



        public void GetPreviousNote(List<Notes_Model> notesList, int id)
        {
            bool foundIt = false;

            int i = 0;
            int foundItIndex = 0;

            while (!foundIt && (i < notesList.Count))
            {
                if (notesList[i].Id == ViewNoteId)
                {
                    foundItIndex = i;
                    foundIt = true;
                }
                else
                {
                    i++;
                }
            }

            if (!foundIt)
            {
                Note = Sql.GetNullNote();
            }
            else if (i != (notesList.Count-1))
            {
                Note = notesList[i+1];
            }
            else
            {
                Note = notesList[i];
            }

        }



        public void GetNextNote(List<Notes_Model> notesList, int id)
        {
            bool foundIt = false;

            int i = 0;
            int foundItIndex = 0;

            while (!foundIt && (i < notesList.Count))
            {
                if (notesList[i].Id == ViewNoteId)
                {
                    foundItIndex = i;
                    foundIt = true;
                }
                else
                {
                    i++;
                }
            }

            if (!foundIt)
            {
                Note = Sql.GetNullNote();
            }
            else if (i != 0)
            {
                Note = notesList[i - 1];
            }
            else
            {
                Note = notesList[i];
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
