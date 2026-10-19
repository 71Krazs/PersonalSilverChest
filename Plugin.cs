using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace PersonalSilverChest;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "local.valheim.personalsilverchest";
    public const string PluginName = "PersonalSilverChest";
    public const string PluginVersion = "1.0.0";

    private const string PrefabName = "PersonalSilverChest";
    internal const string PieceNameToken = "$silverchest_name";
    private const string PersonalChestPrefab = "piece_chest_private";
    private const string BlackMetalChestPrefab = "piece_chest_blackmetal";
    private const string TreasureChestPrefab = "piece_chest_treasure";
    private const float ChestScale = 0.9f;
    // Valheim's reinforced chest prefab is named piece_chest. The icon remains
    // chest_iron, which is why using piece_chest_iron silently fails at runtime.
    private const string ReinforcedChestPrefab = "piece_chest";

    private void Awake()
    {
        new Harmony(PluginGuid).PatchAll();
        PrefabManager.OnVanillaPrefabsAvailable += AddSilverChest;
    }

    private void OnDestroy()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddSilverChest;
    }

    private void AddSilverChest()
    {
        if (PrefabManager.Instance.GetPrefab(PrefabName) != null)
            return;

        var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, BlackMetalChestPrefab);
        prefab.transform.localScale *= ChestScale;
        var personalChest = PrefabManager.Instance.GetPrefab(PersonalChestPrefab);
        var treasureChest = PrefabManager.Instance.GetPrefab(TreasureChestPrefab);
        var personalContainer = personalChest?.GetComponentInChildren<Container>(true);
        var silverContainer = prefab.GetComponentInChildren<Container>(true);
        var personalPiece = personalChest?.GetComponent<Piece>();
        var silverPiece = prefab.GetComponent<Piece>();
        var reinforcedContainer = PrefabManager.Instance.GetPrefab(ReinforcedChestPrefab)?.GetComponentInChildren<Container>(true);

        if (personalContainer == null || silverContainer == null || personalPiece == null || silverPiece == null || reinforcedContainer == null || treasureChest == null)
        {
            Logger.LogError("Could not create PersonalSilverChest: a vanilla chest prefab was unavailable.");
            return;
        }

        // The model comes from the Black Metal Chest; access behavior comes from the Personal Chest.
        silverContainer.m_name = PieceNameToken;
        silverContainer.m_width = reinforcedContainer.m_width;
        silverContainer.m_height = reinforcedContainer.m_height;
        silverContainer.m_privacy = personalContainer.m_privacy;
        silverPiece.m_canBeRemoved = personalPiece.m_canBeRemoved;

        var treasureShellMaterial = FindMaterial(treasureChest, "BlackMetalChest_silver_mat");
        if (treasureShellMaterial == null)
        {
            Logger.LogError("Could not create PersonalSilverChest: Treasure Chest silver shell material was unavailable.");
            return;
        }

        var replacedMaterialSlots = ApplyTreasureChestShell(prefab, treasureShellMaterial);
        Logger.LogInfo($"PersonalSilverChest assigned Treasure Chest silver/wood shell material to {replacedMaterialSlots} Black Metal Chest material slots.");

        var config = new PieceConfig
        {
            Name = "$silverchest_name",
            Description = "$silverchest_description",
            PieceTable = "Hammer",
            Category = "Furniture",
            CraftingStation = "forge",
            Requirements = new[]
            {
                new RequirementConfig { Item = "FineWood", Amount = 10, Recover = true },
                new RequirementConfig { Item = "Iron", Amount = 2, Recover = true },
                new RequirementConfig { Item = "Silver", Amount = 10, Recover = true }
            }
        };

        var localization = LocalizationManager.Instance.GetLocalization();
        localization.AddTranslation("English", "$silverchest_name", "PersonalSilverChest");
        localization.AddTranslation("English", "$silverchest_description", "A private chest with the capacity of a reinforced chest.");

        if (!PieceManager.Instance.AddPiece(new CustomPiece(prefab, false, config)))
            Logger.LogError("Could not register PersonalSilverChest.");
        else
            Logger.LogInfo($"PersonalSilverChest registered with {silverContainer.m_width * silverContainer.m_height} slots.");
    }

    private static int ApplyTreasureChestShell(GameObject blackMetalChest, Material treasureShellMaterial)
    {
        var replaced = 0;
        foreach (var renderer in blackMetalChest.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            var rendererChanged = false;
            for (var i = 0; i < materials.Length; ++i)
            {
                var name = materials[i] != null ? materials[i].name : string.Empty;
                if (name.IndexOf("BlackMetalChest_mat", System.StringComparison.OrdinalIgnoreCase) < 0
                    || name.IndexOf("silver", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                materials[i] = treasureShellMaterial;
                replaced++;
                rendererChanged = true;
            }

            if (rendererChanged) renderer.sharedMaterials = materials;
        }

        return replaced;
    }

    private static Material? FindMaterial(GameObject prefab, string exactName)
    {
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        foreach (var material in renderer.sharedMaterials)
            if (material != null && string.Equals(material.name, exactName, System.StringComparison.OrdinalIgnoreCase))
                return material;
        return null;
    }
}

/// <summary>Suppresses the Hammer entry until the player has personally discovered Silver.</summary>
[HarmonyPatch(typeof(PieceTable), "UpdateAvailable")]
internal static class SilverDiscoveryGate
{
    private static readonly HashSet<long> AnnouncedPlayers = new();

    private static void Postfix(HashSet<string> __0, Player __1)
    {
        if (__1 == null) return;
        if (HasDiscoveredSilver(__1))
        {
            __0.Add(Plugin.PieceNameToken);
            if (AnnouncedPlayers.Add(__1.GetPlayerID()))
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft, "New build piece: PersonalSilverChest");
        }
        else
            __0.Remove(Plugin.PieceNameToken);
    }

    private static bool HasDiscoveredSilver(Player player)
    {
        var silver = ObjectDB.instance?.GetItemPrefab("Silver")?.GetComponent<ItemDrop>();
        return silver != null && player.IsKnownMaterial(silver.m_itemData.m_shared.m_name);
    }
}
