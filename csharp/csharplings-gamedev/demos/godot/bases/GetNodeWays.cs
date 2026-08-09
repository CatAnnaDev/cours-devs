using Godot;

namespace Demos.Bases;

public partial class GetNodeWays : Node
{
    [Export] private NodePath _healthBarPath;
    [Export] private Node _directReference;
    [Export] private PackedScene _bulletScene;

    private Node _cachedChild;

    public override void _Ready()
    {
        _cachedChild = GetNodeOrNull("Sprite2D");
        GD.Print($"chemin relatif  -> {Describe(_cachedChild)}");

        GD.Print($"chemin absolu   -> {Describe(GetNodeOrNull("/root/LeakSentry"))}");
        GD.Print($"nom unique (%)  -> {Describe(GetNodeOrNull("%HealthBar"))}");
        GD.Print($"parent          -> {Describe(GetParentOrNull<Node>())}");

        if (_healthBarPath != null && !_healthBarPath.IsEmpty)
            GD.Print($"export NodePath -> {Describe(GetNodeOrNull(_healthBarPath))}");

        GD.Print($"export direct   -> {Describe(_directReference)}");

        SpawnBullet();
    }

    private void SpawnBullet()
    {
        if (_bulletScene == null)
        {
            GD.Print("pas de PackedScene assignee dans l'inspecteur");
            return;
        }

        Node bullet = _bulletScene.Instantiate();
        AddChild(bullet);
        GD.Print($"instancie -> {bullet.Name}");
    }

    private static string Describe(Node node) =>
        node == null ? "introuvable (null)" : $"{node.Name} ({node.GetType().Name})";
}
