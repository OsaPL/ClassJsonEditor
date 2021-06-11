using System;

namespace ClassJsonEditor.Models
{
    public class ClassRepresentation
    {
        public Type Type { get; set; }
        
        public override string ToString()
        {
            return Type.ToString();
        }
    }
}