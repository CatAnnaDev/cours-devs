using Csharplings.Unity;

namespace Csharplings;

public sealed class EnemyStats : ScriptableObject
{
    public int MaxHealth = 30;

    public float Speed = 4f;

    public int CurrentHealth;
}

public sealed class Enemy
{
    public Enemy(EnemyStats stats)
    {
        Stats = stats;
        Health = stats.CurrentHealth;
    }

    public EnemyStats Stats { get; }

    public int Health { get; private set; }

    public float Speed => Stats.Speed;

    public void Damage(int amount) => Stats.CurrentHealth = Math.Max(0, Stats.CurrentHealth - amount);

    public void DamageTheWrongWay(int amount) => Stats.CurrentHealth = Math.Max(0, Stats.CurrentHealth - amount);
}

public static class Asset1
{
    public const bool NotDone = true;

    public static void Run()
    {
        ScriptableObject.ResetCounter();

        var database = new AssetDatabase();

        database.Register("Enemies/Gobelin", new EnemyStats { MaxHealth = 30, Speed = 4f, CurrentHealth = 30 });

        EnemyStats first = database.Load<EnemyStats>("Enemies/Gobelin");
        EnemyStats second = database.Load<EnemyStats>("Enemies/Gobelin");

        Check.True(ReferenceEquals(first, second),
            "charger deux fois le meme asset rend le MEME objet. Un ScriptableObject n'est pas un modele qu'on copie, c'est une instance unique partagee par tout le jeu");

        Check.Equal(ScriptableObject.LoadedFromDisk, 2, "deux chargements demandes");
        Check.Equal(database.Count, 1, "un seul asset en memoire");

        var pack = new List<Enemy>();

        for (int i = 0; i < 3; i++)
            pack.Add(new Enemy(first));

        Check.Equal(pack[0].Health, 30, "chaque ennemi part avec les points de vie de l'asset");
        Check.Equal(pack[0].Speed, 4f, "et sa vitesse");

        pack[0].Damage(10);

        Check.Equal(pack[0].Health, 20, "le premier prend des degats");
        Check.Equal(pack[1].Health, 30, "les deux autres n'ont rien : leur etat courant vit dans l'INSTANCE");
        Check.Equal(first.MaxHealth, 30, "et l'asset n'a pas bouge");

        pack[0].DamageTheWrongWay(10);

        Check.Equal(first.CurrentHealth, 20, "voila la mauvaise version : elle ecrit dans l'ASSET");
        Check.Equal(pack[1].Stats.CurrentHealth, 20,
            "donc les deux autres ennemis viennent de perdre dix points de vie eux aussi, sans avoir ete touches");

        Check.Equal(pack[2].Stats.CurrentHealth, 20, "tous, en fait : ils pointent le meme objet");

        EnemyStats reloaded = database.Load<EnemyStats>("Enemies/Gobelin");

        Check.Equal(reloaded.CurrentHealth, 20,
            "et le pire arrive ici : recharger l'asset ne le remet PAS a neuf. Dans l'editeur, la modification survit a l'arret du mode jeu et se retrouve versionnee dans le depot");

        Check.Equal(reloaded.MaxHealth, 30, "les champs de configuration, eux, sont a leur place : ils sont faits pour etre lus");

        var elite = database.Register("Enemies/Elite", new EnemyStats { MaxHealth = 200, Speed = 2f });

        Check.Equal(database.Count, 2, "un second asset donne un second reglage");
        Check.Equal(new Enemy(elite).Health, 200, "et les ennemis qui le pointent partent avec 200 points de vie");
        Check.Equal(first.MaxHealth, 30, "sans toucher au premier : c'est exactement ce qu'on achete avec un ScriptableObject");

        Check.True(elite.Speed < first.Speed,
            "regler un ennemi devient une modification de DONNEES, pas de code : le designer change une valeur dans l'inspecteur, personne ne recompile, et un patch d'equilibrage ne touche pas une ligne de C#");

        Check.Equal(pack[0].Health, 20,
            "la regle qui resume tout : l'asset porte la CONFIGURATION, l'instance porte l'ETAT. Un champ qui change pendant la partie n'a rien a faire dans un ScriptableObject");
    }
}
