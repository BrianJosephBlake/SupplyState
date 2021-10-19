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
    public class BackOrderItemMaster_ExecuteModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string IC211Input { get; set; }

        [BindProperty(SupportsGet = true)]
        public string BackOrderItemMaster_Path { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Notes_Path { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool HasAccess { get; set; }

        [BindProperty(SupportsGet = true)]
        public string UsersLocations_Path { get; set; }

        [BindProperty]
        public string Site { get; set; }

        [BindProperty]
        public string OutputUsersLocationsPath { get; set; }

        [BindProperty]
        public string Scope_LocationsPath { get; set; }

        [BindProperty]
        public string SubInitItem { get; set; }

        [BindProperty]
        public string SubLawsonItem { get; set; }

        [BindProperty]
        public string SubMfrItem { get; set; }

        [BindProperty]
        public List<string> AllSitesList { get; set; }

        [BindProperty]
        public string TableName { get; set; }

        SQLCrud sql = new SQLCrud(ConnectionString.GetConnectionString());

        [BindProperty]
        public string Locations_Sites_Path { get; set; }

        [BindProperty]
        public string FilePath { get; set; }

        [BindProperty]
        public int[] BackOrderStagingFieldList { get; set; }

        [BindProperty]
        public string SourceField { get; set; }

        [BindProperty]
        public int ItemField { get; set; }

        [BindProperty]
        public int[] NotesFields { get; set; }

        [BindProperty]
        public string CarryOverFilePath { get; set; }

        [BindProperty]
        public string PyxisSite { get; set; }

        [BindProperty (SupportsGet = true)]
        public string AddSite_Site { get; set; }

        [BindProperty(SupportsGet = true)]
        public string AddSite_SiteName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string AddSite_Company { get; set; }

        [BindProperty(SupportsGet = true)]
        public string AddSite_IC211File { get; set; }

        [BindProperty(SupportsGet = true)]
        public string AddSite_LocationSiteScopeFilePath { get; set; }

        [BindProperty(SupportsGet = true)]
        public string AddSite_UsageFilePath{ get; set; }

        [BindProperty(SupportsGet = true)]
        public string AddSite_RQ201FilePath { get; set; }

        [BindProperty(SupportsGet = true)]
        public string AddSite_ScopesListFilePath { get; set; }

        [BindProperty(SupportsGet = true)]
        public string AddSite_Users_LocationsFilePath { get; set; }

        [BindProperty]
        public string RegionField { get; set; }



        public void OnGet()
        {
            BackOrderStagingFieldList = new int[8];
            NotesFields = new int[10];

            IC211Input = "";

            AllSitesList = sql.GetAllSites();

            //sql.CreateItemScopeResolvedTables();
        }

        public IActionResult OnPostClearBackOrderItemMaster()
        {
            sql.ClearBackOrderItemMaster();

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostUpdateAllToMaster()
        {
            //sql.UpdateAllToMaster();

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostIngestIC211()
        {
            sql.BulkIngestIC211(IC211Input, ',', "site");

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostIngestLocations_Sites()
        {
            sql.Bulk_IngestLocationsSites(Locations_Sites_Path, ",");

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostIngestBackOrderItemMaster()
        {
            sql.BulkIngestBackOrderItemMaster(BackOrderItemMaster_Path, ',');

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostIngestUsersLocations()
        {
            sql.BulkIngestUsers_Locations(UsersLocations_Path, ',',"site");

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostIngestNotes()
        {
            sql.BulkIngestNotes(Notes_Path, ',');

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostIngestSubs()
        {
            sql.IngestSubs("/Users/brianblake/Downloads/SUBS-csv.csv", ",");

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostClearItemSiteResolved()
        {
            sql.ResetItemSiteResolved();

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostOutputUsers()
        {
            sql.OutputUserNamesLocations(OutputUsersLocationsPath, false);

            return RedirectToPage("/Index");
        }

        public IActionResult OnPostParseBOIM()
        {
            sql.ParseBOIM();

            return RedirectToPage("/Index");
        }

        public IActionResult OnPostMatchResolved()
        {
            List<string> sites = new List<string>
            {
                "2200STF",
                "2200GSH",
                "2200STE",
                "2200NHI"
            };

            foreach (var site in sites)
            {
                sql.MatchResolved(site);
            }


            return RedirectToPage("/Index");
        }

        public IActionResult OnPostParseCarryOver()
        {
            sql.ParseStillOpen();

            return RedirectToPage("/Index");
        }

        public IActionResult OnPostClearIC211()
        {
            sql.ClearIC211();

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostMatchIC211LocationToSite()
        {
            sql.MatchIC211LocationToSite();

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostAddSub()
        {
            Subs_Model newSub = new Subs_Model
            {
                Item = SubInitItem,
                Sub_Lawson = SubLawsonItem,
                Sub_Mfr = SubMfrItem
            };

            sql.AddSub(newSub);

            return RedirectToPage("/BackOrderItemMaster_Execute");

        }

        public IActionResult OnPostParseIC211()
        {
            sql.ParseIC211();

            return RedirectToPage("/Index");
        }

        public IActionResult OnPostParseUsers_Locations()
        {
            sql.ParseUsers_Locations();

            return RedirectToPage("/Index");
        }

        public string GetSiteName(string site)
        {
            return sql.TranslateSiteToName(site);
        }

        public IActionResult OnPostUpdateUserResolvedTables()
        {
            sql.UpdateScopeResolvedTables();

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostParseScope_Locations()
        {
            sql.ParseScope_Locations();

            return RedirectToPage("/BackOrderItemMaster_Execute");
        }

        public IActionResult OnPostDropItemScopeResolvedTables()
        {
            sql.DropItemScopeResolvedTables();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostBulkIngestScope_Locations()
        {
            sql.BulkIngestScope_Locations(Scope_LocationsPath, ',');
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostUpdateUsers_LocationsWithSites()
        {
            sql.UpdateUsers_LocationsWithSites();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostSetAllBOIMToOpen()
        {
            sql.SetAllBOIMToOpen();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostCreateTable()
        {
            sql.CreateTable(TableName, FilePath, ',');
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostStageData()
        {
            sql.IngestAndStageBackOrderData(FilePath, ',', BackOrderStagingFieldList.ToList(),SourceField,RegionField);
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostDuplicates()
        {
            sql.FormatBackOrderDataByPriority();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostNotesCruncher()
        {
            sql.NotesCruncher(FilePath, ',', ItemField, NotesFields.ToList());
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostActivateBackOrderStaging()
        {
            sql.ActivateBackOrderStaging();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostRectifyBOIMwithItemScopeResolvedTables()
        {
            sql.RectifyBOIMwithItemScopeResolvedTables();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostExportCarryOverByAllSites()
        {
            sql.ExportCarryOverByAllSites(CarryOverFilePath);
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostExportProgressReport()
        {
            sql.ExportProgressReport();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostSetAccessPermissionsForAllSites()
        {
            sql.SetAccessPermissionsForAllSites();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostBulkIngestSubs()
        {
            sql.BulkIngestSubs(FilePath, ',');
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostBulk_IngestLRQ201()
        {
            sql.Bulk_IngestLRQ201(FilePath,",");
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostBulk_IngestPyxisLocations()
        {
            sql.Bulk_IngestPyxisLocations(FilePath, ',', PyxisSite);
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostMatchUsageSKUsToIC211Tables()
        {
            sql.UpdateItemSiteScopeUsageTablesSKUsPerIC211();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostCreateItemScopeResolvedTables()
        {
            sql.CreateItemScopeResolvedTables();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostAutoResolveRollingBackorders()
        {
            sql.AutoResolveRollingBackorders();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostUsageCrunch()
        {
            sql.UsageCrunch();
            return RedirectToPage("/Index");
        }



        public IActionResult OnPostParseSiteScopeUsageBOIMforAllSites()
        {
            sql.ParseSiteScopeUsageBOIMforAllSites();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostParseUsage()
        {
            sql.ParseUsage();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostResolveNegativeGaps()
        {
            sql.ResolveNegativeGaps();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostResolveOldDstats()
        {
            sql.ResolveOldDstats();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostCreateSubsListBySiteScope()
        {
            sql.CreateSubsListBySiteScope("2200GSH","CS");
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostPurgeErroneousResolves()
        {
            sql.PurgeErroneousResolves();
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostResetAndUpdateItemScopeResolved_ALL_Tables()
        {
            sql.ResetAndUpdateItemScopeResolved_ALL_Tables();
            return RedirectToPage("/Index");
        }


        public IActionResult OnPostUpdateBOIMsMidCycle()
        {
            sql.UpdateBOIMsMidCycle();
            return RedirectToPage("/Index");
        }
        

        public IActionResult OnPostAddSite()
        {
            sql.AddSite(AddSite_Site,AddSite_SiteName,AddSite_Company,AddSite_ScopesListFilePath,AddSite_Users_LocationsFilePath,AddSite_IC211File,AddSite_LocationSiteScopeFilePath,AddSite_UsageFilePath,AddSite_RQ201FilePath);
            return RedirectToPage("/Index");
        }

    }
}