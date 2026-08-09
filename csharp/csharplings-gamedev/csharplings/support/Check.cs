using System.Collections;

namespace Csharplings;

public sealed class CheckFailedException : Exception
{
    public CheckFailedException(string message) : base(message) { }
}

public static class Todo
{
    public static T Value<T>() =>
        throw new CheckFailedException("remplace Todo.Value<...>() par ta reponse");
}

public static class Check
{
    public static void Equal<T>(T actual, T expected, string what)
    {
        if (EqualityComparer<T>.Default.Equals(actual, expected))
        {
            Pass(what, actual);
            return;
        }

        throw new CheckFailedException($"{what}\n      attendu : {Show(expected)}\n      obtenu  : {Show(actual)}");
    }

    public static void True(bool condition, string what)
    {
        if (condition)
        {
            Pass(what, true);
            return;
        }

        throw new CheckFailedException($"{what}\n      attendu : true\n      obtenu  : false");
    }

    public static void False(bool condition, string what) => True(!condition, what);

    public static void Near(double actual, double expected, string what, double tolerance = 0.001)
    {
        if (Math.Abs(actual - expected) <= tolerance)
        {
            Pass(what, actual);
            return;
        }

        throw new CheckFailedException($"{what}\n      attendu : {expected} (a {tolerance} pres)\n      obtenu  : {actual}");
    }

    public static void Near(Vector2 actual, Vector2 expected, string what, double tolerance = 0.001)
    {
        if (Mathf.Abs(actual.X - expected.X) <= tolerance && Mathf.Abs(actual.Y - expected.Y) <= tolerance)
        {
            Pass(what, actual);
            return;
        }

        throw new CheckFailedException($"{what}\n      attendu : {expected} (a {tolerance} pres)\n      obtenu  : {actual}");
    }

    public static void Sequence<T>(IEnumerable<T> actual, IEnumerable<T> expected, string what)
    {
        List<T> a = actual?.ToList() ?? new List<T>();
        List<T> e = expected.ToList();

        if (a.SequenceEqual(e))
        {
            Pass(what, a);
            return;
        }

        throw new CheckFailedException($"{what}\n      attendu : {Show(e)}\n      obtenu  : {Show(a)}");
    }

    public static void Throws<TException>(Action action, string what)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            Pass(what, typeof(TException).Name);
            return;
        }
        catch (Exception other)
        {
            throw new CheckFailedException($"{what}\n      attendu : {typeof(TException).Name}\n      obtenu  : {other.GetType().Name}");
        }

        throw new CheckFailedException($"{what}\n      attendu : {typeof(TException).Name}\n      obtenu  : aucune exception");
    }

    private static void Pass(string what, object value) =>
        Console.WriteLine($"      ok  {what}  ({Show(value)})");

    private static string Show(object value)
    {
        if (value is null)
            return "null";

        if (value is string s)
            return $"\"{s}\"";

        if (value is bool b)
            return b ? "true" : "false";

        if (value is IEnumerable list and not string)
            return "[" + string.Join(", ", list.Cast<object>().Select(Show)) + "]";

        return value.ToString();
    }
}
