namespace Csharplings.Runner;

public sealed record Question(
    string Section,
    string Topic,
    string Prompt,
    string[] Answers,
    int Correct,
    string Explanation);

public static class Quiz
{
    public static readonly IReadOnlyList<Question> All = Bank.Build();

    public static int Run(string filter)
    {
        Question[] chosen = Select(filter);

        if (chosen.Length == 0)
        {
            Write($"  aucune question pour '{filter}'\n", ConsoleColor.Red);
            Write("  essaie une section : 15_perf, 17_ecs, 20_time...\n", ConsoleColor.DarkGray);

            return 1;
        }

        Random.Shared.Shuffle(chosen);

        var missed = new List<Question>();
        int correct = 0;
        int asked = 0;

        Console.WriteLine();
        Write($"  {chosen.Length} questions", ConsoleColor.Cyan);

        if (string.IsNullOrWhiteSpace(filter))
            Console.Write($"  ·  profil {Config.Current.ToString().ToLowerInvariant()}");

        Console.WriteLine("  ·  tape le numero de ta reponse, 'q' pour arreter");

        foreach (Question question in chosen)
        {
            asked++;

            Present(question, asked, chosen.Length);

            int answer = ReadChoice(question.Answers.Length);

            if (answer == -1)
            {
                asked--;
                break;
            }

            Console.WriteLine();

            if (answer == question.Correct)
            {
                correct++;
                Write("  ✓ juste\n", ConsoleColor.Green);
            }
            else
            {
                missed.Add(question);
                Write($"  ✗ non, c'etait la {question.Correct + 1} : {question.Answers[question.Correct]}\n", ConsoleColor.Red);
            }

            foreach (string line in Wrap(question.Explanation, 84))
                Console.WriteLine($"    {line}");
        }

        Summarise(correct, asked, missed);

        return 0;
    }

    public static int List()
    {
        Console.WriteLine();

        foreach (IGrouping<string, Question> group in All.GroupBy(question => question.Section).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            EngineTarget target = Targets.OfSection(group.Key);
            bool shown = Targets.Allows(Config.Current, target);
            string tag = target == EngineTarget.Any ? string.Empty : $"  ({target.ToString().ToLowerInvariant()} seulement)";

            Write(shown ? "  " : "  - ", shown ? ConsoleColor.Gray : ConsoleColor.DarkGray);
            Console.WriteLine($"{group.Key,-14} {group.Count(),2} questions{tag}");
        }

        int visible = All.Count(question => Targets.Allows(Config.Current, Targets.Of(question)));

        Console.WriteLine();
        Console.WriteLine($"  {visible} pour le profil '{Config.Current.ToString().ToLowerInvariant()}', {All.Count} au total");
        Console.WriteLine();

        return 0;
    }

    private static Question[] Select(string filter)
    {
        if (!string.IsNullOrWhiteSpace(filter))
            return All
                .Where(question => question.Section.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToArray();

        return All
            .Where(question => Targets.Allows(Config.Current, Targets.Of(question)))
            .ToArray();
    }

    private static void Present(Question question, int index, int total)
    {
        Console.WriteLine();
        Console.WriteLine(new string('-', 72));
        Write($"  {index} / {total}", ConsoleColor.DarkGray);
        Write($"   ·   {question.Section}\n", ConsoleColor.Cyan);
        Console.WriteLine();

        foreach (string line in Wrap(question.Prompt, 84))
            Console.WriteLine($"  {line}");

        Console.WriteLine();

        for (int i = 0; i < question.Answers.Length; i++)
            Console.WriteLine($"    {i + 1}) {question.Answers[i]}");

        Console.WriteLine();
        Console.Write("  ton choix > ");
    }

    private static int ReadChoice(int count)
    {
        while (true)
        {
            string line = Console.ReadLine();

            if (line is null)
            {
                Console.WriteLine();

                return -1;
            }

            line = line.Trim();

            if (line.Length == 0 || line is "q" or "Q")
                return -1;

            if (int.TryParse(line, out int choice) && choice >= 1 && choice <= count)
                return choice - 1;

            Console.Write($"  entre 1 et {count}, ou q > ");
        }
    }

    private static void Summarise(int correct, int asked, List<Question> missed)
    {
        Console.WriteLine();
        Console.WriteLine(new string('-', 72));

        if (asked == 0)
        {
            Write("  aucune question repondue.\n\n", ConsoleColor.DarkGray);

            return;
        }

        ConsoleColor colour = correct == asked
            ? ConsoleColor.Green
            : correct * 2 >= asked ? ConsoleColor.Yellow : ConsoleColor.Red;

        Write($"  {correct} / {asked} bonnes reponses\n", colour);

        if (missed.Count == 0)
        {
            Console.WriteLine();
            Write("  Tout juste. Tu peux passer a la suite.\n\n", ConsoleColor.Green);

            return;
        }

        Console.WriteLine();
        Console.WriteLine("  a revoir :");

        foreach (IGrouping<string, Question> group in missed.GroupBy(question => question.Section))
        {
            foreach (Question question in group)
                Console.WriteLine($"    {group.Key,-14} {question.Topic}");
        }

        Console.WriteLine();
        Console.WriteLine("  pour reviser une section : dotnet run -- quiz " + missed[0].Section);
        Console.WriteLine();
    }

    private static IEnumerable<string> Wrap(string text, int width)
    {
        foreach (string paragraph in text.Split('\n'))
        {
            if (paragraph.Length <= width)
            {
                yield return paragraph;

                continue;
            }

            var line = new System.Text.StringBuilder();

            foreach (string word in paragraph.Split(' '))
            {
                bool loneMark = word.Length == 1 && !char.IsLetterOrDigit(word[0]);

                if (line.Length > 0 && !loneMark && line.Length + 1 + word.Length > width)
                {
                    yield return line.ToString();
                    line.Clear();
                }

                if (line.Length > 0)
                    line.Append(' ');

                line.Append(word);
            }

            if (line.Length > 0)
                yield return line.ToString();
        }
    }

    private static void Write(string text, ConsoleColor colour)
    {
        ConsoleColor previous = Console.ForegroundColor;
        Console.ForegroundColor = colour;
        Console.Write(text);
        Console.ForegroundColor = previous;
    }
}
