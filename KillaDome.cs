/*
 * KillaDome.cs - Full COD-Style Rust Server Experience Plugin
 * 
 * Features:
 * - Lobby system with comprehensive UI (Play/Loadouts/Store/Stats tabs)
 * - Drag-and-drop loadout editor with image library
 * - Persistent weapon progression and attachment upgrades
 * - Custom VFX/SFX for bullets and attachments
 * - Store integration (Tebex-compatible)
 * - High performance, GC-friendly architecture
 * 
 * Version: 1.0.0
 * Author: KillaDome Dev Team
 */

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

namespace Oxide.Plugins
{
    [Info("KillaDome", "KillaDome", "1.0.0")]
    [Description("Full COD-style server experience with lobby, loadouts, and progression")]
    public class KillaDome : RustPlugin
    {
        #region Fields
        
        [PluginReference]
        private Plugin ImageLibrary;
        
        // DEPRECATED: KillaUI v1 is no longer used. Use KillaUIv2 instead.
        // Kept for backward compatibility only. Will be removed in future version.
        [PluginReference]
        private Plugin KillaUI;
        
        [PluginReference]
        private Plugin KillaUIv2;
        
        private DomeManager _domeManager;
        private LoadoutEditor _loadoutEditor;
        private AttachmentSystem _attachmentSystem;
        private WeaponProgression _weaponProgression;
        private VFXManager _vfxManager;
        private SFXManager _sfxManager;
        private ForgeStationSystem _forgeStation;
        private BloodTokenEconomy _tokenEconomy;
        private StoreAPI _storeAPI;
        private SaveManager _saveManager;
        private AntiExploit _antiExploit;
        private TelemetrySystem _telemetry;
        
        private PluginConfig _config;
        private GunConfig _gunConfig;
        private OutfitConfig _outfitConfig;
        private Dictionary<ulong, PlayerSession> _activeSessions = new Dictionary<ulong, PlayerSession>();
        
        private const string PERMISSION_ADMIN = "killadome.admin";
        private const string PERMISSION_VIP = "killadome.vip";
        
        #endregion
        
        #region Gun & Image Configuration
        
        /// <summary>
        /// CENTRALIZED GUN AND IMAGE CONFIGURATION
        /// This is the ONLY place you need to add/edit guns and their images!
        /// 
        /// AUTOMATIC FEATURES:
        /// - When you add a new gun to Guns dictionary, it automatically appears in the Loadout Tab
        /// - When you add a new skin to Skins list, it automatically appears in the Store Tab
        /// - No need to edit any other code - everything updates automatically!
        /// 
        /// Changes here automatically apply to both Store Tab and Loadout Tab.
        /// </summary>
        public class GunConfig
        {
            // ===== GUNS CONFIGURATION =====
            // Add or modify guns here. Each gun needs:
            // - Id: Internal identifier (lowercase, no spaces)
            // - DisplayName: Name shown to players
            // - RustItemShortname: The actual Rust item shortname
            // - ImageUrl: Direct URL to the gun's image
            
            public Dictionary<string, GunDefinition> Guns = new Dictionary<string, GunDefinition>
            {
                ["ak47"] = new GunDefinition
                {
                    Id = "ak47",
                    DisplayName = "AK-47",
                    RustItemShortname = "rifle.ak",
                    ImageUrl = "https://i.imgur.com/YourAK47Image.png"
                },
                ["lr300"] = new GunDefinition
                {
                    Id = "lr300",
                    DisplayName = "LR-300",
                    RustItemShortname = "rifle.lr300",
                    ImageUrl = "https://i.imgur.com/YourLR300Image.png"
                },
                ["m249"] = new GunDefinition
                {
                    Id = "m249",
                    DisplayName = "M249",
                    RustItemShortname = "lmg.m249",
                    ImageUrl = "https://i.imgur.com/YourM249Image.png"
                },
                ["mp5"] = new GunDefinition
                {
                    Id = "mp5",
                    DisplayName = "MP5A4",
                    RustItemShortname = "smg.mp5",
                    ImageUrl = "https://i.imgur.com/YourMP5Image.png"
                },
                ["thompson"] = new GunDefinition
                {
                    Id = "thompson",
                    DisplayName = "Thompson",
                    RustItemShortname = "smg.thompson",
                    ImageUrl = "https://i.imgur.com/YourThompsonImage.png"
                },
                ["python"] = new GunDefinition
                {
                    Id = "python",
                    DisplayName = "Python Revolver",
                    RustItemShortname = "pistol.python",
                    ImageUrl = "https://i.imgur.com/YourPythonImage.png"
                },
                ["bolt"] = new GunDefinition
                {
                    Id = "bolt",
                    DisplayName = "Bolt Action Rifle",
                    RustItemShortname = "rifle.bolt",
                    ImageUrl = "https://i.imgur.com/YourBoltImage.png"
                },
                ["sarpistol"] = new GunDefinition
                {
                    Id = "sarpistol",
                    DisplayName = "Semi-Auto Pistol",
                    RustItemShortname = "pistol.semiauto",
                    ImageUrl = "https://i.imgur.com/YourSARImage.png"
                },
                ["custom"] = new GunDefinition
                {
                    Id = "custom",
                    DisplayName = "Custom SMG",
                    RustItemShortname = "smg.2",
                    ImageUrl = "https://i.imgur.com/YourCustomImage.png"
                },
                ["m39"] = new GunDefinition
                {
                    Id = "m39",
                    DisplayName = "M39 Rifle",
                    RustItemShortname = "rifle.m39",
                    ImageUrl = "https://i.imgur.com/YourM39Image.png"
                }
            };
            
            // ===== SKINS CONFIGURATION =====
            // Add or modify weapon skins here. Each skin needs:
            // - Name: Display name for the skin
            // - SkinId: Rust workshop skin ID or custom identifier
            // - WeaponId: Which gun this skin is for (must match a gun Id above)
            // - ImageUrl: Direct URL to the skin preview image
            // - Cost: Price in Blood Tokens (default: 300)
            // - Tag: Optional badge like "NEW", "POPULAR", "HOT" (default: "")
            // - Rarity: Rarity tier like "Common", "Rare", "Epic", "Legendary" (default: "Common")
            
            public List<SkinDefinition> Skins = new List<SkinDefinition>
            {
                // AK-47 Skins
                new SkinDefinition
                {
                    Name = "AK-47 Tempered",
                    SkinId = "3602286295",
                    WeaponId = "ak47",
                    ImageUrl = "https://i.imgur.com/YourAK47TemperedSkin.png",
                    Cost = 650,
                    Tag = "NEW",
                    Rarity = "Legendary"
                },
                new SkinDefinition
                {
                    Name = "AK-47 Neon",
                    SkinId = "3102802323",
                    WeaponId = "ak47",
                    ImageUrl = "https://i.imgur.com/YourAK47NeonSkin.png",
                    Cost = 500,
                    Tag = "POPULAR",
                    Rarity = "Epic"
                },
                new SkinDefinition
                {
                    Name = "AK-47 Classic",
                    SkinId = "skin_ak47_classic",
                    WeaponId = "ak47",
                    ImageUrl = "https://i.imgur.com/YourAK47ClassicSkin.png",
                    Cost = 400,
                    Tag = "",
                    Rarity = "Rare"
                },
                
                // M249 Skins
                new SkinDefinition
                {
                    Name = "M249 Chrome",
                    SkinId = "skin_m249_chrome",
                    WeaponId = "m249",
                    ImageUrl = "https://i.imgur.com/YourM249ChromeSkin.png",
                    Cost = 450,
                    Tag = "",
                    Rarity = "Epic"
                },
                
                // Python Skins
                new SkinDefinition
                {
                    Name = "Python Black",
                    SkinId = "skin_python_black",
                    WeaponId = "python",
                    ImageUrl = "https://i.imgur.com/YourPythonBlackSkin.png",
                    Cost = 250,
                    Tag = "",
                    Rarity = "Common"
                },
                
                // LR-300 Skins
                new SkinDefinition
                {
                    Name = "LR-300 Gold",
                    SkinId = "skin_lr300_gold",
                    WeaponId = "lr300",
                    ImageUrl = "https://i.imgur.com/YourLR300GoldSkin.png",
                    Cost = 600,
                    Tag = "",
                    Rarity = "Legendary"
                },
                
                // MP5 Skins
                new SkinDefinition
                {
                    Name = "MP5 Tactical",
                    SkinId = "skin_mp5_tactical",
                    WeaponId = "mp5",
                    ImageUrl = "https://i.imgur.com/YourMP5TacticalSkin.png",
                    Cost = 350,
                    Tag = "",
                    Rarity = "Rare"
                },
                
                // Example: Add a new skin here and it will automatically appear in Store Tab!
                new SkinDefinition
                {
                    Name = "Thompson Dragon",
                    SkinId = "skin_thompson_dragon",
                    WeaponId = "thompson",
                    ImageUrl = "https://i.imgur.com/YourThompsonDragonSkin.png",
                    Cost = 550,
                    Tag = "HOT",
                    Rarity = "Epic"
                }
            };
            
            // ===== HELPER METHODS =====
            
            public string GetGunImageUrl(string gunId)
            {
                return Guns.ContainsKey(gunId) ? Guns[gunId].ImageUrl : "";
            }
            
            public string GetSkinImageUrl(string skinId)
            {
                var skin = Skins.FirstOrDefault(s => s.SkinId == skinId);
                return skin?.ImageUrl ?? "";
            }
            
            public string[] GetAllGunIds()
            {
                return Guns.Keys.ToArray();
            }
            
            public SkinDefinition[] GetSkinsForWeapon(string weaponId)
            {
                return Skins.Where(s => s.WeaponId == weaponId).ToArray();
            }
        }
        
        public class GunDefinition
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string RustItemShortname { get; set; }
            public string ImageUrl { get; set; }
        }
        
        public class SkinDefinition
        {
            public string Name { get; set; }
            public string SkinId { get; set; }
            public string WeaponId { get; set; }
            public string ImageUrl { get; set; }
            public int Cost { get; set; } = 300; // Default cost
            public string Tag { get; set; } = ""; // Optional tag like "NEW", "POPULAR", etc.
            public string Rarity { get; set; } = "Common"; // Rarity tier
        }
        
