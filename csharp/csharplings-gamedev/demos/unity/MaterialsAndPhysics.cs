using UnityEngine;

namespace Demos.Unity
{
    public sealed class TintingWithoutLeaking : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _tint = Color.red;

        private MaterialPropertyBlock _block;
        private Material _ownedClone;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
        }

        private void TheWrongWay()
        {
            _renderer.material.SetColor(BaseColorId, _tint);

            Debug.Log("lire '.material' CLONE le materiau. Une propriete, pas une methode, et elle alloue.");
            Debug.Log("Cent ennemis teintes comme ca, cent materiaux, et cent batches de rendu au lieu d'un.");
        }

        private void TheSharedWay()
        {
            _renderer.sharedMaterial.SetColor(BaseColorId, _tint);

            Debug.Log("'.sharedMaterial' ne clone pas, mais il teint TOUS ceux qui partagent l'asset,");
            Debug.Log("et en editeur il modifie le fichier sur le disque. A reserver aux changements globaux.");
        }

        private void TheRightWay()
        {
            _block.Clear();
            _block.SetColor(BaseColorId, _tint);
            _renderer.SetPropertyBlock(_block);

            Debug.Log("MaterialPropertyBlock : une couleur par instance, aucun materiau clone,");
            Debug.Log("et le rendu reste groupe. C'est la reponse, et presque personne ne la connait.");
        }

        private void IfYouReallyMustClone()
        {
            _ownedClone = new Material(_renderer.sharedMaterial);
            _renderer.material = _ownedClone;
        }

        private void OnDestroy()
        {
            if (_ownedClone != null)
                Destroy(_ownedClone);

            Debug.Log("un materiau est un objet natif : le ramasse-miettes ne le prendra jamais.");
            Debug.Log("Tout clone doit etre detruit a la main, ici, sinon la memoire ne redescend plus.");
        }
    }

    public sealed class NonAllocQueries : MonoBehaviour
    {
        [SerializeField] private float _range = 10f;
        [SerializeField] private LayerMask _enemyLayers;

        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly Collider[] _overlaps = new Collider[32];

        private int _lastHitCount;

        private void FixedUpdate()
        {
            ShootWithoutAllocating();
            SweepWithoutAllocating();
        }

        private void ShootWithoutAllocating()
        {
            var ray = new Ray(transform.position, transform.forward);

            _lastHitCount = Physics.RaycastNonAlloc(ray, _hits, _range, _enemyLayers);

            if (_lastHitCount == _hits.Length)
                Debug.LogWarning("tampon plein : des impacts ont ete perdus EN SILENCE. Agrandis-le ou trie autrement.");

            for (int i = 0; i < _lastHitCount; i++)
            {
                if (_hits[i].collider.TryGetComponent(out Rigidbody body))
                    body.AddForce(ray.direction * 5f, ForceMode.Impulse);
            }
        }

        private void SweepWithoutAllocating()
        {
            int found = Physics.OverlapSphereNonAlloc(transform.position, _range, _overlaps, _enemyLayers);

            for (int i = 0; i < found; i++)
            {
                Collider candidate = _overlaps[i];

                if ((candidate.transform.position - transform.position).sqrMagnitude > _range * _range)
                    continue;

                Debug.Log($"contact confirme : {candidate.name}");
            }
        }

        private void WhatTheAllocatingVersionsCost()
        {
            RaycastHit[] fresh = Physics.RaycastAll(new Ray(transform.position, transform.forward), _range);
            Collider[] around = Physics.OverlapSphere(transform.position, _range, _enemyLayers);

            Debug.Log($"RaycastAll rend un TABLEAU NEUF ({fresh.Length} entrees), OverlapSphere aussi ({around.Length}).");
            Debug.Log("Soixante fois par seconde, ce sont cent vingt tableaux jetes par seconde et par tireur.");
            Debug.Log("Les versions NonAlloc prennent le tampon de l'appelant et rendent un compte.");
            Debug.Log("Regle de la phase grossiere : elle peut proposer trop, jamais oublier. D'ou le second");
            Debug.Log("test precis dans la boucle, et le masque de couches qui filtre AVANT tout le reste.");
            Debug.Log("Chez Godot, IntersectRay rend un Dictionary : meme probleme, meme parade.");
        }
    }
}
