using System.Collections.Generic;
using Godot;

namespace Demos.Bases;

public sealed class Weapon
{
    public string Name { get; init; } = "poing";
    public Weapon Upgrade { get; set; }
}

public sealed class PlayerProfile
{
    public string DisplayName { get; set; }
    public Weapon Equipped { get; set; }
    public List<string> Tags { get; set; }
}

public partial class NullSafety : Node
{
    public override void _Ready()
    {
        var empty = new PlayerProfile();
        var full = new PlayerProfile
        {
            DisplayName = "Anna",
            Equipped = new Weapon { Name = "epee", Upgrade = new Weapon { Name = "epee+1" } },
        };

        GD.Print($"?.  chaine sans crash   -> {empty.Equipped?.Upgrade?.Name ?? "rien d'equipe"}");
        GD.Print($"?.  sur profil rempli   -> {full.Equipped?.Upgrade?.Name}");
        GD.Print($"??  valeur de repli     -> {empty.DisplayName ?? "Anonyme"}");

        empty.Tags ??= new List<string>();
        empty.Tags.Add("nouveau");
        GD.Print($"??= initialise une fois -> {empty.Tags.Count} tag(s)");

        GD.Print($"is null  (lisible)      -> {empty.Equipped is null}");
        GD.Print($"is not null             -> {full.Equipped is not null}");

        if (full.Equipped is { Upgrade.Name: "epee+1" })
            GD.Print("pattern matching imbrique -> l'arme amelioree est la bonne");

        Node missing = GetNodeOrNull("PasLa");
        GD.Print($"GetNodeOrNull -> {(missing is null ? "null, pas de crash" : "trouve")}");

        GD.Print($"?[] sur collection nulle -> {empty.Tags?[0] ?? "vide"}");
    }
}
