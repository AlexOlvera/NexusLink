using System;

namespace NexusLink.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; set; }
        public bool isRequired { get; set; }
        public bool isAddedAutomatically { get; set; }
        public bool isCriterial { get; set; }

        public ColumnAttribute() { }

        public ColumnAttribute(string name)
        {
            Name = name;
        }

        public ColumnAttribute(string name, bool isCriterial = false, bool isAddedAutomatically = false, bool isRequired = false)
        {
            Name = name;
            this.isCriterial = isCriterial;
            this.isAddedAutomatically = isAddedAutomatically;
            this.isRequired = isRequired;
        }
    }
}