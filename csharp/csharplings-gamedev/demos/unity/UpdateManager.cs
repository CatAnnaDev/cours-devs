using System.Collections.Generic;
using UnityEngine;

namespace Demos.Unity
{
    public interface ITicking
    {
        void Tick(float delta);
    }

    public sealed class SlowEnemy : MonoBehaviour
    {
        private float _health = 100f;

        private void Update()
        {
            _health -= Time.deltaTime;
        }
    }

    public sealed class FastEnemy : ITicking
    {
        private float _health = 100f;

        public void Tick(float delta)
        {
            _health -= delta;
        }
    }

    public sealed class TickManager : MonoBehaviour
    {
        private static TickManager _instance;

        private readonly List<ITicking> _ticking = new List<ITicking>(1024);
        private readonly List<ITicking> _pendingAdds = new List<ITicking>();
        private readonly List<ITicking> _pendingRemoves = new List<ITicking>();

        public static TickManager Instance => _instance;

        public int Count => _ticking.Count;

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public void Register(ITicking target) => _pendingAdds.Add(target);

        public void Unregister(ITicking target) => _pendingRemoves.Add(target);

        private void Update()
        {
            Flush();

            float delta = Time.deltaTime;

            for (int i = 0; i < _ticking.Count; i++)
                _ticking[i].Tick(delta);
        }

        private void Flush()
        {
            for (int i = 0; i < _pendingRemoves.Count; i++)
            {
                int index = _ticking.IndexOf(_pendingRemoves[i]);

                if (index < 0)
                    continue;

                _ticking[index] = _ticking[_ticking.Count - 1];
                _ticking.RemoveAt(_ticking.Count - 1);
            }

            _pendingRemoves.Clear();

            for (int i = 0; i < _pendingAdds.Count; i++)
                _ticking.Add(_pendingAdds[i]);

            _pendingAdds.Clear();
        }
    }

    public sealed class UpdateTaxNotes : MonoBehaviour
    {
        [SerializeField] private int _count = 1000;

        private void Start()
        {
            Debug.Log("Chaque Update() est un appel du moteur vers ton code, et il traverse la frontiere");
            Debug.Log("natif vers manage. Mille MonoBehaviour, c'est mille traversees par frame.");
            Debug.Log($"Avec un manager : UNE traversee, puis une boucle sur {_count} objets C# ordinaires.");
            Debug.Log("Le meme travail, mille fois moins d'allers-retours.");
            Debug.Log("Trois details qui comptent dans le manager :");
            Debug.Log("  1. les ajouts et retraits passent par des files, jamais pendant l'iteration");
            Debug.Log("  2. le retrait se fait par echange avec le dernier : temps constant");
            Debug.Log("  3. Time.deltaTime est lu UNE fois : c'est aussi un appel natif");
            Debug.Log("Version pauvre de la meme optimisation : desactiver un script suffit a");
            Debug.Log("supprimer son appel. Un script desactive ne coute rien.");
            Debug.Log("Chez Godot le probleme existe aussi, mais on le resout avec set_process(false)");
            Debug.Log("ou en groupant dans un seul _Process parent.");
        }
    }
}
