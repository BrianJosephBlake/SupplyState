using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLibrary.Models
{
    public class Column_Model
    {
        public string ColumnName { get; set; }
        public string DataTypeString { get; set; }
        public string IsPrimaryKey { get; set; }
        public string CanBeNull { get; set; }

    }

    public class Table_Model
    {

        public string TableName { get; set; }

        public List<Column_Model> Columns { get; set; }

        public Table_Model()
        {
            Columns = new List<Column_Model>();
        }


    }
}
