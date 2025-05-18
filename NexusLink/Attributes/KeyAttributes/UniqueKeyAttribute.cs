using System;

namespace NexusLink.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class UniqueKeyAttribute : Attribute
    {
        public string Name { get; set; }
        public bool isUniqueKey { get; set; } = true;
        public bool isForeingKey { get; set; } = false;
        public bool isPrimaryKey { get; set; } = false;
        public bool isRequired { get; set; }
        public bool isAddedAutomatically { get; set; }
        public bool isCriterial { get; set; }

        public UniqueKeyAttribute() { }

        public UniqueKeyAttribute(string name)
        {
            Name = name;
        }

        public UniqueKeyAttribute(string name, bool isCriterial = false, bool isRequired = false)
        {
            Name = name;
            this.isCriterial = isCriterial;
            this.isRequired = isRequired;
        }
    }
}