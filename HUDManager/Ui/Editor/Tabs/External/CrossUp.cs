using System;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using HUD_Manager.Configuration;
using HUD_Manager;
using HUD_Manager.Ui;
using ImGuiNET;
using static HUDManager.Structs.External.CrossUpConfig;


namespace HUDManager.Ui.Editor.Tabs;
internal partial class ExternalElements
{
    public sealed class CrossUp : IExternalElement
    {
        public bool Available(Plugin plugin)
        {
            try
            {
                return plugin.Interface.PluginNames.Contains("CrossUp") && plugin.Interface.GetIpcSubscriber<bool>("CrossUp.Available").InvokeFunc();
            }
            catch
            {
                return false;
            }
        }

        public void AddButtonToList(SavedLayout layout, ref bool update, bool avail)
        {
            var exists = layout.CrossUpConfig != null;
            var icon = exists ? FontAwesomeIcon.TrashAlt : FontAwesomeIcon.Plus;
            var label = $"uimanager-{(exists ? "remove" : "add")}-crossup";

            if (ImGuiExt.IconButton(icon, label))
            {
                layout.CrossUpConfig = exists ? null : new();
                update = true;
            }

            ImGui.SameLine();

            if (avail)
                ImGui.Text("CrossUp");
            else
                ImGui.TextDisabled("CrossUp (not installed)");

            ImGui.SameLine();
            ImGuiExt.HelpMarker("CrossUp is a plugin that enables additional customization and features for the Cross Hotbar. If you have the CrossUp plugin installed, you can use HUD Manager layouts to manipulate your CrossUp settings.");
        }

