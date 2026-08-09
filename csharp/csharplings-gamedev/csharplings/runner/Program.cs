using System.Text.RegularExpressions;

namespace Csharplings.Runner;

public static class Program
{
    private static readonly Regex NotDonePattern =
        new(@"NotDone\s*=\s*true", RegexOptions.Compiled);

    public static int Main(string[] args)
    {
        string command = args.Length > 0 ? args[0].ToLowerInvariant() : "watch";
        string target = args.Length > 1 ? args[1] : null;

        return command switch
        {
            "watch" => Watch(),
            "list" => List(),
            "run" => RunOne(target),
            "hint" => Hint(target),
            "solution" => ShowSolution(target),
            "verify" => VerifyAllSolutions(),
            "quiz" => target is "list" ? Quiz.List() : Quiz.Run(target),
            "config" => Configure(target),
            _ => Usage(),
        };
    }

    private static bool Visible(Exercise exercise) =>
        Targets.Allows(Config.Current, Targets.Of(exercise));

    private static int Configure(string wanted)
    {
        if (!string.IsNullOrWhiteSpace(wanted))
        {
            if (!Config.TryParse(wanted, out Profile parsed))
            {
                Write($"  profil inconnu : {wanted}\n", ConsoleColor.Red);
                Write("  au choix : pure, godot, unity, all\n", ConsoleColor.DarkGray);

                return 1;
            }

            Config.Save(parsed);
        }

        Profile profile = Config.Current;

        Console.WriteLine();
        Write($"  profil : {profile.ToString().ToLowerInvariant()}", ConsoleColor.Cyan);
        Console.WriteLine($"  ·  {Config.Describe(profile)}");
        Console.WriteLine();

        foreach (Profile candidate in new[] { Profile.Pure, Profile.Godot, Profile.Unity, Profile.All })
        {
            int exercises = Catalog.All.Count(e => Targets.Allows(candidate, Targets.Of(e)));
            int questions = Quiz.All.Count(q => Targets.Allows(candidate, Targets.Of(q)));

            Write(candidate == profile ? "    > " : "      ", ConsoleColor.Green);
            Console.WriteLine($"{candidate.ToString().ToLowerInvariant(),-6} {exercises,3} exercices, {questions,2} questions   {Config.Describe(candidate)}");
        }

        Console.WriteLine();
        Console.WriteLine("  Ce qui est masque n'est pas verrouille : 'run', 'hint' et 'solution'");
        Console.WriteLine("  marchent sur n'importe quel exercice, profil ou pas.");
        Console.WriteLine();
        Console.WriteLine("  changer : dotnet run -- config unity");
        Console.WriteLine();

        return 0;
    }

    private static int Usage()
    {
        Console.WriteLine("""
            csharplings — apprendre le C# en reparant du code

              dotnet run                    reprend la ou tu en es (mode surveillance)
              dotnet run -- list            la liste des exercices et ton avancement
              dotnet run -- run <id>        lance un exercice precis
              dotnet run -- hint <id>       un indice
              dotnet run -- solution <id>   la correction
              dotnet run -- verify          verifie que toutes les solutions passent
              dotnet run -- quiz            questionnaire, selon ton profil
              dotnet run -- quiz 15_perf    seulement une section
              dotnet run -- quiz list       combien de questions par section
              dotnet run -- config          ton profil : pure, godot, unity ou all
              dotnet run -- config unity    n'avoir que le C# et Unity
            """);

        return 0;
    }

    private static int List()
    {
        List<Exercise> shown = Catalog.All.Where(Visible).ToList();
        string section = null;

        foreach (Exercise exercise in shown)
        {
            if (exercise.Section != section)
            {
                section = exercise.Section;
                Console.WriteLine();
                Console.WriteLine($"  {section}");
            }

            bool done = IsDone(exercise);
            Write(done ? "    [x] " : "    [ ] ", done ? ConsoleColor.Green : ConsoleColor.DarkGray);
            Console.WriteLine($"{exercise.Id,-14} {exercise.Title}");
        }

        int completed = shown.Count(IsDone);
        int hidden = Catalog.All.Count - shown.Count;

        Console.WriteLine();
        Console.WriteLine($"  {completed} / {shown.Count} termines");

        if (hidden > 0)
        {
            Write($"  {hidden} exercices masques par le profil '{Config.Current.ToString().ToLowerInvariant()}'", ConsoleColor.DarkGray);
            Write("  ·  dotnet run -- config\n", ConsoleColor.DarkGray);
        }

        Console.WriteLine();

        return 0;
    }

    private const int SettleMilliseconds = 120;
    private const int FallbackPollMilliseconds = 1000;

    private static int Watch()
    {
        while (true)
        {
            Exercise exercise = Catalog.All.FirstOrDefault(e => Visible(e) && !IsDone(e));

            if (exercise is null)
            {
                Celebrate();
                return 0;
            }

            string file = Paths.ExerciseFile(exercise);
            DateTime stamp = File.GetLastWriteTimeUtc(file);

            Present(exercise, file);
            WaitForSave(file, stamp);
        }
    }

    private static void WaitForSave(string file, DateTime stamp)
    {
        using var watcher = new FileSystemWatcher(Path.GetDirectoryName(file), Path.GetFileName(file))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
        };

