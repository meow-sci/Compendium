using Brutal.ImGuiApi;
using Brutal.ImGuiApi.Extensions;
using Brutal.Numerics;
using KSA;
using ImGui = Brutal.ImGuiApi.ImGui;

namespace Compendium
{
    public partial class Compendium
    {
        private const double RainbowCycleSeconds = 4d;

        private static bool showBodyPointer = false;
        private static bool pointerDualLines = false;
        private static bool pointerRainbow = false;
        private static float3 pointerColor = new float3(1f, 1f, 1f);

        // Compendium window rect from the last frame it was drawn; the pointer anchors to its corners.
        private static bool pointerWindowVisible;
        private static float2 pointerWindowPosition;
        private static float2 pointerWindowSize;

        private static string? pointerTargetKey;
        private static Astronomical? pointerTargetRoot;
        private static Astronomical? pointerTarget;

        private static void CaptureWindowRectForPointer()
        {
            pointerWindowPosition = ImGui.GetWindowPos();
            pointerWindowSize = ImGui.GetWindowSize();
            pointerWindowVisible = true;
        }

        private static Astronomical? GetPointerTarget()
        {
            if (showWindow != "Celestial")
            {
                return null;
            }

            var root = GetSelectedRoot();
            if (pointerTargetKey != selectedCelestialId || pointerTargetRoot != root)
            {
                pointerTargetKey = selectedCelestialId;
                pointerTargetRoot = root;
                pointerTarget = FindSystemStar(selectedCelestialId) ?? FindCelestialByKey(root, selectedCelestialId);
            }
            return pointerTarget;
        }

        // Actual rendered width of the Pointer button from the last frame; ConsoleWidgets buttons are wider than plain text + padding.
        private static float pointerButtonWidth;
        // Screen X of the D button's right edge, so the Pointer button lines up under it.
        private static float topRowRightEdgeX;

        private void DrawPointerToggle()
        {
            const string onText = "Pointer On";
            var style = ImGui.GetStyle();
            float buttonWidth = pointerButtonWidth > 0f ? pointerButtonWidth : ImGui.CalcTextSize("Pointer").X + style.FramePadding.X * 2f;
            float totalWidth = buttonWidth;
            if (showBodyPointer)
            {
                totalWidth += ImGui.CalcTextSize(onText).X + style.ItemSpacing.X;
            }
            float rowStartX = ImGui.GetCursorScreenPos().X;
            float rightEdgeX = topRowRightEdgeX > 0f ? topRowRightEdgeX : rowStartX + ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorScreenPos(new float2(Math.Max(rowStartX, rightEdgeX - totalWidth), ImGui.GetCursorScreenPos().Y));

            if (showBodyPointer)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(in ConsoleStyle.Positive, onText);
                ImGui.SameLine();
                if (ConsoleWidgets.PositiveButton("Pointer", "CompendiumBodyPointer", default))
                {
                    showBodyPointer = false;
                    SaveSettings();
                }
            }
            else if (ConsoleWidgets.Button("Pointer", "CompendiumBodyPointer", default))
            {
                showBodyPointer = true;
                SaveSettings();
            }
            pointerButtonWidth = ImGui.GetItemRectSize().X;
        }

        private void DrawPointerSettings()
        {
            ImGui.Text("Pointer Settings:");

            if (ConsoleWidgets.Button(pointerDualLines ? "Double" : "Single", "CompendiumPointerLineMode", default))
            {
                pointerDualLines = !pointerDualLines;
                SaveSettings();
            }

            float3 color = pointerColor;
            using (new ImGuiDisabledScope(pointerRainbow))
            {
                if (ImGui.ColorEdit3(new ImString("Pointer Color##CompendiumPointerColor"), ref color, ImGuiColorEditFlags.NoInputs))
                {
                    pointerColor = color;
                    SaveSettings();
                }
            }

            if (pointerRainbow)
            {
                if (ConsoleWidgets.PositiveButton("Rainbow Spectrum", "CompendiumPointerRainbow", default))
                {
                    pointerRainbow = false;
                    SaveSettings();
                }
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(in ConsoleStyle.Positive, "Rainbow On");
            }
            else if (ConsoleWidgets.Button("Rainbow Spectrum", "CompendiumPointerRainbow", default))
            {
                pointerRainbow = true;
                SaveSettings();
            }
        }