        public void DrawControls(SavedLayout layout, ref bool update)
        {
            if (layout.CrossUpConfig == null || !ImGui.CollapsingHeader("CrossUp Settings##xup")) {return;}
            
            const ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.PadOuterX |
                                               ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg;
            const ImGuiColorEditFlags pickerFlags = ImGuiColorEditFlags.PickerMask | ImGuiColorEditFlags.DisplayHex;

            if (ImGui.BeginTabBar("CrossUpTabs"))
            {
                if (ImGui.BeginTabItem("Cross Hotbar Layout"))
                {
                    if (ImGui.BeginTable("CrossUpTable", 2, tableFlags))
                    {
                        SetupColumns();

                        ImGui.Indent(15f * Scale);
                        LRSplit(ref update);
                        Padlock(ref update);
                        SetNumText(ref update);
                        ChangeSetDisplay(ref update);
                        TriggerText(ref update);
                        UnassignedSlots(ref update);
                        ImGui.Indent(-15f * Scale);

                        ImGui.EndTable();
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Colors"))
                {
                    if (ImGui.BeginTable("CrossUpTable", 2, tableFlags))
                    {
                        SetupColumns();

                        ImGui.Indent(15f * Scale);
                        SelectBG(ref update);
                        Buttons(ref update);
                        TextColor(ref update);
                        ImGui.Indent(-15f * Scale);

                        ImGui.EndTable();
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Expanded Hold Controls"))
                {
                    if (ImGui.BeginTable("CrossUpTable", 2, tableFlags))
                    {
                        SetupColumns();

                        ImGui.Indent(15f * Scale);
                        SeparateEx(ref update);
                        LRpos(ref update);
                        if (!layout.CrossUpConfig.OnlyOneEx) RLpos(ref update);
                        ImGui.Indent(-15f * Scale);

                        ImGui.EndTable();
                    }

                    ImGui.EndTabItem();
                }
                

                
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.SetNextItemWidth(23f*Scale);
                if (ImGui.TabItemButton($"{FontAwesomeIcon.TrashAlt.ToIconString()}##xup-overlay-remove"))
                {
                    layout.CrossUpConfig = null;
                    update = true;
                }
                ImGui.PopFont();
                ImGuiExt.HoverTooltip("Remove CrossUp settings from this layout");

                ImGui.EndTabBar();
            }

            static void SetupColumns()
            {
                ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, 50 * Scale);
                ImGui.TableSetupColumn("Setting", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();
            }

            void DrawEnabledCheckbox(CrossUpComponent component, ref bool update)
            {
                var enabled = layout.CrossUpConfig[component];
                if (!ImGui.Checkbox($"###xup-{component}-enabled", ref enabled)) return;
                layout.CrossUpConfig[component] = enabled;
                update = true;
            }

            void LRSplit(ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(CrossUpComponent.Split, ref update);

                ImGui.TableNextColumn();

                ImGui.BeginGroup();
                ImGui.TextColored(ImGuiColors.DalamudGrey3, "BAR SEPARATION");

                ImGui.Text("Separate Left/Right");
                ImGui.SameLine(160f * Scale);
                if (ImGui.Checkbox("##xup-splitOn", ref layout.CrossUpConfig.Split.on)) update = true;

                if (layout.CrossUpConfig.Split.on)
                {
                    ImGui.Text("Separation Distance");
                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-resetSplit");
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                    {
                        layout.CrossUpConfig.Split.distance = 100;
                        update = true;
                    }

                    ImGui.PopID();
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-splitDistance", ref layout.CrossUpConfig.Split.distance))
                    {
                        layout.CrossUpConfig.Split.distance =
                            Math.Max(layout.CrossUpConfig.Split.distance, -142);
                        update = true;
                    }


                    ImGui.Text("Center Point");
                    ImGui.SameLine(160f * Scale);

                    ImGui.PushID("xup-resetCenter");
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                    {
                        layout.CrossUpConfig.Split.center = 0;
                        update = true;
                    }

                    ImGui.PopID();
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-centerPoint", ref layout.CrossUpConfig.Split.center))
                        update = true;

                    ImGuiComponents.HelpMarker(
                        "This will override your HUD setting for the bar's horizontal position.");
                }

                ImGui.EndGroup();
            }

            void Padlock(ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(CrossUpComponent.Padlock, ref update);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.Text("Padlock Icon");

                ImGui.SameLine(160f * Scale);

                ImGui.PushID("xup-resetPadlock");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.Padlock = (0, 0, false);
                    update = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(90 * Scale);
                if (ImGui.InputInt("##xup-padlockX", ref layout.CrossUpConfig.Padlock.x)) update = true;

                WriteIcon(FontAwesomeIcon.ArrowsAltH, true);

                ImGui.SameLine();
                ImGui.SetNextItemWidth(90 * Scale);
                if (ImGui.InputInt("##xup-padlockY", ref layout.CrossUpConfig.Padlock.y)) update = true;

                WriteIcon(FontAwesomeIcon.ArrowsAltV, true);

                ImGui.SameLine();
                if (ImGui.Checkbox("Hide##xup-hidePadlock", ref layout.CrossUpConfig.Padlock.hide))
                    update = true;

                ImGui.EndGroup();
            }

            void SetNumText(ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(CrossUpComponent.SetNum, ref update);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.Text("SET # Text");

                ImGui.SameLine(160f * Scale);

                ImGui.PushID("xup-resetSetNumText");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.SetNum = (0, 0, false);
                    update = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(90 * Scale);
                if (ImGui.InputInt("##xup-setNumTextX", ref layout.CrossUpConfig.SetNum.x)) update = true;

                WriteIcon(FontAwesomeIcon.ArrowsAltH, true);

                ImGui.SameLine();
                ImGui.SetNextItemWidth(90 * Scale);
                if (ImGui.InputInt("##xup-SetNumTextY", ref layout.CrossUpConfig.SetNum.y)) update = true;

                WriteIcon(FontAwesomeIcon.ArrowsAltV, true);

                ImGui.SameLine();
                if (ImGui.Checkbox("Hide##xup-hideSetNumText", ref layout.CrossUpConfig.SetNum.hide))
                    update = true;

                ImGui.EndGroup();
            }

            void ChangeSetDisplay(ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(CrossUpComponent.ChangeSet, ref update);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.Text("CHANGE SET Display");

                ImGui.SameLine(160f * Scale);

                ImGui.PushID("xup-resetChangeSet");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.ChangeSet = (0, 0);
                    update = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(90 * Scale);
                if (ImGui.InputInt("##xup-changeSetX", ref layout.CrossUpConfig.ChangeSet.x)) update = true;

                WriteIcon(FontAwesomeIcon.ArrowsAltH, true);

                ImGui.SameLine();
                ImGui.SetNextItemWidth(90 * Scale);
                if (ImGui.InputInt("##xup-changeSetY", ref layout.CrossUpConfig.ChangeSet.y)) update = true;

                WriteIcon(FontAwesomeIcon.ArrowsAltV, true);

                ImGui.EndGroup();
            }

            void TriggerText(ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(CrossUpComponent.TriggerText, ref update);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.Text("Hide L/R Trigger Text");
                ImGui.SameLine(160f * Scale);

                if (ImGui.Checkbox("##xup-hideTriggerText", ref layout.CrossUpConfig.HideTriggerText))
                    update = true;

                ImGui.EndGroup();
            }

            void UnassignedSlots(ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(CrossUpComponent.Unassigned, ref update);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.Text("Hide Unassigned Slots");
                ImGui.SameLine(160f * Scale);

                if (ImGui.Checkbox("##xup-hideUnassigned", ref layout.CrossUpConfig.HideUnassigned))
                    update = true;
                ImGui.EndGroup();
            }

            void SelectBG(ref bool update)
            {
                var solid = layout.CrossUpConfig.SelectBG.style == 0;
                var frame = layout.CrossUpConfig.SelectBG.style == 1;
                var hidden = layout.CrossUpConfig.SelectBG.style == 2;
                var normal = layout.CrossUpConfig.SelectBG.blend == 0;
                var dodge = layout.CrossUpConfig.SelectBG.blend == 2;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(CrossUpComponent.SelectBG, ref update);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.TextColored(ImGuiColors.DalamudGrey3, "SELECTED BAR");

                ImGui.Text("Backdrop Color");
                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-resetBG");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.SelectBG.color = new(100f / 255f);
                    update = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.ColorEdit3("##xup-bgColor", ref layout.CrossUpConfig.SelectBG.color, pickerFlags))
                    update = true;

                ImGui.Text("Backdrop Style");
                ImGui.SameLine(160f * Scale);
                if (ImGui.RadioButton("Solid##xup-bgStyle0", solid))
                {
                    layout.CrossUpConfig.SelectBG.style = 0;
                    update = true;
                }

                ImGui.SameLine();
                if (ImGui.RadioButton("Frame##xup-bgStyle1", frame))
                {
                    layout.CrossUpConfig.SelectBG.style = 1;
                    update = true;
                }

                ImGui.SameLine();
                if (ImGui.RadioButton("Hidden##xup-bgStyle2", hidden))
                {
                    layout.CrossUpConfig.SelectBG.style = 2;
                    update = true;
                }

                ImGui.Text("Color Blending");
                ImGui.SameLine(160f * Scale);
                if (ImGui.RadioButton("Normal##xup-bgBlend0", normal))
                {
                    layout.CrossUpConfig.SelectBG.blend = 0;
                    update = true;
                }

                ImGui.SameLine();
                if (ImGui.RadioButton("Dodge##xup-bgBlend2", dodge))
                {
                    layout.CrossUpConfig.SelectBG.blend = 2;
                    update = true;
                }

                ImGui.EndGroup();
            }

            void Buttons(ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(CrossUpComponent.Buttons, ref update);
                ImGui.TableNextColumn();

                ImGui.BeginGroup();
                ImGui.TextColored(ImGuiColors.DalamudGrey3, "BUTTONS");


                ImGui.Text("Button Glow");

                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-resetButtonGlow");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.Buttons.glow = new(1f);
                    update = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.ColorEdit3("##xup-buttonGlow", ref layout.CrossUpConfig.Buttons.glow, pickerFlags))
                    update = true;


                ImGui.Text("Button Pulse");

                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-resetButtonPulse");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.Buttons.pulse = new(1f);
                    update = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.ColorEdit3("##xup-ButtonPulse", ref layout.CrossUpConfig.Buttons.pulse, pickerFlags))
                    update = true;

                ImGui.EndGroup();
            }

            void TextColor(ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(CrossUpComponent.Text, ref update);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.TextColored(ImGuiColors.DalamudGrey3, "TEXT & BORDERS");


                ImGui.Text("Text Color");

                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-resetTextColor");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.Text.color = new(1f);
                    update = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.ColorEdit3("##xup-textColor", ref layout.CrossUpConfig.Text.color, pickerFlags))
                    update = true;


                ImGui.Text("Text Glow");

                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-resetTextGlow");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.Text.glow = new(0.616f, 0.514f, 0.357f);
                    update = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.ColorEdit3("##xup-textGlow", ref layout.CrossUpConfig.Text.glow, pickerFlags))
                    update = true;


                ImGui.Text("Border Color");

                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-resetBorder");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.Text.border = new(1f);
                    update = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.ColorEdit3("##xup-border", ref layout.CrossUpConfig.Text.border, pickerFlags))
                    update = true;

