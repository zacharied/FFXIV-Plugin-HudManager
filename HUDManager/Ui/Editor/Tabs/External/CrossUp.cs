using System;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using HUD_Manager;
using HUD_Manager.Configuration;
using HUD_Manager.Ui;
using HUDManager.Structs.External;
using ImGuiNET;
using System.Linq;
using static Dalamud.Interface.FontAwesomeIcon;
using static HUDManager.Structs.External.CrossUpConfig;

namespace HUDManager.Ui.Editor.Tabs;
internal partial class ExternalElements
{
    public sealed class CrossUp : IExternalElement
    {
        public static Plugin Plugin = null!;

        public CrossUp(Plugin plugin)
        {
            Plugin = plugin;
        }

        public bool Available()
        {
            try
            {
                return Plugin.Interface.InstalledPlugins.Any(state => state is { Name: "CrossUp", IsLoaded: true }) && Plugin.Interface.GetIpcSubscriber<bool>("CrossUp.Available").InvokeFunc();
            }
            catch
            {
                return false;
            }
        }
        public void AddButtonToList(SavedLayout layout, ref bool update, bool avail)
        {
            var exists = layout.CrossUpConfig != null;
            var icon = exists ? TrashAlt : Plus;
            var label = $"uimanager-{(exists ? "remove" : "add")}-crossup";

            if (ImGuiExt.IconButton(icon, label))
            {
                layout.CrossUpConfig = exists ? null : new();
                update = true;
            }

            ImGui.SameLine();
            ((Action<string>)(avail ? ImGui.Text : ImGui.TextDisabled)).Invoke(avail ? "CrossUp" : "CrossUp (not installed)");

            ImGui.SameLine();
            ImGuiExt.HelpMarker("CrossUp is a plugin that enables additional customization and features for the Cross Hotbar. If you have the CrossUp plugin installed, you can use HUD Manager layouts to manipulate your CrossUp settings.");
        }

        public void DrawControls(SavedLayout layout, ref bool update)
        {
            var config = layout.CrossUpConfig;
            if (config == null || !ImGui.CollapsingHeader("CrossUp Settings##xup")) {return;}

            if (!ImGui.BeginTabBar("CrossUpTabs",ImGuiTabBarFlags.FittingPolicyDefault)) return;

            Tabs.BarLayout(ref config, ref update);
            Tabs.Color(ref config, ref update);
            Tabs.Exhb(ref config, ref update);

            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.SetNextItemWidth(40f*Scale);

            if (ImGui.TabItemButton($"{Cog.ToIconString()}{AngleDoubleRight.ToIconString()}##xup-open"))
            {
                layout.CrossUpConfig!.OpenCrossUp(ref Plugin);
            }
            ImGui.PopFont();
            ImGuiExt.HoverTooltip("Open CrossUp");

            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.SetNextItemWidth(23f * Scale);
            if (ImGui.TabItemButton($"{TrashAlt.ToIconString()}##xup-overlay-remove"))
            {
                layout.CrossUpConfig = null;
                update = true;
            }
            ImGui.PopFont();
            ImGuiExt.HoverTooltip("Remove CrossUp settings from this layout");

            ImGui.EndTabBar();
        }

        private const ImGuiTableFlags TableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg;
        private const ImGuiColorEditFlags PickerFlags = ImGuiColorEditFlags.PickerMask | ImGuiColorEditFlags.DisplayHex;

        public class Tabs
        {
            public static void BarLayout(ref CrossUpConfig config, ref bool update)
            {

                if (!ImGui.BeginTabItem("Cross Hotbar Layout")) return;

                if (ImGui.BeginTable("CrossUpTable", 2, TableFlags))
                {
                    SetUpColumns();

                    ImGui.Indent(15f * Scale);
                    Rows.SplitBar(ref config, ref update);
                    Rows.Padlock(ref config, ref update);
                    Rows.SetNum(ref config, ref update);
                    Rows.ChangeSet(ref config, ref update);
                    Rows.TriggerText(ref config, ref update);
                    Rows.UnassignedSlots(ref config, ref update);
                    ImGui.Indent(-15f * Scale);

                    ImGui.EndTable();
                }

                ImGui.EndTabItem();
            }
            public static void Color(ref CrossUpConfig config, ref bool update)
            {
                if (!ImGui.BeginTabItem("Colors")) return;

                if (ImGui.BeginTable("CrossUpTable", 2, TableFlags))
                {
                    SetUpColumns();

                    ImGui.Indent(15f * Scale);
                    Rows.SelectBg(ref config, ref update);
                    Rows.ButtonColor(ref config, ref update);
                    Rows.TextAndBorder(ref config, ref update);
                    ImGui.Indent(-15f * Scale);

                    ImGui.EndTable();
                }

                ImGui.EndTabItem();
            }
            public static void Exhb(ref CrossUpConfig config, ref bool update)
            {
                if (!ImGui.BeginTabItem("Expanded Hold Controls")) return;

                if (ImGui.BeginTable("CrossUpTable", 2, TableFlags))
                {
                    SetUpColumns();

                    ImGui.Indent(15f * Scale);
                    Rows.SepEx(ref config, ref update);
                    Rows.LRpos(ref config, ref update);
                    if (!config.OnlyOneEx) Rows.RLpos(ref config, ref update);
                    ImGui.Indent(-15f * Scale);

                    ImGui.EndTable();
                }

                ImGui.EndTabItem();
            }

