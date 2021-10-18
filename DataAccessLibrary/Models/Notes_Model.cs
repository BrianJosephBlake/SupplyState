using System;
namespace DataAccessLibrary.Models
{
    public class Notes_Model
    {
        public Notes_Model()
        {
        }

        public int Id { get; set; }

        public string ITEM { get; set; }

        public string DATE_CREATED { get; set; }

        public string NOTE { get; set; }

        public string Site { get; set; }

        public int UserId { get; set; }


    }
}
