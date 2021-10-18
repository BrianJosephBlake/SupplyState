using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DataAccessLibrary;
using RazorPagesUI.PublicLibrary;

namespace RazorPagesUI.Pages
{
    public class GloomhavenModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string SiteName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string UserName { get; set; }

        [BindProperty(SupportsGet = true)]
        public int UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool HasAccess { get; set; }

        public int SiteUserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ItemId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int DisplayState { get; set; }

        [BindProperty]
        public bool UserHasItems { get; set; }

        [BindProperty]
        public List<string> AllSitesList { get; set; }

        [BindProperty]
        public List<string> AllMBOsList { get; set; }

        /// <summary>
        /// ////////////////////////
        /// </summary>


        SqlDataAccess db = new SqlDataAccess();

        public List<Entity_Model> AllEntities { get; set; }

        [BindProperty(SupportsGet = true)]
        public int DisplayType { get; set; }

        [BindProperty]
        public string NewName { get; set; }

        [BindProperty]
        public string NewEntityType { get; set; }

        [BindProperty]
        public int NewIsPlayer { get; set; }

        [BindProperty]
        public int NewIsElite { get; set; }

        [BindProperty]
        public int NewEntityIndex { get; set; }

        [BindProperty]
        public int NewHealth { get; set; }

        [BindProperty]
        public int NewEntityPosition { get; set; }

        [BindProperty(SupportsGet = true)]
        public int RemoveEntityId { get; set; }

        [BindProperty]
        public int HealthDelta { get; set; }

        [BindProperty (SupportsGet = true)]
        public int EntityFocus { get; set; }

        //[BindProperty]
        //public int[,] HexagonNameArray { get; set; }

        [BindProperty(SupportsGet = true)]
        public int HexSelected { get; set; }

        [BindProperty]
        public int NameIndex { get; set; }

        [BindProperty]
        List<Hex_Model> BoardPlacementsList { get; set; }

        [BindProperty]
        public string NewBoardChar { get; set; }

        [BindProperty]
        public List<string> EntityTypes { get; set; }

        [BindProperty]
        public List<string> EntityActionTypes { get; set; }

        [BindProperty]
        public int ActionDisplay { get; set; }

        [BindProperty]
        public string ActionTypeSelected { get; set; }

        [BindProperty (SupportsGet = true)]
        public bool IsVertical { get; set; }

        [BindProperty (SupportsGet = true)]
        public bool MovementMapVisible { get; set; }

        [BindProperty]
        public List<HexValuePositionPair> MovementMap { get; set; }

        public void OnGet()
        {
            //InitializeBoard();

            BuildBoard();
            UpdateBoardPlacementTable();

            //ConnString = ConnectionString.GetConnectionString();

            AllEntities = new List<Entity_Model>();
            AllEntities = GetAllEntites();

            EntityTypes = new List<string>();
            EntityTypes = GetAllEntityTypes();

            EntityActionTypes = new List<string>();
            EntityActionTypes = GetAllEntityActionTypes();

            BoardPlacementsList = new List<Hex_Model>();
            BoardPlacementsList = RetrieveHexPositionData();

            if (MovementMapVisible)
            {
                MovementMap = new List<HexValuePositionPair>();
                MovementMap = CreateMovementMap(HexSelected);
            }



        }

        public IActionResult OnPostEmptyHexSelect()
        {
            //Console.WriteLine("Hex Selected: " + HexSelected);
            //Console.WriteLine("Hex % 2: " + HexSelected % 2);

            //CreateMovementMap(HexSelected);

            return RedirectToPage("/Gloomhaven", new { HexSelected = HexSelected, DisplayType = 1, MovementMapVisible = false});

        }

        public IActionResult OnPostEntityActionSelect()
        {
            if(ActionTypeSelected == "Move")
            {
                

                return RedirectToPage("/Gloomhaven", new { HexSelected = HexSelected, DisplayType = 0, ActionDisplay = ActionDisplay, MovementMapVisible = true, EntityFocus = HexSelected });
            }

            return RedirectToPage("/Gloomhaven",new { HexSelected = HexSelected, DisplayType = 2, ActionDisplay = ActionDisplay });
        }


