using UnityEngine;

namespace Demos.Unity
{
    public sealed class MonoBehaviourLifecycle : MonoBehaviour
    {
        [SerializeField] private int _framesBeforeDisabling = 120;

        private int _frames;

        private void Awake()
        {
            Debug.Log("1. Awake : l'objet existe, ses champs serialises sont remplis. Les AUTRES objets ne sont pas forcement prets.");
        }

        private void OnEnable()
        {
            Debug.Log("2. OnEnable : a chaque activation, pas seulement au demarrage. C'est ICI qu'on s'abonne.");
        }

        private void Start()
        {
            Debug.Log("3. Start : tous les Awake de la scene sont passes. C'est ici qu'on cherche les autres objets.");
        }

        private void FixedUpdate()
        {
            if (_frames == 0)
                Debug.Log("4. FixedUpdate : pas fixe, independant du framerate. Physique, forces, Rigidbody.");
        }

        private void Update()
        {
            _frames++;

            if (_frames == 1)
                Debug.Log("5. Update : une fois par image affichee. Input, timers, interface.");

            if (_frames == _framesBeforeDisabling)
                enabled = false;
        }

        private void LateUpdate()
        {
            if (_frames == 1)
                Debug.Log("6. LateUpdate : apres tous les Update. La camera qui suit sa cible va ICI.");
        }

        private void OnDisable()
        {
            Debug.Log("7. OnDisable : desactivation OU destruction. C'est ici qu'on se desabonne.");
        }

        private void OnDestroy()
        {
            Debug.Log("8. OnDestroy : la fin. Detruire ici les materiaux clones et les textures crees a la main.");
        }

        private void OnValidate()
        {
            _framesBeforeDisabling = Mathf.Max(_framesBeforeDisabling, 1);
        }
    }

    public sealed class LifecycleOrderNotes : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("Ordre entre objets : tous les Awake, puis tous les OnEnable, puis tous les Start.");
            Debug.Log("Donc chercher un autre objet dans Awake est un pari ; dans Start, non.");
            Debug.Log("Et l'ordre entre deux Awake n'est PAS defini : si tu en depends, utilise Script Execution Order.");
            Debug.Log("Equivalents Godot : Awake vaut _EnterTree, Start vaut _Ready, OnDestroy vaut _ExitTree.");
            Debug.Log("Difference qui compte : Godot n'a pas de LateUpdate, il a ProcessPriority.");
        }
    }
}
