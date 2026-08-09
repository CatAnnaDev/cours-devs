using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Csharplings.Unity;

public abstract class UnityObject
{
    private static readonly List<UnityObject> PendingDestruction = new();

    public string Name { get; set; } = "GameObject";

    public bool NativeAlive { get; private set; } = true;

    public static int PendingDestructionCount => PendingDestruction.Count;

    public static void Destroy(UnityObject target)
    {
        if (target is not null && target.NativeAlive && !PendingDestruction.Contains(target))
            PendingDestruction.Add(target);
    }

    public static void DestroyImmediate(UnityObject target) => target?.Kill();

    public static void FlushDestruction()
    {
        foreach (UnityObject target in PendingDestruction)
            target.Kill();

        PendingDestruction.Clear();
    }

    public static int ComparisonCount { get; set; }

    protected virtual void OnNativeDestroyed() { }

    private void Kill()
    {
        if (!NativeAlive)
            return;

        NativeAlive = false;
        OnNativeDestroyed();
    }

    public static bool operator ==(UnityObject left, UnityObject right) => LooksEqual(left, right);

    public static bool operator !=(UnityObject left, UnityObject right) => !LooksEqual(left, right);

    public override bool Equals(object other) => LooksEqual(this, other as UnityObject);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    public override string ToString() => NativeAlive ? Name : $"{Name} (detruit)";

    public string Describe()
    {
        if (!NativeAlive)
            throw new InvalidOperationException($"l'objet natif de {Name} a ete detruit");

        return Name;
    }

    private static bool LooksEqual(UnityObject left, UnityObject right)
    {
        ComparisonCount++;

        bool leftMissing = left is null || !left.NativeAlive;
        bool rightMissing = right is null || !right.NativeAlive;

        if (leftMissing || rightMissing)
            return leftMissing && rightMissing;

        return ReferenceEquals(left, right);
    }
}

public sealed class Material : UnityObject
{
    public Material(string shader)
    {
        Shader = shader;
        Created++;
    }

    public static int Created { get; private set; }

    public string Shader { get; }

    public float Alpha { get; set; } = 1f;

    public static void ResetCounter() => Created = 0;

    internal Material CloneForInstance() =>
        new Material(Shader) { Alpha = Alpha, Name = Name + " (Instance)" };
}

public sealed class MeshRenderer : UnityObject
{
    private Material _instance;

    public MeshRenderer(Material shared)
    {
        SharedMaterial = shared;
    }

    public Material SharedMaterial { get; }

    public bool HasOwnInstance => _instance is not null;

    public Material Material
    {
        get
        {
            _instance ??= SharedMaterial.CloneForInstance();

            return _instance;
        }
    }

    public Material Rendered => _instance ?? SharedMaterial;
}

public class Component : UnityObject
{
    public GameObject GameObject { get; internal set; }

    public T GetComponent<T>()
        where T : Component => GameObject?.GetComponent<T>();

    public bool TryGetComponent<T>(out T found)
        where T : Component
    {
        if (GameObject is not null)
            return GameObject.TryGetComponent(out found);

        found = null;

        return false;
    }
}

public sealed class GameObject : UnityObject
{
    private readonly List<Component> _components = new();

    public GameObject(string name = "GameObject")
    {
        Name = name;
    }

    public static int LookupCount { get; set; }

    public int ComponentCount => _components.Count;

    public T AddComponent<T>(T component)
        where T : Component
    {
        component.GameObject = this;
        _components.Add(component);

        return component;
    }

    public T GetComponent<T>()
        where T : Component
    {
        LookupCount++;

        for (int i = 0; i < _components.Count; i++)
        {
            if (_components[i] is T typed)
                return typed;
        }

        return null;
    }

    public bool TryGetComponent<T>(out T found)
        where T : Component
    {
        LookupCount++;

        for (int i = 0; i < _components.Count; i++)
        {
            if (_components[i] is T typed)
            {
                found = typed;

                return true;
            }
        }

        found = null;

        return false;
    }

    protected override void OnNativeDestroyed()
    {
        foreach (Component component in _components)
            DestroyImmediate(component);
    }
}

