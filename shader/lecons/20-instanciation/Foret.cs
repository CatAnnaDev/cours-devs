using UnityEngine;

public class Foret : MonoBehaviour
{
    private const int TailleLot = 1023;

    [SerializeField] private Mesh maille;
    [SerializeField] private Material materiau;
    [SerializeField] private int nombre = 2000;
    [SerializeField] private float rayonZone = 25f;
    [SerializeField] private int graineAleatoire = 12345;

    private static readonly int IdVariation = Shader.PropertyToID("_Variation");

    private Matrix4x4[][] _lots;
    private MaterialPropertyBlock[] _blocs;
    private Bounds _limites;

    private void OnEnable()
    {
        var hasard = new System.Random(graineAleatoire);
        var nombreLots = Mathf.CeilToInt(nombre / (float)TailleLot);

        _lots = new Matrix4x4[nombreLots][];
        _blocs = new MaterialPropertyBlock[nombreLots];
        _limites = new Bounds(transform.position, new Vector3(rayonZone * 2f, 10f, rayonZone * 2f));

        var restant = nombre;
        for (var lot = 0; lot < nombreLots; lot++)
        {
            var taille = Mathf.Min(TailleLot, restant);
            restant -= taille;

            var reperes = new Matrix4x4[taille];
            var variations = new Vector4[taille];

            for (var i = 0; i < taille; i++)
            {
                var angle = (float)hasard.NextDouble() * Mathf.PI * 2f;
                var distance = Mathf.Sqrt((float)hasard.NextDouble()) * rayonZone;
                var position = transform.position + new Vector3(
                    Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);

                var rotation = Quaternion.Euler(0f, (float)hasard.NextDouble() * 360f, 0f);
                reperes[i] = Matrix4x4.TRS(position, rotation, Vector3.one);

                variations[i] = new Vector4(
                    (float)hasard.NextDouble(),
                    (float)hasard.NextDouble(),
                    (float)hasard.NextDouble(),
                    1f);
            }

            var bloc = new MaterialPropertyBlock();
            bloc.SetVectorArray(IdVariation, variations);

            _lots[lot] = reperes;
            _blocs[lot] = bloc;
        }
    }

    private void Update()
    {
        for (var lot = 0; lot < _lots.Length; lot++)
        {
            var parametres = new RenderParams(materiau)
            {
                worldBounds = _limites,
                matProps = _blocs[lot],
                shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                receiveShadows = true
            };

            Graphics.RenderMeshInstanced(parametres, maille, 0, _lots[lot]);
        }
    }
}
