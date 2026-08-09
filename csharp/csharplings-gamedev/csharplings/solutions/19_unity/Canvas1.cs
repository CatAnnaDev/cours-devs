using Csharplings.Unity;

namespace Csharplings;

public static class Canvas1
{
    public const bool NotDone = false;

    public static void Run()
    {
        Time.Reset();
        Canvas.Clear();

        var scene = new Scene();
        var single = new Canvas();

        CanvasElement score = single.Add("score 0");
        CanvasElement timer = single.Add("00:00");

        for (int i = 0; i < 18; i++)
            single.Add("emplacement " + i);

        Check.Equal(single.ElementCount, 20, "un canevas de vingt elements : un score, un chrono, et l'inventaire");
        Check.True(single.Dirty, "il est sale a la construction, forcement");

        scene.Frame();

        Check.False(single.Dirty, "l'image le reconstruit une fois, puis il est propre");
        Check.Equal(Canvas.Rebuilds, 1, "une reconstruction");
        Check.Equal(Canvas.RebuiltElements, 20, "de vingt elements");

        scene.Frame();

        Check.Equal(Canvas.Rebuilds, 1, "une image ou rien ne change ne reconstruit RIEN : c'est le cas frequent, et il est gratuit");

        timer.Text = "00:01";

        scene.Frame();

        Check.Equal(Canvas.Rebuilds, 2, "changer le chrono salit le canevas");
        Check.Equal(Canvas.RebuiltElements, 40,
            "et il faut relire les VINGT elements. Un caractere de chrono qui change fait recalculer tout l'inventaire, soixante fois par seconde : c'est la premiere cause de saccades d'interface");

        timer.Text = "00:01";

        scene.Frame();

        Check.Equal(Canvas.Rebuilds, 2,
            "en revanche, reecrire la MEME valeur ne salit rien : le test d'egalite avant l'ecriture est la parade la moins chere qui existe, et presque personne ne l'ecrit");

        Canvas.Clear();

        var still = new Canvas();
        var moving = new Canvas();

        for (int i = 0; i < 18; i++)
            still.Add("emplacement " + i);

        CanvasElement liveTimer = moving.Add("00:00");
        CanvasElement liveScore = moving.Add("score 0");

        scene.Frame();

        Check.Equal(Canvas.Rebuilds, 2, "deux canevas, deux reconstructions initiales");
        Check.Equal(Canvas.RebuiltElements, 20, "vingt elements au total, comme avant");

        liveTimer.Text = "00:01";

        scene.Frame();

        Check.Equal(Canvas.Rebuilds, 3, "le chrono change");
        Check.Equal(Canvas.RebuiltElements, 22,
            "et seuls DEUX elements sont relus : l'inventaire est sur un autre canevas, et celui-la est reste propre. Voila tout le gain, pour une decoupe qui ne coute rien");

        Check.False(still.Dirty, "le canevas immobile n'a pas bouge");
        Check.True(!moving.Dirty, "et le canevas vivant a ete traite");

        liveScore.Text = "score 10";
        liveTimer.Text = "00:02";

        scene.Frame();

        Check.Equal(Canvas.Rebuilds, 4,
            "deux changements dans la MEME image ne font qu'une reconstruction : le canevas est marque sale, et il n'est relu qu'une fois, a la fin");

        Check.Equal(Canvas.RebuiltElements, 24, "deux elements de plus, pas quatre");

        Check.True(still.ElementCount > moving.ElementCount,
            "la regle : separer ce qui change a chaque image de ce qui ne change jamais. Un canevas pour le HUD vivant, un pour les menus, un pour l'inventaire - et surtout pas un seul pour toute l'interface");
    }
}
