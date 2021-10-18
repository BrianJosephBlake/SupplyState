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
    public class AddSubModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public bool HasAccess { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchKey { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ItemId { get; set; }

        public List<Notes_Model> NotesList { get; set; }

        public string Site { get; set; }

        public List<string> NoteCaptionList { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SiteName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string DisplayState { get; set; }

        public IC211_Model IC211 { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ViewState { get; set; }

        [BindProperty(SupportsGet = true)]
        public string DetailDisplayState { get; set; }

        [BindProperty]
        public int ViewNoteId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int FromMyItems { get; set; }

        [BindProperty]
        public bool UserHasItems { get; set; }

        [BindProperty]
        public List<string> AllSitesList { get; set; }

        /// <summary>
        /// /////////
        /// </summary>

        [BindProperty (SupportsGet = true)]
        public string OEMitemNumber  { get; set; }

        [BindProperty]
        public string SubLawsonNumber { get; set; }

        [BindProperty]
        public string SubMfrNumber { get; set; }

        [BindProperty]
        public string SubNote  { get; set; }

        [BindProperty (SupportsGet = true)]
        public bool IsFromCriticalItemsReadout { get; set; }

        SQLCrud Sql = new SQLCrud(ConnectionString.GetConnectionString());

        public void OnGet()
        {

            AllSitesList = Sql.GetAllSites();
        }

        public IActionResult OnPostAddSub()
        {
            Subs_Model newSub = new Subs_Model();

            if((OEMitemNumber != "") && (SubLawsonNumber != "" || SubMfrNumber != ""))
            {
                newSub.Item = OEMitemNumber;
                newSub.Sub_Lawson = SubLawsonNumber;
                newSub.Sub_Mfr = SubMfrNumber;
                newSub.Notes = SubNote;

                Console.WriteLine(newSub.Item + ", " + newSub.Sub_Lawson + ", " + newSub.Sub_Mfr);

                Sql.AddSub(newSub);

                if(IsFromCriticalItemsReadout)
                {
                    return RedirectToPage("/AllCriticalReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, ViewNoteId = ViewNoteId, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
                }
                else
                {
                    return RedirectToPage("/Item_Lookup_Readout", new { SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, ViewState = 1, ViewNoteId = ViewNoteId, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
                }
                
            }
            else
            {
                return RedirectToPage("/AddSub", new { IsFromCriticalItemsReadout = IsFromCriticalItemsReadout, SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = DetailDisplayState, HasAccess = HasAccess, UserId = UserId, FromMyItems = 0 });
            }
               
            
        }

        public IActionResult OnPostBack()
        {
            if(IsFromCriticalItemsReadout)
            {
                return RedirectToPage("/AllCriticalReadout", new { SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = 1, ViewState = 1, ViewNoteId = ViewNoteId, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
            }
            else
            {

            }
            return RedirectToPage("/Item_Lookup_Readout", new { SearchKey = SearchKey, SiteName = SiteName, ItemId = ItemId, DisplayState = DisplayState, DetailDisplayState = 1, ViewState = 1, ViewNoteId = ViewNoteId, HasAccess = HasAccess, UserId = UserId, FromMyItems = FromMyItems });
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
