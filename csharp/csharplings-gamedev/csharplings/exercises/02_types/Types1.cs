namespace Csharplings;

public static class Types1
{
    public const bool NotDone = true;

    public static void Run()
    {
        float speed = 2.5;
        double precise = 3.14159;
        char grade = "A";
        long huge = 9000000000;

        Check.Near(speed, 2.5, "un float a besoin du suffixe f");
        Check.Near(precise, 3.14159, "un double n'a besoin de rien");
        Check.Equal(grade, 'A', "un char se met entre apostrophes simples");
        Check.True(huge > int.MaxValue, "un long depasse la limite d'un int");
    }
}
