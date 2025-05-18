using System;

namespace NexusLink.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableAttribute : Attribute
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public string Database { get; set; }

        public TableAttribute() { }

        public TableAttribute(string name)
        {
            Name = name;
        }

        public TableAttribute(string schema, string name)
        {
            Schema = schema;
            Name = name;
        }

        public TableAttribute(string database, string schema, string name)
        {
            Database = database;
            Schema = schema;
            Name = name;
        }
    }
}