using UnityEngine;

namespace Demos.Unity
{
    public sealed class DestroyedObjects : MonoBehaviour
    {
        [SerializeField] private GameObject _target;

        private GameObject _spawned;

        private void Start()
        {
            _spawned = new GameObject("Cible");

            Debug.Log($"vivant : == null donne {_spawned == null}, is null donne {_spawned is null}");

            Destroy(_spawned);

            Debug.Log("Destroy ne detruit pas tout de suite : l'objet vit jusqu'a la fin de la frame,");
            Debug.Log($"et il recoit encore ses callbacks. Pour l'instant == null donne {_spawned == null}");
        }

        private void OnEnable()
        {
            Invoke(nameof(ReportAfterFrame), 0f);
        }

        private void ReportAfterFrame()
        {
            Debug.Log($"apres la frame : == null donne {_spawned == null}");
            Debug.Log($"                 is null  donne {_spawned is null}   <-- FALSE, la reference existe toujours");
            Debug.Log("Unity surcharge l'operateur == pour rendre true quand l'objet natif est parti.");
            Debug.Log("Le motif 'is null' ne passe PAS par l'operateur, donc il ne voit rien.");
            Debug.Log("Et le '?.' non plus : il teste la reference. Sur un objet detruit il appelle quand meme,");
            Debug.Log("et tu recoltes une MissingReferenceException.");
        }

        public bool SafeCheck() => _target != null;

        public bool BrokenCheck() => _target is not null;

        public string BrokenAccess() => _target?.name;

        public string SafeAccess() => _target != null ? _target.name : "aucune cible";

        private void CacheTheAnswerWhenItMatters()
        {
            bool alive = _target != null;

            for (int i = 0; i < 1000; i++)
            {
                if (alive)
                    _target.transform.position += Vector3.up * Time.deltaTime;
            }

            Debug.Log("l'operateur == coute un appel natif : dans une boucle chaude, on le teste UNE fois.");
        }

        private void ImmediateVersion()
        {
            var doomed = new GameObject("Immediat");

            DestroyImmediate(doomed);

            Debug.Log($"DestroyImmediate tue sur place : == null donne deja {doomed == null}");
            Debug.Log("A ne PAS utiliser en jeu : reserve a l'editeur et aux outils.");
        }

        private void ComparedToGodot()
        {
            Debug.Log("Chez Godot le piege est INVERSE : un noeud libere n'est pas null,");
            Debug.Log("le test null ment par omission, et il faut IsInstanceValid(node).");
            Debug.Log("Meme cause dans les deux moteurs : un objet manage et un objet natif,");
            Debug.Log("qui ne meurent pas ensemble.");
        }
    }
}
