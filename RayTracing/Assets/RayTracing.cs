using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracing : MonoBehaviour
{
    private Texture2D renderTexture;
    public bool rayTracing = false;
    public int renderResolution = 1;
    private Light[] lights;
    private LayerMask collisionMask = 1 << 31;

    //Create render texture with screen size

    void Start()
    {
        GenerateColliders();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (rayTracing)
                rayTracing = false;
            else
                rayTracing = true;
        }
        if (rayTracing)
        {
            renderTexture = new Texture2D(Screen.width * renderResolution, Screen.height * renderResolution);
            RayTrace();
        }   
    }


    void OnGUI()
    {
        if(rayTracing)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), renderTexture);
    }


    void RayTrace()
    {
        lights = FindObjectsOfType(typeof(Light)) as Light[];
        for (int x = 0; x < renderTexture.width; x++)
        {
            for (int y = 0; y < renderTexture.height; y++)
            {
                Ray ray = Camera.main.ScreenPointToRay(new Vector3(x/renderResolution, y/renderResolution, 0));
                renderTexture.SetPixel(x, y, TraceRay(ray));
            }
        }

        renderTexture.Apply();
    }

    Color TraceLight (Vector3 pos, Vector3 normal)
    {
        Color returnColor = RenderSettings.ambientLight;

        foreach (Light light in lights)
        {
            if (light.enabled)
                returnColor += LightTrace(light, pos, normal);
        }

        return returnColor;
    }

    Color LightTrace(Light light, Vector3 pos, Vector3 normal)
    {
        float dot;

        if (light.type == LightType.Directional)
        {
            dot = Vector3.Dot(-light.transform.forward, normal);
            if (dot > 0)
            {
                if (Physics.Raycast(pos, -light.transform.forward, Mathf.Infinity))
                    return Color.black;
            }
            return light.color * light.intensity * dot;
        }
        else
        {
            Vector3 direction = (light.transform.position - pos).normalized;
            dot = Vector3.Dot(normal, direction);
            float distance = Vector3.Distance(pos, light.transform.position);
            if (distance < light.range && dot > 0)
            {
                if (light.type == LightType.Point)
                {
                    if (Physics.Raycast(pos, direction, distance))
                        return Color.black;
                    else
                        return light.color * light.intensity * dot * (1 - light.range / distance);
                }
                else if (light.type == LightType.Spot)
                {
                    float dot2 = Vector3.Dot(-light.transform.forward, normal);
                    if (dot2 < (1-light.spotAngle/180))
                    {
                        if (Physics.Raycast(pos, direction, distance))
                            return Color.black;
                        else
                            return light.color*light.intensity*dot* (1 - light.range / distance) * ((dot2 / (1 - light.spotAngle / 180)));
                    }
                }
            }
            return Color.black;
        }
    }

    Color TraceRay(Ray ray)
    {
        RaycastHit hit;
        Color returnColor = Color.black;

        if (Physics.Raycast(ray, out hit))
        {
            Material mat;

            mat = hit.collider.GetComponent<Renderer>().material;
            if (mat.mainTexture)
            {
                returnColor += (mat.mainTexture as Texture2D).GetPixelBilinear(hit.textureCoord.x, hit.textureCoord.y);
            }
            else
               returnColor += mat.color;
        }
        returnColor *= TraceLight(hit.point + hit.normal * 0.0001f, hit.normal);
        return returnColor;
    }

    void GenerateColliders()
    {
        foreach (MeshFilter meshFilter in FindObjectsOfType(typeof(MeshFilter)) as MeshFilter[])
        {
            if (!meshFilter.GetComponent<MeshCollider>())
            {
                MeshCollider mesh = meshFilter.gameObject.AddComponent<MeshCollider>();
                mesh.convex = true;
                mesh.isTrigger = true;
            }
            else
            {
                MeshCollider mesh = meshFilter.GetComponent<MeshCollider>();
                mesh.convex = true;
                mesh.isTrigger = true;
            }
        }
    }
}