        public IActionResult OnPostMoveTo()
        {
            EntityFocus = WhatsOnHex(EntityFocus);

            MoveEntity(EntityFocus, HexSelected);

            return RedirectToPage("/Gloomhaven", new { DisplayType = 0 });
        }

        public IActionResult OnPostAddNewEntity()
        {
            Console.WriteLine("New Entity: " + NewName + " " + NewEntityType + " " + NewHealth + " " + NewEntityIndex + " " + NewIsElite + " " + NewIsPlayer);

            Entity_Model newEntity = new Entity_Model
            {
                Name = NewName,
                EntityType = NewEntityType,
                IsPlayer = NewIsPlayer,
                IsElite = NewIsElite,
                EntityIndex = NewEntityIndex,
                Health = NewHealth,
                BoardPosition = NewEntityPosition,
                BoardChar = NewBoardChar

            };

            AddEntity(newEntity);
            return RedirectToPage("/Gloomhaven", new { DisplayType = 0});
        }

        public IActionResult OnPostChangeDisplayType()
        {
            return RedirectToPage("/Gloomhaven", new { DisplayType = 1 });
        }

        public IActionResult OnPostRemoveEntity()
        {
            Console.WriteLine(RemoveEntityId);
            DeleteEntity(RemoveEntityId);
            return RedirectToPage("/Gloomhaven", new { DisplayState = 0 });
        }

        public IActionResult OnPostEndTurn()
        {
            UpdateEntityHealth(EntityFocus, HealthDelta);
            return RedirectToPage("/Gloomhaven", new { DisplayType = 0 });
        }

        public IActionResult OnPostInitializeBoard()
        {
            InitializeBoard();

            return RedirectToPage("/Gloomhaven", new { DisplayType = 1 });
        }



        public int WhatsOnHex(int hexId)
        {
            int entityId = 0;

            List<int> results = new List<int>();

            string sql = "select top 1 [Id] from dbo.EntityList where [BoardPosition] = @HexId";

            results = db.LoadData<int, dynamic>(sql, new { HexId = hexId }, ConnectionString.GetConnectionString());

            if(results.Count > 0)
            {
                entityId = results[0];
            }

            Console.WriteLine("Found EntityId {0} at HexId {1}", entityId, hexId);

            return entityId;
        }

        public string WhatsEntityType(int entityId)
        {
            string type = "";

            List<string> results = new List<string>();

            string sql = "select top 1 [EntityType] from dbo.EntityList where [Id] = @EntityId";

            results = db.LoadData<string, dynamic>(sql, new { EntityId = entityId }, ConnectionString.GetConnectionString());

            if(results.Count > 0)
            {
                type = results[0];
            }

            return type;
        }

        public void AddEntity(Entity_Model newEntity)
        {

            Console.WriteLine("Adding New Entity: " + newEntity.Name);

            string sql = "insert into dbo.EntityList (Name,EntityType,IsPlayer,IsElite,EntityIndex,Health,BoardPosition,BoardChar) values (@Name,@EntityType,@IsPlayer,@IsElite,@EntityIndex,@Health,@BoardPosition,@BoardChar)";
            db.SaveData(sql, new { Name = newEntity.Name, EntityType = newEntity.EntityType, IsPlayer = newEntity.IsPlayer, IsElite = newEntity.IsElite, EntityIndex = newEntity.EntityIndex, Health = newEntity.Health, BoardPosition = NewEntityPosition, BoardChar = newEntity.BoardChar}, ConnectionString.GetConnectionString());

            sql = "select top 1 [Id] from dbo.EntityList where [BoardChar] = @BoardChars";
            int entityId = db.LoadData<int, dynamic>(sql, new { BoardChars = newEntity.BoardChar }, ConnectionString.GetConnectionString())[0];

            sql = "update dbo.BoardPlacement set [EntityId] = @EntityId where [HexId] = @HexId";
            db.SaveData(sql, new { EntityId = entityId, HexId = newEntity.BoardPosition },ConnectionString.GetConnectionString());
        }

