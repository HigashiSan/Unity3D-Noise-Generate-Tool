using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

public class NoiseGenTool : EditorWindow
{
    [MenuItem("Tools/Noise Tool")]
    static void CreateWindow()
    {
        Rect size = new Rect(0, 0, 400, 600);
        NoiseGenTool window = (NoiseGenTool)EditorWindow.GetWindowWithRect(typeof(NoiseGenTool), size, true);
        window.Show();
    }

    public enum NoiseType { Worley, Perlin, Simplex};
    public enum TextureSize { x64 = 64, x128 = 128, x256 = 256, x512 = 512, x1024 = 1024, x2048 = 2048 };

    private ComputeShader noiseShader;
    private NoiseType noiseType = NoiseType.Perlin;
    private RenderTextureFormat format = RenderTextureFormat.ARGB32;
    private TextureSize size = TextureSize.x512;
    private float noiseScale = 10f;

    RenderTexture renderTexture;
    int kernel;
    Texture2D texture;

    private string CSMainName = "CSNoise";
    private string SavePath = "Assets/noise.png";

    private void OnGUI()
    {
        noiseShader = EditorGUILayout.ObjectField("Compute Noise Shader:", noiseShader, typeof(ComputeShader), true) as ComputeShader;
        noiseType = (NoiseType)EditorGUILayout.EnumPopup("Noise Type£º", noiseType);
        format = (RenderTextureFormat)EditorGUILayout.EnumPopup("Texture Format£º", format);
        size = (TextureSize)EditorGUILayout.EnumPopup("Texture Size£º", size);
        noiseScale = EditorGUILayout.Slider("Noise Scale:", noiseScale, 20f, 200f);

        if (GUILayout.Button("Generate Noise Texture"))
        {
            if (noiseShader == null)
            {
                ShowNotification(new GUIContent("Noise Shader is null!!"));
            }
            else
            {
                Generate();
            }
        }

        if (renderTexture!=null)
        {
            GUI.DrawTexture(new Rect(5, 180, 390, 390), renderTexture);
        }

        if (GUILayout.Button("Save Texture"))
        {
            if (renderTexture == null)
            {
                ShowNotification(new GUIContent("Texture is null!!"));
            }
            else
            {
                SaveTexture();
                AssetDatabase.Refresh();
                ShowNotification(new GUIContent("Successfully saved!!"));
            }
        }
    }

    private void Generate()
    {
        renderTexture = CreateRT((int)size);
        kernel = noiseShader.FindKernel(CSMainName);
        noiseShader.SetTexture(kernel, "Texture", renderTexture);
        noiseShader.SetInt("size", (int)size);
        noiseShader.SetFloat("noiseScale", noiseScale);
        noiseShader.SetInt("Type", (int)noiseType);

        noiseShader.Dispatch(kernel, (int)size / 8, (int)size / 8, 1);
    }

    void SaveTexture()
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        texture = new Texture2D(renderTexture.width, renderTexture.height);
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = previous;

        byte[] bytes = texture.EncodeToTGA();
        File.WriteAllBytes(SavePath, bytes);
    }

    private RenderTexture CreateRT(int size)
    {
        RenderTexture renderTexture = new RenderTexture(size, size, 0, format);
        renderTexture.enableRandomWrite = true;
        renderTexture.wrapMode = TextureWrapMode.Repeat;
        renderTexture.Create();
        return renderTexture;
    }


    private void OnDisable()
    {
        if (renderTexture == null) return;
        renderTexture.Release();
    }
}
