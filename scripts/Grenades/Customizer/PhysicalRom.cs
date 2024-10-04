using Godot;

public partial class PhysicalRom : Node3D
{
    public enum CompileAs
    {
        Assembly,
        NadeBasic,
        NadeSy,
    }

    [Export] public CompileAs compileAs = CompileAs.Assembly;
    [Export(PropertyHint.MultilineText)] public string sourceCode = "";

    public (ROM prgm, NadeBasic initRam) Compile()
    {
        return compileAs switch
        {
            CompileAs.Assembly => (ROM.Parse(sourceCode), null),
            CompileAs.NadeBasic => (NadeBasic.InterpreterROM, NadeBasic.Parse(sourceCode)),
            CompileAs.NadeSy => (NadeSy.Compile(sourceCode), null),
            _ => (null, null),
        };

    }
}
