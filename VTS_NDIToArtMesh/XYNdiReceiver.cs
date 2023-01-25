using System;
using Klak.Ndi;
using UnityEngine;
using Klak.Ndi.Interop;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VTS_NDIToArtMesh
{
    public sealed class XYNdiReceiver : MonoBehaviour
    {
        public bool HFlip, VFlip;

        private void PrepareReceiverObjects()
        {
            if (this._recv == null)
            {
                this._recv = RecvHelper.TryCreateRecv(this.ndiName);
            }
            if (this._converter == null)
            {
                this._converter = new FormatConverter(this._resources);
            }
            if (this._override == null)
            {
                this._override = new MaterialPropertyBlock();
            }
        }

        private void ReleaseReceiverObjects()
        {
            Recv recv = this._recv;
            if (recv != null)
            {
                recv.Dispose();
            }
            this._recv = null;
            FormatConverter converter = this._converter;
            if (converter != null)
            {
                converter.Dispose();
            }
            this._converter = null;
        }

        private RenderTexture TryReceiveFrame()
        {
            this.PrepareReceiverObjects();
            if (this._recv == null)
            {
                return null;
            }
            VideoFrame? videoFrame = RecvHelper.TryCaptureVideoFrame(this._recv);
            if (videoFrame == null)
            {
                return null;
            }
            VideoFrame value = videoFrame.Value;
            RenderTexture renderTexture = this._converter.Decode(value.Width, value.Height, Util.HasAlpha(value.FourCC), value.Data);
            if (value.Metadata != IntPtr.Zero)
            {
                this.metadata = Marshal.PtrToStringAnsi(value.Metadata);
            }
            else
            {
                this.metadata = null;
            }
            this._recv.FreeVideoFrame(value);
            return renderTexture;
        }

        internal void Restart()
        {
            this.ReleaseReceiverObjects();
        }

        private void OnDisable()
        {
            this.ReleaseReceiverObjects();
        }

        private void Update()
        {
            RenderTexture renderTexture = this.TryReceiveFrame();
            if (renderTexture == null)
            {
                return;
            }
            if (this.targetRenderers != null && this.targetRenderers.Count > 0)
            {
                if (HFlip && VFlip)
                {
                    HVFlipRenderTexture(renderTexture);
                }
                else if (HFlip)
                {
                    HFlipRenderTexture(renderTexture);
                }
                else if (VFlip)
                {
                    VFlipRenderTexture(renderTexture);
                }
                foreach (var r in this.targetRenderers)
                {
                    r.GetPropertyBlock(this._override);
                    this._override.SetTexture(this.targetMaterialProperty, renderTexture);
                    r.SetPropertyBlock(this._override);
                }
            }
            if (this.targetTexture != null)
            {
                Graphics.Blit(renderTexture, this.targetTexture);
            }
        }

        public static void VFlipRenderTexture(RenderTexture target)
        {
            var temp = RenderTexture.GetTemporary(target.descriptor);
            Graphics.Blit(target, temp, new Vector2(1, -1), new Vector2(0, 1));
            Graphics.Blit(temp, target);
            RenderTexture.ReleaseTemporary(temp);
        }

        public static void HFlipRenderTexture(RenderTexture target)
        {
            var temp = RenderTexture.GetTemporary(target.descriptor);
            Graphics.Blit(target, temp, new Vector2(-1, 1), new Vector2(1, 0));
            Graphics.Blit(temp, target);
            RenderTexture.ReleaseTemporary(temp);
        }

        public static void HVFlipRenderTexture(RenderTexture target)
        {
            var temp = RenderTexture.GetTemporary(target.descriptor);
            Graphics.Blit(target, temp, new Vector2(-1, -1), new Vector2(1, 1));
            Graphics.Blit(temp, target);
            RenderTexture.ReleaseTemporary(temp);
        }

        public string ndiName
        {
            get
            {
                return this._ndiNameRuntime;
            }
            set
            {
                this.SetNdiName(value);
            }
        }

        private void SetNdiName(string name)
        {
            if (this._ndiNameRuntime == name)
            {
                return;
            }
            this._ndiNameRuntime = name;
            this._ndiName = name;
            this.Restart();
        }

        public RenderTexture targetTexture
        {
            get
            {
                return this._targetTexture;
            }
            set
            {
                this._targetTexture = value;
            }
        }

        public List<Renderer> targetRenderers
        {
            get
            {
                return this._targetRenderers;
            }
            set
            {
                this._targetRenderers = value;
            }
        }

        public string targetMaterialProperty
        {
            get
            {
                return this._targetMaterialProperty;
            }
            set
            {
                this._targetMaterialProperty = value;
            }
        }

        public RenderTexture texture
        {
            get
            {
                FormatConverter converter = this._converter;
                if (converter == null)
                {
                    return null;
                }
                return converter.LastDecoderOutput;
            }
        }

        public string metadata { get; set; }

        public Recv internalRecvObject
        {
            get
            {
                return this._recv;
            }
        }

        public void SetResources(NdiResources resources)
        {
            this._resources = resources;
        }

        private void Awake()
        {
            this.ndiName = this._ndiName;
        }

        private Recv _recv;

        private FormatConverter _converter;

        private MaterialPropertyBlock _override;

        [SerializeField]
        private string _ndiName;

        private string _ndiNameRuntime;

        [SerializeField]
        private RenderTexture _targetTexture;

        [SerializeField]
        private List<Renderer> _targetRenderers;

        [SerializeField]
        private string _targetMaterialProperty;

        [SerializeField]
        [HideInInspector]
        private NdiResources _resources;
    }
}