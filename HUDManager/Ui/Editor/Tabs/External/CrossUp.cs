using System;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using HUD_Manager.Configuration;
using HUD_Manager.Ui;
using ImGuiNET;
using static HUDManager.Structs.External.CrossUpConfig;


namespace HUDManager.Ui.Editor.Tabs;
internal partial class ExternalElements
{
    public sealed class CrossUp : IExternalElement
    {
        public void AddButtonToList(SavedLayout layout, ref bool update)
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

            ImGui.Text("CrossUp");
            ImGui.SameLine();
            ImGuiExt.HelpMarker(
                "Install CrossUp before use. You can modify certain CrossUp settings for this layout.");
        }

        public void DrawControls(SavedLayout layout, ref bool update)
        {
            if (layout.CrossUpConfig == null || !ImGui.CollapsingHeader("CrossUp Settings##xup")) return;

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

                TrashButton(ref update);

                ImGui.EndTabBar();
            }

            static void SetupColumns()
            {
                ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, 50 * Scale);
                ImGui.TableSetupColumn("Setting", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();
            }

            void DrawEnabledCheckbox(CrossUpComponent component, ref bool update1)
            {
                var enabled = layout.CrossUpConfig[component];
                if (!ImGui.Checkbox($"###xup-{component}-enabled", ref enabled)) return;
                layout.CrossUpConfig[component] = enabled;
                update1 = true;
            }

            void LRSplit(ref bool update2)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(CrossUpComponent.Split, ref update2);

                ImGui.TableNextColumn();

                ImGui.BeginGroup();
                ImGui.TextColored(ImGuiColors.DalamudGrey3, "BAR SEPARATION");

                ImGui.Text("Separate Left/Right");
                ImGui.SameLine(160f * Scale);
                if (ImGui.Checkbox("##xup-splitOn", ref layout.CrossUpConfig.Split.on)) update2 = true;

                if (layout.CrossUpConfig.Split.on)
                {
                    ImGui.Text("Separation Distance");
                    ImGui.SameLine(160f * Scale);
                    ImGui.PushID("xup-resetSplit");
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                    {
                        layout.CrossUpConfig.Split.distance = 100;
                        update2 = true;
                    }

                    ImGui.PopID();
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-splitDistance", ref layout.CrossUpConfig.Split.distance))
                    {
                        layout.CrossUpConfig.Split.distance =
                            Math.Max(layout.CrossUpConfig.Split.distance, -142);
                        update2 = true;
                    }


                    ImGui.Text("Center Point");
                    ImGui.SameLine(160f * Scale);

                    ImGui.PushID("xup-resetCenter");
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                    {
                        layout.CrossUpConfig.Split.center = 0;
                        update2 = true;
                    }

                    ImGui.PopID();
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-centerPoint", ref layout.CrossUpConfig.Split.center))
                        update2 = true;

                    ImGuiComponents.HelpMarker(
                        "This will override your HUD setting for the bar's horizontal position.");
                }

