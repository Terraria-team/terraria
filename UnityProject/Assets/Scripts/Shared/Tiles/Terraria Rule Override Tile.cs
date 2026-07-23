using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Editor
{
    public class RuleOverrideTileCreator : EditorWindow
    {
        private AdvancedRuleTile originalTile;
        private Texture2D newSpriteSheet;
        
        [MenuItem("Tools/Create Rule Override Tile")]
        public static void ShowWindow()
        {
            GetWindow<RuleOverrideTileCreator>("Rule Override Tile Generator");
        }

        void OnGUI()
        {
            GUILayout.Label("Generate Rule Override Tile", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            originalTile = (AdvancedRuleTile)EditorGUILayout.ObjectField("Original Rule Tile", originalTile, typeof(AdvancedRuleTile), false);
            newSpriteSheet = (Texture2D)EditorGUILayout.ObjectField("New Sprite Sheet", newSpriteSheet, typeof(Texture2D), false);

            EditorGUILayout.Space();

            GUI.enabled = originalTile != null && newSpriteSheet != null;
            if (GUILayout.Button("Create Override Tile", GUILayout.Height(30)))
            {
                GenerateOverrideTile();
            }
            GUI.enabled = true;
        }

        private void GenerateOverrideTile()
        {
            // 1. Load all sliced sprites from the target texture
            string sheetPath = AssetDatabase.GetAssetPath(newSpriteSheet);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            List<Sprite> targetSprites = assets.OfType<Sprite>().ToList();

            if (targetSprites.Count == 0)
            {
                Debug.LogError($"No sprites found in {newSpriteSheet.name}. Ensure the texture is set to 'Sprite (2D and UI)' and is sliced.");
                return;
            }

            // Ensure unique IDs on the original tile before copying rules
            ValidateAdvancedRuleTileIds(originalTile);

            // 2. Determine asset path and create the RuleOverrideTile / AdvancedRuleOverrideTile instance
            string originalPath = AssetDatabase.GetAssetPath(originalTile);
            string directory = System.IO.Path.GetDirectoryName(originalPath);
            string newAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{newSpriteSheet.name}_Clone.asset");
            
            AssetDatabase.CopyAsset(originalPath, newAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Завантажуємо скопійований тайл для редагування
            AdvancedRuleTile clonedTile = AssetDatabase.LoadAssetAtPath<AdvancedRuleTile>(newAssetPath);

            // 3. Замінюємо дефолтний спрайт
            if (clonedTile.m_DefaultSprite != null)
            {
                Sprite mappedDefault = MapSprite(clonedTile.m_DefaultSprite, targetSprites, newSpriteSheet.name);
                if (mappedDefault != null) clonedTile.m_DefaultSprite = mappedDefault;
            }

            // 4. Проходимося по правилах і просто міняємо спрайти всередині клону
            foreach (var rule in clonedTile.m_TilingRules)
            {
                if (rule.m_Sprites != null)
                {
                    for (int i = 0; i < rule.m_Sprites.Length; i++)
                    {
                        if (rule.m_Sprites[i] != null)
                        {
                            Sprite mappedSpr = MapSprite(rule.m_Sprites[i], targetSprites, newSpriteSheet.name);
                            if (mappedSpr != null) rule.m_Sprites[i] = mappedSpr;
                        }
                    }
                }
            }

            // 5. Зберігаємо зміни у клонованому ассеті
            EditorUtility.SetDirty(clonedTile);
            AssetDatabase.SaveAssets();

            EditorGUIUtility.PingObject(clonedTile);
            Debug.Log("Successfully created a standalone cloned Rule Tile!");
        }

        private string GetSpriteIndex(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            
            var match = Regex.Match(spriteName, @"_(\d+)\D*$");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Резервний варіант (Fallback)
            string[] parts = spriteName.Split('_');
            if (parts.Length >= 2)
            {
                string lastSegment = parts.Last().Trim();
                var numMatch = Regex.Match(lastSegment, @"^(\d+)");
                if (numMatch.Success)
                {
                    return numMatch.Groups[1].Value;
                }
            }

            return null;
        }

        private Sprite MapSprite(Sprite origSprite, List<Sprite> targetSprites, string targetSheetName)
        {
            if (origSprite == null || targetSprites == null || targetSprites.Count == 0) return null;

            string origName = origSprite.name;
            string origIndex = GetSpriteIndex(origName);

            // 1. If we extracted a numeric index (e.g. "0", "17") from "<original name>_<index>.*":
            if (!string.IsNullOrEmpty(origIndex))
            {
                // Priority 1: Exact expected target name format "<targetSheetName>_<origIndex>" across any optional extensions/suffixes
                string expectedPrefix = $"{targetSheetName}_{origIndex}";
                Sprite exactMatch = targetSprites.FirstOrDefault(s => 
                    s.name == expectedPrefix || 
                    s.name.StartsWith($"{expectedPrefix}.") || 
                    s.name.StartsWith($"{expectedPrefix} "));
                if (exactMatch != null) return exactMatch;

                // Priority 2: Match any sprite in targetSprites whose extracted index equals origIndex EXACTLY ("<target name>_<origIndex>.*")
                Sprite indexMatch = targetSprites.FirstOrDefault(s => GetSpriteIndex(s.name) == origIndex);
                if (indexMatch != null) return indexMatch;
            }

            // 2. Fallback to exact sprite name match across sheets
            Sprite nameMatch = targetSprites.FirstOrDefault(s => s.name == origName);
            if (nameMatch != null) return nameMatch;

            return null;
        }

        private void ValidateAdvancedRuleTileIds(AdvancedRuleTile advancedRuleTile)
        {
            if (advancedRuleTile == null || advancedRuleTile.m_TilingRules == null) return;
            
            HashSet<int> uniqueIds = new HashSet<int>();
            int startId = 0;
            bool dirty = false;
            foreach (var rule in advancedRuleTile.m_TilingRules)
            {
                if (uniqueIds.Contains(rule.m_Id))
                {
                    do
                    {
                        rule.m_Id = startId++;
                    } while (uniqueIds.Contains(rule.m_Id));
                    dirty = true;
                }
                uniqueIds.Add(rule.m_Id);
                startId++;
            }
            if (dirty)
            {
                EditorUtility.SetDirty(advancedRuleTile);
            }
        }
    }
}