        // ===== OUTFIT/ARMOR CONFIGURATION =====
        public class OutfitConfig
        {
            public List<ArmorItem> Armors = new List<ArmorItem>
            {
                // Head Armor
                new ArmorItem
                {
                    Name = "Metal Facemask",
                    ItemShortname = "metal.facemask",
                    Slot = "head",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/mVY2Uav.png",
                    Cost = 300,
                    Rarity = "Common"
                },
                new ArmorItem
                {
                    Name = "Coffee Can Helmet",
                    ItemShortname = "coffeecan.helmet",
                    Slot = "head",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourCoffeeCanImage.png",
                    Cost = 250,
                    Rarity = "Common"
                },
                
                // Chest Armor
                new ArmorItem
                {
                    Name = "Metal Chest Plate",
                    ItemShortname = "metal.plate.torso",
                    Slot = "chest",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourMetalChestImage.png",
                    Cost = 400,
                    Rarity = "Rare"
                },
                new ArmorItem
                {
                    Name = "Road Sign Jacket",
                    ItemShortname = "roadsign.jacket",
                    Slot = "chest",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourRoadSignImage.png",
                    Cost = 300,
                    Rarity = "Common"
                },
                
                // Legs Armor
                new ArmorItem
                {
                    Name = "Heavy Plate Pants",
                    ItemShortname = "heavy.plate.pants",
                    Slot = "legs",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourHeavyPantsImage.png",
                    Cost = 400,
                    Rarity = "Rare"
                },
                new ArmorItem
                {
                    Name = "Road Sign Kilt",
                    ItemShortname = "roadsign.kilt",
                    Slot = "legs",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourRoadSignKiltImage.png",
                    Cost = 300,
                    Rarity = "Common"
                },
                
                // Hands/Gloves
                new ArmorItem
                {
                    Name = "Tactical Gloves",
                    ItemShortname = "tactical.gloves",
                    Slot = "hands",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourTacticalGlovesImage.png",
                    Cost = 200,
                    Rarity = "Common"
                },
                
                // Feet/Boots
                new ArmorItem
                {
                    Name = "Heavy Plate Boots",
                    ItemShortname = "shoes.boots",
                    Slot = "feet",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourBootsImage.png",
                    Cost = 250,
                    Rarity = "Common"
                }
            };
            
            public ArmorItem[] GetArmorsBySlot(string slot)
            {
                return Armors.Where(a => a.Slot == slot).ToArray();
            }
        }
        
        public class ArmorItem
        {
            public string Name { get; set; }
            public string DisplayName => Name; // Alias for consistency
            public string ItemShortname { get; set; }
            public string Slot { get; set; } // "head", "chest", "legs", "hands", "feet"
            public string SkinId { get; set; } = "0";
            public string ImageUrl { get; set; }
            public int Cost { get; set; } = 300;
            public string Rarity { get; set; } = "Common";
            public string Tag { get; set; } = "";
        }
        
        #endregion
        
        #region Configuration
        
        internal class PluginConfig
        {
            [JsonProperty("Lobby Spawn Position")]
            public Vector3 LobbySpawnPosition { get; set; } = new Vector3(0, 100, 0);
            
            [JsonProperty("Arena Spawn Positions")]
            public List<Vector3> ArenaSpawnPositions { get; set; } = new List<Vector3>
            {
                new Vector3(0, 100, 500)
            };
            
            [JsonProperty("Starting Blood Tokens")]
            public int StartingTokens { get; set; } = 500;
            
            [JsonProperty("Tokens Per Kill")]
            public int TokensPerKill { get; set; } = 10;
            
            [JsonProperty("Enable Tebex Integration")]
            public bool EnableTebex { get; set; } = false;
            
            [JsonProperty("Tebex Secret Key")]
            public string TebexSecretKey { get; set; } = "YOUR_SECRET_KEY_HERE";
            
            [JsonProperty("Max Weapon Level")]
            public int MaxWeaponLevel { get; set; } = 10;
            
            [JsonProperty("Max Attachment Level")]
            public int MaxAttachmentLevel { get; set; } = 5;
            
            [JsonProperty("UI Update Throttle MS")]
            public int UIUpdateThrottleMS { get; set; } = 100;
            
            [JsonProperty("Auto Save Interval Seconds")]
            public float AutoSaveInterval { get; set; } = 300f;
            
