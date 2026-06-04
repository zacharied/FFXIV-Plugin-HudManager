using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using System;

namespace HUDManager.Ui;

public sealed class WindowManager : IDisposable {
    public Interface Settings { get; }
    public CustomConditions CustomConditions { get; }

    private readonly IUiBuilder uiBuilder;
    private readonly WindowSystem windowSystem;

    public WindowManager(Plugin plugin) {
        uiBuilder = plugin.Interface.UiBuilder;

        windowSystem = new WindowSystem("HUD Manager");

        Settings = new Interface(plugin);
        windowSystem.AddWindow(Settings);

        CustomConditions = new CustomConditions(plugin);
        windowSystem.AddWindow(CustomConditions);

        uiBuilder.Draw += windowSystem.Draw;
        uiBuilder.OpenConfigUi += Settings.Toggle;
    }

    public void Dispose() {
        uiBuilder.Draw -= windowSystem.Draw;
        uiBuilder.OpenConfigUi -= Settings.Toggle;
    }
}
