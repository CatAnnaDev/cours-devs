using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

namespace Demos.WeakRefs;

public sealed class SelfTestNote
{
    public int Value { get; set; }
}

public partial class WeakRefsSelfTest : Node
{
    [Export] public bool QuitWhenDone { get; set; } = true;

    private int _checks;
    private int _failures;

    public override async void _Ready()
    {
        GD.Print("=== WEAKREFS.md, verifie dans le moteur ===");
        GD.Print($"Godot {Engine.GetVersionInfo()["string"]}");
        GD.Print(string.Empty);

        await NodeIsNeverFreedByTheCollector();
        await ManagedWeakReferenceOutlivesTheNative();
        await GodotWeakRefFollowsTheNative();
        await InstanceIdsAreNotRecycled();
        ResourcesAreReferenceCounted();
        await TableFollowsTheWrapper();
        OrphanDiagnostics();
        await TheTreeOwnsItsChildren();
        WhatDisposeReallyDoes();
        FreeVersusQueueFree();

        GD.Print(string.Empty);
        GD.Print($"=== {_checks - _failures} / {_checks} affirmations verifiees ===");

        if (_failures > 0)
            GD.PushError($"{_failures} affirmation(s) de WEAKREFS.md ne tiennent plus");

        if (QuitWhenDone)
            GetTree().Quit(_failures);
    }

