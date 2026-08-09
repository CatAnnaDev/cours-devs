using System.Collections.Generic;
using UnityEngine;

namespace Demos.Unity
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Demos/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Slime";
        [SerializeField] private int _maxHealth = 30;
        [SerializeField, Range(0f, 400f)] private float _speed = 60f;
        [SerializeField] private GameObject _visual;

        public string DisplayName => _displayName;

        public int MaxHealth => _maxHealth;

        public float Speed => _speed;

        public GameObject Visual => _visual;
    }

    public sealed class LootTable : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField] private List<string> _itemKeys = new List<string>();
        [SerializeField] private List<int> _itemCounts = new List<int>();

        [SerializeField] private string _label = "coffre";

        public int Version { get; set; } = 3;

        public readonly Dictionary<string, int> Items = new Dictionary<string, int>();

        public string Label => _label;

        public void OnBeforeSerialize()
        {
            _itemKeys.Clear();
            _itemCounts.Clear();

            foreach (KeyValuePair<string, int> entry in Items)
            {
                _itemKeys.Add(entry.Key);
                _itemCounts.Add(entry.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Items.Clear();

            int count = Mathf.Min(_itemKeys.Count, _itemCounts.Count);

            for (int i = 0; i < count; i++)
                Items[_itemKeys[i]] = _itemCounts[i];
        }
    }

    public sealed class SerializationRules : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition _definition;

        [SerializeField, Tooltip("un champ prive avec SerializeField : visible dans l'inspecteur, invisible du reste du code")]
        private int _wave = 1;

        [HideInInspector] public int RuntimeOnly;

        public int PublicField = 7;

        public int AutoProperty { get; set; } = 7;

        private void Start()
        {
            Debug.Log("Ce que Unity serialise :");
            Debug.Log("  oui : les champs publics, et les champs prives marques SerializeField");
            Debug.Log("  non : les proprietes, meme publiques, meme auto");
            Debug.Log("  non : readonly, static, const");
            Debug.Log("  non : Dictionary, HashSet, Queue, Stack");
            Debug.Log("  non : les interfaces et le polymorphisme (sauf SerializeReference)");
            Debug.Log("  non : les tableaux multidimensionnels, seulement les jagged via une classe intermediaire");

            Debug.Log($"PublicField vaut {PublicField} et sera sauvegarde");
            Debug.Log($"AutoProperty vaut {AutoProperty} et repartira TOUJOURS a 7, en silence");

            Debug.Log("La parade pour un dictionnaire : deux listes serialisees, remplies dans OnBeforeSerialize");
            Debug.Log("et relues dans OnAfterDeserialize. Les listes sont un cache, pas la verite.");

            if (_definition != null)
                Debug.Log($"le ScriptableObject partage : {_definition.DisplayName}, {_definition.MaxHealth} PV");

            Debug.Log("Un ScriptableObject est l'equivalent exact d'une Resource Godot : UNE fiche en memoire");
            Debug.Log("pour cinq cents ennemis. Et comme chez Godot, la modifier a l'execution modifie l'asset");
            Debug.Log("dans l'editeur : en jeu on lit la fiche, on n'ecrit jamais dedans.");
        }
    }
}
