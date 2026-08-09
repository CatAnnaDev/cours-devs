using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CuiseurVAT
{
    private const string Dossier = "Assets/VAT";

    [MenuItem("Cours/Cuire une VAT depuis la selection")]
    public static void Cuire()
    {
        var objet = Selection.activeGameObject;
        if (objet == null)
        {
            Debug.LogError("Selectionne un GameObject portant un SkinnedMeshRenderer et un Animator.");
            return;
        }

        var peau = objet.GetComponentInChildren<SkinnedMeshRenderer>();
        var animateur = objet.GetComponentInChildren<Animator>();
        if (peau == null || animateur == null || animateur.runtimeAnimatorController == null)
        {
            Debug.LogError("Il faut un SkinnedMeshRenderer et un Animator avec un controller.");
            return;
        }

        var clip = animateur.runtimeAnimatorController.animationClips[0];
        var nombreImages = Mathf.Max(2, Mathf.RoundToInt(clip.length * clip.frameRate));
        var maille = new Mesh();

        var positions = new List<Vector3[]>(nombreImages);
        var normales = new List<Vector3[]>(nombreImages);

        var minimum = Vector3.one * float.MaxValue;
        var maximum = Vector3.one * float.MinValue;

        for (var image = 0; image < nombreImages; image++)
        {
            var temps = clip.length * image / nombreImages;
            clip.SampleAnimation(objet, temps);
            peau.BakeMesh(maille);

            var sommets = maille.vertices;
            positions.Add(sommets);
            normales.Add(maille.normals);

            foreach (var sommet in sommets)
            {
                minimum = Vector3.Min(minimum, sommet);
                maximum = Vector3.Max(maximum, sommet);
            }
        }

        var nombreSommets = positions[0].Length;
        var etendue = maximum - minimum;
        etendue = new Vector3(
            Mathf.Max(etendue.x, 0.0001f),
            Mathf.Max(etendue.y, 0.0001f),
            Mathf.Max(etendue.z, 0.0001f));

        var texturePositions = CreerTexture(nombreSommets, nombreImages);
        var textureNormales = CreerTexture(nombreSommets, nombreImages);

        var pixelsPositions = new Color[nombreSommets * nombreImages];
        var pixelsNormales = new Color[nombreSommets * nombreImages];

        for (var image = 0; image < nombreImages; image++)
        {
            for (var sommet = 0; sommet < nombreSommets; sommet++)
            {
                var index = image * nombreSommets + sommet;

                var p = positions[image][sommet];
                var normalise = new Vector3(
                    (p.x - minimum.x) / etendue.x,
                    (p.y - minimum.y) / etendue.y,
                    (p.z - minimum.z) / etendue.z);
                pixelsPositions[index] = new Color(normalise.x, normalise.y, normalise.z, 1f);

                var n = normales[image][sommet];
                pixelsNormales[index] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
        }

        texturePositions.SetPixels(pixelsPositions);
        textureNormales.SetPixels(pixelsNormales);
        texturePositions.Apply(false, false);
        textureNormales.Apply(false, false);

        Directory.CreateDirectory(Dossier);
        var nom = objet.name;
        AssetDatabase.CreateAsset(texturePositions, $"{Dossier}/{nom}_positions.asset");
        AssetDatabase.CreateAsset(textureNormales, $"{Dossier}/{nom}_normales.asset");
        AssetDatabase.SaveAssets();

        Object.DestroyImmediate(maille);

        Debug.Log(
            $"VAT cuite : {nombreSommets} sommets x {nombreImages} images\n" +
            $"_NombreImages = {nombreImages}\n" +
            $"_BorneMin = ({minimum.x}, {minimum.y}, {minimum.z})\n" +
            $"_BorneMax = ({maximum.x}, {maximum.y}, {maximum.z})");
    }

    private static Texture2D CreerTexture(int largeur, int hauteur)
    {
        return new Texture2D(largeur, hauteur, TextureFormat.RGBAHalf, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
    }
}
