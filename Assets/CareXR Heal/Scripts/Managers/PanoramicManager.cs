using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.UIElements;

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

    // JSON data for the bounding box
    private static string boundingBoxJson = @"{
        ""x"": 4074,
        ""y"": 1504,
        ""width"": 229,
        ""height"": 331
    }";

    // JSON data for the second bounding box
    private static string boundingBoxJsonTwo = @"{
        ""x"": 6355,
        ""y"": 1621,
        ""width"": 168,
        ""height"": 152
    }";

    private static string boundingBoxJsonThree = @"{
        ""x"": 1877,
        ""y"": 1455,
        ""width"": 478,
        ""height"": 766
    }";

    [System.Serializable]
    private class BoundingBoxData {
        public int x;
        public int y;
        public int width;
        public int height;
    }

    private static Vector3 ConvertBoundingBoxTo3DPosition(string boundingBoxData) {
        // Parse the bounding box JSON data
        BoundingBoxData boundingBox = JsonUtility.FromJson<BoundingBoxData>(boundingBoxData);

        // Calculate the center position of the bounding box
        float centerX = boundingBox.x + boundingBox.width / 2f;
        float centerY = boundingBox.y + boundingBox.height / 2f;

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

    private static Vector3 PixelToSphere(float x, float y) {
        // Normalize pixel coordinates to range [0, 1]
        float normalizedX = x / (float)panoramicImageWidth;
        float normalizedY = y / (float)panoramicImageHeight;

        // Calculate theta and phi angles using normalized pixel coordinates
        float theta = Mathf.PI * normalizedY;
        float phi = 2f * Mathf.PI * normalizedX;

        // Calculate the 3D position on the sphere's surface
        float c_t = Mathf.Cos(theta);
        return new Vector3(c_t * Mathf.Cos(phi), c_t * Mathf.Sin(phi), Mathf.Sin(theta)) * (sphereDiameter / 2f);
    }

    private static Vector3 Mercator(float x, float y, int w, int h) {
        // outside of valid pixel region
        if (x < -0.5f || x >= w - 0.5f || y < -0.5f || y >= h - 0.5f)
            return new Vector3();

        float theta = (float)((y + 0.5f) / h * Math.PI);
        float phi = (float)(((x + 0.5f) / w - 0.5f) * 2.0 * Math.PI);

        float c_t = (float)Math.Cos(theta);
        return new Vector3((float)(c_t * Math.Cos(phi)), (float)(c_t * Math.Sin(phi)), (float)Math.Sin(theta));
    }

    private static Vector3 CalculateCubeScale(int boundingBoxWidth, int boundingBoxHeight) {
        float scaleX = (float)panoramicImageWidth / (float)boundingBoxWidth;
        float scaleY = (float)panoramicImageHeight / (float)boundingBoxHeight;
        return new Vector3 (scaleX, scaleY, 0.1f);

    }




    public static void MountHotspots(JToken data, Action onComplete) {

        //float imageHeight = 3456;
        //float imageWidth =  6912;

        panoramicImageWidth = (int)data["imageWidth"];
        panoramicImageHeight = (int)data["imageHeight"];

        _hotspotContainer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _hotspotContainer.transform.position = Vector3.zero;
        _hotspotContainer.transform.rotation = Quaternion.identity;
        _hotspotContainer.transform.localScale = Vector3.one * sphereDiameter;

        //Vector3 cubePosition1 = ConvertBoundingBoxTo3DPosition(boundingBoxJson);
        //Vector3 cubePosition2 = ConvertBoundingBoxTo3DPosition(boundingBoxJsonTwo);
        //Vector3 cubePosition3 = ConvertBoundingBoxTo3DPosition(boundingBoxJsonThree);

        float radius = sphereDiameter / 2f;
        float theta, phi, x, y;

        foreach (JToken hotspot in data["mapping"]) {
            Debug.Log("||----||");
            Debug.Log(hotspot);

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

        }

        _hotspotContainer.transform.position = new Vector3(0, 1.43f, 0);

        onComplete?.Invoke();

        return;

        BoundingBoxData boundingBoxTest = JsonUtility.FromJson<BoundingBoxData>(boundingBoxJson);
        float centerX = boundingBoxTest.x ;
        float centerY = boundingBoxTest.y ;
       
        // Convert 2D bounding box center to spherical coordinates
         theta = (centerY / panoramicImageHeight) * Mathf.PI;
         phi = (centerX / panoramicImageWidth) * Mathf.PI * 2f;

        // Convert spherical coordinates to 3D position on the sphere's surface
         radius = sphereDiameter / 2f;
        Vector3 spherePosition = new Vector3(
            radius * Mathf.Sin(theta) * Mathf.Cos(phi),
            radius * Mathf.Cos(theta),
            radius * Mathf.Sin(theta) * Mathf.Sin(phi)
        );

        //GameObject sphereTest = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphereTest.transform.position = spherePosition;
        //sphereTest.transform.localScale = Vector3.one * 0.1f;
        //sphereTest.transform.parent = sphere.transform;
        //sphereTest.gameObject.name = "x1, y1";

        centerX = boundingBoxTest.x + boundingBoxTest.width;
        centerY = boundingBoxTest.y;

        // Convert 2D bounding box center to spherical coordinates
        theta = (centerY / panoramicImageHeight) * Mathf.PI;
        phi = (centerX / panoramicImageWidth) * Mathf.PI * 2f;

        // Convert spherical coordinates to 3D position on the sphere's surface
        radius = sphereDiameter / 2f;
        Vector3 spherePosition1 = new Vector3(
            radius * Mathf.Sin(theta) * Mathf.Cos(phi),
            radius * Mathf.Cos(theta),
            radius * Mathf.Sin(theta) * Mathf.Sin(phi)
        );

        //GameObject sphereTest1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphereTest1.transform.position = spherePosition1;
        //sphereTest1.transform.localScale = Vector3.one * 0.1f;
        //sphereTest1.transform.parent = sphere.transform;
        //sphereTest1.gameObject.name = "x2, y1";


        centerX = boundingBoxTest.x ;
        centerY = boundingBoxTest.y + boundingBoxTest.height;

        // Convert 2D bounding box center to spherical coordinates
        theta = (centerY / panoramicImageHeight) * Mathf.PI;
        phi = (centerX / panoramicImageWidth) * Mathf.PI * 2f;

        // Convert spherical coordinates to 3D position on the sphere's surface
        radius = sphereDiameter / 2f;
        Vector3 spherePosition2 = new Vector3(
            radius * Mathf.Sin(theta) * Mathf.Cos(phi),
            radius * Mathf.Cos(theta),
            radius * Mathf.Sin(theta) * Mathf.Sin(phi)
        );

        //GameObject sphereTest2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphereTest2.transform.position = spherePosition2;
        //sphereTest2.transform.localScale = Vector3.one * 0.1f;
        //sphereTest2.transform.parent = sphere.transform;
        //sphereTest2.gameObject.name = "x1, y2";


        centerX = boundingBoxTest.x + boundingBoxTest.width;
        centerY = boundingBoxTest.y + boundingBoxTest.height;

        // Convert 2D bounding box center to spherical coordinates
        theta = (centerY / panoramicImageHeight) * Mathf.PI;
        phi = (centerX / panoramicImageWidth) * Mathf.PI * 2f;

        // Convert spherical coordinates to 3D position on the sphere's surface
        radius = sphereDiameter / 2f;
        Vector3 spherePosition3 = new Vector3(
            radius * Mathf.Sin(theta) * Mathf.Cos(phi),
            radius * Mathf.Cos(theta),
            radius * Mathf.Sin(theta) * Mathf.Sin(phi)
        );

        //GameObject sphereTest3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphereTest3.transform.position = spherePosition3;
        //sphereTest3.transform.localScale = Vector3.one * 0.1f;
        //sphereTest3.transform.parent = sphere.transform;
        //sphereTest3.gameObject.name = "x2, y2";
        /*
         * 
 
       GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
       Vector3 width = spherePosition - spherePosition1;

       Vector3 height = spherePosition3 - spherePosition1;
       box.gameObject.transform.localScale = new Vector3(width.magnitude, height.magnitude, 0.0005f);
       box.gameObject.transform.position = cubePosition1;
       box.gameObject.transform.LookAt(new Vector3(0,0,0));
       box.transform.parent = _hotspotContainer.transform;

       //----


       GameObject cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
       cube1.transform.position = cubePosition1;
       cube1.transform.localScale = Vector3.one * 0.1f;
       cube1.transform.parent = _hotspotContainer.transform;
       //cube1.transform.LookAt(new Vector3(0, -179, 0));  

       GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
       cube2.transform.localScale = Vector3.one * 0.1f;
       cube2.transform.position = cubePosition2;
       cube2.transform.parent = _hotspotContainer.transform;

       GameObject cube3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
       cube3.transform.localScale = Vector3.one * 0.1f;
       cube3.transform.position = cubePosition3;
       cube3.transform.parent = _hotspotContainer.transform;
       */

        _hotspotContainer.transform.position = new Vector3(0, 1.43f, 0);

        /*
        foreach (JToken hotspot in data["mapping"]) {
            JToken boundingBox = hotspot["boundingBox"];

            Rect boundingBoxRect = new Rect((float)boundingBox["x"], (float)boundingBox["y"], (float)boundingBox["width"], (float)boundingBox["height"]);

            float normalizedX = boundingBoxRect.x / imageWidth;
            float normalizedY = boundingBoxRect.y / imageHeight;
            float normalizedWidth = boundingBoxRect.width / imageWidth;
            float normalizedHeight = boundingBoxRect.height / imageHeight;

            Debug.Log(boundingBoxRect.ToString());


            GameObject sphereTest = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            float sphereRadius = 4.5f;
            sphereTest.gameObject.name = "Here";
            sphereTest.transform.position = Mercator(boundingBoxRect.x + (boundingBoxRect.width / 2), boundingBoxRect.y + (boundingBoxRect.height / 2), (int)imageWidth , (int)imageHeight) * sphereRadius;
            sphereTest.transform.position = new Vector3(sphereTest.transform.position.x, sphereTest.transform.position.y + 1.43f, sphereTest.transform.position.z);

            float theta = (normalizedX + normalizedWidth * 0.5f) * _horizontalFOV * Mathf.Deg2Rad - _horizontalFOV * 0.5f * Mathf.Deg2Rad;
            float phi = (normalizedY + normalizedHeight * 0.5f) * _verticalFOV * Mathf.Deg2Rad - _verticalFOV * 0.5f * Mathf.Deg2Rad;
            float xCoord = sphereRadius * Mathf.Cos(phi) * Mathf.Sin(theta);
            float yCoord = sphereRadius * Mathf.Sin(phi);
            float zCoord = sphereRadius * Mathf.Cos(phi) * Mathf.Cos(theta);

            //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //sphere.transform.localScale = new Vector3(sphereRadius * 2f, sphereRadius * 2f, sphereRadius * 2f);
            GameObject boundingBoxObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundingBoxObject.transform.position = sphereTest.transform.position;
            //boundingBoxObject.transform.LookAt(Camera.main.transform);
            //boundingBoxObject.transform.parent = sphere.transform;
            boundingBoxObject.transform.localScale = new Vector3(sphereRadius * normalizedWidth * 0.01f, sphereRadius * normalizedHeight * 0.01f, 0.01f) * 154;

            sphereTest.transform.LookAt(Camera.main.transform);

            boundingBoxObject.transform.rotation = sphereTest.transform.rotation;

        }
        */
        
    }
}



