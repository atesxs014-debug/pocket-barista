#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// "Pocket Barista > Her Şeyi Kur": modellerden materyal ve prefab üretir, tüm ScriptableObject verilerini,
/// 5 mekan sahnesini (zemin, duvar, tezgah, istasyonlar, sistemler, kamera, ışık, UI) kurar ve Build Settings'e ekler.
/// Tekrar çalıştırmak güvenlidir: üretilen varlıkların üzerine yazar. Modeller Assets/PocketBarista/Models altında olmalı.
/// </summary>
public static class PocketBaristaSetup
{
    private const string Root = "Assets/PocketBarista";
    private const string ModelsDir = Root + "/Models";
    private const string MatDir = Root + "/Generated/Materials";
    private const string PrefabDir = Root + "/Generated/Prefabs";
    private const string DataDir = Root + "/Generated/Data";
    private const string SceneDir = Root + "/Scenes";

    private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
    private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
    private static bool _urp;
    private static TMP_FontAsset _font;

    // ------------------------------------------------------------------ Menü

    [MenuItem("Pocket Barista/Her Şeyi Kur", priority = 0)]
    public static void SetupAll()
    {
        try
        {
            Step("Klasörler", 0.02f); EnsureFolders();
            Step("Proje ayarları", 0.04f); ConfigureProject();
            Step("Materyaller", 0.08f); BuildMaterials();
            Step("Model içe aktarma", 0.12f); ConfigureModelImporters();
            Step("Prefab'lar", 0.25f); BuildPrefabs();
            Step("Veriler", 0.45f); BuildData();
            for (int i = 0; i < Venues.Length; i++)
            {
                Step("Sahne: " + Venues[i].Title, 0.5f + i * 0.09f);
                BuildVenueScene(i);
            }
            Step("Build Settings", 0.97f); RegisterScenes();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath(0));
            Debug.Log("[PocketBaristaSetup] TAMAM. Play'e basın. Sahneler: " + SceneDir);
        }
        catch (Exception e)
        {
            Debug.LogError("[PocketBaristaSetup] Kurulum durdu: " + e);
            throw;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>Batch modda çağırmak için: -executeMethod PocketBaristaSetup.SetupAllBatch</summary>
    public static void SetupAllBatch()
    {
        SetupAll();
        AssetDatabase.SaveAssets();
    }

    private static void Step(string label, float p)
    {
        Debug.Log("[PocketBaristaSetup] " + label);
        if (!Application.isBatchMode) EditorUtility.DisplayProgressBar("Pocket Barista Kurulum", label, p);
    }

    private static void EnsureFolders()
    {
        foreach (string d in new[] { Root, Root + "/Generated", MatDir, PrefabDir, DataDir, SceneDir,
                                     DataDir + "/Products", DataDir + "/Customers", DataDir + "/Staff", DataDir + "/Upgrades",
                                     DataDir + "/Quests", DataDir + "/Venues", DataDir + "/Buildables" })
        {
            if (AssetDatabase.IsValidFolder(d)) continue;
            string parent = Path.GetDirectoryName(d).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(d));
        }
        if (!AssetDatabase.IsValidFolder(ModelsDir))
            throw new Exception("Modeller bulunamadı: " + ModelsDir + " (Models klasörünü buraya kopyalayın)");
    }

    private static void ConfigureProject()
    {
        _urp = GraphicsSettings.defaultRenderPipeline != null;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.companyName = "Ates";
        PlayerSettings.productName = "Pocket Barista";

        // Kod eski Input sınıfını kullanır: "Both" (2) gerekir. Unity 6 yeni projelerde sadece Input System açar.
        var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (settings != null && settings.Length > 0)
        {
            var so = new SerializedObject(settings[0]);
            SerializedProperty p = so.FindProperty("activeInputHandler");
            // Input System paketi yoksa "Both" geçersizdir: eski Input Manager (0) kullanılır
            bool hasInputSystem = File.ReadAllText("Packages/manifest.json").Contains("com.unity.inputsystem");
            int wanted = hasInputSystem ? 2 : 0;
            if (p != null && p.intValue != wanted)
            {
                p.intValue = wanted;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.LogWarning("[PocketBaristaSetup] Active Input Handling = " + wanted + " yapıldı. Editörü yeniden başlatmanız gerekebilir.");
            }
        }

        try { _font = TMP_Settings.defaultFontAsset; } catch (Exception) { _font = null; } // TMP Settings yoksa
        if (_font == null && !AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) PocketBaristaCI.ImportTmpEssentials();
        if (_font == null)
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (_font == null)
            Debug.LogWarning("[PocketBaristaSetup] TMP font bulunamadı. Window > TextMeshPro > Import TMP Essential Resources yapıp kurulumu tekrar çalıştırın.");
    }

    // ------------------------------------------------------------------ Materyaller

    private static void BuildMaterials()
    {
        Materials.Clear();
        Shader shader = _urp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Standard");

        foreach (string mtl in Directory.GetFiles(ModelsDir, "*.mtl"))
        {
            string current = null;
            Color kd = Color.white;
            Color ke = Color.black;
            float alpha = 1f;
            foreach (string raw in File.ReadAllLines(mtl))
            {
                string line = raw.Trim();
                if (line.StartsWith("newmtl ", StringComparison.Ordinal))
                {
                    if (current != null) MakeMaterial(shader, current, kd, ke, alpha);
                    current = line.Substring(7).Trim();
                    kd = Color.white; ke = Color.black; alpha = 1f;
                }
                else if (line.StartsWith("Kd ", StringComparison.Ordinal)) kd = ParseColor(line);
                else if (line.StartsWith("Ke ", StringComparison.Ordinal)) ke = ParseColor(line);
                else if (line.StartsWith("d ", StringComparison.Ordinal)) alpha = float.Parse(line.Substring(2), CultureInfo.InvariantCulture);
            }
            if (current != null) MakeMaterial(shader, current, kd, ke, alpha);
        }
        Debug.Log($"[PocketBaristaSetup] {Materials.Count} materyal hazır.");
    }

