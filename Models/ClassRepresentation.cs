using System;

namespace ClassJsonEditor.Models
{
    public class ClassRepresentation
    {
        public ClassRepresentation(Type type, bool isReflectionOnly=false)
        {
            IsReflectionOnly = isReflectionOnly;
            Type = type;
        }
        public Type Type { get; set; }
        public bool IsReflectionOnly { get; set; }
        public override string ToString()
        {
            return Type.Name;
        }
    }
}