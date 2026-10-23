using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Jotunn.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace VerlerLeben
{
    /// <summary>
    /// Puts the cow (Quaternius, CC0, smoothed and with baked coat textures) on the Lox skeleton. The mesh in
    /// Assets/cow_mesh.bin is already aligned to the Lox body and weighted to its bones (tools/cow), so the cow walks,
    /// eats and dies with the Lox animations. Each cow gets one of three coats, the same on every client.
    /// </summary>
    internal static class CowModel
    {
        private const string MeshResource = "VerlerLeben.cow_mesh.bin";

        // Holstein (black and white), red and white, brown. Weights: how often each coat appears.
        private static readonly (string resource, int weight)[] Coats =
        {
            ("VerlerLeben.cow_schwarzbunt.png", 5),
            ("VerlerLeben.cow_rotbunt.png", 3),
            ("VerlerLeben.cow_braun.png", 2),
        };

        private static Mesh _mesh;
        internal static Material[] CoatMaterials;

        /// <summary>Returns false if the prefab has no Lox body to replace or the data is missing.</summary>
        public static bool Apply(GameObject prefab)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return true;   // dedicated server, nothing is drawn there
            }

            SkinnedMeshRenderer body = FindBody(prefab);
            if (body == null || body.sharedMesh == null)
            {
                return false;
            }

            if (_mesh == null)
            {
                _mesh = LoadMesh(body.bones);
                _mesh.bindposes = body.sharedMesh.bindposes;
                CoatMaterials = new Material[Coats.Length];
                for (int i = 0; i < Coats.Length; i++)
                {
                    CoatMaterials[i] = CoatMaterial(body.sharedMaterial, LoadTexture(Coats[i].resource));
                }
            }

            body.sharedMesh = _mesh;
            body.sharedMaterials = new[] { CoatMaterials[0] };
            if (prefab.GetComponent<CowLegs>() == null)
            {
                prefab.AddComponent<CowLegs>();
            }

            if (prefab.GetComponent<CowCoat>() == null)
            {
                prefab.AddComponent<CowCoat>();
            }

            // The shaggy fur cards and the saddle belong to the Lox.
            foreach (SkinnedMeshRenderer renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer != body)
                {
                    renderer.gameObject.SetActive(false);
                }
            }

            return true;
        }

        internal static SkinnedMeshRenderer FindBody(GameObject root)
        {
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.name == "Body")
                {
                    return renderer;
                }
            }

            return null;
        }

        /// <summary>Picks a coat from a number every client agrees on (the object's network id).</summary>
        internal static int CoatFor(uint id)
        {
            int total = 0;
            foreach (var coat in Coats)
            {
                total += coat.weight;
            }

            int pick = (int)(id % (uint)total);
            for (int i = 0; i < Coats.Length; i++)
            {
                if (pick < Coats[i].weight)
                {
                    return i;
                }

                pick -= Coats[i].weight;
            }

            return 0;
        }

        private static Material CoatMaterial(Material lox, Texture2D coat)
        {
            var material = new Material(lox) { mainTexture = coat };
            if (material.HasProperty("_BumpMap"))
            {
                // The Lox normal map does not fit the cow's UVs; a flat one keeps the mesh normals.
                material.SetTexture("_BumpMap", FlatNormalMap());
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.black);   // the Lox eye glows, a cow's does not
            }

            if (material.HasProperty("_EmissionMap"))
            {
                material.SetTexture("_EmissionMap", Texture2D.blackTexture);
            }

            material.DisableKeyword("_EMISSION");

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0f);   // matt coat
            }

            return material;
        }

        private static Mesh LoadMesh(Transform[] skeleton)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(MeshResource))
            using (var reader = new BinaryReader(stream))
            {
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "COW2")
                {
                    throw new InvalidDataException("cow_mesh.bin has an unknown format.");
                }

                // The file names its bones; find each one in this skeleton.
                var byName = new Dictionary<string, int>();
                for (int i = 0; i < skeleton.Length; i++)
                {
                    if (skeleton[i] != null)
                    {
                        byName[skeleton[i].name] = i;
                    }
                }

                int boneCount = reader.ReadInt32();
                var remap = new int[boneCount];
                for (int i = 0; i < boneCount; i++)
                {
                    string name = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
                    remap[i] = byName.TryGetValue(name, out int index) ? index : 0;
                }

                int vertexCount = reader.ReadInt32();
                var vertices = new Vector3[vertexCount];
                var normals = new Vector3[vertexCount];
                var uvs = new Vector2[vertexCount];
                var weights = new BoneWeight[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    uvs[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    byte b0 = reader.ReadByte(), b1 = reader.ReadByte(), b2 = reader.ReadByte(), b3 = reader.ReadByte();
                    weights[i] = new BoneWeight
                    {
                        boneIndex0 = remap[b0], boneIndex1 = remap[b1], boneIndex2 = remap[b2], boneIndex3 = remap[b3],
                        weight0 = reader.ReadSingle(), weight1 = reader.ReadSingle(), weight2 = reader.ReadSingle(), weight3 = reader.ReadSingle(),
                    };
                }

                int indexCount = reader.ReadInt32();
                var triangles = new int[indexCount];
                for (int i = 0; i < indexCount; i++)
                {
                    triangles[i] = reader.ReadInt32();
                }

                var mesh = new Mesh { name = "DF_Kuh" };
                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.uv = uvs;
                mesh.boneWeights = weights;
                mesh.triangles = triangles;
                mesh.RecalculateTangents();   // the creature shader uses a normal map
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static Texture2D _flatNormal;

        /// <summary>Flat normal in both encodings Unity uses: RGB (0.5, 0.5, 1) and DXT5nm (G and A = 0.5).</summary>
        private static Texture2D FlatNormalMap()
        {
            if (_flatNormal == null)
            {
                _flatNormal = new Texture2D(4, 4, TextureFormat.RGBA32, false, true) { name = "DF_FlatNormal" };
                var pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color32(128, 128, 255, 128);
                }

                _flatNormal.SetPixels32(pixels);
                _flatNormal.Apply(false);
            }

            return _flatNormal;
        }

        private static Texture2D LoadTexture(string resource)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                Texture2D texture = AssetUtils.LoadImage(buffer.ToArray());
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.anisoLevel = 4;
                return texture;
            }
        }
    }

    /// <summary>Gives every cow and calf its coat, chosen by its network id so all players see the same.</summary>
    internal class CowCoat : MonoBehaviour
    {
        private void Start()
        {
            var nview = GetComponent<ZNetView>();
            SkinnedMeshRenderer body = CowModel.FindBody(gameObject);
            if (nview == null || !nview.IsValid() || body == null || CowModel.CoatMaterials == null)
            {
                return;
            }

            body.sharedMaterial = CowModel.CoatMaterials[CowModel.CoatFor(nview.GetZDO().m_uid.ID)];
        }
    }
}


