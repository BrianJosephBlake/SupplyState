using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Linq;
using DataAccessLibrary.Models;
using System.IO;
using OpenHtmlToPdf;

namespace DataAccessLibrary

{
    public class SQLCrud
    {
        private readonly string _connectionString;
        private SqlDataAccess db = new SqlDataAccess();

        public SQLCrud(string connectionString)
        {
            _connectionString = connectionString;
        }








        public IC211_Model GetNullIC211()
        {
            IC211_Model nullIC211 = new IC211_Model();

            nullIC211.Company = "";
            nullIC211.Description = "";
            nullIC211.Item_Number = "";
            nullIC211.ITL_ACTIVE_STATUS_XLT = "";
            nullIC211.Location_Code = "";
            nullIC211.PIV_VEN_ITEM = "";

            return nullIC211;
        }

        public IC211_Model GetFirstIC211FromSiteTable(string site, int displayState)
        {
            List<IC211_Model> resultsList = new List<IC211_Model>();

            IC211_Model IC211 = new IC211_Model();

            if (displayState == 0)
            {
                string sql = "select top 1 * from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number order by st.Item";
                resultsList = db.LoadData<IC211_Model, dynamic>(sql, new { }, _connectionString);
                //Console.WriteLine(resultsList.FirstOrDefault().Item_Number);
                return resultsList.First();
            }
            else if (displayState == 1)
            {
                string sql = "select top 1 * from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number where ([Resolved] <> @State or [Resolved] is null) order by st.Item";
                resultsList = db.LoadData<IC211_Model, dynamic>(sql, new { State = "Resolved" }, _connectionString);

                if (resultsList.Count > 0)
                {
                    return resultsList.First();
                }
                else
                {
                    return GetNullIC211();
                }

            }
            else if (displayState == 2)
            {
                string sql = "select top 1 * from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number where [Resolved] = @State order by st.Item";
                resultsList = db.LoadData<IC211_Model, dynamic>(sql, new { State = "Resolved" }, _connectionString);

                if (resultsList.Count > 0)
                {
                    return resultsList.First();
                }
                else
                {
                    return GetNullIC211();
                }
            }
            return GetNullIC211();


        }


        public string CheckItemsWithUsageExistbySiteScope(string site, string scope)
        {
            string output = "none";

            List<string> results = new List<string>();

            string sql = "select top 1 ic.[Item_Number] from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number inner join dbo.Scope_Locations_" + site + " sl on ic.Location_Code = sl.[Location] left join dbo.[ItemSiteScopeUsage] issu on st.[Item] = issu.[Item] where sl.Scope = @Scope and issu.[Site] = @Site and issu.[Scope] = @Scope order by issu.[Usage], st.Item";

            results = db.LoadData<string, dynamic>(sql, new { Site = site, Scope = scope }, _connectionString);

            if (results.Count > 0)
            {
                output = results[0];
            }

            return output;
        }

        public void UpdateItemSiteScopeUsageTablesSKUsPerIC211()
        {
            List<string> sites = GetAllSites();

            foreach (var site in sites)
            {
                List<string> scopes = GetAllScopesBySite(site);
                foreach (var scope in scopes)
                {
                    string sql = "IF OBJECT_ID('[dbo].[Usage_" + site + "_" + scope + "]', 'U') IS NOT NULL DROP TABLE[dbo].[Usage_" + site + "_" + scope + "]";
                    db.SaveData(sql, new { }, _connectionString);

                    sql = "create table dbo.[Usage_" + site + "_" + scope + "] ([Id] int primary key identity, [Item] nvarchar(50) not null, [Usage] nvarchar(50) null)";
                    db.SaveData(sql, new { }, _connectionString);

                    List<string> items = new List<string>();
                    sql = "select distinct [Item_Number] from dbo.[IC211_" + site + "]";
                    items = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

                    Thousandator itemSets = new Thousandator(items);

                    foreach (var set in itemSets.Sets)
                    {
                        sql = "insert into dbo.[Usage_" + site + "_" + scope + "] ([Item],[Usage]) values ";

                        foreach (var item in set)
                        {
                            sql += ("('" + item + "', 0),");
                        }

                        sql = sql.Remove(sql.Length - 1);

                        db.SaveData(sql, new { }, _connectionString);

                        //Console.WriteLine(sql);
                    }

                }
            }

        }

        public void ParseUsage()
        {
            List<string> sites = GetAllSites();

            foreach (var site in sites)
            {
                List<string> scopes = GetAllScopesBySite(site);

                string company = site.Substring(0, 4);

                scopes.Remove("");

                
                
                    foreach (var scope in scopes)
                    {
                        List<string> items = new List<string>();

                        string sql = "select [Item] from dbo.[Usage_" + site + "_" + scope + "]";

                        items = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

                        foreach (var item in items)
                        {
                            string usage;

                            sql = "select Sum(Cast([Received_Quantity] as int)) from dbo.[Master_Usage] mu inner join dbo.[LocationSiteScope] lss on mu.[Location] = lss.[Location] where mu.[Company] = @Company and mu.[Item_Number] = @Item and lss.[Site] = @Site and lss.[Scope] = @Scope";

                            usage = db.LoadData<string, dynamic>(sql, new { Company = company, Item = item, Site = site, Scope = scope }, _connectionString).First();

                            sql = "update dbo.[Usage_" + site + "_" + scope + "] set [Usage] = @Usage where [Item] = @Item";

                            db.SaveData(sql, new { Usage = usage, Item = item }, _connectionString);
                        }
                    }
                
            }
        }


        public void GenerateRandomPasswordForUserId(int userId)
        {
            string newPassword = GeneratePassword();

            string sql = "update dbo.users set [Password] = @Password where [Id] = @Id";

            db.SaveData(sql, new { Password = newPassword, Id = userId }, _connectionString);

            OutputToLog("New Password for " + GetUserNameByUserId(userId) + ": " + newPassword);
        }

        public void GenerateRandomPasswordsForAllUsers()
        {
            string sql = "select [Id] from dbo.users where [UserName] <> [Password]";

            List<int> userIds = new List<int>();

            userIds = db.LoadData<int, dynamic>(sql, new { }, _connectionString);

            foreach(var userId in userIds)
            {
                GenerateRandomPasswordForUserId(userId);
            }
        }

        public string GeneratePassword()
        {
            string newPassword = "";
            
            char[] newPasswordArray = new char[6];

            Random randomizer = new Random();

            //ascii range is 32 through 122

            for (int i = 0; i < newPasswordArray.Length; i++)
            {
                int newRandomInt = 0;

                while(newRandomInt == 0 || newRandomInt == 39 || newRandomInt == 44 || newRandomInt == 46 || newRandomInt == 40 || newRandomInt == 41 || newRandomInt == 42 || newRandomInt == 34 || newRandomInt == 47 || newRandomInt == 91 || newRandomInt == 92 || newRandomInt == 93 || newRandomInt == 58 || newRandomInt == 59 || newRandomInt == 61)
                {
                    newRandomInt = randomizer.Next(32, 122);
                }

                newPasswordArray[i] = (char)newRandomInt;
            }

            foreach(var character in newPasswordArray)
            {
                newPassword += character;
            }

            return newPassword;
        }

        
        public IC211_Model GetFirstIC211ByUserDisplayStateFromSiteTable(int userId, int displayState)
        {
            List<IC211_Model> resultsList = new List<IC211_Model>();

            IC211_Model IC211 = new IC211_Model();

            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);

            //Console.WriteLine("Site: " + site);
            //Console.WriteLine("{0},{1},{2}", userId, displayState,scope);

            if (string.IsNullOrWhiteSpace(site))
            {
                return GetNullIC211();
            }
            else if (displayState == 0)
            {

                Console.WriteLine("UserId: " + userId);
                Console.WriteLine("Site: " + site);
                Console.WriteLine("Scope: " + scope);

                if (scope != "ALL")
                {
                    string sql = "select top 1 ic.[Item_Number],ic.[PIV_VEN_ITEM],ic.[Description],ic.[Location_Code],ic.[Company],ic.[ITL_ACTIVE_STATUS_XLT],ic.[Site] from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number inner join dbo.Scope_Locations_" + site + " sl on ic.Location_Code = sl.[Location] inner join dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] uss on st.[Item] = uss.[Item] where sl.Scope = @Scope order by Cast(uss.Usage as int) desc, st.Item";
                    resultsList = db.LoadData<IC211_Model, dynamic>(sql, new { Site = site, Scope = scope }, _connectionString);
                    Console.WriteLine("Item: " + resultsList.FirstOrDefault().Item_Number);
                    return resultsList.First();
                }
                else
                {
                    string sql = "select top 1 ic.[Item_Number],ic.[PIV_VEN_ITEM],ic.[Description],ic.[Location_Code],ic.[Company],ic.[ITL_ACTIVE_STATUS_XLT],ic.[Site] from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number order by st.Item";
                    resultsList = db.LoadData<IC211_Model, dynamic>(sql, new { Site = site, Scope = scope }, _connectionString);
                    Console.WriteLine("Item: " + resultsList.FirstOrDefault().Item_Number);
                    return resultsList.First();
                }

            }
            else if (displayState == 1)
            {
                string sql;

                List<string> resultsStrings = new List<string>();

                sql = "select top 1 st.Item from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.[Item] = ic.[Item_Number] inner join dbo.[Scope_Locations_" + site + "] sl on ic.[Location_Code] = sl.[Location] inner join dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] uss on st.[Item] = uss.[Item] left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] ir on st.Item = ir.Item where sl.Scope = @Scope AND ir.Item is null order by Cast(uss.Usage as int) desc, st.Item";
                resultsStrings = db.LoadData<string, dynamic>(sql, new { Scope = scope }, _connectionString);

                resultsList.Add(new IC211_Model
                {
                    Item_Number = resultsStrings[0]
                });

                if (resultsList.Count > 0)
                {
                    return resultsList.First();
                }
                else
                {
                    return GetNullIC211();
                }

            }
            else if (displayState == 2)
            {

                string sql;

                List<string> resultsStrings = new List<string>();

                sql = "select top 1 st.Item from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.[Item] = ic.[Item_Number] inner join dbo.[Scope_Locations_" + site + "] sl on ic.[Location_Code] = sl.[Location] inner join dbo.[Usage_" + site + "_" + scope + "] uss on st.[Item] = uss.[Item] left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] ir on st.Item = ir.Item where sl.Scope = @Scope AND ir.Item is not null order by Cast(uss.Usage as int) desc, st.Item";
                resultsStrings = db.LoadData<string, dynamic>(sql, new { Scope = scope }, _connectionString);

                resultsList.Add(new IC211_Model
                {
                    Item_Number = resultsStrings[0]
                });


                if (resultsList.Count > 0)
                {
                    return resultsList.First();
                }
                else
                {
                    return GetNullIC211();
                }
            }

            return GetNullIC211();

        }

        public IC211_Model Get_First_IC211_ByUserDisplayState(int userId, int displayState)
        {
            string site = GetSiteByUser(userId);

            string sql = "select * from dbo.[IC211_" + site + "] ic inner join dbo.[" + site + "] boim on ic.Item_Number = boim.Item inner join dbo.Users_Locations_" + site + " ul on ic.Location_Code = ul.[Location] where ul.UserId = @UserID order by ic.Item_Number";


            List<IC211_Model> resultsList = new List<IC211_Model>();

            IC211_Model IC211 = new IC211_Model();

            resultsList = db.LoadData<IC211_Model, dynamic>(sql, new { UserId = userId }, _connectionString);


            if (resultsList.Count > 0)
            {
                if (displayState == 0)
                {
                    return resultsList.First();
                }
                else if (displayState == 1)
                {
                    bool foundOne = false;
                    int i = 0;

                    while (!foundOne)
                    {
                        foundOne = (!this.IsResolved(resultsList[i].Item_Number, site) && this.IsItemInScopeByUser(userId, resultsList[i].Item_Number));
                        i++;
                    }

                    return resultsList[i - 1];

                }
                else if (displayState == 2)
                {
                    bool foundOne = false;
                    int i = 0;

                    while (!foundOne)
                    {
                        foundOne = (this.IsResolved(resultsList[i].Item_Number, site) && this.IsItemInScopeByUser(userId, resultsList[i].Item_Number));
                        i++;
                    }

                    return resultsList[i - 1];
                }

            }

            return GetNullIC211();

        }

        public IC211_Model GetIC211FromSiteTable(string item, string site)
        {
            string sql = "select top 1 * from dbo.[IC211_" + site + "] ic inner join dbo.[" + site + "] st on ic.Item_Number = st.Item where st.Item = @Item";

            List<IC211_Model> resultsList = new List<IC211_Model>();

            resultsList = db.LoadData<IC211_Model, dynamic>(sql, new { Item = item }, _connectionString);

            if (resultsList.Count > 0)
            {
                return resultsList.FirstOrDefault();
            }
            else
            {
                return GetNullIC211();
            }
        }

        public IC211_Model Get_IC211_ByItemSite(string item, string site)
        {
            string sql = "select * from dbo.[IC211_" + site + "] ic inner join dbo.BackOrderItemMaster bo on ic.Item_Number = bo.Item where ic.Item_Number = @Item and ic.Site = @Site";

            List<IC211_Model> resultsList = new List<IC211_Model>();

            resultsList = db.LoadData<IC211_Model, dynamic>(sql, new { Item = item, Site = site }, _connectionString);

            if (resultsList.Count > 0)
            {
                return resultsList.FirstOrDefault();
            }
            else
            {
                return GetNullIC211();
            }

        }

        public bool ItemHasNotes(string itemId)
        {
            bool itemHasNotes = false;

            List<Notes_Model> resultsList = new List<Notes_Model>();

            string sql = "select top 1 * from dbo.notes where [Item] = @ItemId";

            resultsList = db.LoadData<Notes_Model, dynamic>(sql, new { ItemId = itemId }, _connectionString);

            if (resultsList.Count > 0)
            { itemHasNotes = true; }

            return itemHasNotes;
        }

        public bool HasNotesHistory(string searchKey)
        {
            bool hasNotesHistory = false;

            List<Notes_Model> notesList = new List<Notes_Model>();

            string sql = "select [Item],[DATE_CREATED],[NOTE],[Site],[UserId] from dbo.NOTES where [Item] like @SearchKey or [DATE_CREATED] like @SearchKey or [NOTE] like @SearchKey or [Site] like @SearchKey or [UserId] like @SearchKey order by Cast([DATE_CREATED] as datetime) desc";

            notesList = db.LoadData<Notes_Model, dynamic>(sql, new { SearchKey = searchKey }, _connectionString);

            if (notesList.Count > 0)
            { hasNotesHistory = true; }

            return hasNotesHistory;
        }

        public List<Notes_Model> GetNotesHistory(string searchKey)
        {
            List<Notes_Model> output = new List<Notes_Model>();

            string sql = "select [Item],[DATE_CREATED],[NOTE],[Site],[UserId] from dbo.NOTES where [Item] like @SearchKey or [DATE_CREATED] like @SearchKey or [NOTE] like @SearchKey or [Site] like @SearchKey or [UserId] like @SearchKey order by Cast([DATE_CREATED] as datetime) desc";

            output = db.LoadData<Notes_Model, dynamic>(sql, new { SearchKey = searchKey }, _connectionString);

            if (output.Count == 0)
            {
                output.Add(GetNullNote());
            }

            return output;
        }

        public List<Notes_Model> GetTop100Notes()
        {
            List<Notes_Model> output = new List<Notes_Model>();

            string sql = "select top 100 [Item],[DATE_CREATED],[NOTE],[Site],[UserId] from dbo.NOTES";

            output = db.LoadData<Notes_Model, dynamic>(sql, new { }, _connectionString);

            if (output.Count == 0)
            {
                output.Add(GetNullNote());
            }

            return output;
        }

        public List<ItemList_Model> SmartSearch_SiteTable(string searchKey, int userId)
        {
            List<ItemList_Model> output = new List<ItemList_Model>();

            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);

            string sql = "select distinct st.[Item],st.[MfrNum],st.[Description],st.[ReleaseDate],st.[Source],st.[StockStatus],ir.Item as [Resolved] from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.[Item] = ic.[Item_Number] inner join dbo.[Scope_Locations_" + site + "] sl on ic.[Location_Code] = sl.[Location] left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] ir on st.[Item] = ir.[Item] where sl.[Scope] = @Scope and (st.[Item] like @SearchKey or st.[MfrNum] like @SearchKey or st.[Description] like @SearchKey or ic.[Description] like @SearchKey or st.[ReleaseDate] like @SearchKey or st.[StockStatus] like @SearchKey or [Resolved] like @SearchKey or st.[Source] like @SearchKey) order by [Item]";

            output = db.LoadData<ItemList_Model, dynamic>(sql, new { SearchKey = "%" + searchKey + "%", Scope = scope }, _connectionString);

