using System;

namespace NexusLink.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ForeignKeyAttribute : Attribute
    {
        public string Name { get; set; }
        public string ReferenceTableName { get; set; }
        public bool isUniqueKey { get; set; }
        public bool isForeingKey { get; set; } = true;
        public bool isPrimaryKey { get; set; }
        public bool isRequired { get; set; }
        public bool isAddedAutomatically { get; set; }
        public bool isCriterial { get; set; }

        public ForeignKeyAttribute() { }

        public ForeignKeyAttribute(string name)
        {
            Name = name;
        }

        public ForeignKeyAttribute(string name, string referenceTableName)
        {
            Name = name;
            ReferenceTableName = referenceTableName;
        }

        public ForeignKeyAttribute(string name, bool isCriterial = false, bool isUniqueKey = false, bool isRequired = false)
        {
            Name = name;
            this.isCriterial = isCriterial;
            this.isUniqueKey = isUniqueKey;
            this.isRequired = isRequired;
        }
    }
}