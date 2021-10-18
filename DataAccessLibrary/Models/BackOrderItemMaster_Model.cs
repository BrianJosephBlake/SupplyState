using System;
namespace DataAccessLibrary.Models
{
    public class BackOrderItemMaster_Model
    {
        public BackOrderItemMaster_Model()
        {
        }

        public int Id { get; set; }

        public string Item { get; set; }

        public string MfrNum { get; set; }

        public string Description { get; set; }

        public string StockOutDate { get; set; }

        public string ReleaseDate { get; set; }

        public string GapDays { get; set; }

        public string ReasonCode { get; set; }

        public string StockStatus { get; set; }

        public string Source { get; set; }

    }
}
