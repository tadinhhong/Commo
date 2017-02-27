using EloBuddy;

namespace LeagueSharp.Common
{
    /// <summary>
    ///     Adds hacks to the menu.
    /// </summary>
    internal class Hacks
    {
        #region Constants

        private const int WM_KEYDOWN = 0x100;

        private const int WM_KEYUP = 0x101;

        #endregion

        #region Static Fields

        private static Menu menu;

        private static MenuItem MenuAntiAfk;

        private static MenuItem MenuDisableDrawings;

        private static MenuItem MenuDisableSay;

        private static MenuItem MenuTowerRange;

        public static bool AntiAFK
        {
            get
            {
                return EloBuddy.Hacks.AntiAFK;
            }
            set
            {
                if (value == EloBuddy.Hacks.AntiAFK)
                {
                    return;
                }

                EloBuddy.Hacks.AntiAFK = value;
            }
        }

        public static bool DisableDrawings
        {
            get
            {
                return EloBuddy.Hacks.DisableDrawings;
            }
            set
            {
                if (value == EloBuddy.Hacks.DisableDrawings)
                {
                    return;
                }

                EloBuddy.Hacks.DisableDrawings = value;
            }
        }

        public static bool DisableSay
        {
            get
            {
                return EloBuddy.Hacks.IngameChat;
            }
            set
            {
                if (value == EloBuddy.Hacks.IngameChat)
                {
                    return;
                }

                EloBuddy.Hacks.IngameChat = value;
            }
        }

        public static bool TowerRanges
        {
            get
            {
                return EloBuddy.Hacks.TowerRanges;
            }
            set
            {
                if (value == EloBuddy.Hacks.TowerRanges)
                {
                    return;
                }

                EloBuddy.Hacks.TowerRanges = value;
            }
        }

        private static MenuItem DisableDrawing { get; set; }

        #endregion

        #region Public Methods and Operators

        public static void Shutdown()
        {
            Menu.Remove(menu);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        internal static void Initialize()
        {
            CustomEvents.Game.OnGameLoad += eventArgs =>
                {
                    menu = new Menu("Hacks", "Hacks");

                    MenuAntiAfk = menu.AddItem(new MenuItem("AfkHack", "Anti-AFK").SetValue(false));
                    MenuAntiAfk.ValueChanged += (sender, args) => AntiAFK = args.GetNewValue<bool>();

                    MenuDisableDrawings = menu.AddItem(new MenuItem("DrawingHack", "Disable Drawing").SetValue(true));
                    MenuDisableDrawings.ValueChanged += (sender, args) => DisableDrawings = args.GetNewValue<bool>();
                    MenuDisableDrawings.SetValue(DisableDrawings);

                    MenuDisableSay = menu.AddItem(new MenuItem("SayHack", "Disable L# Send Chat").SetValue(false).SetTooltip("Block Game.Say from Assemblies"));
                    MenuDisableSay.ValueChanged += (sender, args) => DisableSay = args.GetNewValue<bool>();

                    MenuTowerRange = menu.AddItem(new MenuItem("TowerHack", "Show Tower Ranges").SetValue(false));
                    MenuTowerRange.ValueChanged += (sender, args) => TowerRanges = args.GetNewValue<bool>();

                    AntiAFK = MenuAntiAfk.GetValue<bool>();
                    DisableDrawings = MenuDisableDrawings.GetValue<bool>();
                    DisableSay = MenuDisableSay.GetValue<bool>();
                    TowerRanges = MenuTowerRange.GetValue<bool>();

                    CommonMenu.Instance.AddSubMenu(menu);

                    Game.OnWndProc += args =>
                        {
                            if (!MenuDisableDrawings.GetValue<bool>())
                            {
                                return;
                            }

                            if ((int)args.WParam != Config.ShowMenuPressKey)
                            {
                                return;
                            }

                            if (args.Msg == WM_KEYDOWN)
                            {
                                EloBuddy.Hacks.DisableDrawings = false;
                            }

                            if (args.Msg == WM_KEYUP)
                            {
                                EloBuddy.Hacks.DisableDrawings = true;
                            }
                        };
                };
        }

        #endregion
    }
}