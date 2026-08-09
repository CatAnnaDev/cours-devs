namespace Csharplings;

public sealed class Fighter
{
    public Fighter(int id, Vector2 position, int health)
    {
        Id = id;
        Position = position;
        Health = health;
    }

    public int Id { get; }

    public Vector2 Position { get; set; }

    public Vector2 Knockback { get; set; }

    public int Health { get; private set; }

    public float Invulnerable { get; private set; }

    public bool Alive => Health > 0;

    public bool CanBeHit => Invulnerable <= 0f;

    public void Damage(int amount, float invulnerability)
    {
        Health = Math.Max(0, Health - amount);
        Invulnerable = invulnerability;
    }

    public void Tick(float delta)
    {
        Invulnerable = Mathf.Max(0f, Invulnerable - delta);
        Position += Knockback * delta;
        Knockback = Knockback.MoveToward(Vector2.Zero, 40f * delta);
    }
}

public sealed class Attack
{
    private readonly HashSet<int> _alreadyHit = new();

    public Attack(int id, int damage, float force)
    {
        Id = id;
        Damage = damage;
        Force = force;
    }

    public int Id { get; }

    public int Damage { get; }

    public float Force { get; }

    public int HitCount => _alreadyHit.Count;

    public bool TryHit(Fighter attacker, Fighter target, float invulnerability)
    {
        if (!target.Alive || !target.CanBeHit)
            return false;

        target.Damage(Damage, invulnerability);
        target.Knockback = Direction(attacker.Position, target.Position) * Force;

        return true;
    }

    public static Vector2 Direction(Vector2 from, Vector2 to)
    {
        return (to - from).Normalized();
    }
}

public static class Hurt1
{
    public const bool NotDone = true;

    public const float Frame = 1f / 60f;
    public const float IFrames = 0.5f;

    public static void Run()
    {
        var hero = new Fighter(1, Vector2.Zero, 100);
        var slime = new Fighter(2, new Vector2(10f, 0f), 30);
        var swing = new Attack(1, damage: 12, force: 30f);

        Check.True(swing.TryHit(hero, slime, IFrames), "le premier contact touche");
        Check.Equal(slime.Health, 18, "et retire les degats");
        Check.Near(slime.Knockback, new Vector2(30f, 0f), "le recul part de l'attaquant VERS la cible, jamais dans le sens du stick");

        Check.False(swing.TryHit(hero, slime, IFrames),
            "le second contact de la MEME attaque ne touche pas. Sans ce registre, une epee qui traverse un ennemi le frappe a chaque image de contact, et il meurt en trois images");

        Check.Equal(slime.Health, 18, "les points de vie n'ont pas bouge");
        var aura = new Attack(9, damage: 1, force: 0f);
        var walker = new Fighter(6, new Vector2(5f, 0f), 100);

        Check.True(aura.TryHit(hero, walker, invulnerability: 0f),
            "toutes les attaques ne donnent pas d'invulnerabilite : une aura de degats, une zone de feu, un piege a pointes n'en donnent aucune");

        Check.False(aura.TryHit(hero, walker, invulnerability: 0f),
            "et la, le registre est SEUL a proteger. Sans lui, la cible perd un point de vie a chaque image de contact, soit soixante par seconde");

        Check.Equal(walker.Health, 99, "un seul point perdu, pas soixante");
        Check.Equal(aura.HitCount, 1, "une attaque, une cible, un coup, quoi qu'il arrive");
        Check.Equal(swing.HitCount, 1, "et chaque attaque tient son propre registre : elles ne se genent pas entre elles");

        var second = new Attack(2, damage: 5, force: 10f);

        Check.False(second.TryHit(hero, slime, IFrames),
            "une AUTRE attaque ne touche pas non plus, mais pour une raison differente : les images d'invulnerabilite courent encore");

        Check.True(slime.Invulnerable > 0f, "elles se decomptent avec le temps");

        for (int i = 0; i < 40; i++)
            slime.Tick(Frame);

        Check.True(slime.CanBeHit, "et une fois passees, la cible redevient touchable");
        Check.True(second.TryHit(hero, slime, IFrames), "la seconde attaque porte alors");
        Check.Equal(slime.Health, 13, "cinq degats de plus");

        var pushed = new Fighter(3, Vector2.Zero, 50);

        pushed.Knockback = new Vector2(60f, 0f);

        for (int i = 0; i < 10; i++)
            pushed.Tick(Frame);

        Check.True(pushed.Position.X > 0f, "un recul DEPLACE la cible");
        Check.True(pushed.Knockback.Length() < 60f, "et s'amortit : sans amortissement, l'ennemi part a l'infini");

        for (int i = 0; i < 200; i++)
            pushed.Tick(Frame);

        Check.Near(pushed.Knockback, Vector2.Zero, "jusqu'a s'arreter net, sans jamais changer de sens");

        var superposed = new Fighter(4, Vector2.Zero, 10);
        var contact = new Attack(3, damage: 1, force: 20f);

        Check.True(contact.TryHit(hero, superposed, IFrames), "deux corps exactement superposes, ca arrive : un ennemi qui apparait sur le joueur");

        Check.True(superposed.Knockback.Length() > 0f,
            "la direction n'est alors PAS calculable. Normaliser un vecteur nul rend NaN, et l'ennemi disparait de la carte a la premiere image : il faut une direction de repli");

        var dying = new Fighter(5, Vector2.Zero, 3);
        var lethal = new Attack(4, damage: 10, force: 5f);

        Check.True(lethal.TryHit(hero, dying, IFrames), "un coup mortel touche");
        Check.Equal(dying.Health, 0, "et les points de vie s'arretent a zero, jamais en dessous : un negatif traverse ensuite tous les calculs de pourcentage");
        Check.False(dying.Alive, "la cible est morte");
        Check.False(lethal.TryHit(hero, dying, IFrames), "et un cadavre ne se frappe pas : c'est le test qui evite les compteurs de degats a rallonge");
    }
}