public abstract class MonoBehaviour : Component
{
    private static readonly Dictionary<Type, Declared> Cache = new();

    public static int EngineCallbacks { get; set; }

    public bool IsEnabled { get; private set; } = true;

    public bool HasStarted { get; private set; }

    public bool SurvivesSceneChange { get; private set; }

    public static void DontDestroyOnLoad(MonoBehaviour behaviour)
    {
        if (behaviour is not null)
            behaviour.SurvivesSceneChange = true;
    }

    public virtual void Awake() { }

    public virtual void OnEnable() { }

    public virtual void Start() { }

    public virtual void FixedUpdate() { }

    public virtual void Update() { }

    public virtual void LateUpdate() { }

    public virtual void OnDisable() { }

    public virtual void OnDestroy() { }

    private sealed class Declared
    {
        public bool Start;
        public bool FixedUpdate;
        public bool Update;
        public bool LateUpdate;
    }

    private Declared Wanted
    {
        get
        {
            Type type = GetType();

            if (Cache.TryGetValue(type, out Declared cached))
                return cached;

            var declared = new Declared
            {
                Start = Overrides(type, nameof(Start)),
                FixedUpdate = Overrides(type, nameof(FixedUpdate)),
                Update = Overrides(type, nameof(Update)),
                LateUpdate = Overrides(type, nameof(LateUpdate)),
            };

            Cache[type] = declared;

            return declared;
        }
    }

    private static bool Overrides(Type type, string name) =>
        type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.DeclaringType != typeof(MonoBehaviour);

    public void SetEnabled(bool enabled)
    {
        if (IsEnabled == enabled)
            return;

        IsEnabled = enabled;

        if (enabled)
            OnEnable();
        else
            OnDisable();
    }

    internal void EngineAwake()
    {
        Awake();
        OnEnable();
    }

    internal bool Runnable => NativeAlive && IsEnabled;

    internal void EngineFixedUpdate()
    {
        if (!Runnable || !Wanted.FixedUpdate)
            return;

        EngineCallbacks++;
        FixedUpdate();
    }

    internal void EngineStart()
    {
        if (!Runnable || HasStarted)
            return;

        HasStarted = true;

        if (Wanted.Start)
            EngineCallbacks++;

        Start();
    }

    internal void EngineUpdate()
    {
        if (!Runnable || !Wanted.Update)
            return;

        EngineCallbacks++;
        Update();
    }

    internal void EngineLateUpdate()
    {
        if (!Runnable || !Wanted.LateUpdate)
            return;

        EngineCallbacks++;
        LateUpdate();
    }
}

public sealed class AotException : Exception
{
    public AotException(string message) : base(message) { }
}

public static class Il2Cpp
{
    private static readonly HashSet<Type> Buildable = new();

    public static bool StripsUnreferencedCode { get; set; } = true;

    public static int ReflectionCalls { get; private set; }

    public static void Reset()
    {
        Buildable.Clear();
        StripsUnreferencedCode = true;
        ReflectionCalls = 0;
    }

    public static void Preserve<T>() => Buildable.Add(typeof(T));

    public static bool CanBuild(Type type) => Buildable.Contains(type);

    public static T Build<T>()
        where T : new()
    {
        Buildable.Add(typeof(T));

        return new T();
    }

    public static object BuildByReflection(Type type)
    {
        ReflectionCalls++;

        if (StripsUnreferencedCode && !Buildable.Contains(type))
            throw new AotException(
                $"{type.Name} : aucune ligne de code ne l'instancie, le compilateur n'a pas genere son constructeur");

        return Activator.CreateInstance(type);
    }
}

public sealed class Transform : Component
{
    private Vector2 _position;
    private float _rotation;

    public static int Crossings { get; set; }

    public Vector2 Position
    {
        get
        {
            Crossings++;

            return _position;
        }

        set
        {
            Crossings++;
            _position = value;
        }
    }

    public float Rotation
    {
        get
        {
            Crossings++;

            return _rotation;
        }

        set
        {
            Crossings++;
            _rotation = value;
        }
    }

    public void SetPositionAndRotation(Vector2 position, float rotation)
    {
        Crossings++;
        _position = position;
        _rotation = rotation;
    }

