namespace Csharplings;

public class Enemy
{
    public virtual int Damage() => 5;

    public virtual string Describe() => $"ennemi ({Damage()} degats)";
}

public sealed class Boss : Enemy
{
    public int Damage() => 50;

    public string Describe() => "BOSS : " + base.Describe();
}

public static class Classes3
{
    public const bool NotDone = true;

    public static void Run()
    {
        var basic = new Enemy();
        var boss = new Boss();

        Check.Equal(basic.Damage(), 5, "un ennemi normal tape a 5");
        Check.Equal(boss.Damage(), 50, "le boss tape a 50");
        Check.Equal(boss.Describe(), "BOSS : ennemi (50 degats)", "base.Describe() doit utiliser le Damage du boss");

        Enemy vuAsEnemy = boss;
        Check.Equal(vuAsEnemy.Damage(), 50, "meme vu comme un Enemy, c'est le code du Boss qui tourne");
    }
}
