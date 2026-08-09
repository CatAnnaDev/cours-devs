using System.Text.Json;
using System.Text.Json.Serialization;

namespace Csharplings;

public sealed class Traveler
{
    public string Name { get; set; } = "sans nom";

    public int Level { get; set; } = 1;

    public List<string> Flags { get; set; } = new();
}

public static class Read1
{
    public const bool NotDone = true;

    public static readonly JsonSerializerOptions Strict = new();

    public static readonly JsonSerializerOptions Tolerant = new()
    {
    };

    public static Traveler Read(string json) => JsonSerializer.Deserialize<Traveler>(json, Tolerant);

    public static bool TryRead(string json, out Traveler traveler)
    {
        traveler = JsonSerializer.Deserialize<Traveler>(json, Tolerant);

        return traveler is not null;
    }

    public static void Run()
    {
        const string written = "{\"name\":\"bob\",\"level\":3,\"flags\":[\"cave\"]}";

        Traveler ignored = JsonSerializer.Deserialize<Traveler>(written, Strict);

        Check.Equal(ignored.Name, "sans nom",
            "le piege numero un : par defaut la CASSE compte. 'name' ne remplit pas 'Name', et rien ne proteste");
        Check.Equal(ignored.Level, 1,
            "aucune exception, aucun avertissement : une sauvegarde entiere qui revient vide");
        Check.Equal(ignored.Flags.Count, 0,
            "ce que tu lis, ce sont les valeurs des initialiseurs de la classe, pas celles du fichier");

        Traveler loaded = Read(written);

        Check.Equal(loaded.Name, "bob", "avec PropertyNameCaseInsensitive, le meme texte se relit");
        Check.Equal(loaded.Level, 3, "le niveau aussi");
        Check.Sequence(loaded.Flags, new[] { "cave" }, "et la liste");

        Traveler empty = Read("{}");

        Check.Equal(empty.Name, "sans nom", "un objet vide laisse les initialiseurs en place");
        Check.Equal(empty.Level, 1, "chaque champ absent garde sa valeur par defaut");
        Check.True(empty.Flags is not null, "et une liste initialisee reste une liste, jamais null : c'est ce qui evite le NullReference au chargement");

        Check.True(Read("null") is null,
            "en revanche un fichier qui contient litteralement null rend null : une reference, pas un objet vide");

        Check.Equal(Read("{\"Name\":\"a\",\"skin\":\"rouge\"}").Name, "a",
            "un champ inconnu est ignore en silence, ce qui permet de relire un fichier ecrit par une version plus recente");

        Check.Throws<JsonException>(() => JsonSerializer.Deserialize<Traveler>("{\"Level\":\"3\"}", Strict),
            "un nombre ecrit entre guillemets est une erreur pour le lecteur strict");

        Check.Equal(Read("{\"Level\":\"3\"}").Level, 3,
            "NumberHandling.AllowReadingFromString l'accepte, ce qui sauve les fichiers ecrits par un editeur ou un serveur");

        Check.Equal(Read("{\"Level\":3,}").Level, 3,
            "AllowTrailingCommas pardonne la virgule en trop, celle que tout le monde laisse en editant a la main");

        Check.Equal(Read("{/* le niveau du boss */\"Level\":9}").Level, 9,
            "et ReadCommentHandling.Skip laisse passer les commentaires, que le JSON n'autorise pas");

        Check.Throws<JsonException>(() => JsonSerializer.Deserialize<Traveler>("", Strict),
            "un fichier VIDE n'est pas un objet vide : c'est une erreur");

        Check.Throws<JsonException>(() => JsonSerializer.Deserialize<Traveler>("{\"Name\":", Strict),
            "et un fichier coupe en plein milieu aussi, ce qui arrive a chaque coupure de courant pendant une sauvegarde");

        Check.False(TryRead("{\"Name\":", out Traveler broken),
            "d'ou la version qui ne plante pas : elle attrape JsonException et rend false");
        Check.True(broken is null, "sans laisser d'objet a moitie rempli derriere elle");

        Check.False(TryRead("null", out _), "un null n'est pas une sauvegarde valide non plus");

        Check.True(TryRead(written, out Traveler good), "et un fichier correct rend true");
        Check.Equal(good.Name, "bob", "avec l'objet rempli");

        string again = JsonSerializer.Serialize(good, Tolerant);

        Check.Equal(Read(again).Name, "bob", "relire ce qu'on vient d'ecrire doit redonner le meme objet");
        Check.Equal(JsonSerializer.Serialize(Read(again), Tolerant), again,
            "et reecrire ce qu'on vient de relire doit redonner le meme texte : c'est le seul test qui prouve qu'une sauvegarde tient");
    }
}
