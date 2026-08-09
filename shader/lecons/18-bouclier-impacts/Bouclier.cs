using UnityEngine;

public class Bouclier : MonoBehaviour
{
    private const int MaximumImpacts = 8;

    private static readonly int IdImpacts = Shader.PropertyToID("_Impacts");
    private static readonly int IdNombreImpacts = Shader.PropertyToID("_NombreImpacts");

    private readonly Vector4[] _impacts = new Vector4[MaximumImpacts];
    private Material _materiau;
    private int _prochain;

    private void Awake()
    {
        _materiau = GetComponent<Renderer>().material;

        for (var i = 0; i < MaximumImpacts; i++)
        {
            _impacts[i] = new Vector4(0f, 0f, 0f, -1000f);
        }

        _materiau.SetVectorArray(IdImpacts, _impacts);
        _materiau.SetInt(IdNombreImpacts, MaximumImpacts);
    }

    public void Encaisser(Vector3 pointMonde)
    {
        var local = transform.InverseTransformPoint(pointMonde);
        _impacts[_prochain] = new Vector4(local.x, local.y, local.z, Time.timeSinceLevelLoad);
        _prochain = (_prochain + 1) % MaximumImpacts;
        _materiau.SetVectorArray(IdImpacts, _impacts);
    }
}
