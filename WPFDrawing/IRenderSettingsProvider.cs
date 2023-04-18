using System;

namespace WPFDrawing {
    public interface IRenderSettingsProvider {
        RenderSettings CurrentRenderSettings { get; }
    }

    [Flags]
    public enum RenderSettings {
        None,
        XiaolinAlias,
    }
}
