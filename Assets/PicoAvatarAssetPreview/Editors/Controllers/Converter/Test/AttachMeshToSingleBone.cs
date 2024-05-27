#if UNITY_EDITOR
using UnityEngine;

namespace Pico
{
    namespace AvatarAssetPreview
    {
        [ExecuteInEditMode]
        public class AttachMeshToSingleBone : MonoBehaviour
        {
            public Transform bone;

            private void OnGUI()
            {
                if (GUILayout.Button("AttachMeshToSingleBone"))
                {
                    Work();
                }
            }

            public void Work()
            {
                var mesh = GetComponent<MeshFilter>().sharedMesh;
                mesh = Object.Instantiate(mesh);
                var material = GetComponent<MeshRenderer>().sharedMaterial;

                var boneWeights = new BoneWeight[mesh.vertexCount];
                for (int i = 0; i < boneWeights.Length; ++i)
                {
                    var boneWeight = new BoneWeight();
                    boneWeight.boneIndex0 = 0;
                    boneWeight.boneIndex1 = 0;
                    boneWeight.boneIndex2 = 0;
                    boneWeight.boneIndex3 = 0;
                    boneWeight.weight0 = 1;
                    boneWeight.weight1 = 0;
                    boneWeight.weight2 = 0;
                    boneWeight.weight3 = 0;
                    boneWeights[i] = boneWeight;
                }
                mesh.boneWeights = boneWeights;

                var bindposes = new Matrix4x4[1];
                bindposes[0] = bone.worldToLocalMatrix * transform.localToWorldMatrix;
                mesh.bindposes = bindposes;

                var bones = new Transform[1];
                bones[0] = bone;

                var skin = new GameObject(gameObject.name).AddComponent<SkinnedMeshRenderer>();
                skin.sharedMesh = mesh;
                skin.bones = bones;
                skin.rootBone = bone;
                skin.sharedMaterial = material;

                UnityEditor.AssetDatabase.CreateAsset(mesh, "Assets/" + gameObject.name + ".asset");
            }
        }
    }
}

#endif