    private void Check(bool condition, string claim)
    {
        _checks++;

        if (!condition)
            _failures++;

        GD.Print($"  [{(condition ? "PASS" : "FAIL")}] {claim}");
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong MakeOrphanAndDropIt()
    {
        var orphan = new Node { Name = "Orphelin" };

        return orphan.GetInstanceId();
    }

    private async Task NodeIsNeverFreedByTheCollector()
    {
        GD.Print("1. Un Node dont on lache la reference C#");

        ulong id = MakeOrphanAndDropIt();

        Collect();
        await Frames(3);
        Collect();
        await Frames(3);

        Check(GodotObject.IsInstanceIdValid(id),
            "le natif est TOUJOURS vivant apres collecte : le ramasse-miettes ne libere pas un Node");

        if (GodotObject.InstanceFromId(id) is Node leaked)
            leaked.Free();

        GD.Print(string.Empty);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private (WeakReference<Node> Weak, Node Strong, ulong Id) MakeTrackedChild()
    {
        var child = new Node { Name = "Suivi" };

        AddChild(child);

        return (new WeakReference<Node>(child), child, child.GetInstanceId());
    }

    private async Task ManagedWeakReferenceOutlivesTheNative()
    {
        GD.Print("2. WeakReference<Node>, une reference forte gardee a cote");

        (WeakReference<Node> weak, Node strong, ulong id) = MakeTrackedChild();

        Check(weak.TryGetTarget(out Node before) && GodotObject.IsInstanceValid(before),
            "avant liberation : joignable ET valide");

        strong.QueueFree();

        await Frames(3);
        Collect();

        bool reachable = weak.TryGetTarget(out Node after);

        Check(reachable, "apres QueueFree : TryGetTarget rend ENCORE le wrapper");
        Check(reachable && !GodotObject.IsInstanceValid(after), "mais IsInstanceValid dit false");
        Check(!GodotObject.IsInstanceIdValid(id), "et l'identifiant n'est plus valide");

        GD.Print("        -> c'est pour ca que le double test est obligatoire");
        GD.Print(string.Empty);
    }

    private async Task GodotWeakRefFollowsTheNative()
    {
        GD.Print("3. Le WeakRef du moteur, meme scenario");

        var child = new Node { Name = "SuiviGodot" };

        AddChild(child);

        WeakRef weak = GodotObject.WeakRef(child);

        Check(weak.GetRef().VariantType != Variant.Type.Nil, "avant liberation : GetRef rend le noeud");

        child.QueueFree();

        await Frames(3);

        Check(weak.GetRef().VariantType == Variant.Type.Nil,
            "apres liberation : GetRef rend Nil. Lui suit le natif, pas le wrapper");

        GD.Print(string.Empty);
    }

    private async Task InstanceIdsAreNotRecycled()
    {
        GD.Print("4. Les identifiants d'instance");

        var first = new Node { Name = "Premier" };

        AddChild(first);

        ulong firstId = first.GetInstanceId();

        first.QueueFree();

        await Frames(3);

        Check(!GodotObject.IsInstanceIdValid(firstId), "un identifiant libere devient invalide");

        var second = new Node { Name = "Second" };

        AddChild(second);

        Check(second.GetInstanceId() != firstId, "un nouvel objet recoit un identifiant DIFFERENT");
        Check(!GodotObject.IsInstanceIdValid(firstId), "et l'ancien reste invalide, il ne designera jamais le nouveau");

        second.QueueFree();

        await Frames(2);

        GD.Print(string.Empty);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong MakeResourceAndDropIt()
    {
        var resource = new Resource { ResourceName = "jetable" };

        return resource.GetInstanceId();
    }

    private void ResourcesAreReferenceCounted()
    {
        GD.Print("5. Une Resource est comptee par references");

        var kept = new Resource { ResourceName = "gardee" };
        ulong keptId = kept.GetInstanceId();

        Check(kept.GetReferenceCount() >= 1, "une Resource tenue a un compteur superieur a zero");

        ulong droppedId = MakeResourceAndDropIt();

        Collect();

        Check(!GodotObject.IsInstanceIdValid(droppedId),
            "reference lachee puis collecte : le natif est LIBERE. Le ramasse-miettes decide, indirectement");
        Check(GodotObject.IsInstanceIdValid(keptId), "alors que celle qu'on tient est intacte");

        GD.Print(string.Empty);
    }

    private async Task TableFollowsTheWrapper()
    {
        GD.Print("6. ConditionalWeakTable sur un noeud");

        var table = new ConditionalWeakTable<Node, SelfTestNote>();
        var node = new Node { Name = "AvecNote" };

        AddChild(node);
        table.GetOrCreateValue(node).Value = 7;

        Check(table.TryGetValue(node, out SelfTestNote before) && before.Value == 7, "l'entree est la");

        node.QueueFree();

        await Frames(3);
        Collect();

        Check(table.TryGetValue(node, out SelfTestNote after) && after.Value == 7,
            "apres liberation du natif, l'entree SURVIT : la table suit le wrapper");

        GD.Print("        -> d'ou IsInstanceValid avant de s'en servir");
        GD.Print(string.Empty);
    }

    private void OrphanDiagnostics()
    {
        GD.Print("7. Diagnostic des orphelins");

        double before = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);

        var orphan = new Node { Name = "JamaisAjoute" };

        double after = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);

        Check(after > before, $"un noeud jamais ajoute a l'arbre compte comme orphelin ({before} vers {after})");

        Node.PrintOrphanNodes();

        orphan.Free();

        Check(Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount) <= before,
            "apres Free, le compteur redescend");

        GD.Print($"        noeuds vivants : {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)}");
        GD.Print(string.Empty);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ulong AddChildAndDropTheReference()
    {
        var child = new Node { Name = "PossedeParLArbre" };

        AddChild(child);

        return child.GetInstanceId();
    }

    private async Task TheTreeOwnsItsChildren()
    {
        GD.Print("8. Un noeud DANS l'arbre dont on lache toute reference C#");

        ulong id = AddChildAndDropTheReference();

        Collect();
        await Frames(3);
        Collect();

        Check(GodotObject.IsInstanceIdValid(id),
            "il vit toujours : c'est l'ARBRE qui le possede, pas ta variable");

        Node again = GetNodeOrNull<Node>("PossedeParLArbre");

        Check(again is not null && GodotObject.IsInstanceValid(again),
            "et GetNode redonne un wrapper parfaitement utilisable");

        GD.Print("        -> le wrapper C# est une VUE, pas le proprietaire");

        again?.QueueFree();

        await Frames(2);

        GD.Print(string.Empty);
    }

    private void WhatDisposeReallyDoes()
    {
        GD.Print("9. Ce que fait vraiment Dispose()");

        var loose = new Node { Name = "HorsArbre" };
        ulong looseId = loose.GetInstanceId();

        loose.Dispose();

        Check(GodotObject.IsInstanceIdValid(looseId),
            "sur un Node, Dispose ne libere PAS le natif : il jette seulement le handle C#");
        Check(!GodotObject.IsInstanceValid(loose),
            "et IsInstanceValid rend false sur ce handle jete, alors que l'objet vit encore");

        bool threw = false;

        try
        {
            _ = loose.Name;
        }
        catch (ObjectDisposedException)
        {
            threw = true;
        }

        Check(threw, "y toucher leve ObjectDisposedException");

        if (GodotObject.InstanceFromId(looseId) is Node recovered)
        {
            Check(recovered.Name == "HorsArbre", "le natif reste recuperable par son identifiant");

            recovered.Free();

            Check(!GodotObject.IsInstanceIdValid(looseId), "et seul Free le libere pour de bon");
        }

        var resource = new Resource { ResourceName = "jetable" };
        ulong resourceId = resource.GetInstanceId();

        resource.Dispose();

        Check(!GodotObject.IsInstanceIdValid(resourceId),
            "sur une Resource en revanche, Dispose fait tomber le comptage a zero et libere");

        GD.Print("        -> 'using var node = new Node()' ne libere donc rien : il fabrique une fuite");
        GD.Print(string.Empty);
    }

    private void FreeVersusQueueFree()
    {
        GD.Print("10. Free() contre QueueFree()");

        var immediate = new Node { Name = "Immediat" };

        AddChild(immediate);

        ulong immediateId = immediate.GetInstanceId();

        immediate.Free();

        Check(!GodotObject.IsInstanceIdValid(immediateId), "Free libere TOUT DE SUITE");

        var deferred = new Node { Name = "Differe" };

        AddChild(deferred);

        ulong deferredId = deferred.GetInstanceId();

        deferred.QueueFree();

        Check(GodotObject.IsInstanceIdValid(deferredId), "QueueFree ne libere pas tout de suite");
        Check(deferred.IsQueuedForDeletion(), "mais l'objet sait deja qu'il est condamne");

        GD.Print("        -> d'ou l'attente d'une frame avant de conclure quoi que ce soit");
        GD.Print(string.Empty);
    }
}