        public void DeleteEntity(int entityId)
        {
            string sql = "delete from dbo.EntityList where Id = @Id";
            db.SaveData(sql, new { Id = entityId }, ConnectionString.GetConnectionString());
        }

        public void MoveEntity(int entityId, int moveToHexId)
        {
            Console.WriteLine("Moving EntityId {0} to HexId {1}", entityId, moveToHexId);

            string sql = "update dbo.EntityList set [BoardPosition] = @MoveToHexId where [Id] = @EntityId";

            db.SaveData(sql, new { MoveToHexId = moveToHexId, EntityId = entityId }, ConnectionString.GetConnectionString());
        }

        public List<Entity_Model> GetAllEntites()
        {
            List<Entity_Model> output = new List<Entity_Model>();

            string sql = "select * from dbo.EntityList order by [EntityIndex]";
            output = db.LoadData<Entity_Model, dynamic>(sql, new { }, ConnectionString.GetConnectionString());

            if(output.Count == 0)
            {
                output.Add(new Entity_Model
                {
                    Name = "",
                    EntityType = "",
                    IsPlayer = 0,
                    IsElite = 0,
                    EntityIndex = 0,
                    Health = 0
                });
            }

            return output;
        }

        public List<string> GetAllEntityTypes()
        {
            List<string> allEntityTypes = new List<string>();

            string sql = "select [EntityType] from dbo.EntityTypes";

            allEntityTypes = db.LoadData<string, dynamic>(sql, new { }, ConnectionString.GetConnectionString());

            return allEntityTypes;
        }

        public void UpdateEntityHealth(int entityId, int heatlhDelta)
        {
            int currentHealth = 0;

            string sql = "select [Health] from dbo.EntityList where [Id] = @Id";
            currentHealth = db.LoadData<int, dynamic>(sql, new { Id = entityId }, ConnectionString.GetConnectionString())[0];

            int newHealth = currentHealth - heatlhDelta;

            sql = "update dbo.EntityList set [Health] = @NewHealth where [Id] = @EntityId";
            db.SaveData(sql, new { NewHealth = newHealth, EntityId = entityId }, ConnectionString.GetConnectionString());
            
        }

        public int[,] FillBoardHexNames(int[,] hexNameList)
        {
            int n = 0;
          
         
                for (int i = 0; i < 12; i++)
                {
                    for (int j = 0; j < 12; j++)
                    {
                        hexNameList[i, j] = n + 1;
                        n++;
                    }
                }

            return hexNameList;
            
        }

        public int IncrementNameIndex()
        {
            NameIndex++;

            return NameIndex;
        }

        public void InitializeBoard()
        {
            string sql = "truncate table dbo.BoardPlacement";

            db.SaveData(sql, new { }, ConnectionString.GetConnectionString());

            sql = "insert into dbo.BoardPlacement ([HexId],[EntityId],[BoardChars]) values ";

            for (int i = 1; i <= 144; i++)
            {
                sql += string.Format("({0},0,''),", i);
            }

            sql = sql.Remove(sql.Length - 1);

            db.SaveData(sql, new { }, ConnectionString.GetConnectionString());


            sql = "truncate table dbo.EntityList";

            db.SaveData(sql, new { }, ConnectionString.GetConnectionString());
        }

        public List<Hex_Model> RetrieveHexPositionData()
        {
            List<Hex_Model> output = new List<Hex_Model>();

            string sql = "select * from dbo.BoardPlacement";

            output = db.LoadData<Hex_Model, dynamic>(sql, new { }, ConnectionString.GetConnectionString());

            return output;
        }

        public List<string> GetAllEntityActionTypes()
        {
            List<string> actionTypes = new List<string>();

            string sql = "select [EntityActionType] from dbo.EntityActionTypes";

            actionTypes = db.LoadData<string, dynamic>(sql, new { }, ConnectionString.GetConnectionString());

            return actionTypes;
        }

        public string GetHexEntityChars(int hexId)
        {
            Hex_Model hex = BoardPlacementsList.Find(x => x.HexId == hexId);

            List<string> results = new List<string>();

            return hex.BoardChars;
        }

