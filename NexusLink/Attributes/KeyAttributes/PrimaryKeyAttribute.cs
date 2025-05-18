using System;

namespace NexusLink.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PrimaryKeyAttribute : Attribute
    {
        public string Name { get; set; }
        public bool isUniqueKey { get; set; } = true;
        public bool isForeingKey { get; set; } = false;
        public bool isPrimaryKey { get; set; } = true;
        public bool isRequired { get; set; } = true;
        public bool isAddedAutomatically { get; set; } = true;
        public bool isCriterial { get; set; } = false;

        public PrimaryKeyAttribute() { }

        public PrimaryKeyAttribute(string name)
        {
            Name = name;
        }
    }
}