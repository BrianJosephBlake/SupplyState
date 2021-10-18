using System;
namespace DataAccessLibrary.Models
{
    public class IC211_Model
    {
        public IC211_Model()
        {
        }

        public int Id { get; set; }
        
        public string Item_Number { get; set; }

        public string PIV_VEN_ITEM { get; set; }

        public string Description { get; set; }

        public string Location_Code { get; set; }

        public string Company { get; set; }

        public string ITL_ACTIVE_STATUS_XLT { get; set; }

        public string Site { get; set; }

    }
}