        private static unsafe void DrawBodyPointer()
        {
            if (!showBodyPointer || !pointerWindowVisible)
            {
                return;
            }

            Astronomical? target = GetPointerTarget();
            Camera camera = Program.GetMainCamera();
            ImGuiViewportPtr viewport = ImGui.GetMainViewport();
            if (target == null || camera == null || viewport.IsNull())
            {
                return;
            }

            if (!TryGetPointerScreenTarget(camera, viewport, camera.GetPositionEgo(target), out float2 targetScreen))
            {
                return;
            }

            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowSize(viewport.Size);
            ImGui.SetNextWindowViewport(viewport.ID);
            ImGuiWindowFlags flags =
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoBackground |
                ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNavFocus |
                ImGuiWindowFlags.NoSavedSettings;
            if (ImGui.Begin("##CompendiumPointerOverlay", flags))
            {
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                ImColor8 lineColor = ToLineColor(pointerRainbow ? GetRainbowPointerColor() : pointerColor);
                GetTwoNearestWindowCorners(pointerWindowPosition, pointerWindowSize, targetScreen, out float2 nearest, out float2 secondNearest);
                drawList.AddLine(nearest, targetScreen, lineColor, 1.5f);
                if (pointerDualLines)
                {
                    drawList.AddLine(secondNearest, targetScreen, lineColor, 1.5f);
                }
            }
            ImGui.End();
        }

        // On-screen position of the target, or the point on the screen edge in its direction when off-view.
        private static bool TryGetPointerScreenTarget(Camera camera, ImGuiViewportPtr viewport, double3 positionEgo, out float2 target)
        {
            if (camera.IsPointWithinFov(positionEgo))
            {
                target = camera.EgoToScreen(positionEgo) + viewport.Pos;
                return true;
            }

            float4 clip = camera.EgoToClip(positionEgo);
            if (MathF.Abs(clip.W) < float.Epsilon)
            {
                target = default;
                return false;
            }

            float x = clip.X / clip.W;
            float y = clip.Y / clip.W;
            if (clip.W < 0f)
            {
                x = -x;
                y = -y;
            }
            if (MathF.Abs(x) < float.Epsilon && MathF.Abs(y) < float.Epsilon)
            {
                y = -1f;
            }

            float maximum = MathF.Max(MathF.Abs(x), MathF.Abs(y));
            if (maximum < float.Epsilon)
            {
                target = default;
                return false;
            }

            float2 center = viewport.Pos + viewport.Size * 0.5f;
            target = center + new float2(x / maximum * viewport.Size.X * 0.5f, y / maximum * viewport.Size.Y * 0.5f);
            return true;
        }

        private static void GetTwoNearestWindowCorners(float2 windowPosition, float2 windowSize, float2 target, out float2 nearest, out float2 secondNearest)
        {
            float2[] corners =
            {
                windowPosition,
                new float2(windowPosition.X + windowSize.X, windowPosition.Y),
                new float2(windowPosition.X, windowPosition.Y + windowSize.Y),
                windowPosition + windowSize
            };
            Array.Sort(corners, (a, b) => DistanceSquared(a, target).CompareTo(DistanceSquared(b, target)));
            nearest = corners[0];
            secondNearest = corners[1];
        }

        private static float DistanceSquared(float2 a, float2 b)
        {
            float x = a.X - b.X;
            float y = a.Y - b.Y;
            return x * x + y * y;
        }

        private static ImColor8 ToLineColor(float3 color)
        {
            return new ImColor8(
                (byte)(Math.Clamp(color.X, 0f, 1f) * 255f),
                (byte)(Math.Clamp(color.Y, 0f, 1f) * 255f),
                (byte)(Math.Clamp(color.Z, 0f, 1f) * 255f),
                255);
        }

        // Hue sweep at full saturation, one trip through the spectrum per RainbowCycleSeconds.
        private static float3 GetRainbowPointerColor()
        {
            double cycle = ImGui.GetTime() / RainbowCycleSeconds;
            float sector = (float)(cycle - Math.Floor(cycle)) * 6f;
            int wedge = (int)sector;
            float fraction = sector - wedge;

            return wedge switch
            {
                0 => new float3(1f, fraction, 0f),
                1 => new float3(1f - fraction, 1f, 0f),
                2 => new float3(0f, 1f, fraction),
                3 => new float3(0f, 1f - fraction, 1f),
                4 => new float3(fraction, 0f, 1f),
                _ => new float3(1f, 0f, 1f - fraction),
            };
        }
    }
}
