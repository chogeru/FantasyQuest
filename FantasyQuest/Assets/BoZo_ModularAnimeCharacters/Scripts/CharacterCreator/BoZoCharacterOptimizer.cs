using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Rendering;
using System.Threading.Tasks;
using System;
using System.Collections;

namespace Bozo.AnimeCharacters
{
    public class BoZo_CharacterOptimizer
    {
        private static string path = "/BoZo_ModularAnimeCharacters/CustomCharacters/Prefabs";

        private MergedMaterialData[] mergedMaterialDatas;

        public async void OptimizeCharacter(OutfitSystem source, CharacterData data)
        {
            if (source.mergeMaterial == null)
            {
                Debug.Log("Merge Material required, please assign one in the inspector");
                return;
            }
            //creating clean base to work on
            var body = await PrepareMergeBase(source, data);
            var height = body.height;

            mergedMaterialDatas = source.materialData;

            var mergedBody = await Merge(body);
            source.customMaps = body.customMaps;

            UnityEngine.Object.Destroy(body.gameObject);
            BMAC_SaveSystem.LoadBodyMods(body, data);
            source.SetCharacterBody(mergedBody);

        }

        public async void SaveOptimizedCharacter(OutfitSystem source, CharacterData data)
        {
            //creating clean base to work on
            var body = await PrepareMergeBase(source, data);
            var height = body.height;
            body.data = data;
            mergedMaterialDatas = source.materialData;

            var mergedBody = await Merge(body, true, source.prefabName);

            UnityEngine.Object.Destroy(body.gameObject);
            BMAC_SaveSystem.LoadBodyMods(body, data);
            source.SetCharacterBody(mergedBody);
            source.MuteHeightChange(false);
        }

        private async Task<OutfitSystem> PrepareMergeBase(OutfitSystem source, CharacterData data)
        {
            var characterBaseOb = Resources.Load<OutfitSystem>("BMAC_MergedCharacterBase");
            var body = UnityEngine.Object.Instantiate(characterBaseOb);
            body.mergeBase = true;
            body.MuteHeightChange(true);
            body.animator.enabled = false;
            await BMAC_SaveSystem.LoadCharacter(body, data, true, true);

            body.mergeMaterial = source.mergeMaterial;

            return body;
        }


