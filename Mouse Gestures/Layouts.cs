using System.Collections.Generic;
using System.Text.Json.Serialization;
using WMG.Core;

namespace WMG.Reactions
{
    public struct WindowDimensions
    {
        [JsonInclude]
        public byte MonitorID;

        // If this is false -> X, Y, Width, Height are relevant. They are absolute coordinates on the screen of the given monitor
        // If this is true -> Rx, Ry, Rwidth, Rheight are relevant. They are relative coordinates, i.e., percentage values 0 <= p <= 1, on the screen of the given monitor
        [JsonInclude]
        public bool RelativeCoordinates;

        [JsonInclude]
        public int X, Y, Width, Height;
        [JsonInclude]
        public double Rx, Ry, Rwidth, Rheight;

        public WindowDimensions(byte monitorID, int x, int y, int width, int height)
        {
            this.MonitorID = monitorID;
            this.RelativeCoordinates = false;
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;

            this.Rx = 0;
            this.Ry = 0;
            this.Rwidth = 0;
            this.Rheight = 0;
        }

        public WindowDimensions(byte monitorID, double rx, double ry, double rwidth, double rheight)
        {
            this.MonitorID = monitorID;
            this.RelativeCoordinates = true;
            this.Rx = rx;
            this.Ry = ry;
            this.Rwidth = rwidth;
            this.Rheight = rheight;

            this.X = 0;
            this.Y = 0;
            this.Width = 0;
            this.Height = 0;
        }

        public Rect? ConvertToVirtualScreenCoords(List<WinAPI.MonitorInformation> monitorList, bool fullscreen = false)
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

    public class Layout
    {
        public string Name { get; set; }

        public List<WindowDimensions> Dimensions { get; set; } = new List<WindowDimensions>();
    }

    public class LayoutManager
    {
        // TODO make private, add methods to this class?
        [JsonInclude]
        public readonly List<Layout> Layouts = new List<Layout>();
        public bool Fullscreen { get; set; } = false;

        [JsonConstructor]
        public LayoutManager(List<Layout> layouts, bool fullscreen)
        {
            Layouts = layouts;
            Fullscreen = fullscreen;
        }

        public LayoutManager() { }

        private int activeLayoutIndex = 0;
        [JsonIgnore]
        public int ActiveLayoutIndex
        {
            get => activeLayoutIndex;
            set
            {
                if (value >= 0 && value < Layouts.Count)
                {
                    activeLayoutIndex = value;
                }
            }
        }

        [JsonIgnore]
        public Layout CurrentLayout => Layouts[ActiveLayoutIndex];

        public Rect? LayoutWindow(Rect? currentDimensions, LayoutAction action)
        {
            List<WinAPI.MonitorInformation> monitorList = WinAPI.ListMonitors();
            int? currentPosition = positionInCurrentLayout(currentDimensions, monitorList);
            if (currentPosition == null)
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
                    targetPosition = CurrentLayout.Dimensions.Count - 1;
                    break;
                case LayoutAction.NEXT:
                    targetPosition = (int)currentPosition + 1;
                    break;
                case LayoutAction.PREVIOUS:
                    targetPosition = (int)currentPosition - 1;
                    break;
            }
            if (targetPosition < 0)
                targetPosition += CurrentLayout.Dimensions.Count;
            else if (targetPosition >= CurrentLayout.Dimensions.Count)
                targetPosition -= CurrentLayout.Dimensions.Count;

            return CurrentLayout.Dimensions[targetPosition].ConvertToVirtualScreenCoords(monitorList, Fullscreen);
        }

        private int? positionInCurrentLayout(Rect? dimensions, List<WinAPI.MonitorInformation> monitorList)
        {
            for (int i = 0; i < CurrentLayout.Dimensions.Count; i++)
            {
                if (dimensions is Rect currentDim &&
                    CurrentLayout.Dimensions[i].ConvertToVirtualScreenCoords(monitorList, Fullscreen) is Rect layoutDim &&
                    currentDim == layoutDim)
                {
                    return i;
                }
            }
            return null;
        }
    }

    public enum LayoutAction
    {
        FIRST, PREVIOUS, NEXT, LAST
    }
}