/*


using UnityEngine;

public class PanoramicBoundingBox : MonoBehaviour
{
    public Texture2D panoramicImage;
    public Material boundingBoxMaterial;

    private float horizontalFOV = 360f;
    private float verticalFOV = 180f;
    private float sphereRadius = 4.5f; // The sphere's diameter is 9 units, so the radius is half of that

    // Define the position and scale of the bounding box on the panoramic image (in pixel coordinates)
    public Rect boundingBoxRect = new Rect(4074f, 1504f, 229f, 331f);

    private GameObject boundingBoxObject;

    void Start()
    {
        // Create a sphere to display the panoramic image
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = new Vector3(sphereRadius * 2f, sphereRadius * 2f, sphereRadius * 2f);

        // Apply the panoramic image to the sphere
        sphere.GetComponent<Renderer>().material.mainTexture = panoramicImage;

        // Convert the bounding box's position to normalized coordinates (between 0 and 1)
        float normalizedX = boundingBoxRect.x / panoramicImage.width;
        float normalizedY = boundingBoxRect.y / panoramicImage.height;
        float normalizedWidth = boundingBoxRect.width / panoramicImage.width;
        float normalizedHeight = boundingBoxRect.height / panoramicImage.height;

        // Convert the normalized coordinates to spherical coordinates
        float theta = (normalizedX + normalizedWidth * 0.5f) * horizontalFOV * Mathf.Deg2Rad - horizontalFOV * 0.5f * Mathf.Deg2Rad;
        float phi = (normalizedY + normalizedHeight * 0.5f) * verticalFOV * Mathf.Deg2Rad - verticalFOV * 0.5f * Mathf.Deg2Rad;

        // Convert the spherical coordinates to cartesian coordinates within the sphere
        float xCoord = sphereRadius * Mathf.Sin(phi) * Mathf.Cos(theta);
        float yCoord = sphereRadius * Mathf.Cos(phi);
        float zCoord = sphereRadius * Mathf.Sin(phi) * Mathf.Sin(theta);

        // Create the bounding box object as a cube
        boundingBoxObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundingBoxObject.transform.parent = sphere.transform;
        boundingBoxObject.transform.localPosition = new Vector3(xCoord, yCoord, zCoord);
        boundingBoxObject.transform.localScale = new Vector3(sphereRadius * normalizedWidth, sphereRadius * normalizedHeight, 0.01f);

        // Apply custom material to the bounding box
        boundingBoxObject.GetComponent<Renderer>().material = boundingBoxMaterial;

        // Destroy the sphere used to display the panoramic image
        Destroy(sphere);
    }

    void Update()
    {
        // Continuously update the position and scale of the bounding box object inside the sphere

        float normalizedX = boundingBoxRect.x / panoramicImage.width;
        float normalizedY = boundingBoxRect.y / panoramicImage.height;
        float normalizedWidth = boundingBoxRect.width / panoramicImage.width;
        float normalizedHeight = boundingBoxRect.height / panoramicImage.height;

        // Convert the normalized coordinates to spherical coordinates
        float theta = (normalizedX + normalizedWidth * 0.5f) * horizontalFOV * Mathf.Deg2Rad - horizontalFOV * 0.5f * Mathf.Deg2Rad;
        float phi = (normalizedY + normalizedHeight * 0.5f) * verticalFOV * Mathf.Deg2Rad - verticalFOV * 0.5f * Mathf.Deg2Rad;

        // Convert the spherical coordinates to cartesian coordinates within the sphere
        float xCoord = sphereRadius * Mathf.Sin(phi) * Mathf.Cos(theta);
        float yCoord = sphereRadius * Mathf.Cos(phi);
        float zCoord = sphereRadius * Mathf.Sin(phi) * Mathf.Sin(theta);

        boundingBoxObject.transform.localPosition = new Vector3(xCoord, yCoord, zCoord);
        boundingBoxObject.transform.localScale = new Vector3(sphereRadius * normalizedWidth, sphereRadius * normalizedHeight, 0.01f);
    }
}
}*/