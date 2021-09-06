namespace WMG.Core
{
    /*
     * TODO
     */
    public sealed class Settings
    {
        private Settings() { }

        public delegate void SettingsUpdateHandler();
        public static event SettingsUpdateHandler OnChange;

        public static bool Disabled { get => false; }

        public static int WiggleRoomSq { get => 625; }
    }
}
