namespace Csharplings;

public sealed class Hitstop
{
    public float Remaining { get; private set; }

    public bool Frozen => Remaining > 0f;

    public float Scale => Frozen ? 0f : 1f;

    public void Trigger(float seconds)
    {
        if (seconds > Remaining)
            Remaining = seconds;
    }

    public float Tick(float unscaledDelta)
    {
        if (Frozen)
        {
            Remaining = Mathf.Max(0f, Remaining - unscaledDelta);

            return 0f;
        }

        return unscaledDelta;
    }
}

public static class Hitstop1
{
    public const bool NotDone = false;

    public const float Frame = 1f / 60f;

    public static void Run()
    {
        var hitstop = new Hitstop();

        Check.False(hitstop.Frozen, "au repos, rien n'est gele");
        Check.Equal(hitstop.Scale, 1f, "et le temps de jeu s'ecoule normalement");
        Check.Equal(hitstop.Tick(Frame), Frame, "une image rend son delta entier");

        hitstop.Trigger(0.08f);

        Check.True(hitstop.Frozen, "un coup qui touche fige le jeu");
        Check.Equal(hitstop.Scale, 0f, "l'echelle de temps tombe a zero : plus rien ne bouge, et c'est ce qui donne du POIDS a l'impact");

        Check.Equal(hitstop.Tick(Frame), 0f, "pendant le gel, le jeu ne recoit aucun temps");

        Check.Near(hitstop.Remaining, 0.08f - Frame,
            "et pourtant le gel, LUI, avance : il consomme le temps REEL. Le decompter en temps de jeu le figerait pour toujours, puisque le temps de jeu vaut zero");

        float realTime = Frame;

        while (hitstop.Frozen)
        {
            hitstop.Tick(Frame);
            realTime += Frame;
        }

        Check.Near(realTime, 0.0833f, "le gel dure bien 80 millisecondes de temps reel, soit cinq images", 0.001);
        Check.Equal(hitstop.Scale, 1f, "puis le jeu repart");

        hitstop.Trigger(0.08f);
        hitstop.Tick(Frame);
        hitstop.Trigger(0.05f);

        Check.Near(hitstop.Remaining, 0.08f - Frame,
            "un second coup PENDANT le gel prend le maximum, il ne s'ajoute pas : un combo de six coups figerait le jeu une demi-seconde, et le joueur croirait a un plantage");

        hitstop.Trigger(0.2f);

        Check.Near(hitstop.Remaining, 0.2f, "mais un coup PLUS FORT allonge le gel, ce qui est exactement l'effet voulu pour un coup final");

        var fresh = new Hitstop();
        float gameTime = 0f;
        float audioTime = 0f;

        fresh.Trigger(0.05f);

        for (int i = 0; i < 10; i++)
        {
            gameTime += fresh.Tick(Frame);
            audioTime += Frame;
        }

        Check.Near(gameTime, 10f * Frame - 0.05f,
            "sur dix images dont trois gelees, le jeu n'a vieilli que du reste", 0.001);

        Check.Near(audioTime, 10f * Frame,
            "alors que le son, l'interface et le menu de pause continuent sur le temps reel. Un hitstop qui coupe la musique n'est pas un hitstop, c'est un freeze", 0.001);

        var accumulator = 0f;
        var physics = new Hitstop();
        int steps = 0;

        physics.Trigger(0.05f);

        for (int i = 0; i < 10; i++)
        {
            accumulator += physics.Tick(Frame);

            while (accumulator >= 0.02f)
            {
                accumulator -= 0.02f;
                steps++;
            }
        }

        Check.Equal(steps, 5,
            "et l'accumulateur de physique se remplit du temps de JEU : cinq pas au lieu de huit. S'il se remplissait du temps reel, le jeu rattraperait tout d'un coup a la fin du gel, et le personnage traverserait le decor");
    }
}
