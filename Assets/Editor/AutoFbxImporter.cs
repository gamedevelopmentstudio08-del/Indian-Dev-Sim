#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AutoFbxImporter : AssetPostprocessor
{
    private enum Category
    {
        None,
        Buildings,
        Trees,
        BusStops,
        BusModels
    }

    private void OnPreprocessModel()
    {
        if (!assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ModelImporter importer = (ModelImporter)assetImporter;
        importer.globalScale = 1f;
        importer.importCameras = false;
        importer.importLights = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.importAnimation = false;

        // Unity 2022.3+: `importMaterials` is removed; use `materialImportMode` instead.
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.materialName = ModelImporterMaterialName.BasedOnTextureName;

        importer.meshCompression = ModelImporterMeshCompression.Medium;
        importer.isReadable = false;
        importer.optimizeMeshPolygons = true;
        importer.optimizeMeshVertices = true;
    }

    private void OnPostprocessModel(GameObject importedRoot)
    {
        if (importedRoot == null)
        {
            return;
        }

        if (!assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string name = Path.GetFileNameWithoutExtension(assetPath);
        Category category = Categorize(name);
        if (category == Category.None)
        {
            return;
        }

        string prefabPath = GetPrefabPath(category, name);
        if (File.Exists(prefabPath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(prefabPath) ?? "Assets/Resources/Auto");

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (source == null)
        {
            return;
        }

        GameObject wrapper = new GameObject(name);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
        model.transform.SetParent(wrapper.transform, false);

        // Keep runtime physics/AI clean: default to visuals-only prefabs.
        DisableAllColliders(model);

        if (category == Category.BusModels)
        {
            // Make it easier for AutoBusReplacer to find it.
            wrapper.name = "Bus";
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
        if (prefab != null)
        {
            AssetDatabase.SetLabels(prefab, new[] { "AutoImported", category.ToString() });
        }

        UnityEngine.Object.DestroyImmediate(wrapper);
    }

    private static void DisableAllColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private static Category Categorize(string assetName)
    {
        string n = assetName.ToLowerInvariant();
        if (n.Contains("busstop") || n.Contains("bus_stop") || n.Contains("stop"))
        {
            return Category.BusStops;
        }
        if (n.Contains("building") || n.Contains("house") || n.Contains("shop") || n.Contains("tower"))
        {
            return Category.Buildings;
        }
        if (n.Contains("tree") || n.Contains("palm") || n.Contains("pine"))
        {
            return Category.Trees;
        }
        if (n.Contains("bus"))
        {
            return Category.BusModels;
        }

        return Category.None;
    }

    private static string GetPrefabPath(Category category, string name)
    {
        // Runtime loaders use Resources/Auto/...
        string safeName = Sanitize(name);
        switch (category)
        {
            case Category.Buildings:
                return $"Assets/Resources/Auto/Buildings/{safeName}.prefab";
            case Category.Trees:
                return $"Assets/Resources/Auto/Trees/{safeName}.prefab";
            case Category.BusStops:
                return $"Assets/Resources/Auto/BusStops/{safeName}.prefab";
            case Category.BusModels:
                // A single, predictable name for the player bus.
                return "Assets/Resources/Bus.prefab";
            default:
                return $"Assets/Resources/Auto/Misc/{safeName}.prefab";
        }
    }

    private static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name.Trim();
    }
}
#endif
