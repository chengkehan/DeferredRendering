using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

namespace JCDeferredShading
{
    [RequireComponent(typeof(Camera))]
    public class JCDSCamera : MonoBehaviour
    {
        private static JCDSCamera s_instance = null;
        public static JCDSCamera instance
        {
            get
            {
                return s_instance;
            }
        }

        public bool debug = false;

        public Mesh pointLightMesh = null;

        private Camera cam = null;
        private Camera doubleFaceDepthCam = null;

        // 0 : diffuse(rgb) shininess(a)
        // 1 : normal(rgb)
        // 2 : position(rgb)
        private JCDSRenderTexture mrtGBuffer = null;

        // 0 : result (rgb) depthBuffer
        private JCDSRenderTexture resultRT = null;

        // 0 : ssr (rgb)
        private JCDSRenderTexture ssrRT = null;

        // 0 : fonrt face depth(r) back face depth(g)
        private JCDSRenderTexture doubleFaceDepthRT = null;

        private Material compositeResultBufferMtrl = null;
        private Material ssrMtrl = null;

        private Shader frontFaceDepthShader = null;
        private Shader backFaceDepthShader = null;

        private int shaderPropId_diffuseBuffer = 0;
        private int shaderPropId_normalBuffer = 0;
        private int shaderPropId_positionBuffer = 0;
        private int shaderPropId_resultBuffer = 0;
        private int shaderPropId_ssrBuffer = 0;
        private int shaderPropId_doubleFaceDepthBuffer = 0;

        private int shaderPropId_ssrVPMatrix = 0;

        private int shaderPropId_dirLightDir = 0;
        private int shaderPropId_dirLightColor = 0;
        private int shaderPropId_dirLightIntensity = 0;

        private int shaderPropId_pointLightPos = 0;
        private int shaderPropId_pointLightColor = 0;
        private int shaderPropId_pointLightRange = 0;

        private Light[] dirLights = null;
        private Light[] pointLights = null;

        private void Awake()
        {
            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
            {
                Debug.LogError("Unsupported RenderTexture Format ARGBHalf");
                Destroy(this);
                return;
            }

            s_instance = this;

            cam = GetComponent<Camera>();

            compositeResultBufferMtrl = new Material(Shader.Find("Hidden/JCDeferredShading/CompositeResultBuffer"));
            ssrMtrl = new Material(Shader.Find("Hidden/JCDeferredShading/ScreenSpaceReflection"));

            frontFaceDepthShader = Shader.Find("Hidden/JCDeferredShading/FrontFaceDepth");
            backFaceDepthShader = Shader.Find("Hidden/JCDeferredShading/BackFaceDepth");

            shaderPropId_diffuseBuffer = Shader.PropertyToID("_DiffuseBuffer");
            shaderPropId_normalBuffer = Shader.PropertyToID("_NormalBuffer");
            shaderPropId_positionBuffer = Shader.PropertyToID("_PositionBuffer");
            shaderPropId_resultBuffer = Shader.PropertyToID("_ResultBuffer");
            shaderPropId_ssrBuffer = Shader.PropertyToID("_SSRBuffer");
            shaderPropId_doubleFaceDepthBuffer = Shader.PropertyToID("_DoubleFaceDepthBuffer");

            shaderPropId_ssrVPMatrix = Shader.PropertyToID("_SSR_VP_MATRIX");

            shaderPropId_dirLightDir = Shader.PropertyToID("_DirLightDir");
            shaderPropId_dirLightColor = Shader.PropertyToID("_DirLightColor");

            shaderPropId_pointLightPos = Shader.PropertyToID("_PointLightPos");
            shaderPropId_pointLightColor = Shader.PropertyToID("_PointLightColor");
            shaderPropId_pointLightRange = Shader.PropertyToID("_PointLightRange");

            mrtGBuffer = new JCDSRenderTexture(
                3, Screen.width, Screen.height,
                JCDSRenderTexture.ValueToMask(null), 
                RenderTextureFormat.ARGBHalf, FilterMode.Point, false
            );

            resultRT = new JCDSRenderTexture(
                1, Screen.width, Screen.height,
                JCDSRenderTexture.ValueToMask(new bool[] { true }), 
                RenderTextureFormat.ARGB32, FilterMode.Point, false    
            );

            ssrRT = new JCDSRenderTexture(
                1, Screen.width, Screen.height,
                JCDSRenderTexture.ValueToMask(null), 
                RenderTextureFormat.ARGB32, FilterMode.Point, false
            );

            doubleFaceDepthRT = new JCDSRenderTexture(
                1, Screen.width, Screen.height, 
                JCDSRenderTexture.ValueToMask(new bool[] { true }),
                RenderTextureFormat.ARGBHalf, FilterMode.Point, false 
            );

            CollectLights();

            CreateDoubleFaceDepthCamera();
        }