                ImGui.EndGroup();
            }

            void SeparateEx(ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(CrossUpComponent.SepEx, ref update);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();


                ImGui.TextColored(ImGuiColors.DalamudGrey3, "EXPANDED HOLD CONTROLS");
                if (ImGui.Checkbox("Display Expanded Hold Controls Separately##xup-sepEx",
                        ref layout.CrossUpConfig.SepEx)) update = true;

                ImGuiComponents.HelpMarker(
                    "NOTE: This feature functions by borrowing the buttons from two of your standard mouse/keyboard hotbars. Please use CrossUp's plugin configuration to select which bars to borrow.\n\nThe hotbars you choose will not be overwritten, but they will be unavailable while the feature is active.");


                if (ImGui.RadioButton("Show Only One Bar##xup-onlyone", layout.CrossUpConfig.OnlyOneEx))
                {
                    layout.CrossUpConfig.OnlyOneEx = true;
                    update = true;
                }
                
                if (ImGui.RadioButton("Show Both##xup-showBoth", !layout.CrossUpConfig.OnlyOneEx))
                {
                    layout.CrossUpConfig.OnlyOneEx = false;
                    update = true;
                }


                ImGui.EndGroup();
            }

            void LRpos(ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                DrawEnabledCheckbox(CrossUpComponent.LRpos, ref update);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.Text($"{(layout.CrossUpConfig.OnlyOneEx ? "" : "L→R ")}Bar Position");
                ImGui.SameLine();
                ImGuiComponents.HelpMarker(
                    "Position is relative to the center of the Cross Hotbar.\n\nDefault: (-214, -88), which matches the Left WXHB's default location.");


                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-lrReset");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.LRpos = (-214, -88);
                    update = true;
                }

