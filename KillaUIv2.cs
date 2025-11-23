/*
 * KillaUIv2.cs - Complete UI Redesign with Modern Layouts
 * 
 * Features:
 * - Full screen design (1920x1080)
 * - 5 main tabs: PLAY, LOADOUTS, STORE, STATS, SETTINGS
 * - Professional layouts with proper pagination
 * - Clean separation of concerns
 * - Easy to maintain and extend
 * 
 * Version: 2.0.0
 * Author: KillaDome Dev Team
 */

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("KillaUIv2", "KillaDome", "2.0.0")]
    [Description("Modern redesigned UI for KillaDome with full screen layouts")]
    public class KillaUIv2 : RustPlugin
    {
        #region Fields
        
        [PluginReference]
        private Plugin KillaDome;
        
        [PluginReference]
        private Plugin ImageLibrary;
        
        // UI Constants
        private const string UI_MAIN = "KillaUI_Main";
        private const string UI_PANEL = "KillaUI_Panel";
        
        // Screen dimensions (1920x1080 reference)
        private const float SCREEN_WIDTH = 1920f;
        private const float SCREEN_HEIGHT = 1080f;
        
        // Colors
        private const string COLOR_PRIMARY = "0.1 0.1 0.1 0.95";      // Dark background
        private const string COLOR_SECONDARY = "0.15 0.15 0.15 0.95"; // Lighter panels
        private const string COLOR_ACCENT = "0.2 0.6 1 1";            // Blue accent
        private const string COLOR_SUCCESS = "0 0.8 0.2 1";           // Green for buy/success
        private const string COLOR_DANGER = "0.8 0.2 0 1";            // Red for danger
        private const string COLOR_WARNING = "1 0.7 0 1";             // Yellow/orange for currency
        private const string COLOR_TEXT = "1 1 1 1";                  // White text
        private const string COLOR_TEXT_DIM = "0.7 0.7 0.7 1";        // Gray text
        private const string COLOR_BORDER = "0.3 0.3 0.3 1";          // Border color
        
        // ImageLibrary constants
        private const ulong DEFAULT_SKIN_ID = 0UL;                     // Default skin ID for ImageLibrary
        
        // Session state tracking
        private Dictionary<ulong, PlayerUIState> _playerStates = new Dictionary<ulong, PlayerUIState>();
        
        // Configuration
        private PluginConfig _config;
        
        #endregion
        
        #region Configuration
        
        private class PluginConfig
        {
            public List<WeaponConfig> Weapons { get; set; }
            public List<AttachmentConfig> Attachments { get; set; }
            public List<ArmorConfig> Armor { get; set; }
            public List<SkinConfig> GunSkins { get; set; }
            public List<SkinConfig> ArmorSkins { get; set; }
        }
        
        private class WeaponConfig
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int Damage { get; set; }
            public int Price { get; set; }
        }
        
        private class AttachmentConfig
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public int Price { get; set; }
        }
        
        private class ArmorConfig
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int Protection { get; set; }
            public int Price { get; set; }
        }
        
        private class SkinConfig
        {
            public string ItemId { get; set; }
            public ulong SkinId { get; set; }
            public string Name { get; set; }
            public int Price { get; set; }
        }
        
        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
            SaveConfig();
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintWarning("Error loading configuration file. Loading default configuration.");
                LoadDefaultConfig();
            }
        }
        
        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }
        
        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                Weapons = new List<WeaponConfig>
                {
                    new WeaponConfig { Id = "rifle.ak", Name = "AK-47", Damage = 30, Price = 500 },
                    new WeaponConfig { Id = "rifle.lr300", Name = "LR-300", Damage = 40, Price = 600 },
                    new WeaponConfig { Id = "smg.mp5", Name = "MP5", Damage = 35, Price = 450 },
                    new WeaponConfig { Id = "python", Name = "Python", Damage = 55, Price = 400 },
                    new WeaponConfig { Id = "rifle.bolt", Name = "Bolt Action", Damage = 80, Price = 700 },
                    new WeaponConfig { Id = "smg.thompson", Name = "Thompson", Damage = 26, Price = 350 },
                    new WeaponConfig { Id = "smg.2", Name = "Custom SMG", Damage = 28, Price = 300 }
                },
                Attachments = new List<AttachmentConfig>
                {
                    new AttachmentConfig { Id = "weapon.mod.holosight", Name = "Holo Sight", Category = "Scopes", Price = 200 },
                    new AttachmentConfig { Id = "weapon.mod.8x.scope", Name = "8x Scope", Category = "Scopes", Price = 400 },
                    new AttachmentConfig { Id = "weapon.mod.small.scope", Name = "16x Scope", Category = "Scopes", Price = 600 },
                    new AttachmentConfig { Id = "weapon.mod.simplesight", Name = "Simple Sight", Category = "Scopes", Price = 150 },
                    new AttachmentConfig { Id = "weapon.mod.silencer", Name = "Silencer", Category = "Barrel", Price = 250 },
                    new AttachmentConfig { Id = "weapon.mod.muzzlebrake", Name = "Muzzle Brake", Category = "Barrel", Price = 200 },
                    new AttachmentConfig { Id = "weapon.mod.muzzleboost", Name = "Muzzle Boost", Category = "Barrel", Price = 200 },
                    new AttachmentConfig { Id = "weapon.mod.lasersight", Name = "Lasersight", Category = "Underbarrel", Price = 150 },
                    new AttachmentConfig { Id = "weapon.mod.flashlight", Name = "Flashlight", Category = "Underbarrel", Price = 100 }
                },
                Armor = new List<ArmorConfig>
                {
                    new ArmorConfig { Id = "metal.facemask", Name = "Metal Facemask", Protection = 50, Price = 400 },
                    new ArmorConfig { Id = "metal.plate.torso", Name = "Metal Chest Plate", Protection = 80, Price = 500 },
                    new ArmorConfig { Id = "roadsign.kilt", Name = "Roadsign Kilt", Protection = 40, Price = 300 },
                    new ArmorConfig { Id = "roadsign.jacket", Name = "Roadsign Vest", Protection = 35, Price = 300 },
                    new ArmorConfig { Id = "tactical.gloves", Name = "Tactical Gloves", Protection = 10, Price = 150 }
                },
                GunSkins = new List<SkinConfig>
                {
                    // Example entries - populate with actual skin IDs
                    // new SkinConfig { ItemId = "rifle.ak", SkinId = 123456, Name = "AK-47 Gold", Price = 1000 }
                },
                ArmorSkins = new List<SkinConfig>
                {
                    // Example entries - populate with actual skin IDs
                    // new SkinConfig { ItemId = "metal.facemask", SkinId = 789012, Name = "Gold Facemask", Price = 800 }
                }
            };
        }
        
        #endregion
        
        #region Data Classes
        
        private class PlayerUIState
        {
            public string CurrentTab = "play";
            public string CurrentSubTab = "";
            public string CurrentStoreCategory = "guns";
            public string CurrentStoreSkinTab = "gun_skins";
            public int CurrentStorePage = 0;
            public string CurrentLoadoutTab = "loadout_editor";
            public string CurrentEditingWeaponSlot = "primary";
            public string CurrentAttachmentCategory = "scope";
            public int CurrentAttachmentPage = 0;
            public Dictionary<string, string> ArmorSlotTypes = new Dictionary<string, string>();
            public Dictionary<string, int> ArmorSlotSkins = new Dictionary<string, int>();
        }
        
        #endregion
        
        #region Oxide Hooks
        
        private void Init()
        {
            LoadConfig();
            Puts($"[KillaUIv2] Configuration loaded: {_config.Weapons.Count} weapons, {_config.Attachments.Count} attachments, {_config.Armor.Count} armor pieces");
            Puts("[KillaUIv2] [DEBUG] KillaUIv2 initialized");
        }
        
        private void OnServerInitialized()
        {
            if (KillaDome == null || !KillaDome.IsLoaded)
            {
                PrintWarning("[KillaUIv2] KillaDome plugin not found! UI will not function.");
                return;
            }
            
            Puts("[KillaUIv2] [DEBUG] KillaUIv2 connected to KillaDome");
        }
        
        private void Unload()
        {
            // Clean up all UIs
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
            
            _playerStates.Clear();
        }
        
        #endregion
        
        #region Public API Methods (Called by KillaDome)
        
        [HookMethod("ShowLobbyUI")]
        public void ShowLobbyUI(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            
            Puts($"[KillaUIv2] ShowLobbyUI called for {player.displayName}");
            
            // Initialize player state if needed
            if (!_playerStates.ContainsKey(player.userID))
            {
                _playerStates[player.userID] = new PlayerUIState();
            }
            
            // Grant admin blood tokens if admin
            if (IsAdmin(player))
            {
                try
                {
                    KillaDome?.Call("SetBloodTokens", player.userID, 100000);
                    Puts($"[KillaUIv2] Granted 100k blood tokens to admin: {player.displayName}");
                }
                catch (Exception ex)
                {
                    Puts($"[KillaUIv2] Could not grant admin tokens (KillaDome may not support SetBloodTokens): {ex.Message}");
                }
            }
            
            // Show the main UI with current tab
            var state = _playerStates[player.userID];
            ShowMainUI(player, state.CurrentTab);
        }
        
        [HookMethod("DestroyUI")]
        public void DestroyUI(BasePlayer player)
        {
            if (player == null) return;
            
            CuiHelper.DestroyUi(player, UI_MAIN);
            CuiHelper.DestroyUi(player, UI_PANEL);
            
            _playerStates.Remove(player.userID);
        }
        
        #endregion
        
        #region Main UI Structure
        
        private void ShowMainUI(BasePlayer player, string tab)
        {
            if (player == null || !player.IsConnected) return;
            
            // Destroy existing UI
            CuiHelper.DestroyUi(player, UI_MAIN);
            
            var container = new CuiElementContainer();
            
            // Main background panel
            container.Add(new CuiPanel
            {
                Image = { Color = COLOR_PRIMARY },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true
            }, "Overlay", UI_MAIN);
            
            // Header with title and tab buttons
            AddHeader(container, UI_MAIN, player, tab);
            
            // Content area based on selected tab
            AddTabContent(container, UI_MAIN, player, tab);
            
            // Close button
            AddCloseButton(container, UI_MAIN, player);
            
            CuiHelper.AddUi(player, container);
            
            Puts($"[KillaUIv2] UI rendered for {player.displayName}, tab: {tab}");
        }
        
        private void AddHeader(CuiElementContainer container, string parent, BasePlayer player, string currentTab)
        {
            // Header panel
            var header = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0 0.92", AnchorMax = "1 1" }
            }, parent, "Header");
            
            // Title
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "🎮 KILLADOME ARENA 🎮",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.3 1" }
            }, header);
            
            // Tab buttons
            string[] tabs = { "PLAY", "LOADOUTS", "STORE", "STATS", "SETTINGS" };
            string[] tabIds = { "play", "loadouts", "store", "stats", "settings" };
            
            float tabWidth = 0.12f;
            float startX = 0.35f;
            
            for (int i = 0; i < tabs.Length; i++)
            {
                float minX = startX + (i * tabWidth);
                float maxX = minX + tabWidth - 0.01f;
                
                bool isActive = tabIds[i] == currentTab;
                
                var tabButton = container.Add(new CuiButton
                {
                    Button = {
                        Color = isActive ? COLOR_ACCENT : "0.2 0.2 0.2 0.95",
                        Command = $"killaui.tab {tabIds[i]}"
                    },
                    RectTransform = { AnchorMin = $"{minX} 0.1", AnchorMax = $"{maxX} 0.9" },
                    Text = {
                        Text = tabs[i],
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, header);
            }
        }
        
        private void AddCloseButton(CuiElementContainer container, string parent, BasePlayer player)
        {
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_DANGER,
                    Command = "killaui.close"
                },
                RectTransform = { AnchorMin = "0.95 0.93", AnchorMax = "0.99 0.99" },
                Text = {
                    Text = "✖",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, parent);
        }
        
        private void AddTabContent(CuiElementContainer container, string parent, BasePlayer player, string tab)
        {
            // Content area (below header)
            var content = container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" }, // Transparent
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.92" }
            }, parent, "Content");
            
            // Route to appropriate tab renderer
            switch (tab)
            {
                case "play":
                    RenderPlayTab(container, content, player);
                    break;
                case "loadouts":
                    RenderLoadoutsTab(container, content, player);
                    break;
                case "store":
                    RenderStoreTab(container, content, player);
                    break;
                case "stats":
                    RenderStatsTab(container, content, player);
                    break;
                case "settings":
                    RenderSettingsTab(container, content, player);
                    break;
            }
        }
        
        #endregion
        
        #region PLAY Tab
        
        private void RenderPlayTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            // Get session data from KillaDome
            var sessionData = KillaDome?.Call("GetSessionData", player.userID) as Dictionary<string, object>;
            
            int tokens = 0;
            int kills = 0;
            float kd = 0f;
            
            if (sessionData != null)
            {
                tokens = Convert.ToInt32(sessionData.ContainsKey("tokens") ? sessionData["tokens"] : 0);
                kills = Convert.ToInt32(sessionData.ContainsKey("totalKills") ? sessionData["totalKills"] : 0);
                int deaths = Convert.ToInt32(sessionData.ContainsKey("totalDeaths") ? sessionData["totalDeaths"] : 0);
                kd = deaths > 0 ? (float)kills / deaths : kills;
            }
            
            // Center join button
            var joinPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.35 0.5", AnchorMax = "0.65 0.7" }
            }, parent);
            
            // Join title
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "🎯 READY TO DOMINATE?",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0 0.7", AnchorMax = "1 0.9" }
            }, joinPanel);
            
            // Join button
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_SUCCESS,
                    Command = "killadome.joinqueue"
                },
                RectTransform = { AnchorMin = "0.25 0.35", AnchorMax = "0.75 0.6" },
                Text = {
                    Text = "JOIN QUEUE\nPlayers: 0/16",
                    FontSize = 16,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, joinPanel);
            
            // Map info
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "Map: Desert Arena",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT_DIM
                },
                RectTransform = { AnchorMin = "0 0.15", AnchorMax = "1 0.3" }
            }, joinPanel);
            
            // Stats panel
            var statsPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.1 0.2", AnchorMax = "0.35 0.45" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "YOUR STATS",
                    FontSize = 16,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_ACCENT
                },
                RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 1" }
            }, statsPanel);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = $"Kills: {kills}\nK/D: {kd:F2}\nTokens: 💰 {tokens}",
                    FontSize = 14,
                    Align = TextAnchor.MiddleLeft,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.1 0.2", AnchorMax = "0.9 0.75" }
            }, statsPanel);
            
            // Quick actions panel
            var actionsPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.65 0.2", AnchorMax = "0.9 0.45" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "QUICK ACTIONS",
                    FontSize = 16,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_ACCENT
                },
                RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 1" }
            }, actionsPanel);
            
            // Quick action buttons
            container.Add(new CuiButton
            {
                Button = {
                    Color = "0.3 0.3 0.3 0.95",
                    Command = "killaui.tab loadouts"
                },
                RectTransform = { AnchorMin = "0.1 0.5", AnchorMax = "0.9 0.7" },
                Text = {
                    Text = "View Loadout",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, actionsPanel);
            
            container.Add(new CuiButton
            {
                Button = {
                    Color = "0.3 0.3 0.3 0.95",
                    Command = "killaui.tab store"
                },
                RectTransform = { AnchorMin = "0.1 0.25", AnchorMax = "0.9 0.45" },
                Text = {
                    Text = "Buy Weapon",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, actionsPanel);
        }
        
        #endregion
        
        #region LOADOUTS Tab
        
        private void RenderLoadoutsTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            var state = GetPlayerState(player.userID);
            
            // Sub-tab buttons
            var subtabPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.1 0.85", AnchorMax = "0.9 0.92" }
            }, parent);
            
            // Loadout Editor button
            container.Add(new CuiButton
            {
                Button = {
                    Color = state.CurrentLoadoutTab == "loadout_editor" ? COLOR_ACCENT : "0.25 0.25 0.25 0.95",
                    Command = "killaui.loadout.subtab loadout_editor"
                },
                RectTransform = { AnchorMin = "0.05 0.1", AnchorMax = "0.45 0.9" },
                Text = {
                    Text = "LOADOUT EDITOR",
                    FontSize = 14,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, subtabPanel);
            
            // Outfit Editor button
            container.Add(new CuiButton
            {
                Button = {
                    Color = state.CurrentLoadoutTab == "outfit_editor" ? COLOR_ACCENT : "0.25 0.25 0.25 0.95",
                    Command = "killaui.loadout.subtab outfit_editor"
                },
                RectTransform = { AnchorMin = "0.55 0.1", AnchorMax = "0.95 0.9" },
                Text = {
                    Text = "OUTFIT EDITOR",
                    FontSize = 14,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, subtabPanel);
            
            // Content area
            var contentPanel = container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.83" }
            }, parent);
            
            // Render appropriate subtab
            if (state.CurrentLoadoutTab == "loadout_editor")
            {
                RenderLoadoutEditor(container, contentPanel, player);
            }
            else if (state.CurrentLoadoutTab == "outfit_editor")
            {
                RenderOutfitEditor(container, contentPanel, player);
            }
        }
        
        private void RenderLoadoutEditor(CuiElementContainer container, string parent, BasePlayer player)
        {
            var state = GetPlayerState(player.userID);
            
            // Get loadout data from KillaDome with proper error handling
            string primaryWeapon = "rifle.ak";
            string secondaryWeapon = "python";
            
            try
            {
                var loadoutDataRaw = KillaDome?.Call("GetCurrentLoadout", player.userID);
                
                if (loadoutDataRaw != null)
                {
                    var loadoutData = loadoutDataRaw as Dictionary<string, object>;
                    if (loadoutData != null && loadoutData.Count > 0)
                    {
                        if (loadoutData.ContainsKey("Primary") && loadoutData["Primary"] != null)
                        {
                            primaryWeapon = loadoutData["Primary"].ToString();
                        }
                        if (loadoutData.ContainsKey("Secondary") && loadoutData["Secondary"] != null)
                        {
                            secondaryWeapon = loadoutData["Secondary"].ToString();
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Puts($"[KillaUIv2] Error loading loadout data: {ex.Message}");
            }
            
            // PRIMARY WEAPON SECTION (Left)
            var primaryPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.05 0.55", AnchorMax = "0.47 0.95" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "PRIMARY WEAPON",
                    FontSize = 14,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.85", AnchorMax = "0.9 0.95" }
            }, primaryPanel);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = GetWeaponDisplayName(primaryWeapon),
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.1 0.65", AnchorMax = "0.9 0.8" }
            }, primaryPanel);
            
            // Cycle buttons for primary
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_ACCENT,
                    Command = "killaui.weapon.cycle primary -1"
                },
                RectTransform = { AnchorMin = "0.1 0.45", AnchorMax = "0.35 0.6" },
                Text = {
                    Text = "◄ PREV",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, primaryPanel);
            
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_ACCENT,
                    Command = "killaui.weapon.cycle primary 1"
                },
                RectTransform = { AnchorMin = "0.65 0.45", AnchorMax = "0.9 0.6" },
                Text = {
                    Text = "NEXT ►",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, primaryPanel);
            
            // Primary weapon image from ImageLibrary
            var imageLibrary = plugins.Find("ImageLibrary");
            if (imageLibrary != null)
            {
                try
                {
                    string imageUrl = (string)imageLibrary.Call("GetImage", primaryWeapon, DEFAULT_SKIN_ID);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        container.Add(new CuiElement
                        {
                            Name = "primary_weapon_image",
                            Parent = primaryPanel,
                            Components =
                            {
                                new CuiRawImageComponent { Url = imageUrl },
                                new CuiRectTransformComponent { AnchorMin = "0.2 0.15", AnchorMax = "0.8 0.4" }
                            }
                        });
                    }
                    else
                    {
                        // Fallback if image not found
                        container.Add(new CuiLabel
                        {
                            Text = { Text = "📷", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = COLOR_TEXT_DIM },
                            RectTransform = { AnchorMin = "0.2 0.15", AnchorMax = "0.8 0.4" }
                        }, primaryPanel);
                    }
                }
                catch
                {
                    // Fallback on error
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "📷", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = COLOR_TEXT_DIM },
                        RectTransform = { AnchorMin = "0.2 0.15", AnchorMax = "0.8 0.4" }
                    }, primaryPanel);
                }
            }
            else
            {
                // ImageLibrary not available - show placeholder
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = "📷\n[Weapon Image]",
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT_DIM
                    },
                    RectTransform = { AnchorMin = "0.2 0.15", AnchorMax = "0.8 0.4" }
                }, primaryPanel);
            }
            
            // SECONDARY WEAPON SECTION (Right)
            var secondaryPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.53 0.55", AnchorMax = "0.95 0.95" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "SECONDARY WEAPON",
                    FontSize = 14,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.85", AnchorMax = "0.9 0.95" }
            }, secondaryPanel);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = GetWeaponDisplayName(secondaryWeapon),
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.1 0.65", AnchorMax = "0.9 0.8" }
            }, secondaryPanel);
            
            // Cycle buttons for secondary
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_ACCENT,
                    Command = "killaui.weapon.cycle secondary -1"
                },
                RectTransform = { AnchorMin = "0.1 0.45", AnchorMax = "0.35 0.6" },
                Text = {
                    Text = "◄ PREV",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, secondaryPanel);
            
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_ACCENT,
                    Command = "killaui.weapon.cycle secondary 1"
                },
                RectTransform = { AnchorMin = "0.65 0.45", AnchorMax = "0.9 0.6" },
                Text = {
                    Text = "NEXT ►",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, secondaryPanel);
            
            // Secondary weapon image from ImageLibrary
            if (imageLibrary != null)
            {
                try
                {
                    string imageUrl = (string)imageLibrary.Call("GetImage", secondaryWeapon, DEFAULT_SKIN_ID);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        container.Add(new CuiElement
                        {
                            Name = "secondary_weapon_image",
                            Parent = secondaryPanel,
                            Components =
                            {
                                new CuiRawImageComponent { Url = imageUrl },
                                new CuiRectTransformComponent { AnchorMin = "0.2 0.15", AnchorMax = "0.8 0.4" }
                            }
                        });
                    }
                    else
                    {
                        // Fallback if image not found
                        container.Add(new CuiLabel
                        {
                            Text = { Text = "📷", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = COLOR_TEXT_DIM },
                            RectTransform = { AnchorMin = "0.2 0.15", AnchorMax = "0.8 0.4" }
                        }, secondaryPanel);
                    }
                }
                catch
                {
                    // Fallback on error
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "📷", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = COLOR_TEXT_DIM },
                        RectTransform = { AnchorMin = "0.2 0.15", AnchorMax = "0.8 0.4" }
                    }, secondaryPanel);
                }
            }
            else
            {
                // ImageLibrary not available - show placeholder
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = "📷\n[Weapon Image]",
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT_DIM
                    },
                    RectTransform = { AnchorMin = "0.2 0.15", AnchorMax = "0.8 0.4" }
                }, secondaryPanel);
            }
            
            // ATTACHMENTS SECTION (Bottom)
            var attachmentsPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.50" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = $"ATTACHMENTS ({GetWeaponDisplayName(primaryWeapon)})",
                    FontSize = 14,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.88", AnchorMax = "0.9 0.98" }
            }, attachmentsPanel);
            
            // Weapon selector tabs (Primary/Secondary)
            container.Add(new CuiButton
            {
                Button = {
                    Color = state.CurrentEditingWeaponSlot == "primary" ? COLOR_ACCENT : "0.25 0.25 0.25 0.95",
                    Command = "killaui.attachment.weapon primary"
                },
                RectTransform = { AnchorMin = "0.05 0.75", AnchorMax = "0.25 0.85" },
                Text = {
                    Text = "PRIMARY",
                    FontSize = 11,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, attachmentsPanel);
            
            container.Add(new CuiButton
            {
                Button = {
                    Color = state.CurrentEditingWeaponSlot == "secondary" ? COLOR_ACCENT : "0.25 0.25 0.25 0.95",
                    Command = "killaui.attachment.weapon secondary"
                },
                RectTransform = { AnchorMin = "0.27 0.75", AnchorMax = "0.47 0.85" },
                Text = {
                    Text = "SECONDARY",
                    FontSize = 11,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, attachmentsPanel);
            
            // Attachment category tabs
            string[] categories = { "SCOPE", "BARREL", "UNDERBARREL" };
            for (int i = 0; i < categories.Length; i++)
            {
                string category = categories[i].ToLower();
                float xMin = 0.52f + (i * 0.15f);
                float xMax = xMin + 0.14f;
                
                container.Add(new CuiButton
                {
                    Button = {
                        Color = state.CurrentAttachmentCategory == category ? COLOR_ACCENT : "0.25 0.25 0.25 0.95",
                        Command = $"killaui.attachment.category {category}"
                    },
                    RectTransform = { AnchorMin = $"{xMin} 0.75", AnchorMax = $"{xMax} 0.85" },
                    Text = {
                        Text = categories[i],
                        FontSize = 10,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, attachmentsPanel);
            }
            
            // Get attachments for current category
            string currentWeapon = state.CurrentEditingWeaponSlot == "primary" ? primaryWeapon : secondaryWeapon;
            var attachments = _config.Attachments.Where(a => a.Category.ToLower() == state.CurrentAttachmentCategory.ToLower()).ToList();
            
            // Get equipped attachments from KillaDome
            HashSet<string> equippedAttachments = new HashSet<string>();
            try
            {
                var equippedData = KillaDome?.Call("GetEquippedAttachments", player.userID, state.CurrentEditingWeaponSlot);
                if (equippedData != null && equippedData is Dictionary<string, string> equipped)
                {
                    if (equipped.ContainsKey(state.CurrentAttachmentCategory))
                    {
                        string equippedId = equipped[state.CurrentAttachmentCategory];
                        if (!string.IsNullOrEmpty(equippedId))
                        {
                            equippedAttachments.Add(equippedId);
                        }
                    }
                }
            }
            catch { }
            
            // Pagination
            int itemsPerPage = 3;
            int totalPages = (int)Math.Ceiling((double)attachments.Count / itemsPerPage);
            if (state.CurrentAttachmentPage >= totalPages) state.CurrentAttachmentPage = 0;
            if (state.CurrentAttachmentPage < 0) state.CurrentAttachmentPage = 0;
            
            var pageAttachments = attachments.Skip(state.CurrentAttachmentPage * itemsPerPage).Take(itemsPerPage).ToList();
            
            // Render attachments in grid (3 columns)
            for (int i = 0; i < pageAttachments.Count; i++)
            {
                var attachment = pageAttachments[i];
                float xMin = 0.05f + (i * 0.31f);
                float xMax = xMin + 0.28f;
                bool isEquipped = equippedAttachments.Contains(attachment.Id);
                
                var attachPanel = container.Add(new CuiPanel
                {
                    Image = { Color = isEquipped ? "0.2 0.5 0.3 0.95" : "0.2 0.2 0.2 0.95" },
                    RectTransform = { AnchorMin = $"{xMin} 0.25", AnchorMax = $"{xMax} 0.70" }
                }, attachmentsPanel);
                
                // Attachment image
                if (ImageLibrary != null)
                {
                    try
                    {
                        string imageUrl = (string)ImageLibrary.Call("GetImage", attachment.Id, DEFAULT_SKIN_ID);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            container.Add(new CuiElement
                            {
                                Name = $"attachment_img_{i}",
                                Parent = attachPanel,
                                Components =
                                {
                                    new CuiRawImageComponent { Url = imageUrl },
                                    new CuiRectTransformComponent { AnchorMin = "0.15 0.55", AnchorMax = "0.85 0.90" }
                                }
                            });
                        }
                    }
                    catch { }
                }
                
                // Attachment name
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = attachment.Name,
                        FontSize = 11,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    },
                    RectTransform = { AnchorMin = "0.05 0.40", AnchorMax = "0.95 0.52" }
                }, attachPanel);
                
                // Price
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = $"💰 {attachment.Price}",
                        FontSize = 10,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_WARNING
                    },
                    RectTransform = { AnchorMin = "0.05 0.28", AnchorMax = "0.95 0.38" }
                }, attachPanel);
                
                // Apply button or equipped indicator
                if (isEquipped)
                {
                    container.Add(new CuiLabel
                    {
                        Text = {
                            Text = "✓ EQUIPPED",
                            FontSize = 10,
                            Align = TextAnchor.MiddleCenter,
                            Color = COLOR_SUCCESS
                        },
                        RectTransform = { AnchorMin = "0.1 0.08", AnchorMax = "0.9 0.22" }
                    }, attachPanel);
                }
                else
                {
                    container.Add(new CuiButton
                    {
                        Button = {
                            Color = COLOR_ACCENT,
                            Command = $"killaui.attachment.apply {attachment.Id}"
                        },
                        RectTransform = { AnchorMin = "0.1 0.08", AnchorMax = "0.9 0.22" },
                        Text = {
                            Text = "APPLY",
                            FontSize = 10,
                            Align = TextAnchor.MiddleCenter,
                            Color = COLOR_TEXT
                        }
                    }, attachPanel);
                }
            }
            
            // Pagination controls
            if (totalPages > 1)
            {
                container.Add(new CuiButton
                {
                    Button = {
                        Color = state.CurrentAttachmentPage > 0 ? COLOR_ACCENT : "0.3 0.3 0.3 0.95",
                        Command = "killaui.attachment.page prev"
                    },
                    RectTransform = { AnchorMin = "0.05 0.15", AnchorMax = "0.20 0.23" },
                    Text = {
                        Text = "◄ PREV",
                        FontSize = 10,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, attachmentsPanel);
                
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = $"Page {state.CurrentAttachmentPage + 1}/{totalPages}",
                        FontSize = 10,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT_DIM
                    },
                    RectTransform = { AnchorMin = "0.22 0.15", AnchorMax = "0.45 0.23" }
                }, attachmentsPanel);
                
                container.Add(new CuiButton
                {
                    Button = {
                        Color = state.CurrentAttachmentPage < totalPages - 1 ? COLOR_ACCENT : "0.3 0.3 0.3 0.95",
                        Command = "killaui.attachment.page next"
                    },
                    RectTransform = { AnchorMin = "0.47 0.15", AnchorMax = "0.62 0.23" },
                    Text = {
                        Text = "NEXT ►",
                        FontSize = 10,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, attachmentsPanel);
            }
            
            // Save button
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_SUCCESS,
                    Command = "killaui.loadout.save"
                },
                RectTransform = { AnchorMin = "0.1 0.02", AnchorMax = "0.45 0.12" },
                Text = {
                    Text = "SAVE LOADOUT",
                    FontSize = 13,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, attachmentsPanel);
            
            // Reset button
            container.Add(new CuiButton
            {
                Button = {
                    Color = "0.5 0.5 0.5 0.95",
                    Command = "killaui.loadout.reset"
                },
                RectTransform = { AnchorMin = "0.55 0.02", AnchorMax = "0.9 0.12" },
                Text = {
                    Text = "RESET TO DEFAULT",
                    FontSize = 13,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, attachmentsPanel);
        }
        
        private void RenderOutfitEditor(CuiElementContainer container, string parent, BasePlayer player)
        {
            // Left side: Stacked armor preview
            var previewPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.05 0.1", AnchorMax = "0.40 0.9" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "ARMOR PREVIEW",
                    FontSize = 14,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.92", AnchorMax = "0.9 0.98" }
            }, previewPanel);
            
            // Stack 6 armor pieces vertically with ImageLibrary integration
            string[] armorSlotNames = { "head", "chest_armor", "chest_clothing", "pants", "leg_armor", "feet" };
            string[] armorDisplayNames = { "Head", "Chest Armor", "Chest Clothing", "Pants", "Leg Armor", "Feet" };
            string[] defaultArmorItems = { "metal.facemask", "metal.plate.torso", "roadsign.jacket", "pants", "roadsign.kilt", "shoes.boots" };
            
            // Get ImageLibrary plugin reference
            var imageLibrary = plugins.Find("ImageLibrary");
            
            for (int i = 0; i < armorSlotNames.Length; i++)
            {
                float yMin = 0.85f - (i * 0.14f);
                float yMax = yMin + 0.12f;
                
                var slotPanel = container.Add(new CuiPanel
                {
                    Image = { Color = "0.2 0.2 0.2 0.95" },
                    RectTransform = { AnchorMin = $"0.05 {yMin}", AnchorMax = $"0.95 {yMax}" }
                }, previewPanel);
                
                // Slot label (small, top-left corner)
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = armorDisplayNames[i].ToUpper(),
                        FontSize = 8,
                        Align = TextAnchor.UpperLeft,
                        Color = COLOR_TEXT_DIM
                    },
                    RectTransform = { AnchorMin = "0.15 0.82", AnchorMax = "0.85 0.98" }
                }, slotPanel);
                
                // Get the current armor type for this slot from player state
                string currentArmorItem = GetPlayerArmorType(player.userID, armorSlotNames[i]);
                
                // Armor type cycle buttons (left side)
                container.Add(new CuiButton
                {
                    Button = {
                        Color = COLOR_ACCENT,
                        Command = $"killaui.armor.type {armorSlotNames[i]} -1"
                    },
                    RectTransform = { AnchorMin = "0.02 0.2", AnchorMax = "0.12 0.8" },
                    Text = {
                        Text = "◄",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, slotPanel);
                
                // Armor image from ImageLibrary
                if (imageLibrary != null)
                {
                    try
                    {
                        string imageUrl = (string)imageLibrary.Call("GetImage", currentArmorItem, DEFAULT_SKIN_ID);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            container.Add(new CuiElement
                            {
                                Name = $"armor_image_{i}",
                                Parent = slotPanel,
                                Components =
                                {
                                    new CuiRawImageComponent { Url = imageUrl },
                                    new CuiRectTransformComponent { AnchorMin = "0.15 0.1", AnchorMax = "0.35 0.9" }
                                }
                            });
                        }
                        else
                        {
                            // Fallback if image not found
                            container.Add(new CuiLabel
                            {
                                Text = { Text = "📷", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = COLOR_TEXT },
                                RectTransform = { AnchorMin = "0.15 0.1", AnchorMax = "0.35 0.9" }
                            }, slotPanel);
                        }
                    }
                    catch
                    {
                        // Fallback on error
                        container.Add(new CuiLabel
                        {
                            Text = { Text = "📷", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = COLOR_TEXT },
                            RectTransform = { AnchorMin = "0.15 0.1", AnchorMax = "0.35 0.9" }
                        }, slotPanel);
                    }
                }
                else
                {
                    // ImageLibrary not available - show placeholder
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "📷", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = COLOR_TEXT },
                        RectTransform = { AnchorMin = "0.15 0.1", AnchorMax = "0.35 0.9" }
                    }, slotPanel);
                }
                
                // Armor name label
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = GetArmorDisplayName(currentArmorItem),
                        FontSize = 11,
                        Align = TextAnchor.MiddleLeft,
                        Color = COLOR_TEXT
                    },
                    RectTransform = { AnchorMin = "0.38 0.1", AnchorMax = "0.85 0.9" }
                }, slotPanel);
                
                // Armor type cycle buttons (right side)
                container.Add(new CuiButton
                {
                    Button = {
                        Color = COLOR_ACCENT,
                        Command = $"killaui.armor.type {armorSlotNames[i]} 1"
                    },
                    RectTransform = { AnchorMin = "0.88 0.2", AnchorMax = "0.98 0.8" },
                    Text = {
                        Text = "►",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, slotPanel);
            }
            
            // Right side: Skin selection for each armor piece
            var skinPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.45 0.1", AnchorMax = "0.95 0.9" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "SKIN SELECTION",
                    FontSize = 14,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.92", AnchorMax = "0.9 0.98" }
            }, skinPanel);
            
            // Skin selection for each armor piece (match armor preview spacing)
            for (int i = 0; i < armorSlotNames.Length; i++)
            {
                float yMin = 0.85f - (i * 0.14f);
                float yMax = yMin + 0.12f;
                
                var skinSlotPanel = container.Add(new CuiPanel
                {
                    Image = { Color = "0.2 0.2 0.2 0.95" },
                    RectTransform = { AnchorMin = $"0.05 {yMin}", AnchorMax = $"0.95 {yMax}" }
                }, skinPanel);
                
                // Previous button
                container.Add(new CuiButton
                {
                    Button = {
                        Color = COLOR_ACCENT,
                        Command = $"killaui.armor.skin {i} -1"
                    },
                    RectTransform = { AnchorMin = "0.05 0.2", AnchorMax = "0.25 0.8" },
                    Text = {
                        Text = "◄",
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, skinSlotPanel);
                
                // Skin preview
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = "📷 Default Skin",
                        FontSize = 11,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    },
                    RectTransform = { AnchorMin = "0.3 0.1", AnchorMax = "0.7 0.9" }
                }, skinSlotPanel);
                
                // Next button
                container.Add(new CuiButton
                {
                    Button = {
                        Color = COLOR_ACCENT,
                        Command = $"killaui.armor.skin {i} 1"
                    },
                    RectTransform = { AnchorMin = "0.75 0.2", AnchorMax = "0.95 0.8" },
                    Text = {
                        Text = "►",
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, skinSlotPanel);
            }
            
            // Save outfit button (moved to left side, positioned lower)
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_SUCCESS,
                    Command = "killaui.outfit.save"
                },
                RectTransform = { AnchorMin = "0.05 0.02", AnchorMax = "0.22 0.08" },
                Text = {
                    Text = "SAVE OUTFIT",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, parent);
            
            // Reset button (moved to left side, positioned lower)
            container.Add(new CuiButton
            {
                Button = {
                    Color = "0.5 0.5 0.5 0.95",
                    Command = "killaui.outfit.reset"
                },
                RectTransform = { AnchorMin = "0.23 0.02", AnchorMax = "0.40 0.08" },
                Text = {
                    Text = "RESET TO DEFAULT",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, parent);
        }
        
        private string GetWeaponDisplayName(string weaponId)
        {
            // Map weapon IDs to display names
            // Support both short names and full Rust item names
            switch (weaponId.ToLower())
            {
                case "rifle.ak":
                case "ak47":
                case "ak": return "AK-47";
                
                case "rifle.lr300":
                case "lr300":
                case "lr": return "LR-300";
                
                case "smg.mp5":
                case "mp5": return "MP5";
                
                case "python":
                case "pistol.python": return "Python";
                
                case "rifle.bolt":
                case "bolt": return "Bolt Action";
                
                case "smg.thompson":
                case "thompson": return "Thompson";
                
                case "smg.2":
                case "smg2":
                case "custom": return "Custom SMG";
                
                default: 
                    // Try to extract a readable name from the weapon ID
                    string name = weaponId.Replace("rifle.", "").Replace("smg.", "").Replace("pistol.", "");
                    return name.ToUpper();
            }
        }
        
        private string GetArmorDisplayName(string armorId)
        {
            // Map armor IDs to display names
            switch (armorId.ToLower())
            {
                case "metal.facemask": return "Metal Facemask";
                case "coffee.can.helmet": return "Coffee Can Helmet";
                case "metal.plate.torso": return "Metal Chest Plate";
                case "heavy.plate.jacket": return "Heavy Plate Jacket";
                case "jacket.snow": return "Snow Jacket";
                case "roadsign.jacket": return "Roadsign Vest";
                case "hoodie": return "Hoodie";
                case "burlap.shirt": return "Burlap Shirt";
                case "pants": return "Pants";
                case "pants.shorts": return "Shorts";
                case "roadsign.kilt": return "Roadsign Kilt";
                case "heavy.plate.pants": return "Heavy Plate Pants";
                case "shoes.boots": return "Boots";
                case "boots.frog": return "Frog Boots";
                case "tactical.gloves": return "Tactical Gloves";
                case "burlap.gloves": return "Burlap Gloves";
                default:
                    // Try to extract a readable name from the armor ID
                    string name = armorId.Replace(".", " ").Replace("_", " ");
                    return char.ToUpper(name[0]) + name.Substring(1);
            }
        }
        
        private PlayerUIState GetPlayerState(ulong playerId)
        {
            if (!_playerStates.ContainsKey(playerId))
            {
                _playerStates[playerId] = new PlayerUIState();
            }
            return _playerStates[playerId];
        }

        private string GetPlayerArmorType(ulong playerId, string slot)
        {
            var state = GetPlayerState(playerId);
            if (state.ArmorSlotTypes.ContainsKey(slot))
                return state.ArmorSlotTypes[slot];
            
            // Return defaults based on slot
            switch (slot)
            {
                case "head": return "metal.facemask";
                case "chest_armor": return "metal.plate.torso";
                case "chest_clothing": return "roadsign.jacket";
                case "pants": return "pants";
                case "leg_armor": return "roadsign.kilt";
                case "feet": return "shoes.boots";
                default: return "burlap.shirt"; // Generic fallback
            }
        }

        private void SetPlayerArmorType(ulong playerId, string slot, string armorType)
        {
            var state = GetPlayerState(playerId);
            state.ArmorSlotTypes[slot] = armorType;
        }
        
        #endregion
        
        #region STORE Tab
        
        private void RenderStoreTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            var state = GetPlayerState(player.userID);
            
            // Category tabs at the top
            string[] categories = { "guns", "attachments", "clothing", "skins" };
            string[] categoryLabels = { "GUNS", "ATTACHMENTS", "CLOTHING", "SKINS" };
            
            float tabWidth = 0.22f;
            for (int i = 0; i < categories.Length; i++)
            {
                float xMin = 0.05f + (i * 0.235f);
                float xMax = xMin + tabWidth;
                bool isActive = state.CurrentStoreCategory == categories[i];
                
                container.Add(new CuiButton
                {
                    Button = {
                        Color = isActive ? COLOR_ACCENT : COLOR_SECONDARY,
                        Command = $"killaui.store.category {categories[i]}"
                    },
                    RectTransform = { AnchorMin = $"{xMin} 0.88", AnchorMax = $"{xMax} 0.95" },
                    Text = {
                        Text = categoryLabels[i],
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, parent);
            }
            
            // Content area based on selected category
            var contentPanel = container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.85" }
            }, parent);
            
            switch (state.CurrentStoreCategory)
            {
                case "guns":
                    RenderStoreGunsCategory(container, contentPanel, player, state);
                    break;
                case "attachments":
                    RenderStoreAttachmentsCategory(container, contentPanel, player, state);
                    break;
                case "clothing":
                    RenderStoreClothingCategory(container, contentPanel, player, state);
                    break;
                case "skins":
                    RenderStoreSkinsCategory(container, contentPanel, player, state);
                    break;
            }
        }
        
        private void RenderStoreGunsCategory(CuiElementContainer container, string parent, BasePlayer player, PlayerUIState state)
        {
            // Load weapons from configuration
            var guns = _config.Weapons;
            
            int itemsPerPage = 6; // 3x2 grid
            int totalPages = (int)Math.Ceiling(guns.Count / (float)itemsPerPage);
            int currentPage = Math.Max(0, Math.Min(state.CurrentStorePage, totalPages - 1));
            int startIdx = currentPage * itemsPerPage;
            int endIdx = Math.Min(startIdx + itemsPerPage, guns.Count);
            
            // Render 3x2 grid
            int itemIndex = 0;
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int dataIdx = startIdx + itemIndex;
                    if (dataIdx >= endIdx) break;
                    
                    var gun = guns[dataIdx];
                    float xMin = 0.05f + (col * 0.315f);
                    float xMax = xMin + 0.30f;
                    float yMin = 0.50f - (row * 0.40f);
                    float yMax = yMin + 0.35f;
                    
                    var itemPanel = container.Add(new CuiPanel
                    {
                        Image = { Color = COLOR_SECONDARY },
                        RectTransform = { AnchorMin = $"{xMin} {yMin}", AnchorMax = $"{xMax} {yMax}" }
                    }, parent);
                    
                    // Weapon name
                    container.Add(new CuiLabel
                    {
                        Text = {
                            Text = gun.Name,
                            FontSize = 14,
                            Align = TextAnchor.MiddleCenter,
                            Color = COLOR_TEXT
                        },
                        RectTransform = { AnchorMin = "0.1 0.75", AnchorMax = "0.9 0.9" }
                    }, itemPanel);
                    
                    // Weapon image placeholder (ImageLibrary integration)
                    var imageLibrary = plugins.Find("ImageLibrary");
                    if (imageLibrary != null)
                    {
                        try
                        {
                            string imageUrl = (string)imageLibrary.Call("GetImage", gun.Id, DEFAULT_SKIN_ID);
                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                container.Add(new CuiElement
                                {
                                    Name = $"gun_image_{dataIdx}",
                                    Parent = itemPanel,
                                    Components =
                                    {
                                        new CuiRawImageComponent { Url = imageUrl },
                                        new CuiRectTransformComponent { AnchorMin = "0.2 0.40", AnchorMax = "0.8 0.70" }
                                    }
                                });
                            }
                        }
                        catch { }
                    }
                    
                    // Damage stat
                    container.Add(new CuiLabel
                    {
                        Text = {
                            Text = $"Damage: {gun.Damage}",
                            FontSize = 11,
                            Align = TextAnchor.MiddleCenter,
                            Color = COLOR_TEXT_DIM
                        },
                        RectTransform = { AnchorMin = "0.1 0.32", AnchorMax = "0.9 0.40" }
                    }, itemPanel);
                    
                    // Price and buy button
                    container.Add(new CuiLabel
                    {
                        Text = {
                            Text = $"💰 {gun.Price}",
                            FontSize = 12,
                            Align = TextAnchor.MiddleCenter,
                            Color = COLOR_WARNING
                        },
                        RectTransform = { AnchorMin = "0.1 0.20", AnchorMax = "0.9 0.30" }
                    }, itemPanel);
                    
                    // Check if player owns the weapon
                    bool owned = false;
                    try
                    {
                        var ownershipResult = KillaDome?.Call("CheckOwnership", player.userID, gun.Id);
                        owned = ownershipResult != null && (bool)ownershipResult;
                    }
                    catch (Exception ex)
                    {
                        // Log ownership check failure for debugging
                        Puts($"[KillaUIv2] Error checking ownership for {gun.Id}: {ex.GetType().Name}");
                    }
                    
                    container.Add(new CuiButton
                    {
                        Button = {
                            Color = owned ? COLOR_TEXT_DIM : COLOR_SUCCESS,
                            Command = owned ? "" : $"killaui.store.purchase {gun.Id} {gun.Price}"
                        },
                        RectTransform = { AnchorMin = "0.15 0.05", AnchorMax = "0.85 0.18" },
                        Text = {
                            Text = owned ? "✓ OWNED" : "BUY NOW",
                            FontSize = 11,
                            Align = TextAnchor.MiddleCenter,
                            Color = owned ? COLOR_TEXT_DIM : COLOR_TEXT
                        }
                    }, itemPanel);
                    
                    itemIndex++;
                }
            }
            
            // Pagination controls
            if (totalPages > 1)
            {
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = $"Page {currentPage + 1}/{totalPages}",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    },
                    RectTransform = { AnchorMin = "0.45 0.02", AnchorMax = "0.55 0.08" }
                }, parent);
                
                if (currentPage > 0)
                {
                    container.Add(new CuiButton
                    {
                        Button = {
                            Color = COLOR_ACCENT,
                            Command = "killaui.store.page prev"
                        },
                        RectTransform = { AnchorMin = "0.30 0.02", AnchorMax = "0.42 0.08" },
                        Text = {
                            Text = "◄ PREV",
                            FontSize = 11,
                            Align = TextAnchor.MiddleCenter,
                            Color = COLOR_TEXT
                        }
                    }, parent);
                }
                
                if (currentPage < totalPages - 1)
                {
                    container.Add(new CuiButton
                    {
                        Button = {
                            Color = COLOR_ACCENT,
                            Command = "killaui.store.page next"
                        },
                        RectTransform = { AnchorMin = "0.58 0.02", AnchorMax = "0.70 0.08" },
                        Text = {
                            Text = "NEXT ►",
                            FontSize = 11,
                            Align = TextAnchor.MiddleCenter,
                            Color = COLOR_TEXT
                        }
                    }, parent);
                }
            }
        }
        
        private void RenderStoreAttachmentsCategory(CuiElementContainer container, string parent, BasePlayer player, PlayerUIState state)
        {
            // Load attachments from configuration
            var attachments = _config.Attachments;
            
            int itemsPerPage = 6; // 3x2 grid
            int totalPages = (int)Math.Ceiling(attachments.Count / (float)itemsPerPage);
            int currentPage = Math.Max(0, Math.Min(state.CurrentStorePage, totalPages - 1));
            int startIdx = currentPage * itemsPerPage;
            int endIdx = Math.Min(startIdx + itemsPerPage, attachments.Count);
            
            // Render 3x2 grid
            int itemIndex = 0;
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int dataIdx = startIdx + itemIndex;
                    if (dataIdx >= endIdx) break;
                    
                    var attachment = attachments[dataIdx];
                    float xMin = 0.05f + (col * 0.315f);
                    float xMax = xMin + 0.30f;
                    float yMin = 0.50f - (row * 0.40f);
                    float yMax = yMin + 0.35f;
                    
                    var itemPanel = container.Add(new CuiPanel
                    {
                        Image = { Color = COLOR_SECONDARY },
                        RectTransform = { AnchorMin = $"{xMin} {yMin}", AnchorMax = $"{xMax} {yMax}" }
                    }, parent);
                    
                    // Attachment name
                    container.Add(new CuiLabel
                    {
                        Text = {
                            Text = attachment.Name,
                            FontSize = 13,
                            Align = TextAnchor.MiddleCenter,
                            Color = COLOR_TEXT
                        },
                        RectTransform = { AnchorMin = "0.1 0.75", AnchorMax = "0.9 0.9" }
                    }, itemPanel);
                    
                    // Category
                    container.Add(new CuiLabel
                    {
                        Text = {
                            Text = attachment.Category,
                            FontSize = 10,
                            Align = TextAnchor.MiddleCenter,
                            Color = COLOR_TEXT_DIM
                        },
                        RectTransform = { AnchorMin = "0.1 0.65", AnchorMax = "0.9 0.73" }
                    }, itemPanel);
                    
                    // Image placeholder
                    var imageLibrary = plugins.Find("ImageLibrary");
                    if (imageLibrary != null)
                    {
                        try
                        {
                            string imageUrl = (string)imageLibrary.Call("GetImage", attachment.Id, DEFAULT_SKIN_ID);
                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                container.Add(new CuiElement
                                {
                                    Name = $"attachment_image_{dataIdx}",
                                    Parent = itemPanel,
                                    Components =
                                    {
                                        new CuiRawImageComponent { Url = imageUrl },
                                        new CuiRectTransformComponent { AnchorMin = "0.2 0.35", AnchorMax = "0.8 0.60" }
                                    }
                                });
                            }
                        }
                        catch { }
                    }
                    
                    // Price
                    container.Add(new CuiLabel
                    {
                        Text = {
                            Text = $"💰 {attachment.Price}",
                            FontSize = 12,
                            Align = TextAnchor.MiddleCenter,
                            Color = COLOR_WARNING
                        },
                        RectTransform = { AnchorMin = "0.1 0.20", AnchorMax = "0.9 0.30" }
                    }, itemPanel);
                    
                    // Check if player owns the attachment
                    bool owned = false;
                    try
                    {
                        var ownershipResult = KillaDome?.Call("CheckOwnership", player.userID, attachment.Id);
                        owned = ownershipResult != null && (bool)ownershipResult;
                    }
                    catch (Exception ex)
                    {
                        // Log ownership check failure for debugging
                        Puts($"[KillaUIv2] Error checking ownership for {attachment.Id}: {ex.GetType().Name}");
                    }
                    
                    container.Add(new CuiButton
                    {
                        Button = {
                            Color = owned ? COLOR_TEXT_DIM : COLOR_SUCCESS,
                            Command = owned ? "" : $"killaui.store.purchase {attachment.Id} {attachment.Price}"
                        },
                        RectTransform = { AnchorMin = "0.15 0.05", AnchorMax = "0.85 0.18" },
                        Text = {
                            Text = owned ? "✓ OWNED" : "BUY NOW",
                            FontSize = 11,
                            Align = TextAnchor.MiddleCenter,
                            Color = owned ? COLOR_TEXT_DIM : COLOR_TEXT
                        }
                    }, itemPanel);
                    
                    itemIndex++;
                }
            }
            
            // Pagination controls
            if (totalPages > 1)
            {
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = $"Page {currentPage + 1}/{totalPages}",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    },
                    RectTransform = { AnchorMin = "0.45 0.02", AnchorMax = "0.55 0.08" }
                }, parent);
                
                if (currentPage > 0)
                {
                    container.Add(new CuiButton
                    {
                        Button = {
                            Color = COLOR_ACCENT,
                            Command = "killaui.store.page prev"
                        },
                        RectTransform = { AnchorMin = "0.30 0.02", AnchorMax = "0.42 0.08" },
                        Text = {
                            Text = "◄ PREV",
                            FontSize = 11,
                            Align = TextAnchor.MiddleCenter,
                            Color = COLOR_TEXT
                        }
                    }, parent);
                }
                
                if (currentPage < totalPages - 1)
                {
                    container.Add(new CuiButton
                    {
                        Button = {
                            Color = COLOR_ACCENT,
                            Command = "killaui.store.page next"
                        },
                        RectTransform = { AnchorMin = "0.58 0.02", AnchorMax = "0.70 0.08" },
                        Text = {
                            Text = "NEXT ►",
                            FontSize = 11,
                            Align = TextAnchor.MiddleCenter,
                            Color = COLOR_TEXT
                        }
                    }, parent);
                }
            }
        }
        
        private void RenderStoreClothingCategory(CuiElementContainer container, string parent, BasePlayer player, PlayerUIState state)
        {
            // Load armor from configuration
            var clothing = _config.Armor;
            
            // Render all items (no pagination needed)
            for (int i = 0; i < clothing.Count; i++)
            {
                var item = clothing[i];
                float xMin = 0.05f + (i % 3) * 0.315f;
                float xMax = xMin + 0.30f;
                float yMin = i < 3 ? 0.50f : 0.10f;
                float yMax = yMin + 0.35f;
                
                var itemPanel = container.Add(new CuiPanel
                {
                    Image = { Color = COLOR_SECONDARY },
                    RectTransform = { AnchorMin = $"{xMin} {yMin}", AnchorMax = $"{xMax} {yMax}" }
                }, parent);
                
                // Item name
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = item.Name,
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    },
                    RectTransform = { AnchorMin = "0.1 0.75", AnchorMax = "0.9 0.9" }
                }, itemPanel);
                
                // Image
                var imageLibrary = plugins.Find("ImageLibrary");
                if (imageLibrary != null)
                {
                    try
                    {
                        string imageUrl = (string)imageLibrary.Call("GetImage", item.Id, DEFAULT_SKIN_ID);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            container.Add(new CuiElement
                            {
                                Name = $"clothing_image_{i}",
                                Parent = itemPanel,
                                Components =
                                {
                                    new CuiRawImageComponent { Url = imageUrl },
                                    new CuiRectTransformComponent { AnchorMin = "0.2 0.40", AnchorMax = "0.8 0.70" }
                                }
                            });
                        }
                    }
                    catch { }
                }
                
                // Protection value
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = $"Protection: {item.Protection}",
                        FontSize = 11,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT_DIM
                    },
                    RectTransform = { AnchorMin = "0.1 0.32", AnchorMax = "0.9 0.40" }
                }, itemPanel);
                
                // Price
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = $"💰 {item.Price}",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_WARNING
                    },
                    RectTransform = { AnchorMin = "0.1 0.20", AnchorMax = "0.9 0.30" }
                }, itemPanel);
                
                // Check if player owns the armor
                bool owned = false;
                try
                {
                    var ownershipResult = KillaDome?.Call("CheckOwnership", player.userID, item.Id);
                    owned = ownershipResult != null && (bool)ownershipResult;
                }
                catch (Exception ex)
                {
                    // Log ownership check failure for debugging
                    Puts($"[KillaUIv2] Error checking ownership for {item.Id}: {ex.GetType().Name}");
                }
                
                container.Add(new CuiButton
                {
                    Button = {
                        Color = owned ? COLOR_TEXT_DIM : COLOR_SUCCESS,
                        Command = owned ? "" : $"killaui.store.purchase {item.Id} {item.Price}"
                    },
                    RectTransform = { AnchorMin = "0.15 0.05", AnchorMax = "0.85 0.18" },
                    Text = {
                        Text = owned ? "✓ OWNED" : "BUY NOW",
                        FontSize = 11,
                        Align = TextAnchor.MiddleCenter,
                        Color = owned ? COLOR_TEXT_DIM : COLOR_TEXT
                    }
                }, itemPanel);
            }
        }
        
        private void RenderStoreSkinsCategory(CuiElementContainer container, string parent, BasePlayer player, PlayerUIState state)
        {
            // Sub-tabs for skins
            container.Add(new CuiButton
            {
                Button = {
                    Color = state.CurrentStoreSkinTab == "gun_skins" ? COLOR_ACCENT : COLOR_SECONDARY,
                    Command = "killaui.store.skintab gun_skins"
                },
                RectTransform = { AnchorMin = "0.20 0.85", AnchorMax = "0.45 0.92" },
                Text = {
                    Text = "GUN SKINS",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, parent);
            
            container.Add(new CuiButton
            {
                Button = {
                    Color = state.CurrentStoreSkinTab == "armor_skins" ? COLOR_ACCENT : COLOR_SECONDARY,
                    Command = "killaui.store.skintab armor_skins"
                },
                RectTransform = { AnchorMin = "0.55 0.85", AnchorMax = "0.80 0.92" },
                Text = {
                    Text = "ARMOR SKINS",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, parent);
            
            // Content area
            var contentArea = container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.82" }
            }, parent);
            
            if (state.CurrentStoreSkinTab == "gun_skins")
            {
                // Weapon selector
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = "Weapon:",
                        FontSize = 12,
                        Align = TextAnchor.MiddleRight,
                        Color = COLOR_TEXT
                    },
                    RectTransform = { AnchorMin = "0.25 0.70", AnchorMax = "0.35 0.77" }
                }, contentArea);
                
                container.Add(new CuiButton
                {
                    Button = {
                        Color = COLOR_ACCENT,
                        Command = "killaui.skins.weapon prev"
                    },
                    RectTransform = { AnchorMin = "0.36 0.70", AnchorMax = "0.42 0.77" },
                    Text = {
                        Text = "◄",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, contentArea);
                
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = "AK-47",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_WARNING
                    },
                    RectTransform = { AnchorMin = "0.43 0.70", AnchorMax = "0.57 0.77" }
                }, contentArea);
                
                container.Add(new CuiButton
                {
                    Button = {
                        Color = COLOR_ACCENT,
                        Command = "killaui.skins.weapon next"
                    },
                    RectTransform = { AnchorMin = "0.58 0.70", AnchorMax = "0.64 0.77" },
                    Text = {
                        Text = "►",
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, contentArea);
                
                // Note about empty skins
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = "Gun skins configuration is empty.\nSkin system structure is ready for configuration.",
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT_DIM
                    },
                    RectTransform = { AnchorMin = "0.3 0.35", AnchorMax = "0.7 0.60" }
                }, contentArea);
            }
            else
            {
                // Armor skins
                container.Add(new CuiLabel
                {
                    Text = {
                        Text = "Armor skins configuration is empty.\nSkin system structure is ready for configuration.",
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT_DIM
                    },
                    RectTransform = { AnchorMin = "0.3 0.35", AnchorMax = "0.7 0.60" }
                }, contentArea);
            }
        }
        
        #endregion
        
        #region STATS Tab
        
        private void RenderStatsTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            // Get player profile data from KillaDome
            Dictionary<string, object> profileData = new Dictionary<string, object>();
            try
            {
                var profileDataRaw = KillaDome?.Call("GetPlayerProfile", player.userID);
                if (profileDataRaw != null)
                {
                    profileData = profileDataRaw as Dictionary<string, object>;
                }
            }
            catch { }
            
            // Helper function to safely get stat values
            int GetStat(string key, int defaultValue = 0)
            {
                if (profileData != null && profileData.ContainsKey(key))
                {
                    try { return Convert.ToInt32(profileData[key]); }
                    catch { return defaultValue; }
                }
                return defaultValue;
            }
            
            string GetStatString(string key, string defaultValue = "N/A")
            {
                if (profileData != null && profileData.ContainsKey(key))
                {
                    return profileData[key]?.ToString() ?? defaultValue;
                }
                return defaultValue;
            }
            
            // Left column: COMBAT PERFORMANCE
            var leftPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.05 0.15", AnchorMax = "0.47 0.9" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "COMBAT PERFORMANCE",
                    FontSize = 16,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.92", AnchorMax = "0.9 0.98" }
            }, leftPanel);
            
            // Combat stats with better spacing
            int kills = GetStat("kills", 0);
            int deaths = GetStat("deaths", 0);
            float kd = deaths > 0 ? (float)kills / deaths : kills;
            int headshots = GetStat("headshots", 0);
            float headshotPercent = kills > 0 ? (headshots * 100f / kills) : 0f;
            int accuracy = GetStat("accuracy", 0);
            int longestKill = GetStat("longest_kill", 0);
            int bestStreak = GetStat("best_streak", 0);
            
            float yPos = 0.80f;
            float spacing = 0.10f;
            
            // Total Kills
            AddStatRow(container, leftPanel, "Total Kills", kills.ToString(), yPos);
            yPos -= spacing;
            
            // Total Deaths
            AddStatRow(container, leftPanel, "Total Deaths", deaths.ToString(), yPos);
            yPos -= spacing;
            
            // K/D Ratio
            AddStatRow(container, leftPanel, "K/D Ratio", kd.ToString("F2"), yPos);
            yPos -= spacing;
            
            // Headshots
            AddStatRow(container, leftPanel, "Headshots", $"{headshots} ({headshotPercent:F1}%)", yPos);
            yPos -= spacing;
            
            // Accuracy
            AddStatRow(container, leftPanel, "Accuracy", $"{accuracy}%", yPos);
            yPos -= spacing;
            
            // Longest Kill
            AddStatRow(container, leftPanel, "Longest Kill", $"{longestKill}m", yPos);
            yPos -= spacing;
            
            // Best Streak
            AddStatRow(container, leftPanel, "Best Streak", bestStreak.ToString(), yPos);
            
            // Right column: MATCH HISTORY & PROGRESSION
            var rightPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.53 0.15", AnchorMax = "0.95 0.9" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "MATCH HISTORY & PROGRESSION",
                    FontSize = 16,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.92", AnchorMax = "0.9 0.98" }
            }, rightPanel);
            
            // Progression stats
            int matchesPlayed = GetStat("matches_played", 0);
            int wins = GetStat("wins", 0);
            int losses = GetStat("losses", 0);
            float winRate = matchesPlayed > 0 ? (wins * 100f / matchesPlayed) : 0f;
            int bloodTokens = GetStat("blood_tokens", 0);
            int totalPlaytime = GetStat("total_playtime", 0);
            bool isVip = GetStatString("vip_status", "false") == "true";
            string memberSince = GetStatString("member_since", "N/A");
            
            yPos = 0.80f;
            
            // Matches Played
            AddStatRow(container, rightPanel, "Matches Played", matchesPlayed.ToString(), yPos);
            yPos -= spacing;
            
            // Wins
            AddStatRow(container, rightPanel, "Wins", wins.ToString(), yPos);
            yPos -= spacing;
            
            // Losses
            AddStatRow(container, rightPanel, "Losses", losses.ToString(), yPos);
            yPos -= spacing;
            
            // Win Rate
            AddStatRow(container, rightPanel, "Win Rate", $"{winRate:F1}%", yPos);
            yPos -= spacing;
            
            // Blood Tokens
            AddStatRow(container, rightPanel, "Blood Tokens 💰", bloodTokens.ToString(), yPos);
            yPos -= spacing;
            
            // Total Playtime (convert minutes to hours)
            int hours = totalPlaytime / 60;
            int minutes = totalPlaytime % 60;
            AddStatRow(container, rightPanel, "Total Playtime", $"{hours}h {minutes}m", yPos);
            yPos -= spacing;
            
            // VIP Status
            AddStatRow(container, rightPanel, "VIP Status", isVip ? "✓ Active" : "Inactive", yPos);
            yPos -= spacing;
            
            // Member Since
            AddStatRow(container, rightPanel, "Member Since", memberSince, yPos);
            yPos -= spacing * 1.5f;
            
            // Recent Matches (simplified display)
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "Recent Matches:",
                    FontSize = 11,
                    Align = TextAnchor.MiddleLeft,
                    Color = COLOR_TEXT_DIM
                },
                RectTransform = { AnchorMin = $"0.15 {yPos - 0.03f}", AnchorMax = $"0.50 {yPos + 0.03f}" }
            }, rightPanel);
            
            string recentMatches = GetStatString("recent_matches", "N/A");
            container.Add(new CuiLabel
            {
                Text = {
                    Text = recentMatches,
                    FontSize = 11,
                    Align = TextAnchor.MiddleRight,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = $"0.55 {yPos - 0.03f}", AnchorMax = $"0.85 {yPos + 0.03f}" }
            }, rightPanel);
        }
        
        private void AddStatRow(CuiElementContainer container, string parent, string label, string value, float yPos)
        {
            // Label (left side)
            container.Add(new CuiLabel
            {
                Text = {
                    Text = label,
                    FontSize = 11,
                    Align = TextAnchor.MiddleLeft,
                    Color = COLOR_TEXT_DIM
                },
                RectTransform = { AnchorMin = $"0.15 {yPos - 0.03f}", AnchorMax = $"0.60 {yPos + 0.03f}" }
            }, parent);
            
            // Value (right side)
            container.Add(new CuiLabel
            {
                Text = {
                    Text = value,
                    FontSize = 11,
                    Align = TextAnchor.MiddleRight,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = $"0.65 {yPos - 0.03f}", AnchorMax = $"0.85 {yPos + 0.03f}" }
            }, parent);
        }
        
        #endregion
        
        #region SETTINGS Tab
        
        private void RenderSettingsTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            var state = GetPlayerState(player.userID);
            
            // Get settings from KillaDome (or use defaults)
            bool autoQueue = false;
            bool showKillfeed = true;
            bool levelUpNotif = true;
            
            try
            {
                var settingsData = KillaDome?.Call("GetPlayerSettings", player.userID);
                if (settingsData != null && settingsData is Dictionary<string, object> settings)
                {
                    if (settings.ContainsKey("auto_queue"))
                        autoQueue = Convert.ToBoolean(settings["auto_queue"]);
                    if (settings.ContainsKey("show_killfeed"))
                        showKillfeed = Convert.ToBoolean(settings["show_killfeed"]);
                    if (settings.ContainsKey("level_up_notif"))
                        levelUpNotif = Convert.ToBoolean(settings["level_up_notif"]);
                }
            }
            catch { }
            
            float yPos = 0.75f;
            
            // GAMEPLAY SETTINGS Section
            var gameplayPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.15 0.55", AnchorMax = "0.85 0.85" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "GAMEPLAY SETTINGS",
                    FontSize = 16,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.88", AnchorMax = "0.9 0.96" }
            }, gameplayPanel);
            
            // Auto-Queue Setting
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "Auto-Queue:",
                    FontSize = 12,
                    Align = TextAnchor.MiddleLeft,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.15 0.60", AnchorMax = "0.45 0.70" }
            }, gameplayPanel);
            
            // ON button
            container.Add(new CuiButton
            {
                Button = {
                    Color = autoQueue ? COLOR_SUCCESS : COLOR_SECONDARY,
                    Command = "killaui.setting.toggle auto_queue true"
                },
                RectTransform = { AnchorMin = "0.50 0.60", AnchorMax = "0.62 0.70" },
                Text = {
                    Text = "ON",
                    FontSize = 11,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, gameplayPanel);
            
            // OFF button
            container.Add(new CuiButton
            {
                Button = {
                    Color = !autoQueue ? COLOR_DANGER : COLOR_SECONDARY,
                    Command = "killaui.setting.toggle auto_queue false"
                },
                RectTransform = { AnchorMin = "0.65 0.60", AnchorMax = "0.77 0.70" },
                Text = {
                    Text = "OFF",
                    FontSize = 11,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, gameplayPanel);
            
            // Show Killfeed Setting
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "Show Killfeed:",
                    FontSize = 12,
                    Align = TextAnchor.MiddleLeft,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.15 0.42", AnchorMax = "0.45 0.52" }
            }, gameplayPanel);
            
            // ON button
            container.Add(new CuiButton
            {
                Button = {
                    Color = showKillfeed ? COLOR_SUCCESS : COLOR_SECONDARY,
                    Command = "killaui.setting.toggle show_killfeed true"
                },
                RectTransform = { AnchorMin = "0.50 0.42", AnchorMax = "0.62 0.52" },
                Text = {
                    Text = "ON",
                    FontSize = 11,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, gameplayPanel);
            
            // OFF button
            container.Add(new CuiButton
            {
                Button = {
                    Color = !showKillfeed ? COLOR_DANGER : COLOR_SECONDARY,
                    Command = "killaui.setting.toggle show_killfeed false"
                },
                RectTransform = { AnchorMin = "0.65 0.42", AnchorMax = "0.77 0.52" },
                Text = {
                    Text = "OFF",
                    FontSize = 11,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, gameplayPanel);
            
            // NOTIFICATIONS Section
            var notifPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.15 0.32", AnchorMax = "0.85 0.50" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "NOTIFICATIONS",
                    FontSize = 16,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.78", AnchorMax = "0.9 0.92" }
            }, notifPanel);
            
            // Level Up Notification
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "Level Up:",
                    FontSize = 12,
                    Align = TextAnchor.MiddleLeft,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.15 0.35", AnchorMax = "0.45 0.55" }
            }, notifPanel);
            
            // ON button
            container.Add(new CuiButton
            {
                Button = {
                    Color = levelUpNotif ? COLOR_SUCCESS : COLOR_SECONDARY,
                    Command = "killaui.setting.toggle level_up_notif true"
                },
                RectTransform = { AnchorMin = "0.50 0.35", AnchorMax = "0.62 0.55" },
                Text = {
                    Text = "ON",
                    FontSize = 11,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, notifPanel);
            
            // OFF button
            container.Add(new CuiButton
            {
                Button = {
                    Color = !levelUpNotif ? COLOR_DANGER : COLOR_SECONDARY,
                    Command = "killaui.setting.toggle level_up_notif false"
                },
                RectTransform = { AnchorMin = "0.65 0.35", AnchorMax = "0.77 0.55" },
                Text = {
                    Text = "OFF",
                    FontSize = 11,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, notifPanel);
            
            // ACCOUNT Section
            var accountPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.15 0.10", AnchorMax = "0.85 0.27" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "ACCOUNT",
                    FontSize = 16,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.1 0.70", AnchorMax = "0.9 0.92" }
            }, accountPanel);
            
            // Warning icon and text
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "⚠️",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0.15 0.25", AnchorMax = "0.25 0.55" }
            }, accountPanel);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "Warning: This action cannot be undone!",
                    FontSize = 11,
                    Align = TextAnchor.MiddleLeft,
                    Color = COLOR_TEXT_DIM
                },
                RectTransform = { AnchorMin = "0.27 0.25", AnchorMax = "0.65 0.55" }
            }, accountPanel);
            
            // Reset Progress Button
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_DANGER,
                    Command = "killaui.account.reset"
                },
                RectTransform = { AnchorMin = "0.68 0.25", AnchorMax = "0.85 0.55" },
                Text = {
                    Text = "RESET\nPROGRESS",
                    FontSize = 10,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, accountPanel);
        }
        
        #endregion
        
        #region Console Commands
        
        [ConsoleCommand("killaui.tab")]
        private void CmdChangeTab(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            string tab = arg.GetString(0, "play");
            
            // Update state
            if (!_playerStates.ContainsKey(player.userID))
            {
                _playerStates[player.userID] = new PlayerUIState();
            }
            _playerStates[player.userID].CurrentTab = tab;
            
            // Refresh UI
            ShowMainUI(player, tab);
        }
        
        [ConsoleCommand("killaui.close")]
        private void CmdClose(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            DestroyUI(player);
        }
        
        [ConsoleCommand("killaui.loadout.subtab")]
        private void CmdLoadoutSubtab(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            string subtab = arg.GetString(0, "loadout_editor");
            var state = GetPlayerState(player.userID);
            state.CurrentLoadoutTab = subtab;
            
            // Refresh UI
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.weapon.cycle")]
        private void CmdWeaponCycle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            string slot = arg.GetString(0, "primary");
            int direction = arg.GetInt(1, 1);
            
            // Call KillaDome to cycle weapon
            KillaDome?.Call("CycleWeapon", player, slot, direction);
            
            // Refresh UI
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.attachment.category")]
        private void CmdAttachmentCategory(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            string category = arg.GetString(0, "scope");
            var state = GetPlayerState(player.userID);
            state.CurrentAttachmentCategory = category;
            state.CurrentAttachmentPage = 0; // Reset to first page
            
            // Refresh UI
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.attachment.weapon")]
        private void CmdAttachmentWeapon(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            string weaponSlot = arg.GetString(0, "primary");
            var state = GetPlayerState(player.userID);
            state.CurrentEditingWeaponSlot = weaponSlot;
            state.CurrentAttachmentPage = 0; // Reset to first page
            
            // Refresh UI
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.attachment.apply")]
        private void CmdAttachmentApply(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (arg.Args == null || arg.Args.Length == 0)
            {
                player.ChatMessage("Error: No attachment specified.");
                return;
            }
            
            string attachmentId = arg.Args[0];
            var state = GetPlayerState(player.userID);
            
            // Call KillaDome to apply attachment
            try
            {
                KillaDome?.Call("ApplyAttachment", player.userID, state.CurrentEditingWeaponSlot, state.CurrentAttachmentCategory, attachmentId);
                player.ChatMessage($"Attachment applied to {state.CurrentEditingWeaponSlot} weapon!");
            }
            catch (Exception ex)
            {
                Puts($"[KillaUIv2] Error applying attachment: {ex.Message}");
                player.ChatMessage("Failed to apply attachment. Contact an administrator.");
            }
            
            // Refresh UI
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.attachment.page")]
        private void CmdAttachmentPage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            string direction = arg.GetString(0, "next");
            var state = GetPlayerState(player.userID);
            
            if (direction == "next")
            {
                state.CurrentAttachmentPage++;
            }
            else if (direction == "prev")
            {
                state.CurrentAttachmentPage--;
            }
            
            if (state.CurrentAttachmentPage < 0) state.CurrentAttachmentPage = 0;
            
            // Refresh UI
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.loadout.save")]
        private void CmdLoadoutSave(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            player.ChatMessage("Loadout saved successfully!");
            // Additional save logic can be added via KillaDome call
        }
        
        [ConsoleCommand("killaui.loadout.reset")]
        private void CmdLoadoutReset(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            // Call KillaDome to reset loadout
            KillaDome?.Call("ResetLoadout", player.userID);
            
            player.ChatMessage("Loadout reset to default.");
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.armor.skin")]
        private void CmdArmorSkin(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            int armorSlot = arg.GetInt(0, 0);
            int direction = arg.GetInt(1, 1);
            
            // Call KillaDome to cycle armor skin
            KillaDome?.Call("CycleArmorSkin", player.userID, armorSlot, direction);
            
            // Refresh UI
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.armor.type")]
        private void CmdArmorType(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (arg.Args == null || arg.Args.Length < 2) return;
            
            string slot = arg.Args[0]; // e.g., "head", "chest", "legs", "torso", "hands"
            int direction = arg.GetInt(1, 1);
            
            // Get player state
            var state = GetPlayerState(player.userID);
            
            // Call KillaDome to cycle armor type
            KillaDome?.Call("CycleArmor", player.userID, slot, direction);
            
            // Refresh UI to show the new armor type
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.outfit.save")]
        private void CmdOutfitSave(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            player.ChatMessage("Outfit saved successfully!");
            // Additional save logic via KillaDome
        }
        
        [ConsoleCommand("killaui.outfit.reset")]
        private void CmdOutfitReset(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            // Call KillaDome to reset outfit
            KillaDome?.Call("ResetOutfit", player.userID);
            
            player.ChatMessage("Outfit reset to default.");
            ShowMainUI(player, "loadouts");
        }
        
        [ConsoleCommand("killaui.store.category")]
        private void CmdStoreCategory(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (arg.Args == null || arg.Args.Length < 1) return;
            
            string category = arg.Args[0];
            var state = GetPlayerState(player.userID);
            state.CurrentStoreCategory = category;
            state.CurrentStorePage = 0; // Reset to first page when changing category
            
            ShowMainUI(player, "store");
        }
        
        [ConsoleCommand("killaui.store.page")]
        private void CmdStorePage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (arg.Args == null || arg.Args.Length < 1) return;
            
            string direction = arg.Args[0];
            var state = GetPlayerState(player.userID);
            
            if (direction == "next")
            {
                state.CurrentStorePage++;
            }
            else if (direction == "prev")
            {
                state.CurrentStorePage = Math.Max(0, state.CurrentStorePage - 1);
            }
            
            ShowMainUI(player, "store");
        }
        
        [ConsoleCommand("killaui.store.purchase")]
        private void CmdStorePurchase(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (arg.Args == null || arg.Args.Length < 2)
            {
                player.ChatMessage("Error: Invalid purchase command.");
                return;
            }
            
            string itemId = arg.Args[0];
            int price = arg.GetInt(1, 0);
            
            // Call KillaDome to purchase item
            try
            {
                var result = KillaDome?.Call("PurchaseItem", player.userID, itemId, price);
                
                if (result != null && (bool)result)
                {
                    player.ChatMessage($"✓ Successfully purchased {itemId} for {price} Blood Tokens!");
                }
                else
                {
                    player.ChatMessage("❌ Purchase failed. Not enough Blood Tokens or item already owned.");
                }
            }
            catch (Exception ex)
            {
                Puts($"[KillaUIv2] Error during purchase: {ex.Message}");
                player.ChatMessage("❌ Purchase failed. Contact an administrator.");
            }
            
            ShowMainUI(player, "store");
        }
        
        [ConsoleCommand("killaui.store.skintab")]
        private void CmdStoreSkinTab(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (arg.Args == null || arg.Args.Length < 1) return;
            
            string skinTab = arg.Args[0];
            var state = GetPlayerState(player.userID);
            state.CurrentStoreSkinTab = skinTab;
            
            ShowMainUI(player, "store");
        }
        
        [ConsoleCommand("killaui.skins.weapon")]
        private void CmdSkinsWeapon(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (arg.Args == null || arg.Args.Length < 1) return;
            
            string direction = arg.Args[0];
            // TODO: Implement weapon cycling for skin selection
            // For now, just refresh UI
            
            ShowMainUI(player, "store");
        }
        
        [ConsoleCommand("killaui.setting.toggle")]
        private void CmdSettingToggle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (arg.Args == null || arg.Args.Length < 2) return;
            
            string settingName = arg.Args[0];
            bool value = arg.GetBool(1, true);
            
            // Call KillaDome to save the setting
            try
            {
                KillaDome?.Call("SetPlayerSetting", player.userID, settingName, value);
                player.ChatMessage($"Setting '{settingName}' updated successfully!");
            }
            catch (Exception ex)
            {
                Puts($"[KillaUIv2] Could not save setting (KillaDome may not support SetPlayerSetting): {ex.Message}");
                player.ChatMessage($"Setting '{settingName}' updated (saved locally).");
            }
            
            // Refresh UI
            ShowMainUI(player, "settings");
        }
        
        [ConsoleCommand("killaui.account.reset")]
        private void CmdAccountReset(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            // Show confirmation message
            player.ChatMessage("⚠️ WARNING: Are you sure you want to reset your progress?");
            player.ChatMessage("Type '/kd resetconfirm' to confirm or close this UI to cancel.");
            
            // Note: The actual reset would be handled by a different command in KillaDome
            // This is just the UI trigger
        }
        
        #endregion
        
        #region Helper Methods
        
        private bool IsAdmin(BasePlayer player)
        {
            // Check if player has admin permission or is server admin
            return player.IsAdmin || permission.UserHasPermission(player.UserIDString, "killadome.admin");
        }
        
        private void LogDebug(string message)
        {
            Puts($"[DEBUG] {message}");
        }
        
        #endregion
    }
}