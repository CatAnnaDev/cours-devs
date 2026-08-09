using Godot;
using Godot.Collections;

namespace Demos.Bridge;

public partial class Marshalling : Node2D
{
    private static readonly StringName GroupEnemies = "enemies";
    private static readonly StringName ActionJump = "jump";
    private static readonly NodePath LabelPath = "UI/Score";

    [Signal]
    public delegate void ScoreChangedEventHandler(int value);

    public event System.Action<int> ScoreChangedInCSharp;

    private Label _label;
    private float _speed = 200f;

    public override void _Ready()
    {
        _label = GetNodeOrNull<Label>(LabelPath);

        ShowVariantRoundTrip();
        ShowCollectionCopy();
        ShowDeferredCall();
    }

    public override void _PhysicsProcess(double delta)
    {
        MoveTheSlowWay((float)delta);
        MoveTheBatchedWay((float)delta);
    }

    private void MoveTheSlowWay(float delta)
    {
        Position = new Vector2(Position.X + _speed * delta, Position.Y);

        if (Position.X > 500f)
            Position = new Vector2(0f, Position.Y);
    }

    private void MoveTheBatchedWay(float delta)
    {
        Vector2 local = Position;

        local.X += _speed * delta;

        if (local.X > 500f)
            local.X = 0f;

        Position = local;
    }

    private void EmitTheSlowWay(int score)
    {
        EmitSignal("score_changed", score);
    }

    private void EmitTheFastWay(int score)
    {
        EmitSignal(SignalName.ScoreChanged, score);
    }

    private void EmitWithoutCrossingAtAll(int score)
    {
        ScoreChangedInCSharp?.Invoke(score);
    }

    private void ShowVariantRoundTrip()
    {
        Variant asVariant = Variant.From(42);

        GD.Print($"un int passe en Variant puis revient : {asVariant.AsInt32()}");

        Variant asVector = Variant.From(new Vector2(1f, 2f));

        GD.Print($"un Vector2 aussi : {asVector.AsVector2()}");
        GD.Print("tout ce qui traverse vers le moteur passe par la : signaux, Export, Set, Call");
    }

    private void ShowCollectionCopy()
    {
        var managed = new System.Collections.Generic.List<int> { 1, 2, 3 };
        var engineSide = new Array<int>();

        foreach (int value in managed)
            engineSide.Add(value);

        managed[0] = 99;

        GD.Print($"la liste C# vaut maintenant {managed[0]}, le tableau moteur vaut toujours {engineSide[0]}");
        GD.Print("ce sont deux memoires differentes : chaque passage recopie");

        var settings = new Dictionary
        {
            { "volume", 0.8f },
            { "difficulty", 2 },
        };

        GD.Print($"un dictionnaire moteur se lit en Variant : {settings["volume"].AsSingle()}");
    }

    private void ShowDeferredCall()
    {
        CallDeferred(MethodName.SpawnLater);
        Callable.From(() => GD.Print("et une lambda differee marche aussi")).CallDeferred();

        GD.Print("les deux appels ci-dessus partiront a la fin de la frame, pas maintenant");
    }

    private void SpawnLater()
    {
        var bullet = new Node2D { Name = "Bullet" };

        AddChild(bullet);
        bullet.AddToGroup(GroupEnemies);

        GD.Print($"noeud ajoute apres coup, dans le groupe {GroupEnemies}");
    }

    private void ReadInput()
    {
        if (Input.IsActionJustPressed(ActionJump))
            GD.Print("le nom d'action est garde lui aussi : pas de conversion par frame");
    }

    private void UpdateLabelOnlyWhenNeeded(int score)
    {
        if (_label is null)
            return;

        string wanted = $"Score {score}";

        if (_label.Text != wanted)
            _label.Text = wanted;
    }
}