        public async Task<GameObject> Merge(OutfitSystem outfitSystem, bool saveAsPrefab = false, string saveName = "")
        {
            if (!Application.isPlaying) return null;

            var outfitsToMerge = outfitSystem.GetOutfits();
            var rig = outfitSystem.GetCharacterBody();
            var materialBase = outfitSystem.mergeMaterial;


            #region ----- Initalization -----

            if (outfitsToMerge == null || outfitsToMerge.Count == 0)
            {
                Debug.LogError("No Skinned Mesh Renderers assigned.");
                return null;
            }

            //Initalizing Bones
            var masterBones = outfitSystem.GetBones().Values.ToList();
            var rootBone = rig.rootBone;
            var parent = rig.transform.parent;

            //Creating Map
            Dictionary<string, boneData> boneMap = new Dictionary<string, boneData>();

            for (int i = 0; i < masterBones.Count; i++)
            {
                var data = new boneData();
                data.bone = masterBones[i];
                data.index = i;

                boneMap.Add(masterBones[i].name, data);
            }

            bool MergeMaterials = true;

            //Mesh Data Initaliztion
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector4> tangents = new List<Vector4>();
            List<Vector2> uv = new List<Vector2>();
            List<Vector2> uv2 = new List<Vector2>();
            List<Color> colors = new List<Color>();
            List<BoneWeight> boneWeights = new List<BoneWeight>();
            List<Material> materials = new List<Material>();
            List<Matrix4x4> bindposes = new List<Matrix4x4>();
            Dictionary<string, List<BlendshapeData>> blendshapeGroups = new Dictionary<string, List<BlendshapeData>>();
            int vertexOffset = 0;
            List<List<int>> submeshTriangles = new List<List<int>>();

            var mergedTexture = new Texture2D(2, 2);
            var atlasTransform = new Dictionary<string, Rect>();

            Material newMaterial = new Material(materialBase);
            newMaterial.mainTexture = null;
            List<Renderer> rendererList = new List<Renderer>();

            foreach (var item in outfitsToMerge)
            {
                rendererList.AddRange(item.GetComponentsInChildren<Renderer>());
                var anim = item.GetComponentInChildren<Animator>();
                if (anim != null) anim.enabled = false;
            }

            foreach (var item in rendererList)
            {
                item.enabled = (false);
            }



            var packedTextures = await CreateMergedTextures(outfitsToMerge);
            newMaterial.mainTexture = packedTextures.texture;

            foreach (var item in mergedMaterialDatas)
            {
                newMaterial.SetTexture(item.toMateiralProperty, packedTextures.additionalMaps[item.toMateiralProperty]);
            }
            atlasTransform = packedTextures.rect;
            outfitSystem.customMaps = packedTextures.customMaps;


            #endregion

            Dictionary<int, int> boneIndexMap = new Dictionary<int, int>();

            #region ---- Main Merge Loop -----
            for (int i = 0; i < rendererList.Count; i++)
            {
                var smr = rendererList[i].GetComponentInChildren<SkinnedMeshRenderer>(true);
                var outfit = rendererList[i].GetComponentInParent<Outfit>(true);

                //Not SkinnedMesh converting to SkinnedMesh
                if (smr == null)
                {
                    Mesh staticMesh = rendererList[i].GetComponentInChildren<MeshFilter>(true).sharedMesh;
                    var meshRenderer = rendererList[i].GetComponentInChildren<MeshRenderer>(true);
                    var staticMaterial = meshRenderer.sharedMaterial;
                    var meshGameObject = meshRenderer.gameObject;
                    UnityEngine.Object.DestroyImmediate(meshRenderer);
                    UnityEngine.Object.DestroyImmediate(meshGameObject.GetComponent<MeshFilter>());
                    smr = meshGameObject.AddComponent<SkinnedMeshRenderer>();
                    smr.sharedMaterial = staticMaterial;
                    smr.sharedMesh = staticMesh;
                }



                Mesh mesh = smr.sharedMesh;
                var boneIndexOffset = 0;
                bool hasSkeleton = true;



                //Check if its the same skeleton
                if (smr.rootBone != null)
                {
                    if (!boneMap.ContainsKey(smr.rootBone.name))
                    {
                        boneIndexOffset = masterBones.Count;

                        if (outfit.AttachPoint == "")
                        {
                            Debug.Log("What are you doing here? Stop the show...");
                            return null;
                        }


                        smr.rootBone = boneMap[outfit.AttachPoint].bone;
                        masterBones.AddRange(smr.bones);
                        smr.bones[0].SetParent(boneMap[outfit.AttachPoint].bone);

                        for (int b = 0; b < smr.bones.Length; b++)
                        {
                            var dupCounter = 1;

                            boneData data = new boneData();
                            data.bone = smr.bones[b];
                            data.index = b + boneIndexOffset;

                            try
                            {
                                boneMap.Add(smr.bones[b].name, data);
                            }
                            catch
                            {
                                Debug.LogWarning($"Duplicate bone naming in: <{outfit.name}> <{smr.bones[b].name}>Please give bone a unique name");
                                boneMap.Add(smr.bones[b].name + dupCounter, data);
                            }
                        }
                    }
                }



                // Collect transforms in this mesh
                Transform[] meshBones = smr.bones;

                if (smr.rootBone != null)
                {
                    Debug.Log(meshBones.Length);
                    // Build remap table: mesh bone index -> master bone index
                    for (int b = 0; b < meshBones.Length; b++)
                    {
                        Transform bone = meshBones[b];
                        int masterIndex = -1;
                        if (bone == null) continue;
                        if (boneMap.ContainsKey(bone.name))
                        {
                            masterIndex = boneMap[bone.name].index;
                        }
                        else
                        {
                            Debug.LogWarning(bone.name + " Is not in BoneMap");
                        }

                        if (masterIndex == -1)
                        {
                            continue;
                        }
                        boneIndexMap[b] = masterIndex;
                    }
                }
                else
                {
                    hasSkeleton = false;
                    int masterIndex = -1;
                    masterIndex = boneMap[outfit.AttachPoint].index;
                    boneIndexMap[0] = masterIndex;
                    smr.bones = masterBones.ToArray();
                    smr.rootBone = rootBone;
                }




                #region meshVertices

                // Bake transform into vertices/normals/tangents
                Matrix4x4 localToWorld = smr.transform.localToWorldMatrix;

                // Transform vertices
                Vector3[] transformedVertices = new Vector3[mesh.vertexCount];
                Vector3[] transformedNormals = new Vector3[mesh.vertexCount];
                Vector4[] transformedTangents = new Vector4[mesh.vertexCount];

                Vector3[] meshVertices = mesh.vertices;
                Vector3[] meshNormals = mesh.normals;
                Vector4[] meshTangents = mesh.tangents;


                for (int v = 0; v < mesh.vertexCount; v++)
                {
                    transformedVertices[v] = localToWorld.MultiplyPoint3x4(meshVertices[v]);
                    transformedNormals[v] = localToWorld.MultiplyVector(meshNormals[v]).normalized;

                    // Tangents need special handling (w is the handedness)
                    Vector3 t = localToWorld.MultiplyVector(new Vector3(meshTangents[v].x, meshTangents[v].y, meshTangents[v].z)).normalized;
                    transformedTangents[v] = new Vector4(t.x, t.y, t.z, meshTangents[v].w);
                }


                // Append vertex attributes
                vertices.AddRange(transformedVertices);
                normals.AddRange(transformedNormals);
                tangents.AddRange(transformedTangents);



                #endregion

                if (MergeMaterials)
                {
                    Vector2[] originalUVs = mesh.uv;
                    Vector2[] remappedUVs = new Vector2[originalUVs.Length];

                    Rect rect = atlasTransform[outfit.name];


                    for (int u = 0; u < originalUVs.Length; u++)
                    {
                        Vector2 uvVert = originalUVs[u];
                        remappedUVs[u] = new Vector2(
                            rect.x + uvVert.x * rect.width,
                            rect.y + uvVert.y * rect.height
                        );
                    }

                    uv.AddRange(remappedUVs);
                }
                else
                {
                    //UV merging
                    uv.AddRange(mesh.uv);

                    if (mesh.uv2 != null && mesh.uv2.Length == mesh.vertexCount)
                    {
                        // This mesh has valid UV2s
                        uv2.AddRange(mesh.uv2);
                    }
                    else
                    {
                        Vector2[] defaultUV2 = new Vector2[mesh.vertexCount];
                        uv2.AddRange(defaultUV2);
                        // This mesh has no UV2s
                    }
                }






                // Vertex colors
                Color[] meshColors = mesh.colors;
                if (meshColors != null && meshColors.Length == mesh.vertexCount)
                {
                    colors.AddRange(meshColors);
                }
                else
                {
                    // Fill with white if missing
                    for (int c = 0; c < mesh.vertexCount; c++)
                        colors.Add(Color.white);
                }




                if (hasSkeleton)
                {
                    // Remap bone weights
                    BoneWeight[] meshBoneWeights = mesh.boneWeights;
                    foreach (BoneWeight bw in meshBoneWeights)
                    {
                        //Debug.Log($"{bw.boneIndex0} {bw.boneIndex1} {bw.boneIndex2} {bw.boneIndex3}");
                        if (!boneIndexMap.ContainsKey(bw.boneIndex0))
                        {
                            BoneWeight newBw = new BoneWeight
                            {
                                boneIndex0 = boneIndexMap[boneMap[smr.rootBone.name].index],
                                weight0 = 1,
                            };
                            boneWeights.Add(newBw);
                        }
                        else
                        {
                            BoneWeight newBw = new BoneWeight
                            {
                                boneIndex0 = boneIndexMap[bw.boneIndex0],
                                boneIndex1 = boneIndexMap[bw.boneIndex1],
                                boneIndex2 = boneIndexMap[bw.boneIndex2],
                                boneIndex3 = boneIndexMap[bw.boneIndex3],
                                weight0 = bw.weight0,
                                weight1 = bw.weight1,
                                weight2 = bw.weight2,
                                weight3 = bw.weight3
                            };
                            boneWeights.Add(newBw);
                        }
                    }
                }
                else
                {
                    //Has no weights and is being assigned them
                    BoneWeight[] meshBoneWeights = new BoneWeight[mesh.vertexCount];
                    foreach (BoneWeight bw in meshBoneWeights)
                    {
                        BoneWeight newBw = new BoneWeight
                        {
                            boneIndex0 = boneIndexMap[bw.boneIndex0],
                            weight0 = 1,
                        };
                        boneWeights.Add(newBw);
                    }
                }


                if (MergeMaterials)
                {
                    // Submeshes and materials
                    for (int s = 0; s < mesh.subMeshCount; s++)
                    {
                        List<int> triangles = mesh.GetTriangles(s).ToList();
                        for (int t = 0; t < triangles.Count; t++)
                        {
                            triangles[t] += vertexOffset;
                        }
                        if (submeshTriangles.Count == 0)
                        {
                            submeshTriangles.Add(triangles);
                        }
                        else
                        {
                            submeshTriangles[0].AddRange(triangles);
                        }

                    }
                }
                else
                {
                    // Submeshes and materials
                    for (int s = 0; s < mesh.subMeshCount; s++)
                    {
                        List<int> triangles = mesh.GetTriangles(s).ToList();
                        for (int t = 0; t < triangles.Count; t++)
                        {
                            triangles[t] += vertexOffset;
                        }
                        submeshTriangles.Add(triangles);
                        materials.Add(smr.sharedMaterials[s]);
                    }

                }

                // Extract blendshapes
                int blendshapeCount = mesh.blendShapeCount;
                for (int b = 0; b < blendshapeCount; b++)
                {
                    string shapeName = mesh.GetBlendShapeName(b);
                    var split = shapeName.Split(".");
                    if (split.Length > 0)
                    {
                        shapeName = shapeName.Replace(split[0] + ".", "");
                    }
                    int frameCount = mesh.GetBlendShapeFrameCount(b);

                    for (int f = 0; f < frameCount; f++)
                    {
                        float weight = mesh.GetBlendShapeFrameWeight(b, f);
                        float currentWeight = smr.GetBlendShapeWeight(b);

                        Vector3[] deltaVertices = new Vector3[mesh.vertexCount];
                        Vector3[] deltaNormals = new Vector3[mesh.vertexCount];
                        Vector3[] deltaTangents = new Vector3[mesh.vertexCount];

                        mesh.GetBlendShapeFrameVertices(b, f, deltaVertices, deltaNormals, deltaTangents);

                        if (!blendshapeGroups.ContainsKey(shapeName))
                            blendshapeGroups[shapeName] = new List<BlendshapeData>();
                        blendshapeGroups[shapeName].Add(new BlendshapeData
                        {
                            name = shapeName,
                            weight = weight,
                            currentWeight = currentWeight,
                            deltaVertices = deltaVertices,
                            deltaNormals = deltaNormals,
                            deltaTangents = deltaTangents,
                            vertexOffset = vertexOffset
                        });
                    }
                }


                vertexOffset += mesh.vertexCount;
            }



            foreach (var item in outfitsToMerge)
            {
                UnityEngine.Object.DestroyImmediate(item.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(rig.gameObject);

            #endregion

            #region ------ Mesh Creation -----



            //Bind Pose Merge
            for (int i = 0; i < masterBones.Count; i++)
            {
                bindposes.Add(masterBones[i].worldToLocalMatrix * rootBone.localToWorldMatrix);
            }


            // Build combined mesh
            Mesh combinedMesh = new Mesh();
            combinedMesh.name = "Combined Skinned Mesh";
            combinedMesh.SetVertices(vertices);
            combinedMesh.SetNormals(normals);
            combinedMesh.SetTangents(tangents);
            combinedMesh.SetUVs(0, uv);
            combinedMesh.SetUVs(1, uv2);
            combinedMesh.SetColors(colors);
            combinedMesh.boneWeights = boneWeights.ToArray();
            combinedMesh.bindposes = bindposes.ToArray();
            combinedMesh.subMeshCount = submeshTriangles.Count;



            //Creating Material Regions
            for (int i = 0; i < submeshTriangles.Count; i++)
            {
                combinedMesh.SetTriangles(submeshTriangles[i], i);
            }

            #region BlendShapes
            // Create merged blendshapes
            foreach (var kv in blendshapeGroups)
            {
                string shapeName = kv.Key;
                List<BlendshapeData> entries = kv.Value;

                // Assuming all frames have the same weight
                float frameWeight = entries[0].weight;

                // Prepare deltas for the combined mesh size
                int totalVertices = combinedMesh.vertexCount;
                Vector3[] mergedDeltaVertices = new Vector3[totalVertices];
                Vector3[] mergedDeltaNormals = new Vector3[totalVertices];
                Vector3[] mergedDeltaTangents = new Vector3[totalVertices];

                foreach (var e in entries)
                {
                    for (int i = 0; i < e.deltaVertices.Length; i++)
                    {
                        mergedDeltaVertices[e.vertexOffset + i] = e.deltaVertices[i];
                        mergedDeltaNormals[e.vertexOffset + i] = e.deltaNormals[i];
                        mergedDeltaTangents[e.vertexOffset + i] = e.deltaTangents[i];
                    }
                }

                combinedMesh.AddBlendShapeFrame(
                    shapeName,
                    frameWeight,
                    mergedDeltaVertices,
                    mergedDeltaNormals,
                    mergedDeltaTangents
                );
            }
            #endregion

            #region Final GameObject
            // Create new GameObject
            GameObject mergedGO = new GameObject("CombinedSkinnedMesh");

            SkinnedMeshRenderer newRenderer = mergedGO.AddComponent<SkinnedMeshRenderer>();
            newRenderer.sharedMesh = combinedMesh;
            newRenderer.bones = masterBones.ToArray();
            newRenderer.rootBone = rootBone;

            if (mergedTexture)
            {
                newRenderer.material = newMaterial;
            }
            else
            {
                newRenderer.materials = materials.ToArray();
            }

            foreach (var key in blendshapeGroups.Keys)
            {
                var blendData = blendshapeGroups[key][0];

                var index = newRenderer.sharedMesh.GetBlendShapeIndex(blendData.name);
                newRenderer.SetBlendShapeWeight(index, blendData.currentWeight);
            }

            newRenderer.transform.parent = parent;

            Debug.Log("Dynamic skinning merge complete with bone remapping!");
            #endregion


            #region Saving Final GameObject
#if UNITY_EDITOR
            if (saveAsPrefab)
            {
                if (saveName == "") saveName = "NewCharacter";
                var assetPath = $"{Application.dataPath}{path}/{saveName}";
                var savePath = $"Assets/{path}/{saveName}";

                if (!System.IO.Directory.Exists($"{Application.dataPath}{path}/{saveName}"))
                {
                    System.IO.Directory.CreateDirectory(assetPath);
                    AssetDatabase.Refresh();
                }

                //Saving Mesh
                string meshPath = $"{savePath}/{saveName}_Mesh.asset";
                AssetDatabase.CreateAsset(newRenderer.sharedMesh, meshPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                //Saving Diffuse
                string diffusePath = $"{assetPath}/{saveName}_D.png";
                byte[] bytes = packedTextures.texture.EncodeToPNG();
                System.IO.File.WriteAllBytes(diffusePath, bytes);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>($"{savePath}/{saveName}_D.png");

                var savedAdditionalMaps = new Dictionary<string, Texture2D>();
                foreach (var additionalMap in packedTextures.additionalMaps)
                {
                    string additonalMapPath = $"{assetPath}/{saveName}{additionalMap.Key}.png";
                    bytes = additionalMap.Value.EncodeToPNG();
                    System.IO.File.WriteAllBytes(additonalMapPath, bytes);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    savedAdditionalMaps[additionalMap.Key] = AssetDatabase.LoadAssetAtPath<Texture2D>($"{savePath}/{saveName}{additionalMap.Key}.png");
                }


                //Saving CustomMaps
                foreach (var customMap in packedTextures.customMaps)
                {
                    string customMapPath = $"{assetPath}/{saveName}_{customMap.Key}.png";
                    bytes = customMap.Value.EncodeToPNG();
                    System.IO.File.WriteAllBytes(customMapPath, bytes);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                //Saving Material
                string matPath = $"{savePath}/{saveName}_Mat.mat";
                AssetDatabase.CreateAsset(newRenderer.material, matPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

                mat.mainTexture = diffuse;

                foreach (var additionalMap in savedAdditionalMaps)
                {
                    Debug.Log(additionalMap.Value);
                    mat.SetTexture(additionalMap.Key, additionalMap.Value);
                }

                BMAC_SaveSystem.LoadBodyMods(outfitSystem, outfitSystem.data);

                await Task.Yield();

                // Save the prefab
                string prefabPath = $"{savePath}/{saveName}.prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(parent.gameObject, prefabPath);
                var prefabSkinnedMeshrenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
                prefabSkinnedMeshrenderer.sharedMesh = mesh;
                prefabSkinnedMeshrenderer.sharedMaterial = mat;


                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Saved Prefab at: {prefabPath}");

            }
#endif
            #endregion


            #endregion


            return parent.gameObject;


        }

        public async Task<(Texture2D texture, Dictionary<string, Texture2D> additionalMaps, Dictionary<string, Rect> rect, Dictionary<string, Texture2D> customMaps)> CreateMergedTextures(List<Outfit> outfits)
        {
            var diffuseMaps = new List<Texture2D>();
            var normalMaps = new List<Texture2D>();
            var additionalMaps = new Dictionary<string, Texture2D[]>();
            var customMaps = new Dictionary<string, Texture2D[]>();
            var bakeMaterial = new Material(Shader.Find("BoZo/BakeTexture"));
            int index = 0;

            foreach (var item in outfits)
            {

                var smr = item.GetComponentInChildren<SkinnedMeshRenderer>();
                var renderer = item.GetComponentInChildren<Renderer>();
                var mesh = new List<Mesh>();

                foreach (var s in item.skinnedRenderers)
                {
                    mesh.Add(s.sharedMesh);
                }

                //Doesnt have a SkinnedMesh
                if (smr == null)
                {
                    mesh.Add(item.GetComponentInChildren<MeshFilter>(true).sharedMesh);
                }

                var originalMaterial = renderer.sharedMaterial;
                renderer.sharedMaterial = bakeMaterial;

                if (!item.customShader)
                {
                    //Copy All Colors and Data To Bake Material
                    bakeMaterial.mainTexture = originalMaterial.mainTexture;
                    bakeMaterial.SetTexture("_DecalMap", originalMaterial.GetTexture("_DecalMap"));
                    bakeMaterial.SetFloat("_DecalUVSet", originalMaterial.GetFloat("_DecalUVSet"));
                    bakeMaterial.SetFloat("_DecalBlend", originalMaterial.GetFloat("_DecalBlend"));
                    bakeMaterial.SetVector("_DecalScale", originalMaterial.GetVector("_DecalScale"));

                    bakeMaterial.SetTexture("_PatternMap", originalMaterial.GetTexture("_PatternMap"));
                    bakeMaterial.SetFloat("_PatternUVSet", originalMaterial.GetFloat("_PatternUVSet"));
                    bakeMaterial.SetFloat("_PatternBlend", originalMaterial.GetFloat("_PatternBlend"));
                    bakeMaterial.SetVector("_PatternScale", originalMaterial.GetVector("_PatternScale"));

                    for (int i = 0; i < 9; i++)
                    {
                        bakeMaterial.SetColor("_Color_" + (i + 1), originalMaterial.GetColor("_Color_" + (i + 1)));
                        bakeMaterial.SetColor("_Color_" + (i + 1), originalMaterial.GetColor("_Color_" + (i + 1)));

                        if (i + 1 <= 3)
                        {
                            bakeMaterial.SetColor("_DecalColor_" + (i + 1), originalMaterial.GetColor("_DecalColor_" + (i + 1)));
                            bakeMaterial.SetColor("_PatternColor_" + (i + 1), originalMaterial.GetColor("_PatternColor_" + (i + 1)));
                        }

                    }
                }
                else
                {
                    diffuseMaps.Add((Texture2D)originalMaterial.mainTexture);
                }

                foreach (var map in mergedMaterialDatas)
                {
                    if (!additionalMaps.ContainsKey(map.toMateiralProperty)) { additionalMaps[map.toMateiralProperty] = new Texture2D[outfits.Count]; }
                    additionalMaps[map.toMateiralProperty][index] = (Texture2D)originalMaterial.GetTexture(map.fromMateiralProperty);
                }

                //Get CustomMaps form extensions
                var extensions = item.GetComponentsInChildren<IOutfitExtension>();

                foreach (var extension in extensions)
                {
                    if (extension.GetValue() is Texture2D && !customMaps.ContainsKey(extension.GetID())) customMaps[extension.GetID()] = new Texture2D[outfits.Count];
                    if (extension.GetValue() is Texture2D)
                    {
                        customMaps[extension.GetID()][index] = (Texture2D)extension.GetValue();
                    }
                }


                var tex = await BakeTextureAsyncTask(mesh, bakeMaterial);

                diffuseMaps.Add(tex);

                renderer.sharedMaterial = originalMaterial;
                RenderTexture.active = null;

                index++;
            }


            int atlasSize = 2048;

            Texture2D atlas = new Texture2D(atlasSize, atlasSize);
            Texture2D atlasNormal = new Texture2D(atlasSize, atlasSize);
            Dictionary<string, Texture2D> additionalMapsList = new Dictionary<string, Texture2D>();
            Dictionary<string, Texture2D> atlasCustomMapsList = new Dictionary<string, Texture2D>();
            atlas.wrapMode = TextureWrapMode.Repeat;
            Rect[] rects = atlas.PackTextures(diffuseMaps.ToArray(), 0, atlasSize);

            //Creating Map for Outfits with Multiple Meshes
            Dictionary<string, Rect> rectMap = new Dictionary<string, Rect>();
            for (int i = 0; i < outfits.Count; i++)
            {
                rectMap.Add(outfits[i].name, rects[i]);
            }

            var pixels = atlas.GetPixels32();
            pixels = await DilateTextureAsync(atlas, pixels);

            atlas.SetPixels32(pixels);
            atlas.Apply();

            var addIndex = 0;
            foreach (var additional in additionalMaps)
            {
                var additionalValues = additional.Value.ToList();
                Texture2D newAdditionalMap = new Texture2D(atlasSize, atlasSize);
                newAdditionalMap.wrapMode = TextureWrapMode.Repeat;
                additionalMapsList[additional.Key] = await RemapTextureAsync(additionalValues, rects, atlasSize, newAdditionalMap, mergedMaterialDatas[addIndex].backgroundColor);
                addIndex++;
            }

            foreach (var custom in customMaps)
            {
                var customValues = custom.Value.ToList();
                if (customValues.Count == 0) continue;
                Texture2D newCustomMap = new Texture2D(atlasSize, atlasSize);
                newCustomMap.wrapMode = TextureWrapMode.Repeat;
                atlasCustomMapsList[custom.Key] = await RemapTextureAsync(customValues, rects, atlasSize, newCustomMap, Color.black);
            }

            return (atlas, additionalMapsList, rectMap, atlasCustomMapsList);
        }

        public async Task<Texture2D> BakeTextureAsyncTask(List<Mesh> mesh, Material bakeMaterial)
        {
            var textureSize = bakeMaterial.mainTexture.width;
            var rt = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32)
            {
                useMipMap = false,
                autoGenerateMips = false
            };
            rt.Create();

            CommandBuffer cb = new CommandBuffer();
            cb.SetRenderTarget(rt);
            cb.ClearRenderTarget(true, true, Color.clear);
            foreach (var m in mesh)
            {
                cb.DrawMesh(m, Matrix4x4.identity, bakeMaterial);
            }
            Graphics.ExecuteCommandBuffer(cb);
            cb.Release();

            var tcs = new TaskCompletionSource<Texture2D>();

            AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBA32, request =>
            {
                if (request.hasError)
                {
                    tcs.SetException(new Exception("Async GPU readback failed."));
                }
                else
                {
                    var data = request.GetData<Color32>();
                    var tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
                    tex.LoadRawTextureData(data);
                    tex.Apply();
                    tcs.SetResult(tex);
                }
            });

            return await tcs.Task;
        }

        public async Task<Texture2D> RemapTextureAsync(List<Texture2D> normalMaps, Rect[] rects, int atlasSize, Texture2D atlasNormal, Color fillColor)
        {
            List<Task> pendingReads = new List<Task>();

            Color[] pixels = new Color[atlasSize * atlasSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = fillColor;
            }
            atlasNormal.SetPixels(pixels);

            for (int i = 0; i < normalMaps.Count; i++)
            {
                if (normalMaps[i] == null) continue;
                Rect r = rects[i];

                int x = Mathf.RoundToInt(r.x * atlasSize);
                int y = Mathf.RoundToInt(r.y * atlasSize);
                int w = Mathf.RoundToInt(r.width * atlasSize);
                int h = Mathf.RoundToInt(r.height * atlasSize);

                Texture2D normal = normalMaps[i];

                RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(normal, rt);

                int copyX = x, copyY = y, copyW = w, copyH = h;
                RenderTexture copyRT = rt;

                var tcs = new TaskCompletionSource<bool>();
                pendingReads.Add(tcs.Task);

                AsyncGPUReadback.Request(copyRT, 0, TextureFormat.RGBA32, request =>
                {
                    if (request.hasError)
                    {
                        Debug.LogError("Normal map readback failed");
                        tcs.SetResult(true);
                        RenderTexture.ReleaseTemporary(copyRT);
                        return;
                    }

                    var data = request.GetData<Color32>();
                    Color32[] pixels = data.ToArray();

                    // Fill into the atlas
                    Color[] colors = new Color[pixels.Length];
                    for (int j = 0; j < pixels.Length; j++)
                    {
                        colors[j] = pixels[j]; // Converts Color32 to Color
                    }

                    atlasNormal.SetPixels(copyX, copyY, copyW, copyH, colors);

                    RenderTexture.ReleaseTemporary(copyRT);
                    tcs.SetResult(true);
                });
            }

            await Task.WhenAll(pendingReads);

            atlasNormal.Apply(); // Apply once after all blocks are set
            return atlasNormal;
        }



        public async Task<Color32[]> DilateTextureAsync(Texture2D tex, Color32[] pixels)
        {
            var iterations = 2;
            int w = tex.width;
            int h = tex.height;

            Color32[] resultPixels = await Task.Run(() =>
            {
                Color32[] workingPixels = new Color32[pixels.Length];
                Color32[] temp = new Color32[pixels.Length];
                bool[] originalMask = new bool[pixels.Length];

                // Initialize workingPixels and mask
                for (int i = 0; i < pixels.Length; i++)
                {
                    workingPixels[i] = pixels[i];
                    originalMask[i] = pixels[i].a >= 255; // original valid pixels
                }

                for (int it = 0; it < iterations; it++)
                {
                    workingPixels.CopyTo(temp, 0);

                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int idx = y * w + x;
                            if (workingPixels[idx].a >= 255) continue;

                            for (int dy = -1; dy <= 1; dy++)
                            {
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    if (dx == 0 && dy == 0) continue;

                                    int nx = x + dx;
                                    int ny = y + dy;
                                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                                    int nIdx = ny * w + nx;

                                    // ONLY copy from original source pixels
                                    if (originalMask[nIdx])
                                    {
                                        temp[idx] = workingPixels[nIdx];
                                        goto FoundPixel;
                                    }
                                }
                            }

                        FoundPixel:;
                        }
                    }

                    // Update working pixels
                    var swap = workingPixels;
                    workingPixels = temp;
                    temp = swap;

                    // After first iteration, also allow pixels from the new ones as source
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        if (workingPixels[i].a >= 1)
                            originalMask[i] = true;
                    }
                }

                return workingPixels;
            });

            return resultPixels;
        }



        private class boneData
        {
            public Transform bone;
            public int index;
        }

        struct BlendshapeData
        {
            public string name;
            public float weight;
            public float currentWeight;
            public Vector3[] deltaVertices;
            public Vector3[] deltaNormals;
            public Vector3[] deltaTangents;
            public int vertexOffset;
        }

    }

    [System.Serializable]
    public class MergedMaterialData
    {
        public string fromMateiralProperty;
        public string toMateiralProperty;
        public Color backgroundColor = Color.gray;
    }

}
