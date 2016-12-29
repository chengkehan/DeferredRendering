using UnityEngine;
using System.Collections;

namespace JCDeferredShading
{
    public class JCDSRenderTexture
    {
        private RenderTexture[] rts = null;

        private RenderBuffer[] colorBuffers = null;

        private RenderBuffer[] depthBuffers = null;

        private int depthBuffersMask = 0;

        private bool isDestroyed = false;

        public int numRTs
        {
            get
            {
                CheckIsDestroyed();
                return rts == null ? 0 : rts.Length;
            }
        }

        public JCDSRenderTexture(int numRTs, int width, int height, int depthBuffersMask, RenderTextureFormat format, FilterMode filterMode, bool useMipMap)
        {
            CheckSize(width, height);

            if (numRTs <= 0)
            {
                throw new System.ArgumentException();
            }

            if(!SystemInfo.SupportsRenderTextureFormat(format))
            {
                throw new System.FormatException();
            }

            rts = new RenderTexture[numRTs];
            colorBuffers = new RenderBuffer[numRTs];
            depthBuffers = new RenderBuffer[numRTs];
            this.depthBuffersMask = depthBuffersMask;

            for(int i = 0; i < numRTs; ++i)
            {
                bool useDepth = MaskIsSet(depthBuffersMask, i);

                RenderTexture rt = new RenderTexture(width, height, useDepth ? 32 : 0, format);
                rt.generateMips = useMipMap;
                rt.useMipMap = useMipMap;
                rt.filterMode = filterMode;
                rt.Create();
                rts[i] = rt;

                colorBuffers[i] = rt.colorBuffer;
                
                if(useDepth)
                {
                    depthBuffers[i] = rt.depthBuffer;
                }
            }
        }

        public void Destroy()
        {
            if (rts != null)
            {
                int numRTs = rts.Length;
                for (int i = 0; i < numRTs; ++i)
                {
                    RenderTexture.Destroy(rts[i]);
                    rts[i] = null;
                }
                rts = null;

                colorBuffers = null;
                depthBuffers = null;
            }
            isDestroyed = true;
        }

        public void ResetSize(int width, int height)
        {
            CheckIsDestroyed();
            CheckSize(width, height);

            int numRTs = rts.Length;
            for(int i = 0; i < numRTs; ++i)
            {
                RenderTexture rt = rts[i];
                if(!rt.IsCreated() || rt.width != width || rt.height != height)
                {
                    bool useDepth = MaskIsSet(depthBuffersMask, i);

                    rt.Release();
                    rt.width = width;
                    rt.height = height;
                    rt.Create();

                    colorBuffers[i] = rt.colorBuffer;

                    if(useDepth)
                    {
                        depthBuffers[i] = rt.depthBuffer;
                    }
                }
            }
        }

        public static void SetMultipleRenderTargets(Camera camera, JCDSRenderTexture colorBuffers, JCDSRenderTexture depthBuffer, int depthBufferIndex)
        {
            if(camera == null || colorBuffers == null || depthBuffer == null)
            {
                throw new System.ArgumentNullException();
            }

            camera.SetTargetBuffers(colorBuffers.GetColorBuffers(), depthBuffer.GetDepthBuffer(depthBufferIndex));
        }

        public void SetActiveRenderTexture(int index)
        {
            CheckIsDestroyed();
            CheckIndex(index);

            RenderTexture rt = GetRenderTexture(index);
            Graphics.SetRenderTarget(rt);
        }

        public static void ResetActiveRenderTexture()
        {
            Graphics.SetRenderTarget(null);
        }

        public static void ClearActiveRenderTexture(bool clearDepth, bool clearColor, Color backgroundColor, float defaultDepthValue)
        {
            GL.Clear(clearDepth, clearColor, backgroundColor, defaultDepthValue);
        }

        public RenderTexture[] GetRenderTextures()
        {
            CheckIsDestroyed();
            return rts;
        }

        public RenderBuffer[] GetColorBuffers()
        {
            CheckIsDestroyed();
            return colorBuffers;
        }

        public RenderBuffer[] GetDepthBuffers()
        {
            CheckIsDestroyed();
            return depthBuffers;
        }

        public RenderTexture GetRenderTexture(int index)
        {
            CheckIsDestroyed();
            CheckIndex(index);

            return rts[index];
        }

        public RenderBuffer GetColorBuffer(int index)
        {
            CheckIsDestroyed();
            CheckIndex(index);

            return colorBuffers[index];
        }

        public RenderBuffer GetDepthBuffer(int index)
        {
            CheckIsDestroyed();

            if (!MaskIsSet(depthBuffersMask, index))
            {
                throw new System.ArgumentException();
            }

            return depthBuffers[index];
        }

        private void CheckSize(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new System.ArgumentException();
            }
        }

        private void CheckIndex(int index)
        {
            if (index < 0 || index >= rts.Length)
            {
                throw new System.IndexOutOfRangeException();
            }
        }

        private void CheckIsDestroyed()
        {
            if(isDestroyed)
            {
                throw new System.InvalidOperationException();
            }
        }

        public static bool MaskIsSet(int mask, int index)
        {
            return ((mask >> index) & 1) == 1;
        }

        public static int ValueToMask(bool[] values)
        {
            int mask = 0;
            if (values == null)
            {
                return mask;
            }
            for (int i = 0; i < values.Length; ++i)
            {
                if (values[i])
                {
                    mask |= 1 << i;
                }
            }
            return mask;
        }
    }
}
