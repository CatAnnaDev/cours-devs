using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Demos.Bases;

public partial class CollectionsChoice : Node
{
    public override void _Ready()
    {
        ArrayWhenSizeIsFixed();
        ListWhenSizeGrows();
        DictionaryWhenYouLookUpByKey();
        HashSetWhenYouOnlyAskDoesItContain();
        QueueWhenOrderMatters();
        LinqWhenYouDescribeInsteadOfLoop();
    }

    private static void ArrayWhenSizeIsFixed()
    {
        float[] damagePerLevel = { 10f, 15f, 22f, 33f };
        GD.Print($"array    : taille figee, acces le plus rapide -> niveau 3 = {damagePerLevel[2]}");
    }

    private static void ListWhenSizeGrows()
    {
        var enemies = new List<string> { "slime", "gobelin" };
        enemies.Add("dragon");
        enemies.Remove("slime");
        GD.Print($"list     : taille variable, ordre garanti -> {string.Join(", ", enemies)}");
    }

    private static void DictionaryWhenYouLookUpByKey()
    {
        var loot = new Dictionary<string, int> { ["potion"] = 3, ["cle"] = 1 };
        loot["potion"] += 2;

        bool found = loot.TryGetValue("epee", out int count);
        GD.Print($"dict     : recherche instantanee par cle -> potions={loot["potion"]}, epee trouvee={found} ({count})");
    }

    private static void HashSetWhenYouOnlyAskDoesItContain()
    {
        var visitedRooms = new HashSet<string>();
        bool firstVisit = visitedRooms.Add("crypte");
        bool secondVisit = visitedRooms.Add("crypte");
        GD.Print($"hashset  : unicite auto, Contains instantane -> 1re={firstVisit} 2e={secondVisit}");
    }

    private static void QueueWhenOrderMatters()
    {
        var dialogue = new Queue<string>();
        dialogue.Enqueue("Bonjour.");
        dialogue.Enqueue("Tu es qui ?");
        GD.Print($"queue    : premier entre premier sorti -> {dialogue.Dequeue()}");

        var undo = new Stack<string>();
        undo.Push("place mur");
        undo.Push("place porte");
        GD.Print($"stack    : dernier entre premier sorti -> annule '{undo.Pop()}'");
    }

    private static void LinqWhenYouDescribeInsteadOfLoop()
    {
        var scores = new List<int> { 42, 7, 99, 13, 64 };

        List<int> bigOnes = scores.Where(s => s > 40).OrderByDescending(s => s).ToList();
        int total = scores.Sum();
        int best = scores.Max();
        bool anyPerfect = scores.Any(s => s == 100);

        GD.Print($"linq     : {string.Join(" > ", bigOnes)} | total={total} best={best} parfait={anyPerfect}");
    }
}
