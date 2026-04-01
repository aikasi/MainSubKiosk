using UnityEngine;
using UnityEngine.UI;

public class FixedSizeScrollbar : Scrollbar
{
    [Range(0.01f, 1f)]
    [SerializeField] private float fixedSize = 0.1f;

    public new float size
    {
        get => fixedSize;
        set => fixedSize = Mathf.Clamp01(value);
    }

    protected override void Start()
    {
        base.Start();
        base.size = fixedSize;
    }

    public override void Rebuild(CanvasUpdate executing)
    {
        base.Rebuild(executing);
        if (Application.isPlaying && base.size != fixedSize)
        {
            base.size = fixedSize;
        }
    }
}
