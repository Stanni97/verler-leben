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
    /// Puts the low-poly cow (Quaternius, CC0) on the Lox skeleton. The mesh in Assets/cow_mesh.bin is already
    /// aligned to the Lox body and weighted to its bones (tools/cow/build_cow.py), so the cow walks, eats and dies
    /// with the Lox animations.
    /// </summary>
    internal static class CowModel
    {
        private const string MeshResource = "VerlerLeben.cow_mesh.bin";
        private const string PaletteResource = "VerlerLeben.cowpalette.png";

        private static Mesh _mesh;
        private static Texture2D _palette;

        /// <summary>Returns false if the prefab has no Lox body to replace or the data is missing.</summary>
        public static bool Apply(GameObject prefab)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return true;   // dedicated server, nothing is drawn there
            }

            SkinnedMeshRenderer body = null;
            foreach (SkinnedMeshRenderer renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.name == "Body")
                {
                    body = renderer;
                }
            }

            if (body == null || body.sharedMesh == null)
            {
                return false;
            }

            if (_mesh == null)
            {
                _mesh = LoadMesh(body.bones);
                _mesh.bindposes = body.sharedMesh.bindposes;
                _palette = LoadPalette();
            }

            body.sharedMesh = _mesh;
            var material = new Material(body.sharedMaterial) { mainTexture = _palette };
            if (material.HasProperty("_BumpMap"))
            {
                // Without a normal map Unity samples plain white, the normals then point at the sky and every
                // white face turns blue. A flat normal map keeps the real face normals.
                material.SetTexture("_BumpMap", FlatNormalMap());
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.black);
            }

            body.sharedMaterials = new[] { material };

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

        private static Mesh LoadMesh(Transform[] skeleton)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(MeshResource))
            using (var reader = new BinaryReader(stream))
            {
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "COW1")
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
                var uvs = new Vector2[vertexCount];
                var weights = new BoneWeight[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
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
                mesh.uv = uvs;
                mesh.boneWeights = weights;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();   // vertices are not shared, so the low-poly faces stay flat
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

        private static Texture2D LoadPalette()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(PaletteResource))
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                Texture2D texture = AssetUtils.LoadImage(buffer.ToArray());
                texture.filterMode = FilterMode.Point;   // sharp colours, no bleeding between the palette cells
                texture.wrapMode = TextureWrapMode.Clamp;
                return texture;
            }
        }
    }
}
