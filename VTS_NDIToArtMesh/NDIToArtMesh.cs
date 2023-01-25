using System;
using BepInEx;
using Klak.Ndi;
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

        private VTubeStudioModelLoader modelLoader;
        private VTubeStudioModel nowModel;
        private ModelDefinitionJSON nowModelDef;
        private string nowControlModel = "";

        private NDIToArtMeshConfig nowConfig;
        private XYNdiReceiver ndi;
        private bool searched; // 是否查找过
        private NdiResources ndiResources;

        private void Init()
        {
            modelLoader = GameObject.FindObjectOfType<VTubeStudioModelLoader>();
            if (modelLoader == null)
            {
                Debug.LogError("未找到模型加载器");
                return;
            }
            modelLoader.modelLoadStarted.AddListener(OnModelLoadStarted);
            modelLoader.modelLoadingFinished.AddListener(OnModelLoadingFinished);
            // 创建NDI
            ndi = gameObject.AddComponent<XYNdiReceiver>();
        }

        private void Start()
        {
            Init();
        }

        private void Update()
        {
            // 如果模型为空，则清空配置
            if (nowModel == null)
            {
                nowConfig = null;
                nowControlModel = "";
                ndi.targetRenderers = null;
                ndi.ndiName = "";
                ndi.enabled = false;
                searched = false;
                return;
            }
            // 如果当前模型不为空但是配置为空，则创建配置
            if (nowConfig == null || nowControlModel != nowModelDef.Name)
            {
                nowControlModel = nowModelDef.Name;
                LoadConfig();
            }
            // 如果有配置并且NDI是关闭状态，则根据配置查找ArtMesh并开启NDI
            if (nowConfig != null && !ndi.enabled && !searched)
            {
                searched = true;
                if (nowConfig.ArtMeshNames == null || nowConfig.ArtMeshNames.Count == 0)
                {
                    Debug.LogWarning($"NDIToArtMesh 模型:{nowControlModel} 上没有需要NDI接收的ArtMesh，忽略此功能");
                    return;
                }
                int count = 0;
                FindNdiResource();
                ndi.ndiName = nowConfig.NDIName;
                ndi.HFlip = nowConfig.HorizontalFlip;
                ndi.VFlip = nowConfig.VerticalFlip;
                ndi.targetMaterialProperty = "_MainTex";
                ndi.targetRenderers = new List<Renderer>();
                foreach (var d in nowModel.Live2DModel.Drawables)
                {
                    // ID相同，找到目标
                    if (nowConfig.ArtMeshNames.Contains(d.Id))
                    {
                        var renderer = d.GetComponent<Renderer>();
                        ndi.targetRenderers.Add(renderer);
                        count++;
                        Debug.Log($"NDIToArtMesh在模型:{nowControlModel} 的ArtMesh:{d.Id} 上开启了NDI接收，NDIName:{ndi.ndiName}");
                    }
                }
                ndi.enabled = true;
                Debug.Log($"NDIToArtMesh在模型:{nowControlModel} 的{count}/{nowConfig.ArtMeshNames.Count}个ArtMesh上开启了NDI接收");
            }
        }

        public void LoadConfig()
        {
            string path = nowModelDef.FilePath.Replace(".vtube.json", ".NDIToArtMesh.json");
            FileInfo file = new FileInfo(path);
            if (file.Exists)
            {
                string json = File.ReadAllText(file.FullName);
                try
                {
                    var con = JsonConvert.DeserializeObject<NDIToArtMeshConfig>(json);
                    nowConfig = con;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"NDIToArtMesh解析配置文件异常 {ex}");
                }
            }
            else
            {
                //NDIToArtMeshConfig config = new NDIToArtMeshConfig();
                //config.NDIName = "XYMAINPC (OBS)";
                //config.ArtMeshNames = new List<string>() { "Square2048" };
                //nowConfig = config;
                //var json = JsonConvert.SerializeObject(nowConfig, Formatting.Indented);
                //FileHelper.WriteAllText(path, json);
            }
        }

        public void FindNdiResource()
        {
            if (ndiResources == null)
            {
                var ndiSender = GameObject.Find("Live2D Camera").GetComponent<NdiSender>();
                ndiResources = Traverse.Create(ndiSender).Field("_resources").GetValue<NdiResources>();
                ndi.SetResources(ndiResources);
                Traverse.Create(ndi).Field("_converter").SetValue(null);
            }
        }

        private void OnModelLoadStarted(ModelDefinitionJSON modelDef)
        {
            nowModel = null;
            nowModelDef = modelDef;
        }

        private void OnModelLoadingFinished(VTubeStudioModel model)
        {
            nowModel = model;
        }
    }
}