using System;
using Godot;

namespace Demos.WeakRefs;

public sealed class HeavyTexturePayload
{
    public string Name { get; }
    public byte[] Pixels { get; }

    public HeavyTexturePayload(string name, int byteSize)
    {
        Name = name;
        Pixels = new byte[byteSize];
    }
}

public partial class WeakReferenceBasics : Node
{
    private WeakReference<HeavyTexturePayload> _weak;
    private HeavyTexturePayload _strong;

    public override void _Ready()
    {
        _strong = new HeavyTexturePayload("atlas", 8 * 1024 * 1024);
        _weak = new WeakReference<HeavyTexturePayload>(_strong);

        Report("juste apres creation");

        _strong = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Report("apres avoir lache la reference forte");
    }

    private void Report(string step)
    {
        if (_weak.TryGetTarget(out HeavyTexturePayload payload))
            GD.Print($"[{step}] vivant : {payload.Name}");
        else
            GD.Print($"[{step}] collecte");
    }
}
