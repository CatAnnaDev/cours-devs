using System.Collections.Generic;
using System.Text;
using Godot;

namespace Demos.Pieges;

public partial class Pieges : Node2D
{
    private Label _scoreLabel;
    private readonly StringBuilder _builder = new();

    private static readonly NodePath ScoreLabelPath = "UI/ScoreLabel";

    public override void _Ready()
    {
        _scoreLabel = GetNodeOrNull<Label>(ScoreLabelPath);

        FloatComparison();
        FreeVsQueueFree();
        ValidityAfterFree();
        DivisionByZero();
        IntegerDivision();
    }

    public override void _Process(double delta)
    {
        SlowVersion();
        FastVersion();
    }

    private void SlowVersion()
    {
        Label label = GetNodeOrNull<Label>("UI/ScoreLabel");
        var enemies = new List<Node>(GetTree().GetNodesInGroup("enemies"));
        string text = "Score : " + 42 + " / Ennemis : " + enemies.Count;

        if (label != null)
            label.Text = text;
    }

    private void FastVersion()
    {
        if (_scoreLabel == null)
            return;

        _builder.Clear();
        _builder.Append("Score : ").Append(42);
        _scoreLabel.Text = _builder.ToString();
    }

    private static void FloatComparison()
    {
        float a = 0.1f + 0.2f;
        float b = 0.3f;

        GD.Print($"a == b            -> {a == b} (faux, et c'est normal)");
        GD.Print($"IsEqualApprox     -> {Mathf.IsEqualApprox(a, b)} (la bonne facon)");
        GD.Print($"IsZeroApprox      -> {Mathf.IsZeroApprox(a - b)}");
    }

    private void FreeVsQueueFree()
    {
        var temporary = new Node { Name = "Temporaire" };
        AddChild(temporary);

        temporary.QueueFree();

        GD.Print("QueueFree : detruit en fin de frame, sur. Free() : detruit maintenant, peut casser une iteration en cours.");
    }

    private void ValidityAfterFree()
    {
        var doomed = new Node { Name = "Condamne" };
        AddChild(doomed);
        doomed.Free();

        GD.Print($"test null classique      -> {doomed is null} (ne dit RIEN sur l'objet natif)");
        GD.Print($"IsInstanceValid(doomed)  -> {IsInstanceValid(doomed)} (la seule verification fiable)");
    }

    private static void DivisionByZero()
    {
        float divisor = 0f;
        float floatResult = 10f / divisor;
        GD.Print($"float / 0 -> {floatResult} (pas d'exception, mais tout ce qui touche a Infinity devient NaN)");
        GD.Print($"garde-fou : {(Mathf.IsZeroApprox(divisor) ? 0f : 10f / divisor)}");
    }

    private static void IntegerDivision()
    {
        int current = 3;
        int max = 4;

        GD.Print($"int / int -> {current / max} (zero, le ratio est perdu)");
        GD.Print($"cast      -> {(float)current / max} (0.75, correct)");
    }
}

public partial class LeakyNode : Node
{
    private static readonly List<Node> AllSpawned = new();

    public override void _Ready()
    {
        AllSpawned.Add(this);
    }
}

public partial class CleanNode : Node
{
    private static readonly List<Node> AllSpawned = new();

    public override void _Ready()
    {
        AllSpawned.Add(this);
    }

    public override void _ExitTree()
    {
        AllSpawned.Remove(this);
    }
}
