using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using DataAccessLibrary;
using DataAccessLibrary.Models;
using RazorPagesUI;
using RazorPagesUI.PublicLibrary;
using DataAccessLibrary.WebAPI;

namespace RazorPagesUI.Pages
{
    public class IndexModel : PageModel

    {
        [BindProperty (SupportsGet = true)]
        public string SiteName { get; set; }

        [BindProperty (SupportsGet = true)]
        public string UserName { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty (SupportsGet = true)]
        public bool HasAccess { get; set; }

        [BindProperty(SupportsGet = true)]
        public int UserId { get; set; }

        [BindProperty]
        public string  newPasswordOne { get; set; }

        [BindProperty]
        public string newPasswordTwo { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ChangePasswordState { get; set; }

        [BindProperty (SupportsGet = true)]
        public string ItemId { get; set; }

        [BindProperty]
        public List<string> AllSitesList { get; set; }

        [BindProperty]
        public List<string> AllMBOsList { get; set; }

        [BindProperty]
        public List<string> AllRegionsList { get; set; }

        SQLCrud Sql = new SQLCrud(ConnectionString.GetConnectionString());

        [BindProperty]
        public bool UserHasItems { get; set; }

        [BindProperty (SupportsGet = true)]
        public int DisplayState { get; set; }

        [BindProperty]
        public bool IsOffLine { get; set; }

        private readonly ILogger<IndexModel> _logger;

        public ComicViewModel Comic { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }



        public async Task OnGet()
        {
            //ComicProcessor comicProcessor = new ComicProcessor();


            //ComicViewModel newComic = await comicProcessor.LoadComic(0);

            //await LoadComic(0);

            

            //Sql.OutputToLog("Comic Title: " + Comic.Title);
            //Sql.OutputToLog("Comic Day:" + Comic.Day);

            //Sql.DeployAllTables(@"D:\SupplyState\ASP-Learning-Project\ASP-Learning-Project\RazorPagesUI\TableDeployData\");

            IsOffLine = false;

            UserName = Sql.GetUserNameByUserId(UserId);

            AllRegionsList = Sql.GetAllRegions();

            

            if (HasAccess && !string.IsNullOrWhiteSpace(Sql.GetSiteByUser(UserId)))
            { UserHasItems = true; }

            if (!string.IsNullOrWhiteSpace(UserName))
            {
                //UserId = Sql.GetUserId(UserName);

                //Console.WriteLine(UserName + " " + UserId);


                Console.WriteLine(UserId);
               
            }


        }

        public async Task LoadComic(int comicNumber)
        {
            ComicProcessor comicProcessor = new ComicProcessor();

            ComicViewModel comic = await comicProcessor.LoadComic();

            Comic = comic;
        }

        public IActionResult OnPostLogin()
        {
            HasAccess = Sql.AccessGranted(UserName, Password);

            UserId = Sql.GetUserId(UserName);

            if (HasAccess && !string.IsNullOrWhiteSpace(Sql.GetSiteByUser(UserId)))
            {
                Sql.LogSignIn(UserId);
                return RedirectToPage("/All_Items", new { UserName = UserName, HasAccess = HasAccess, UserId = UserId });
            }
            else if(HasAccess)
            {
                Sql.LogSignIn(UserId);
                return RedirectToPage("/Critical_All_Items", new { UserName = UserName, HasAccess = HasAccess, UserId = UserId });
            }
            else
            {
                return RedirectToPage("/Index", new { UserName = UserName, HasAccess = HasAccess, UserId = UserId });
            }
            
            
        }

        public IActionResult OnPostChangePassword()
        {

           if(ChangePasswordState == 2)
            {
                bool success = Sql.ChangeUserPassword(UserName, Password, newPasswordOne);

                if (success)
                {
                    ChangePasswordState = 3;
                    HasAccess = false;
                }
                else
                {
                    ChangePasswordState = 4;
                }
            }

            Console.WriteLine(HasAccess);
            return RedirectToPage("/Index", new { UserName = UserName, HasAccess = HasAccess, UserId = UserId, ChangePasswordState = ChangePasswordState });
        }

        public IActionResult OnPostSignOut()
        {
            return RedirectToPage("/Index", new { UserName = "", HasAccess = false, UserId = 0, ChangePasswordState = 0 });
        }

        public string GetSiteName(string site)
        {
            return Sql.TranslateSiteToName(site);
        }

        public List<string> GetAllMBOsByRegion(string region)
        {
            return Sql.GetAllMBOsByRegion(region);
        }

        public List<string> GetAllSitesByMBO(string mbo)
        {
            return Sql.GetAllSitesByMBO(mbo);
        }

    }
}
