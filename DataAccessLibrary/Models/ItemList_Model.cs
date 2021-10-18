using System;
namespace DataAccessLibrary.Models
{
    public class ItemList_Model
    {
        public ItemList_Model()
        {
        }

        public int Id { get; set; }

        public string Item { get; set; }

        public string MfrNum { get; set; }

        public string Description { get; set; }

        public string ReleaseDate { get; set; }

        public string StockStatus { get; set; }

        public string Resolved { get; set; }
    }
}
