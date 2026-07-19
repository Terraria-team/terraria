using UnityEngine;
using UnityEditor;
using Shared.DataDefinitions;
using Server.AI.Bosses;
using Server.AI;
using Mirror;

namespace EditorScripts
{
    public class BossSetupHelper
    {
        [MenuItem("Tools/Setup Bosses")]
        public static void SetupBosses()
        {
            CreateBoss("King Slime", "N1_Slime", 3f, 2000, 40, typeof(KingSlimeController));
            CreateBoss("Eye of Cthulhu", "L1_DemonEye", 2f, 2800, 25, typeof(EyeOfCthulhuController));
            CreateBoss("Queen Bee", "L2_MossHornet", 2.5f, 3400, 30, typeof(QueenBeeController));
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Bosses setup complete!");
        }

        private static void CreateBoss(string bossName, string baseMobName, float scale, int hp, float damage, System.Type controllerType)
        {
            string baseDataPath = $"Assets/Data/Enemies/{baseMobName}.asset";
            string bossDataPath = $"Assets/Data/Enemies/Boss_{bossName.Replace(" ", "")}.asset";

            EnemyData baseData = AssetDatabase.LoadAssetAtPath<EnemyData>(baseDataPath);
            if (baseData == null)
            {
                Debug.LogError($"Base data not found: {baseDataPath}");
                return;
            }

            EnemyData bossData = AssetDatabase.LoadAssetAtPath<EnemyData>(bossDataPath);
            if (bossData == null)
            {
                bossData = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(bossData, bossDataPath);
            }

            EditorUtility.CopySerialized(baseData, bossData);
            bossData.enemyName = bossName;
            bossData.attackDamage = damage;
            
            if (bossName == "Queen Bee")
            {
                bossData.behaviorType = EnemyBehaviorType.FlyerShooter;
            }
            EditorUtility.SetDirty(bossData);

            string basePrefabPath = $"Assets/Prefabs/Enemies/{baseMobName}.prefab";
            string bossPrefabPath = $"Assets/Prefabs/Enemies/Boss_{bossName.Replace(" ", "")}.prefab";

            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
            if (basePrefab == null)
            {
                Debug.LogError($"Base prefab not found: {basePrefabPath}");
                return;
            }

            GameObject bossInstance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            bossInstance.name = bossName;
            bossInstance.transform.localScale = Vector3.one * scale;

            var health = bossInstance.GetComponent<Shared.Components.HealthComponent>();
            if (health == null)
            {
                health = bossInstance.AddComponent<Shared.Components.HealthComponent>();
            }
            if (health != null)
            {
                SerializedObject serializedHealth = new SerializedObject(health);
                serializedHealth.FindProperty("MaxHealth").intValue = hp;
                serializedHealth.ApplyModifiedProperties();
            }

            var oldController = bossInstance.GetComponent<ServerEnemyController>();
            if (oldController != null)
            {
                GameObject projPrefab = oldController.ProjectilePrefab;
                
                SerializedObject oldSo = new SerializedObject(oldController);
                int gLayer = oldSo.FindProperty("_groundLayer").intValue;
                int pLayer = oldSo.FindProperty("_playerLayer").intValue;
                int bLayer = oldSo.FindProperty("_blockingLayer").intValue;

                Object.DestroyImmediate(oldController, true);
                
                var newController = bossInstance.AddComponent(controllerType) as ServerEnemyController;
                newController.Data = bossData;
                if (projPrefab != null)
                {
                    newController.SetProjectilePrefab(projPrefab);
                }

                SerializedObject newSo = new SerializedObject(newController);
                newSo.FindProperty("_groundLayer").intValue = gLayer;
                newSo.FindProperty("_playerLayer").intValue = pLayer;
                newSo.FindProperty("_blockingLayer").intValue = bLayer;
                newSo.ApplyModifiedProperties();

                if (controllerType == typeof(KingSlimeController))
                {
                    SerializedObject so = new SerializedObject(newController);
                    so.FindProperty("_slimeMinionPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/N1_Slime.prefab");
                    so.ApplyModifiedProperties();
                }
                else if (controllerType == typeof(EyeOfCthulhuController))
                {
                    SerializedObject so = new SerializedObject(newController);
                    so.FindProperty("_servantPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/L1_DemonEye.prefab");
                    so.ApplyModifiedProperties();
                }
                else if (controllerType == typeof(QueenBeeController))
                {
                    SerializedObject so = new SerializedObject(newController);
                    so.FindProperty("_hornetPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/L2_MossHornet.prefab");
                    so.ApplyModifiedProperties();
                }
            }

            PrefabUtility.SaveAsPrefabAsset(bossInstance, bossPrefabPath);
            Object.DestroyImmediate(bossInstance);
        }
    }
}
