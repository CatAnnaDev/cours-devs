using System;
using System.Runtime.CompilerServices;
using Godot;

namespace Demos.WeakRefs;

public sealed class TargetNote
{
    public int TimesSeen { get; set; }
}

public partial class LifetimeProbe : Node
{
    private static readonly ConditionalWeakTable<Node, TargetNote> Notes = new();

    private ulong _watchedId;
    private WeakReference<Node> _weakWrapper;
    private WeakRef _godotWeakRef;

    public override void _Ready()
    {
        var watched = new Node { Name = "Surveille" };

        AddChild(watched);

        ShowInstanceId(watched);
        ShowGodotWeakRef(watched);
        ShowManagedWeakReference(watched);
        ShowConditionalWeakTable(watched);
        ShowRefCountedLifetime();
        ShowDiagnostics();
    }

    private void ShowInstanceId(Node watched)
    {
        _watchedId = watched.GetInstanceId();

        GD.Print($"identifiant natif : {_watchedId}");
        GD.Print($"encore valide ? {GodotObject.IsInstanceIdValid(_watchedId)}");

        GodotObject resolved = GodotObject.InstanceFromId(_watchedId);

        GD.Print($"resolu : {resolved is Node}");
    }

    private void ShowGodotWeakRef(Node watched)
    {
        _godotWeakRef = GodotObject.WeakRef(watched);

        Variant target = _godotWeakRef.GetRef();

        GD.Print($"WeakRef de Godot rend un Variant vide ? {target.VariantType == Variant.Type.Nil}");
        GD.Print($"et sinon le noeud : {target.As<Node>() is Node}");
    }

    private void ShowManagedWeakReference(Node watched)
    {
        _weakWrapper = new WeakReference<Node>(watched);

        if (_weakWrapper.TryGetTarget(out Node wrapper))
            GD.Print($"le wrapper manage est joignable, natif valide ? {GodotObject.IsInstanceValid(wrapper)}");
    }

    private void ShowConditionalWeakTable(Node watched)
    {
        Notes.GetOrCreateValue(watched).TimesSeen = 3;

        GD.Print($"metadonnee attachee sans rien retenir : {Notes.TryGetValue(watched, out TargetNote note)} {note?.TimesSeen}");
    }

    private void ShowRefCountedLifetime()
    {
        var shared = new Resource { ResourceName = "fiche" };
        ulong id = shared.GetInstanceId();

        GD.Print($"une Resource est comptee par references : {shared.GetReferenceCount()}");
        GD.Print($"valide tant qu'on la tient : {GodotObject.IsInstanceIdValid(id)}");
    }

    private void ShowDiagnostics()
    {
        Node.PrintOrphanNodes();

        GD.Print($"noeuds vivants : {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)}");
        GD.Print($"noeuds orphelins : {Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)}");
        GD.Print($"objets Godot en tout : {Performance.GetMonitor(Performance.Monitor.ObjectCount)}");
    }

    public Node ResolveByIdOrForget()
    {
        if (!GodotObject.IsInstanceIdValid(_watchedId))
        {
            _watchedId = 0UL;

            return null;
        }

        return GodotObject.InstanceFromId(_watchedId) as Node;
    }
}
