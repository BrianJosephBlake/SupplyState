using System;
using DataAccessLibrary;
using DataAccessLibrary.Models;

namespace RazorPagesUI.PublicLibrary
{
    public class BackorderMasterItem_Interface
    {
        public BackorderMasterItem_Interface()
        {
        }

        public static void UpdateMaster()
        {
            SQLCrud sql = new SQLCrud(ConnectionString.GetConnectionString());

            //sql.ClearBackOrderItemMaster();

            //sql.UpdateValuelinkToMaster();

        }

    }
}