    public static void ResetCounter() => Crossings = 0;
}

public abstract class ScriptableObject : UnityObject
{
    public static int LoadedFromDisk { get; private set; }

    public static void ResetCounter() => LoadedFromDisk = 0;

    internal static T LoadAsset<T>(T asset)
        where T : ScriptableObject
    {
        LoadedFromDisk++;

        return asset;
    }
}

public sealed class AssetDatabase
{
    private readonly Dictionary<string, ScriptableObject> _assets = new(StringComparer.Ordinal);

    public int Count => _assets.Count;

    public T Register<T>(string path, T asset)
        where T : ScriptableObject
    {
        asset.Name = path;
        _assets[path] = asset;

        return asset;
    }

    public T Load<T>(string path)
        where T : ScriptableObject =>
        _assets.TryGetValue(path, out ScriptableObject found) ? ScriptableObject.LoadAsset((T)found) : null;

    public bool Contains(string path) => _assets.ContainsKey(path);
}

public sealed class Rigidbody : Component
{
    private Vector2 _pending;
    private bool _hasPending;

    public static int Teleports { get; set; }

    public Vector2 Velocity { get; set; }

    public Vector2 Position { get; private set; }

    public Vector2 PreviousPosition { get; private set; }

    public bool Interpolate { get; set; }

    public int Steps { get; private set; }

    public void MovePosition(Vector2 target)
    {
        _pending = target;
        _hasPending = true;
    }

    public void Teleport(Vector2 target)
    {
        Teleports++;
        PreviousPosition = target;
        Position = target;
        _hasPending = false;
    }

    public Vector2 Rendered(float alpha) =>
        Interpolate ? PreviousPosition.Lerp(Position, Mathf.Clamp(alpha, 0f, 1f)) : Position;

    internal void Step(float delta)
    {
        Steps++;
        PreviousPosition = Position;

        if (_hasPending)
        {
            Position = _pending;
            _hasPending = false;

            return;
        }

        Position += Velocity * delta;
    }

    internal static readonly List<Rigidbody> Live = new();

    public static Rigidbody Create()
    {
        var body = new Rigidbody();

        Live.Add(body);

        return body;
    }

    public static void Clear()
    {
        Live.Clear();
        Teleports = 0;
    }

    internal static void StepAll(float delta)
    {
        for (int i = 0; i < Live.Count; i++)
            Live[i].Step(delta);
    }
}

public sealed class CanvasElement
{
    internal CanvasElement(Canvas owner, string text)
    {
        Owner = owner;
        _text = text;
    }

    private string _text;

    public Canvas Owner { get; }

    public string Text
    {
        get => _text;

        set
        {
            if (string.Equals(_text, value, StringComparison.Ordinal))
                return;

            _text = value;
            Owner.MarkDirty();
        }
    }
}

public sealed class Canvas
{
    private readonly List<CanvasElement> _elements = new();

    internal static readonly List<Canvas> Live = new();

    public Canvas()
    {
        Live.Add(this);
    }

    public static int Rebuilds { get; private set; }

    public static int RebuiltElements { get; private set; }

    public bool Dirty { get; private set; }

    public int ElementCount => _elements.Count;

    public CanvasElement Add(string text)
    {
        var element = new CanvasElement(this, text);

        _elements.Add(element);
        MarkDirty();

        return element;
    }

    public void MarkDirty() => Dirty = true;

    public static void Clear()
    {
        Live.Clear();
        Rebuilds = 0;
        RebuiltElements = 0;
    }

    internal static void RebuildAll()
    {
        for (int i = 0; i < Live.Count; i++)
        {
            Canvas canvas = Live[i];

            if (!canvas.Dirty)
                continue;

            Rebuilds++;
            RebuiltElements += canvas._elements.Count;
            canvas.Dirty = false;
        }
    }
}

public sealed class AssetHandle
{
    internal AssetHandle(string path, int bytes)
    {
        Path = path;
        Bytes = bytes;
    }

    public string Path { get; }

    public int Bytes { get; }

    public bool Released { get; private set; }

    internal void Release() => Released = true;
}