        private void OnDestroy()
        {
            if (mrtGBuffer != null)
            {
                mrtGBuffer.Destroy();
                mrtGBuffer = null;
            }
            if(resultRT != null)
            {
                resultRT.Destroy();
                resultRT = null;
            }
            if(ssrRT != null)
            {
                ssrRT.Destroy();
                ssrRT = null;
            }
            if(doubleFaceDepthRT != null)
            {
                doubleFaceDepthRT.Destroy();
                doubleFaceDepthRT = null;
            }

            if(compositeResultBufferMtrl != null)
            {
                Material.Destroy(compositeResultBufferMtrl);
                compositeResultBufferMtrl = null;
            }

            if(ssrMtrl != null)
            {
                Material.Destroy(ssrMtrl);
                ssrMtrl = null;
            }

            frontFaceDepthShader = null;
            backFaceDepthShader = null;

            s_instance = null;
        }

        private void OnPreRender()
        {
            mrtGBuffer.ResetSize(Screen.width, Screen.height);
            resultRT.ResetSize(Screen.width, Screen.height);
            ssrRT.ResetSize(Screen.width, Screen.height);
            doubleFaceDepthRT.ResetSize(Screen.width, Screen.height);

            JCDSRenderTexture.SetMultipleRenderTargets(cam, mrtGBuffer, resultRT, 0);
        }

        private void OnPostRender()
        {
            doubleFaceDepthRT.SetActiveRenderTexture(0);
            JCDSRenderTexture.ClearActiveRenderTexture(true, true, Color.black, 1.0f);
            JCDSRenderTexture.SetMultipleRenderTargets(doubleFaceDepthCam, doubleFaceDepthRT, doubleFaceDepthRT, 0);
            doubleFaceDepthCam.RenderWithShader(frontFaceDepthShader, null);
            JCDSRenderTexture.ClearActiveRenderTexture(true, false, Color.black, 1.0f);
            doubleFaceDepthCam.RenderWithShader(backFaceDepthShader, null);

            resultRT.SetActiveRenderTexture(0);
            JCDSRenderTexture.ClearActiveRenderTexture(true, true, Color.black, 0.0f);

            compositeResultBufferMtrl.SetTexture(shaderPropId_diffuseBuffer, mrtGBuffer.GetRenderTexture(0));
            compositeResultBufferMtrl.SetTexture(shaderPropId_normalBuffer, mrtGBuffer.GetRenderTexture(1));
            compositeResultBufferMtrl.SetTexture(shaderPropId_positionBuffer, mrtGBuffer.GetRenderTexture(2));
            compositeResultBufferMtrl.SetTexture(shaderPropId_resultBuffer, resultRT.GetRenderTexture(0));
            compositeResultBufferMtrl.SetTexture(shaderPropId_ssrBuffer, ssrRT.GetRenderTexture(0));

            int numDirLights = dirLights == null ? 0 : dirLights.Length;
            for (int i = 0; i < numDirLights; ++i)
            {
                Light light = dirLights[i];
                if (light != null && light.enabled && light.gameObject.activeSelf)
                {
                    Vector3 dir = -light.transform.forward;
                    compositeResultBufferMtrl.SetVector(shaderPropId_dirLightDir, new Vector4(dir.x, dir.y, dir.z, light.intensity));
                    compositeResultBufferMtrl.SetColor(shaderPropId_dirLightColor, light.color);
                    DrawScreenQuad(compositeResultBufferMtrl, 0, false, false);
                }
            }

            int numPointLights = pointLights == null ? 0 : pointLights.Length;
            for (int i = 0; i < numPointLights; ++i)
            {
                Light light = pointLights[i];
                if (light != null && light.enabled && light.gameObject.activeSelf)
                {
                    compositeResultBufferMtrl.SetVector(shaderPropId_pointLightPos, light.transform.position);
                    compositeResultBufferMtrl.SetColor(shaderPropId_pointLightColor, light.color);
                    compositeResultBufferMtrl.SetVector(shaderPropId_pointLightRange, new Vector4(1.0f / light.range, light.intensity, 0, 0));
                    compositeResultBufferMtrl.SetPass(2);
                    Graphics.DrawMeshNow(pointLightMesh, Matrix4x4.TRS(light.transform.position, Quaternion.identity, Vector3.one * light.range * 2));
                }
            }

            ssrMtrl.SetTexture(shaderPropId_diffuseBuffer, mrtGBuffer.GetRenderTexture(0));
            ssrMtrl.SetTexture(shaderPropId_normalBuffer, mrtGBuffer.GetRenderTexture(1));
            ssrMtrl.SetTexture(shaderPropId_positionBuffer, mrtGBuffer.GetRenderTexture(2));
            ssrMtrl.SetTexture(shaderPropId_resultBuffer, resultRT.GetRenderTexture(0));
            ssrMtrl.SetTexture(shaderPropId_doubleFaceDepthBuffer, doubleFaceDepthRT.GetRenderTexture(0));
            ssrMtrl.SetMatrix(shaderPropId_ssrVPMatrix, cam.projectionMatrix * cam.worldToCameraMatrix);
            ssrRT.SetActiveRenderTexture(0);
            JCDSRenderTexture.ClearActiveRenderTexture(true, true, Color.black, 1.0f);
            DrawScreenQuad(ssrMtrl, 0, false, false);

            JCDSRenderTexture.ResetActiveRenderTexture();
            DrawScreenQuad(compositeResultBufferMtrl, 3, true, true);
        }