        public List<HexValuePositionPair> CreateMovementMap(int selectedHexId)
        {
            List<HexValuePositionPair> board = new List<HexValuePositionPair>();
            int n = 0;
            HexValuePositionPair focusHex = new HexValuePositionPair();

            List<HexValuePositionPair> oneSpaceList = new List<HexValuePositionPair>();
            List<HexValuePositionPair> orbitalElementsList = new List<HexValuePositionPair>();
            List<List<HexValuePositionPair>> orbitalsList = new List<List<HexValuePositionPair>>();
            List<HexValuePositionPair> hexChecked = new List<HexValuePositionPair>();
            //List<HexValuePositionPair> hexClaimed = new List<HexValuePositionPair>();

            List<int> output = new List<int>();
            

            for (int k = 0; k < 12; k++)
            {
                for (int j = 0; j < 12; j++)
                {
                    n++;

                    board.Add(new HexValuePositionPair
                    {
                        Row = k,
                        Column = j,
                        Value = n
                    }) ;

                }
            }

            List<int> obstacles = new List<int>();

            string sql = "select [HexId] from dbo.BoardPlacement where [EntityId] <> 0 and [HexId] <> @SelectedHex";
            obstacles = db.LoadData<int, dynamic>(sql, new {SelectedHex = selectedHexId }, ConnectionString.GetConnectionString());

            focusHex = board.Find(x => x.Value == selectedHexId);

            orbitalElementsList.Add(focusHex);

            oneSpaceList.Add(new HexValuePositionPair
            {
                Row = focusHex.Row,
                Column = focusHex.Column,
                Value = focusHex.Value,
                RelativeDistanceToFocus = 0
            }) ;


            orbitalsList.Add(orbitalElementsList);

            int i = 0;

            List<int> hexCountDown = new List<int>();

            for (int k = 0; k < 12; k++)
            {
                for (int j = 0; j < 12; j++)
                {
                    i++;

                    if((k % 2 == 0 && j % 2 == 0) || (k % 2 > 0 && j % 2 > 0))
                    {

                        hexCountDown.Add(i);
                    }
                }
            }

            foreach (var hex in obstacles)
            {
               
                    hexCountDown.Remove(hex);
                
            }


            i = 0;

            while(hexCountDown.Count > 0)
            {
                //Console.WriteLine("Hex Count Left: " + hexCountDown.Count + " and i= " + i);
                List<HexValuePositionPair> nextList = new List<HexValuePositionPair>();

                foreach (var hex in orbitalsList[i])
                {
                   

                    if (!hexChecked.Exists(x => x.Value == hex.Value) && !obstacles.Exists(x => x == hex.Value))
                    {
                        //Console.WriteLine("Beginning Hex: " + hex.Row + "," + hex.Column);

                        sql = "select top 1 [Row],[Column] from dbo.Board where [Row] = @Row and [Column] = @Column";

                        List<int> results = new List<int>();
                        results = db.LoadData<int, dynamic>(sql, new { Row = hex.Row, Column = hex.Column - 2}, ConnectionString.GetConnectionString());


                        if (results.Count > 0)
                        {

                            oneSpaceList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row && x.Column == hex.Column - 2).Row,
                                Column = board.Find(x => x.Row == hex.Row && x.Column == hex.Column - 2).Column,
                                Value = board.Find(x => x.Row == hex.Row && x.Column == hex.Column - 2).Value,
                                RelativeDistanceToFocus = i + 1
                            });

                            nextList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row && x.Column == hex.Column - 2).Row,
                                Column = board.Find(x => x.Row == hex.Row && x.Column == hex.Column - 2).Column,
                                Value = board.Find(x => x.Row == hex.Row && x.Column == hex.Column - 2).Value,
                                RelativeDistanceToFocus = i + 1
                            });

                        }

                        if (board.Exists(x => x.Row == hex.Row && x.Column == hex.Column + 2))
                        {
                            oneSpaceList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row && x.Column == hex.Column + 2).Row,
                                Column = board.Find(x => x.Row == hex.Row && x.Column == hex.Column + 2).Column,
                                Value = board.Find(x => x.Row == hex.Row && x.Column == hex.Column + 2).Value,
                                RelativeDistanceToFocus = i + 1
                            });


                            nextList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row && x.Column == hex.Column + 2).Row,
                                Column = board.Find(x => x.Row == hex.Row && x.Column == hex.Column + 2).Column,
                                Value = board.Find(x => x.Row == hex.Row && x.Column == hex.Column + 2).Value,
                                RelativeDistanceToFocus = i + 1
                            });
                        }

                        if (board.Exists(x => x.Row == hex.Row - 1 && x.Column == hex.Column - 1))
                        {
                            oneSpaceList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row - 1 && x.Column == hex.Column - 1).Row,
                                Column = board.Find(x => x.Row == hex.Row - 1 && x.Column == hex.Column - 1).Column,
                                Value = board.Find(x => x.Row == hex.Row - 1 && x.Column == hex.Column - 1).Value,
                                RelativeDistanceToFocus = i + 1
                            });

                            nextList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row - 1 && x.Column == hex.Column - 1).Row,
                                Column = board.Find(x => x.Row == hex.Row - 1 && x.Column == hex.Column - 1).Column,
                                Value = board.Find(x => x.Row == hex.Row - 1 && x.Column == hex.Column - 1).Value,
                                RelativeDistanceToFocus = i + 1
                            });
                        }

                        if (board.Exists(x => x.Row == hex.Row - 1 && x.Column == hex.Column + 1))
                        {
                            oneSpaceList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row - 1 && x.Column == hex.Column + 1).Row,
                                Column = board.Find(x => x.Row == hex.Row - 1 && x.Column == hex.Column + 1).Column,
                                Value = board.Find(x => x.Row == hex.Row - 1 && x.Column == hex.Column + 1).Value,
                                RelativeDistanceToFocus = i + 1
                            });

                            nextList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row - 1 && x.Column == hex.Column + 1).Row,
                                Column = board.Find(x => x.Row == hex.Row - 1 && x.Column == hex.Column + 1).Column,
                                Value = board.Find(x => x.Row == hex.Row - 1 && x.Column == hex.Column + 1).Value,
                                RelativeDistanceToFocus = i + 1
                            });
                        }

                        if (board.Exists(x => x.Row == hex.Row && x.Column == hex.Column + 2))
                        {
                            oneSpaceList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row && x.Column == hex.Column + 2).Row,
                                Column = board.Find(x => x.Row == hex.Row && x.Column == hex.Column + 2).Column,
                                Value = board.Find(x => x.Row == hex.Row && x.Column == hex.Column + 2).Value,
                                RelativeDistanceToFocus = i + 1
                            });

                            nextList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row && x.Column == hex.Column + 2).Row,
                                Column = board.Find(x => x.Row == hex.Row && x.Column == hex.Column + 2).Column,
                                Value = board.Find(x => x.Row == hex.Row && x.Column == hex.Column + 2).Value,
                                RelativeDistanceToFocus = i + 1
                            });
                        }

                        if (board.Exists(x => x.Row == hex.Row + 1 && x.Column == hex.Column + 1))
                        {
                            oneSpaceList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row + 1 && x.Column == hex.Column + 1).Row,
                                Column = board.Find(x => x.Row == hex.Row + 1 && x.Column == hex.Column + 1).Column,
                                Value = board.Find(x => x.Row == hex.Row + 1 && x.Column == hex.Column + 1).Value,
                                RelativeDistanceToFocus = i + 1
                            });

                            nextList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row + 1 && x.Column == hex.Column + 1).Row,
                                Column = board.Find(x => x.Row == hex.Row + 1 && x.Column == hex.Column + 1).Column,
                                Value = board.Find(x => x.Row == hex.Row + 1 && x.Column == hex.Column + 1).Value,
                                RelativeDistanceToFocus = i + 1
                            });
                        }

                        if (board.Exists(x => x.Row == hex.Row + 1 && x.Column == hex.Column - 1))
                        {
                            oneSpaceList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row + 1 && x.Column == hex.Column - 1).Row,
                                Column = board.Find(x => x.Row == hex.Row + 1 && x.Column == hex.Column - 1).Column,
                                Value = board.Find(x => x.Row == hex.Row + 1 && x.Column == hex.Column - 1).Value,
                                RelativeDistanceToFocus = i + 1
                            });

                            nextList.Add(new HexValuePositionPair
                            {
                                Row = board.Find(x => x.Row == hex.Row + 1 && x.Column == hex.Column - 1).Row,
                                Column = board.Find(x => x.Row == hex.Row + 1 && x.Column == hex.Column - 1).Column,
                                Value = board.Find(x => x.Row == hex.Row + 1 && x.Column == hex.Column - 1).Value,
                                RelativeDistanceToFocus = i + 1
                            });
                        }

                        hexChecked.Add(hex);
                        int hexValue = hex.Value;
                        
                        hexCountDown.Remove(hexCountDown.Find(x => x == hexValue));


                    }

                    //foreach (var element in nextList)
                    //{
                    //    Console.WriteLine(string.Format("Orbitals {0}: {1}", i, element.Value));
                    //}

                    

                    

                }
                orbitalsList.Add(nextList);
                i++;
            }

            //oneSpaceList.Sort((x, y) => x.RelativeDistanceToFocus.CompareTo(y.RelativeDistanceToFocus));

            Console.WriteLine("Hexes Remaining in HexCountdown: {0}", hexCountDown.Count);


            //foreach (var hex in oneSpaceList)
            //{
            //    Console.WriteLine(string.Format("Hex Value {0} / Distance {1}", hex.Value, hex.RelativeDistanceToFocus));
            //}

            //foreach(var hex in hexChecked)
            //{
            //    Console.WriteLine(hex.Value);
            //}

            return oneSpaceList;

        }

        public void UpdateBoardPlacementTable()
        {

            string sql = "update dbo.BoardPlacement set [EntityId] = 0, [BoardChars] = ''";

            db.SaveData(sql, new { }, ConnectionString.GetConnectionString());

            sql = "update dbo.BoardPlacement set [EntityId] = el.[Id], [BoardChars] = el.[BoardChar] from dbo.BoardPlacement bp inner join dbo.EntityList el on bp.[HexId] = el.[BoardPosition] where bp.[HexId] = el.[BoardPosition]";

            db.SaveData(sql, new { }, ConnectionString.GetConnectionString());

        }

        public void BuildBoard()
        {
            string sql = "IF OBJECT_ID('[dbo].[Board]', 'U') IS NOT NULL DROP TABLE[dbo].[Board]";

            db.SaveData(sql, new { }, ConnectionString.GetConnectionString());

            sql = "CREATE TABLE [dbo].[Board] ([Id] INT NOT NULL PRIMARY KEY IDENTITY,[Row] INT NOT NULL,[Column] INT NOT NULL)";

            db.SaveData(sql, new { }, ConnectionString.GetConnectionString());

            sql = "insert into dbo.Board ([Row],[Column]) values ";

            for (int i = 0; i < 12; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    sql += string.Format("({0},{1}),", i, j);
                }
            }

            sql = sql.Remove(sql.Length - 1);

            db.SaveData(sql, new { }, ConnectionString.GetConnectionString());
        }

        public bool IsHexObstacleToPlayerAlly(int hex)
        {
            bool isObstacle = false;

            List<int> results = new List<int>();

            string sql = "select top 1 [EntityId] from dbo.BoardPlacement where [HexId] = @Hex";

            results = db.LoadData<int, dynamic>(sql, new { Hex = hex }, ConnectionString.GetConnectionString());

            if(results[0] != 0)
            {
                isObstacle = true;
            }

            return isObstacle;
        }


    }





    public class Entity_Model
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string EntityType { get; set; }

        public int  IsPlayer { get; set; }

        public int IsElite { get; set; }

        public int EntityIndex { get; set; }

        public int Health { get; set; }

        public int BoardPosition { get; set; }

        public string BoardChar { get; set; }
    }

    public class Hex_Model
    {
        public int HexId { get; set; }

        public int EntityId { get; set; }

        public string BoardChars { get; set; }
    }

    public class HexValuePositionPair
    {
        public int Row { get; set; }

        public int Column { get; set; }

        public int Value { get; set; }

        public int RelativeDistanceToFocus { get; set; }
    }
   
}
