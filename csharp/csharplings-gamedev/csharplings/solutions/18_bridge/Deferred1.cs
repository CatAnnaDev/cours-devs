namespace Csharplings;

public sealed class Turret : Node
{
    public int PhysicsTicks { get; private set; }

    public int ChildrenSeenDuringPhysics { get; private set; }

    public int ChildrenRightAfterRequest { get; private set; }

    public override void _PhysicsProcess(double delta)
    {
        PhysicsTicks++;
        ChildrenSeenDuringPhysics = Children.Count;

        if (PhysicsTicks > 3)
            return;

        CallDeferred(() => AddChild(new Node { Name = $"Bullet{PhysicsTicks}" }));

        ChildrenRightAfterRequest = Children.Count;
    }
}

public sealed class Chainer : Node
{
    public int Rounds { get; private set; }

    public override void _Process(double delta)
    {
        if (Rounds > 0)
            return;

        CallDeferred(FirstStep);
    }

    private void FirstStep()
    {
        Rounds++;
        CallDeferred(SecondStep);
    }

    private void SecondStep() => Rounds++;
}

public static class Deferred1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var tree = new SceneTree();
        var turret = new Turret { Name = "Tourelle" };

        tree.Root.AddChild(turret);
        tree.Start();

        Check.Equal(Node.DeferredPending, 0, "rien en attente avant le premier tour");

        tree.Tick();

        Check.Equal(turret.PhysicsTicks, 1, "un tour de physique a eu lieu");
        Check.Equal(turret.ChildrenSeenDuringPhysics, 0,
            "et pendant ce tour la tourelle n'avait aucun enfant : la balle n'existe pas encore, meme si on a demande sa creation");
        Check.Equal(turret.ChildrenRightAfterRequest, 0,
            "et juste APRES avoir demande sa creation, elle n'existe toujours pas : c'est la difference entre demander et faire");
        Check.Equal(turret.Children.Count, 1,
            "elle est arrivee a la fin de la frame, quand la file differee a ete videe. C'est tout l'interet : on ne modifie jamais l'arbre au milieu d'un callback physique");
        Check.Equal(Node.DeferredPending, 0, "et la file est repartie vide");

        tree.Tick();

        Check.Equal(turret.ChildrenSeenDuringPhysics, 1,
            "au tour suivant, la tourelle voit enfin sa premiere balle");
        Check.Equal(turret.Children.Count, 2, "et une deuxieme est nee a la fin de ce tour");

        tree.Run(10);

        Check.Equal(turret.Children.Count, 3,
            "la tourelle arrete d'en demander apres trois tours, et il n'en apparait pas une de plus");
        Check.Equal(turret.PhysicsTicks, 12, "alors que la physique, elle, a bien continue de tourner");

        var chained = new SceneTree();
        var chainer = new Chainer { Name = "Chaine" };

        chained.Root.AddChild(chainer);
        chained.Start();

        chained.Tick();

        Check.Equal(chainer.Rounds, 2,
            "un appel differe qui en demande un autre part dans la MEME vidange : la file se vide jusqu'a etre epuisee, pas jusqu'a la fin d'une photo");
        Check.Equal(Node.DeferredPending, 0, "et il ne reste rien en attente a la fin de la frame");

        chained.Tick();

        Check.Equal(chainer.Rounds, 2,
            "la frame suivante n'a donc plus rien a faire. C'est pratique, mais ca a un prix : un appel differe qui se redemande LUI-MEME gele la frame pour toujours, le moteur ne t'en protege pas");

        Check.Equal(chainer.Children.Count, 0,
            "et la chaine n'a rien fabrique : un appel differe n'est pas forcement un ajout de noeud, c'est juste 'plus tard, quand ce sera sur'");
    }
}