namespace VerlerLeben
{
    /// <summary>
    /// The Lox animations splay the legs sideways, which suits the bulky Lox but gives the slim cow bow legs.
    /// After the animation of each frame, this removes the sideways part of every leg segment, so the legs stay
    /// straight seen from the front and only swing forwards and backwards.
    /// </summary>
    internal class CowLegs : UnityEngine.MonoBehaviour
    {
        private static readonly string[][] Chains =
        {
            new[] { "Frontupperleg.l", "Frontlowerleg.l", "Frontfoot.l" },
            new[] { "Frontupperleg.r", "Frontlowerleg.r", "Frontfoot.r" },
            new[] { "Backupperleg.l", "Backlowerleg.l", "Backfoot.l" },
            new[] { "Backupperleg.r", "Backlowerleg.r", "Backfoot.r" },
        };

        private UnityEngine.Transform[][] _legs;

        private void Awake()
        {
            var byName = new System.Collections.Generic.Dictionary<string, UnityEngine.Transform>();
            foreach (UnityEngine.Transform t in GetComponentsInChildren<UnityEngine.Transform>(true))
            {
                if (!byName.ContainsKey(t.name))
                {
                    byName[t.name] = t;
                }
            }

            var legs = new System.Collections.Generic.List<UnityEngine.Transform[]>();
            foreach (string[] chain in Chains)
            {
                if (byName.TryGetValue(chain[0], out var upper) && byName.TryGetValue(chain[1], out var lower) && byName.TryGetValue(chain[2], out var foot))
                {
                    legs.Add(new[] { upper, lower, foot });
                }
            }

            _legs = legs.ToArray();
        }

        private void LateUpdate()
        {
            UnityEngine.Vector3 right = transform.right;
            foreach (UnityEngine.Transform[] leg in _legs)
            {
                Straighten(leg[0], leg[1], right);
                Straighten(leg[1], leg[2], right);
            }
        }

        private static void Straighten(UnityEngine.Transform joint, UnityEngine.Transform child, UnityEngine.Vector3 right)
        {
            UnityEngine.Vector3 direction = child.position - joint.position;
            UnityEngine.Vector3 straight = direction - UnityEngine.Vector3.Project(direction, right);
            if (straight.sqrMagnitude < 1e-8f || direction.sqrMagnitude < 1e-8f)
            {
                return;
            }

            joint.rotation = UnityEngine.Quaternion.FromToRotation(direction, straight) * joint.rotation;
        }
    }
}
