using System.Text;

namespace Csharplings;

public sealed class HudLabel
{
    private readonly StringBuilder _builder = new();

    private int _shownCurrent = -1;
    private int _shownMax = -1;
    private string _text = string.Empty;

    public int Rebuilds { get; private set; }

    public string Text(int current, int max)
    {
        if (current == _shownCurrent && max == _shownMax)
            return _text;

        _shownCurrent = current;
        _shownMax = max;
        Rebuilds++;

        _builder.Clear();
        _builder.Append("PV ").Append(current).Append('/').Append(max);
        _text = _builder.ToString();

        return _text;
    }
}

public static class Text1
{
    public const bool NotDone = false;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static string JoinSlow(List<string> parts)
    {
        string result = string.Empty;

        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
                result += ", ";

            result += parts[i];
        }

        return result;
    }

    public static string Join(List<string> parts)
    {
        var builder = new StringBuilder();

        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
                builder.Append(", ");

            builder.Append(parts[i]);
        }

        return builder.ToString();
    }

    public static void Run()
    {
        var label = new HudLabel();

        Check.Equal(label.Text(100, 100), "PV 100/100", "le texte affiche");
        Check.Equal(label.Rebuilds, 1, "construit une fois");

        for (int frame = 0; frame < 1000; frame++)
        {
            string shown = label.Text(100, 100);
        }

        Check.Equal(label.Rebuilds, 1, "mille frames sans degat : toujours une seule construction");

        Check.Equal(
            Measure(() =>
            {
                for (int frame = 0; frame < 1000; frame++)
                {
                    string shown = label.Text(100, 100);
                }
            }),
            0L,
            "et zero octet : le HUD ne doit pas nourrir le ramasse-miettes quand rien ne change");

        Check.Equal(label.Text(99, 100), "PV 99/100", "quand la valeur change, le texte suit");
        Check.Equal(label.Rebuilds, 2, "et on reconstruit exactement une fois");

        var parts = new List<string>();

        for (int i = 0; i < 100; i++)
            parts.Add($"item{i}");

        Check.Equal(Join(parts), JoinSlow(parts), "les deux versions produisent le meme texte");
        Check.True(Join(parts).StartsWith("item0, item1"), "et il est bien forme");

        long builder = Measure(() => Join(parts));
        long concat = Measure(() => JoinSlow(parts));

        Check.True(concat > builder * 2,
            "coller 100 morceaux avec '+=' fabrique 100 chaines intermediaires ; le StringBuilder n'en fabrique qu'une");
    }
}