            public class Rows
            {
                public static void SplitBar(ref CrossUpConfig config, ref bool update)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGui.Spacing();
                    DrawEnabledCheckbox(ref config, CrossUpComponent.Split, ref update);

                    ImGui.TableNextColumn();

                    ImGui.BeginGroup();
                    ImGui.TextColored(ImGuiColors.DalamudGrey3, "BAR SEPARATION");

                    ImGui.Text("Separate Left/Right");
                    ImGui.SameLine(160f * Scale);
                    if (ImGui.Checkbox("##xup-splitOn", ref config.Split.on)) update = true;

                    if (config.Split.on)
                    {
                        ImGui.Text("Separation Distance");
                        ImGui.SameLine(160f * Scale);
                        ImGui.PushID("xup-resetSplit");
                        if (ImGuiComponents.IconButton(UndoAlt))
                        {
                            config.Split.distance = 100;
                            update = true;
                        }

                        ImGui.PopID();
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(90 * Scale);
                        if (ImGui.InputInt("##xup-splitDistance", ref config.Split.distance))
                        {
                            config.Split.distance = Math.Max(config.Split.distance, -142);
                            update = true;
                        }

                        ImGui.Text("Center Point");
                        ImGui.SameLine(160f * Scale);

                        ImGui.PushID("xup-resetCenter");
                        if (ImGuiComponents.IconButton(UndoAlt))
                        {
                            config.Split.center = 0;
                            update = true;
                        }

                        ImGui.PopID();
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(90 * Scale);
                        if (ImGui.InputInt("##xup-centerPoint", ref config.Split.center)) update = true;

                        ImGuiComponents.HelpMarker("This will override your HUD setting for the bar's horizontal position.");
                    }

                    ImGui.EndGroup();
                }
                public static void Padlock(ref CrossUpConfig config, ref bool update)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    DrawEnabledCheckbox(ref config, CrossUpComponent.Padlock, ref update);

                    ImGui.TableNextColumn();
                    ImGui.BeginGroup();

                    ImGui.Text("Padlock Icon");

                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-resetPadlock");
                    if (ImGuiComponents.IconButton(UndoAlt))
                    {
                        config.Padlock = (0, 0, false);
                        update = true;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-padlockX", ref config.Padlock.x)) update = true;

                    WriteIcon(ArrowsAltH, true);

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-padlockY", ref config.Padlock.y)) update = true;

                    WriteIcon(ArrowsAltV, true);

                    ImGui.SameLine();
                    if (ImGui.Checkbox("Hide##xup-hidePadlock", ref config.Padlock.hide)) update = true;

                    ImGui.EndGroup();
                }
                public static void SetNum(ref CrossUpConfig config, ref bool update)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    DrawEnabledCheckbox(ref config, CrossUpComponent.SetNum, ref update);

                    ImGui.TableNextColumn();
                    ImGui.BeginGroup();

                    ImGui.Text("SET # Text");

                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-resetSetNumText");
                    if (ImGuiComponents.IconButton(UndoAlt))
                    {
                        config.SetNum = (0, 0, false);
                        update = true;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-setNumTextX", ref config.SetNum.x)) update = true;

                    WriteIcon(ArrowsAltH, true);

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-SetNumTextY", ref config.SetNum.y)) update = true;

                    WriteIcon(ArrowsAltV, true);

                    ImGui.SameLine();
                    if (ImGui.Checkbox("Hide##xup-hideSetNumText", ref config.SetNum.hide)) update = true;

                    ImGui.EndGroup();
                }
                public static void ChangeSet(ref CrossUpConfig config, ref bool update)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    DrawEnabledCheckbox(ref config, CrossUpComponent.ChangeSet, ref update);

                    ImGui.TableNextColumn();
                    ImGui.BeginGroup();

                    ImGui.Text("CHANGE SET Display");

                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-resetChangeSet");
                    if (ImGuiComponents.IconButton(UndoAlt))
                    {
                        config.ChangeSet = (0, 0);
                        update = true;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-changeSetX", ref config.ChangeSet.x)) update = true;

                    WriteIcon(ArrowsAltH, true);

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-changeSetY", ref config.ChangeSet.y)) update = true;

                    WriteIcon(ArrowsAltV, true);

                    ImGui.EndGroup();
                }
                public static void TriggerText(ref CrossUpConfig config, ref bool update)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    DrawEnabledCheckbox(ref config, CrossUpComponent.TriggerText, ref update);