            [JsonProperty("Enable Debug Logging")]
            public bool EnableDebugLogging { get; set; } = false;
        }
        
        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig();
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
                PrintError("Configuration file is corrupt. Loading defaults...");
                LoadDefaultConfig();
            }
            SaveConfig();
        }
        
        protected override void SaveConfig() => Config.WriteObject(_config, true);
        
        #endregion
        
        #region Oxide Hooks
        
        private void Init()
        {
            permission.RegisterPermission(PERMISSION_ADMIN, this);
            permission.RegisterPermission(PERMISSION_VIP, this);
            
            // Initialize gun configuration
            _gunConfig = new GunConfig();
            _outfitConfig = new OutfitConfig();
            
            // Initialize all systems
            _saveManager = new SaveManager(this, _config);
            _antiExploit = new AntiExploit(this);
            _tokenEconomy = new BloodTokenEconomy(this, _config);
            _attachmentSystem = new AttachmentSystem(this, _config);
            _weaponProgression = new WeaponProgression(this, _config);
            _vfxManager = new VFXManager(this);
            _sfxManager = new SFXManager(this);
            _forgeStation = new ForgeStationSystem(this, _config, _tokenEconomy, _attachmentSystem, _weaponProgression);
            _loadoutEditor = new LoadoutEditor(this, _attachmentSystem);
            _storeAPI = new StoreAPI(this, _config, _tokenEconomy);
            _domeManager = new DomeManager(this, _config);
            _telemetry = new TelemetrySystem(this);
            
            LogDebug("KillaDome initialized successfully");
        }
        
        private void OnServerInitialized()
        {
            Puts("[KillaDome] OnServerInitialized called");
            Puts($"[KillaDome] KillaUI reference is null: {KillaUI == null}");
            Puts($"[KillaDome] KillaUI is loaded: {KillaUI?.IsLoaded}");
            
            if (KillaUI != null && KillaUI.IsLoaded)
            {
                Puts("[KillaDome] Successfully connected to KillaUI plugin");
            }
            else
            {
                PrintWarning("[KillaDome] KillaUI plugin not found or not loaded. UI features will not work.");
            }
            
            timer.Every(_config.AutoSaveInterval, () => AutoSaveAllPlayers());
            LogDebug("Auto-save timer started");
            
            // Load images after server is ready
            timer.Once(5f, () => LoadImages());
        }
        
        private void LoadImages()
        {
            if (ImageLibrary == null || !ImageLibrary.IsLoaded)
            {
                PrintWarning("ImageLibrary not loaded. Images will not display. Please install ImageLibrary plugin.");
                return;
            }
            
            // Load gun images
            foreach (var gun in _gunConfig.Guns.Values)
            {
                if (!string.IsNullOrEmpty(gun.ImageUrl))
                {
                    ImageLibrary.Call("AddImage", gun.ImageUrl, gun.ImageUrl);
                }
            }
            
            // Load skin images
            foreach (var skin in _gunConfig.Skins)
            {
                if (!string.IsNullOrEmpty(skin.ImageUrl))
                {
                    ImageLibrary.Call("AddImage", skin.ImageUrl, skin.ImageUrl);
                }
            }
            
            // Load armor images
            foreach (var armor in _outfitConfig.Armors)
            {
                if (!string.IsNullOrEmpty(armor.ImageUrl))
                {
                    ImageLibrary.Call("AddImage", armor.ImageUrl, armor.ImageUrl);
                }
            }
            
            Puts($"Loaded {_gunConfig.Guns.Count} gun images, {_gunConfig.Skins.Count} skin images, and {_outfitConfig.Armors.Count} armor images into ImageLibrary");
        }
        
        private void Unload()
        {
            // Clean up all UI - delegate to KillaUI plugin
            if (KillaUI != null && KillaUI.IsLoaded)
            {
                try
                {
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        KillaUI.Call("DestroyUI", player);
                    }
                }
                catch (Exception ex)
                {
                    PrintWarning($"Error cleaning up UI: {ex.Message}");
                }
            }
            
            // Save all player data
            foreach (var session in _activeSessions.Values)
            {
                _saveManager?.SavePlayerProfile(session.Profile);
            }
            
            _activeSessions.Clear();
            
            LogDebug("KillaDome unloaded and cleaned up");
        }
        
        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || _saveManager == null) return;
            
            NextTick(() =>
            {
                if (player == null || !player.IsConnected) return;
                
                var profile = _saveManager.LoadPlayerProfile(player.userID);
                var session = new PlayerSession(player, profile);
                _activeSessions[player.userID] = session;
                
                // Teleport to lobby
                TeleportToLobby(player);
                
                // Show lobby UI via KillaUI plugin
                timer.Once(1f, () =>
                {
                    if (player != null && player.IsConnected && KillaUI != null && KillaUI.IsLoaded)
                    {
                        try
                        {
                            KillaUI.Call("ShowLobbyUI", player);
                        }
                        catch (Exception ex)
                        {
                            PrintError($"Error showing lobby UI on connect: {ex}");
                        }
                    }
                });
                
                LogDebug($"Player {player.displayName} ({player.userID}) connected");
            });
        }
        
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            
            // Destroy UI via KillaUI plugin
            if (KillaUI != null && KillaUI.IsLoaded)
            {
                KillaUI.Call("DestroyUI", player);
            }
            
            if (_activeSessions.TryGetValue(player.userID, out var session))
            {
                _saveManager?.SavePlayerProfile(session.Profile);
                _activeSessions.Remove(player.userID);
            }
            
            LogDebug($"Player {player.displayName} disconnected: {reason}");
        }
        
        private void OnEntityDeath(BasePlayer victim, HitInfo info)
        {
            if (victim == null || _tokenEconomy == null || _telemetry == null) return;
            
            var attacker = info?.InitiatorPlayer;
            if (attacker != null && attacker != victim && attacker.IsConnected)
            {
                // Award tokens for kill
                _tokenEconomy.AwardTokens(attacker.userID, _config.TokensPerKill);
                
                // Track telemetry
                _telemetry.RecordKill(attacker.userID, victim.userID);
                
                LogDebug($"{attacker.displayName} killed {victim.displayName}");
            }
            
            // Respawn victim in lobby after delay
            timer.Once(3f, () =>
            {
                if (victim != null && victim.IsConnected)
                {
                    TeleportToLobby(victim);
                    victim.Respawn();
                }
            });
        }
        
        #endregion
        
        #region Helper Methods
        
        private void TeleportToLobby(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            player.Teleport(_config.LobbySpawnPosition);
        }
        
        private void TeleportToArena(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            
            // Select random spawn point from configured arena spawns
            Vector3 spawnPos;
            if (_config.ArenaSpawnPositions != null && _config.ArenaSpawnPositions.Count > 0)
            {
                spawnPos = _config.ArenaSpawnPositions[UnityEngine.Random.Range(0, _config.ArenaSpawnPositions.Count)];
            }
            else
            {
                // Fallback to default if no spawns configured
                spawnPos = new Vector3(0, 100, 500);
            }
            
            player.Teleport(spawnPos);
            
            // Apply loadout when entering arena
            ApplyLoadout(player);
        }
        
        private void ApplyLoadout(BasePlayer player)
        {
            var session = GetSession(player.userID);
            if (session == null || session.Profile.Loadouts.Count == 0) return;
            
            var loadout = session.Profile.Loadouts[0];
            
            // Strip existing items
            player.inventory.Strip();
            
            // Give primary weapon
            GiveWeapon(player, loadout.Primary, loadout.PrimaryAttachments, loadout.Skins);
            
            // Give secondary weapon
            GiveWeapon(player, loadout.Secondary, loadout.SecondaryAttachments, loadout.Skins);
            
            // Give equipped armor/outfit
            GiveArmor(player, loadout);
            
            LogDebug($"Applied loadout to {player.displayName}");
        }
        
        private void GiveWeapon(BasePlayer player, string weaponName, Dictionary<string, string> attachments, Dictionary<string, string> skins)
        {
            if (string.IsNullOrEmpty(weaponName)) return;
            
            // Get weapon info from centralized config
            string itemName = "rifle.ak"; // Default fallback
            string normalizedWeaponName = weaponName?.ToLower() ?? "ak47";
            if (_gunConfig?.Guns != null && _gunConfig.Guns.TryGetValue(normalizedWeaponName, out var gunDef))
            {
                itemName = gunDef.RustItemShortname;
            }
            
            var item = ItemManager.CreateByName(itemName, 1);
            if (item == null)
            {
                LogDebug($"Failed to create weapon: {itemName}");
                return;
            }
            
            // Apply skin if exists
            if (skins != null && skins.TryGetValue(weaponName, out string skinId))
            {
                if (ulong.TryParse(skinId, out ulong skin))
                {
                    item.skin = skin;
                    item.MarkDirty(); // Mark for network update
                }
            }
            
            // Apply attachments if exists
            if (attachments != null && attachments.Count > 0)
            {
                var heldEntity = item.GetHeldEntity() as BaseProjectile;
                if (heldEntity != null && item.contents != null)
                {
                    foreach (var attachmentEntry in attachments)
                    {
                        string attachmentId = attachmentEntry.Value;
                        if (!string.IsNullOrEmpty(attachmentId))
                        {
                            var attachmentItem = ItemManager.CreateByName(attachmentId, 1);
                            if (attachmentItem != null)
                            {
                                // Add attachment to weapon's content container
                                if (!attachmentItem.MoveToContainer(item.contents))
                                {
                                    attachmentItem.Remove(); // Clean up if can't add
                                }
                            }
                        }
                    }
                }
            }
            
            // Give item to player
            if (!player.inventory.GiveItem(item))
            {
                LogDebug($"Failed to give weapon {itemName} to {player.displayName} - inventory full?");
                item.Remove(); // Clean up item if can't give
                return;
            }
            
            // If item was given to belt, ensure visual update
            var heldItem = item.GetHeldEntity();
            if (heldItem != null)
            {
                heldItem.skinID = item.skin;
                heldItem.SendNetworkUpdate();
            }
            
            // Give ammo based on weapon type
            string ammoType = "ammo.rifle"; // Default
            
            if (_gunConfig.Guns.ContainsKey(weaponName))
            {
                var gunShortname = _gunConfig.Guns[weaponName].RustItemShortname;
                
                // Determine ammo type based on weapon
                if (gunShortname.Contains("pistol") || gunShortname == "pistol.python" || gunShortname == "pistol.revolver")
                {
                    ammoType = "ammo.pistol";
                }
                else if (gunShortname.Contains("shotgun"))
                {
                    ammoType = "ammo.shotgun";
                }
                else if (gunShortname.Contains("rifle") || gunShortname.Contains("smg") || gunShortname.Contains("lmg"))
                {
                    ammoType = "ammo.rifle";
                }
            }
            
            var ammo = ItemManager.CreateByName(ammoType, 250);
            if (ammo != null)
            {
                player.inventory.GiveItem(ammo);
            }
        }
        
        private void GiveArmor(BasePlayer player, Loadout loadout)
        {
            if (player == null || loadout == null) return;
            
            // Give head armor
            if (!string.IsNullOrEmpty(loadout.ArmorHead))
            {
                GiveArmorPiece(player, loadout.ArmorHead);
            }
            
            // Give chest armor
            if (!string.IsNullOrEmpty(loadout.ArmorChest))
            {
                GiveArmorPiece(player, loadout.ArmorChest);
            }
            
            // Give legs armor
            if (!string.IsNullOrEmpty(loadout.ArmorLegs))
            {
                GiveArmorPiece(player, loadout.ArmorLegs);
            }
            
            // Give hands armor
            if (!string.IsNullOrEmpty(loadout.ArmorHands))
            {
                GiveArmorPiece(player, loadout.ArmorHands);
            }
            
            // Give feet armor
            if (!string.IsNullOrEmpty(loadout.ArmorFeet))
            {
                GiveArmorPiece(player, loadout.ArmorFeet);
            }
            
            LogDebug($"Applied armor to {player.displayName}");
        }
        
        private void GiveArmorPiece(BasePlayer player, string armorShortname)
        {
            if (string.IsNullOrEmpty(armorShortname)) return;
            
            var item = ItemManager.CreateByName(armorShortname, 1);
            if (item == null)
            {
                LogDebug($"Failed to create armor item: {armorShortname}");
                return;
            }
            
            // Try to move to wear container (for clothing/armor)
            if (!item.MoveToContainer(player.inventory.containerWear))
            {
                // If wear container is full or item can't be worn, give to main inventory
                player.inventory.GiveItem(item);
            }
            
            LogDebug($"Gave armor piece {armorShortname} to {player.displayName}");
        }
        
        private void AutoSaveAllPlayers()
        {
            if (_saveManager == null) return;
            
            int saved = 0;
            foreach (var session in _activeSessions.Values)
            {
                if (session?.Profile != null)
                {
                    _saveManager.SavePlayerProfile(session.Profile);
                    saved++;
                }
            }
            LogDebug($"Auto-saved {saved} player profiles");
        }
        
        private void LogDebug(string message)
        {
            if (_config?.EnableDebugLogging == true)
            {
                Puts($"[DEBUG] {message}");
            }
        }
        
        internal PlayerSession GetSession(ulong steamId)
        {
            _activeSessions.TryGetValue(steamId, out var session);
            return session;
        }
        
        #endregion
        
        #region Console Commands
        
        [ConsoleCommand("kd.open")]
        private void CmdOpen(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                SendReply(arg, "You don't have permission to use this command");
                return;
            }
            
            if (KillaUI != null && KillaUI.IsLoaded)
            {
                try
                {
                    KillaUI.Call("ShowLobbyUI", player);
                    SendReply(arg, "Lobby UI opened");
                }
                catch (Exception ex)
                {
                    SendReply(arg, $"Error: {ex.Message}");
                    PrintError($"Error in kd.open: {ex}");
                }
            }
            else
            {
                SendReply(arg, "KillaUI plugin not loaded");
            }
        }
        
        [ConsoleCommand("kd.start")]
        private void CmdStart(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                SendReply(arg, "You don't have permission to use this command");
                return;
            }
            
            _domeManager.StartMatch();
            SendReply(arg, "Match started");
        }
        
        [ConsoleCommand("kd.giveskin")]
        private void CmdGiveSkin(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(2))
            {
                SendReply(arg, "Usage: kd.giveskin <steamid> <skinid>");
                return;
            }
            
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                SendReply(arg, "You don't have permission to use this command");
                return;
            }
            
            if (!ulong.TryParse(arg.Args[0], out ulong targetId))
            {
                SendReply(arg, "Invalid Steam ID");
                return;
            }
            
            string skinId = arg.Args[1];
            
            if (_activeSessions.TryGetValue(targetId, out var session))
            {
                session.Profile.OwnedSkins.Add(skinId);
                _saveManager.SavePlayerProfile(session.Profile);
                SendReply(arg, $"Granted skin {skinId} to player {targetId}");
            }
            else
            {
                SendReply(arg, "Player not found or not online");
            }
        }
        
        [ConsoleCommand("kd.resetprogress")]
        private void CmdResetProgress(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1))
            {
                SendReply(arg, "Usage: kd.resetprogress <steamid>");
                return;
            }
            
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                SendReply(arg, "You don't have permission to use this command");
                return;
            }
            
            if (!ulong.TryParse(arg.Args[0], out ulong targetId))
            {
                SendReply(arg, "Invalid Steam ID");
                return;
            }
            
            var newProfile = new PlayerProfile(targetId, _config.StartingTokens);
            _saveManager.SavePlayerProfile(newProfile);
            
            if (_activeSessions.TryGetValue(targetId, out var session))
            {
                session.Profile = newProfile;
            }
            
            SendReply(arg, $"Reset progress for player {targetId}");
        }
        
        #endregion
        
        #region Chat Commands
        
        [ChatCommand("kd")]
        private void CmdKD(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                SendReply(player, "KillaDome Commands:\n" +
                    "/kd open - Open lobby UI\n" +
                    "/kd v2 - Open lobby UI (alias for 'open')\n" +
                    "/kd stats - View your stats\n" +
                    "/kd help - Show this help");
                return;
            }
            
            switch (args[0].ToLower())
            {
                case "open":
                case "v2":
                case "openv2":
                    // Phase 7: Redirecting all commands to KillaUIv2 (new UI)
                    if (KillaUIv2 != null && KillaUIv2.IsLoaded)
                    {
                        try
                        {
                            var result = KillaUIv2.Call("ShowLobbyUI", player);
                            SendReply(player, "Lobby UI opened");
                            LogDebug($"ShowLobbyUI v2 called successfully for {player.displayName}, result: {result}");
                        }
                        catch (Exception ex)
                        {
                            SendReply(player, $"Error opening UI. Check console logs.");
                            PrintError($"Error calling ShowLobbyUI v2: {ex}");
                        }
                    }
                    else
                    {
                        SendReply(player, "KillaUIv2 plugin not loaded");
                        PrintWarning($"KillaUIv2 plugin not available. KillaUIv2: {KillaUIv2}, IsLoaded: {KillaUIv2?.IsLoaded}");
                    }
                    break;
                    
                case "v1":
                case "old":
                case "legacy":
                    // Deprecated: Old UI support (KillaUI v1)
                    SendReply(player, "⚠️ DEPRECATED: The old UI has been replaced.\n" +
                        "Use /kd open for the new improved UI.");
                    PrintWarning($"Player {player.displayName} attempted to use deprecated KillaUI v1");
                    break;
                    
                case "stats":
                    if (_activeSessions.TryGetValue(player.userID, out var session))
                    {
                        SendReply(player, $"Blood Tokens: {session.Profile.Tokens}\n" +
                            $"VIP Status: {(session.Profile.IsVIP ? "Active" : "Inactive")}");
                    }
                    break;
                    
                case "help":
                    SendReply(player, "KillaDome - Full COD Experience\n" +
                        "Use /kd open to access the lobby\n" +
                        "New UI with 5 tabs: PLAY, LOADOUTS, STORE, STATS, SETTINGS");
                    break;
                    
                default:
                    SendReply(player, "Unknown command. Use /kd help");
                    break;
            }
        }
        
        #endregion
        
        #region Public API for KillaUI Plugin
        
        /// <summary>
        /// Add player to queue - called by KillaUI plugin
        /// </summary>
        [HookMethod("AddToQueue")]
        public void AddToQueue(ulong steamId)
        {
            _domeManager?.AddToQueue(steamId);
        }
        
        /// <summary>
        /// Cycle weapon selection - called by KillaUI plugin
        /// </summary>
        [HookMethod("CycleWeapon")]
        public void CycleWeapon(BasePlayer player, string slot, int direction)
        {
            if (player == null) return;
            
            var session = GetSession(player.userID);
            if (session == null || session.Profile.Loadouts.Count == 0) return;
            
            var loadout = session.Profile.Loadouts[0];
            string[] availableWeapons = _gunConfig.GetAllGunIds();
            
            string currentWeapon = slot == "primary" ? loadout.Primary : loadout.Secondary;
            int currentIndex = Array.IndexOf(availableWeapons, currentWeapon);
            
            if (currentIndex == -1) currentIndex = 0;
            
            int newIndex = (currentIndex + direction + availableWeapons.Length) % availableWeapons.Length;
            string newWeapon = availableWeapons[newIndex];
            
            if (slot == "primary")
            {
                loadout.Primary = newWeapon;
            }
            else
            {
                loadout.Secondary = newWeapon;
            }
            
            _saveManager.SavePlayerProfile(session.Profile);
            LogDebug($"Player {player.displayName} changed {slot} weapon to {newWeapon}");
        }
        
        /// <summary>
        /// Purchase item - called by KillaUI plugin
        /// </summary>
        [HookMethod("PurchaseItem")]
        public bool PurchaseItem(ulong steamId, string itemId, int cost)
        {
            var session = GetSession(steamId);
            if (session == null)
            {
                var player = BasePlayer.FindByID(steamId);
                if (player == null) return false; // Player must be online
                
                var profile = _saveManager.LoadPlayerProfile(steamId);
                session = new PlayerSession(player, profile);
                _activeSessions[steamId] = session;
            }
            
            if (session.Profile.Tokens < cost)
            {
                return false;
            }
            
            if (_storeAPI.PurchaseItem(steamId, itemId, cost))
            {
                _saveManager.SavePlayerProfile(session.Profile);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Purchase armor - called by KillaUI plugin
        /// </summary>
        [HookMethod("PurchaseArmor")]
        public bool PurchaseArmor(ulong steamId, string itemShortname, int cost)
        {
            var session = GetSession(steamId);
            if (session == null)
            {
                var player = BasePlayer.FindByID(steamId);
                if (player == null) return false; // Player must be online
                
                var profile = _saveManager.LoadPlayerProfile(steamId);
                session = new PlayerSession(player, profile);
                _activeSessions[steamId] = session;
            }
            
            if (session.Profile.OwnedArmor.Contains(itemShortname))
            {
                return false;
            }
            
            if (session.Profile.Tokens < cost)
            {
                return false;
            }
            
            session.Profile.Tokens -= cost;
            session.Profile.OwnedArmor.Add(itemShortname);
            _saveManager.SavePlayerProfile(session.Profile);
            
            return true;
        }
        
        /// <summary>
        /// Apply skin to weapon - called by KillaUI plugin
        /// </summary>
        [HookMethod("ApplySkin")]
        public bool ApplySkin(ulong steamId, string weapon, string skinId)
        {
            var session = GetSession(steamId);
            if (session == null || session.Profile.Loadouts.Count == 0) return false;
            
            if (!session.Profile.OwnedSkins.Contains(skinId))
            {
                return false;
            }
            
            var loadout = session.Profile.Loadouts[0];
            string weaponName = weapon == "primary" ? loadout.Primary : loadout.Secondary;
            
            loadout.Skins[weaponName] = skinId;
            _saveManager.SavePlayerProfile(session.Profile);
            
            return true;
        }
        
        /// <summary>
        /// Apply attachment to weapon - called by KillaUI plugin
        /// </summary>
        [HookMethod("ApplyAttachment")]
        public bool ApplyAttachment(ulong steamId, string weapon, string attachmentSlot, string attachmentId)
        {
            var session = GetSession(steamId);
            if (session == null || session.Profile.Loadouts.Count == 0) return false;
            
            if (!session.Profile.OwnedSkins.Contains(attachmentId))
            {
                return false;
            }
            
            var loadout = session.Profile.Loadouts[0];
            var attachments = weapon == "primary" ? loadout.PrimaryAttachments : loadout.SecondaryAttachments;
            
            attachments[attachmentSlot] = attachmentId;
            _saveManager.SavePlayerProfile(session.Profile);
            
            return true;
        }
        
        /// <summary>
        /// Set attachment category - called by KillaUI plugin
        /// </summary>
        [HookMethod("SetAttachmentCategory")]
        public void SetAttachmentCategory(ulong steamId, string category)
        {
            var session = GetSession(steamId);
            if (session != null)
            {
                session.SelectedAttachmentCategory = category;
            }
        }
        
        /// <summary>
        /// Set editing weapon slot - called by KillaUI plugin
        /// </summary>
        [HookMethod("SetEditingWeaponSlot")]
        public void SetEditingWeaponSlot(ulong steamId, string slot)
        {
            var session = GetSession(steamId);
            if (session != null)
            {
                session.EditingWeaponSlot = slot;
            }
        }
        
        /// <summary>
        /// Set store category - called by KillaUI plugin
        /// </summary>
        [HookMethod("SetStoreCategory")]
        public void SetStoreCategory(ulong steamId, string category)
        {
            var session = GetSession(steamId);
            if (session != null)
            {
                session.SelectedStoreCategory = category;
                session.GunsStorePage = 0;
                session.SkinsStorePage = 0;
            }
        }
        
        /// <summary>
        /// Change store page - called by KillaUI plugin
        /// </summary>
        [HookMethod("ChangeStorePage")]
        public void ChangeStorePage(ulong steamId, string direction)
        {
            var session = GetSession(steamId);
            if (session == null) return;
            
            string category = session.SelectedStoreCategory ?? "guns";
            
            if (category == "guns")
            {
                int itemsPerPage = 6;
                int totalItems = _gunConfig.Guns.Count;
                int maxPage = (int)Math.Ceiling((double)totalItems / itemsPerPage) - 1;
                
                if (direction == "next" && session.GunsStorePage < maxPage)
                    session.GunsStorePage++;
                else if (direction == "prev" && session.GunsStorePage > 0)
                    session.GunsStorePage--;
            }
            else if (category == "skins")
            {
                int itemsPerPage = 12;
                int totalItems = _gunConfig.Skins.Count + 3;
                int maxPage = (int)Math.Ceiling((double)totalItems / itemsPerPage) - 1;
                
                if (direction == "next" && session.SkinsStorePage < maxPage)
                    session.SkinsStorePage++;
                else if (direction == "prev" && session.SkinsStorePage > 0)
                    session.SkinsStorePage--;
            }
        }
        
        /// <summary>
        /// Set loadout tab - called by KillaUI plugin
        /// </summary>
        [HookMethod("SetLoadoutTab")]
        public void SetLoadoutTab(ulong steamId, string tab)
        {
            var session = GetSession(steamId);
            if (session != null)
            {
                session.SelectedLoadoutTab = tab;
            }
        }
        
        /// <summary>
        /// Cycle armor - called by KillaUI plugin
        /// </summary>
        [HookMethod("CycleArmor")]
        public void CycleArmor(ulong steamId, string slot, int direction)
        {
            var session = GetSession(steamId);
            if (session == null || session.Profile.Loadouts.Count == 0) return;
            
            var loadout = session.Profile.Loadouts[0];
            
            var ownedArmor = _outfitConfig.Armors
                .Where(a => a.Slot == slot && session.Profile.OwnedArmor.Contains(a.ItemShortname))
                .ToArray();
            
            if (ownedArmor.Length == 0) return;
            
            string currentArmorShortname = slot switch
            {
                "head" => loadout.ArmorHead,
                "chest" => loadout.ArmorChest,
                "legs" => loadout.ArmorLegs,
                "hands" => loadout.ArmorHands,
                "feet" => loadout.ArmorFeet,
                _ => null
            };
            
            int currentIndex = Array.FindIndex(ownedArmor, a => a.ItemShortname == currentArmorShortname);
            if (currentIndex == -1) currentIndex = 0;
            
            int newIndex = (currentIndex + direction + ownedArmor.Length) % ownedArmor.Length;
            string newArmor = ownedArmor[newIndex].ItemShortname;
            
            switch (slot)
            {
                case "head": loadout.ArmorHead = newArmor; break;
                case "chest": loadout.ArmorChest = newArmor; break;
                case "legs": loadout.ArmorLegs = newArmor; break;
                case "hands": loadout.ArmorHands = newArmor; break;
                case "feet": loadout.ArmorFeet = newArmor; break;
            }
        }
        
        /// <summary>
        /// Get session data for UI - called by KillaUI plugin
        /// </summary>
        [HookMethod("GetSessionData")]
        public Dictionary<string, object> GetSessionData(ulong steamId)
        {
            var session = GetSession(steamId);
            
            // Create session on demand if it doesn't exist
            if (session == null)
            {
                var player = BasePlayer.FindByID(steamId);
                if (player == null) return null;
                
                var profile = _saveManager.LoadPlayerProfile(steamId);
                session = new PlayerSession(player, profile);
                _activeSessions[steamId] = session;
                
                LogDebug($"Created session on demand for {player.displayName}");
            }
            
            return new Dictionary<string, object>
            {
                ["Tokens"] = session.Profile.Tokens,
                ["EditingWeaponSlot"] = session.EditingWeaponSlot,
                ["SelectedLoadoutTab"] = session.SelectedLoadoutTab,
                ["SelectedAttachmentCategory"] = session.SelectedAttachmentCategory,
                ["SelectedStoreCategory"] = session.SelectedStoreCategory,
                ["GunsStorePage"] = session.GunsStorePage,
                ["SkinsStorePage"] = session.SkinsStorePage
            };
        }
        
        /// <summary>
        /// Get current loadout data for UI - called by KillaUI plugin
        /// </summary>
        [HookMethod("GetCurrentLoadout")]
        public Dictionary<string, object> GetCurrentLoadout(ulong steamId)
        {
            var session = GetSession(steamId);
            
            // Create session on demand if it doesn't exist
            if (session == null)
            {
                var player = BasePlayer.FindByID(steamId);
                if (player == null) return null;
                
                var profile = _saveManager.LoadPlayerProfile(steamId);
                session = new PlayerSession(player, profile);
                _activeSessions[steamId] = session;
                
                LogDebug($"Created session on demand for {player.displayName}");
            }
            
            // Ensure at least one loadout exists
            if (session.Profile.Loadouts.Count == 0)
            {
                session.Profile.Loadouts.Add(new Loadout
                {
                    Name = "Default",
                    Primary = "ak47",
                    Secondary = "python",
                    PrimaryAttachments = new Dictionary<string, string>(),
                    SecondaryAttachments = new Dictionary<string, string>(),
                    Skins = new Dictionary<string, string>()
                });
            }
            
            var loadout = session.Profile.Loadouts[0];
            return new Dictionary<string, object>
            {
                ["Primary"] = loadout.Primary ?? "ak47",
                ["Secondary"] = loadout.Secondary ?? "python",
                ["ArmorHead"] = loadout.ArmorHead,
                ["ArmorChest"] = loadout.ArmorChest,
                ["ArmorLegs"] = loadout.ArmorLegs,
                ["ArmorHands"] = loadout.ArmorHands,
                ["ArmorFeet"] = loadout.ArmorFeet
            };
        }
        
        /// <summary>
        /// Get weapon info for UI - called by KillaUI plugin
        /// </summary>
        [HookMethod("GetWeaponInfo")]
        public Dictionary<string, object> GetWeaponInfo(string weaponId)
        {
            if (_gunConfig.Guns.ContainsKey(weaponId))
            {
                var gun = _gunConfig.Guns[weaponId];
                return new Dictionary<string, object>
                {
                    ["DisplayName"] = gun.DisplayName,
                    ["ImageUrl"] = gun.ImageUrl,
                    ["RustItemShortname"] = gun.RustItemShortname
                };
            }
            
            return new Dictionary<string, object>
            {
                ["DisplayName"] = weaponId.ToUpper(),
                ["ImageUrl"] = "",
                ["RustItemShortname"] = weaponId
            };
        }
        
        /// <summary>
        /// Get available guns for store - called by KillaUI plugin
        /// </summary>
        [HookMethod("GetAvailableGuns")]
        public List<Dictionary<string, object>> GetAvailableGuns()
        {
            var result = new List<Dictionary<string, object>>();
            
            foreach (var gun in _gunConfig.Guns.Values)
            {
                result.Add(new Dictionary<string, object>
                {
                    ["Id"] = gun.Id,
                    ["DisplayName"] = gun.DisplayName,
                    ["ImageUrl"] = gun.ImageUrl,
                    ["Cost"] = 500
                });
            }
            
            return result;
        }
        
        /// <summary>
        /// Get player profile data for UI - called by KillaUI plugin
        /// </summary>
        [HookMethod("GetPlayerProfile")]
        public Dictionary<string, object> GetPlayerProfile(ulong steamId)
        {
            var session = GetSession(steamId);
            
            // Create session on demand if it doesn't exist
            if (session == null)
            {
                var player = BasePlayer.FindByID(steamId);
                if (player == null) return null;
                
                var profile = _saveManager.LoadPlayerProfile(steamId);
                session = new PlayerSession(player, profile);
                _activeSessions[steamId] = session;
                
                LogDebug($"Created session on demand for {player.displayName}");
            }
            
            return new Dictionary<string, object>
            {
                ["TotalKills"] = session.Profile.TotalKills,
                ["TotalDeaths"] = session.Profile.TotalDeaths,
                ["Tokens"] = session.Profile.Tokens,
                ["MatchesPlayed"] = session.Profile.MatchesPlayed,
                ["IsVIP"] = session.Profile.IsVIP
            };
        }
        
        /// <summary>
        /// Reset player loadout to default - called by KillaUI plugin
        /// </summary>
        [HookMethod("ResetLoadout")]
        public void ResetLoadout(ulong steamId)
        {
            var session = GetSession(steamId);
            if (session == null || session.Profile.Loadouts.Count == 0) return;
            
            var loadout = session.Profile.Loadouts[0];
            loadout.Primary = "ak47";
            loadout.Secondary = "python";
            loadout.PrimaryAttachments = new Dictionary<string, string>();
            loadout.SecondaryAttachments = new Dictionary<string, string>();
            loadout.Skins = new Dictionary<string, string>();
            
            _saveManager.SavePlayerProfile(session.Profile);
            LogDebug($"Reset loadout for player {steamId}");
        }
        
        /// <summary>
        /// Reset player outfit to default - called by KillaUI plugin
        /// </summary>
        [HookMethod("ResetOutfit")]
        public void ResetOutfit(ulong steamId)
        {
            var session = GetSession(steamId);
            if (session == null || session.Profile.Loadouts.Count == 0) return;
            
            var loadout = session.Profile.Loadouts[0];
            loadout.ArmorHead = null;
            loadout.ArmorChest = null;
            loadout.ArmorLegs = null;
            loadout.ArmorHands = null;
            loadout.ArmorFeet = null;
            
            _saveManager.SavePlayerProfile(session.Profile);
            LogDebug($"Reset outfit for player {steamId}");
        }
        
        /// <summary>
        /// Cycle armor skin for a specific slot - called by KillaUI plugin
        /// </summary>
        [HookMethod("CycleArmorSkin")]
        public void CycleArmorSkin(ulong steamId, int armorSlot, int direction)
        {
            var session = GetSession(steamId);
            if (session == null || session.Profile.Loadouts.Count == 0) return;
            
            // armorSlot: 0=head, 1=chest, 2=legs, 3=hands, 4=feet
            // For now, this is a placeholder - skin cycling for specific armor pieces
            // would require a skin database per armor type
            LogDebug($"CycleArmorSkin called for player {steamId}, slot {armorSlot}, direction {direction}");
            
            // This method is primarily for future skin support
            // Currently armor "skins" are managed through the armor type selection (CycleArmor)
        }
        
        /// <summary>
        /// Get equipped attachments for a weapon - called by KillaUI plugin
        /// </summary>
        [HookMethod("GetEquippedAttachments")]
        public Dictionary<string, string> GetEquippedAttachments(ulong steamId, string weaponSlot)
        {
            var session = GetSession(steamId);
            if (session == null || session.Profile.Loadouts.Count == 0)
            {
                return new Dictionary<string, string>();
            }
            
            var loadout = session.Profile.Loadouts[0];
            var attachments = weaponSlot == "primary" ? loadout.PrimaryAttachments : loadout.SecondaryAttachments;
            
            return attachments ?? new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Get player settings - called by KillaUI plugin
        /// </summary>
        [HookMethod("GetPlayerSettings")]
        public Dictionary<string, bool> GetPlayerSettings(ulong steamId)
        {
            var session = GetSession(steamId);
            if (session == null)
            {
                // Return default settings
                return new Dictionary<string, bool>
                {
                    ["auto_queue"] = false,
                    ["show_killfeed"] = true,
                    ["level_up_notif"] = true
                };
            }
            
            // For now, return default settings
            // In a full implementation, these would be stored in PlayerProfile
            return new Dictionary<string, bool>
            {
                ["auto_queue"] = false,
                ["show_killfeed"] = true,
                ["level_up_notif"] = true
            };
        }
        
        /// <summary>
        /// Set player setting - called by KillaUI plugin
        /// </summary>
        [HookMethod("SetPlayerSetting")]
        public void SetPlayerSetting(ulong steamId, string settingName, bool value)
        {
            var session = GetSession(steamId);
            if (session == null) return;
            
            // In a full implementation, these would be stored in PlayerProfile and saved
            LogDebug($"SetPlayerSetting called for player {steamId}: {settingName} = {value}");
            
            // For now, just log the setting change
            // Future: Add settings dictionary to PlayerProfile and save
        }
        
        /// <summary>
        /// Set player blood tokens (admin feature) - called by KillaUI plugin
        /// </summary>
        [HookMethod("SetBloodTokens")]
        public void SetBloodTokens(ulong steamId, int amount)
        {
            var session = GetSession(steamId);
            if (session == null)
            {
                var player = BasePlayer.FindByID(steamId);
                if (player == null) return;
                
                var profile = _saveManager.LoadPlayerProfile(steamId);
                session = new PlayerSession(player, profile);
                _activeSessions[steamId] = session;
            }
            
            session.Profile.Tokens = amount;
            _saveManager.SavePlayerProfile(session.Profile);
            LogDebug($"Set blood tokens for player {steamId} to {amount}");
        }
        
        /// <summary>
        /// Check if player owns an item - called by KillaUI plugin
        /// </summary>
        [HookMethod("CheckOwnership")]
        public bool CheckOwnership(ulong steamId, string itemId)
        {
            var session = GetSession(steamId);
            if (session == null) return false;
            
            // Check in OwnedSkins (weapons, attachments, skins are all stored here)
            if (session.Profile.OwnedSkins.Contains(itemId))
                return true;
            
            // Check in OwnedArmor
            if (session.Profile.OwnedArmor.Contains(itemId))
                return true;
            
            return false;
        }
        
        #endregion
        
        #region Helper Methods
        
        [ChatCommand("kdlobby")]
        private void CmdSetLobbySpawn(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                SendReply(player, "<color=#FF0000>Error:</color> You don't have permission to use this command!");
                return;
            }
            
            if (args.Length == 0 || args[0].ToLower() != "set")
            {
                SendReply(player, "<color=#00FF00>Lobby Spawn:</color> Usage: /kdlobby set");
                return;
            }
            
            // Set lobby spawn to player's current position
            _config.LobbySpawnPosition = player.transform.position;
            SaveConfig();
            
            SendReply(player, $"<color=#00FF00>Lobby Spawn:</color> Set to {player.transform.position}");
            LogDebug($"Admin {player.displayName} set lobby spawn to {player.transform.position}");
        }
        
        [ChatCommand("kdspawn")]
        private void CmdSetArenaSpawn(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                SendReply(player, "<color=#FF0000>Error:</color> You don't have permission to use this command!");
                return;
            }
            
            if (args.Length < 2 || args[0].ToLower() != "set")
            {
                SendReply(player, "<color=#00FF00>Arena Spawn:</color> Usage: /kdspawn set <number>");
                SendReply(player, "Example: /kdspawn set 1");
                return;
            }
            
            if (!int.TryParse(args[1], out int spawnIndex) || spawnIndex < 1)
            {
                SendReply(player, "<color=#FF0000>Error:</color> Spawn number must be a positive integer!");
                return;
            }
            
            // Ensure list exists
            if (_config.ArenaSpawnPositions == null)
            {
                _config.ArenaSpawnPositions = new List<Vector3>();
            }
            
            // Expand list if needed
            while (_config.ArenaSpawnPositions.Count < spawnIndex)
            {
                _config.ArenaSpawnPositions.Add(new Vector3(0, 100, 500));
            }
            
            // Set spawn point (spawnIndex - 1 because 0-indexed)
            _config.ArenaSpawnPositions[spawnIndex - 1] = player.transform.position;
            SaveConfig();
            
            SendReply(player, $"<color=#00FF00>Arena Spawn #{spawnIndex}:</color> Set to {player.transform.position}");
            LogDebug($"Admin {player.displayName} set arena spawn #{spawnIndex} to {player.transform.position}");
        }
        
        [ChatCommand("kdspawns")]
        private void CmdListSpawns(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                SendReply(player, "<color=#FF0000>Error:</color> You don't have permission to use this command!");
                return;
            }
            
            SendReply(player, "<color=#00FF00>=== Spawn Points ===</color>");
            SendReply(player, $"<color=#FFFF00>Lobby:</color> {_config.LobbySpawnPosition}");
            
            if (_config.ArenaSpawnPositions != null && _config.ArenaSpawnPositions.Count > 0)
            {
                SendReply(player, $"<color=#FFFF00>Arena Spawns:</color> {_config.ArenaSpawnPositions.Count} configured");
                for (int i = 0; i < _config.ArenaSpawnPositions.Count; i++)
                {
                    SendReply(player, $"  #{i + 1}: {_config.ArenaSpawnPositions[i]}");
                }
            }
            else
            {
                SendReply(player, "<color=#FF8A00>Arena Spawns:</color> None configured (using default)");
            }
        }
        
        [ChatCommand("dice")]
        private void CmdDiceGame(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            
            var session = GetSession(player.userID);
            if (session == null)
            {
                SendReply(player, "Session not found! Please rejoin.");
                return;
            }
            
            // Check if player is in cooldown
            if (session.LastDiceGame != default && (DateTime.UtcNow - session.LastDiceGame).TotalSeconds < 30)
            {
                int remaining = 30 - (int)(DateTime.UtcNow - session.LastDiceGame).TotalSeconds;
                SendReply(player, $"<color=#FF8A00>Dice Game:</color> Wait {remaining}s before playing again!");
                return;
            }
            
            if (args.Length == 0)
            {
                SendReply(player, "<color=#FF8A00>Dice Game:</color> Roll the dice! Win 2x your bet!");
                SendReply(player, "Usage: /dice <bet> (10-100 tokens)");
                SendReply(player, $"Your tokens: <color=#FF8A00>{session.Profile.Tokens}</color>");
                return;
            }
            
            if (!int.TryParse(args[0], out int bet))
            {
                SendReply(player, "<color=#FF8A00>Dice Game:</color> Invalid bet amount!");
                return;
            }
            
            if (bet < 10 || bet > 100)
            {
                SendReply(player, "<color=#FF8A00>Dice Game:</color> Bet must be between 10-100 tokens!");
                return;
            }
            
            if (session.Profile.Tokens < bet)
            {
                SendReply(player, $"<color=#FF8A00>Dice Game:</color> Not enough tokens! You have {session.Profile.Tokens}");
                return;
            }
            
            // Deduct bet
            session.Profile.Tokens -= bet;
            session.LastDiceGame = DateTime.UtcNow;
            
            // Roll dice (1-6 for player, 1-6 for house)
            int playerRoll = UnityEngine.Random.Range(1, 7);
            int houseRoll = UnityEngine.Random.Range(1, 7);
            
            SendReply(player, $"<color=#FF8A00>╔═══════════════════╗</color>");
            SendReply(player, $"<color=#FF8A00>║</color>   DICE GAME    <color=#FF8A00>║</color>");
            SendReply(player, $"<color=#FF8A00>╚═══════════════════╝</color>");
            SendReply(player, $"Your roll: <color=#4CFF4C>[{playerRoll}]</color>");
            SendReply(player, $"House roll: <color=#FF4C4C>[{houseRoll}]</color>");
            
            if (playerRoll > houseRoll)
            {
                int winnings = bet * 2;
                session.Profile.Tokens += winnings;
                SendReply(player, $"<color=#4CFF4C>★ YOU WIN! ★</color> +{winnings} tokens");
                SendReply(player, $"Balance: <color=#FF8A00>{session.Profile.Tokens}</color> tokens");
                Effect.server.Run("assets/prefabs/deployable/vendingmachine/effects/buy.prefab", player.transform.position);
            }
            else if (playerRoll < houseRoll)
            {
                SendReply(player, $"<color=#FF4C4C>✖ YOU LOSE!</color> -{bet} tokens");
                SendReply(player, $"Balance: <color=#FF8A00>{session.Profile.Tokens}</color> tokens");
                Effect.server.Run("assets/prefabs/deployable/vendingmachine/effects/deny.prefab", player.transform.position);
            }
            else
            {
                // Tie - return bet
                session.Profile.Tokens += bet;
                SendReply(player, $"<color=#FFD700>═ TIE! ═</color> Bet returned");
                SendReply(player, $"Balance: <color=#FF8A00>{session.Profile.Tokens}</color> tokens");
            }
            
            _saveManager.SavePlayerProfile(session.Profile);
        }
        
        [ChatCommand("tokengame")]
        private void CmdTokenGameHelp(BasePlayer player, string command, string[] args)
        {
            SendReply(player, "<color=#FF8A00>╔═══════════════════════════╗</color>");
            SendReply(player, "<color=#FF8A00>║</color>  BLOOD TOKEN GAMES     <color=#FF8A00>║</color>");
            SendReply(player, "<color=#FF8A00>╚═══════════════════════════╝</color>");
            SendReply(player, "");
            SendReply(player, "<color=#4CFF4C>/dice <bet></color> - Roll dice vs house");
            SendReply(player, "  • Bet: 10-100 tokens");
            SendReply(player, "  • Win: 2x your bet");
            SendReply(player, "  • Cooldown: 30 seconds");
            SendReply(player, "");
            SendReply(player, "<color=#FFD700>More games coming soon!</color>");
        }
        
        #endregion
        
        #region Data Models
        
        internal class PlayerSession
        {
            public BasePlayer Player { get; set; }
            public PlayerProfile Profile { get; set; }
            public DateTime LastAction { get; set; }
            public string SelectedItem { get; set; }
            public bool IsInMatch { get; set; }
            public string EditingWeaponSlot { get; set; } // "primary" or "secondary"
            public string SelectedAttachmentCategory { get; set; } // "scopes", "silencers", "underbarrel"
            public string SelectedStoreCategory { get; set; } // "guns", "skins", or "outfits"
            public int GunsStorePage { get; set; } // Current page for gun store
            public int SkinsStorePage { get; set; } // Current page for skins store
            public DateTime LastDiceGame { get; set; } // Cooldown for dice game
            public string SelectedLoadoutTab { get; set; } // "weapons" or "outfit"
            
            internal PlayerSession(BasePlayer player, PlayerProfile profile)
            {
                Player = player;
                Profile = profile;
                LastAction = DateTime.UtcNow;
                EditingWeaponSlot = "primary"; // Default to editing primary
                SelectedAttachmentCategory = "scopes"; // Default to scopes tab
                SelectedStoreCategory = "guns"; // Default to guns store
                GunsStorePage = 0; // Start at first page
                SkinsStorePage = 0; // Start at first page
                SelectedLoadoutTab = "weapons"; // Default to weapons tab
            }
        }
        
        public class PlayerProfile
        {
            public ulong SteamID { get; set; }
            public List<Loadout> Loadouts { get; set; }
            public Dictionary<string, int> WeaponLevels { get; set; }
            public Dictionary<string, int> AttachmentLevels { get; set; }
            public List<string> OwnedSkins { get; set; }
            public List<string> OwnedArmor { get; set; } // List of owned armor shortnames
            public int Tokens { get; set; }
            public bool IsVIP { get; set; }
            public DateTime LastUpdated { get; set; }
            public int TotalKills { get; set; }
            public int TotalDeaths { get; set; }
            public int MatchesPlayed { get; set; }
            
            public PlayerProfile()
            {
                Loadouts = new List<Loadout>();
                WeaponLevels = new Dictionary<string, int>();
                AttachmentLevels = new Dictionary<string, int>();
                OwnedSkins = new List<string>();
                OwnedArmor = new List<string>();
            }
            
            public PlayerProfile(ulong steamId, int startingTokens) : this()
            {
                SteamID = steamId;
                Tokens = startingTokens;
                LastUpdated = DateTime.UtcNow;
                
                // Create default loadout
                Loadouts.Add(new Loadout
                {
                    Name = "Default",
                    Primary = "ak47",
                    Secondary = "python",
                    PrimaryAttachments = new Dictionary<string, string>(),
                    Skins = new Dictionary<string, string>()
                });
            }
        }
        
        public class Loadout
        {
            public string Name { get; set; }
            public string Primary { get; set; }
            public string Secondary { get; set; }
            public Dictionary<string, string> PrimaryAttachments { get; set; }
            public Dictionary<string, string> SecondaryAttachments { get; set; }
            public Dictionary<string, string> Skins { get; set; }
            public string Lethal { get; set; }
            public string Tactical { get; set; }
            public List<string> Perks { get; set; }
            // Outfit/Armor slots
            public string ArmorHead { get; set; }
            public string ArmorChest { get; set; }
            public string ArmorLegs { get; set; }
            public string ArmorHands { get; set; }
            public string ArmorFeet { get; set; }
            
            public Loadout()
            {
                PrimaryAttachments = new Dictionary<string, string>();
                SecondaryAttachments = new Dictionary<string, string>();
                Skins = new Dictionary<string, string>();
                Perks = new List<string>();
            }
        }
        
        #endregion
        
        #region Module: DomeManager
        
        internal class DomeManager
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            private Match _currentMatch;
            private List<ulong> _matchQueue = new List<ulong>();
            
            internal DomeManager(KillaDome plugin, PluginConfig config)
            {
                _plugin = plugin;
                _config = config;
            }
            
            public void StartMatch()
            {
                if (_currentMatch != null && _currentMatch.IsActive)
                {
                    _plugin.PrintWarning("Match already in progress");
                    return;
                }
                
                _currentMatch = new Match
                {
                    MatchId = Guid.NewGuid().ToString(),
                    StartTime = DateTime.UtcNow,
                    IsActive = true
                };
                
                // Teleport queued players to arena
                foreach (var steamId in _matchQueue)
                {
                    var player = BasePlayer.FindByID(steamId);
                    if (player != null && player.IsConnected)
                    {
                        _plugin.TeleportToArena(player);
                        
                        var session = _plugin.GetSession(steamId);
                        if (session != null)
                        {
                            session.IsInMatch = true;
                        }
                    }
                }
                
                _plugin.Puts($"Match {_currentMatch.MatchId} started with {_matchQueue.Count} players");
                _matchQueue.Clear();
            }
            
            public void EndMatch()
            {
                if (_currentMatch == null || !_currentMatch.IsActive)
                {
                    return;
                }
                
                _currentMatch.IsActive = false;
                _currentMatch.EndTime = DateTime.UtcNow;
                
                // Return players to lobby
                foreach (var player in BasePlayer.activePlayerList)
                {
                    var session = _plugin.GetSession(player.userID);
                    if (session != null && session.IsInMatch)
                    {
                        _plugin.TeleportToLobby(player);
                        session.IsInMatch = false;
                    }
                }
                
                _plugin.Puts($"Match {_currentMatch.MatchId} ended");
            }
            
            public void AddToQueue(ulong steamId)
            {
                if (!_matchQueue.Contains(steamId))
                {
                    _matchQueue.Add(steamId);
                }
            }
            
            public void RemoveFromQueue(ulong steamId)
            {
                _matchQueue.Remove(steamId);
            }
        }
        
        internal class Match
        {
            public string MatchId { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public bool IsActive { get; set; }
            public List<ulong> Participants { get; set; } = new List<ulong>();
        }
        
        #endregion
        
        
        
        #region Module: LoadoutEditor
        
        internal class LoadoutEditor
        {
            private KillaDome _plugin;
            private AttachmentSystem _attachmentSystem;
            private Dictionary<ulong, string> _selectedItems = new Dictionary<ulong, string>();
            
            internal LoadoutEditor(KillaDome plugin, AttachmentSystem attachmentSystem)
            {
                _plugin = plugin;
                _attachmentSystem = attachmentSystem;
            }
            
            public void SelectItem(ulong steamId, string itemId)
            {
                _selectedItems[steamId] = itemId;
            }
            
            public string GetSelectedItem(ulong steamId)
            {
                _selectedItems.TryGetValue(steamId, out string itemId);
                return itemId;
            }
            
            public void ClearSelection(ulong steamId)
            {
                _selectedItems.Remove(steamId);
            }
            
            public bool TryEquipItem(ulong steamId, string slotId, string itemId)
            {
                var session = _plugin.GetSession(steamId);
                if (session == null || session.Profile.Loadouts.Count == 0)
                {
                    return false;
                }
                
                var loadout = session.Profile.Loadouts[0]; // Current loadout
                
                // Validate and equip based on slot type
                if (slotId.StartsWith("primary_att_"))
                {
                    string attachmentSlot = slotId.Replace("primary_att_", "");
                    loadout.PrimaryAttachments[attachmentSlot] = itemId;
                    return true;
                }
                
                return false;
            }
        }
        
        #endregion
        
        #region Module: AttachmentSystem
        
        internal class AttachmentSystem
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            private Dictionary<string, AttachmentDefinition> _attachments;
            
            internal AttachmentSystem(KillaDome plugin, PluginConfig config)
            {
                _plugin = plugin;
                _config = config;
                InitializeAttachments();
            }
            
            private void InitializeAttachments()
            {
                _attachments = new Dictionary<string, AttachmentDefinition>
                {
                    ["silencer"] = new AttachmentDefinition
                    {
                        Id = "silencer",
                        Name = "Silencer",
                        Slot = "barrel",
                        MaxLevel = 5,
                        StatModifiers = new Dictionary<string, float>
                        {
                            ["noise_reduction"] = 0.8f,
                            ["damage"] = -0.05f
                        }
                    },
                    ["extended_mag"] = new AttachmentDefinition
                    {
                        Id = "extended_mag",
                        Name = "Extended Magazine",
                        Slot = "mag",
                        MaxLevel = 5,
                        StatModifiers = new Dictionary<string, float>
                        {
                            ["mag_size"] = 1.5f,
                            ["reload_speed"] = -0.1f
                        }
                    },
                    ["reflex"] = new AttachmentDefinition
                    {
                        Id = "reflex",
                        Name = "Reflex Sight",
                        Slot = "optic",
                        MaxLevel = 3,
                        StatModifiers = new Dictionary<string, float>
                        {
                            ["accuracy"] = 1.2f
                        }
                    }
                };
            }
            
            internal AttachmentDefinition GetAttachment(string attachmentId)
            {
                _attachments.TryGetValue(attachmentId, out var attachment);
                return attachment;
            }
            
            public Dictionary<string, float> CalculateWeaponStats(string weaponId, Dictionary<string, string> attachments)
            {
                var stats = new Dictionary<string, float>
                {
                    ["damage"] = 1.0f,
                    ["fire_rate"] = 1.0f,
                    ["accuracy"] = 1.0f,
                    ["mag_size"] = 1.0f,
                    ["reload_speed"] = 1.0f
                };
                
                foreach (var attachment in attachments.Values)
                {
                    var def = GetAttachment(attachment);
                    if (def != null)
                    {
                        foreach (var mod in def.StatModifiers)
                        {
                            if (stats.ContainsKey(mod.Key))
                            {
                                stats[mod.Key] *= mod.Value;
                            }
                            else
                            {
                                stats[mod.Key] = mod.Value;
                            }
                        }
                    }
                }
                
                return stats;
            }
        }
        
        internal class AttachmentDefinition
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Slot { get; set; }
            public int MaxLevel { get; set; }
            public Dictionary<string, float> StatModifiers { get; set; }
            public string VFXTag { get; set; }
            public string SFXTag { get; set; }
        }
        
        #endregion
        
        #region Module: WeaponProgression
        
        internal class WeaponProgression
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            private Dictionary<string, WeaponDefinition> _weapons;
            
            internal WeaponProgression(KillaDome plugin, PluginConfig config)
            {
                _plugin = plugin;
                _config = config;
                InitializeWeapons();
            }
            
            private void InitializeWeapons()
            {
                _weapons = new Dictionary<string, WeaponDefinition>
                {
                    ["ak47"] = new WeaponDefinition
                    {
                        Id = "ak47",
                        Name = "AK-47",
                        MaxLevel = 10,
                        BaseStats = new Dictionary<string, float>
                        {
                            ["damage"] = 35f,
                            ["fire_rate"] = 0.13f,
                            ["accuracy"] = 0.75f
                        }
                    },
                    ["m249"] = new WeaponDefinition
                    {
                        Id = "m249",
                        Name = "M249",
                        MaxLevel = 10,
                        BaseStats = new Dictionary<string, float>
                        {
                            ["damage"] = 30f,
                            ["fire_rate"] = 0.1f,
                            ["accuracy"] = 0.7f
                        }
                    }
                };
            }
            
            public int GetWeaponLevel(ulong steamId, string weaponId)
            {
                var session = _plugin.GetSession(steamId);
                if (session == null) return 0;
                
                session.Profile.WeaponLevels.TryGetValue(weaponId, out int level);
                return level;
            }
            
            public bool UpgradeWeapon(ulong steamId, string weaponId, int cost)
            {
                var session = _plugin.GetSession(steamId);
                if (session == null) return false;
                
                int currentLevel = GetWeaponLevel(steamId, weaponId);
                if (currentLevel >= _config.MaxWeaponLevel)
                {
                    return false;
                }
                
                if (session.Profile.Tokens < cost)
                {
                    return false;
                }
                
                session.Profile.Tokens -= cost;
                session.Profile.WeaponLevels[weaponId] = currentLevel + 1;
                
                return true;
            }
        }
        
        internal class WeaponDefinition
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int MaxLevel { get; set; }
            public Dictionary<string, float> BaseStats { get; set; }
        }
        
        #endregion
        
        #region Module: VFXManager
        
        internal class VFXManager
        {
            private KillaDome _plugin;
            
            internal VFXManager(KillaDome plugin)
            {
                _plugin = plugin;
            }
            
            public void PlayVFX(BasePlayer player, string vfxTag, Vector3 position)
            {
                // This would trigger client-side VFX
                player.SendConsoleCommand($"killadome.vfx {vfxTag} {position.x} {position.y} {position.z}");
            }
        }
        
        #endregion
        
        #region Module: SFXManager
        
        internal class SFXManager
        {
            private KillaDome _plugin;
            
            internal SFXManager(KillaDome plugin)
            {
                _plugin = plugin;
            }
            
            public void PlaySFX(BasePlayer player, string sfxTag)
            {
                // This would trigger client-side SFX
                player.SendConsoleCommand($"killadome.sfx {sfxTag}");
            }
        }
        
        #endregion
        
        #region Module: ForgeStationSystem
        
        internal class ForgeStationSystem
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            private BloodTokenEconomy _economy;
            private AttachmentSystem _attachmentSystem;
            private WeaponProgression _weaponProgression;
            
            internal ForgeStationSystem(KillaDome plugin, PluginConfig config, BloodTokenEconomy economy, 
                AttachmentSystem attachmentSystem, WeaponProgression weaponProgression)
            {
                _plugin = plugin;
                _config = config;
                _economy = economy;
                _attachmentSystem = attachmentSystem;
                _weaponProgression = weaponProgression;
            }
            
            public int CalculateUpgradeCost(int currentLevel)
            {
                return 100 * (currentLevel + 1);
            }
            
            public bool UpgradeAttachment(ulong steamId, string attachmentId)
            {
                var session = _plugin.GetSession(steamId);
                if (session == null) return false;
                
                int currentLevel = 0;
                session.Profile.AttachmentLevels.TryGetValue(attachmentId, out currentLevel);
                
                if (currentLevel >= _config.MaxAttachmentLevel)
                {
                    return false;
                }
                
                int cost = CalculateUpgradeCost(currentLevel);
                
                if (!_economy.SpendTokens(steamId, cost))
                {
                    return false;
                }
                
                session.Profile.AttachmentLevels[attachmentId] = currentLevel + 1;
                return true;
            }
        }
        
        #endregion
        
        #region Module: BloodTokenEconomy
        
        internal class BloodTokenEconomy
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            
            internal BloodTokenEconomy(KillaDome plugin, PluginConfig config)
            {
                _plugin = plugin;
                _config = config;
            }
            
            public void AwardTokens(ulong steamId, int amount)
            {
                var session = _plugin.GetSession(steamId);
                if (session == null) return;
                
                session.Profile.Tokens += amount;
                _plugin.LogDebug($"Awarded {amount} tokens to {steamId}. New balance: {session.Profile.Tokens}");
            }
            
            public bool SpendTokens(ulong steamId, int amount)
            {
                var session = _plugin.GetSession(steamId);
                if (session == null || session.Profile.Tokens < amount)
                {
                    return false;
                }
                
                session.Profile.Tokens -= amount;
                return true;
            }
            
            public int GetBalance(ulong steamId)
            {
                var session = _plugin.GetSession(steamId);
                return session?.Profile.Tokens ?? 0;
            }
        }
        
        #endregion
        
        #region Module: StoreAPI
        
        internal class StoreAPI
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            private BloodTokenEconomy _economy;
            
            internal StoreAPI(KillaDome plugin, PluginConfig config, BloodTokenEconomy economy)
            {
                _plugin = plugin;
                _config = config;
                _economy = economy;
            }
            
            public bool PurchaseItem(ulong steamId, string itemId, int cost)
            {
                if (!_economy.SpendTokens(steamId, cost))
                {
                    return false;
                }
                
                var session = _plugin.GetSession(steamId);
                if (session == null) return false;
                
                session.Profile.OwnedSkins.Add(itemId);
                _plugin.LogDebug($"Player {steamId} purchased {itemId} for {cost} tokens");
                
                return true;
            }
            
            // Tebex integration stub
            public void ProcessTebexPurchase(ulong steamId, string packageId, string transactionId)
            {
                if (!_config.EnableTebex)
                {
                    _plugin.PrintWarning("Tebex integration is disabled");
                    return;
                }
                
                // TODO: Verify purchase with Tebex API using secret key
                // For now, just log
                _plugin.LogDebug($"Processing Tebex purchase: {steamId}, {packageId}, {transactionId}");
            }
        }
        
        #endregion
        
        #region Module: SaveManager
        
        internal class SaveManager
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            private string _dataDirectory;
            
            internal SaveManager(KillaDome plugin, PluginConfig config)
            {
                _plugin = plugin;
                _config = config;
                _dataDirectory = Path.Combine(Interface.Oxide.DataDirectory, "KillaDome");
                
                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                }
            }
            
            public PlayerProfile LoadPlayerProfile(ulong steamId)
            {
                string filePath = Path.Combine(_dataDirectory, $"{steamId}.json");
                
                if (!File.Exists(filePath))
                {
                    return new PlayerProfile(steamId, _config.StartingTokens);
                }
                
                try
                {
                    string json = File.ReadAllText(filePath);
                    var profile = JsonConvert.DeserializeObject<PlayerProfile>(json);
                    _plugin.LogDebug($"Loaded profile for {steamId}");
                    return profile ?? new PlayerProfile(steamId, _config.StartingTokens);
                }
                catch (Exception ex)
                {
                    _plugin.PrintError($"Failed to load profile for {steamId}: {ex.Message}");
                    return new PlayerProfile(steamId, _config.StartingTokens);
                }
            }
            
            public void SavePlayerProfile(PlayerProfile profile)
            {
                if (profile == null) return;
                
                profile.LastUpdated = DateTime.UtcNow;
                
                string filePath = Path.Combine(_dataDirectory, $"{profile.SteamID}.json");
                string tempPath = filePath + ".tmp";
                
                try
                {
                    string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                    File.WriteAllText(tempPath, json);
                    
                    // Atomic swap
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    File.Move(tempPath, filePath);
                    
                    _plugin.LogDebug($"Saved profile for {profile.SteamID}");
                }
                catch (Exception ex)
                {
                    _plugin.PrintError($"Failed to save profile for {profile.SteamID}: {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region Module: AntiExploit
        
        internal class AntiExploit
        {
            private KillaDome _plugin;
            private Dictionary<ulong, RateLimiter> _rateLimiters = new Dictionary<ulong, RateLimiter>();
            
            internal AntiExploit(KillaDome plugin)
            {
                _plugin = plugin;
            }
            
            public bool CheckRateLimit(ulong steamId, int maxActionsPerSecond = 5)
            {
                if (!_rateLimiters.TryGetValue(steamId, out var limiter))
                {
                    limiter = new RateLimiter(maxActionsPerSecond);
                    _rateLimiters[steamId] = limiter;
                }
                
                return limiter.AllowAction();
            }
            
            public bool ValidateAction(ulong steamId, string action)
            {
                if (!CheckRateLimit(steamId))
                {
                    _plugin.PrintWarning($"Rate limit exceeded for {steamId} on action {action}");
                    return false;
                }
                
                return true;
            }
        }
        
        internal class RateLimiter
        {
            private int _maxActions;
            private Queue<DateTime> _actions = new Queue<DateTime>();
            
            internal RateLimiter(int maxActionsPerSecond)
            {
                _maxActions = maxActionsPerSecond;
            }
            
            public bool AllowAction()
            {
                var now = DateTime.UtcNow;
                var cutoff = now.AddSeconds(-1);
                
                // Remove old actions
                while (_actions.Count > 0 && _actions.Peek() < cutoff)
                {
                    _actions.Dequeue();
                }
                
                if (_actions.Count >= _maxActions)
                {
                    return false;
                }
                
                _actions.Enqueue(now);
                return true;
            }
        }
        
        #endregion
        
        #region Module: TelemetrySystem
        
        internal class TelemetrySystem
        {
            private KillaDome _plugin;
            private Dictionary<string, int> _eventCounts = new Dictionary<string, int>();
            
            internal TelemetrySystem(KillaDome plugin)
            {
                _plugin = plugin;
            }
            
            public void RecordKill(ulong attackerId, ulong victimId)
            {
                IncrementEvent("kills");
                
                var session = _plugin.GetSession(attackerId);
                if (session != null)
                {
                    session.Profile.TotalKills++;
                }
                
                var victimSession = _plugin.GetSession(victimId);
                if (victimSession != null)
                {
                    victimSession.Profile.TotalDeaths++;
                }
            }
            
            public void RecordPurchase(ulong steamId, string itemId, int cost)
            {
                IncrementEvent("purchases");
                _plugin.LogDebug($"Telemetry: Purchase - {steamId}, {itemId}, {cost}");
            }
            
            private void IncrementEvent(string eventName)
            {
                if (!_eventCounts.ContainsKey(eventName))
                {
                    _eventCounts[eventName] = 0;
                }
                _eventCounts[eventName]++;
            }
            
            public Dictionary<string, int> GetStats()
            {
                return new Dictionary<string, int>(_eventCounts);
            }
        }
        
        #endregion
    }
}