            return output;
        }

        public List<ItemList_Model> AllOpenItemsByUserId(int userId)
        {
            List<ItemList_Model> output = new List<ItemList_Model>();

            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);

            string sql = "select ssub.[Id], st.[Item], st.[MfrNum], st.[Description], st.[ReleaseDate], st.[StockStatus], st.[Resolved] from dbo.[" + site + "] st inner join dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] ssub on st.[Item] = ssub.[Item] left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] isr on st.[Item] = isr.[Item] where isr.[Item] is null order by ssub.[Id]";

            output = db.LoadData<ItemList_Model, dynamic>(sql, new { }, _connectionString);

            return output;
        }

        public List<ItemList_Model> AllResolvedItemsByUserId(int userId)
        {
            List<ItemList_Model> output = new List<ItemList_Model>();

            Console.WriteLine("Hit AllResolvedMethod");

            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);

            string sql = "select ssub.[Id], st.[Item], st.[MfrNum], st.[Description], st.[ReleaseDate], st.[StockStatus], st.[Resolved] from dbo.[" + site + "] st inner join dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] ssub on st.[Item] = ssub.[Item] left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] isr on st.[Item] = isr.[Item] where isr.[Item] is not null order by ssub.[Id]";

            output = db.LoadData<ItemList_Model, dynamic>(sql, new { }, _connectionString);

            return output;
        }

        public List<ItemList_Model> SmartSearch_BOIM(string searchKey)
        {
            List<ItemList_Model> output = new List<ItemList_Model>();

            string sql = "select [Item],[MfrNum],[Description],[ReleaseDate],[Source],[StockStatus] from dbo.[BackOrderItemMaster] where [Item] like @SearchKey or [MfrNum] like @SearchKey or [Description] like @SearchKey or [ReleaseDate] like @SearchKey or [StockStatus] like @SearchKey or [Source] like @SearchKey order by [Item]";

            output = db.LoadData<ItemList_Model, dynamic>(sql, new { SearchKey = "%" + searchKey + "%" }, _connectionString);

            return output;
        }

        public List<IC211_Model> SmartSearch_AllItems(string searchKey)
        {
            List<IC211_Model> output = new List<IC211_Model>();

            string sql = "select distinct [Item_Number],[PIV_VEN_ITEM],[Description],[ITL_ACTIVE_STATUS_XLT] from dbo.IC211 where ([Item_Number] like @SearchKey or [PIV_VEN_ITEM] like @SearchKey or [Description] like @SearchKey or [Location_Code] like @SearchKey or [Company] like @SearchKey or [ITL_ACTIVE_STATUS_XLT] like @SearchKey or [Site] like @SearchKey) order by [Item_Number]";

            output = db.LoadData<IC211_Model, dynamic>(sql, new { SearchKey = "%" + searchKey + "%" }, _connectionString);

            return output;
        }


        public DateTime CalculateSnoozeDate(int snoozeWeeks = 1)
        {
            DateTime wednesdayThisWeek = DateTime.Today;

            while (wednesdayThisWeek.DayOfWeek != DayOfWeek.Wednesday)
            {
                wednesdayThisWeek = wednesdayThisWeek.AddDays(-1);
                //SQL.OutputToLog(wednesdayThisWeek.ToString());
            }

            DateTime snoozeDate = wednesdayThisWeek.AddDays(snoozeWeeks * 7);

            OutputToLog(wednesdayThisWeek.ToString());
            OutputToLog(snoozeDate.ToString() + ", " + snoozeDate.DayOfWeek.ToString());

            return snoozeDate;
        }


        public DateTime GetSnoozeDateByItemSiteScope(string item, string site, string scope)
        {
            int itemInt = Int32.Parse(item);

            DateTime snoozeDate = new DateTime();

            snoozeDate = DateTime.Parse("1/1/2000");

            string sql = "select top 1 [SnoozeTil] from dbo.[Snooze] where [Item] = @ItemInt and [Site] = @Site and [Scope] = @Scope";

            List<DateTime> results = db.LoadData<DateTime, dynamic>(sql, new { ItemInt = itemInt, Site = site, Scope = scope }, _connectionString);

            if(results.Count > 0)
            {
                snoozeDate = results[0];  
            }

            return snoozeDate;
        }


        public void SnoozeItembySiteScope(int userId, string item,string site, string scope,int snoozeWeeks)
        {
            DateTime snoozeDate = CalculateSnoozeDate(snoozeWeeks);

            int itemInt = Int32.Parse(item);

            string sql = "select [Id] from dbo.[Snooze] where [Item] = @ItemInt and [Site] = @Site and [Scope] = @Scope";

            List<int> results = db.LoadData<int, dynamic>(sql, new { ItemInt = itemInt, Site = site, Scope = scope }, _connectionString);

            foreach(var id in results)
            {
                sql = "delete dbo.[Snooze] where [Id] = @Id";

                db.SaveData(sql, new { Id = id }, _connectionString);
            }

            sql = "insert into dbo.[Snooze] ([Item],[SnoozeTil],[UserId],[Site],[Scope]) values " +
                "(@ItemInt, @SnoozeDate, @UserId, @Site, @Scope)";

            db.SaveData(sql, new { ItemInt = itemInt, SnoozeDate = snoozeDate, UserId = userId, Scope = scope, Site = site }, _connectionString);

        }

        public List<ItemList_Model> GetAllItemsByUserFromSiteTable(int userId)
        {
            List<ItemList_Model> results = new List<ItemList_Model>();

            string sql;
            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);

            Console.WriteLine("Get All Items: " + userId + " " + site);

            if (scope != "ALL")
            {
                sql = "select ssub.[Id], st.[Item], st.[MfrNum], st.[Description], st.[ReleaseDate], st.[StockStatus], st.[Resolved] from dbo.[" + site + "] st inner join dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] ssub on st.[Item] = ssub.[Item] order by ssub.[Id]";
            }
            else
            {
                sql = "select distinct st.[Item],st.[MfrNum],st.[Description],st.[ReleaseDate],st.[StockStatus],st.[Resolved] from dbo.[" + site + "] st";
            }


            results = db.LoadData<ItemList_Model, dynamic>(sql, new { Scope = scope }, _connectionString);

            if (results.Count < 1)
            {
                ItemList_Model nullItem = new ItemList_Model();

                nullItem.Description = "";
                nullItem.Id = 0;
                nullItem.Item = "";
                nullItem.MfrNum = "";
                nullItem.ReleaseDate = "";
                nullItem.Resolved = "";
                nullItem.StockStatus = "";

                results.Add(nullItem);
            }

            return results;
        }

        public IC211_Model GetIC211ByUserFromSiteTable(int userId, string item)
        {
            string sql;
            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);

            if (scope != "ALL")
            {
                sql = "select top 1 ic.[Item_Number],ic.[PIV_VEN_ITEM],ic.[Description],ic.[Location_Code],ic.[Company],ic.[ITL_ACTIVE_STATUS_XLT],ic.[Site] from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number inner join dbo.Scope_Locations_" + site + " sl on ic.Location_Code = sl.[Location] where sl.Scope = @Scope and ic.[Item_Number] = @Item order by ic.[Item_Number]";
            }
            else
            {
                sql = "select top 1 ic.[Item_Number],ic.[PIV_VEN_ITEM],ic.[Description],ic.[Location_Code],ic.[Company],ic.[ITL_ACTIVE_STATUS_XLT],ic.[Site] from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number where ic.[Item_Number] = @Item order by ic.[Item_Number]";
            }

            List<IC211_Model> resultsList = new List<IC211_Model>();

            resultsList = db.LoadData<IC211_Model, dynamic>(sql, new { Site = site, Item = item, Scope = scope }, _connectionString);

            if (resultsList.Count > 0)
            {
                return resultsList.FirstOrDefault();
            }
            else
            {
                return GetNullIC211();
            }
        }
        




        public IC211_Model GetPrevAll_IC211_ByItemUserFromSiteTable(int userId, string item)
        {
            IC211_Model output = new IC211_Model();
            List<string> results = new List<string>();
            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);

            string sql;

            if (scope != "ALL")
            {
                sql = "select distinct ic.Item_Number from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number inner join dbo.Scope_Locations_" + site + " sl on sl.Location = ic.Location_Code where sl.Scope = @Scope and ic.Item_Number < @Item order by ic.Item_Number desc";
            }
            else
            {
                sql = "select distinct ic.Item_Number from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number where ic.Item_Number < @Item order by ic.Item_Number desc";
            }

            results = db.LoadData<string, dynamic>(sql, new { Scope = scope, Item = item }, _connectionString);

            if (results.Count > 0)
            {
                output = GetIC211ByUserFromSiteTable(userId, results[0]);
            }
            else
            {
                output = GetIC211ByUserFromSiteTable(userId, item);
            }

            return output;
        }

        public IC211_Model GetNext_All_IC211ByItemUserFromSiteTable(string item, int userId)
        {
            IC211_Model output = new IC211_Model();
            List<string> results = new List<string>();
            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);
            string sql;

            Console.WriteLine(userId);
            Console.WriteLine(site);

            if (scope != "ALL")
            {
                sql = "select distinct ic.Item_Number from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number inner join dbo.Scope_Locations_" + site + " sl on sl.Location = ic.Location_Code where sl.Scope = @Scope and ic.Item_Number > @Item order by ic.Item_Number";
            }
            else
            {
                sql = "select distinct ic.Item_Number from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number where ic.Item_Number > @Item order by ic.Item_Number";
            }


            results = db.LoadData<string, dynamic>(sql, new { Scope = scope, Item = item }, _connectionString);

            if (results.Count > 0)
            {
                output = GetIC211ByUserFromSiteTable(userId, results[0]);
            }
            else
            {
                output = GetIC211ByUserFromSiteTable(userId, item);
            }

            return output;
        }





        public string GetPreviousResolvedItemBySiteAndScope(string site, string scope, string itemId)
        {
            string output = "";
            List<string> results = new List<string>();

            string sql = "select distinct st.Item from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.[Item] = ic.[Item_Number] inner join dbo.[Scope_Locations_" + site + "] sl on ic.[Location_Code] = sl.[Location] left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] ir on st.Item = ir.Item where sl.Scope = @Scope AND st.Item < @ItemId AND ir.Item is not null order by st.Item desc";

            results = db.LoadData<string, dynamic>(sql, new { Scope = scope, ItemId = itemId }, _connectionString);

            if (results.Count > 0)
            {
                output = results[0];
            }

            return output;
        }

        public string GetPreviousOpenItemBySiteAndScope(string site, string scope, string itemId)
        {
            string output = "";
            List<string> results = new List<string>();

            string sql = "select distinct st.Item from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.[Item] = ic.[Item_Number] inner join dbo.[Scope_Locations_" + site + "] sl on ic.[Location_Code] = sl.[Location] left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] ir on st.Item = ir.Item where sl.Scope = @Scope AND st.Item < @ItemId AND ir.Item is null order by st.Item desc";

            results = db.LoadData<string, dynamic>(sql, new { Scope = scope, ItemId = itemId }, _connectionString);

            if (results.Count > 0)
            {
                output = results[0];
            }

            return output;
        }



        public IC211_Model GetPrevOpen_IC211_ByUserFromSiteTable(int userId, string item)
        {
            IC211_Model output = new IC211_Model();
            List<string> results = new List<string>();
            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);
            string sql;

            if (scope != "ALL")
            {
                sql = "select distinct ic.Item_Number from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number inner join dbo.Scope_Locations_" + site + " sl on sl.Location = ic.Location_Code where sl.Scope = @Scope and (st.Resolved <> @State or st.Resolved is null) and ic.Item_Number < @Item order by ic.Item_Number desc";
            }
            else
            {
                sql = "select distinct ic.Item_Number from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number where (st.Resolved <> @State or st.Resolved is null) and ic.Item_Number < @Item order by ic.Item_Number desc";
            }



            results = db.LoadData<string, dynamic>(sql, new { Item = item, Scope = scope, State = "Resolved" }, _connectionString);

            if (results.Count > 0)
            {
                output = GetIC211ByUserFromSiteTable(userId, results[0]);
            }
            else
            {
                output = GetIC211ByUserFromSiteTable(userId, item);
            }

            return output;

        }

        public string GetNextResolvedItemBySiteAndScope(string site, string scope, string itemId)
        {
            string output = "";
            List<string> results = new List<string>();

            string sql = "select distinct st.Item from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.[Item] = ic.[Item_Number] inner join dbo.[Scope_Locations_" + site + "] sl on ic.[Location_Code] = sl.[Location] left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] ir on st.Item = ir.Item where sl.Scope = @Scope AND st.Item > @ItemId AND ir.Item is not null order by st.Item";

            results = db.LoadData<string, dynamic>(sql, new { Scope = scope, ItemId = itemId }, _connectionString);

            if (results.Count > 0)
            {
                output = results[0];
            }

            return output;
        }

        public string GetNextOpenItemBySiteAndScope(string site, string scope, string itemId)
        {
            string output = "";
            List<string> results = new List<string>();

            string sql = "select distinct st.Item from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.[Item] = ic.[Item_Number] inner join dbo.[Scope_Locations_" + site + "] sl on ic.[Location_Code] = sl.[Location] left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] ir on st.Item = ir.Item where sl.Scope = @Scope AND st.Item > @ItemId AND ir.Item is null order by st.Item";

            results = db.LoadData<string, dynamic>(sql, new { Scope = scope, ItemId = itemId }, _connectionString);

            if (results.Count > 0)
            {
                output = results[0];
            }

            return output;
        }

        public IC211_Model GetNextOpen_IC211_ByUserFromSiteTable(int userId, string item)
        {
            IC211_Model output = new IC211_Model();
            List<string> results = new List<string>();
            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);
            string sql;


            if (scope != "ALL")
            {
                sql = "select distinct ic.Item_Number from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number inner join dbo.Scope_Locations_" + site + " sl on sl.Location = ic.Location_Code where sl.Scope = @Scope and (st.Resolved <> @State or st.Resolved is null) and ic.Item_Number > @Item order by ic.Item_Number";
            }
            else
            {
                sql = "select distinct ic.Item_Number from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number where (st.Resolved <> @State or st.Resolved is null) and ic.Item_Number > @Item order by ic.Item_Number";

            }

            results = db.LoadData<string, dynamic>(sql, new { Item = item, Scope = scope, State = "Resolved" }, _connectionString);

            if (results.Count > 0)
            {
                output = GetIC211ByUserFromSiteTable(userId, results[0]);
            }
            else
            {
                output = GetIC211ByUserFromSiteTable(userId, item);
            }

            return output;
        }

        public IC211_Model GetPrevResolved_IC211_ByUserFromSiteTable(int userId, string item)
        {
            IC211_Model output = new IC211_Model();
            List<string> results = new List<string>();
            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);
            string sql;

            if (scope != "ALL")
            {
                sql = "select distinct ic.Item_Number from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number inner join dbo.Scope_Locations_" + site + " sl on sl.Location = ic.Location_Code where sl.Scope = @Scope and st.Resolved = @State and ic.Item_Number < @Item order by ic.Item_Number desc";
            }
            else
            {
                sql = "select distinct ic.Item_Number from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number where st.Resolved = @State and ic.Item_Number < @Item order by ic.Item_Number desc";
            }


            results = db.LoadData<string, dynamic>(sql, new { Item = item, Scope = scope, State = "Resolved" }, _connectionString);

            if (results.Count > 0)
            {
                output = GetIC211ByUserFromSiteTable(userId, results[0]);
            }
            else
            {
                output = GetIC211ByUserFromSiteTable(userId, item);
            }

            return output;
        }

        public IC211_Model GetNextResolved_IC211_ByUserFromSiteTable(int userId, string item)
        {
            IC211_Model output = new IC211_Model();
            List<string> results = new List<string>();
            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);
            string sql;

            if (scope != "ALL")
            {
                sql = "select distinct ic.Item_Number from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number inner join dbo.Scope_Locations_" + site + " sl on sl.Location = ic.Location_Code where sl.Scope = @Scope and st.Resolved = @State and ic.Item_Number > @Item order by ic.Item_Number";
            }
            else
            {
                sql = "select distinct ic.Item_Number from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number where st.Resolved = @State and ic.Item_Number > @Item order by ic.Item_Number";
            }

            results = db.LoadData<string, dynamic>(sql, new { Item = item, Scope = scope, State = "Resolved" }, _connectionString);

            if (results.Count > 0)
            {
                output = GetIC211ByUserFromSiteTable(userId, results[0]);
            }
            else
            {
                output = GetIC211ByUserFromSiteTable(userId, item);
            }

            return output;
        }








        public void CreateTable(string tableName, string filePath, char delimiter)
        {
            string sql;
            List<string> rows = new List<string>();
            List<string> fieldNames = new List<string>();

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            sql = "IF OBJECT_ID('[dbo].[" + tableName + "]', 'U') IS NOT NULL DROP TABLE[dbo].[" + tableName + "]";
            db.SaveData(sql, new { }, _connectionString);

            sql = "create table [dbo].[" + tableName + "] (";

            foreach (var field in rows[0].Split(delimiter))
            {
                sql += "[" + field + "] NVARCHAR(50) NULL,";
                fieldNames.Add(field);
            }

            sql = sql.Remove(sql.Length - 1);
            sql += ")";
            db.SaveData(sql, new { }, _connectionString);

            Console.WriteLine(sql);

            rows.RemoveAt(0);

            sql = "insert into dbo.[" + tableName + "] (";
            foreach (var field in fieldNames)
            {
                sql += "[" + field + "],";
            }

            sql = sql.Remove(sql.Length - 1);
            sql += ") values ";

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                foreach (var row in set)
                {
                    sql += "(";
                    foreach (var field in row.Split(delimiter))
                    {
                        sql += "'" + field + "',";
                    }

                    sql = sql.Remove(sql.Length - 1);
                    sql += "),";
                }

                sql = sql.Remove(sql.Length - 1);
            }

            Console.WriteLine(sql);
            db.SaveData(sql, new { }, _connectionString);
        }


        public void NotesCruncher(string filePath, char delimiter, int itemField, List<int> noteFields)
        {
            string sql;

            List<string> rows = new List<string>();
            List<int> noteFieldsSorted = new List<int>();

            foreach (var field in noteFields)
            { noteFieldsSorted.Add(field); }

            noteFieldsSorted.Sort();

            int maxFieldPosition = noteFieldsSorted.Last();

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    row.Replace((char)10, '.');
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                sql = "insert into dbo.Notes ([Item],[Note],[Site],[UserId]) values ";

                foreach (var row in set)
                {
                    if (row.Split(delimiter).Length >= maxFieldPosition)
                    {
                        List<string> values = row.Split(delimiter).ToList();

                        string item = values[itemField - 1];
                        string note = "";

                        foreach (var noteField in noteFields)
                        {
                            if (noteField > 0)
                            { note += " - " + values[noteField - 1]; }

                        }

                        sql += "('" + item + "','" + note + "','Test',18),";
                    }

                }

                sql = sql.Remove(sql.Length - 1);
                Console.WriteLine(sql);
                db.SaveData(sql, new { }, _connectionString);
            }

        }

        public void IngestAndStageBackOrderData(string filePath, char delimiter, List<int> fields, string source, string region)
        {
            string sql = "";
            List<string> rows = new List<string>();
            List<string> fieldNames = new List<string>();

            List<int> fieldsSorted = new List<int>();

            foreach (var field in fields)
            {
                fieldsSorted.Add(field);
            }

            fieldsSorted.Sort();

            int maxFieldPosition = fieldsSorted.Last();

            for (int i = 0; i < fields.Count - 1; i++)
            {
                Console.WriteLine("Field" + i + ": " + fields[i]);
            }

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                sql = "insert into dbo.[BackOrderItemMaster_Staging] ([Item],[MfrNum],[Description],[StockOutDate],[ReleaseDate],[GapDays],[ReasonCode],[StockStatus],[Source],[Region]) values ";

                foreach (var row in set)
                {
                    if (row.Split(delimiter).Length >= maxFieldPosition)
                    {
                        string mfrNum;
                        string description;
                        string stockOutDate;
                        string releaseDate;
                        string gapDays;
                        string reasonCode;
                        string stockStatus;

                        string item = row.Split(delimiter)[fields[0] - 1];

                        if (fields[1] > 0)
                        {
                            mfrNum = row.Split(delimiter)[fields[1] - 1];
                        }
                        else
                        {
                            mfrNum = "";
                        }


                        if (fields[2] > 0)
                        {
                            description = row.Split(delimiter)[fields[2] - 1];
                        }
                        else
                        {
                            description = "";
                        }


                        if (fields[3] > 0)
                        {
                            stockOutDate = row.Split(delimiter)[fields[3] - 1];
                        }
                        else
                        {
                            stockOutDate = "";
                        }


                        if (fields[4] > 0)
                        {
                            releaseDate = row.Split(delimiter)[fields[4] - 1];
                        }
                        else
                        {
                            releaseDate = "";
                        }



                        if (fields[5] > 0)
                        {
                            gapDays = row.Split(delimiter)[fields[5] - 1];
                        }
                        else
                        {
                            gapDays = "";
                        }


                        if (fields[6] > 0)
                        {
                            reasonCode = row.Split(delimiter)[fields[6] - 1];
                        }
                        else
                        {
                            reasonCode = "";
                        }


                        if (fields[7] > 0)
                        {
                            stockStatus = row.Split(delimiter)[fields[7] - 1];
                        }
                        else
                        {
                            stockStatus = "";
                        }

                        sql += "('" + item + "','" + mfrNum + "','" + description + "','" + stockOutDate + "','" + releaseDate + "','" + gapDays + "','" + reasonCode + "','" + stockStatus + "','" + source + "','" + region + "'),";
                    }
                }

                sql = sql.Remove(sql.Length - 1);
                Console.WriteLine(sql);
                db.SaveData(sql, new { }, _connectionString);
            }
        }

        public string GetRegionByMBO(string mbo)
        {
            string sql = "select distinct [Region] from dbo.MBO_Region where [MBO] = @MBO";
            string region = "Region not found";

            List<string> results = new List<string>();

            results = db.LoadData<string, dynamic>(sql, new {MBO = mbo }, _connectionString);

            if(results.Count > 0)
            {
                region = results[0];
            }

            return region;
        }

        

        public void FormatBackOrderDataByPriority()
        {
            List<string> duplicateItems = new List<string>();

            List<string> regions = new List<string>();

            
            
            string sql = "delete dbo.BackOrderItemMaster_Staging where [GapDays] like '%-%'";

            db.SaveData(sql, new { }, _connectionString);

            //sql = "delete from dbo.BackOrderItemMaster_Staging where Cast([StockOutDate] as datetime) < @DstatDateThreshold";

            //DateTime dstatDateThreshold = DateTime.Today.AddDays(-15);

            //db.SaveData(sql, new { DstatDateThreshold = dstatDateThreshold }, _connectionString);



            sql = "select distinct [Region] from dbo.BackOrderItemMaster_Staging";

            regions = db.LoadData<string, dynamic>(sql, new { }, _connectionString);    

            foreach(var region in regions)
            {
                sql = "select [Item] from dbo.BackOrderItemMaster_Staging where [Region] = @Region group by [Item] having Count([Item]) > 1";

                duplicateItems = db.LoadData<string, dynamic>(sql, new {Region =  region}, _connectionString);

                foreach (var item in duplicateItems)
                {

                    List<NullCount_Model> nulls = new List<NullCount_Model>();
                    List<BackOrderItemMaster_Model> results = new List<BackOrderItemMaster_Model>();
                    List<string> sources = new List<string>();

                    string sourceString = "";
                    
                    sql = "select * from dbo.BackOrderItemMaster_Staging where [Item] = @Item and [Region] = @Region";
                    
                    results = db.LoadData<BackOrderItemMaster_Model, dynamic>(sql, new { Item = item, Region = region }, _connectionString);

                    foreach (var result in results)
                    {
                        int nullCount = 0;
                        int resultId = result.Id;

                        if (string.IsNullOrWhiteSpace(result.Item) || result.Item == "")
                        {
                            nullCount++;
                        }

                        if (string.IsNullOrWhiteSpace(result.MfrNum) || result.MfrNum == "")
                        {
                            nullCount++;
                        }

                        if (string.IsNullOrWhiteSpace(result.Description) || result.Description == "")
                        {
                            nullCount++;
                        }

                        if (string.IsNullOrWhiteSpace(result.StockOutDate) || result.StockOutDate == "")
                        {
                            nullCount++;
                        }

                        if (string.IsNullOrWhiteSpace(result.ReleaseDate) || result.ReleaseDate == "")
                        {
                            nullCount++;
                        }

                        if (string.IsNullOrWhiteSpace(result.GapDays) || result.GapDays == "")
                        {
                            nullCount++;
                        }

                        if (string.IsNullOrWhiteSpace(result.ReasonCode) || result.ReasonCode == "")
                        {
                            nullCount++;
                        }

                        if (string.IsNullOrWhiteSpace(result.StockStatus) || result.StockStatus == "")
                        {
                            nullCount++;
                        }

                        if (!sources.Contains(result.Source))
                        {
                            sources.Add(result.Source);
                        }



                        nulls.Add(new NullCount_Model { ItemId = resultId, NullCount = nullCount });
                    }

                    NullCount_Model maxNull = new NullCount_Model();

                    if(nulls.Count > 0)
                    {
                        maxNull = nulls.OrderByDescending(NullCount_Model => NullCount_Model.NullCount).Last();
                        Console.WriteLine("Item: " + item + ", MaxNull: " + maxNull.NullCount + ", Id: " + maxNull.ItemId);

                        sql = "delete dbo.BackOrderItemMaster_Staging where [Item] = @Item and [Id] <> @Id and [Region] = @Region";
                        db.SaveData(sql, new { Item = item, Id = maxNull.ItemId, Region = region }, _connectionString);

                    }

                    if (sources.Count > 1)
                    {
                        sourceString = sources[0];

                        for (int i = 1; i < sources.Count - 1; i++)
                        {
                            sourceString += ("/" + sources[i]);
                        }

                        Console.WriteLine("SourceString = {0}", sourceString);


                        if (nulls.Count > 0)
                        {
                            sql = "update dbo.backorderitemmaster_staging set [Source] = @SourceString where [Id] = @MaxNullId and [Region] = @Region";
                            db.SaveData(sql, new { SourceString = sourceString, MaxNullId = maxNull.ItemId, Region = region }, _connectionString);
                        }
                        else
                        {
                            sql = "update dbo.backorderitemmaster_staging set [Source] = @SourceString where [Item] = @Item and [Region] = @Region";
                            db.SaveData(sql, new { SourceString = sourceString, Item = item, Region = region }, _connectionString);

                        }
                    }
                }


            }
        }

        public void FormatBOIMByPriority()
        {
            List<string> duplicateItems = new List<string>();

            string sql = "select [Item] from dbo.BackOrderItemMaster group by [Item] having Count([Item]) > 1";

            duplicateItems = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

            foreach (var item in duplicateItems)
            {

                List<NullCount_Model> nulls = new List<NullCount_Model>();
                List<BackOrderItemMaster_Model> results = new List<BackOrderItemMaster_Model>();

                sql = "select * from dbo.BackOrderItemMaster where [Item] = @Item";
                results = db.LoadData<BackOrderItemMaster_Model, dynamic>(sql, new { Item = item }, _connectionString);

                foreach (var result in results)
                {
                    int nullCount = 0;
                    int resultId = result.Id;

                    if (string.IsNullOrWhiteSpace(result.Item) || result.Item == "")
                    {
                        nullCount++;
                    }

                    if (string.IsNullOrWhiteSpace(result.MfrNum) || result.MfrNum == "")
                    {
                        nullCount++;
                    }

                    if (string.IsNullOrWhiteSpace(result.Description) || result.Description == "")
                    {
                        nullCount++;
                    }

                    if (string.IsNullOrWhiteSpace(result.StockOutDate) || result.StockOutDate == "")
                    {
                        nullCount++;
                    }

                    if (string.IsNullOrWhiteSpace(result.ReleaseDate) || result.ReleaseDate == "")
                    {
                        nullCount++;
                    }

                    if (string.IsNullOrWhiteSpace(result.GapDays) || result.GapDays == "")
                    {
                        nullCount++;
                    }

                    if (string.IsNullOrWhiteSpace(result.ReasonCode) || result.ReasonCode == "")
                    {
                        nullCount++;
                    }

                    if (string.IsNullOrWhiteSpace(result.StockStatus) || result.StockStatus == "")
                    {
                        nullCount++;
                    }

                    if (string.IsNullOrWhiteSpace(result.Source) || result.Source == "")
                    {
                        nullCount++;
                    }

                    nulls.Add(new NullCount_Model { ItemId = resultId, NullCount = nullCount });
                }

                var maxNull = nulls.OrderByDescending(NullCount_Model => NullCount_Model.NullCount).Last();

                Console.WriteLine("Item: " + item + ", MaxNull: " + maxNull.NullCount + ", Id: " + maxNull.ItemId);

                sql = "delete from dbo.BackOrderItemMaster where [Item] = @Item and [Id] <> @Id";
                db.SaveData(sql, new { Item = item, Id = maxNull.ItemId }, _connectionString);

            }
        }


        public void OopsNothingAutoResolved()
        {
            List<string> sites = GetAllSites();

            sites.Remove("ALL");
            sites.Remove("");

            foreach(var site in sites)
            {
                List<string> scopes = GetAllScopesBySite(site);

                scopes.Remove("");

                foreach(var scope in scopes)
                {
                    string sql = "INSERT INTO DBO.[ItemScopeResolved_" + site + "_" + scope + "] ([ITEM]) select DISTINCT RESOLVEDLOG.ITEM from dbo.resolvedlog inner join DBO.BackOrderItemMaster ON ResolvedLog.ITEM = BackOrderItemMaster.ITEM INNER JOIN dbo.users on ResolvedLog.UserId = users.id where AccessPermissions = '" + site + "' AND SCOPE = '" + scope + "' and BackOrderItemMaster.Region = 'Midwest' and (DATE_CREATED like 'OCT 27%' OR DATE_CREATED like 'OCT 28%' OR DATE_CREATED like 'OCT 29%' OR DATE_CREATED like 'OCT 30%' OR DATE_CREATED like 'OCT 31%' OR DATE_CREATED like 'NOV  1%' OR DATE_CREATED like 'NOV  2%')";

                    db.SaveData(sql, new { }, _connectionString);

                    sql = "update dbo.[ItemScopeResolved_" + site + "_" + scope + "] set [Release_Date] = BOIM.[ReleaseDate] from dbo.[ItemScopeResolved_" + site + "_" + scope  + "] isr inner join dbo.BackOrderItemMaster BOIM on isr.Item = boim.Item";

                    db.SaveData(sql, new { }, _connectionString);
                }
            }
        }
        public void ClearBackOrderItemMaster_Staging()
        {
            string sql = "truncate table dbo.BackOrderItemMaster_Staging";
            db.SaveData(sql, new { }, _connectionString);
        }

        public void ActivateBackOrderStaging(string region)
        {
            ParseStillOpen(region);
            ClearBackOrderItemMaster(region);
            FormatBackOrderDataByPriority();

            List<BackOrderItemMaster_Model> rows = new List<BackOrderItemMaster_Model>();

            string sql = "insert into dbo.BackOrderItemMaster select [Item],[MfrNum],[Description],[StockOutDate],[ReleaseDate],[GapDays],[ReasonCode],[StockStatus],[Source],[Region] from dbo.BackOrderItemMaster_Staging where [Region] = @Region";

            db.SaveData(sql, new {Region = region }, _connectionString);

            //ClearBackOrderItemMaster_Staging();
        }

        public string GetMBOByUserId(int userId)
        {
            string sql = "select top 1 [AccessPermissions] from dbo.users where [Id] = @Id";

            List<string> results = db.LoadData<string, dynamic>(sql, new { Id = userId }, _connectionString);

            string mbo = "No MBO Found";

            if(results.Count > 0)
            {
                mbo = results[0].Remove(results[0].Length - 3);
            }

            return mbo;
        }

        public List<Notes_Model> GetNotesByMBO(string item, string mbo)
        {
            List<Notes_Model> notesList = new List<Notes_Model>();

            OutputToLog("Item: " + item + ", MBO: " + mbo);

            string mboLikeSearch = mbo + "___";

            string sql = "select * from dbo.notes inner join dbo.users on notes.UserId = users.Id where users.AccessPermissions like @MBO and notes.ITEM = @Item";

            notesList = db.LoadData<Notes_Model, dynamic>(sql, new { Item = item, MBO = mboLikeSearch }, _connectionString);

            return notesList;

        }

        public List<Notes_Model> GetNotesBySiteScope(string item, string site, string scope)
        {
            List<Notes_Model> notesList = new List<Notes_Model>();

            OutputToLog("Item: " + item + ", Site: " + site + ", Scope: " + scope);

            string sql = "select * from dbo.notes inner join dbo.users on notes.UserId = users.Id where users.AccessPermissions = @Site and users.Scope = @Scope AND notes.ITEM = @Item";

            notesList = db.LoadData<Notes_Model, dynamic>(sql, new { Site = site, Scope = scope, Item = item }, _connectionString);

            return notesList;
        }

        public List<Notes_Model> GetNotesBySite(string item, string site)
        {
            List<Notes_Model> notesList = new List<Notes_Model>();

            OutputToLog("Item: " + item + ", Site: " + site);

            string sql = "select * from dbo.notes inner join dbo.users on notes.UserId = users.Id where users.AccessPermissions = @Site and notes.ITEM = @Item";

            notesList = db.LoadData<Notes_Model, dynamic>(sql, new { Site = site, Item = item }, _connectionString);

            return notesList;

        }

        public List<Notes_Model> GetNotesByItem(string itemId)
        {
            string sql = "select * from dbo.NOTES where ITEM = @lawsonId order by Id desc";

            List<Notes_Model> resultsList = new List<Notes_Model>();

            resultsList = db.LoadData<Notes_Model, dynamic>(sql, new { lawsonId = itemId }, _connectionString);

            if (resultsList.Count == 0)
            {
                resultsList.Add(GetNullNote());
            }

            return resultsList;
        }

        public List<Notes_Model> GetNotesByItemSite(string itemId, string site)
        {
            string sql = "select * from dbo.NOTES where (ITEM = @lawsonId and Site = @Site) order by Id desc";

            List<Notes_Model> resultsList = new List<Notes_Model>();

            resultsList = db.LoadData<Notes_Model, dynamic>(sql, new { lawsonId = itemId, Site = site, @Global = "Global" }, _connectionString);

            if (resultsList.Count == 0)
            {
                resultsList.Add(GetNullNote());
            }

            return resultsList;
        }


        public Notes_Model GetNullNote()
        {
            Notes_Model nullNote = new Notes_Model
            {
                Id = 0,
                ITEM = "",
                DATE_CREATED = "No notes we________re found that contain your search term.",
                NOTE = ""
            };

            return nullNote;
        }



        public void ClearBackOrderItemMaster(string region)
        {
            string sql = "delete dbo.BackOrderItemMaster where [Region] = @Region";

            db.SaveData(sql, new {Region = region }, _connectionString);

        }





        public string TranslateNameToSite(string siteName)
        {
            SiteName_Model output = new SiteName_Model();

            string sql = "select * from dbo.SiteName where Name = @Name";

            output = db.LoadData<SiteName_Model, dynamic>(sql, new { Name = siteName }, _connectionString).FirstOrDefault();

            return output.Site;
        }

        public string TranslateLocationToSite(string location)
        {
            //Do not use

            string sql = "select * from dbo.Locations_Sites where Location = @Location";

            List<Location_Sites_Model> resultsList = new List<Location_Sites_Model>();

            resultsList = db.LoadData<Location_Sites_Model, dynamic>(sql, new { Location = location }, _connectionString);

            if (resultsList.Count == 0)
            {
                return "LocationNotFound";
            }
            else
            {
                return resultsList.First().SITE;
            }

        }




        public bool IsResolved(string item, string site)
        {
            bool isResolved = false;

            string sql = "select top 1 [Resolved] from dbo.[" + site + "] where [Item] = @Item and [Resolved] = @State";

            List<string> resolvedResult = new List<string>();

            resolvedResult = db.LoadData<string, dynamic>(sql, new { Item = item, State = "Resolved" }, _connectionString);

            if (resolvedResult.Count > 0)
            {
                isResolved = true;
            }

            return isResolved;
        }

        public bool IsResolved(string item, int userId)
        {
            bool isResolved = false;

            string site = GetSiteByUser(userId);

            string scope = GetUserScope(userId);

            List<string> resolvedResult = new List<string>();

            string sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NOT NULL select top 1 [Item] from dbo.[ItemScopeResolved_" + site + "_" + scope + "] where [Item] = @Item";

            resolvedResult = db.LoadData<string, dynamic>(sql, new { Item = item }, _connectionString);

            if (resolvedResult.Count > 0)
            {
                isResolved = true;
            }

            return isResolved;
        }

        public bool IsResolved(string item, string site, string scope)
        {
            bool isResolved = false;

            List<string> resolvedResult = new List<string>();

            string sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NOT NULL select top 1 [Item] from dbo.[ItemScopeResolved_" + site + "_" + scope + "] where [Item] = @Item";

            resolvedResult = db.LoadData<string, dynamic>(sql, new { Item = item }, _connectionString);

            if (resolvedResult.Count > 0)
            {
                isResolved = true;
            }

            return isResolved;
        }

        public string IsResolvedOrOpen(string item, int userId)
        {
            string isResolved = "OPEN";

            if (this.IsResolved(item, userId))
            {
                isResolved = "RESOLVED";
            }

            return isResolved;
        }


        public string IsResolvedOrOpen(string item, string site)
        {
            string isResolved = "OPEN";

            if (this.IsResolved(item, site))
            {
                isResolved = "RESOLVED";
            }

            return isResolved;
        }

        public BackOrderItemMaster_Model GetBOIMByItemUserFromSiteTable(string item, int userId)
        {
            BackOrderItemMaster_Model output = new BackOrderItemMaster_Model();
            string site = GetSiteByUser(userId);

            string sql = "select top 1 * from dbo.[" + site + "] where Item = @Item";

            

            output = db.LoadData<BackOrderItemMaster_Model, dynamic>(sql, new { Item = item }, _connectionString).FirstOrDefault();

            OutputToLog("in");

            OutputToLog("select top 1 * from dbo.[" + site + "] where Item = '" + item + "'");

            return output;
        }


        public List<Subs_Model> GetSubsByItem(string itemId)
        {

            string sql = "select * from dbo.SUBS where Item = @Item and Active = 'A'";

            List<Subs_Model> resultsList = new List<Subs_Model>();

            resultsList = db.LoadData<Subs_Model, dynamic>(sql, new { Item = itemId }, _connectionString);

            if (resultsList.Count == 0)
            {
                resultsList.Add(this.GetNullSub());
            }

            return resultsList;

        }

        public Subs_Model GetNullSub()
        {
            Subs_Model nullSub = new Subs_Model
            {
                Id = 0,
                Item = "",
                Sub_Lawson = "",
                Sub_Mfr = ""
            };

            return nullSub;
        }

        public List<IC211_Model> GetLocationsByItemSite(string itemId, string site)
        {
            string sql = "select * from dbo.[IC211_" + site + "] where Item_Number = @Item";

            List<IC211_Model> resultsList = new List<IC211_Model>();

            resultsList = db.LoadData<IC211_Model, dynamic>(sql, new { Item = itemId }, _connectionString);

            if (resultsList.Count == 0)
            {
                resultsList.Add(this.GetNullIC211());
            }

            return resultsList;
        }

        public List<string> GetSitesByItem(string itemId)
        {
            List<string> results = new List<string>();

            string sql = "select distinct [Site] from dbo.IC211 where [Item_Number] = @Item order by [Site]";

            results = db.LoadData<string, dynamic>(sql, new { Item = itemId }, _connectionString);

            if (results.Count == 0)
            {
                results.Add("");
            }

            return results;
        }

        public void AddNoteByItem(Notes_Model newNote)
        {
            string sql = "insert into dbo.NOTES (ITEM,DATE_CREATED,NOTE,Site,UserId) values (@Item,@Date,@Note,@Site,@UserId)";

            db.SaveData(sql, new { Item = newNote.ITEM, Date = DateTime.Now, Note = newNote.NOTE, Site = newNote.Site, UserId = newNote.UserId }, _connectionString);
        }


        public bool DoesResolutionNoteExistForScope(string itemId, int userId)
        {
            bool hasResolutionNote = false;

            string site = GetSiteByUser(userId);

            DateTime noteDate = new DateTime();

            List<string> results = new List<string>();

            string sql = "select top 1 [DATE_CREATED] from dbo.notes where [ITEM] = @Item and [Site] = @Site order by [Id] desc";

            results = db.LoadData<string, dynamic>(sql, new { Item = itemId, Site = site }, _connectionString);

            Console.WriteLine("Resolution Notes Count: " + results.Count);

            if (results.Count > 0)
            {
                noteDate = DateTime.Parse(results[0]);

                Console.WriteLine(noteDate);

                TimeSpan noteAge = new TimeSpan();

                noteAge = DateTime.Now - noteDate;

                Console.WriteLine(noteAge);

                if (noteAge.Days < 30)
                {
                    hasResolutionNote = true;
                }

            }

            return hasResolutionNote;
        }

        public void ToggleResolvedState(string itemID, string site, int userId)
        {
            bool isResolved = this.IsResolved(itemID, userId);
            string sql = "";
            string resolvedLogSql = "";
            string siteTableResolvedSql = "";
            string state = "";


            if (!isResolved)
            {
                sql = "insert into dbo.ItemSiteResolved (Item,Site) values (@Item, @Site)";
                resolvedLogSql = "insert into dbo.ResolvedLog (ITEM, DATE_CREATED, SITE, STATE, UserId) values (@ITEM, @Date, @SITE, 'RESOLVED', @UserId)";
                //siteTableResolvedSql = "update [dbo].[" + site + "] SET [Resolved] = @State where Item = @Item";
                state = "Resolved";

            }
            else if (isResolved)
            {
                sql = "delete from dbo.ItemSiteResolved where Item = @Item and Site = @Site";
                resolvedLogSql = "insert into dbo.ResolvedLog (ITEM, DATE_CREATED, SITE, STATE, UserId) values (@ITEM, @Date, @SITE, 'OPEN', @UserId)";
                //siteTableResolvedSql = "update [dbo].[" + site + "] SET [Resolved] = @State where Item = @Item";
                state = "Open";
            }

            ToggleItemScopeResolvedState(itemID, userId);
            db.SaveData(sql, new { Item = itemID, Site = site }, _connectionString);
            db.SaveData(resolvedLogSql, new { ITEM = itemID, Date = DateTime.Now, SITE = site, UserId = userId }, _connectionString);
            //db.SaveData(siteTableResolvedSql, new { ITEM = itemID, State = state }, _connectionString);

        }

        public void ToggleResolvedState(string itemID, string site, string scope)
        {
            bool isResolved = this.IsResolved(itemID, site, scope);
            string sql = "";
            string resolvedLogSql = "";
            string siteTableResolvedSql = "";
            string state = "";


            if (!isResolved)
            {
                sql = "insert into dbo.ItemSiteResolved (Item,Site) values (@Item, @Site)";
                resolvedLogSql = "insert into dbo.ResolvedLog (ITEM, DATE_CREATED, SITE, STATE, UserId) values (@ITEM, @Date, @SITE, 'RESOLVED', @UserId)";
                //siteTableResolvedSql = "update [dbo].[" + site + "] SET [Resolved] = @State where Item = @Item";
                state = "Resolved";
            }
            else if (isResolved)
            {
                sql = "delete from dbo.ItemSiteResolved where Item = @Item and Site = @Site";
                resolvedLogSql = "insert into dbo.ResolvedLog (ITEM, DATE_CREATED, SITE, STATE, UserId) values (@ITEM, @Date, @SITE, 'OPEN', @UserId)";
                //siteTableResolvedSql = "update [dbo].[" + site + "] SET [Resolved] = @State where Item = @Item";
                state = "Open";
            }

            ToggleItemScopeResolvedState(itemID, site, scope);

            db.SaveData(sql, new { Item = itemID, Site = site }, _connectionString);
            string userId = "SYS";
            db.SaveData(resolvedLogSql, new { ITEM = itemID, Date = DateTime.Now, SITE = site, UserId = 18 }, _connectionString);
            //db.SaveData(siteTableResolvedSql, new { ITEM = itemID, State = state }, _connectionString);
        }

        public int GetAllCountbyUserFromSiteTable(int userId)
        {
            int allCount = 0;
            string sql;
            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);
            List<string> resultsList = new List<string>();

            if (scope == "ALL")
            {
                sql = "select distinct Item from dbo.[" + site + "] st";
            }
            else
            {
                sql = "select distinct Item from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number inner join dbo.Scope_Locations_" + site + " sl on ic.Location_Code = sl.Location where sl.Scope = @Scope";
            }


            resultsList = db.LoadData<string, dynamic>(sql, new { Site = site, Scope = scope }, _connectionString);

            allCount = resultsList.Count;


            return allCount;
        }

        public int GetAllCountbyScopeFromSiteTable(string site, string scope)
        {
            int allCount = 0;
            string sql;
            List<string> resultsList = new List<string>();

            if (scope == "ALL")
            {
                sql = "select distinct Item from dbo.[" + site + "] st";
            }
            else
            {
                sql = "select distinct Item from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.Item = ic.Item_Number inner join dbo.Scope_Locations_" + site + " sl on ic.Location_Code = sl.Location where sl.Scope = @Scope";
            }


            resultsList = db.LoadData<string, dynamic>(sql, new { Site = site, Scope = scope }, _connectionString);

            allCount = resultsList.Count;


            return allCount;
        }


        public void Bulk_IngestLRQ201(string filePath, string delimiter)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[7];

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.RQ201 ([Company],[RQL_REQ_LOCATION],[Name],[RQL_FROM_LOCATION],[RQL_ACTIVE_STATUS]) values ";

                foreach (var row in set)
                {
                    string record0 = row.Split(delimiter)[0];

                    string record1 = row.Split(delimiter)[1];

                    string record2 = row.Split(delimiter)[2];

                    string record3 = row.Split(delimiter)[3];

                    string record4 = row.Split(delimiter)[4];

                    sqlInsert += "('" + record0 + "','" + record1 + "','" + record2 + "','" + record3 + "','" + record4 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

                Console.WriteLine(sqlInsert);
            }
        }



        public void Bulk_IngestLocationsSites(string filePath, string delimiter)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[7];

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.Locations_Sites ([Location],[SITE]) values ";

                foreach (var row in set)
                {
                    string record0 = row.Split(delimiter)[1];

                    string record1 = row.Split(delimiter)[2];

                    sqlInsert += "('" + record0 + "','" + record1 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

                Console.WriteLine(sqlInsert);
            }
        }


        public void IngestSubs(string fileName, string delimiter)
        {
            int linesRead = 0;

            List<string> rows = new List<string>();

            Subs_Model sub = new Subs_Model();

            using (StreamReader subsFile = new StreamReader(fileName))
            {
                string line;

                while ((line = subsFile.ReadLine()) != null)
                {
                    rows.Add(line);
                }
            }

            foreach (var row in rows)
            {
                int totalColumns = row.Split(delimiter).Count();


                for (int i = 0; i < totalColumns - 2; i = i + 2)
                {
                    if ((!string.IsNullOrWhiteSpace(row.Split(delimiter)[i + 1])) || (!string.IsNullOrWhiteSpace(row.Split(delimiter)[i + 2])))
                    {
                        sub.Item = row.Split(delimiter)[0];
                        sub.Sub_Lawson = row.Split(delimiter)[i + 1];
                        sub.Sub_Mfr = row.Split(delimiter)[i + 2];

                        this.AddSub(sub);


                    }
                    linesRead++;
                    Console.WriteLine(linesRead);

                }


            }

            linesRead = 0;

            List<Subs_Model> subsList = new List<Subs_Model>();

            string sql = "select distinct Item, Sub_Lawson, Sub_Mfr from dbo.Subs";

            subsList = db.LoadData<Subs_Model, dynamic>(sql, new { }, _connectionString);

            sql = "truncate table dbo.Subs";

            db.SaveData(sql, new { }, _connectionString);

            foreach (var subItem in subsList)
            {
                this.AddSub(subItem);

                linesRead++;
                Console.WriteLine(subsList.Count - linesRead);
            }


            Console.WriteLine("IngestSubs Complete.");
        }

        public void BulkIngestIC211(string filePath, char delimiter, string site)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[7];

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.IC211 ([Item_Number],[PIV_VEN_ITEM],[Description],[Location_Code],[Company],[ITL_ACTIVE_STATUS_XLT],[Site]) values ";

                foreach (var row in set)
                {
                    string record0 = row.Split(delimiter)[0];

                    string record1 = row.Split(delimiter)[1];

                    string record2 = row.Split(delimiter)[2];

                    string record3 = row.Split(delimiter)[3];

                    string record4 = row.Split(delimiter)[4];

                    string record5 = row.Split(delimiter)[5];

                    string record6 = site;

                    sqlInsert += "('" + record0 + "','" + record1 + "','" + record2 + "','" + record3 + "','" + record4 + "','" + record5 + "','" + record6 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

                //Console.WriteLine(sqlInsert);

                //AddSitesToIC211FromScopeLocationTables();
            }
        }




        public void BulkIngestSubs(string filePath, char delimiter)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[7];

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.Subs ([Item],[Sub_Mfr]) values ";

                foreach (var row in set)
                {
                    string record0 = row.Split(delimiter)[2];

                    string record1 = row.Split(delimiter)[8];

                    sqlInsert += "('" + record0 + "','" + record1 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

                Console.WriteLine(sqlInsert);


            }
        }


        public void AddSitesToIC211FromScopeLocationTables()
        {
            List<string> sites = GetAllSites();


            foreach (var site in sites)
            {
                string mbo = site.Remove(4);

                Console.WriteLine(site + "/" + mbo);

                string sql = "update dbo.IC211 set [Site] = @Site from dbo.IC211 ic inner join dbo.[Scope_Locations_" + site + "] sl on ic.[Location_Code] = sl.[Location] where ic.[Company] = @Mbo";

                db.SaveData(sql, new { Site = site, Mbo = mbo }, _connectionString);
            }

        }

        public void MatchIC211LocationToSite()
        {
            string sql = "update dbo.IC211 set DBO.IC211.[Site] = ls.[Site] from dbo.IC211 ic inner join dbo.Locations_Sites ls on ic.[Location_Code] = ls.[Location] where ls.Site like CONCAT(ic.Company,@Underscores)";

            db.SaveData(sql, new { Underscores = "___" }, _connectionString);
        }

        public void ClearIC211()
        {
            string sql = "truncate table dbo.IC211";

            db.SaveData(sql, new { }, _connectionString);
        }


        public void BulkIngestBackOrderItemMaster(string filePath, char delimiter)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[9];

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.BackOrderItemMaster (Item,MfrNum,Description,StockOutDate,ReleaseDate,GapDays,ReasonCode,StockStatus,Source) values ";

                foreach (var row in set)
                {
                    string record0 = row.Split(delimiter)[0];

                    string record1 = row.Split(delimiter)[1];

                    string record2 = row.Split(delimiter)[2];

                    string record3 = row.Split(delimiter)[3];

                    string record4 = row.Split(delimiter)[4];

                    string record5 = row.Split(delimiter)[5];

                    string record6 = row.Split(delimiter)[6];

                    string record7 = row.Split(delimiter)[7];

                    string record8 = row.Split(delimiter)[8];

                    sqlInsert += "('" + record0 + "','" + record1 + "','" + record2 + "','" + record3 + "','" + record4 + "','" + record5 + "','" + record6 + "','" + record7 + "','" + record8 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

                Console.WriteLine(sqlInsert);
            }

        }





        public void AddSub(Subs_Model newSub)
        {
            string sql = "insert into dbo.Subs (Item,Sub_Lawson,Sub_Mfr,Notes,Active) values (@Item,@Sub_Lawson,@Sub_Mfr,@Note,'A')";

            db.SaveData(sql, new { Item = newSub.Item, Sub_Lawson = newSub.Sub_Lawson, Sub_Mfr = newSub.Sub_Mfr, Note = newSub.Notes }, _connectionString);
        }

        public void ResetItemSiteResolved()
        {
            string sql = "truncate table dbo.ItemSiteResolved";

            db.SaveData(sql, new { }, _connectionString);

            sql = "insert into dbo.RefreshLog (RefreshedOn) values (@RefreshDate)";

            db.SaveData(sql, new { RefreshDate = DateTime.Now }, _connectionString);
        }

        public bool AccessGranted(string userName, string password)
        {
            bool isGranted = false;

            string sql = "select * from dbo.Users where UserName = @User and Password = @Password";

            List<User_Model> userList = new List<User_Model>();

            userList = db.LoadData<User_Model, dynamic>(sql, new { User = userName, Password = password }, _connectionString);

            if (userList.Count > 0)
            {
                isGranted = true;
            }

            return isGranted;
        }

        public void BulkIngestNotes(string filePath, char delimiter)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[7];

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.Notes ([Item],[Date_Created],[Note],[Site],[UserId]) values ";

                foreach (var row in set)
                {
                    string record4watch = row.Split(delimiter)[4];

                    string record0 = row.Split(delimiter)[0];

                    string record1 = row.Split(delimiter)[1];

                    string record2 = row.Split(delimiter)[2];

                    string record3 = row.Split(delimiter)[3];

                    int record4 = Int32.Parse(row.Split(delimiter)[4]);

                    sqlInsert += "('" + record0 + "','" + record1 + "','" + record2 + "','" + record3 + "','" + record4 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

                Console.WriteLine(sqlInsert);
            }
        }




        public int GetUserId(string userName)
        {
            string sql = "select * from dbo.users where UserName=@UserName";

            User_Model user = new User_Model();

            user = db.LoadData<User_Model, dynamic>(sql, new { UserName = userName }, _connectionString).FirstOrDefault();

            return user.Id;
        }

        public bool IsItemInScopeByUser(int userId, string itemId)
        {
            bool isInScope = false;

            string site = GetSiteByUser(userId);

            List<string> matches = new List<string>();

            string sql = "select * from dbo.[IC211_" + site + "] ic inner join dbo.Users_Locations_" + site + " ul on ic.Location_Code = ul.Location where ul.UserId = @User and ic.Item_Number = @Item";

            matches = db.LoadData<string, dynamic>(sql, new { User = userId, Item = itemId }, _connectionString);

            if (matches.Count > 0)
            { isInScope = true; }

            return isInScope;
        }


        public void AddLocationsUsers(LocationsUsers_Model locationsUsers)
        {
            string sql = "insert into dbo.Users_Locations (UserId,Location) values( @Id, @Location)";

            db.SaveData(sql, new { Id = locationsUsers.UserID, Location = locationsUsers.Location }, _connectionString);
        }

        public void BulkIngestScope_Locations(string fileName, char delimiter)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[7];

            using (StreamReader fileStream = new StreamReader(fileName))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.[Scope_Locations] ([Scope],[Location]) values ";

                foreach (var row in set)
                {
                    string record0 = row.Split(delimiter)[0];

                    string record1 = row.Split(delimiter)[1];



                    sqlInsert += "(" + record0 + ",'" + record1 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

                Console.WriteLine(sqlInsert);
            }
        }

        public void BulkIngestUsers_Locations(string fileName, char delimiter, string site)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[7];

            using (StreamReader fileStream = new StreamReader(fileName))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.[Users_Locations] ([UserId],[Location],[Site]) values ";

                foreach (var row in set)
                {
                    string sql = "select top 1 [Id] from dbo.users where [UserName] = @UserName";

                    int record0 = db.LoadData<int, dynamic>(sql, new { UserName = row.Split(delimiter)[0] }, _connectionString).First();

                    string record1 = row.Split(delimiter)[1];

                    string record2 = site;

                    sqlInsert += "(" + record0 + ",'" + record1 + "','" + record2 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

                Console.WriteLine(sqlInsert);
            }
        }




        public string GetSiteByUser(int userId)
        {
            string output = "";
            List<string> results = new List<string>();

            string sql = "select top 1 AccessPermissions from dbo.users where Id = @UserId";

            results = db.LoadData<string, dynamic>(sql, new { UserId = userId }, _connectionString);

            if (results.Count > 0)
            {
                output = results[0];
            }

            return output;

        }

        public string TranslateSiteToName(string site)
        {
            string sql = "select top 1 sn.Name from dbo.SiteName sn where sn.Site = @Site";

            return db.LoadData<string, dynamic>(sql, new { Site = site }, _connectionString).FirstOrDefault();
        }

        public string GetUserNameByUserId(int userId)
        {
            string sql = "select UserName from dbo.users where Id = @UserId";

            return db.LoadData<string, dynamic>(sql, new { UserId = userId }, _connectionString).FirstOrDefault();
        }




        public void OutputUserNamesLocations(string filePath, bool appendFile)
        {
            List<LocationsUserNames_Model> locationsUserNamesList = new List<LocationsUserNames_Model>();

            string row;

            string sql = "select ul.Id,ul.Location,u.UserName from dbo.Users_Locations ul inner join dbo.Users u on ul.UserId = u.Id";

            locationsUserNamesList = db.LoadData<LocationsUserNames_Model, dynamic>(sql, new { }, _connectionString);

            using (StreamWriter sw = new StreamWriter(filePath, false))
            {
                row = "Id,Location,UserName";

                sw.WriteLine(row);

                foreach (var locationUser in locationsUserNamesList)
                {
                    row = locationUser.Id + "," + locationUser.Location + "," + locationUser.UserName;

                    sw.WriteLine(row);
                }
            }


        }

        public bool IsUserMonitored(int userId)
        {
            bool isUserMonitored = false;

            List<User_Model> monitoredUsers = new List<User_Model>();

            string sql = "select * from dbo.users where [IsMonitored] = 1 and Id = @Id";

            monitoredUsers = db.LoadData<User_Model, dynamic>(sql, new { Id = userId }, _connectionString);

            if (monitoredUsers.Count > 0)
            { isUserMonitored = true; }

            return isUserMonitored;

        }

        public List<User_Model> GetMonitoredUsers()
        {
            List<User_Model> monitoredUsers = new List<User_Model>();

            string sql = "select * from dbo.users where [IsMonitored] = 1";

            monitoredUsers = db.LoadData<User_Model, dynamic>(sql, new { }, _connectionString);

            return monitoredUsers;

        }


        public bool ChangeUserPassword(string userName, string userPassword, string newPassword)
        {
            bool successState = false;
            bool hasAccess = false;

            hasAccess = AccessGranted(userName, userPassword);

            if (hasAccess)
            {
                string sql = "update dbo.Users set [Password] = @Password where UserName = @UserName";

                db.SaveData(sql, new { Password = newPassword, UserName = userName }, _connectionString);

                successState = true;
            }

            return successState;
        }


        public List<string> GetAllSites()
        {
            List<string> output = new List<string>();

            string sql = "select [Site] from dbo.Sites order by Id";

            
            output = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

            

            Console.WriteLine(output.Count);

            foreach (var site in output)
            { Console.WriteLine(site); }

            output.Sort();

            return output;
        }

        public void ParseIC211()
        {
            List<string> sites = new List<string>();

            sites = GetAllSites();

            string sql;

            foreach (var site in sites)
            {
                sql = "IF OBJECT_ID('[dbo].[IC211_" + site + "]', 'U') IS NOT NULL DROP TABLE [dbo].[IC211_" + site + "]";

                db.SaveData(sql, new { }, _connectionString);

                Console.WriteLine("dropped one");

                sql = "CREATE TABLE [dbo].[IC211_" + site + "] ([Id] [int] IDENTITY(1,1) NOT NULL, [Item_Number][nvarchar](50) NOT NULL,[PIV_VEN_ITEM] [nvarchar](50) NULL,[Description] [nvarchar](50) NULL,[Location_Code] [nvarchar](50) NOT NULL,[Company] [nvarchar](50) NULL, [ITL_ACTIVE_STATUS_XLT] [nvarchar](50) NULL,[Site] [nvarchar](50) NOT NULL)";

                db.SaveData(sql, new { }, _connectionString);

                Console.WriteLine("created one");

                sql = "insert into dbo.[IC211_" + site + "]  select distinct [Item_Number],[PIV_VEN_ITEM],[Description],[Location_Code],[Company],[ITL_ACTIVE_STATUS_XLT],[Site] from dbo.IC211 where [Site] = @Site order by [Item_Number]";

                db.SaveData(sql, new { Site = site }, _connectionString);


            }

            RemoveInactivesBySiteIC211forAllSites();


        }

        public void RemoveInactivesBySiteIC211forAllSites()
        {
            List<string> sites = GetAllSites();

            foreach (var site in sites)
            {
                string sql = "IF OBJECT_ID('[dbo].[IC211_" + site + "]', 'U') IS NOT NULL delete from dbo.[IC211_" + site + "] where [ITL_ACTIVE_STATUS_XLT] = 'Inactive'";

                db.SaveData(sql, new { }, _connectionString);

                Console.WriteLine("Removed Inactives from {0}", site);
            }
        }


        public void AutoResolveSnoozedItems()
        {
            List<string> sites = GetAllSites();

            sites.Remove("");

            foreach(var site in sites)
            {
                List<string> scopes = GetAllScopes();

                scopes.Remove("ALL");

                scopes.Remove("");

                foreach(var scope in scopes)
                {
                    List<int> results = new List<int>();

                    string sql = "select sn.[Item] from dbo.[Snooze] sn " +
                        "inner join dbo.[" + site +"] BOIM on convert(int,BOIM.[Item]) = sn.[Item] " +
                        "where sn.[SnoozeTil] > @Today and sn.[Site] = @Site and sn.[Scope] = @Scope";

                    results = db.LoadData<int, dynamic>(sql, new { Today = DateTime.Today, Site = site, Scope = scope }, _connectionString);

                    foreach(var item in results)
                    {
                        if(!IsItemResolvedAtSiteScope(item.ToString(),site,scope))
                        {
                            ToggleItemScopeResolvedState(item.ToString(), site, scope);
                        }
                    }
                }
            }
        }

        public void AutoResolveRollingBackorders(string region)
        {

            List<string> sites = new List<string>();

            List<string> allSites = new List<string>();

            allSites = GetAllSites();

            foreach (var site in allSites)
            {
                string mbo = GetMBOBySite(site);
                string siteRegion = GetRegionByMBO(mbo);

                if (siteRegion == region)
                {
                    sites.Add(site);
                }
            }
            foreach (var site in sites)
            {
                string sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_ALL]','U') IS NOT NULL truncate table dbo.[ItemScopeResolved_" + site + "_ALL]";

                db.SaveData(sql, new { }, _connectionString);

                List<string> scopes = GetAllScopesBySite(site);

                scopes.Remove("ALL");

                foreach (var scope in scopes)
                {
                    List<string> itemsToResolve = new List<string>();

                    sql = "IF OBJECT_ID('[dbo].[" + site + "]', 'U') IS NOT NULL select distinct st.[Item] from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic211 on st.Item = ic211.Item_Number inner join dbo.[Scope_Locations_" + site + "] sl on ic211.Location_Code = sl.Location inner join dbo.[ItemScopeResolved_" + site + "_" + scope + "] isr on st.Item = isr.Item where st.ReleaseDate = isr.Release_Date and sl.Scope = @Scope and isr.Release_Date is not null and isr.Release_Date <> ''";

                    itemsToResolve = db.LoadData<string, dynamic>(sql, new { Scope = scope }, _connectionString);

                    sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NULL create table dbo.[ItemScopeResolved_" + site + "_" + scope + "] ([Item][nvarchar](50) not null,[Release_Date][nvarchar](500))";

                    db.SaveData(sql, new { }, _connectionString);


                    sql = "truncate table dbo.[ItemScopeResolved_" + site + "_" + scope + "]";

                    db.SaveData(sql, new { }, _connectionString);

                    if (itemsToResolve.Count > 0)
                    {
                        foreach (var item in itemsToResolve)
                        {
                            ToggleResolvedState(item, site, scope);
                        }
                    }

                    ResolveNegativeGaps(region);
                    PurgeErroneousResolves(region);
                    ResetAndUpdateItemScopeResolved_ALL_Tables(region);
                }
            }
        }


        public List<string> GetAllRegions()
        {
            List<string> allRegions = new List<string>();

            string sql = "select distinct [Region] from dbo.[MBO_Region]";

            allRegions = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

            while(allRegions.Contains(""))
            {
                allRegions.Remove("");
            }

            if(allRegions.Count == 0)
            {
                allRegions.Add("NO REGIONS FOUND");
            }

            return allRegions;    
        }

        public List<string> GetAllMBOsByRegion(string region)
        {
            List<string> mbos = new List<string>();

            string sql = "select distinct [MBO] from dbo.[MBO_Region] where [Region] = @Region";

            mbos = db.LoadData<string, dynamic>(sql, new { Region = region }, _connectionString);

            while(mbos.Contains(""))
            {
                mbos.Remove("");
            }

            if(mbos.Count == 0)
            {
                mbos.Add("NO MBOS FOUND FOR REGION " + region);
            }

            return mbos;
        }

        public List<string> GetAllSitesByMBO(string mbo)
        {
            List<string> sites = new List<string>();

            string sql = "select distinct [Site] from dbo.[Sites] where [MBO] = @MBO";

            sites = db.LoadData<string, dynamic>(sql, new { MBO = mbo }, _connectionString);

            while(sites.Contains(""))
            {
                sites.Remove("");
            }

            if(sites.Count == 0)
            {
                sites.Add("NO SITES FOUND FOR MBO " + mbo);
            }

            return sites;
        }
        public int ItemRunwayInDAys(int qoh, string item, string site, string scope)
        {
            int usage = 0;
            int runway = 0;

            string sql = "select top 1 [Usage] from dbo.[Usage_" + site + "_" + scope + "] where [Item] = @Item";

            usage = db.LoadData<int, dynamic>(sql, new { Item = item }, _connectionString).First();

            usage = usage / 180;

            if (usage == 0)
            {
                usage = 1;
            }

            runway = qoh / usage;

            return runway;
        }

        public string GetMBOBySite(string site)
        {
            string sql = "select distinct [MBO] from dbo.sites where [Site] = @Site";

            List<string> results = new List<string>();

            results = db.LoadData<string, dynamic>(sql, new { Site = site }, _connectionString);

            string mbo = "MBO not found";

            if(results.Count > 0)
            {
                mbo = results[0];
            }

            return mbo;
        }


        public void ParseBOIM(string region)
        {
            List<string> sites = new List<string>();

            List<string> allSites = new List<string>();

            allSites = GetAllSites();

            foreach(var site in allSites)
            {
                string mbo = GetMBOBySite(site);
                string siteRegion = GetRegionByMBO(mbo);

                if(siteRegion == region)
                {
                    sites.Add(site);
                }
            }


            string sql;

            foreach (var site in sites)
            {
                


                sql = "IF OBJECT_ID('[dbo].[" + site + "]', 'U') IS NOT NULL DROP TABLE [dbo].[" + site + "]";

                db.SaveData(sql, new { }, _connectionString);

                //Console.WriteLine("dropped one");

                sql = "CREATE TABLE [dbo].[" + site + "] ([Item] NVARCHAR(50) NOT NULL,[MfrNum] NVARCHAR(50) NULL,[Description] NVARCHAR(150),[StockOutDate] NVARCHAR(50),[ReleaseDate] NVARCHAR(500),[GapDays] NVARCHAR(50),[ReasonCode] NVARCHAR(500),[StockStatus] NVARCHAR(500),[Source] NVARCHAR(50))";

                db.SaveData(sql, new { }, _connectionString);

                Console.WriteLine("created one");

                sql = "insert into dbo.[" + site + "]  select distinct boim.Item, boim.MfrNum, boim.[Description], boim.StockOutDate, boim.ReleaseDate, boim.GapDays, boim.ReasonCode, boim.StockStatus, boim.Source from dbo.BackOrderItemMaster boim inner join dbo.[IC211_" + site + "] ic on boim.Item = ic.Item_Number where boim.[Region] = @Region and ic.[Site] = @Site order by Item";

                db.SaveData(sql, new { Site = site, Region = region}, _connectionString);

                sql = "ALTER TABLE [dbo].[" + site + "] ADD [Resolved] NVARCHAR(50)";

                db.SaveData(sql, new { }, _connectionString);

                CreateItemScopeResolvedTables(region);

            }


        }
        public void MatchResolved(string site)
        {
            string sql = "select Item from dbo.itemsiteresolved where Site = @Site";

            List<string> resolvedItems = db.LoadData<string, dynamic>(sql, new { Site = site }, _connectionString);

            sql = "UPDATE [dbo].[" + site + "] SET [Resolved] = @State where Item = @Item";

            foreach (var item in resolvedItems)
            {
                db.SaveData(sql, new { Item = item, State = "Resolved" }, _connectionString);
            }


        }

        public int GetMasterUserIdBySite(string site)
        {
            int masterUserId = 0;
            List<int> results = new List<int>();

            Console.WriteLine("Site given to GetMasterUserIdBySite: " + site);

            string sql = "select top 1 Id from dbo.Users where UserName = @Site";

            results = db.LoadData<int, dynamic>(sql, new { Site = site }, _connectionString);

            if (results.Count > 0)
            { masterUserId = results[0]; }

            return masterUserId;

        }

        public class Thousandator
        {
            public List<List<string>> Sets { get; set; }

            public Thousandator(List<string> rows)
            {
                Sets = new List<List<string>>();

                Thousandate(rows);
            }

            private void Thousandate(List<string> rows)
            {
                bool hasModuloSet = false;

                if (rows.Count % 1000 > 0)
                { hasModuloSet = true; }

                int numberOfFullSets = (rows.Count / 1000);

                for (int i = 1; i < numberOfFullSets + 1; i++)
                {
                    List<string> fullSet = new List<string>();

                    for (int j = 0; j < 1000; j++)
                    {
                        fullSet.Add(rows[j]);
                    }

                    Sets.Add(fullSet);

                    for (int j = 999; j > -1; j--)
                    {
                        rows.Remove(rows[j]);
                    }
                }

                if (hasModuloSet)
                {
                    List<string> partialSet = new List<string>();

                    foreach (var row in rows)
                    {
                        partialSet.Add(row);
                    }

                    Sets.Add(partialSet);
                }
            }





        }


        public void ParseStillOpen(string region)
        {
            List<string> sites = new List<string>();

            List<string> allSites = new List<string>();

            allSites = GetAllSites();

            foreach(var site in allSites)
            {
                string mbo = GetMBOBySite(site);
                string siteRegion = GetRegionByMBO(mbo);

                if(siteRegion == region)
                {
                    sites.Add(site);
                }
            }

            string sql;


            foreach (var site in sites)
            {
                sql = "IF OBJECT_ID('[dbo].[CarryOver_" + site + "]', 'U') IS NOT NULL DROP TABLE[dbo].[CarryOver_" + site + "]";

                db.SaveData(sql, new { }, _connectionString);

                Console.WriteLine("dropped one");

                sql = "CREATE TABLE [dbo].[CarryOver_" + site + "] ([Item] NVARCHAR(50) NOT NULL,[MfrNum] NVARCHAR(50) NULL,[Description] NVARCHAR(150),[StockOutDate] NVARCHAR(50),[ReleaseDate] NVARCHAR(500),[GapDays] NVARCHAR(50),[ReasonCode] NVARCHAR(500),[StockStatus] NVARCHAR(500),[Source] NVARCHAR(50),[Resolved] NVARCHAR(50))";

                db.SaveData(sql, new { }, _connectionString);

                Console.WriteLine("created one");

                sql = "insert into dbo.[CarryOver_" + site + "]  select * from dbo.[" + site + "] where ([Resolved] <> @Resolved or [Resolved] is null) order by Item";

                db.SaveData(sql, new { @Resolved = "Resolved" }, _connectionString);

            }
        }

        public void ParseScope_Locations()
        {
            List<string> sites = new List<string>();

            sites = GetAllSites();


            string sql;

            foreach (var site in sites)
            {
                sql = "IF OBJECT_ID('[dbo].[Scope_Locations_" + site + "]', 'U') IS NOT NULL DROP TABLE [dbo].[Scope_Locations_" + site + "]";

                db.SaveData(sql, new { }, _connectionString);

                Console.WriteLine("dropped one");

                sql = "CREATE TABLE [dbo].[Scope_Locations_" + site + "] ([Id] [int] IDENTITY(1,1) NOT NULL, [Scope][nvarchar](50) NOT NULL, [Location] [nvarchar](50) NOT NULL)";

                db.SaveData(sql, new { }, _connectionString);

                Console.WriteLine("created one");

                sql = "insert into dbo.[Scope_Locations_" + site + "] select distinct [Scope],[Location] from dbo.Users_Locations ul inner join dbo.Users us on ul.UserId = us.Id where [Site] = @Site and [Scope] <> @ALL order by [Scope]";

                db.SaveData(sql, new { ALL = "ALL", Site = site }, _connectionString);


            }
        }

        public void ParseUsers_Locations()
        {
            List<string> sites = new List<string>();

            sites = GetAllSites();

            string sql;

            foreach (var site in sites)
            {
                sql = "IF OBJECT_ID('[dbo].[Users_Locations_" + site + "]', 'U') IS NOT NULL DROP TABLE [dbo].[Users_Locations_" + site + "]";

                db.SaveData(sql, new { }, _connectionString);

                Console.WriteLine("dropped one");

                sql = "CREATE TABLE [dbo].[Users_Locations_" + site + "] ([Id] [int] IDENTITY(1,1) NOT NULL, [UserId][int] NOT NULL, [Location] [nvarchar](50) NOT NULL)";

                db.SaveData(sql, new { }, _connectionString);

                Console.WriteLine("created one");

                sql = "insert into dbo.[Users_Locations_" + site + "] select distinct [UserId],[Location]from dbo.Users_Locations where [Site] = @Site order by [UserId]";

                db.SaveData(sql, new { Site = site }, _connectionString);
            }
        }

        public List<string> GetAllMBOs()
        {
            List<string> output = new List<string>();

            string sql = "select distinct [MBO] from dbo.Sites";

            output = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

            return output;
        }



        public int GetResolvedCountByUserFromItemScopeResolved(int userId)
        {
            int resolvedCount = 0;
            List<int> resultsList = new List<int>();
            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);


            string sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + " ]', 'U') IS NOT NULL select distinct * from dbo.[ItemScopeResolved_" + site + "_" + scope + "]";

            resultsList = db.LoadData<int, dynamic>(sql, new { }, _connectionString);

            if (resultsList.Count > 0)
            {
                resolvedCount = resultsList.Count;
            }

            return resolvedCount;
        }

        public int GetResolvedCountByScopeFromItemScopeResolved(string site, string scope)
        {
            int resolvedCount = 0;
            List<string> resultsList = new List<string>();

            string sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + " ]', 'U') IS NOT NULL select distinct * from dbo.[ItemScopeResolved_" + site + "_" + scope + "]";

            resultsList = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

            if (resultsList.Count > 0)
            {
                resolvedCount = resultsList.Count;
            }

            return resolvedCount;
        }

        public string GetUserScope(int userId)
        {
            string scope;
            List<string> results = new List<string>();

            string sql = "select top 1 [Scope] from dbo.Users where Id = @UserId";
            results = db.LoadData<string, dynamic>(sql, new { UserId = userId }, _connectionString);

            if (results.Count > 0)
            {
                scope = results[0];
            }
            else
            {
                scope = "ALL";
            }

            return scope;
        }

        public void ToggleItemScopeResolvedState(string itemId, int userId)
        {
            string site = GetSiteByUser(userId);
            int siteId = GetMasterUserIdBySite(site);
            bool isResolved = IsResolved(itemId, userId);
            string scope = GetUserScope(userId);

            string sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NULL CREATE TABLE [dbo].[ItemScopeResolved_" + site + "_" + scope + "] ([Item] NVARCHAR(50) NOT NULL)";
            db.SaveData(sql, new { }, _connectionString);

            sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_ALL]', 'U') IS NULL CREATE TABLE [dbo].[ItemScopeResolved_" + site + "_All] ([Item] NVARCHAR(50) NOT NULL)";
            db.SaveData(sql, new { }, _connectionString);

            if (isResolved)
            {
                sql = "delete from [dbo].[ItemScopeResolved_" + site + "_" + scope + "] where [Item] = @ItemId";
                db.SaveData(sql, new { ItemId = itemId }, _connectionString);

                sql = "delete from [dbo].[ItemScopeResolved_" + site + "_ALL] where [Item] = @ItemId";
                db.SaveData(sql, new { ItemId = itemId }, _connectionString);

                sql = "update dbo.[" + site + "] set [Resolved] = 'Open' where [Item] = @ItemId";
                db.SaveData(sql, new { ItemId = itemId }, _connectionString);
            }
            else
            {
                string releaseDate = GetReleaseDateFromBOIM(itemId);
                sql = "insert into [dbo].[ItemScopeResolved_" + site + "_" + scope + "] ([Item],[Release_Date]) values (@ItemId,@ReleaseDate)";
                db.SaveData(sql, new { itemId = itemId, ReleaseDate = releaseDate }, _connectionString);

                if (IsResolvedInEachScopesBySite(itemId, site))
                {
                    sql = "insert into [dbo].[ItemScopeResolved_" + site + "_ALL] ([Item]) values (@ItemId)";
                    db.SaveData(sql, new { itemId = itemId }, _connectionString);

                    sql = "update dbo.[" + site + "] set [Resolved] = 'Resolved' where [Item] = @ItemId";
                    db.SaveData(sql, new { ItemId = itemId }, _connectionString);
                }

            }


        }

        public void ToggleItemScopeResolvedState(string itemId, string site, string scope)
        {
            int siteId = GetMasterUserIdBySite(site);

            bool isResolved = IsResolved(itemId, site, scope);

            string sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NULL CREATE TABLE [dbo].[ItemScopeResolved_" + site + "_" + scope + "] ([Item] NVARCHAR(50) NOT NULL)";
            db.SaveData(sql, new { }, _connectionString);

            sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_ALL]', 'U') IS NULL CREATE TABLE [dbo].[ItemScopeResolved_" + site + "_All] ([Item] NVARCHAR(50) NOT NULL)";
            db.SaveData(sql, new { }, _connectionString);

            if (isResolved)
            {
                sql = "delete from [dbo].[ItemScopeResolved_" + site + "_" + scope + "] where [Item] = @ItemId";
                db.SaveData(sql, new { ItemId = itemId }, _connectionString);

                sql = "delete from [dbo].[ItemScopeResolved_" + site + "_ALL] where [Item] = @ItemId";
                db.SaveData(sql, new { ItemId = itemId }, _connectionString);

                sql = "update dbo.[" + site + "] set [Resolved] = 'Open' where [Item] = @ItemId";
                db.SaveData(sql, new { ItemId = itemId }, _connectionString);
            }
            else
            {
                string releaseDate = GetReleaseDateFromBOIM(itemId);

                sql = "insert into [dbo].[ItemScopeResolved_" + site + "_" + scope + "] ([Item],[Release_Date]) values (@ItemId,@ReleaseDate)";
                db.SaveData(sql, new { itemId = itemId, ReleaseDate = releaseDate }, _connectionString);

                if (IsResolvedInEachScopesBySite(itemId, site))
                {
                    sql = "insert into [dbo].[ItemScopeResolved_" + site + "_ALL] ([Item]) values (@ItemId)";
                    db.SaveData(sql, new { itemId = itemId }, _connectionString);

                    sql = "update dbo.[" + site + "] set [Resolved] = 'Resolved' where [Item] = @ItemId";
                    db.SaveData(sql, new { ItemId = itemId }, _connectionString);
                }

                sql = "insert into dbo.[ResolvedLog] ([Item],[Date_Created],[State],[Site],[UserId]) values (@ItemId, @Today,'RESOLVED',@Site,@UserId)";

                db.SaveData(sql, new { ItemId = itemId, Today = DateTime.Today, Site = site, UserId = siteId }, _connectionString);
            }
        }

        public string GetReleaseDateFromBOIM(string item)
        {
            string releaseDate = "";

            string sql = "select [ReleaseDate] from [dbo].[BackOrderItemMaster] where [Item] = @Item";
            releaseDate += db.LoadData<string, dynamic>(sql, new { Item = item }, _connectionString).FirstOrDefault();

            return releaseDate;
        }

        public void UpdateScopeResolvedTables()
        {
            List<int> userIds = new List<int>();


            string sql = "select distinct Id from dbo.Users";
            userIds = db.LoadData<int, dynamic>(sql, new { }, _connectionString);

            foreach (var user in userIds)
            {
                string site = GetSiteByUser(user);
                int siteId = GetMasterUserIdBySite(site);
                string scope = GetUserScope(user);

                List<string> resolvedItems = new List<string>();
                sql = "select distinct [Item] from.ResolvedLog where cast([DATE_CREATED] as datetime) > @Date and [State] = @State and [UserId] = @UserId order by [Item]";
                resolvedItems = db.LoadData<string, dynamic>(sql, new { Date = "3/17/2021", State = "Resolved", UserId = user }, _connectionString);

                sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NULL CREATE TABLE [dbo].[ItemScopeResolved_" + site + "_" + scope + "] ([Item] NVARCHAR(50) NOT NULL)";
                db.SaveData(sql, new { }, _connectionString);

                sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_ALL]', 'U') IS NULL CREATE TABLE [dbo].[ItemScopeResolved_" + site + "_ALL] ([Item] NVARCHAR(50) NOT NULL)";
                db.SaveData(sql, new { }, _connectionString);



                foreach (var item in resolvedItems)
                {
                    sql = "insert into dbo.[ItemScopeResolved_" + site + "_" + scope + "] ([Item]) values (@Item)";
                    db.SaveData(sql, new { Item = item }, _connectionString);

                    sql = "insert into dbo.[ItemScopeResolved_" + site + "_ALL] ([Item]) values (@Item)";
                    db.SaveData(sql, new { Item = item }, _connectionString);
                }


            }
        }

        public void ResetAndUpdateItemScopeResolved_ALL_Tables(string region)
        {
            List<string> sites = new List<string>();

            List<string> allSites = new List<string>();

            allSites = GetAllSites();

            foreach (var site in allSites)
            {
                string mbo = GetMBOBySite(site);
                string siteRegion = GetRegionByMBO(mbo);

                if (siteRegion == region)
                {
                    sites.Add(site);
                }
            }

            foreach (var site in sites)
            {
                string sql = "select distinct [Item] from dbo.[" + site + "]";

                List<string> boimItems = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

                sql = "truncate table dbo.[ItemScopeResolved_" + site + "_ALL]";

                db.SaveData(sql, new { }, _connectionString);


                if (boimItems.Count > 0)
                {
                    foreach (var item in boimItems)
                    {
                        if (IsResolvedInEachScopesBySite(item, site))
                        {

                            sql = "insert into dbo.[ItemScopeResolved_" + site + "_ALL] ([Item]) values (@Item)";

                            db.SaveData(sql, new { Item = item }, _connectionString);

                            sql = "update dbo.[" + site + "] set [Resolved] = 'Resolved' where item = @Item";

                            db.SaveData(sql, new { Item = item }, _connectionString);

                        }
                    }
                }
            }
        }

        public void PurgeScopeLevelItemScopeResolvedTablesOfNonBOIMItems(string region)
        {
            List<string> sites = new List<string>();

            List<string> allSites = new List<string>();

            allSites = GetAllSites();

            foreach (var site in allSites)
            {
                string mbo = GetMBOBySite(site);
                string siteRegion = GetRegionByMBO(mbo);

                if (siteRegion == region)
                {
                    sites.Add(site);
                }
            }
            sites.Remove("");

            foreach(var site in sites)
            {
                List<string> scopes = GetAllScopesBySite(site);

                scopes.Remove("ALL");
                scopes.Remove("");

                foreach(var scope in scopes)
                {
                    string sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NOT NULL select distinct isr.[Item] from dbo.[ItemScopeResolved_" + site + "_" + scope + "] isr left join dbo.["+site+"] boim on isr.item = boim.item where boim.item is null";

                    List<string> nonBOIMItems = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

                    if(nonBOIMItems.Count > 0)
                    {
                        sql = "delete dbo.[ItemScopeResolved_" + site + "_" + scope + "] where ";

                        foreach(var item in nonBOIMItems)
                        {
                            sql += "[Item] = '" + item + "' or ";
                        }

                        sql = sql.Remove(sql.Length - 4);

                        db.SaveData(sql, new { }, _connectionString);
                    }
                }

            }
        }

        public void UpdateBOIMsMidCycle(string region)
        {
            ParseBOIM(region);
            ParseSiteScopeUsageBOIMforAllSites(region);
            PurgeScopeLevelItemScopeResolvedTablesOfNonBOIMItems(region);
            ResetAndUpdateItemScopeResolved_ALL_Tables(region);
            SetAllBOIMToOpen(region);
        }

        public bool IsResolvedInEachScopesBySite(string itemId, string site)
        {
            bool isResolvedAll = true;

            List<string> scopes = GetAllScopesBySite(site);

            scopes.Remove("ALL");
            scopes.Remove("");



            List<string> results = new List<string>();

            if(scopes.Count == 0)
            {
                isResolvedAll = false;
            }

            if (IsItemInAnyScopeBySite(itemId, site))
            {

                foreach (var scope in scopes)
                {
                    string sql = "select top 1 [Item_Number] from dbo.[IC211_" + site + "] ic inner join dbo.[Scope_Locations_" + site + "] sl on ic.[Location_Code] = sl.[Location] where ic.[Item_Number] = @ItemId and sl.[Scope] = @Scope";

                    results = db.LoadData<string, dynamic>(sql, new { ItemId = itemId, Scope = scope }, _connectionString);


                    if (results.Count > 0)
                    {
                        sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NOT NULL select top 1 [Item] from dbo.[ItemScopeResolved_" + site + "_" + scope + "] where [Item] = @ItemId";

                        results = db.LoadData<string, dynamic>(sql, new { ItemId = itemId }, _connectionString);


                        if (results.Count == 0)
                        {
                            isResolvedAll = false;
                        }
                    }
                }
            }
            else
            {
                isResolvedAll = false;
            }
            return isResolvedAll;
        }

        public bool IsItemInAnyScopeBySite(string item, string site)
        {
            bool isInAnyScope = false;

            List<string> scopes = GetAllScopesBySite(site);
            scopes.Remove("ALL");
            scopes.Remove("");

            foreach(var scope in scopes)
            {
                string sql = "select top 1 item from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.item = ic.item_number inner join dbo.[Scope_Locations_" + site + "] sl on ic.location_code = sl.location where st.item = @Item and sl.Scope = @Scope";
                List<string> results = db.LoadData<string, dynamic>(sql, new { Item = item, Scope = scope }, _connectionString);

                if(results.Count > 0)
                {
                    isInAnyScope = true;
                }

            }

            return isInAnyScope;
        }

        public List<string> GetAllScopesBySite(string site)
        {
            List<string> scopes = new List<string>();

            string sql = "select distinct [Scope] from dbo.Users where [AccessPermissions] = @Site and [Scope] <> 'ALL' and [Scope] is not null and [Scope] <> ''";

            scopes = db.LoadData<string, dynamic>(sql, new { Site = site }, _connectionString);

            if (scopes.Count == 0)
            {
                scopes.Add("");
            }

            return scopes;
        }


        public void DropItemScopeResolvedTables()
        {
            List<string> sites = new List<string>();

            List<string> scopes = new List<string>();

            sites = GetAllSites();

            string sql = "select distinct [Scope] from dbo.users";
            scopes = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

            foreach (var site in sites)
            {
                sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_ALL]', 'U') IS NOT NULL drop table dbo.[ItemScopeResolved_" + site + "_ALL]";
                db.SaveData(sql, new { }, _connectionString);

                foreach (var scope in scopes)
                {
                    sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NOT NULL drop table dbo.[ItemScopeResolved_" + site + "_" + scope + "]";
                    db.SaveData(sql, new { }, _connectionString);
                }

                sql = "if object_id('[dbo].[ItemScopeResolved_" + site + "_]', 'U') is not null drop table [dbo].[ItemScopeResolved_" + site + "_]";
                db.SaveData(sql, new { }, _connectionString);
            }

        }

        public void UpdateUsers_LocationsWithSites()
        {
            List<int> users = new List<int>();

            string sql = "select distinct Id from dbo.users";
            users = db.LoadData<int, dynamic>(sql, new { }, _connectionString);

            foreach (var user in users)
            {
                string site = GetSiteByUser(user);

                sql = "update dbo.Users_Locations set [Site] = @Site where UserId = @UserId";
                db.SaveData(sql, new { Site = site, UserId = user }, _connectionString);
            }
        }

        public void SetAllBOIMToOpen(string region)
        {
            List<string> sites = new List<string>();

            List<string> allSites = new List<string>();

            allSites = GetAllSites();

            foreach (var site in allSites)
            {
                string mbo = GetMBOBySite(site);
                string siteRegion = GetRegionByMBO(mbo);

                if (siteRegion == region)
                {
                    sites.Add(site);
                }
            }


            foreach (var site in sites)
            {
                string sql = "update dbo.[" + site + "] set [Resolved] = 'Open' where ([Resolved] <> 'Resolved' or [Resolved] is null)";
                db.SaveData(sql, new { }, _connectionString);
            }

        }

        public List<string> GetAllScopes()
        {
            List<string> results = new List<string>();

            string sql = "select distinct [Scope] from dbo.users";
            results = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

            return results;
        }

        public List<string> GetAllScopesForSite(string site)
        {
            List<string> scopes = new List<string>();

            string sql = "select distinct [Scope] from dbo.[Scope_Locations_" + site + "]";

            scopes = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

            if (scopes.Count == 0)
            {
                scopes.Add("NoScope");
            }

            return scopes;
        }

        public void RectifyBOIMwithItemScopeResolvedTables()
        {
            List<string> sites = new List<string>();

            sites = GetAllSites();

            foreach (var site in sites)
            {
                string sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_ALL]', 'U') IS NOT NULL update dbo.[" + site + "] set [Resolved] = 'Resolved' from dbo.[" + site + "] st inner join dbo.[ItemScopeResolved_" + site + "_ALL] isr on st.Item = isr.Item";
                db.SaveData(sql, new { }, _connectionString);
            }

        }

        public void ExportCarryOverByAllSites(string directoryPath, string region)
        {
            string row;
            string sql;
            string filePath = directoryPath;

            List<BackOrderItemMaster_Model> results = new List<BackOrderItemMaster_Model>();

            List<string> sites = new List<string>();

            List<string> allSites = new List<string>();

            allSites = GetAllSites();

            foreach (var site in allSites)
            {
                string mbo = GetMBOBySite(site);
                string siteRegion = GetRegionByMBO(mbo);

                if (siteRegion == region)
                {
                    sites.Add(site);
                }
            }

            foreach (var site in sites)
            {
                filePath = @"C:\Users\Brian J Blake\Documents\SupplyState_Exports\CarryOver_" + site + ".csv";


                using (StreamWriter sw = new StreamWriter(filePath, false))
                {
                    row = "Item,MfrNum,Description,StockoutDate,ReleaseDate,GapDays,ReasonCode,StockStatus,Source";

                    sw.WriteLine(row);

                    sql = "select co.[Item],co.[MfrNum],co.[Description],co.[StockOutDate],co.[ReleaseDate],co.[GapDays],co.[ReasonCode],co.[StockStatus],co.[Source],co.[Resolved] from dbo.[CarryOver_" + site + "] co inner join dbo.[" + site + "] st on co.[Item] = st.[Item]";
                    results = db.LoadData<BackOrderItemMaster_Model, dynamic>(sql, new { }, _connectionString);

                    foreach (var result in results)
                    {
                        row = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", result.Item, result.MfrNum, result.Description, result.StockOutDate, result.ReleaseDate, result.GapDays, result.ReasonCode, result.StockStatus, result.Source);

                        sw.WriteLine(row);
                    }
                }
            }
        }

        public void ExportProgressReport()
        {
            List<string> sites = new List<string>();
            List<User_Model> monitoredUsers = new List<User_Model>();
            List<string> rows = new List<string>();

            int siteUserId;
            int allCount;
            int resolvedCount;
            int openCount;
            int progress;
            string UserName;
            string row;


            row = "Site,User,All,Resolved,Open,Progress(%)";
            rows.Add(row);

            sites = GetAllSites();

            monitoredUsers = GetMonitoredUsers();


            foreach (var user in monitoredUsers)
            {
                if (!String.IsNullOrWhiteSpace(user.AccessPermissions) && user.AccessPermissions != "")
                {

                    allCount = GetAllCountbyUserFromSiteTable(user.Id);
                    resolvedCount = GetResolvedCountByUserFromItemScopeResolved(user.Id);
                    openCount = allCount - resolvedCount;
                    if (allCount > 0)
                    {
                        progress = (resolvedCount * 100) / allCount;
                    }
                    else
                    {
                        progress = 0;
                    }

                    row = user.AccessPermissions + "," + user.UserName + "," + allCount + "," + resolvedCount + "," + openCount + "," + progress;
                    rows.Add(row);
                }
            }

            string filePath = @"C:\Users\Brian J Blake\Documents\SupplyState_Exports\SupplyState_IS_Performance.csv";

            using (StreamWriter sw = new StreamWriter(filePath, false))
            {

                foreach (var record in rows)
                {
                    sw.WriteLine(record);
                }
            }

        }

        public BackOrderItemMaster_Model GetBackOrderDisplayFromMasterBOIMbyItem(string itemId)
        {
            List<BackOrderItemMaster_Model> results = new List<BackOrderItemMaster_Model>();

            string sql = "select top 1 * from dbo.BackOrderItemMaster where [Item] = @Item";

            results = db.LoadData<BackOrderItemMaster_Model, dynamic>(sql, new { Item = itemId }, _connectionString);

            if (results.Count == 0)
            {
                results.Add(new BackOrderItemMaster_Model
                {
                    Item = ""

                });
            }

            return results[0];

        }

        public IC211_Model GetIC211FromMasterIC211byItem(string itemId)
        {
            List<IC211_Model> results = new List<IC211_Model>();

            string sql = "select top 1 * from dbo.IC211 where [Item_Number] = @Item";

            results = db.LoadData<IC211_Model, dynamic>(sql, new { Item = itemId }, _connectionString);

            if (results.Count == 0)
            {
                results.Add(GetNullIC211());

            }

            return results[0];
        }

        public bool IsItemCritical(string itemId)
        {
            bool isCritical = false;

            List<string> results = new List<string>();

            string sql = "select top 1 [Item] from dbo.BackOrderItemMaster where [Item] = @Item";
            results = db.LoadData<string, dynamic>(sql, new { Item = itemId }, _connectionString);

            if (results.Count > 0)
            {
                isCritical = true;
            }

            return isCritical;
        }

        public bool ItemHasSubs(string itemId)
        {
            bool hasSubs = false;

            List<string> results = new List<string>();

            string sql = "select top 1 [Item] from dbo.Subs where [Item] = @Item";
            results = db.LoadData<string, dynamic>(sql, new { Item = itemId }, _connectionString);

            if (results.Count > 0)
            {
                hasSubs = true;
            }

            return hasSubs;
        }

        public int ItemSubCount(string itemId)
        {
            int subCount = 0;

            List<string> results = new List<string>();

            string sql = "select [Item] from dbo.Subs where [Item] = @Item and [Active] = 'A'";

            results = db.LoadData<string, dynamic>(sql, new { Item = itemId }, _connectionString);

            subCount = results.Count;

            return subCount;
        }

        public int ItemNoteCount(string itemId)
        {
            int noteCount = 0;

            List<string> results = new List<string>();

            string sql = "select [Item] from dbo.Notes where [Item] = @Item";

            results = db.LoadData<string, dynamic>(sql, new { Item = itemId }, _connectionString);

            noteCount = results.Count;

            return noteCount;
        }

        public int ItemSiteCount(string itemId)
        {
            int siteCount = 0;

            List<string> results = new List<string>();

            string sql = "select distinct [Site] from dbo.IC211 where [Item_Number] = @Item and [ITL_ACTIVE_STATUS_XLT] = 'Active'";

            results = db.LoadData<string, dynamic>(sql, new { Item = itemId }, _connectionString);

            siteCount = results.Count;

            return siteCount;
        }

        public bool IsItemActive(string itemId)
        {
            bool isActive = false;

            List<string> results = new List<string>();

            string sql = "select top 1 [Site] from dbo.IC211 where [Item_Number] = @Item and [ITL_ACTIVE_STATUS_XLT] = 'Active'";

            results = db.LoadData<string, dynamic>(sql, new { Item = itemId }, _connectionString);

            if (results.Count > 0)
            {
                isActive = true;

            }

            return isActive;
        }

        public bool CanUserEditSubs(int userId)
        {
            bool userCanEditSubs = false;

            List<int> results = new List<int>();

            string sql = "select top 1 [CanEditSubs] from dbo.users where [Id] = @UserId";

            results = db.LoadData<int, dynamic>(sql, new { UserId = userId }, _connectionString);

            if (results[0] == 1)
            {
                userCanEditSubs = true;
            }

            return userCanEditSubs;
        }

        public List<ItemList_Model> GetAllItemListModelFromBOIM()
        {
            List<ItemList_Model> results = new List<ItemList_Model>();

            string sql = "select [Item],[MfrNum],[Description],[ReleaseDate],[StockStatus] from dbo.BackOrderItemMaster order by [Item]";

            results = db.LoadData<ItemList_Model, dynamic>(sql, new { }, _connectionString);

            if (results.Count == 0)
            {
                results.Add(new ItemList_Model
                {
                    Item = ""
                });
            }

            return results;
        }

        public bool IsItemInScopeAtSite(string itemId, string scope, string site)
        {
            bool itemIsInScope = false;

            List<string> results = new List<string>();

            string sql = "select top 1 [Item_Number] from dbo.[IC211_" + site + "] ic inner join dbo.[Scope_Locations_" + site + "] sl on ic.[Location_Code] = sl.[Location] where ic.[Item_Number] = @ItemId and sl.[Scope] = @Scope";

            results = db.LoadData<string, dynamic>(sql, new { ItemId = itemId, Scope = scope }, _connectionString);

            if (results.Count > 0)
            {
                itemIsInScope = true;
            }

            return itemIsInScope;
        }

        public void CleanAllScopeItemResolvedTables()
        {
            List<string> sites = GetAllSites();

            foreach (var site in sites)
            {
                List<string> resolvedItems = new List<string>();

                string sql = "IF OBJECT_ID('[dbo].[" + site + "]', 'U') IS NOT NULL select distinct [Item] from dbo.[" + site + "] where [Resolved] = 'Resolved'";

                resolvedItems = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

                if (resolvedItems.Count > 0)
                {
                    List<string> scopes = GetAllScopesBySite(site);

                    foreach (var item in resolvedItems)
                    {
                        foreach (var scope in scopes)
                        {
                            if (IsItemInScopeAtSite(item, scope, site))
                            {
                                List<string> results = new List<string>();

                                sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NOT NULL select top 1 [Item] from dbo.[ItemScopeResolved_" + site + "_" + scope + "] where [Item] = @ItemId";

                                results = db.LoadData<string, dynamic>(sql, new { ItemId = item }, _connectionString);

                                if (results.Count == 0)
                                {
                                    sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_All]', 'U') IS NOT NULL delete from dbo.[ItemScopeResolved_" + site + "_ALL] where [Item] = @ItemId";

                                    db.SaveData(sql, new { ItemId = item }, _connectionString);

                                    sql = "update dbo.[" + site + "] set [Resolved] = 'Open' where [Item] = @ItemId";

                                    db.SaveData(sql, new { ItemId = item }, _connectionString);
                                }
                            }
                        }
                    }

                }
            }
        }


        public void LogSignIn(int userId)
        {
            string sql = "insert into dbo.[SignInLog] ([UserId],[Date_Time]) values (@UserId,@Now)";

            db.SaveData(sql, new { UserId = userId, Now = DateTime.Now }, _connectionString);
        }

        public void OutputToLog(string output)
        {
            string sql = "insert into dbo.OutputLog ([Output]) values (@Output)";
            db.SaveData(sql, new { Output = output }, _connectionString);
        }

        public void CreateItemScopeResolvedTables(string region)
        {
            string sql;

            List<string> allSites = GetAllSites();

            List<string> sites = new List<string>();

            foreach(var site in allSites)
            {
                string mbo = GetMBOBySite(site);
                string siteRegion = GetRegionByMBO(mbo);

                if(siteRegion == region)
                {
                    sites.Add(site);
                }
            }

            foreach (var site in sites)
            {
                List<string> scopes = GetAllScopesBySite(site);

                scopes.Add("ALL");

                foreach (var scope in scopes)
                {
                    //string sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NOT NULL DROP TABLE[dbo].[ItemScopeResolved_" + site + "_" + scope + "]";

                    //db.SaveData(sql, new { }, _connectionString);

                    sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NULL CREATE TABLE [dbo].[ItemScopeResolved_" + site + "_" + scope + "] ([Item] [nvarchar](50) NOT NULL, [Release_Date] [nvarchar](500) null) ON [PRIMARY]";

                    db.SaveData(sql, new { }, _connectionString);
                }



            }

        }

        public void SetAccessPermissionsForAllSites()
        {
            List<string> sites = GetAllSites();

            foreach (var site in sites)
            {
                string sql = "update dbo.Users set [AccessPermissions] = @Site where [UserName] = @Site";

                db.SaveData(sql, new { Site = site }, _connectionString);
            }
        }

        public string NextOpenByUserId(int userId, string itemId)
        {
            List<string> results = new List<string>();

            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);
            int idInitial = GetSSUBoimId(itemId, site, scope);

            string sql = "select top 1 ssub.[Item] from dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] ssub left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] isr on ssub.[Item] = isr.[Item] where ssub.[Id] > @Id and isr.[Item] is null order by ssub.[Id]";

            results = db.LoadData<string, dynamic>(sql, new { Id = idInitial }, _connectionString);

            if (results.Count == 0)
            {
                results.Add(itemId);
            }

            return results[0];
        }

        public string PreviousOpenByUserId(int userId, string itemId)
        {
            List<string> results = new List<string>();

            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);
            int idInitial = GetSSUBoimId(itemId, site, scope);

            string sql = "select top 1 ssub.[Item] from dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] ssub left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] isr on ssub.[Item] = isr.[Item] where ssub.[Id] < @Id and isr.[Item] is null order by ssub.[Id] desc";

            results = db.LoadData<string, dynamic>(sql, new { Id = idInitial }, _connectionString);

            if (results.Count == 0)
            {
                results.Add(itemId);
            }

            return results[0];
        }

        public string NextResolvedByUserId(int userId, string itemId)
        {
            List<string> results = new List<string>();

            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);
            int idInitial = GetSSUBoimId(itemId, site, scope);

            string sql = "select top 1 ssub.[Item] from dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] ssub left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] isr on ssub.[Item] = isr.[Item] where ssub.[Id] > @Id and isr.[Item] is not null order by ssub.[Id]";

            results = db.LoadData<string, dynamic>(sql, new { Id = idInitial }, _connectionString);

            if (results.Count == 0)
            {
                results.Add(itemId);
            }

            return results[0];
        }

        public string PreviousResolvedByUserId(int userId, string itemId)
        {
            List<string> results = new List<string>();

            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);
            int idInitial = GetSSUBoimId(itemId, site, scope);

            string sql = "select top 1 ssub.[Item] from dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] ssub left join dbo.[ItemScopeResolved_" + site + "_" + scope + "] isr on ssub.[Item] = isr.[Item] where ssub.[Id] < @Id and isr.[Item] is not null order by ssub.[Id] desc";

            results = db.LoadData<string, dynamic>(sql, new { Id = idInitial }, _connectionString);

            if (results.Count == 0)
            {
                results.Add(itemId);
            }

            return results[0];
        }

        public int GetUsageByItemSiteScope(string item, string site, string scope)
        {
            string sql = "Select [Usage] from dbo.[Usage_" + site + "_" + scope + "] where [Item] = @Item";
            
            List<string> results =  db.LoadData<string, dynamic>(sql, new { Item = item }, _connectionString);

            int usage = 0;

            if(results.Count > 0)
            {
                usage = Int32.Parse(results[0]);
            }
            
            return usage;
        }

        public int GetSSUBoimId(string item, string site, string scope)
        {
            int idOut = 0;

            string sql = "select [Id] from dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] where [Item] = @Item";

            idOut = db.LoadData<int, dynamic>(sql, new { Item = item }, _connectionString).First();

            return idOut;
        }

        public string NextAllByUserId(int userId, string itemId)
        {
            List<string> results = new List<string>();

            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);
            int idInitial = GetSSUBoimId(itemId, site, scope);

            int usage = GetUsageByItemSiteScope(itemId, site, scope);

            string sql = "select top 1 [Item] from dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] where [Id] > @Id";

            results = db.LoadData<string, dynamic>(sql, new { Id = idInitial }, _connectionString);

            if (results.Count == 0)
            {
                results.Add(itemId);
            }

            OutputToLog("Next item is: " + results[0]);

            return results[0];
        }

        public void FixNullsInUsageTables(string region)
        {
            List<string> sites = new List<string>();

            List<string> allSites = new List<string>();

            allSites = GetAllSites();

            foreach (var site in allSites)
            {
                string mbo = GetMBOBySite(site);
                string siteRegion = GetRegionByMBO(mbo);

                if (siteRegion == region)
                {
                    sites.Add(site);
                }
            }

            foreach (var site in sites)
            {
                List<string> scopes = GetAllScopesBySite(site);

                scopes.Remove("");

                foreach (var scope in scopes)
                {
                    string sql = "IF OBJECT_ID('[dbo].[Usage_" + site + "_" + scope + "]', 'U') IS NOT NULL update dbo.[Usage_" + site + "_" + scope + "] set [Usage] = '0' where [Usage] is null";

                    db.SaveData(sql, new { }, _connectionString);
                }
            }
        }

        public void ParseSiteScopeUsageBOIMforAllSites(string region)
        {

            FixNullsInUsageTables(region);

            List<string> sites = new List<string>();

            List<string> allSites = new List<string>();

            allSites = GetAllSites();

            foreach (var site in allSites)
            {
                string mbo = GetMBOBySite(site);
                string siteRegion = GetRegionByMBO(mbo);

                if (siteRegion == region)
                {
                    sites.Add(site);
                }
            }




            foreach (var site in sites)
            {
                OutputToLog("Site: " + site);

                List<string> scopes = GetAllScopesBySite(site);
                scopes.Remove("");


                foreach (var scope in scopes)
                {
                    OutputToLog("Scope: " + scope);

                    string sql = "IF OBJECT_ID('[dbo].[Usage_" + site + "_" + scope + "]', 'U') IS NULL create table dbo.[Usage_" + site + "_" + scope + "] ([Item] nvarchar(50) not null,[Usage] nvarchar(50))";
                    db.SaveData(sql, new { }, _connectionString);

                    sql = "if object_id('dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "]', 'U') IS NOT NULL drop table dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "]";
                    db.SaveData(sql, new { }, _connectionString);

                    sql = "create table [dbo].[SiteScopeUsageBOIM_" + site + "_" + scope + "] ([Id][int] primary key identity not null, [Item][nvarchar](50) not null, [Scope][nvarchar](50) not null, [Usage][int] not null)";
                    db.SaveData(sql, new { }, _connectionString);

                    sql = "insert into dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] select distinct st.[Item],sl.[Scope],Cast(uss.[Usage] as int) from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic211 on st.[Item] = ic211.[Item_Number] inner join dbo.[Scope_Locations_" + site + "] sl on ic211.[Location_Code] = sl.[Location] inner join dbo.[Usage_" + site + "_" + scope + "] uss on st.[Item] = uss.[Item] where sl.[Scope] = @Scope order by Cast(uss.[Usage] as int) desc, st.[Item]";
                    db.SaveData(sql, new { Scope = scope }, _connectionString);

                    sql = "select distinct st.[Item],sl.[Scope] from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic211 on st.[Item] = ic211.[Item_Number] inner join dbo.[Scope_Locations_" + site + "] sl on ic211.[Location_Code] = sl.[Location] left join dbo.[Usage_" + site + "_" + scope + "] uss on st.[Item] = uss.[Item] where sl.[Scope] = @Scope and uss.item is null order by st.[Item]";
                    List<string> itemsWithNoUsageValue = db.LoadData<string, dynamic>(sql, new { Scope = scope }, _connectionString);

                    Thousandator thousandator = new Thousandator(itemsWithNoUsageValue);

                    foreach (var set in thousandator.Sets)
                    {
                        sql = "insert into dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] ([Item],[Scope],[Usage]) values ";

                        foreach (var item in set)
                        {
                            sql += "('" + item + "','" + scope + "', '0'),";
                        }

                        sql = sql.Remove(sql.Length - 1);

                        db.SaveData(sql, new { }, _connectionString);
                    }
                }

            }
        }
        public string PreviousAllByUserId(int userId, string itemId)
        {
            List<string> results = new List<string>();

            string site = GetSiteByUser(userId);
            string scope = GetUserScope(userId);
            int idInitial = GetSSUBoimId(itemId, site, scope);

            string sql = "select top 1 [Item] from dbo.[SiteScopeUsageBOIM_" + site + "_" + scope + "] where [Id] < @Id order by [Id] desc";

            results = db.LoadData<string, dynamic>(sql, new { Id = idInitial }, _connectionString);

            if (results.Count == 0)
            {
                results.Add(itemId);
            }

            return results[0];
        }


        public void Bulk_IngestPyxisLocations(string filePath, char delimiter, string site)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[7];

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            string createSql = "IF OBJECT_ID('[dbo].[PyxisLocations_" + site + "]', 'U') IS NOT NULL DROP TABLE[dbo].[PyxisLocations_" + site + "]";

            db.SaveData(createSql, new { }, _connectionString);

            createSql = "CREATE TABLE [dbo].[PyxisLocations_" + site + "] ([Id] INT NOT NULL PRIMARY KEY IDENTITY, [Station] NVARCHAR(50) NOT NULL, [Item] NVARCHAR(50) NOT NULL, [Description] NVARCHAR(50) NOT NULL)";

            db.SaveData(createSql, new { }, _connectionString);


            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.[PyxisLocations_" + site + "] ([Station],[Item],[Description]) values ";

                foreach (var row in set)
                {
                    string record0 = row.Split(delimiter)[0];

                    string record1 = row.Split(delimiter)[1];

                    string record2 = row.Split(delimiter)[2];

                    sqlInsert += "('" + record0 + "','" + record1 + "','" + record2 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

                Console.WriteLine(sqlInsert);
            }
        }

        public void DeployTable(Table_Model newTable)
        {
            string tableName = newTable.TableName;

            string sql = "IF OBJECT_ID('[dbo].[" + tableName + "]', 'U') IS NULL create table dbo.[" + tableName + "] (";

            foreach (var column in newTable.Columns)
            {
                string newColumn = "[" + column.ColumnName + "] " + column.DataTypeString + " " + column.IsPrimaryKey + " " + column.CanBeNull + ",";

                sql += newColumn;
            }

            sql = sql.Remove(sql.Length - 1);

            sql += ")";

            //Console.WriteLine(sql);

            db.SaveData(sql, new { }, _connectionString);
        }

        public List<string> GetDeployTableNames(string filePath)
        {
            List<string> tableNames = new List<string>();

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string tableName;

                while ((tableName = fileStream.ReadLine()) != null)
                {
                    tableNames.Add(tableName);
                }
            }

            return tableNames;
        }

        public List<Column_Model> GetColumns(string tableName, string filePath, char delimiter)
        {
            List<Column_Model> output = new List<Column_Model>();

            List<string> rows = new List<string>();

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            foreach (var row in rows)
            {
                List<string> columnAttributes = row.Split(delimiter).ToList();

                Column_Model newColumn = new Column_Model();

                newColumn.ColumnName = columnAttributes[0];
                newColumn.DataTypeString = columnAttributes[1];
                newColumn.IsPrimaryKey = columnAttributes[2];
                newColumn.CanBeNull = columnAttributes[3];


                output.Add(newColumn);
            }

            return output;
        }


        public void DeployAllTables(string filePath)
        {
            List<string> tableNames = new List<string>();

            tableNames = GetDeployTableNames(filePath + "\\DeployTablesList.txt");

            foreach (var table in tableNames)
            {
                Table_Model newTable = new Table_Model();

                newTable.TableName = table;
                newTable.Columns = GetColumns(table, filePath + "\\" + table + ".txt", ',');

                DeployTable(newTable);
            }


        }


        public void ReconcileSiteTableBOIMResolvedWithItemScopeResolved(string site)
        {
            List<string> resolvedItems = new List<string>();

            List<string> scopes = new List<string>();

            scopes = GetAllScopesBySite(site);

            foreach (var scope in scopes)
            {
                List<string> itemsByScope = new List<string>();

                string sql = "IF OBJECT_ID('[dbo].[ItemScopeResolved_" + site + "_" + scope + "]', 'U') IS NOT NULL select [Item] from dbo.[ItemScopeResolved_" + site + "_" + scope + "]";

                itemsByScope = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

                foreach (var item in itemsByScope)
                {
                    resolvedItems.Add(item);
                }
            }

            foreach (var item in resolvedItems)
            {
                string sql = "update dbo.[" + site + "] set [Resolved] = 'Resolved' where [Item] = @Item";

                db.SaveData(sql, new { Item = item }, _connectionString);
            }


        }


        public void InactivateSub(int subId)
        {
            string sql = "update dbo.Subs set [Active] = 'I' WHERE [Id] = @Id";

            db.SaveData(sql, new { Id = subId }, _connectionString);
        }


        public void ScanTrackula_AddEntry(ScanTrackula_AddItem_Model input)
        {
            string sql = "insert into dbo.[ScanTrackula] ([UserId],[UserName],[Created_DateTime],[ImageFileName],[FieldOne],[FieldTwo],[FieldThree],[FieldFour],[FieldFive],[FieldSix],[FieldSeven],[FieldEight],[FieldNine],[FieldTen]) values (@UserId,@UserName,@CreatedDate,@ImageFileName,@FieldOne,@FieldTwo,@FieldThree,@FieldFour,@FieldFive,@FieldSix,@FieldSeven,@FieldEight,@FieldNine,@FieldTen)";

            db.SaveData(sql, new { UserId = input.UserId, UserName = input.UserName, CreatedDate = input.Created_DateTime, ImageFileName = input.ImageFileName, FieldOne = input.FieldOne, FieldTwo = input.FieldTwo, FieldThree = input.FieldThree, FieldFour = input.FieldFour, FieldFive = input.FieldFive, FieldSix = input.FieldSix, FieldSeven = input.FieldSeven, FieldEight = input.FieldEight, FieldNine = input.FieldNine, FieldTen = input.FieldTen }, _connectionString);
        }

        public ScanTrackula_AddItem_Model ScanTrackula_GetFirstRecord()
        {
            List<ScanTrackula_AddItem_Model> output = new List<ScanTrackula_AddItem_Model>();

            string sql = "select * from dbo.[ScanTrackula]";

            output = db.LoadData<ScanTrackula_AddItem_Model, dynamic>(sql, new { }, _connectionString);

            if (output.Count == 0)
            {
                output.Add(new ScanTrackula_AddItem_Model());
            }

            return output.FirstOrDefault();
        }

        public ScanTrackula_AddItem_Model ScanTrackula_GetLastRecord()
        {
            List<ScanTrackula_AddItem_Model> output = new List<ScanTrackula_AddItem_Model>();

            string sql = "select * from dbo.[ScanTrackula]";

            output = db.LoadData<ScanTrackula_AddItem_Model, dynamic>(sql, new { }, _connectionString);

            if (output.Count == 0)
            {
                output.Add(new ScanTrackula_AddItem_Model());
            }

            return output.LastOrDefault();
        }

        public List<ScanTrackula_AddItem_Model> ScanTrackula_Search(string searchString)
        {
            List<ScanTrackula_AddItem_Model> results = new List<ScanTrackula_AddItem_Model>();

            string sql = "select * from dbo.[ScanTrackula] where [Created_DateTime] like @SearchString or [UserName] like @SearchString or [FieldOne] like @SearchString or [FieldTwo] like @SearchString or [FieldThree] like @SearchString or [FieldFour] like @SearchString or [FieldFive] like @SearchString or [FieldSix] like @SearchString or [FieldSeven] like @SearchString or [FieldEight] like @SearchString or [FieldNine] like @SearchString or [FieldTen] like @SearchString";

            results = db.LoadData<ScanTrackula_AddItem_Model, dynamic>(sql, new { SearchString = "%" + searchString + "%" }, _connectionString);

            if (results.Count == 0)
            {
                results.Add(new ScanTrackula_AddItem_Model());
            }

            return results;
        }

        public ScanTrackula_AddItem_Model ScanTrackula_RetrieveRecord(int entryId)
        {
            ScanTrackula_AddItem_Model output = new ScanTrackula_AddItem_Model();
            List<ScanTrackula_AddItem_Model> results = new List<ScanTrackula_AddItem_Model>();

            string sql = "select * from dbo.[ScanTrackula] where [Id] = @EntryId";

            results = db.LoadData<ScanTrackula_AddItem_Model, dynamic>(sql, new { EntryId = entryId }, _connectionString);

            if (results.Count == 0)
            {
                results.Add(new ScanTrackula_AddItem_Model());
            }

            output = results[0];

            return output;
        }

        public List<string> ScanTrackula_ScrapeScansFolder(string folderName)
        {
            List<string> fileNames = FileNamesByDirectory(folderName);
            List<string> output = new List<string>();

            int folderPathLength = folderName.Length + 1;

            foreach (var fileName in fileNames)
            {
                string docNumber = fileName.Remove(fileName.Length - 4);
                docNumber = docNumber.Substring(docNumber.Length - (docNumber.Length - folderPathLength));

                Console.WriteLine(docNumber);
                output.Add(docNumber);

                string sql = "Update dbo.[ScanTrackula] set [ImageFileName] = @FileName where Cast([Id] as nvarchar) = @DocNumber";

                db.SaveData(sql, new { FileName = docNumber + ".pdf", DocNumber = docNumber }, _connectionString);

            }

            return output;
        }

        public bool ScanTrackula_HasImage(int docNumber)
        {
            bool output = false;

            List<string> results = new List<string>();

            string sql = "select top 1 [ImageFileName] from dbo.[ScanTrackula] where [Id] = @DocNumber and [ImageFileName] is not null and [ImageFileName] <> ''";

            results = db.LoadData<string, dynamic>(sql, new { DocNumber = docNumber }, _connectionString);

            if (results.Count > 0)
            {
                output = true;
            }

            return output;
        }

        public List<string> FileNamesByDirectory(string directoryPath)
        {
            List<string> output = new List<string>();

            output = Directory.GetFiles(directoryPath).ToList<string>();

            return output;
        }

        public void ScanTrackula_GenerateDeliverySheetPdf(int entryId)
        {
            ScanTrackula_AddItem_Model scanEntry = new ScanTrackula_AddItem_Model();

            List<ScanTrackula_AddItem_Model> results = new List<ScanTrackula_AddItem_Model>();

            string sql = "select * from dbo.[ScanTrackula] where [Id] = @EntryId";

            results = db.LoadData<ScanTrackula_AddItem_Model, dynamic>(sql, new { EntryId = entryId }, _connectionString);

            if (results.Count > 0)
            {
                scanEntry = results[0];
            }

            string html = "<html><style>h1{font-size:36px},h2{font-size:120px}</style><body>";
            html += "<row><h1>DELIVERY SHEET</h1></row>";
            html += "<hr/>";
            html += "<row><h1>DOCUMENT# " + scanEntry.Id + "</h1></row>";
            html += "<row><h1>" + scanEntry.Created_DateTime + "</h1></row>";
            html += "<row><h1>" + scanEntry.FieldOne + "</h1></row>";
            html += "<row><h1>" + scanEntry.FieldTwo + "</h1></row>";
            html += "<row><h1>" + scanEntry.FieldThree + "</h1></row>";
            html += "<row><h1>" + scanEntry.FieldFour + "</h1></row>";
            html += "<row><h1>" + scanEntry.FieldFive + "</h1></row>";
            html += "<row><h1>" + scanEntry.FieldSix + "</h1></row>";
            html += "<row><h1>" + scanEntry.FieldSeven + "</h1></row>";
            html += "<row><h1>" + scanEntry.FieldEight + "</h1></row>";
            html += "<row><h1>" + scanEntry.FieldNine + "</h1></row>";
            html += "<row><h1>" + scanEntry.FieldTen + "</h1></row>";
            html += "</body></html>";

            var pdf = Pdf.From(html);

            byte[] pdfContent = pdf.Content();

            File.WriteAllBytes(@"wwwroot\lib\ScanScans\DeliverySheet_" + scanEntry.Id + ".pdf", pdfContent);

        }


        public void ResolveNegativeGaps(string region)
        {
            List<string> sites = new List<string>();

            List<string> allSites = new List<string>();

            allSites = GetAllSites();

            foreach (var site in allSites)
            {
                string mbo = GetMBOBySite(site);
                string siteRegion = GetRegionByMBO(mbo);

                if (siteRegion == region)
                {
                    sites.Add(site);
                }
            }

            sites.Remove("");

            foreach (var site in sites)
            {
                List<string> scopes = GetAllScopesBySite(site);

                scopes.Remove("");

                foreach (var scope in scopes)
                {
                    List<string> items = new List<string>();

                    string sql = "IF OBJECT_ID('[dbo].[" + site + "]', 'U') IS NOT NULL select [Item] from dbo.[" + site + "] where [GapDays] like '%-%'";

                    items = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

                    if (items.Count > 0)
                    {
                        foreach (var item in items)
                        {
                            if (!IsResolved(item, site, scope))
                            {
                                ToggleResolvedState(item, site, scope);
                            }
                        }
                    }
                }
            }
        }


        public void ResolveOldDstats()
        {
            List<string> sites = GetAllSites();

            sites.Remove("");

            foreach (var site in sites)
            {
                List<string> scopes = GetAllScopesBySite(site);

                scopes.Remove("");

                foreach (var scope in scopes)
                {
                    List<string> items = new List<string>();

                    string sql = "IF OBJECT_ID('[dbo].[" + site + "]', 'U') IS NOT NULL select [Item] from dbo.[" + site + "] where [Source] = 'Cardinal Dstat' and cast([StockOutDate] as datetime) < '9/10/2021'";

                    items = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

                    if (items.Count > 0)
                    {
                        foreach (var item in items)
                        {
                            if (!IsResolved(item, site, scope))
                            {
                                ToggleResolvedState(item, site, scope);
                            }
                        }
                    }
                }
            }
        }

        public bool IsItemResolvedAtSiteScope(string item, string site, string scope)
        {
            bool isResolvedAtSiteScope = false;

            List<string> results = new List<string>();

            string sql = "select * from dbo.[ItemScopeResolved_" + site + "_" + scope + "] where [Item] = @Item";

            results = db.LoadData<string, dynamic>(sql, new { Item = item }, _connectionString);

            if(results.Count > 0)
            {
                isResolvedAtSiteScope = true;
            }

            return isResolvedAtSiteScope;
        }

        public void CreateSubsListBySiteScope(string site, string scope)
        {
            string sql = "truncate table dbo.subslist";

            db.SaveData(sql, new { }, _connectionString);

            List<HasSubModel> items = new List<HasSubModel>();

            List<string> rows = new List<string>();

            //DateTime dstatDateThreshold = DateTime.Today.AddDays(-15);

            sql = "select distinct ssub.Usage,boim.Item,boim.MfrNum,boim.Description,boim.StockOutDate,boim.ReleaseDate,boim.GapDays,boim.ReasonCode,boim.StockStatus,boim.Resolved from dbo.[" + site + "] boim inner join dbo.[IC211_" + site + "] ic on boim.item = ic.item_number inner join dbo.[Scope_Locations_" + site + "] sl on ic.location_code = sl.location left join dbo.[SiteScopeUsageBOIM_" + site + "_" + scope +"] ssub on boim.item = ssub.item where sl.scope = @Scope and boim.GapDays not like '%-%' order by ssub.usage desc";

            items = db.LoadData<HasSubModel, dynamic>(sql, new { Scope = scope }, _connectionString);

            foreach (var item in items)
            {
                string hasSubs = ItemHasSubs(item.Item).ToString();


                item.HasSub = hasSubs;

                item.Resolved = "Open";

                if(IsItemResolvedAtSiteScope(item.Item,site,scope))
                {
                    item.Resolved = "Resolved";
                }

                string lastNote = "";

                if (item.Resolved == "Resolved")
                {

                    sql = "select top 1 notes.Note from dbo.Notes notes inner join dbo.users us on notes.UserId = us.Id where notes.item = @Item and notes.Site = @Site and us.scope = @Scope order by Notes.id desc";

                    List<string> results = new List<string>();

                    results = db.LoadData<string, dynamic>(sql, new { Item = item.Item, Site = site, Scope = scope }, _connectionString);



                    if (results.Count > 0)
                    {
                        ; lastNote = results[0];
                    }
                    else
                    {
                        lastNote = "";
                    }

                    lastNote = lastNote.Replace('\'', ' ');
                    lastNote = lastNote.Replace('\"', ' ');
                    lastNote = lastNote.Replace(',', '-');

                    item.Note = lastNote;
                }

                string newRow = "(" + item.Usage + ",'" + item.Item + "','" + item.MfrNum + "','" + item.Description + "','" + item.StockOutDate + "','" + item.ReleaseDate + "','" + item.GapDays + "','" + item.ReasonCode + "','" + item.StockStatus + "','" + item.HasSub + "','" + item.Resolved + "','" + item.Note + "'),";

                rows.Add(newRow);
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                sql = "insert into dbo.[SubsList] (Usage,Item,MfrNum,Description,StockOutDate,ReleaseDate,GapDays,ReasonCode,StockStatus,HasSub,Resolved,Note) values ";

                foreach (var item in set)
                {
                    sql += item;
                }

                sql = sql.Remove(sql.Length - 1);

                db.SaveData(sql, new { }, _connectionString);
            }


        }

        public void PurgeErroneousResolves(string region)
        {
            List<string> sites = new List<string>();

            List<string> allSites = new List<string>();

            allSites = GetAllSites();

            foreach (var site in allSites)
            {
                string mbo = GetMBOBySite(site);
                string siteRegion = GetRegionByMBO(mbo);

                if (siteRegion == region)
                {
                    sites.Add(site);
                }
            }

            foreach (var site in sites)
            {
                List<string> scopes = GetAllScopesBySite(site);

                scopes.Add("ALL");


                foreach (var scope in scopes)
                {
                    string sql = "IF OBJECT_ID('[dbo].[" + site + "]','U') IS NOT NULL select distinct [Item] from dbo.[" + site + "] st inner join dbo.[IC211_" + site + "] ic on st.item = ic.item_number inner join dbo.[Scope_Locations_" + site + "] sl on sl.location = ic.location_code where sl.Scope = @Scope";

                    List<string> boimItems = db.LoadData<string, dynamic>(sql, new { Scope = scope }, _connectionString);


                    sql = "IF OBJECT_ID('[DBO].[ItemScopeResolved_" + site + "_" + scope + "]','U') IS NOT NULL select distinct [Item] from dbo.[ItemScopeResolved_" + site + "_" + scope + "]";

                    List<string> resolvedItems = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

                    if (resolvedItems.Count > 0)
                    {
                        foreach (var resolvedItem in resolvedItems)
                        {
                            if (!boimItems.Contains(resolvedItem))
                            {
                                sql = "delete dbo.[ItemScopeResolved_" + site + "_" + scope + "] where [Item] = @ResolvedItem";

                                db.SaveData(sql, new { ResolvedItem = resolvedItem }, _connectionString);

                            }
                        }
                    }
                }
            }
        }

        public void AddSite(string site, string siteName, string company, string scopesListFilePath, string users_LocationsFilePath, string ic211FilePath, string locationSiteScopeFilePath, string usageFilePath, string rq201FilePath)
        {
            //add Site name to Site tables

            string sql = "insert into dbo.[SiteName] ([Site],[Name],[Company]) values (@Site,@SiteName,@Company)";
            db.SaveData(sql, new { Site = site, SiteName = siteName, Company = company }, _connectionString);

            sql = "insert into dbo.[Sites] ([Site],[MBO]) values (@Site,@Company)";
            db.SaveData(sql, new { Site = site, Company = company }, _connectionString);


            //add master site UserId
            
            sql = "select top 1 [Id] from dbo.[Users] order by [Id] desc";
            int lastUserId = db.LoadData<int, dynamic>(sql, new { }, _connectionString).First();

            sql = "insert into dbo.Users ([Id],[UserName],[Password],[AccessPermissions],[Scope],[CanEditSubs]) values (@LastUserId,@UserName,@UserName,@UserName,'ALL',0)";
            db.SaveData(sql, new { LastUserId = lastUserId + 1, UserName = site }, _connectionString);


            //create UserIds for each scope in ScopesList

            List<string> scopesToAdd = IngestScopesList(scopesListFilePath);
            
            sql = "insert into dbo.users ([Id],[UserName],[Password],[AccessPermissions],[Scope],[CanEditSubs]) values ";

            foreach(var scope in scopesToAdd)
            {
                string sqlUserIdFind = "select top 1 [Id] from dbo.[Users] order by [Id] desc";
                lastUserId = db.LoadData<int, dynamic>(sqlUserIdFind, new { }, _connectionString).First();
                lastUserId++;

                sql += "(" + lastUserId + ",'" + site + "-" + scope + "','" + site + "-" + scope + "','" + site + "','" + scope + "',0),";
            }

            sql = sql.Remove(sql.Length - 1);

            db.SaveData(sql, new { }, _connectionString);


            //import Users_Location data for scopes with columns in this order: [UserId],[Location]

            BulkIngestUsers_Locations(users_LocationsFilePath, ',', site);


            //import IC211 data with columns in this order: Item_Number,PIV_VEN_ITEM,Description,Location_Code,Company,ITL_ACTIVE_STATUS_XLT

            BulkIngestIC211(ic211FilePath, ',', site);



            //Scrape IC211 to populate User_Locations data for Master Site UserId

            sql = "select [Id] from dbo.users where [UserName] = @Site";
            int masterSiteUserId = db.LoadData<int, dynamic>(sql, new { Site = site }, _connectionString).First();

            BulkIngestUsers_LocationsFromIC211File(ic211FilePath, ',', site, masterSiteUserId);


            //Scrape IC211 to append Locations_Sites

            sql = "select distinct [Location_Code] from dbo.[IC211] where [Company] = @Company and [Site] = @Site order by [Location_Code]";
            List<string> locations = db.LoadData<string, dynamic>(sql, new { Company = company, Site = site }, _connectionString);

            Thousandator thousandator = new Thousandator(locations);

            foreach (var set in thousandator.Sets)
            {

                sql = "insert into dbo.Locations_Sites ([Location],[SITE]) values ";

                foreach (var location in set)
                {
                    sql += "('" + location + "','" + site + "'),";
                }

                sql = sql.Remove(sql.Length - 1);

                db.SaveData(sql, new { }, _connectionString);

            }


            

            //Ingest LocationSiteScope data with columns in this order: Location,Site,Scope

            BulkIngestLocationSiteScope(locationSiteScopeFilePath, ',');


            //Ingest MasterUsage data with columns in this order: [Received_Quantity],[UOM],[Location],[Item_Number],[PO_Number]

            BulkIngestUsage(usageFilePath, ',', company);


            //Ingest RQ201 data with columns in this order: [RQL_REQ_LOCATION],[Name],[RQL_FROM_LOCATION],[RQL_ACTIVE_STATUS]

            BulkIngestRQ201(rq201FilePath, ',', company);

        }


      

        public List<string> IngestScopesList(string filePath)
        {
            List<string> rows = new List<string>();
            
            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }
            
            return rows;
        }

        public void BulkIngestUsers_LocationsFromIC211File(string filePath, char delimiter, string site, int userId)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[5];

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.[Users_Locations] ([UserId],[Location],[Site]) values ";

                foreach (var row in set)
                {
                    string record0 = userId.ToString();
                    
                    string record1 = row.Split(delimiter)[3];

                    string record2 = site;

                    sqlInsert += "('" + record0 + "','" + record1 + "','" + record2 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

            }

        }

        public void BulkIngestRQ201(string filePath, char delimiter, string company)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[5];

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.[RQ201] ([Company],[RQL_REQ_LOCATION],[Name],[RQL_FROM_LOCATION],[RQL_ACTIVE_STATUS]) values ";

                foreach (var row in set)
                {
                    string record0 = company;

                    string record1 = row.Split(delimiter)[0];

                    string record2 = row.Split(delimiter)[1];

                    string record3 = row.Split(delimiter)[2];

                    string record4 = row.Split(delimiter)[3];

                    sqlInsert += "('" + record0 + "','" + record1 + "','" + record2 + "','" + record3 + "','" + record4 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

            }

        }


        public void BulkIngestLocationSiteScope(string filePath, char delimiter)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[3];

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.[LocationSiteScope] ([Location],[Site],[Scope]) values ";

                foreach (var row in set)
                {
                    string record0 = row.Split(delimiter)[0];

                    string record1 = row.Split(delimiter)[1];

                    string record2 = row.Split(delimiter)[2];

                    sqlInsert += "('" + record0 + "','" + record1 + "','" + record2 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

            }

        }

        public void UsageCrunch()
        {
            string sql = "truncate table dbo.UsageCrunchOutput";

            db.SaveData(sql, new { }, _connectionString);
            
            List<string> sites = new List<string>();

            sql = "select distinct [ICL_PO_NAME] from dbo.[PO131]";

            sites = db.LoadData<string, dynamic>(sql, new { }, _connectionString);

            sites.Remove("");

            if(sites.Count > 0)
            foreach(var site in sites)
            {
                sql = "select distinct [Item_Number] from dbo.PO131 where [ICL_PO_NAME] = @Site";

                List<string> items = db.LoadData<string, dynamic>(sql, new {Site = site }, _connectionString);
           

                if(items.Count > 0)
                {
                    foreach(var item in items)
                        {
                            sql = "select distinct [PLI_ENT_BUY_UOM] from dbo.PO131 where [ICL_PO_NAME] = @Site and [Item_Number] = @Item";

                            List<string> uoms = db.LoadData<string, dynamic>(sql, new {Site = site, Item = item }, _connectionString);

                            if(uoms.Count > 0)
                            {
                                foreach(var uom in uoms)
                                {
                                    sql = "select sum([Received_Quantity]) from dbo.PO131 where [ICL_PO_NAME] = @Site and [Item_Number] = @Item and [PLI_ENT_BUY_UOM] = @UOM";

                                    List<string> results = db.LoadData<string, dynamic>(sql, new { Site = site, Item = item, UOM = uom }, _connectionString);

                                    string qty = "ERROR: NO RESULT";

                                    if(results.Count > 0)
                                    {
                                        qty = results[0];
                                    }

                                    sql = "insert into dbo.UsageCrunchOutput ([Site],[Item],[UOM],[QTY]) values (@Site,@Item,@UOM,@QTY)";

                                    db.SaveData(sql, new { Site = site, Item = item, UOM = uom, QTY = qty }, _connectionString);
                                }
                                
                            }
                        }
                }
            }
        }

       public void BulkIngestUsage(string filePath, char delimiter, string company)
        {
            List<string> rows = new List<string>();
            string[] recordList = new string[5];

            using (StreamReader fileStream = new StreamReader(filePath))
            {
                string row;

                while ((row = fileStream.ReadLine()) != null)
                {
                    rows.Add(row);
                }
            }

            Thousandator thousandator = new Thousandator(rows);

            foreach (var set in thousandator.Sets)
            {
                string sqlInsert = "insert into dbo.[Master_Usage] ([Received_Quantity],[UOM],[Location],[Item_Number],[PO_Number],[Company]) values ";

                foreach (var row in set)
                {
                    string record0 = row.Split(delimiter)[0];

                    string record1 = row.Split(delimiter)[1];

                    string record2 = row.Split(delimiter)[2];

                    string record3 = row.Split(delimiter)[3];

                    string record4 = row.Split(delimiter)[4];

                    string record5 = company;

                    sqlInsert += "('" + record0 + "','" + record1 + "','" + record2 + "','" + record3 + "','" + record4 + "','" + record5 + "'),";

                }

                sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1);

                db.SaveData(sqlInsert, new { }, _connectionString);

            }
        }















    }
}