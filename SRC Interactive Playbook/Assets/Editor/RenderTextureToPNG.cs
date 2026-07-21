using UnityEngine;
using UnityEditor;
using System.IO;

public class RenderTextureToPNG
{
    [MenuItem("Assets/Save RenderTexture to PNG")]
    public static void SaveRTToFile()
    {
        // Get the selected RenderTexture in the Project window
        RenderTexture rt = Selection.activeObject as RenderTexture;
        
        if (rt == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a RenderTexture in the Project window first.", "OK");
            return;
        }

        // Temporarily set the active RenderTexture
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        // Create a new Texture2D and read the RenderTexture pixels into it
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        // Restore the active RenderTexture
        RenderTexture.active = currentRT;

        // Encode the texture to a PNG
        byte[] bytes = tex.EncodeToPNG();
        
        // Destroy the temporary texture to free up memory
        Object.DestroyImmediate(tex);

        // Ask the user where to save the PNG
        string path = EditorUtility.SaveFilePanel("Save as PNG", "Assets", rt.name + ".png", "png");
        
        if (path.Length != 0)
        {
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            Debug.Log("Saved image to: " + path);
        }
    }

    // This ensures the menu item only lights up if a RenderTexture is selected
    [MenuItem("Assets/Save RenderTexture to PNG", true)]
    public static bool SaveRTToFileValidation()
    {
        return Selection.activeObject is RenderTexture;
    }
}