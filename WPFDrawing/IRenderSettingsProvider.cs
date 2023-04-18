namespace WPFDrawing {
    public interface IRenderSettingsProvider {
        RenderSettings CurrentRenderSettings { get; }
    }

    public enum RenderSettings {
        XiaolinAlias = 1,
    }
}