                    ImGui.TableNextColumn();
                    ImGui.BeginGroup();

                    ImGui.Text("Hide L/R Trigger Text");
                    ImGui.SameLine(160f * Scale);

                    if (ImGui.Checkbox("##xup-hideTriggerText", ref config.HideTriggerText)) update = true;

                    ImGui.EndGroup();
                }
                public static void UnassignedSlots(ref CrossUpConfig config, ref bool update)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    DrawEnabledCheckbox(ref config, CrossUpComponent.Unassigned, ref update);

                    ImGui.TableNextColumn();
                    ImGui.BeginGroup();

                    ImGui.Text("Hide Unassigned Slots");
                    ImGui.SameLine(160f * Scale);

                    if (ImGui.Checkbox("##xup-hideUnassigned", ref config.HideUnassigned)) update = true;
                    ImGui.EndGroup();
                }
                public static void SelectBg(ref CrossUpConfig config, ref bool update)
                {
                    var solid = config.SelectBG.style == 0;
                    var frame = config.SelectBG.style == 1;

                    var hidden = config.SelectBG.style == 2;
                    var normal = config.SelectBG.blend == 0;
                    var dodge = config.SelectBG.blend == 2;

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGui.Spacing();
                    DrawEnabledCheckbox(ref config, CrossUpComponent.SelectBG, ref update);

                    ImGui.TableNextColumn();
                    ImGui.BeginGroup();

                    ImGui.TextColored(ImGuiColors.DalamudGrey3, "SELECTED BAR");

                    ImGui.Text("Backdrop Color");
                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-resetBG");
                    if (ImGuiComponents.IconButton(UndoAlt))
                    {
                        config.SelectBG.color = new(100f / 255f);
                        update = true;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.ColorEdit3("##xup-bgColor", ref config.SelectBG.color, PickerFlags))
                        update = true;

                    ImGui.Text("Backdrop Style");
                    ImGui.SameLine(160f * Scale);
                    if (ImGui.RadioButton("Solid##xup-bgStyle0", solid))
                    {
                        config.SelectBG.style = 0;
                        update = true;
                    }

                    ImGui.SameLine();
                    if (ImGui.RadioButton("Frame##xup-bgStyle1", frame))
                    {
                        config.SelectBG.style = 1;
                        update = true;
                    }

                    ImGui.SameLine();
                    if (ImGui.RadioButton("Hidden##xup-bgStyle2", hidden))
                    {
                        config.SelectBG.style = 2;
                        update = true;
                    }

                    ImGui.Text("Color Blending");
                    ImGui.SameLine(160f * Scale);
                    if (ImGui.RadioButton("Normal##xup-bgBlend0", normal))
                    {
                        config.SelectBG.blend = 0;
                        update = true;
                    }

                    ImGui.SameLine();
                    if (ImGui.RadioButton("Dodge##xup-bgBlend2", dodge))
                    {
                        config.SelectBG.blend = 2;
                        update = true;
                    }

                    ImGui.EndGroup();
                }
                public static void ButtonColor(ref CrossUpConfig config, ref bool update)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGui.Spacing();
                    DrawEnabledCheckbox(ref config, CrossUpComponent.Buttons, ref update);
                    ImGui.TableNextColumn();

                    ImGui.BeginGroup();
                    ImGui.TextColored(ImGuiColors.DalamudGrey3, "BUTTONS");


                    ImGui.Text("Button Glow");

                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-resetButtonGlow");
                    if (ImGuiComponents.IconButton(UndoAlt))
                    {
                        config.Buttons.glow = new(1f);
                        update = true;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.ColorEdit3("##xup-buttonGlow", ref config.Buttons.glow, PickerFlags)) update = true;


                    ImGui.Text("Button Pulse");

                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-resetButtonPulse");
                    if (ImGuiComponents.IconButton(UndoAlt))
                    {
                        config.Buttons.pulse = new(1f);
                        update = true;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.ColorEdit3("##xup-ButtonPulse", ref config.Buttons.pulse, PickerFlags)) update = true;