        private void OnGUI()
        {
            if (debug)
            {
                int width = Screen.width / 4;
                int height = Screen.height / 4;
                Rect rect = new Rect(0, 0, width, height);
                OnGUI_DrawRTs(mrtGBuffer, ref rect, true);
                OnGUI_DrawRTs(ssrRT, ref rect, false);
                OnGUI_DrawRTs(resultRT, ref rect, true);
                OnGUI_DrawRTs(doubleFaceDepthRT, ref rect, false);
            }
        }

        private void OnGUI_DrawRTs(JCDSRenderTexture rt, ref Rect rect, bool isNewColumn)
        {
            int numRTs = rt.numRTs;
            for (int i = 0; i < numRTs; ++i)
            {
                GUI.DrawTexture(rect, rt.GetRenderTexture(i), ScaleMode.ScaleToFit, false);
                rect.y += rect.height;
            }
            if (isNewColumn)
            {
                rect.x += rect.width;
                rect.y = 0;
            }
        }

        private void CreateDoubleFaceDepthCamera()
        {
            GameObject go = new GameObject("DoubleFaceDepthCameraGo");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.parent = cam.transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localEulerAngles = Vector3.zero;
            go.transform.localScale = Vector3.one;

            doubleFaceDepthCam = go.AddComponent<Camera>();
            doubleFaceDepthCam.enabled = false;
            doubleFaceDepthCam.cullingMask = cam.cullingMask;
            doubleFaceDepthCam.fieldOfView = cam.fieldOfView;
            doubleFaceDepthCam.orthographic = cam.orthographic;
            doubleFaceDepthCam.nearClipPlane = cam.nearClipPlane;
            doubleFaceDepthCam.farClipPlane = cam.farClipPlane;
            doubleFaceDepthCam.rect = cam.rect;
            doubleFaceDepthCam.clearFlags = CameraClearFlags.Nothing;
        }

        public void CollectLights()
        {
            dirLights = Light.GetLights(LightType.Directional, 0);
            pointLights = Light.GetLights(LightType.Point, 0);
        }

        private void DrawScreenQuad(Material mtrl, int pass, bool isScreen, bool clearScreen)
        {
            GraphicsDeviceType type = SystemInfo.graphicsDeviceType;
            bool isOpenGLLike =
                type == GraphicsDeviceType.OpenGL2 ||
                type == GraphicsDeviceType.OpenGLCore ||
                type == GraphicsDeviceType.OpenGLES2 ||
                type == GraphicsDeviceType.OpenGLES3;

            bool isUvUpsideDown = isOpenGLLike || isScreen;

            if (clearScreen)
            {
                GL.Clear(true, true, Color.black);
            }
            mtrl.SetPass(isUvUpsideDown ? pass : (pass + 1));
            GL.PushMatrix();
            GL.Begin(GL.QUADS);
            GL.TexCoord2(0, 0);
            GL.Vertex3(-1, -1, 0);
            GL.TexCoord2(0, 1);
            GL.Vertex3(-1, 1, 0);
            GL.TexCoord2(1, 1);
            GL.Vertex3(1, 1, 0);
            GL.TexCoord2(1, 0);
            GL.Vertex3(1, -1, 0);
            GL.End();
            GL.PopMatrix();
        }
    }
}
