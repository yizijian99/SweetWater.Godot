namespace SweetWater.Godot.SourceGenerators.SourceGenerator;

internal interface ITypeDeclaredModel
{
    public string Namespace { get; }
    public string ClassName { get; }
}