                ImGui.EndGroup();
            }

            void Padlock(ref bool update3)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(CrossUpComponent.Padlock, ref update3);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.Text("Padlock Icon");

                ImGui.SameLine(160f * Scale);

                ImGui.PushID("xup-resetPadlock");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.Padlock = (0, 0, false);
                    update3 = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(90 * Scale);
                if (ImGui.InputInt("X##xup-padlockX", ref layout.CrossUpConfig.Padlock.x)) update3 = true;

                ImGui.SameLine();
                ImGui.SetNextItemWidth(90 * Scale);
                if (ImGui.InputInt("Y##xup-padlockY", ref layout.CrossUpConfig.Padlock.y)) update3 = true;

                ImGui.SameLine();
                if (ImGui.Checkbox("Hide##xup-hidePadlock", ref layout.CrossUpConfig.Padlock.hide))
                    update3 = true;

                ImGui.EndGroup();
            }

            void SetNumText(ref bool update4)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(CrossUpComponent.SetNum, ref update4);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.Text("SET # Text");

                ImGui.SameLine(160f * Scale);

                ImGui.PushID("xup-resetSetNumText");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.SetNum = (0, 0, false);
                    update4 = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(90 * Scale);
                if (ImGui.InputInt("X##xup-setNumTextX", ref layout.CrossUpConfig.SetNum.x)) update4 = true;

                ImGui.SameLine();
                ImGui.SetNextItemWidth(90 * Scale);
                if (ImGui.InputInt("Y##xup-SetNumTextY", ref layout.CrossUpConfig.SetNum.y)) update4 = true;

                ImGui.SameLine();
                if (ImGui.Checkbox("Hide##xup-hideSetNumText", ref layout.CrossUpConfig.SetNum.hide))
                    update4 = true;

                ImGui.EndGroup();
            }

            void ChangeSetDisplay(ref bool update5)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(CrossUpComponent.ChangeSet, ref update5);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.Text("CHANGE SET Display");

                ImGui.SameLine(160f * Scale);

                ImGui.PushID("xup-resetChangeSet");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.ChangeSet = (0, 0);
                    update5 = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(90 * Scale);
                if (ImGui.InputInt("X##xup-changeSetX", ref layout.CrossUpConfig.ChangeSet.x)) update5 = true;

                ImGui.SameLine();
                ImGui.SetNextItemWidth(90 * Scale);
                if (ImGui.InputInt("Y##xup-changeSetY", ref layout.CrossUpConfig.ChangeSet.y)) update5 = true;

                ImGui.EndGroup();
            }

            void TriggerText(ref bool update6)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(CrossUpComponent.TriggerText, ref update6);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.Text("Hide L/R Trigger Text");
                ImGui.SameLine(160f * Scale);

                if (ImGui.Checkbox("##xup-hideTriggerText", ref layout.CrossUpConfig.HideTriggerText))
                    update6 = true;

                ImGui.EndGroup();
            }

            void UnassignedSlots(ref bool update7)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(CrossUpComponent.Unassigned, ref update7);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.Text("Hide Unassigned Slots");
                ImGui.SameLine(160f * Scale);

                if (ImGui.Checkbox("##xup-hideUnassigned", ref layout.CrossUpConfig.HideUnassigned))
                    update7 = true;
                ImGui.EndGroup();
            }

            void SelectBG(ref bool update8)
            {
                var solid = layout.CrossUpConfig.SelectBG.style == 0;
                var frame = layout.CrossUpConfig.SelectBG.style == 1;
                var hidden = layout.CrossUpConfig.SelectBG.style == 2;
                var normal = layout.CrossUpConfig.SelectBG.blend == 0;
                var dodge = layout.CrossUpConfig.SelectBG.blend == 2;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(CrossUpComponent.SelectBG, ref update8);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.TextColored(ImGuiColors.DalamudGrey3, "SELECTED BAR");

                ImGui.Text("Backdrop Color");
                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-resetBG");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.SelectBG.color = new(100f / 255f);
                    update8 = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.ColorEdit3("##xup-bgColor", ref layout.CrossUpConfig.SelectBG.color, pickerFlags))
                    update8 = true;

                ImGui.Text("Backdrop Style");
                ImGui.SameLine(160f * Scale);
                if (ImGui.RadioButton("Solid##xup-bgStyle0", solid))
                {
                    layout.CrossUpConfig.SelectBG.style = 0;
                    update8 = true;
                }

                ImGui.SameLine();
                if (ImGui.RadioButton("Frame##xup-bgStyle1", frame))
                {
                    layout.CrossUpConfig.SelectBG.style = 1;
                    update8 = true;
                }

                ImGui.SameLine();
                if (ImGui.RadioButton("Hidden##xup-bgStyle2", hidden))
                {
                    layout.CrossUpConfig.SelectBG.style = 2;
                    update8 = true;
                }

                ImGui.Text("Color Blending");
                ImGui.SameLine(160f * Scale);
                if (ImGui.RadioButton("Normal##xup-bgBlend0", normal))
                {
                    layout.CrossUpConfig.SelectBG.blend = 0;
                    update8 = true;
                }

                ImGui.SameLine();
                if (ImGui.RadioButton("Dodge##xup-bgBlend2", dodge))
                {
                    layout.CrossUpConfig.SelectBG.blend = 2;
                    update8 = true;
                }

                ImGui.EndGroup();
            }

            void Buttons(ref bool update9)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(CrossUpComponent.Buttons, ref update9);
                ImGui.TableNextColumn();

                ImGui.BeginGroup();
                ImGui.TextColored(ImGuiColors.DalamudGrey3, "BUTTONS");


                ImGui.Text("Button Glow");

                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-resetButtonGlow");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.Buttons.glow = new(1f);
                    update9 = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.ColorEdit3("##xup-buttonGlow", ref layout.CrossUpConfig.Buttons.glow, pickerFlags))
                    update9 = true;


                ImGui.Text("Button Pulse");

                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-resetButtonPulse");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.Buttons.pulse = new(1f);
                    update9 = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.ColorEdit3("##xup-ButtonPulse", ref layout.CrossUpConfig.Buttons.pulse, pickerFlags))
                    update9 = true;

                ImGui.EndGroup();
            }

            void TextColor(ref bool update10)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(CrossUpComponent.Text, ref update10);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();

                ImGui.TextColored(ImGuiColors.DalamudGrey3, "TEXT & BORDERS");


                ImGui.Text("Text Color");

                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-resetTextColor");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.Text.color = new(1f);
                    update10 = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.ColorEdit3("##xup-textColor", ref layout.CrossUpConfig.Text.color, pickerFlags))
                    update10 = true;


                ImGui.Text("Text Glow");

                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-resetTextGlow");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.Text.glow = new(0.616f, 0.514f, 0.357f);
                    update10 = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.ColorEdit3("##xup-textGlow", ref layout.CrossUpConfig.Text.glow, pickerFlags))
                    update10 = true;


                ImGui.Text("Border Color");

                ImGui.SameLine(160f * Scale);
                ImGui.PushID("xup-resetBorder");
                if (ImGuiComponents.IconButton(FontAwesomeIcon.UndoAlt))
                {
                    layout.CrossUpConfig.Text.border = new(1f);
                    update10 = true;
                }

                ImGui.PopID();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.ColorEdit3("##xup-border", ref layout.CrossUpConfig.Text.border, pickerFlags))
                    update10 = true;

                ImGui.EndGroup();
            }

            void SeparateEx(ref bool update11)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(CrossUpComponent.SepEx, ref update11);

                ImGui.TableNextColumn();
                ImGui.BeginGroup();


                ImGui.TextColored(ImGuiColors.DalamudGrey3, "EXPANDED HOLD CONTROLS");
                if (ImGui.Checkbox("Display Expanded Hold Controls Separately##xup-sepEx",
                        ref layout.CrossUpConfig.SepEx)) update11 = true;

                ImGuiComponents.HelpMarker(
                    "NOTE: This feature functions by borrowing the buttons from two of your standard mouse/keyboard hotbars. Please use CrossUp's plugin configuration to select which bars to borrow.\n\nThe hotbars you choose will not be overwritten, but they will be unavailable while the feature is active.");


                if (ImGui.RadioButton("Show Only One Bar##xup-onlyone", layout.CrossUpConfig.OnlyOneEx))
                {
                    layout.CrossUpConfig.OnlyOneEx = true;
                    update11 = true;
                }

                ImGui.SameLine();
                if (ImGui.RadioButton("Show Both##xup-showBoth", !layout.CrossUpConfig.OnlyOneEx))
                {
                    layout.CrossUpConfig.OnlyOneEx = false;
                    update11 = true;
                }


                ImGui.EndGroup();
            }

            void LRpos(ref bool update12)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                DrawEnabledCheckbox(CrossUpComponent.LRpos, ref update12);

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
                    update12 = true;
                }

                ImGui.PopID();

                ImGui.SameLine();

                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.InputInt("X##xup-lrX", ref layout.CrossUpConfig.LRpos.x)) update12 = true;

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.InputInt("Y##xup-lrY", ref layout.CrossUpConfig.LRpos.y)) update12 = true;

                ImGui.EndGroup();
            }

            void RLpos(ref bool update13)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                DrawEnabledCheckbox(CrossUpComponent.RLpos, ref update13);

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
                    update13 = true;
                }

                ImGui.PopID();

                ImGui.SameLine();

                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.InputInt("X##xup-rlX", ref layout.CrossUpConfig.RLpos.x)) update13 = true;

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100 * Scale);
                if (ImGui.InputInt("Y##xup-rlY", ref layout.CrossUpConfig.RLpos.y)) update13 = true;


                ImGui.EndGroup();
            }

            void TrashButton(ref bool update14)
            {
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 1.5f * Scale);
                if (ImGuiExt.IconButton(FontAwesomeIcon.TrashAlt, "xup-overlay-remove"))
                {
                    layout.CrossUpConfig = null;
                    update14 = true;
                }

                ImGuiExt.HoverTooltip("Remove CrossUp settings from this layout");
            }
        }

    }

    private static float Scale => ImGuiHelpers.GlobalScale;
}