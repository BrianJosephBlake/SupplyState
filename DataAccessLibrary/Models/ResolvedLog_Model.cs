using System;
namespace DataAccessLibrary.Models
{
    public class ResolvedLog_Model
    {

        public int Id { get; set; }

        public string Item { get; set; }

        public string Date_Created { get; set; }

        public string STATE { get; set; }

        public string SITE { get; set; }

        public int UserId { get; set; }



        public ResolvedLog_Model()
        {
        }
    }
}
