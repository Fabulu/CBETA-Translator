namespace CbetaTranslator.App.Models;

public sealed class CoocRow
{
    public string Key { get; set; } = "";
    public int Freq { get; set; }                 // total occurrences in KWIC windows
    public int Range { get; set; }                // distinct files containing it
    public double Assoc { get; set; }             // metric score (changes with selection)
    public double Dominance { get; set; }         // share of occurrences from top file [0..1]
    public string Bar { get; set; } = "";

    public override string ToString()
        => $"{Key}  f={Freq:n0}  r={Range:n0}  dom={Dominance:0.##%}  score={Assoc:0.###}  {Bar}";
}
