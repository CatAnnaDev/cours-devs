using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

public static class VerifShaders
{
    public static void Tout()
    {
        var guids = AssetDatabase.FindAssets("t:Shader", new[] { "Assets/Shaders" });
        var total = 0;
        var casses = 0;

        foreach (var guid in guids.OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
        {
            var chemin = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.ImportAsset(chemin, ImportAssetOptions.ForceUpdate);

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(chemin);
            total++;

            if (shader == null)
            {
                Debug.LogError("VERIF | chargement impossible | " + chemin);
                casses++;
                continue;
            }

            ShaderMessage[] messages = ShaderUtil.GetShaderMessages(shader);
            var erreurs = messages.Where(m => m.severity == ShaderCompilerMessageSeverity.Error).ToArray();
            var nom = System.IO.Path.GetFileName(chemin);

            if (erreurs.Length == 0)
            {
                Debug.Log("VERIF | OK | " + nom + " | avertissements: " + (messages.Length - erreurs.Length));
            }
            else
            {
                casses++;
                foreach (var m in erreurs)
                {
                    Debug.LogError("VERIF | ERREUR | " + nom + " | ligne " + m.line + " | " + m.message + " | " + m.messageDetails);
                }
            }
        }

        Debug.Log("VERIF | BILAN | " + total + " shaders, " + casses + " en erreur");
        EditorApplication.Exit(casses == 0 ? 0 : 1);
    }
}
