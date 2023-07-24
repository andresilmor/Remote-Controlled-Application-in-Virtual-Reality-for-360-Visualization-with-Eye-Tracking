using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PanoramicManager  {

    public static Texture2D CurrentHotspotTexture;

    public static void PrepareHotspotMaterial(ref GameObject sphere, Texture2D panoramicTexture) {
        // Reverse the normals to see the texture from inside the sphere
        MeshFilter meshFilter = sphere.GetComponent<MeshFilter>();
        if (meshFilter != null) {
            Mesh mesh = meshFilter.mesh;
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++) {
                normals[i] = -normals[i];
            }
            mesh.normals = normals;

            // Reverse the triangle winding to display the sphere correctly
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3) {
                int temp = triangles[i];
                triangles[i] = triangles[i + 1];
                triangles[i + 1] = temp;
            }
            mesh.triangles = triangles;
        }

        // Create a new material using the panoramic texture
        Material sphereMaterial = new Material(Shader.Find("Standard"));
        sphereMaterial.mainTexture = panoramicTexture;

        // Assign the material to the sphere
        Renderer sphereRenderer = sphere.GetComponent<Renderer>();
        if (sphereRenderer != null) {
            sphereRenderer.sharedMaterial = sphereMaterial;
        }


    }


}