public static class Addressables
{
    private static readonly Dictionary<string, int> References = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> Sizes = new(StringComparer.Ordinal);

    public static int LiveBytes { get; private set; }

    public static int LoadCalls { get; private set; }

    public static int ReferenceCountOf(string path) => References.GetValueOrDefault(path);

    public static void Reset()
    {
        References.Clear();
        Sizes.Clear();
        LiveBytes = 0;
        LoadCalls = 0;
    }

    public static AssetHandle Load(string path, int bytes)
    {
        LoadCalls++;
        Sizes[path] = bytes;

        int count = References.GetValueOrDefault(path);

        References[path] = count + 1;

        if (count == 0)
            LiveBytes += bytes;

        return new AssetHandle(path, bytes);
    }

    public static void Release(AssetHandle handle)
    {
        if (handle is null || handle.Released)
            return;

        int count = References.GetValueOrDefault(handle.Path);

        if (count == 0)
            return;

        handle.Release();
        References[handle.Path] = count - 1;

        if (count == 1)
            LiveBytes -= Sizes[handle.Path];
    }
}

public static class Time
{
    private static float _rawDelta;
    private static float _scaledDelta;

    public static float TimeScale { get; set; } = 1f;

    public static float MaximumDeltaTime { get; set; } = 1f / 3f;

    public static float FixedDeltaTime { get; set; } = 0.02f;

    public static bool InFixedUpdate { get; internal set; }

    public static float DeltaTime => InFixedUpdate ? FixedDeltaTime : _scaledDelta;

    public static float UnscaledDeltaTime => InFixedUpdate ? FixedDeltaTime : _rawDelta;

    public static float ScaledDelta => _scaledDelta;

    public static float TimeSinceStart { get; private set; }

    public static float UnscaledTimeSinceStart { get; private set; }

    public static float RealtimeSinceStartup { get; private set; }

    public static int FrameCount { get; private set; }

    public static void Reset()
    {
        TimeScale = 1f;
        MaximumDeltaTime = 1f / 3f;
        FixedDeltaTime = 0.02f;
        InFixedUpdate = false;
        _rawDelta = 0f;
        _scaledDelta = 0f;
        TimeSinceStart = 0f;
        UnscaledTimeSinceStart = 0f;
        RealtimeSinceStartup = 0f;
        FrameCount = 0;
    }

    internal static void BeginFrame(float rawDelta)
    {
        FrameCount++;
        RealtimeSinceStartup += rawDelta;

        float clamped = rawDelta < MaximumDeltaTime ? rawDelta : MaximumDeltaTime;

        _rawDelta = clamped;
        _scaledDelta = clamped * TimeScale;

        UnscaledTimeSinceStart += clamped;
        TimeSinceStart += _scaledDelta;
    }
}

public sealed class Scene
{
    private readonly List<MonoBehaviour> _behaviours = new();

    public int BehaviourCount => _behaviours.Count;

    public T Add<T>(T behaviour)
        where T : MonoBehaviour
    {
        _behaviours.Add(behaviour);
        behaviour.EngineAwake();

        return behaviour;
    }

    private double _accumulator;

    public int FixedStepsLastFrame { get; private set; }

    public void Frame(double delta = 1.0 / 60.0)
    {
        Time.BeginFrame((float)delta);

        _accumulator += Time.ScaledDelta;
        FixedStepsLastFrame = 0;

        Time.InFixedUpdate = true;

        while (_accumulator >= Time.FixedDeltaTime)
        {
            _accumulator -= Time.FixedDeltaTime;
            FixedStepsLastFrame++;

            for (int i = 0; i < _behaviours.Count; i++)
                _behaviours[i].EngineFixedUpdate();

            Rigidbody.StepAll(Time.FixedDeltaTime);
        }

        Time.InFixedUpdate = false;

        for (int i = 0; i < _behaviours.Count; i++)
            _behaviours[i].EngineStart();

        for (int i = 0; i < _behaviours.Count; i++)
            _behaviours[i].EngineUpdate();

        for (int i = 0; i < _behaviours.Count; i++)
            _behaviours[i].EngineLateUpdate();

        Canvas.RebuildAll();
        UnityObject.FlushDestruction();
        Sweep();
    }

