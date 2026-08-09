using System.Text;

namespace Csharplings;

public sealed class FakeDisk
{
    private readonly Dictionary<string, byte[]> _files = new();

    public bool FailMidWrite { get; set; }

    public bool Exists(string path) => _files.ContainsKey(path);

    public byte[] Read(string path) =>
        _files.TryGetValue(path, out byte[] content) ? content : throw new FileNotFoundException(path);

    public void Write(string path, ReadOnlySpan<byte> content)
    {
        if (FailMidWrite)
        {
            _files[path] = content.Slice(0, content.Length / 2).ToArray();

            throw new IOException("coupure pendant l'ecriture");
        }

        _files[path] = content.ToArray();
    }

    public void Rename(string from, string to)
    {
        _files[to] = _files[from];
        _files.Remove(from);
    }

    public void Delete(string path) => _files.Remove(path);
}

public static class Checksum1
{
    public const bool NotDone = true;

    public const uint Offset = 2166136261u;
    public const uint Prime = 16777619u;

    public static uint Hash(ReadOnlySpan<byte> content)
    {
        uint hash = Offset;

        foreach (byte value in content)
        {
            hash ^= value;
            hash *= Prime;
        }

        return hash;
    }

    public static byte[] Seal(ReadOnlySpan<byte> payload)
    {
        var sealed_ = new byte[payload.Length + 4];

        payload.CopyTo(sealed_);
        BitConverter.TryWriteBytes(sealed_.AsSpan(payload.Length), Hash(payload));

        return sealed_;
    }

    public static bool TryOpen(ReadOnlySpan<byte> sealedContent, out byte[] payload)
    {
        payload = null;

        if (sealedContent.Length < 4)
            return false;

        ReadOnlySpan<byte> body = sealedContent.Slice(0, sealedContent.Length - 4);

        payload = body.ToArray();

        return true;
    }

    public static void SaveAtomically(FakeDisk disk, string path, ReadOnlySpan<byte> payload)
    {
        disk.Write(path, Seal(payload));
    }

    public static void Run()
    {
        byte[] payload = Encoding.UTF8.GetBytes("anna:7:cave");

        Check.Equal(Hash(payload), Hash(payload), "une empreinte est deterministe");
        Check.True(Hash(payload) != Hash(Encoding.UTF8.GetBytes("anna:8:cave")),
            "et un seul caractere different la change du tout au tout : c'est ce qui permet de detecter une corruption");

        Check.Equal(Hash(ReadOnlySpan<byte>.Empty), Offset, "l'empreinte du vide est la valeur de depart");

        byte[] file = Seal(payload);

        Check.Equal(file.Length, payload.Length + 4, "sceller ajoute quatre octets a la fin");
        Check.True(TryOpen(file, out byte[] opened), "et un fichier intact se rouvre");
        Check.Sequence(opened, payload, "avec exactement son contenu");

        file[2] ^= 0xFF;

        Check.False(TryOpen(file, out byte[] broken), "un octet retourne suffit a faire echouer la verification");
        Check.True(broken is null, "et on ne rend RIEN : une sauvegarde corrompue n'est pas une sauvegarde partielle, c'est une sauvegarde absente");

        Check.False(TryOpen(new byte[2], out _), "un fichier plus court que son empreinte est corrompu par definition");

        var disk = new FakeDisk();

        SaveAtomically(disk, "save.dat", payload);

        Check.True(disk.Exists("save.dat"), "la sauvegarde atomique ecrit le fichier");
        Check.False(disk.Exists("save.dat.tmp"), "et ne laisse pas de temporaire derriere elle");
        Check.True(TryOpen(disk.Read("save.dat"), out _), "il est valide");

        byte[] second = Encoding.UTF8.GetBytes("anna:8:surface");

        disk.FailMidWrite = true;

        Check.Throws<IOException>(() => SaveAtomically(disk, "save.dat", second),
            "on simule maintenant une coupure de courant au milieu de l'ecriture");

        disk.FailMidWrite = false;

        Check.True(TryOpen(disk.Read("save.dat"), out byte[] survivor),
            "et voila tout l'interet : l'ANCIENNE sauvegarde est intacte. On a ecrit dans un temporaire, donc le fichier reel n'a jamais ete ouvert en ecriture");

        Check.Sequence(survivor, payload,
            "le joueur perd sa derniere partie, pas les quarante heures d'avant. Ecrire directement par-dessus, c'est perdre les deux");

        disk.Write("direct.dat", Seal(payload));
        disk.FailMidWrite = true;

        Check.Throws<IOException>(() => disk.Write("direct.dat", Seal(second)), "la version naive, elle, ecrit par-dessus");

        disk.FailMidWrite = false;

        Check.False(TryOpen(disk.Read("direct.dat"), out _),
            "et le fichier reel est maintenant a moitie ecrit, donc illisible. Un temporaire, une relecture, un renommage : trois lignes qui separent 'j'ai perdu ma partie' de 'j'ai perdu mon jeu'");
    }
}
