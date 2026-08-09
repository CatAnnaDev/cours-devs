using System.Runtime.InteropServices;

namespace Csharplings;

public struct Particle
{
    public Vector2 Position;

    public Vector2 Velocity;

    public float Life;
}

public static class Inplace1
{
    public const bool NotDone = true;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static void StepByCopy(List<Particle> particles, float delta)
    {
        for (int i = 0; i < particles.Count; i++)
        {
            Particle particle = particles[i];

            particle.Position += particle.Velocity * delta;
            particle.Life -= delta;
        }
    }

    public static void StepInPlace(List<Particle> particles, float delta)
    {
        Span<Particle> span = CollectionsMarshal.AsSpan(particles);

        for (int i = 0; i < span.Length; i++)
        {
            Particle particle = span[i];

            particle.Position += particle.Velocity * delta;
            particle.Life -= delta;
        }
    }

    public static ref float LifeOf(Span<Particle> particles, int index) => ref particles[index].Life;

    public static ref Particle Weakest(Span<Particle> particles)
    {
        int best = 0;

        for (int i = 1; i < particles.Length; i++)
        {
            if (particles[i].Life < particles[best].Life)
                best = i;
        }

        return ref particles[best];
    }

    private static List<Particle> Build()
    {
        var particles = new List<Particle>(3);

        for (int i = 0; i < 3; i++)
            particles.Add(new Particle { Velocity = new Vector2(i + 1, 0f), Life = 1f });

        return particles;
    }

    public static void Run()
    {
        List<Particle> copied = Build();

        StepByCopy(copied, 0.5f);

        Check.Near(copied[1].Position, new Vector2(1f, 0f), "la version par copie marche");
        Check.Near(copied[1].Life, 0.5f, "a condition de REECRIRE l'element dans la liste");

        List<Particle> direct = Build();

        StepInPlace(direct, 0.5f);

        Check.Near(direct[1].Position, new Vector2(1f, 0f), "la version en place donne le meme resultat");
        Check.Near(direct[1].Life, 0.5f, "sans jamais recopier une particule");

        Check.Equal(Measure(() => StepInPlace(direct, 0f)), 0L,
            "CollectionsMarshal.AsSpan ouvre le tableau INTERNE de la List : plus d'indexeur, plus de copie de struct a l'aller et au retour");

        Span<Particle> span = CollectionsMarshal.AsSpan(direct);

        ref float life = ref LifeOf(span, 0);

        life = 9f;

        Check.Near(direct[0].Life, 9f,
            "un 'ref float' rendu par une methode est un ALIAS sur le champ : ecrire dedans ecrit dans la particule de la liste");

        ref Particle weakest = ref Weakest(span);

        weakest.Life = 0f;

        Check.Near(direct[1].Life, 0f, "et un 'ref' vers un struct entier permet de le modifier sur place apres l'avoir cherche");

        Check.Near(direct[0].Life, 9f, "sans toucher aux autres");

        direct.Add(new Particle());

        Check.Equal(direct.Count, 4,
            "et voila le danger : ajouter un element peut REALLOUER le tableau interne. Le Span pris avant pointe alors l'ANCIEN tableau, et tout ce qu'on y ecrit part dans le vide");

        Check.True(CollectionsMarshal.AsSpan(direct).Length == 4,
            "la regle : on reprend le Span apres toute modification de taille, et on ne le garde jamais dans un champ");

        List<Particle> untouched = Build();
        Particle detached = untouched[0];

        detached.Life = 42f;

        Check.Near(untouched[0].Life, 1f,
            "rappel de pourquoi tout ceci existe : 'liste[0]' rend une COPIE du struct. La modifier ne modifie rien, et le compilateur refuse meme d'ecrire liste[0].Life = 42 pour t'eviter d'y croire");
    }
}