    public void Unload()
    {
        for (int i = 0; i < _behaviours.Count; i++)
        {
            if (!_behaviours[i].SurvivesSceneChange)
                UnityObject.DestroyImmediate(_behaviours[i]);
        }

        Sweep();
    }

    private void Sweep()
    {
        for (int i = _behaviours.Count - 1; i >= 0; i--)
        {
            MonoBehaviour behaviour = _behaviours[i];

            if (behaviour.NativeAlive)
                continue;

            if (behaviour.IsEnabled)
                behaviour.OnDisable();

            behaviour.OnDestroy();
            _behaviours.RemoveAt(i);
        }
    }

    public void Frames(int count, double delta = 1.0 / 60.0)
    {
        for (int frame = 0; frame < count; frame++)
            Frame(delta);
    }
}

public sealed class WaitForSeconds
{
    public WaitForSeconds(float seconds)
    {
        Seconds = seconds;
        Created++;
    }

    public static int Created { get; private set; }

    public float Seconds { get; }

    public static void ResetCounter() => Created = 0;
}

public sealed class CoroutineRunner
{
    private readonly List<Routine> _routines = new();

    public int RunningCount => _routines.Count;

    public void Start(IEnumerator body) => _routines.Add(new Routine(body));

    public void Frame(float delta)
    {
        for (int i = _routines.Count - 1; i >= 0; i--)
        {
            if (!_routines[i].Advance(delta))
                _routines.RemoveAt(i);
        }
    }

    private sealed class Routine
    {
        private readonly IEnumerator _body;
        private float _waiting;

        public Routine(IEnumerator body)
        {
            _body = body;
        }

        public bool Advance(float delta)
        {
            if (_waiting > 0f)
            {
                _waiting -= delta;

                if (_waiting > 0f)
                    return true;
            }

            if (!_body.MoveNext())
                return false;

            _waiting = _body.Current is WaitForSeconds wait ? wait.Seconds : 0f;

            return true;
        }
    }
}

public interface ISerializationCallbackReceiver
{
    void OnBeforeSerialize();

    void OnAfterDeserialize();
}

public static class UnitySerializer
{
    public static bool CanSerialize(Type type) =>
        type == typeof(int)
        || type == typeof(float)
        || type == typeof(bool)
        || type == typeof(string)
        || type == typeof(List<int>)
        || type == typeof(List<string>);

    public static Dictionary<string, string> Save(object target)
    {
        if (target is ISerializationCallbackReceiver receiver)
            receiver.OnBeforeSerialize();

        var asset = new Dictionary<string, string>();

        foreach (FieldInfo field in SerializableFields(target))
            asset[field.Name] = Write(field.GetValue(target));

        return asset;
    }

    public static void Load(object target, Dictionary<string, string> asset)
    {
        foreach (FieldInfo field in SerializableFields(target))
        {
            if (asset.TryGetValue(field.Name, out string stored))
                field.SetValue(target, Read(field.FieldType, stored));
        }

        if (target is ISerializationCallbackReceiver receiver)
            receiver.OnAfterDeserialize();
    }

    private static IEnumerable<FieldInfo> SerializableFields(object target) =>
        target.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(field => CanSerialize(field.FieldType));

    private static string Write(object value) =>
        value switch
        {
            null => string.Empty,
            List<int> integers => string.Join(",", integers),
            List<string> texts => string.Join(",", texts),
            bool flag => flag ? "1" : "0",
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
        };

    private static object Read(Type type, string stored)
    {
        if (type == typeof(int))
            return int.TryParse(stored, out int value) ? value : 0;

        if (type == typeof(float))
            return float.TryParse(stored, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float value) ? value : 0f;

        if (type == typeof(bool))
            return stored == "1";

        if (type == typeof(string))
            return stored;

        if (type == typeof(List<int>))
            return Split(stored).Select(int.Parse).ToList();

        return Split(stored).ToList();
    }

    private static IEnumerable<string> Split(string stored) =>
        stored.Length == 0 ? Array.Empty<string>() : stored.Split(',');
}
