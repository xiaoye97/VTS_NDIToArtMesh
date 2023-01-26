using System;
using BepInEx;
using System.IO;
using HarmonyLib;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace VTS_NDIToArtMesh
{
    [BepInPlugin(GUID, PluginName, VERSION)]
    public class NDIToArtMesh : BaseUnityPlugin
    {
        public const string GUID = "me.xiaoye97.plugin.VTubeStudio.NDIToArtMesh";
        public const string PluginName = "NDIToArtMesh";
        public const string VERSION = "1.0.0";

        private void Start()
        {
            Harmony.CreateAndPatchAll(typeof(NDIToArtMesh));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(VTubeStudioModel), "Start")]
        public static void VTubeStudioModel_Start_Patch(VTubeStudioModel __instance)
        {
            string path = __instance.ModelJSON.FilePath.Replace(".vtube.json", ".NDIToArtMesh.json");
            FileInfo file = new FileInfo(path);
            // 如果配置文件存在则尝试读取
            if (file.Exists)
            {
                string json = File.ReadAllText(file.FullName);
                try
                {
                    var config = JsonConvert.DeserializeObject<NDIToArtMeshConfig>(json);
                    if (config.ArtMeshNames == null || config.ArtMeshNames.Count == 0)
                    {
                        Debug.LogWarning($"NDIToArtMesh 模型:{__instance.ModelJSON.Name} 的NDIToArtMesh配置文件中没有目标ArtMesh，不进行NDI接收");
                        return;
                    }
                    List<Renderer> renderers = new List<Renderer>();
                    foreach (var d in __instance.Live2DModel.Drawables)
                    {
                        // ID相同，找到目标
                        if (config.ArtMeshNames.Contains(d.Id))
                        {
                            var renderer = d.GetComponent<Renderer>();
                            renderers.Add(renderer);
                        }
                    }
                    if (renderers.Count > 0)
                    {
                        var ndi = __instance.GetComponent<XYNdiReceiver>();
                        if (ndi == null)
                        {
                            ndi = __instance.gameObject.AddComponent<XYNdiReceiver>();
                        }
                        ndi.ndiName = config.NDIName;
                        ndi.HFlip = config.HorizontalFlip;
                        ndi.VFlip = config.VerticalFlip;
                        ndi.targetMaterialProperty = "_MainTex";
                        ndi.targetRenderers = renderers;
                        Debug.Log($"NDIToArtMesh在模型:{__instance.ModelJSON.Name} 的{renderers.Count}/{config.ArtMeshNames.Count}个ArtMesh上开启了NDI接收");
                    }
                    else
                    {
                        Debug.Log($"NDIToArtMesh 模型:{__instance.ModelJSON.Name} 上没有目标的ArtMesh，不进行NDI接收");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"NDIToArtMesh解析配置文件异常 {ex}");
                }
            }
        }
    }
}