using Csharplings.Unity;

namespace Csharplings;

public sealed class Walker : MonoBehaviour
{
    private Vector2 _wanted;

    public Walker(Rigidbody body)
    {
        Body = body;
    }

    public Rigidbody Body { get; }

    public int InputReads { get; private set; }

    public override void Update()
    {
        InputReads++;
        _wanted = new Vector2(1f, 0f);
    }

    public override void FixedUpdate()
    {
        Body.MovePosition(Body.Position + _wanted * 10f * Time.FixedDeltaTime);
    }
}

public static class Rigidbody1
{
    public const bool NotDone = false;

    public static void Run()
    {
        Time.Reset();
        Rigidbody.Clear();

        var scene = new Scene();
        Rigidbody body = Rigidbody.Create();
        Walker walker = scene.Add(new Walker(body));

        scene.Frames(60, 1.0 / 60.0);

        Check.Equal(walker.InputReads, 60, "l'entree se lit dans Update, une fois par IMAGE : c'est la que le joueur appuie");

        Check.Equal(body.Steps, 50,
            "et le corps avance dans FixedUpdate, cinquante fois par seconde a 0.02 de pas fixe. Les deux boucles n'ont pas la meme cadence, et confondre les deux est le bug de physique numero un");

        Check.Equal(Rigidbody.Teleports, 0,
            "et AUCUNE teleportation. Ecrire la position dans Update deplacerait le corps sans que le moteur voie le trajet : plus de collision, plus de balayage, et le personnage traverse les murs a grande vitesse");

        Check.True(body.Position.X > 9f && body.Position.X < 11f,
            "en une seconde a dix unites par seconde, on a parcouru dix unites - independamment du nombre d'images affichees");

        Rigidbody.Clear();

        var teleporting = Rigidbody.Create();

        teleporting.Teleport(new Vector2(100f, 0f));

        Check.Equal(Rigidbody.Teleports, 1, "ecrire la position DIRECTEMENT teleporte le corps");
        Check.Near(teleporting.Position, new Vector2(100f, 0f), "il arrive bien la");
        Check.Near(teleporting.PreviousPosition, new Vector2(100f, 0f),
            "mais son etat precedent est ecrase du meme coup : le moteur ne voit AUCUN deplacement entre les deux, donc aucune collision n'est testee sur le trajet, et le corps traverse les murs");

        Rigidbody.Clear();

        var moved = Rigidbody.Create();

        moved.MovePosition(new Vector2(100f, 0f));

        Check.Near(moved.Position, Vector2.Zero,
            "MovePosition ne bouge rien tout de suite : il DEMANDE un deplacement, applique au prochain pas de physique");

        Rigidbody.StepAll(0.02f);

        Check.Near(moved.Position, new Vector2(100f, 0f), "le pas suivant l'applique");
        Check.Near(moved.PreviousPosition, Vector2.Zero,
            "en gardant l'etat d'avant : c'est ce qui permet au moteur de tester le TRAJET et de s'arreter contre un mur, exactement comme le balayage de 21_physics");

        Check.Equal(Rigidbody.Teleports, 0, "et sans compter comme une teleportation");

        Rigidbody.Clear();

        var smooth = Rigidbody.Create();

        smooth.Interpolate = true;
        smooth.MovePosition(new Vector2(10f, 0f));
        Rigidbody.StepAll(0.02f);

        Check.Near(smooth.Rendered(0f), Vector2.Zero, "avec l'interpolation, l'affichage part de l'etat precedent");
        Check.Near(smooth.Rendered(1f), new Vector2(10f, 0f), "et arrive a l'etat courant");
        Check.Near(smooth.Rendered(0.5f), new Vector2(5f, 0f),
            "en passant par le milieu selon l'alpha de l'accumulateur : c'est interp1 de 20_time, fait par le moteur quand on lui demande");

        Rigidbody.Clear();

        var stiff = Rigidbody.Create();

        stiff.Interpolate = false;
        stiff.MovePosition(new Vector2(10f, 0f));
        Rigidbody.StepAll(0.02f);

        Check.Near(stiff.Rendered(0.5f), new Vector2(10f, 0f),
            "sans interpolation, l'affichage saute d'un pas fixe a l'autre. A 50 pas de physique et 144 images par seconde, le joueur voit un ESCALIER, et croit que le jeu rame alors qu'il tourne parfaitement");

        Check.Equal(stiff.Steps, 1, "un seul pas effectue");
    }
}
