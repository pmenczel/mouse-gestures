using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WMG.Core;

namespace WMG.Reactions
{
    public readonly record struct WindowDimensions
    {
        public byte MonitorID { get; init; }

        // If this is false -> X, Y, Width, Height are relevant. They are absolute coordinates on the screen of the given monitor
        // If this is true -> Rx, Ry, Rwidth, Rheight are relevant. They are relative coordinates, i.e., percentage values 0 <= p <= 1, on the screen of the given monitor
        public bool RelativeCoordinates { get; init; }

        public int X { get; init; }
        public int Y { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }

        public double Rx { get; init; }
        public double Ry { get; init; }
        public double Rwidth { get; init; }
        public double Rheight { get; init; }

        public WindowDimensions() { }

        public WindowDimensions(byte monitorID, int x, int y, int width, int height)
        {
            this.MonitorID = monitorID;
            this.RelativeCoordinates = false;
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }

        public WindowDimensions(byte monitorID, double rx, double ry, double rwidth, double rheight)
        {
            this.MonitorID = monitorID;
            this.RelativeCoordinates = true;
            this.Rx = rx;
            this.Ry = ry;
            this.Rwidth = rwidth;
            this.Rheight = rheight;
        }

        internal Rect? ConvertToVirtualScreenCoords(MonitorList monitorList, bool fullscreen = false)
        {
            if (monitorList.Count <= MonitorID)
            {
                return null;
            }
            var mInfo = monitorList[MonitorID];
            Rect baseArea = fullscreen ? mInfo.MonitorArea : mInfo.WorkArea;

            if (RelativeCoordinates)
            {
                int w = baseArea.Width;
                int h = baseArea.Height;
                return Rect.FromDimensions(baseArea.Left + (int)(Rx * w),
                                           baseArea.Top + (int)(Ry * h),
                                           (int)(Rwidth * w),
                                           (int)(Rheight * h));
            }
            else
            {
                return Rect.FromDimensions(baseArea.Left + X, baseArea.Top + Y, Width, Height);
            }
        }
    }

    public record Layout(string Name, List<WindowDimensions> Dimensions);

    public class LayoutManager
    {
        [JsonInclude]
        public List<Layout> Layouts { get; } = new();

        [JsonInclude]
        public bool Fullscreen { get; set; } = false;

        [JsonInclude]
        public int ActiveLayoutIndex { get; set; } = 0;

        public LayoutManager() { }

        [JsonConstructor]
        public LayoutManager(List<Layout> layouts, bool fullscreen, int activeLayoutIndex)
        {
            Layouts.AddRange(layouts);
            Fullscreen = fullscreen;
            ActiveLayoutIndex = activeLayoutIndex;
        }

        [JsonIgnore]
        public Layout? ActiveLayout
        {
            get
            {
                if (ActiveLayoutIndex >= 0 && ActiveLayoutIndex < Layouts.Count)
                    return Layouts[ActiveLayoutIndex];
                else
                    return null;
            }
        }

        public Rect? LayoutWindow(Rect? currentDimensions, LayoutAction action)
        {
            if (ActiveLayout is null || ActiveLayout.Dimensions.Count == 0)
                return null;

            MonitorList monitorList = WinAPI.ListMonitors();
            int? currentPosition = PositionInCurrentLayout(currentDimensions, monitorList);
            if (currentPosition is null)
            {
                if (action == LayoutAction.PREVIOUS)
                    action = LayoutAction.FIRST;
                else if (action == LayoutAction.NEXT)
                    action = LayoutAction.LAST;
            }

            int targetPosition = 0;
            switch (action)
            {
                case LayoutAction.FIRST:
                    targetPosition = 0;
                    break;
                case LayoutAction.LAST:
                    targetPosition = ActiveLayout.Dimensions.Count - 1;
                    break;
                case LayoutAction.NEXT:
                    targetPosition = (int)currentPosition! + 1;
                    if (targetPosition >= ActiveLayout.Dimensions.Count)
                        targetPosition -= ActiveLayout.Dimensions.Count;
                    break;
                case LayoutAction.PREVIOUS:
                    targetPosition = (int)currentPosition! - 1;
                    if (targetPosition < 0)
                        targetPosition += ActiveLayout.Dimensions.Count;
                    break;
            }
            return ActiveLayout.Dimensions[targetPosition].ConvertToVirtualScreenCoords(monitorList, Fullscreen);
        }

        private int? PositionInCurrentLayout(Rect? dimensions, MonitorList monitorList)
        {
            if (dimensions is null || ActiveLayout is null)
                return null;

            for (int i = 0; i < ActiveLayout.Dimensions.Count; i++)
            {
                if (ActiveLayout.Dimensions[i].ConvertToVirtualScreenCoords(monitorList, Fullscreen) is Rect layoutDim && dimensions == layoutDim)
                    return i;
            }
            return null;
        }
    }

    public enum LayoutAction
    {
        FIRST, PREVIOUS, NEXT, LAST
    }
}