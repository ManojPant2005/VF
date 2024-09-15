using System.Collections.Generic;

namespace UI.Data
{
        public class DbConfig
        {
            public string ConnectionString { get; set; }
            public string TableName { get; set; }
            public Dictionary<string, string> ColumnMappings { get; set; }
        }
}
