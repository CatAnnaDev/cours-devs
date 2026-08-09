using Godot;

namespace Demos.Bases;

public partial class NodeLifecycle : Node2D
{
    private int _frames;

    public NodeLifecycle()
    {
        GD.Print("1. constructeur : l'objet C# existe, mais il n'est PAS dans l'arbre. GetNode plante ici.");
    }

    public override void _EnterTree()
    {
        GD.Print("2. _EnterTree : je suis dans l'arbre. Mes enfants ne sont pas encore prets.");
    }

    public override void _Ready()
    {
        GD.Print("3. _Ready : mes enfants sont prets. C'est ICI qu'on fait GetNode.");
    }

    public override void _Process(double delta)
    {
        _frames++;
        if (_frames == 1)
            GD.Print("4. _Process : chaque frame affichee. Vitesse variable. Pour l'affichage, l'input, les timers.");

        if (_frames == 120)
            QueueFree();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_frames == 1)
            GD.Print("4bis. _PhysicsProcess : 60x/s fixe. Pour le mouvement, les collisions, la physique.");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true } key)
            GD.Print($"_Input : touche {key.Keycode}");
    }

    public override void _ExitTree()
    {
        GD.Print("5. _ExitTree : je quitte l'arbre. C'est ICI qu'on se desabonne et qu'on nettoie.");
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
            GD.Print("6. Predelete : l'objet natif va etre detruit. Dernier appel.");
    }
}
