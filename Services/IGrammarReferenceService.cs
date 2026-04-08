using System.Collections.Generic;

namespace ReadZen.App.Services;

public sealed class GrammarParticleInfo
{
    public char Character { get; set; }
    public List<GrammarFunction> Functions { get; set; } = new();
}

public sealed class GrammarFunction
{
    public string Role { get; set; } = "";
    public string Gloss { get; set; } = "";
    public string Example { get; set; } = "";
    public string ExampleGloss { get; set; } = "";
}

public interface IGrammarReferenceService
{
    GrammarParticleInfo? Lookup(char ch);
}
