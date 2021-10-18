using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLibrary.Models
{
    class HasSubModel
    {
        public string Item { get; set; }

        public string MfrNum { get; set; }

        public string Description { get; set; }
        public string StockOutDate { get; set; }
        public string ReleaseDate { get; set; }
        public string GapDays { get; set; }
        public string ReasonCode { get; set; }
        public string StockStatus { get; set; }
        public string Note { get; set; }
        public string HasSub { get; set; }
        public int Usage { get; set; }
        public string Resolved { get; set; }

    }
}