                ImGui.PopID();

                ImGui.SameLine();

                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.InputInt("##xup-lrX", ref layout.CrossUpConfig.LRpos.x)) update = true;

                WriteIcon(FontAwesomeIcon.ArrowsAltH, true);

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.InputInt("##xup-lrY", ref layout.CrossUpConfig.LRpos.y)) update = true;

                WriteIcon(FontAwesomeIcon.ArrowsAltV, true);

                ImGui.EndGroup();
            }

            void RLpos(ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                DrawEnabledCheckbox(CrossUpComponent.RLpos, ref update);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.Text("R→L Bar Position");
                ImGui.SameLine();
                ImGuiComponents.HelpMarker(
                    "Position is relative to the center of the Cross Hotbar.\n\nDefault: (214, -88), which matches the Right WXHB's default location.");

                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-rlReset");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.RLpos = (214, -88);
                    update = true;
                }

                ImGui.PopID();

                ImGui.SameLine();

                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.InputInt("##xup-rlX", ref layout.CrossUpConfig.RLpos.x)) update = true;

                WriteIcon(FontAwesomeIcon.ArrowsAltH, true);

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.InputInt("##xup-rlY", ref layout.CrossUpConfig.RLpos.y)) update = true;

                WriteIcon(FontAwesomeIcon.ArrowsAltV, true);

                ImGui.EndGroup();
            }
        }

        public static void WriteIcon(FontAwesomeIcon icon, bool sameLine = false)
        {
            if (sameLine) ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text($"{icon.ToIconString()}");
            ImGui.PopFont();
        }
    }

    private static float Scale => ImGuiHelpers.GlobalScale;
}