        while (File.GetLastWriteTimeUtc(file) == stamp)
            watcher.WaitForChanged(WatcherChangeTypes.All, FallbackPollMilliseconds);

        Thread.Sleep(SettleMilliseconds);
    }

    private static void Present(Exercise exercise, string file)
    {
        Console.WriteLine();
        Console.WriteLine(new string('-', 72));
        Write($"  {exercise.Id}", ConsoleColor.Cyan);
        Console.WriteLine($"  ·  {exercise.Title}");
        Write($"  {Paths.Display(file)}\n", ConsoleColor.DarkGray);
        Console.WriteLine();

        foreach (string line in exercise.Instructions.Split('\n'))
            Console.WriteLine($"  {line}");

        Console.WriteLine();

        RunResult result = Sandbox.Run(exercise, file);

        if (result.Output.Length > 0)
            Console.WriteLine(result.Output);

        Console.WriteLine();

        if (!result.Ok)
        {
            Write("  ✗ pas encore. Corrige le fichier et sauvegarde.\n", ConsoleColor.Red);
            Write($"    indice : dotnet run -- hint {exercise.Id}\n", ConsoleColor.DarkGray);
            return;
        }

        if (StillMarkedNotDone(file))
        {
            Write("  ✓ le code passe les verifications.\n", ConsoleColor.Green);
            Write("    Passe 'NotDone' a false dans le fichier pour valider l'exercice.\n", ConsoleColor.Yellow);
            return;
        }

        Write("  ✓ valide\n", ConsoleColor.Green);
    }

    private static int RunOne(string id)
    {
        Exercise exercise = Resolve(id);
        if (exercise is null)
            return 1;

        Present(exercise, Paths.ExerciseFile(exercise));
        return 0;
    }

    private static int Hint(string id)
    {
        Exercise exercise = Resolve(id);
        if (exercise is null)
            return 1;

        Console.WriteLine();
        Write($"  indice · {exercise.Id}\n", ConsoleColor.Yellow);
        Console.WriteLine($"  {exercise.Hint}");
        Console.WriteLine();

        return 0;
    }

    private static int ShowSolution(string id)
    {
        Exercise exercise = Resolve(id);
        if (exercise is null)
            return 1;

        string file = Paths.SolutionFile(exercise);

        if (!File.Exists(file))
        {
            Write($"  pas de solution pour {exercise.Id}\n", ConsoleColor.Red);
            return 1;
        }

        Console.WriteLine();
        Write($"  {Paths.Display(file)}\n\n", ConsoleColor.DarkGray);
        Console.WriteLine(File.ReadAllText(file));

        return 0;
    }

    private static int VerifyAllSolutions()
    {
        var broken = new List<string>();

        foreach (Exercise exercise in Catalog.All)
        {
            string file = Paths.SolutionFile(exercise);

            if (!File.Exists(file))
            {
                broken.Add($"{exercise.Id} : solution absente");
                continue;
            }

            RunResult result = Sandbox.Run(exercise, file);

            if (result.Ok && !StillMarkedNotDone(file))
            {
                Write("  ok   ", ConsoleColor.Green);
                Console.WriteLine(exercise.Id);
                continue;
            }

            Write("  RATE ", ConsoleColor.Red);
            Console.WriteLine(exercise.Id);
            Console.WriteLine(result.Output);
            broken.Add(exercise.Id);
        }

        Console.WriteLine();
        Console.WriteLine($"  {Catalog.All.Count - broken.Count} / {Catalog.All.Count} solutions valides");

        return broken.Count == 0 ? 0 : 1;
    }

    private static Exercise Resolve(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Write("  precise un exercice, par exemple : dotnet run -- hint variables1\n", ConsoleColor.Red);
            return null;
        }

        Exercise exercise = Catalog.Find(id);

        if (exercise is null)
            Write($"  exercice inconnu : {id}\n", ConsoleColor.Red);

        return exercise;
    }

    private static bool IsDone(Exercise exercise)
    {
        string file = Paths.ExerciseFile(exercise);
        return File.Exists(file) && !StillMarkedNotDone(file);
    }

    private static bool StillMarkedNotDone(string file) =>
        NotDonePattern.IsMatch(File.ReadAllText(file));

    private static void Celebrate()
    {
        int hidden = Catalog.All.Count(e => !Visible(e));

        Console.WriteLine();
        Write("  Tous les exercices de ton profil sont termines.\n", ConsoleColor.Green);
        Console.WriteLine();

        if (hidden > 0)
        {
            Console.WriteLine($"  Il reste {hidden} exercices que le profil '{Config.Current.ToString().ToLowerInvariant()}' met de cote.");
            Console.WriteLine("  Si la curiosite te prend : dotnet run -- config all");
            Console.WriteLine();
        }

        Console.WriteLine("  Teste ce que tu as retenu : dotnet run -- quiz");
        Console.WriteLine();
        Console.WriteLine("  La suite se passe dans ../demos : du vrai code Godot, commente nulle part");
        Console.WriteLine("  mais qui s'explique tout seul quand tu le lances.");
        Console.WriteLine();
        Console.WriteLine("  Commence par demos/CHEATSHEET.md.");
        Console.WriteLine();
    }

    private static void Write(string text, ConsoleColor color)
    {
        ConsoleColor previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previous;
    }
}
