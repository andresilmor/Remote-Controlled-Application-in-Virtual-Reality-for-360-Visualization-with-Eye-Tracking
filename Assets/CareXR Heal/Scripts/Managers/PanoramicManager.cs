using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public static class PanoramicManager  {

    public static Texture2D CurrentHotspotTexture;

    private static float _horizontalFOV = 360f;
    private static float _verticalFOV = 180f;
    private static float _sphereRadius = 4.5f;

    private static GameObject _currentSphere = null;
    private static GameObject _hotspotContainer = null;

    public static int panoramicImageWidth = 0;
    public static int panoramicImageHeight = 0;

    public static void ApplySphereTexture(ref GameObject sphere, Texture2D panoramicTexture) {
        // Reverse the normals to see the texture from inside the sphere

        _currentSphere = sphere;

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


    // Diameter of the sphere
    public static float sphereDiameter = 9f;

    [System.Serializable]
    public class BoundingBoxData {
        public int x;
        public int y;
        public int width;
        public int height;
    }


    private static Vector3 ConvertBoundingBoxCenterTo3D(JToken boundingBoxData) {
        // Parse the bounding box JSON data
        // Calculate the center position of the bounding box
        float centerX = (float)boundingBoxData["x"] + (float)boundingBoxData["width"] / 2f;
        float centerY = (float)boundingBoxData["y"] + (float)boundingBoxData["height"] / 2f;

        // Convert 2D bounding box center to spherical coordinates
        float theta = (centerY / panoramicImageHeight) * Mathf.PI;
        float phi = (centerX / panoramicImageWidth) * Mathf.PI * 2f;

        // Convert spherical coordinates to 3D position on the sphere's surface
        float radius = sphereDiameter / 2f;
        Vector3 spherePosition = new Vector3(
            radius * Mathf.Sin(theta) * Mathf.Cos(phi),
            radius * Mathf.Cos(theta),
            radius * Mathf.Sin(theta) * Mathf.Sin(phi)
        );

        return spherePosition;

    }

    public static void MountHotspots(JToken data, Action onComplete) {

        panoramicImageWidth = (int)data["imageWidth"];
        panoramicImageHeight = (int)data["imageHeight"];

        _hotspotContainer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _hotspotContainer.transform.position = Vector3.zero;
        _hotspotContainer.transform.rotation = Quaternion.identity;
        _hotspotContainer.transform.localScale = Vector3.one * sphereDiameter;

        float radius = sphereDiameter / 2f;
        float theta, phi, x, y;

        foreach (JToken hotspot in data["mapping"]) {

            if (hotspot["boundingBox"].HasValues) {

                Vector3 centerPosition = ConvertBoundingBoxCenterTo3D(hotspot["boundingBox"]);

                GameObject boundigBox = GameObject.CreatePrimitive(PrimitiveType.Cube);

                x = (float)hotspot["boundingBox"]["x"];
                y = (float)hotspot["boundingBox"]["y"];

                theta = (y / panoramicImageHeight) * Mathf.PI;
                phi = (x / panoramicImageWidth) * Mathf.PI * 2f;

                Vector3 x1y1 = new Vector3(
                    radius * Mathf.Sin(theta) * Mathf.Cos(phi),
                    radius * Mathf.Cos(theta),
                    radius * Mathf.Sin(theta) * Mathf.Sin(phi)
                );

                x = (float)hotspot["boundingBox"]["x"] + (float)hotspot["boundingBox"]["width"];
                y = (float)hotspot["boundingBox"]["y"];

                theta = (y / panoramicImageHeight) * Mathf.PI;
                phi = (x / panoramicImageWidth) * Mathf.PI * 2f;

                Vector3 x2y1 = new Vector3(
                    radius * Mathf.Sin(theta) * Mathf.Cos(phi),
                    radius * Mathf.Cos(theta),
                    radius * Mathf.Sin(theta) * Mathf.Sin(phi)
                );

                Vector3 width = x1y1 - x2y1;

                x = (float)hotspot["boundingBox"]["x"] + (float)hotspot["boundingBox"]["width"];
                y = (float)hotspot["boundingBox"]["y"] + (float)hotspot["boundingBox"]["height"];

                theta = (y / panoramicImageHeight) * Mathf.PI;
                phi = (x / panoramicImageWidth) * Mathf.PI * 2f;

                Vector3 x2y2 = new Vector3(
                    radius * Mathf.Sin(theta) * Mathf.Cos(phi),
                    radius * Mathf.Cos(theta),
                    radius * Mathf.Sin(theta) * Mathf.Sin(phi)
                );

                Vector3 height = x2y2 - x2y1;

                boundigBox.gameObject.transform.localScale = new Vector3(width.magnitude, height.magnitude, 0.0005f);
                boundigBox.gameObject.transform.position = centerPosition;
                boundigBox.gameObject.transform.LookAt(new Vector3(0, 0, 0));
                boundigBox.transform.parent = _hotspotContainer.transform;

                HotspotHandler hotspotHandler = boundigBox.AddComponent<HotspotHandler>();

                boundigBox.gameObject.layer = LayerMask.NameToLayer("Hotspot");

                hotspotHandler.SetHotspotData(hotspot["data"]["alias"].ToString(), hotspot["uuid"].ToString(), hotspot["data"]["content"]);

            }

        }

        _hotspotContainer.transform.position = new Vector3(0, 1.43f, 0);

        onComplete?.Invoke();

        return;
        
        
    }
}