                    ImGui.EndGroup();
                }
                public static void TextAndBorder(ref CrossUpConfig config, ref bool update)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGui.Spacing();
                    DrawEnabledCheckbox(ref config, CrossUpComponent.Text, ref update);

                    ImGui.TableNextColumn();
                    ImGui.BeginGroup();

                    ImGui.TextColored(ImGuiColors.DalamudGrey3, "TEXT & BORDERS");


                    ImGui.Text("Text Color");

                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-resetTextColor");
                    if (ImGuiComponents.IconButton(UndoAlt))
                    {
                        config.Text.color = new(1f);
                        update = true;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.ColorEdit3("##xup-textColor", ref config.Text.color, PickerFlags)) update = true;


                    ImGui.Text("Text Glow");

                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-resetTextGlow");
                    if (ImGuiComponents.IconButton(UndoAlt))
                    {
                        config.Text.glow = new(0.616f, 0.514f, 0.357f);
                        update = true;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.ColorEdit3("##xup-textGlow", ref config.Text.glow, PickerFlags)) update = true;


                    ImGui.Text("Border Color");

                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-resetBorder");
                    if (ImGuiComponents.IconButton(UndoAlt))
                    {
                        config.Text.border = new(1f);
                        update = true;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.ColorEdit3("##xup-border", ref config.Text.border, PickerFlags)) update = true;

                    ImGui.EndGroup();
                }
                public static void SepEx(ref CrossUpConfig config, ref bool update)
                {


                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    ImGui.Spacing();
                    DrawEnabledCheckbox(ref config, CrossUpComponent.SepEx, ref update);

                    ImGui.TableNextColumn();
                    ImGui.BeginGroup();

                    ImGui.TextColored(ImGuiColors.DalamudGrey3, "EXPANDED HOLD CONTROLS");
                    if (ImGui.Checkbox("Display Expanded Hold Controls Separately##xup-sepEx", ref config.SepEx)) update = true;

                    ImGuiComponents.HelpMarker("NOTE: This feature functions by borrowing the buttons from two of your standard mouse/keyboard hotbars. Please use CrossUp's plugin configuration to select which bars to borrow.\n\nThe hotbars you choose will not be overwritten, but they will be unavailable while the feature is active.");


                    if (ImGui.RadioButton("Show Only One Bar##xup-onlyone", config.OnlyOneEx))
                    {
                        config.OnlyOneEx = true;
                        update = true;
                    }

                    if (ImGui.RadioButton("Show Both##xup-showBoth", !config.OnlyOneEx))
                    {
                        config.OnlyOneEx = false;
                        update = true;
                    }

                    ImGui.EndGroup();
                }
                public static void LRpos(ref CrossUpConfig config, ref bool update)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    DrawEnabledCheckbox(ref config, CrossUpComponent.LRpos, ref update);

                    ImGui.TableNextColumn();
                    ImGui.BeginGroup();

                    ImGui.Text($"{(config.OnlyOneEx ? "" : "L→R ")}Bar Position");
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker("Position is relative to the center of the Cross Hotbar.\n\nDefault: (-214, -88), which matches the Left WXHB's default location.");

                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-lrReset");
                    if (ImGuiComponents.IconButton(UndoAlt))
                    {
                        config.LRpos = (-214, -88);
                        update = true;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.InputInt("##xup-lrX", ref config.LRpos.x)) update = true;

                    WriteIcon(ArrowsAltH, true);

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.InputInt("##xup-lrY", ref config.LRpos.y)) update = true;

                    WriteIcon(ArrowsAltV, true);

                    ImGui.EndGroup();
                }
                public static void RLpos(ref CrossUpConfig config, ref bool update)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    DrawEnabledCheckbox(ref config, CrossUpComponent.RLpos, ref update);

                    ImGui.TableNextColumn();
                    ImGui.BeginGroup();

                    ImGui.Text("R→L Bar Position");
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker("Position is relative to the center of the Cross Hotbar.\n\nDefault: (214, -88), which matches the Right WXHB's default location.");

                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-rlReset");
                    if (ImGuiComponents.IconButton(UndoAlt))
                    {
                        config.RLpos = (214, -88);
                        update = true;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.InputInt("##xup-rlX", ref config.RLpos.x)) update = true;

                    WriteIcon(ArrowsAltH, true);

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.InputInt("##xup-rlY", ref config.RLpos.y)) update = true;

                    WriteIcon(ArrowsAltV, true);

                    ImGui.EndGroup();
                }
            }
        }

        private static void SetUpColumns()
        {
            ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, 50 * Scale);
            ImGui.TableSetupColumn("Setting", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();
        }
        private static void WriteIcon(FontAwesomeIcon icon, bool sameLine = false)
        {
            if (sameLine) ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text($"{icon.ToIconString()}");
            ImGui.PopFont();
        }
        private static void DrawEnabledCheckbox(ref CrossUpConfig config, CrossUpComponent component, ref bool update)
        {
            var enabled = config[component];
            if (!ImGui.Checkbox($"###xup-{component}-enabled", ref enabled)) return;
            config[component] = enabled;
            update = true;
        }
    }

    private static float Scale => ImGuiHelpers.GlobalScale;
}
