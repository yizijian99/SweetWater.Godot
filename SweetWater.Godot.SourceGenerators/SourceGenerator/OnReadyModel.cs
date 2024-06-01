using System.Collections.Generic;

namespace SweetWater.Godot.SourceGenerators.SourceGenerator
{
    internal class OnReadyModel : ITypeDeclaredModel
    {
        public string Namespace { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Modifiers { get; set; } = string.Empty;
        public List<Field> Fields { get; } = new();
        public List<Property> Properties { get; } = new();
        public List<OnEventMethod> OnEventMethods { get; } = new();

        internal interface IFieldOrProperty
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public string NodePath { get; set; }
        }

        internal class Field : IFieldOrProperty
        {
            public string Type { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string NodePath { get; set; } = string.Empty;
        }

        internal class Property : IFieldOrProperty
        {
            public string Type { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string NodePath { get; set; } = string.Empty;
        }

        internal class OnEventMethod
        {
            public string Name { get; set; } = string.Empty;
            public List<OnEvent> OnEventList { get; set; } = new();
        }

        internal class OnEvent
        {
            public string EventName { get; set; } = string.Empty;
            public string NodePath { get; set; } = string.Empty;
            public string NodeType { get; set; } = string.Empty;
        }
    }
}