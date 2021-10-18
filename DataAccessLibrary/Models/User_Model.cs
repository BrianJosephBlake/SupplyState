using System;
namespace DataAccessLibrary.Models
{
    public class User_Model
    {
        public User_Model()
        {
        }

        public int Id { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string AccessPermissions { get; set; }

        public int IsMonitored { get; set; }

    }
}