    private static Color ParseColor(string line)
    {
        string[] p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return new Color(float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture), float.Parse(p[3], CultureInfo.InvariantCulture));
    }

    private static void MakeMaterial(Shader shader, string name, Color kd, Color ke, float alpha)
    {
        if (Materials.ContainsKey(name)) return;
        string path = $"{MatDir}/{name}.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = shader;
        Color c = new Color(kd.r, kd.g, kd.b, alpha);
        if (_urp)
        {
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.25f);
        }
        else
        {
            m.SetColor("_Color", c);
            m.SetFloat("_Glossiness", 0.2f);
        }
        if (ke.maxColorComponent > 0.01f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", ke * 1.6f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        if (alpha < 0.99f) MakeTransparent(m);
        EditorUtility.SetDirty(m);
        Materials[name] = m;
    }

    private static void MakeTransparent(Material m)
    {
        if (_urp)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        else
        {
            m.SetFloat("_Mode", 3f);
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON");
        }
        m.renderQueue = (int)RenderQueue.Transparent;
    }

    private static void ConfigureModelImporters()
    {
        foreach (string obj in Directory.GetFiles(ModelsDir, "*.obj"))
        {
            string path = obj.Replace('\\', '/');
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) continue;
            bool changed = false;
            if (Math.Abs(imp.globalScale - 1f) > 0.0001f) { imp.globalScale = 1f; changed = true; }
            if (imp.importAnimation) { imp.importAnimation = false; changed = true; }
            if (imp.isReadable) { imp.isReadable = false; changed = true; }
            if (changed) imp.SaveAndReimport();
        }
    }

    // ------------------------------------------------------------------ Model yardımcıları

    /// <summary>OBJ modelini sahnede örnekler ve kendi materyallerimizi isimle eşler.</summary>
    private static GameObject Model(string name, Transform parent = null, Vector3 pos = default, float rotY = 0f, float scale = 1f)
    {
        GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsDir}/{name}.obj");
        if (src == null) throw new Exception("Model yok: " + name);
        GameObject go = (GameObject)UnityEngine.Object.Instantiate(src, parent);
        go.name = name;
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        go.transform.localScale = Vector3.one * scale;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                string key = mats[i] != null ? mats[i].name.Replace(" (Instance)", "") : "";
                if (Materials.TryGetValue(key, out Material m)) mats[i] = m;
            }
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.On;
        }
        return go;
    }

    private static Transform Child(Transform parent, string name, Vector3 localPos, float rotY = 0f)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        t.localRotation = Quaternion.Euler(0f, rotY, 0f);
        return t;
    }

    private static void FitBoxCollider(GameObject go, bool trigger = false)
    {
        var b = new Bounds(go.transform.position, Vector3.zero);
        bool any = false;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        var col = go.GetComponent<BoxCollider>() ?? go.AddComponent<BoxCollider>();
        col.center = go.transform.InverseTransformPoint(b.center);
        col.size = new Vector3(Mathf.Max(0.1f, b.size.x), Mathf.Max(0.1f, b.size.y), Mathf.Max(0.1f, b.size.z));
        col.isTrigger = trigger;
    }

    private static GameObject SavePrefab(GameObject go, string name)
    {
        string path = $"{PrefabDir}/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        Prefabs[name] = prefab;
        return prefab;
    }

    private static GameObject P(string name)
    {
        if (Prefabs.TryGetValue(name, out GameObject p) && p != null) return p;
        p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
        if (p == null) throw new Exception("Prefab yok: " + name);
        Prefabs[name] = p;
        return p;
    }

    // ------------------------------------------------------------------ Serialized alan yazma

    private static void Set(UnityEngine.Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        SetProp(so, field, value);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetProp(SerializedObject so, string field, object value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null) throw new Exception($"Alan yok: {so.targetObject.GetType().Name}.{field}");
        Assign(p, value);
    }

    private static void Assign(SerializedProperty p, object value)
    {
        if (value == null) { p.objectReferenceValue = null; return; }
        if (value is string s) { p.stringValue = s; return; }
        if (value is bool b) { p.boolValue = b; return; }
        if (value is int i)
        {
            if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = i;
            else if (p.propertyType == SerializedPropertyType.Float) p.floatValue = i;
            else if (p.type == "long") p.longValue = i;
            else p.intValue = i;
            return;
        }
        if (value is long l) { p.longValue = l; return; }
        if (value is float f) { if (p.type == "double") p.doubleValue = f; else p.floatValue = f; return; }
        if (value is double d) { if (p.type == "double") p.doubleValue = d; else p.floatValue = (float)d; return; }
        if (value is Enum e) { p.enumValueIndex = Array.IndexOf(Enum.GetValues(e.GetType()), e); return; }
        if (value is Vector2Int v2) { p.vector2IntValue = v2; return; }
        if (value is Vector3 v3) { p.vector3Value = v3; return; }
        if (value is Color c) { p.colorValue = c; return; }
        if (value is Bounds bo) { p.boundsValue = bo; return; }
        if (value is UnityEngine.Object o) { p.objectReferenceValue = o; return; }
        if (value is Action<SerializedProperty> custom) { custom(p); return; }
        if (value is Array arr)
        {
            p.arraySize = arr.Length;
            for (int k = 0; k < arr.Length; k++) Assign(p.GetArrayElementAtIndex(k), arr.GetValue(k));
            return;
        }
        throw new Exception("Desteklenmeyen tip: " + value.GetType() + " (" + p.propertyPath + ")");
    }

    private static T Asset<T>(string path, params (string field, object value)[] fields) where T : ScriptableObject
    {
        T a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null)
        {
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
        }
        var so = new SerializedObject(a);
        foreach (var fv in fields) SetProp(so, fv.field, fv.value);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(a);
        return a;
    }

    private static T Comp<T>(GameObject go, params (string field, object value)[] fields) where T : Component
    {
        T c = go.GetComponent<T>() ?? go.AddComponent<T>();
        if (fields.Length > 0)
        {
            var so = new SerializedObject(c);
            foreach (var fv in fields) SetProp(so, fv.field, fv.value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return c;
    }

    // ------------------------------------------------------------------ Prefab'lar

    private static readonly string[] Products3D = { "coffee_cup", "croissant", "tea_cup", "donut", "smoothie_cup", "sandwich", "cupcake", "cake_slice" };

    private static void BuildPrefabs()
    {
        Prefabs.Clear();

        // Servis edilen ürünler (sadece görsel)
        foreach (string p in Products3D) SavePrefab(Model(p), "Item_" + p);

        // Para yığını
        {
            var root = new GameObject("MoneyPile");
            GameObject vis = Model("money_pile", root.transform);
            Comp<MoneyPile>(root, ("stackLevels", new UnityEngine.Object[] { vis }));
            FitBoxCollider(root);
            root.GetComponent<BoxCollider>().size += new Vector3(0.25f, 0.2f, 0.25f); // Parmakla kolay dokunma
            SavePrefab(root, "MoneyPile");
        }

        // Masalar: masa + sandalye, müşteri sandalyede oturur
        TablePrefab("Table_Round", "table_round", "chair_wood", 0.62f, 0.76f);
        TablePrefab("Table_Square", "table_square", "chair_wood", 0.66f, 0.76f);
        TablePrefab("Table_Long", "table_long", "sofa_booth", 0.75f, 0.76f, seatScale: 0.6f);
        TablePrefab("Table_High", "high_table", "bar_stool", 0.55f, 1.06f);
        TablePrefab("Table_Modern", "table_round", "modern_chair", 0.62f, 0.76f);

        // İstasyonlar: kısa tezgah + makine
        StationPrefab("Station_Espresso", "espresso_machine", StationType.Espresso);
        StationPrefab("Station_Oven", "oven", StationType.Oven);
        StationPrefab("Station_Tea", "tea_station", StationType.Tea);
        StationPrefab("Station_Blender", "blender_station", StationType.Blender);

        // Müşteriler ve personel
        foreach (string c in new[] { "customer_student", "customer_business", "customer_local", "customer_tourist", "customer_elder" })
        {
            var root = new GameObject(c);
            Model(c, root.transform);
            Transform hand = Child(root.transform, "Hand", new Vector3(0.25f, 0.85f, 0.18f));
            Comp<CustomerAI>(root, ("handAnchor", hand));
            SavePrefab(root, "Customer_" + c.Replace("customer_", ""));
        }
        foreach (string s in new[] { "barista", "waiter", "cashier" })
        {
            var root = new GameObject(s);
            Model(s, root.transform);
            Transform hand = Child(root.transform, "Hand", new Vector3(0.27f, 1.08f, 0.18f));
            Comp<StaffAI>(root, ("handAnchor", hand));
            SavePrefab(root, "Staff_" + s);
        }

        // Dekor (yerleştirilebilir, collider'lı)
        foreach (string d in DecorModels) { var root = new GameObject(d); Model(d, root.transform); FitBoxCollider(root); SavePrefab(root, "Decor_" + d); }

        // Uçan para yazısı
        {
            var go = new GameObject("FloatingText");
            var tmp = go.AddComponent<TextMeshPro>();
            if (_font != null) tmp.font = _font;
            tmp.fontSize = 5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.2f;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 1.2f);
            Comp<FloatingText>(go, ("label", tmp));
            SavePrefab(go, "FloatingText");
        }

        // UI satırı
        SavePrefab(BuildRowPrefab(), "UI_Row");
    }

    private static readonly string[] DecorModels =
    {
        "plant_small", "plant_tall", "bookshelf", "rug", "menu_board", "armchair", "pastry_display", "coffee_bag_shelf",
        "beanbag", "chalkboard_stand", "bike_rack", "parasol", "lounger", "palm_tree", "surfboard", "planter_box", "coat_rack",
        "fire_pit", "string_lights", "tip_jar"
    };

    private static void TablePrefab(string prefabName, string table, string seat, float seatOffset, float topY, float seatScale = 1f)
    {
        var root = new GameObject(prefabName);
        Model(table, root.transform);
        Model(seat, root.transform, new Vector3(-seatOffset, 0f, 0f), seat == "sofa_booth" ? 90f : 90f, seatScale);
        Transform seatPt = Child(root.transform, "Seat", new Vector3(-seatOffset, 0f, 0f), 90f);
        Transform item = Child(root.transform, "ItemAnchor", new Vector3(-0.15f, topY, 0f));
        Transform pile = Child(root.transform, "PileAnchor", new Vector3(0.12f, topY, 0.1f));
        Comp<Table>(root, ("seatPoint", seatPt), ("itemAnchor", item), ("pileAnchor", pile));
        FitBoxCollider(root);
        SavePrefab(root, prefabName);
    }

    private static void StationPrefab(string prefabName, string machine, StationType type)
    {
        var root = new GameObject(prefabName);
        Model("counter", root.transform, Vector3.zero, 0f, 1f).transform.localScale = new Vector3(0.6f, 1f, 1f);
        Model(machine, root.transform, new Vector3(0f, 0.98f, -0.05f));
        Transform work = Child(root.transform, "WorkPoint", new Vector3(0f, 0f, -0.75f), 0f);
        Comp<Station>(root, ("type", type), ("workPoint", work));
        FitBoxCollider(root);
        SavePrefab(root, prefabName);
    }

    // ------------------------------------------------------------------ Veriler

    private static GameContentSO _content;
    private static ProgressionConfigSO _progression;
    private static EconomyConfig _economy;
    private static LocalizationTableSO _loc;
    private static BuildableCatalogSO _catalog;
    private static readonly Dictionary<string, BuildableItemSO> Buildables = new Dictionary<string, BuildableItemSO>();
    private static readonly List<VenueSO> VenueAssets = new List<VenueSO>();

    private struct VenueDef
    {
        public string Id, Title, Scene, Floor;
        public float Price; public double Threshold; public long StartMoney;
        public (string id, float w)[] Weights;
        public Color Ambient;
        public bool Walls, GlassWalls;
        public string[] Decor;
        public (string id, string key, long cost, int lvl)[] Areas;
    }

    private static readonly VenueDef[] Venues =
    {
        new VenueDef { Id = "corner", Title = "Mahalle Köşesi", Scene = "Venue1_Corner", Floor = "floor_tile", Price = 1f, Threshold = 1, StartMoney = 150,
            Weights = new[] { ("local", 2f), ("elder", 1.5f), ("student", 1f), ("business", 0.5f), ("tourist", 0.3f) },
            Ambient = new Color(0.30f, 0.26f, 0.23f), Walls = true, Decor = new[] { "bookshelf", "plant_tall", "menu_board", "rug" },
            Areas = new[] { ("backroom", "area.backroom", 2000L, 3), ("terrace", "area.terrace", 15000L, 7) } },
        new VenueDef { Id = "campus", Title = "Kampüs Kafesi", Scene = "Venue2_Campus", Floor = "floor_tile", Price = 3f, Threshold = 4, StartMoney = 1000,
            Weights = new[] { ("student", 3f), ("local", 1f), ("elder", 0.3f), ("business", 0.5f), ("tourist", 0.5f) },
            Ambient = new Color(0.33f, 0.30f, 0.27f), Walls = true, Decor = new[] { "chalkboard_stand", "beanbag", "bike_rack", "plant_tall" },
            Areas = new[] { ("study_hall", "area.study_hall", 20000L, 5), ("garden", "area.garden", 120000L, 9) } },
        new VenueDef { Id = "beach", Title = "Sahil Kafesi", Scene = "Venue3_Beach", Floor = "sand_tile", Price = 8f, Threshold = 15, StartMoney = 10000,
            Weights = new[] { ("tourist", 3f), ("student", 1f), ("local", 0.5f), ("business", 0.3f), ("elder", 0.7f) },
            Ambient = new Color(0.55f, 0.52f, 0.48f), Walls = false, Decor = new[] { "parasol", "lounger", "palm_tree", "surfboard" },
            Areas = new[] { ("sundeck", "area.sundeck", 200000L, 8), ("pier", "area.pier", 1500000L, 12) } },
        new VenueDef { Id = "downtown", Title = "Şehir Merkezi", Scene = "Venue4_Downtown", Floor = "marble_tile", Price = 20f, Threshold = 60, StartMoney = 100000,
            Weights = new[] { ("business", 3f), ("local", 1f), ("tourist", 1f), ("student", 0.5f), ("elder", 0.5f) },
            Ambient = new Color(0.35f, 0.35f, 0.38f), Walls = true, GlassWalls = true, Decor = new[] { "planter_box", "coat_rack", "plant_tall", "menu_board" },
            Areas = new[] { ("upstairs", "area.upstairs", 3000000L, 10), ("lounge", "area.lounge", 25000000L, 15) } },
        new VenueDef { Id = "rooftop", Title = "Neon Çatı Katı", Scene = "Venue5_Rooftop", Floor = "concrete_tile", Price = 50f, Threshold = 250, StartMoney = 1000000,
            Weights = new (string, float)[0],
            Ambient = new Color(0.20f, 0.17f, 0.25f), Walls = false, Decor = new[] { "fire_pit", "string_lights", "planter_box" },
            Areas = new[] { ("bar", "area.bar", 60000000L, 12), ("sky_terrace", "area.sky_terrace", 500000000L, 18) } },
    };

    private static string ScenePath(int i) => $"{SceneDir}/{Venues[i].Scene}.unity";

    private static void BuildData()
    {
        _economy = Asset<EconomyConfig>($"{DataDir}/EconomyConfig.asset", ("startingMoney", 150L), ("saveFileName", "pocket_barista_save.json"));
        _progression = Asset<ProgressionConfigSO>($"{DataDir}/ProgressionConfig.asset");

        // Yerelleştirme: CSV'yi tabloya aktar
        _loc = Asset<LocalizationTableSO>($"{DataDir}/LocalizationTable.asset");
        string csv = FindFile("localization_starter.csv");
        if (csv != null) ImportCsv(_loc, csv);
        else Debug.LogWarning("[PocketBaristaSetup] localization_starter.csv bulunamadı; metinler anahtar olarak görünecek.");

        // Ürünler
        var products = new List<ProductSO>
        {
            Product("espresso", "coffee_cup", StationType.Espresso, 5, 1, 0, 1, true, 2.5f, 5f),
            Product("croissant", "croissant", StationType.Oven, 8, 2, 150, 2, false, 4f, 6f),
            Product("tea", "tea_cup", StationType.Tea, 6, 2, 300, 3, false, 3f, 6f),
            Product("latte", "coffee_cup", StationType.Espresso, 12, 3, 600, 4, false, 3.5f, 6f),
            Product("donut", "donut", StationType.Oven, 15, 4, 1500, 6, false, 4f, 6f),
            Product("smoothie", "smoothie_cup", StationType.Blender, 25, 5, 4000, 8, false, 4.5f, 7f),
            Product("sandwich", "sandwich", StationType.Oven, 35, 6, 10000, 10, false, 5f, 8f),
            Product("cupcake", "cupcake", StationType.Oven, 50, 8, 30000, 13, false, 4f, 6f),
            Product("cake", "cake_slice", StationType.Oven, 80, 10, 90000, 16, false, 5f, 8f),
        };

        // Müşteri tipleri: (id, ağırlık, açılış, sabır, hız, fiyat, bahşiş şansı, bahşiş oranı)
        var customers = new List<CustomerTypeSO>
        {
            Customer("student", 1.2f, 1, 0.7f, 1.15f, 0.8f, 0.10f, 0.10f),
            Customer("local", 1.0f, 1, 1.3f, 0.9f, 1.0f, 0.30f, 0.15f),
            Customer("business", 0.8f, 4, 0.8f, 1.2f, 1.5f, 0.40f, 0.25f),
            Customer("tourist", 0.8f, 7, 1.0f, 0.95f, 1.3f, 0.50f, 0.30f),
            Customer("elder", 0.6f, 10, 1.6f, 0.7f, 1.1f, 0.25f, 0.20f),
        };

        var staff = new List<StaffSO>
        {
            Staff("barista", StaffRole.Barista, 500, 2, 2.2f, 1f),
            Staff("waiter", StaffRole.Waiter, 1500, 4, 2.6f, 1f),
            Staff("cashier", StaffRole.Cashier, 3000, 6, 2.4f, 1f),
        };

        var upgrades = new List<UpgradeSO>
        {
            Upgrade("income", UpgradeStat.IncomeMultiplier, 50, 100, 0.10f, 1),
            Upgrade("tips", UpgradeStat.TipMultiplier, 25, 150, 0.10f, 1),
            Upgrade("prep", UpgradeStat.PrepSpeed, 25, 120, 0.08f, 1),
            Upgrade("spawn", UpgradeStat.CustomerSpawnRate, 25, 250, 0.08f, 2),
            Upgrade("patience", UpgradeStat.CustomerPatience, 20, 200, 0.10f, 3),
            Upgrade("eat_speed", UpgradeStat.EatSpeed, 20, 300, 0.06f, 3),
            Upgrade("staff_speed", UpgradeStat.StaffMoveSpeed, 20, 400, 0.06f, 4),
            Upgrade("offline_cap", UpgradeStat.OfflineCapHours, 10, 2000, 1f, 5),
            Upgrade("offline_rate", UpgradeStat.OfflineEarningRate, 20, 1500, 0.10f, 5),
        };

        var quests = new List<QuestSO>
        {
            Quest("serve_5", QuestType.ServeCustomers, 5, RewardType.Money, 100),
            Quest("place_2", QuestType.PlaceItems, 2, RewardType.Money, 150),
            Quest("earn_500", QuestType.EarnMoney, 500, RewardType.XP, 50),
            Quest("unlock_product", QuestType.UnlockProducts, 1, RewardType.Money, 300),
            Quest("hire_1", QuestType.HireStaff, 1, RewardType.IncomeBoostMinutes, 15),
            Quest("upgrade_3", QuestType.BuyUpgrades, 3, RewardType.Money, 1000),
            Quest("level_5", QuestType.ReachLevel, 5, RewardType.Money, 2000),
            Quest("tips_200", QuestType.CollectTips, 200, RewardType.IncomeBoostMinutes, 30),
        };

        VenueAssets.Clear();
        foreach (VenueDef v in Venues)
        {
            var weights = new List<Action<SerializedProperty>>();
            foreach (var cw in v.Weights)
            {
                string cid = cw.Item1;
                float w = cw.Item2;
                CustomerTypeSO ct = customers.Find(c => c.Id == cid);
                weights.Add(p => { p.FindPropertyRelative("type").objectReferenceValue = ct; p.FindPropertyRelative("weightMultiplier").floatValue = w; });
            }
            VenueAssets.Add(Asset<VenueSO>($"{DataDir}/Venues/{v.Id}.asset",
                ("id", v.Id), ("nameKey", "venue." + v.Id), ("descriptionKey", $"venue.{v.Id}.desc"), ("sceneName", v.Scene),
                ("priceMultiplier", v.Price), ("prestigeThresholdMultiplier", v.Threshold), ("startingMoney", v.StartMoney),
                ("customerWeights", weights.ToArray()), ("ambientColor", v.Ambient)));
        }

        _content = Asset<GameContentSO>($"{DataDir}/GameContent.asset",
            ("products", products.ToArray()), ("customerTypes", customers.ToArray()), ("staff", staff.ToArray()),
            ("upgrades", upgrades.ToArray()), ("quests", quests.ToArray()), ("venues", VenueAssets.ToArray()));

        // Yerleştirilebilir eşyalar
        Buildables.Clear();
        var items = new List<BuildableItemSO>
        {
            Buildable("table_round", "Table_Round", "item.table_round", 120, 1, 15, BuildableItemSO.ShopCategory.Seating, new Vector2Int(2, 1)),
            Buildable("table_square", "Table_Square", "item.table_square", 400, 3, 30, BuildableItemSO.ShopCategory.Seating, new Vector2Int(2, 1)),
            Buildable("table_modern", "Table_Modern", "item.table_modern", 1500, 6, 60, BuildableItemSO.ShopCategory.Seating, new Vector2Int(2, 1)),
            Buildable("table_high", "Table_High", "item.table_high", 6000, 9, 120, BuildableItemSO.ShopCategory.Seating, new Vector2Int(2, 1)),
            Buildable("table_long", "Table_Long", "item.table_long", 25000, 12, 250, BuildableItemSO.ShopCategory.Seating, new Vector2Int(2, 2)),
            Buildable("station_oven", "Station_Oven", "item.station_oven", 800, 2, 50, BuildableItemSO.ShopCategory.Station, new Vector2Int(2, 1)),
            Buildable("station_tea", "Station_Tea", "item.station_tea", 1200, 3, 60, BuildableItemSO.ShopCategory.Station, new Vector2Int(2, 1)),
            Buildable("station_espresso", "Station_Espresso", "item.station_espresso", 2500, 5, 80, BuildableItemSO.ShopCategory.Station, new Vector2Int(2, 1)),
            Buildable("station_blender", "Station_Blender", "item.station_blender", 3500, 7, 100, BuildableItemSO.ShopCategory.Station, new Vector2Int(2, 1)),
        };
        int decorPrice = 60;
        foreach (string d in DecorModels)
        {
            items.Add(Buildable(d, "Decor_" + d, "item." + d, decorPrice, 1 + items.Count / 4, decorPrice / 5, BuildableItemSO.ShopCategory.Decor, Vector2Int.one, keepPlacing: true));
            decorPrice = Mathf.RoundToInt(decorPrice * 1.35f);
        }
        _catalog = Asset<BuildableCatalogSO>($"{DataDir}/BuildableCatalog.asset", ("items", items.ToArray()));
    }

    private static ProductSO Product(string id, string model, StationType st, long price, long xp, long unlockCost, int lvl, bool def, float prep, float consume) =>
        Asset<ProductSO>($"{DataDir}/Products/{id}.asset", ("id", id), ("nameKey", "product." + id), ("servedPrefab", P("Item_" + model)),
            ("basePrice", price), ("xpPerSale", xp), ("unlockCost", unlockCost), ("unlockLevel", lvl), ("unlockedByDefault", def),
            ("station", st), ("prepSeconds", prep), ("consumeSeconds", consume));

    private static CustomerTypeSO Customer(string id, float weight, int lvl, float patience, float move, float price, float tipChance, float tipRatio) =>
        Asset<CustomerTypeSO>($"{DataDir}/Customers/{id}.asset", ("id", id), ("nameKey", "customer." + id),
            ("prefab", P("Customer_" + id).GetComponent<CustomerAI>()), ("spawnWeight", weight), ("unlockLevel", lvl),
            ("patienceMultiplier", patience), ("moveSpeedMultiplier", move), ("priceMultiplier", price),
            ("tipChance", tipChance), ("tipRatio", tipRatio), ("ordersPerVisit", new Vector2Int(1, 1)));

    private static StaffSO Staff(string id, StaffRole role, long cost, int lvl, float move, float work) =>
        Asset<StaffSO>($"{DataDir}/Staff/{id}.asset", ("id", id), ("nameKey", "staff." + id), ("prefab", P("Staff_" + id).GetComponent<StaffAI>()),
            ("role", role), ("hireCost", cost), ("unlockLevel", lvl), ("moveSpeed", move), ("workSpeed", work));

    private static UpgradeSO Upgrade(string id, UpgradeStat stat, int max, long cost, float per, int lvl) =>
        Asset<UpgradeSO>($"{DataDir}/Upgrades/{id}.asset", ("id", id), ("nameKey", "upgrade." + id), ("stat", stat),
            ("maxLevel", max), ("baseCost", cost), ("costGrowth", 1.15), ("valuePerLevel", per), ("unlockLevel", lvl));

    private static QuestSO Quest(string id, QuestType type, long target, RewardType reward, long amount) =>
        Asset<QuestSO>($"{DataDir}/Quests/{id}.asset", ("id", id), ("descriptionKey", "quest." + id), ("type", type),
            ("target", target), ("rewardType", reward), ("rewardAmount", amount));

    private static BuildableItemSO Buildable(string id, string prefab, string nameKey, long price, int lvl, long xp,
        BuildableItemSO.ShopCategory cat, Vector2Int footprint, bool keepPlacing = false)
    {
        GameObject pf = P(prefab);
        BuildableItemSO item = Asset<BuildableItemSO>($"{DataDir}/Buildables/{id}.asset", ("id", id), ("displayName", nameKey),
            ("price", price), ("unlockLevel", lvl), ("xpOnPlace", xp), ("category", cat), ("prefab", pf),
            ("footprint", footprint), ("snapToGrid", true), ("keepPlacingAfterBuild", keepPlacing));
        Buildables[id] = item;
        return item;
    }

    private static string FindFile(string fileName)
    {
        string[] hits = Directory.GetFiles(Application.dataPath, fileName, SearchOption.AllDirectories);
        return hits.Length > 0 ? hits[0] : null;
    }

    private static void ImportCsv(LocalizationTableSO table, string path)
    {
        var rows = new List<(string k, string tr, string en)>();
        string[] lines = File.ReadAllLines(path);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] c = lines[i].Split(',');
            if (c.Length < 3 || string.IsNullOrWhiteSpace(c[0])) continue;
            rows.Add((c[0].Trim(), c[1].Trim(), string.Join(",", c, 2, c.Length - 2).Trim()));
        }
        // Mağaza eşya adları (CSV'de olmayanlar)
        foreach (var t in ExtraTexts) rows.Add((t.Item1, t.Item2, t.Item3));

        var so = new SerializedObject(table);
        SerializedProperty arr = so.FindProperty("entries");
        var seen = new HashSet<string>();
        arr.arraySize = 0;
        foreach (var row in rows)
        {
            string k = row.k, tr = row.tr, en = row.en;
            if (!seen.Add(k)) continue;
            arr.arraySize++;
            SerializedProperty e = arr.GetArrayElementAtIndex(arr.arraySize - 1);
            e.FindPropertyRelative("key").stringValue = k;
            e.FindPropertyRelative("tr").stringValue = tr;
            e.FindPropertyRelative("en").stringValue = en;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);
    }

    private static readonly (string, string, string)[] ExtraTexts =
    {
        ("item.table_round", "Yuvarlak Masa", "Round Table"), ("item.table_square", "Kare Masa", "Square Table"),
        ("item.table_modern", "Modern Masa", "Modern Table"), ("item.table_high", "Bar Masası", "High Table"),
        ("item.table_long", "Koltuklu Masa", "Booth Table"), ("item.station_oven", "Fırın", "Oven"),
        ("item.station_tea", "Çay Ocağı", "Tea Station"), ("item.station_espresso", "Espresso Makinesi", "Espresso Machine"),
        ("item.station_blender", "Smoothie Blender", "Smoothie Blender"), ("item.plant_small", "Kaktüs", "Cactus"),
        ("item.plant_tall", "Saksı Bitkisi", "Potted Plant"), ("item.bookshelf", "Kitaplık", "Bookshelf"), ("item.rug", "Halı", "Rug"),
        ("item.menu_board", "Menü Panosu", "Menu Board"), ("item.armchair", "Berjer", "Armchair"),
        ("item.pastry_display", "Pasta Vitrini", "Pastry Display"), ("item.coffee_bag_shelf", "Kahve Rafı", "Coffee Shelf"),
        ("item.beanbag", "Armut Koltuk", "Beanbag"), ("item.chalkboard_stand", "Kara Tahta", "Chalkboard"),
        ("item.bike_rack", "Bisiklet Parkı", "Bike Rack"), ("item.parasol", "Şemsiye", "Parasol"), ("item.lounger", "Şezlong", "Lounger"),
        ("item.palm_tree", "Palmiye", "Palm Tree"), ("item.surfboard", "Sörf Tahtası", "Surfboard"), ("item.planter_box", "Saksı Kutusu", "Planter"),
        ("item.coat_rack", "Askılık", "Coat Rack"), ("item.fire_pit", "Ateş Çukuru", "Fire Pit"), ("item.string_lights", "Işık Zinciri", "String Lights"),
        ("item.tip_jar", "Bahşiş Kavanozu", "Tip Jar"),
        ("ui.buy_area", "Bölgeyi Aç", "Unlock Area"), ("ui.close", "Kapat", "Close"), ("ui.level", "Seviye", "Level"),
    };

    // ------------------------------------------------------------------ Sahne

    private const float RoomW = 10f, RoomD = 12f; // X: -5..5, Z: -6..6

    private static void BuildVenueScene(int index)
    {
        VenueDef v = Venues[index];
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Işık ve atmosfer
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = v.Ambient;
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = v.Id == "beach" ? new Color(1f, 0.95f, 0.85f) : new Color(1f, 0.85f, 0.7f);
        sun.intensity = v.Id == "beach" ? 1.2f : v.Id == "rooftop" ? 0.35f : 0.75f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

        Transform env = new GameObject("Environment").transform;
        BuildRoom(env, v);
        BuildCounter(env, v, out Transform counterRoot);

        // Lamba noktası ışıkları (tavanlı mekanlarda)
        Transform lights = new GameObject("Lamps").transform;
        foreach (Vector3 lp in new[] { new Vector3(-2.5f, 2.3f, -1f), new Vector3(2.5f, 2.3f, 1.5f), new Vector3(0f, 2.3f, -3.6f), new Vector3(-2f, 2.3f, 3f) })
        {
            if (v.Walls) Model("pendant_lamp", lights, lp - new Vector3(0, 0.15f, 0));
            var l = new GameObject("PointLight").AddComponent<Light>();
            l.transform.SetParent(lights, false);
            l.transform.localPosition = lp;
            l.type = LightType.Point;
            l.range = 5.5f;
            l.intensity = v.Id == "beach" ? 0.4f : 1.4f;
            l.color = v.Id == "rooftop" ? new Color(0.85f, 0.5f, 1f) : new Color(1f, 0.72f, 0.42f);
        }

        // Tema dekoru (yerleştirilmiş, satın alınmış sayılmaz)
        Vector3[] decorSpots = { new Vector3(-4.3f, 0, -5.2f), new Vector3(4.3f, 0, -2f), new Vector3(-4.3f, 0, 2.5f), new Vector3(3.8f, 0, 4.6f) };
        for (int i = 0; i < v.Decor.Length && i < decorSpots.Length; i++)
            Model(v.Decor[i], env, decorSpots[i], i * 70f);

        // Sistemler
        Transform sys = new GameObject("Systems").transform;
        var gm = new GameObject("GameManager"); // Kök nesne olmalı (DontDestroyOnLoad)
        Comp<GameManager>(gm, ("config", _economy), ("content", _content), ("progression", _progression), ("localizationTable", _loc));

        Transform pickup = Child(sys, "PickupPoint", new Vector3(2.2f, 0f, -3.6f));
        Comp<OrderBoard>(new GameObject("OrderBoard").Also(sys), ("pickupPoint", pickup));
        Comp<ServedItemPool>(new GameObject("ServedItemPool").Also(sys));
        Comp<MoneyPilePool>(new GameObject("MoneyPilePool").Also(sys), ("prefab", P("MoneyPile").GetComponent<MoneyPile>()));
        Comp<FloatingTextPool>(new GameObject("FloatingTextPool").Also(sys), ("prefab", P("FloatingText").GetComponent<FloatingText>()));
        Comp<AudioManager>(new GameObject("AudioManager")); // Kök nesne (DontDestroyOnLoad)

        Transform spawn = Child(sys, "CustomerSpawn", new Vector3(3.6f, 0f, 8.5f));
        Transform door = Child(sys, "Door", new Vector3(3.6f, 0f, 5.6f));
        Transform exit = Child(sys, "CustomerExit", new Vector3(4.6f, 0f, 9f));
        Comp<CustomerSpawner>(new GameObject("CustomerSpawner").Also(sys), ("spawnPoint", spawn), ("doorPoint", door), ("exitPoint", exit), ("logTransitions", false));

        Comp<StaffSpawner>(new GameObject("StaffSpawner").Also(sys),
            ("baristaHome", Child(sys, "BaristaHome", new Vector3(-1.5f, 0f, -5.3f))),
            ("waiterHome", Child(sys, "WaiterHome", new Vector3(3.2f, 0f, -3.4f))),
            ("cashierHome", Child(sys, "CashierHome", new Vector3(1.8f, 0f, -5.3f))));

        // Kamera
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = v.Id == "beach" ? new Color(0.45f, 0.7f, 0.85f) : new Color(0.07f, 0.065f, 0.07f);
        camGo.transform.rotation = Quaternion.Euler(50f, 225f, 0f); // Arka (-Z) ve sol (-X) duvarlara bakar
        camGo.transform.position = Vector3.zero - camGo.transform.forward * 30f;
        camGo.AddComponent<PhysicsRaycaster>();
        if (_urp) AddUrpCameraData(camGo);

        // Yerleştirme + kalıcı düzen
        var placementGo = new GameObject("BuildingPlacement");
        placementGo.transform.SetParent(sys);
        Transform placedRoot = new GameObject("PlacedItems").transform;
        var placement = Comp<BuildingPlacementManager>(placementGo, ("worldCamera", cam), ("placedItemsRoot", placedRoot),
            ("cellSize", 0.5f), ("buildArea", new Bounds(new Vector3(0f, 1f, 0.5f), new Vector3(RoomW - 1f, 2f, RoomD - 4f))),
            ("colorProperty", _urp ? "_BaseColor" : "_Color"));
        Comp<ShopLayoutService>(new GameObject("ShopLayout").Also(sys), ("catalog", _catalog), ("placementManager", placement),
            ("startingLayout", new Action<SerializedProperty>[]
            {
                p => { p.FindPropertyRelative("item").objectReferenceValue = Buildables["table_round"]; p.FindPropertyRelative("position").vector3Value = new Vector3(-1f, 0f, 0f); p.FindPropertyRelative("rotationSteps").intValue = 0; },
            }));

        Comp<TouchCameraController>(camGo, ("placementManager", placement),
            ("panBounds", new Bounds(Vector3.zero, new Vector3(RoomW + 4f, 1f, RoomD + 4f))), ("minZoom", 4f), ("maxZoom", 12f));

        // Kilitli bölgeler (sağ ve sol dış alanlar)
        for (int a = 0; a < v.Areas.Length; a++)
            BuildExpansionArea(env, v, a);

        // UI
        BuildUI(placement);
        new GameObject("VenueArrivalBanner").AddComponent<VenueArrivalBanner>();

        EditorSceneManager.SaveScene(scene, ScenePath(index));
    }

    private static void AddUrpCameraData(GameObject camGo)
    {
        Type t = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (t != null && camGo.GetComponent(t) == null) camGo.AddComponent(t);
    }

    private static void BuildRoom(Transform env, VenueDef v)
    {
        Transform floor = new GameObject("Floor").transform;
        floor.SetParent(env);
        for (float x = -RoomW / 2 + 0.5f; x < RoomW / 2; x += 1f)
            for (float z = -RoomD / 2 + 0.5f; z < RoomD / 2; z += 1f)
                Model(v.Floor, floor, new Vector3(x, 0f, z));

        if (!v.Walls)
        {
            if (v.Id == "rooftop")
                for (float x = -RoomW / 2 + 0.5f; x < RoomW / 2; x += 1f) Model("rooftop_railing", env, new Vector3(x, 0f, -RoomD / 2 - 0.1f));
            return;
        }
        Transform walls = new GameObject("Walls").transform;
        walls.SetParent(env);
        for (float x = -RoomW / 2 + 0.5f; x < RoomW / 2; x += 1f)
        {
            string piece = v.GlassWalls ? "glass_wall" : (Mathf.Abs(x) > 2f && Mathf.Abs(x) < 4f ? "wall_window" : "wall_segment");
            Model(piece, walls, new Vector3(x, 0f, -RoomD / 2 - 0.06f));
        }
        for (float z = -RoomD / 2 + 0.5f; z < RoomD / 2; z += 1f)
            Model(v.GlassWalls ? "glass_wall" : "wall_segment", walls, new Vector3(-RoomW / 2 - 0.06f, 0f, z), 90f);
        Model("neon_open_sign", walls, new Vector3(2.5f, 1.5f, -RoomD / 2 + 0.05f));
        Model("wall_clock", walls, new Vector3(-2.5f, 1.8f, -RoomD / 2 + 0.05f));
    }

    private static void BuildCounter(Transform env, VenueDef v, out Transform counterRoot)
    {
        counterRoot = new GameObject("Counter").transform;
        counterRoot.SetParent(env);
        if (v.Id == "rooftop") Model("neon_bar", counterRoot, new Vector3(2.2f, 0f, -4.4f));
        else
        {
            Model("counter", counterRoot, new Vector3(1.3f, 0f, -4.4f));
            Model("counter", counterRoot, new Vector3(3.3f, 0f, -4.4f));
            Model("cash_register", counterRoot, new Vector3(1.8f, 0.98f, -4.3f));
            Model("pastry_display", counterRoot, new Vector3(3.3f, 0.98f, -4.35f));
        }
        Model("tip_jar", counterRoot, new Vector3(2.6f, v.Id == "rooftop" ? 1.05f : 0.98f, -4.15f));
        // Başlangıç istasyonu: espresso (menü bunun varlığına bakar)
        var st = (GameObject)PrefabUtility.InstantiatePrefab(P("Station_Espresso"));
        st.transform.SetParent(counterRoot);
        st.transform.position = new Vector3(-1.3f, 0f, -4.4f);
    }

    private static void BuildExpansionArea(Transform env, VenueDef v, int a)
    {
        string id = v.Areas[a].id, key = v.Areas[a].key;
        long cost = v.Areas[a].cost;
        int lvl = v.Areas[a].lvl;
        // Bölge 0: sağ dış alan; bölge 1: ön dış alan (kamera bu iki açık kenarı görür)
        var root = new GameObject("Area_" + id);
        root.transform.SetParent(env);
        root.transform.position = a == 0 ? new Vector3(RoomW / 2 + 2f, 0f, 1f) : new Vector3(-1f, 0f, RoomD / 2 + 2f);
        root.transform.rotation = Quaternion.Euler(0f, a == 0 ? 0f : -90f, 0f);

        // Açılınca: zemin + 2 masa
        var unlocked = new GameObject("Unlocked");
        unlocked.transform.SetParent(root.transform, false);
        for (float dx = -1.5f; dx <= 1.5f; dx += 1f)
            for (float dz = -3.5f; dz <= 3.5f; dz += 1f)
                Model(v.Floor == "sand_tile" ? "deck_tile" : v.Floor, unlocked.transform, new Vector3(dx, 0f, dz));
        foreach (float dz in new[] { -1.8f, 1.8f })
        {
            var t = (GameObject)PrefabUtility.InstantiatePrefab(P(a == 0 ? "Table_Round" : "Table_Square"));
            t.transform.SetParent(unlocked.transform, false);
            t.transform.localPosition = new Vector3(0.3f, 0f, dz);
        }
        Model(v.Decor[0], unlocked.transform, new Vector3(-1.2f, 0f, 3f));

        // Kilitliyken: çit + tabela (dokunulabilir)
        var locked = new GameObject("Locked");
        locked.transform.SetParent(root.transform, false);
        for (float dz = -3.5f; dz <= 3.5f; dz += 1f)
            Model("wood_fence", locked.transform, new Vector3(-1.8f, 0f, dz), 90f);
        GameObject sign = Model("sale_sign", locked.transform, new Vector3(-1.2f, 0f, 0f), 45f);
        var signCol = sign.AddComponent<BoxCollider>();
        signCol.center = new Vector3(0f, 0.8f, 0f);
        signCol.size = new Vector3(1.2f, 1.6f, 0.6f);

        // Fiyat etiketi (tabela üstünde dünya yazısı)
        var label = new GameObject("Price").AddComponent<TextMeshPro>();
        label.transform.SetParent(locked.transform, false);
        label.transform.localPosition = new Vector3(-1.2f, 1.75f, 0f);
        label.transform.rotation = Quaternion.Euler(50f, 225f, 0f);
        if (_font != null) label.font = _font;
        label.fontSize = 4f;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(4f, 1f);
        var lvlLabel = UnityEngine.Object.Instantiate(label, locked.transform);
        lvlLabel.name = "Level";
        lvlLabel.transform.localPosition += new Vector3(0f, 0.55f, 0f);
        lvlLabel.fontSize = 3f;

        var area = Comp<ExpansionArea>(sign, ("areaId", id), ("nameKey", key), ("cost", cost), ("requiredLevel", lvl),
            ("lockedRoot", locked), ("unlockedRoot", unlocked), ("priceLabel", label), ("levelLabel", lvlLabel));
        // ExpansionArea kendi GameObject'i kilitliyken açık kalmalı: tabela locked altında, açılınca kapanır (sorun değil, artık gerek yok)
        unlocked.SetActive(false);
    }

    // ------------------------------------------------------------------ UI

    private static readonly Color Panel = new Color(0.11f, 0.10f, 0.10f, 0.96f);
    private static readonly Color Accent = new Color(0.95f, 0.62f, 0.25f);
    private static readonly Color TextCol = new Color(0.96f, 0.92f, 0.86f);

    private static void BuildUI(BuildingPlacementManager placement)
    {
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform safe = Rect("SafeArea", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safe.gameObject.AddComponent<SafeAreaFitter>();

        // ---------- Üst bar
        RectTransform top = Rect("TopBar", safe, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -170), new Vector2(0, 0));
        Img(top.gameObject, new Color(0, 0, 0, 0.45f));
        RectTransform moneyBox = Rect("Money", top, new Vector2(0.25f, 0), new Vector2(0.62f, 1), new Vector2(10, 20), new Vector2(-10, -20));
        Img(moneyBox.gameObject, new Color(0.18f, 0.16f, 0.15f, 0.95f));
        TMP_Text money = Text("Label", moneyBox, "$0", 64, TextAlignmentOptions.Center, Accent);
        Comp<CurrencyView>(moneyBox.gameObject, ("label", money), ("prefix", "$"));
        TMP_Text income = Text("Income", top, "$0/s", 30, TextAlignmentOptions.Center, TextCol);
        Place(income.rectTransform, new Vector2(0.25f, 0), new Vector2(0.62f, 0), new Vector2(0, -26), new Vector2(0, 14));
        Comp<IncomeRateView>(income.gameObject, ("label", income));

        RectTransform lvl = Rect("Level", top, new Vector2(0, 0), new Vector2(0.24f, 1), new Vector2(16, 20), new Vector2(0, -20));
        Img(lvl.gameObject, new Color(0.18f, 0.16f, 0.15f, 0.95f));
        RectTransform fillR = Rect("Fill", lvl, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 14));
        Image fill = Img(fillR.gameObject, Accent);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.sprite = WhiteSprite();
        TMP_Text lvlText = Text("Value", lvl, "1", 60, TextAlignmentOptions.Center, TextCol);
        Comp<LevelView>(lvl.gameObject, ("levelLabel", lvlText), ("fill", fill));

        RectTransform stars = Rect("Stars", top, new Vector2(0.63f, 0), new Vector2(0.82f, 1), new Vector2(6, 20), new Vector2(-6, -20));
        Img(stars.gameObject, new Color(0.18f, 0.16f, 0.15f, 0.95f));
        TMP_Text starsT = Text("Label", stars, "0", 44, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.3f));
        Comp<CurrencyView>(stars.gameObject, ("label", starsT), ("kind", 2), ("prefix", "* "));

        // ---------- Paneller
        UIPanel upgrades = ListPanel<UpgradePanel>(safe, "UpgradePanel", "ui.tab.upgrades");
        UIPanel menu = ListPanel<MenuPanel>(safe, "MenuPanel", "ui.tab.menu");
        UIPanel staff = ListPanel<StaffPanel>(safe, "StaffPanel", "ui.tab.staff");
        UIPanel build = ListPanel<BuildShopPanel>(safe, "BuildPanel", "ui.tab.build",
            p => { Set(p, "catalog", _catalog); Set(p, "placementManager", placement); });
        UIPanel map = ListPanel<VenueMapPanel>(safe, "MapPanel", "ui.tab.map", p => BuildMapExtras((VenueMapPanel)p));
        UIPanel settings = SettingsPanelUI(safe);
        UIPanel stats = StatsPanelUI(safe);
        UIPanel daily = DailyPopupUI(safe);
        UIPanel offline = OfflinePopupUI(safe);

        // ---------- Sağ üst butonlar
        Button gear = Btn("Settings", top, "", new Vector2(0.83f, 0.5f), new Vector2(0.91f, 1), 22);
        SetLabelKey(gear, "ui.tab.settings", 22);
        UnityEventTools.AddVoidPersistentListener(gear.onClick, settings.Toggle);
        Button statsBtn = Btn("StatsBtn", top, "", new Vector2(0.915f, 0.5f), new Vector2(0.995f, 1), 22);
        SetLabelKey(statsBtn, "ui.tab.stats", 22);
        UnityEventTools.AddVoidPersistentListener(statsBtn.onClick, stats.Toggle);
        Button dailyBtn = Btn("DailyBtn", top, "", new Vector2(0.83f, 0f), new Vector2(0.995f, 0.48f), 26);
        SetLabelKey(dailyBtn, "ui.daily.title", 26);
        UnityEventTools.AddVoidPersistentListener(dailyBtn.onClick, daily.Toggle);
        GameObject badge = Img(Rect("Badge", dailyBtn.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-22, -22), new Vector2(0, 0)).gameObject, new Color(0.9f, 0.25f, 0.25f)).gameObject;
        Set(daily, "hudBadge", badge);

        // ---------- Görev kartı + boost
        BuildQuestCard(safe);
        BuildBoost(safe);

        // ---------- Alt sekme çubuğu
        RectTransform bottom = Rect("BottomBar", safe, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 170));
        Img(bottom.gameObject, new Color(0.08f, 0.07f, 0.07f, 0.95f));
        var tabs = new (string key, UIPanel panel)[] { ("ui.tab.build", build), ("ui.tab.upgrades", upgrades), ("ui.tab.menu", menu), ("ui.tab.staff", staff), ("ui.tab.map", map) };
        for (int i = 0; i < tabs.Length; i++)
        {
            Button b = Btn("Tab_" + i, bottom, "", new Vector2(i / 5f, 0), new Vector2((i + 1) / 5f, 1), 30);
            SetLabelKey(b, tabs[i].key, 30);
            UnityEventTools.AddVoidPersistentListener(b.onClick, tabs[i].panel.Toggle);
        }

        // ---------- Yerleştirme çubuğu
        RectTransform pb = Rect("PlacementBar", safe, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 190), new Vector2(-40, 330));
        Img(pb.gameObject, Panel);
        var barComp = Comp<PlacementBar>(canvasGo, ("placementManager", placement), ("bar", pb.gameObject));
        Button ok = Btn("Confirm", pb, "", new Vector2(0.67f, 0.1f), new Vector2(0.98f, 0.9f), 44);
        Button rot = Btn("Rotate", pb, "", new Vector2(0.345f, 0.1f), new Vector2(0.655f, 0.9f), 44);
        Button cancel = Btn("Cancel", pb, "", new Vector2(0.02f, 0.1f), new Vector2(0.33f, 0.9f), 44);
        SetLabelKey(ok, "ui.place.confirm", 44);
        SetLabelKey(rot, "ui.place.rotate", 44);
        SetLabelKey(cancel, "ui.place.cancel", 44);
        UnityEventTools.AddVoidPersistentListener(ok.onClick, barComp.Confirm);
        UnityEventTools.AddVoidPersistentListener(rot.onClick, barComp.Rotate);
        UnityEventTools.AddVoidPersistentListener(cancel.onClick, barComp.Cancel);
        pb.gameObject.SetActive(false);

        // ---------- Bildirim
        RectTransform toast = Rect("Toast", safe, new Vector2(0.1f, 1), new Vector2(0.9f, 1), new Vector2(0, -420), new Vector2(0, -200));
        Img(toast.gameObject, new Color(0.15f, 0.12f, 0.1f, 0.92f));
        TMP_Text tt = Text("Title", toast, "", 44, TextAlignmentOptions.Center, Accent);
        Place(tt.rectTransform, new Vector2(0, 0.5f), Vector2.one, Vector2.zero, Vector2.zero);
        TMP_Text tv = Text("Value", toast, "", 52, TextAlignmentOptions.Center, TextCol);
        Place(tv.rectTransform, Vector2.zero, new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);
        toast.gameObject.AddComponent<CanvasGroup>();
        Comp<Toast>(toast.gameObject, ("title", tt), ("value", tv));
        canvasGo.AddComponent<UnlockNotifier>();

        // Popup'lar en üstte çizilsin
        daily.transform.SetAsLastSibling();
        offline.transform.SetAsLastSibling();
        toast.SetAsLastSibling();
    }

    private static void BuildMapExtras(VenueMapPanel panel)
    {
        Transform window = panel.transform.Find("Window");
        TMP_Text cur = Text("Current", window, "", 40, TextAlignmentOptions.Center, Accent);
        Place(cur.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(30, 190), new Vector2(-30, 240));
        TMP_Text pend = Text("PendingStars", window, "+0", 40, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.3f));
        Place(pend.rectTransform, new Vector2(0, 0), new Vector2(0.4f, 0), new Vector2(30, 30), new Vector2(0, 170));
        Button move = Btn("Move", window, "", new Vector2(0.42f, 0), new Vector2(0.97f, 0), 36);
        Place((RectTransform)move.transform, new Vector2(0.42f, 0), new Vector2(0.97f, 0), new Vector2(0, 30), new Vector2(0, 170));
        SetLabelKey(move, "ui.venue.move", 34);
        RectTransform confirm = Rect("Confirm", window, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(confirm.gameObject, new Color(0, 0, 0, 0.85f));
        Button yes = Btn("Yes", confirm, "", new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.55f), 40);
        SetLabelKey(yes, "ui.prestige.confirm", 40);
        confirm.gameObject.SetActive(false);
        Set(panel, "currentVenueLabel", cur);
        Set(panel, "pendingStarsLabel", pend);
        Set(panel, "moveButton", move);
        Set(panel, "confirmGroup", confirm.gameObject);
        Set(panel, "confirmButton", yes);
    }

    private static void BuildQuestCard(RectTransform safe)
    {
        RectTransform q = Rect("Quest", safe, new Vector2(0, 1), new Vector2(0.62f, 1), new Vector2(16, -400), new Vector2(0, -190));
        Img(q.gameObject, new Color(0.12f, 0.11f, 0.1f, 0.88f));
        TMP_Text desc = Text("Desc", q, "", 32, TextAlignmentOptions.TopLeft, TextCol);
        Place(desc.rectTransform, new Vector2(0, 0.45f), Vector2.one, new Vector2(18, 0), new Vector2(-18, -12));
        RectTransform barBg = Rect("Bar", q, new Vector2(0, 0), new Vector2(0.65f, 0), new Vector2(18, 20), new Vector2(0, 60));
        Img(barBg.gameObject, new Color(0.25f, 0.23f, 0.22f));
        Image fill = Img(Rect("Fill", barBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject, Accent);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.sprite = WhiteSprite();
        TMP_Text prog = Text("Progress", barBg, "0 / 5", 26, TextAlignmentOptions.Center, TextCol);
        TMP_Text reward = Text("Reward", q, "", 28, TextAlignmentOptions.Left, new Color(1f, 0.85f, 0.3f));
        Place(reward.rectTransform, new Vector2(0, 0.25f), new Vector2(0.65f, 0.45f), new Vector2(18, 0), Vector2.zero);
        Button claim = Btn("Claim", q, "", new Vector2(0.68f, 0.05f), new Vector2(0.97f, 0.45f), 32);
        SetLabelKey(claim, "ui.quest.claim", 32);
        GameObject glow = Img(Rect("Glow", claim.transform, Vector2.zero, Vector2.one, new Vector2(-6, -6), new Vector2(6, 6)).gameObject, new Color(1f, 0.8f, 0.3f, 0.35f)).gameObject;
        glow.transform.SetAsFirstSibling();
        Comp<QuestView>(q.gameObject, ("root", q.gameObject), ("description", desc), ("progressLabel", prog), ("rewardLabel", reward),
            ("progressFill", fill), ("claimButton", claim), ("claimGlow", glow));
    }

    private static void BuildBoost(RectTransform safe)
    {
        RectTransform holder = Rect("BoostHolder", safe, new Vector2(0.64f, 1), new Vector2(0.98f, 1), new Vector2(0, -290), new Vector2(0, -190));
        RectTransform b = Rect("Boost", holder, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(b.gameObject, new Color(0.55f, 0.2f, 0.65f, 0.9f));
        TMP_Text t = Text("Timer", b, "x2 0:00", 36, TextAlignmentOptions.Center, Color.white);
        Comp<BoostView>(holder.gameObject, ("root", b.gameObject), ("timerLabel", t));
        b.gameObject.SetActive(false);
    }

    private static UIPanel ListPanel<T>(RectTransform safe, string name, string titleKey, Action<UIPanel> extra = null) where T : ListPanelBase
    {
        RectTransform root = PanelShell(safe, name, titleKey, out RectTransform window, out Button close);
        T panel = root.gameObject.AddComponent<T>();
        Set(panel, "window", window);
        UnityEventTools.AddVoidPersistentListener(close.onClick, panel.Close);

        // Kaydırılabilir liste
        RectTransform viewport = Rect("Viewport", window, Vector2.zero, Vector2.one, new Vector2(24, extra != null && typeof(T) == typeof(VenueMapPanel) ? 260 : 24), new Vector2(-24, -130));
        Img(viewport.gameObject, new Color(0, 0, 0, 0.01f));
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = Rect("Content", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(0.5f, 1);
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14; vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content = content; scroll.horizontal = false; scroll.viewport = viewport; scroll.movementType = ScrollRect.MovementType.Clamped;

        Set(panel, "rowPrefab", P("UI_Row").GetComponent<ShopRowView>());
        Set(panel, "content", content);
        extra?.Invoke(panel);
        return panel;
    }

    private static RectTransform PanelShell(RectTransform safe, string name, string titleKey, out RectTransform window, out Button close)
    {
        RectTransform root = Rect(name, safe, Vector2.zero, Vector2.one, new Vector2(0, 170), new Vector2(0, -170));
        root.gameObject.AddComponent<CanvasGroup>();
        Img(root.gameObject, new Color(0, 0, 0, 0.5f));
        window = Rect("Window", root, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero);
        Img(window.gameObject, Panel);
        TMP_Text title = Text("Title", window, titleKey, 56, TextAlignmentOptions.Center, Accent);
        Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -120), new Vector2(-120, -10));
        title.gameObject.AddComponent<LocalizedText>().Also(lt => Set(lt, "key", titleKey));
        close = Btn("Close", window, "X", new Vector2(1, 1), new Vector2(1, 1), 50);
        Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-115, -115), new Vector2(-15, -15));
        return root;
    }

    private static UIPanel SimplePanel<T>(RectTransform safe, string name, string titleKey, out RectTransform window) where T : UIPanel
    {
        RectTransform root = PanelShell(safe, name, titleKey, out window, out Button close);
        T panel = root.gameObject.AddComponent<T>();
        Set(panel, "window", window);
        UnityEventTools.AddVoidPersistentListener(close.onClick, panel.Close);
        return panel;
    }

    private static UIPanel SettingsPanelUI(RectTransform safe)
    {
        UIPanel p = SimplePanel<SettingsPanel>(safe, "SettingsPanel", "ui.tab.settings", out RectTransform w);
        Toggle sfx = ToggleRow(w, "ui.settings.sfx", 0);
        Toggle music = ToggleRow(w, "ui.settings.music", 1);
        Toggle hap = ToggleRow(w, "ui.settings.haptics", 2);
        Button tr = Btn("TR", w, "Türkçe", new Vector2(0.08f, 0.45f), new Vector2(0.48f, 0.53f), 40);
        Button en = Btn("EN", w, "English", new Vector2(0.52f, 0.45f), new Vector2(0.92f, 0.53f), 40);
        Set(p, "sfxToggle", sfx); Set(p, "musicToggle", music); Set(p, "hapticsToggle", hap);
        Set(p, "turkishButton", tr); Set(p, "englishButton", en);
        return p;
    }

    private static Toggle ToggleRow(RectTransform w, string key, int i)
    {
        float y1 = 0.85f - i * 0.1f;
        RectTransform row = Rect("Toggle_" + key, w, new Vector2(0.08f, y1 - 0.08f), new Vector2(0.92f, y1), Vector2.zero, Vector2.zero);
        TMP_Text label = Text("Label", row, key, 40, TextAlignmentOptions.Left, TextCol);
        Place(label.rectTransform, Vector2.zero, new Vector2(0.75f, 1), Vector2.zero, Vector2.zero);
        label.gameObject.AddComponent<LocalizedText>().Also(lt => Set(lt, "key", key));
        RectTransform box = Rect("Box", row, new Vector2(0.8f, 0.15f), new Vector2(1f, 0.85f), Vector2.zero, Vector2.zero);
        Image bg = Img(box.gameObject, new Color(0.25f, 0.23f, 0.22f));
        Image check = Img(Rect("Check", box, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8)).gameObject, Accent);
        var t = row.gameObject.AddComponent<Toggle>();
        t.targetGraphic = bg; t.graphic = check; t.isOn = true;
        return t;
    }

    private static UIPanel StatsPanelUI(RectTransform safe)
    {
        UIPanel p = SimplePanel<StatsPanel>(safe, "StatsPanel", "ui.tab.stats", out RectTransform w);
        string[] keys = { "ui.stats.lifetime", "ui.stats.lifetime", "ui.stats.served", "ui.stats.lost", "ui.stats.tips", "ui.prestige.title" };
        string[] fields = { "lifetimeEarnings", "runEarnings", "served", "lost", "tips", "prestigeCount" };
        for (int i = 0; i < fields.Length; i++)
        {
            float y1 = 0.86f - i * 0.1f;
            TMP_Text k = Text("K" + i, w, keys[i], 36, TextAlignmentOptions.Left, TextCol);
            Place(k.rectTransform, new Vector2(0.06f, y1 - 0.08f), new Vector2(0.62f, y1), Vector2.zero, Vector2.zero);
            k.gameObject.AddComponent<LocalizedText>().Also(lt => Set(lt, "key", keys[i]));
            TMP_Text v = Text("V" + i, w, "0", 40, TextAlignmentOptions.Right, Accent);
            Place(v.rectTransform, new Vector2(0.62f, y1 - 0.08f), new Vector2(0.94f, y1), Vector2.zero, Vector2.zero);
            Set(p, fields[i], v);
        }
        return p;
    }

    private static UIPanel DailyPopupUI(RectTransform safe)
    {
        UIPanel p = SimplePanel<DailyRewardPopup>(safe, "DailyPopup", "ui.daily.title", out RectTransform w);
        TMP_Text day = Text("Day", w, "1", 90, TextAlignmentOptions.Center, TextCol);
        Place(day.rectTransform, new Vector2(0, 0.6f), new Vector2(1, 0.75f), Vector2.zero, Vector2.zero);
        TMP_Text amount = Text("Amount", w, "$100", 110, TextAlignmentOptions.Center, Accent);
        Place(amount.rectTransform, new Vector2(0, 0.42f), new Vector2(1, 0.6f), Vector2.zero, Vector2.zero);
        var markers = new GameObject[7];
        for (int i = 0; i < 7; i++)
        {
            RectTransform m = Rect("Day" + (i + 1), w, new Vector2(0.06f + i * 0.127f, 0.3f), new Vector2(0.06f + i * 0.127f + 0.11f, 0.37f), Vector2.zero, Vector2.zero);
            markers[i] = Img(m.gameObject, Accent).gameObject;
        }
        Button claim = Btn("Claim", w, "", new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.2f), 48);
        SetLabelKey(claim, "ui.offline.claim", 48);
        Set(p, "dayLabel", day); Set(p, "amountLabel", amount); Set(p, "claimButton", claim);
        Set(p, "dayMarkers", markers);
        return p;
    }

    private static UIPanel OfflinePopupUI(RectTransform safe)
    {
        UIPanel p = SimplePanel<OfflineEarningsPopup>(safe, "OfflinePopup", "ui.offline.title", out RectTransform w);
        TMP_Text away = Text("Away", w, "0:00", 60, TextAlignmentOptions.Center, TextCol);
        Place(away.rectTransform, new Vector2(0, 0.6f), new Vector2(1, 0.72f), Vector2.zero, Vector2.zero);
        TMP_Text amount = Text("Amount", w, "$0", 120, TextAlignmentOptions.Center, Accent);
        Place(amount.rectTransform, new Vector2(0, 0.4f), new Vector2(1, 0.6f), Vector2.zero, Vector2.zero);
        Button claim = Btn("Claim", w, "", new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.24f), 48);
        SetLabelKey(claim, "ui.offline.claim", 48);
        Set(p, "amountLabel", amount); Set(p, "awayLabel", away); Set(p, "claimButton", claim);
        return p;
    }

    private static GameObject BuildRowPrefab()
    {
        var go = new GameObject("UI_Row", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(900, 170);
        go.AddComponent<LayoutElement>().preferredHeight = 170;
        Img(go, new Color(0.18f, 0.16f, 0.15f));
        Image icon = Img(Rect("Icon", rt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(15, 15), new Vector2(155, -15)).gameObject, new Color(0.3f, 0.27f, 0.25f));
        TMP_Text name = Text("Name", rt, "Item", 40, TextAlignmentOptions.BottomLeft, TextCol);
        Place(name.rectTransform, new Vector2(0, 0.5f), new Vector2(0.66f, 1), new Vector2(175, 0), new Vector2(0, -12));
        TMP_Text info = Text("Info", rt, "", 30, TextAlignmentOptions.TopLeft, new Color(0.75f, 0.72f, 0.68f));
        Place(info.rectTransform, new Vector2(0, 0), new Vector2(0.66f, 0.5f), new Vector2(175, 12), Vector2.zero);
        Button buy = Btn("Buy", rt, "$0", new Vector2(0.68f, 0.15f), new Vector2(0.98f, 0.85f), 40);
        TMP_Text cost = buy.GetComponentInChildren<TMP_Text>();
        RectTransform lockR = Rect("Locked", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(lockR.gameObject, new Color(0, 0, 0, 0.65f));
        TMP_Text lockT = Text("Label", lockR, "Lv 1", 44, TextAlignmentOptions.Center, TextCol);
        lockR.gameObject.SetActive(false);
        Comp<ShopRowView>(go, ("icon", icon), ("nameLabel", name), ("infoLabel", info), ("costLabel", cost), ("button", buy),
            ("lockedOverlay", lockR.gameObject), ("lockedLabel", lockT), ("buttonImage", buy.GetComponent<Image>()));
        return go;
    }

    // ---------- UI yardımcıları

    private static Sprite _white;
    private static Sprite WhiteSprite()
    {
        if (_white != null) return _white;
        _white = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        return _white;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Place(rt, aMin, aMax, oMin, oMax);
        return rt;
    }

    private static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
    }

    private static Image Img(GameObject go, Color c)
    {
        Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = c;
        img.sprite = WhiteSprite();
        img.type = Image.Type.Sliced;
        return img;
    }

    private static TMP_Text Text(string name, Transform parent, string text, float size, TextAlignmentOptions align, Color color)
    {
        RectTransform rt = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.enableWordWrapping = true;
        t.raycastTarget = false;
        return t;
    }

    private static Button Btn(string name, Transform parent, string label, Vector2 aMin, Vector2 aMax, float size)
    {
        RectTransform rt = Rect(name, parent, aMin, aMax, Vector2.zero, Vector2.zero);
        Image bg = Img(rt.gameObject, Accent);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = bg;
        Text("Label", rt, label, size, TextAlignmentOptions.Center, new Color(0.1f, 0.08f, 0.07f));
        return b;
    }

    private static void SetLabelKey(Button b, string key, float size)
    {
        TMP_Text t = b.GetComponentInChildren<TMP_Text>();
        t.text = key;
        t.fontSize = size;
        t.gameObject.AddComponent<LocalizedText>().Also(lt => Set(lt, "key", key));
    }

    // ------------------------------------------------------------------ Build Settings

    private static void RegisterScenes()
    {
        var list = new List<EditorBuildSettingsScene>();
        for (int i = 0; i < Venues.Length; i++) list.Add(new EditorBuildSettingsScene(ScenePath(i), true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}

internal static class SetupExtensions
{
    public static GameObject Also(this GameObject go, Transform parent)
    {
        go.transform.SetParent(parent, false);
        return go;
    }

    public static T Also<T>(this T obj, Action<T> act)
    {
        act(obj);
        return obj;
    }
}
#endif
