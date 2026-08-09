using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Neige : MonoBehaviour
{
    private const int MaximumPresseurs = 16;

    [SerializeField] private Renderer terrain;
    [SerializeField] private Shader shaderEmpreinte;
    [SerializeField] private List<Transform> presseurs = new List<Transform>();
    [SerializeField] private int resolution = 512;
    [SerializeField] private Vector2 centreZone = Vector2.zero;
    [SerializeField] private Vector2 tailleZone = new Vector2(20f, 20f);
    [SerializeField] private float rayonPresseur = 0.35f;
    [SerializeField] private float forcePresseur = 1f;
    [SerializeField] private float persistanceParSeconde = 0.15f;
    [SerializeField] private float durete = 0.6f;

    private static readonly int IdPresseurs = Shader.PropertyToID("_Presseurs");
    private static readonly int IdNombrePresseurs = Shader.PropertyToID("_NombrePresseurs");
    private static readonly int IdZone = Shader.PropertyToID("_Zone");
    private static readonly int IdPersistance = Shader.PropertyToID("_Persistance");
    private static readonly int IdDurete = Shader.PropertyToID("_Durete");
    private static readonly int IdDeformation = Shader.PropertyToID("_Deformation");

    private RenderTexture _courante;
    private RenderTexture _precedente;
    private Material _materiauEmpreinte;
    private readonly Vector4[] _tampon = new Vector4[MaximumPresseurs];

    private void OnEnable()
    {
        _materiauEmpreinte = new Material(shaderEmpreinte) { hideFlags = HideFlags.DontSave };
        _courante = CreerCible();
        _precedente = CreerCible();

        var noire = RenderTexture.active;
        RenderTexture.active = _courante;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = _precedente;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = noire;
    }

    private void OnDisable()
    {
        if (_courante != null) _courante.Release();
        if (_precedente != null) _precedente.Release();
        if (_materiauEmpreinte != null) DestroyImmediate(_materiauEmpreinte);
    }

    private RenderTexture CreerCible()
    {
        var cible = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RHalf)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        cible.Create();
        return cible;
    }

    private void LateUpdate()
    {
        var actifs = 0;
        foreach (var presseur in presseurs)
        {
            if (actifs >= MaximumPresseurs || presseur == null) continue;
            var monde = presseur.position;
            _tampon[actifs] = new Vector4(monde.x, monde.z, rayonPresseur, forcePresseur);
            actifs++;
        }

        var zone = new Vector4(centreZone.x, centreZone.y, tailleZone.x, tailleZone.y);
        var persistance = Mathf.Pow(persistanceParSeconde, Time.deltaTime);

        _materiauEmpreinte.SetVectorArray(IdPresseurs, _tampon);
        _materiauEmpreinte.SetInt(IdNombrePresseurs, actifs);
        _materiauEmpreinte.SetVector(IdZone, zone);
        _materiauEmpreinte.SetFloat(IdPersistance, persistance);
        _materiauEmpreinte.SetFloat(IdDurete, durete);

        Graphics.Blit(_precedente, _courante, _materiauEmpreinte);
        (_courante, _precedente) = (_precedente, _courante);

        var materiauTerrain = Application.isPlaying ? terrain.material : terrain.sharedMaterial;
        materiauTerrain.SetTexture(IdDeformation, _precedente);
        materiauTerrain.SetVector(IdZone, zone);
    }
}
