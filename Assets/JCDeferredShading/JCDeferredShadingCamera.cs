using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class JCDeferredShadingCamera : MonoBehaviour
{
    private static JCDeferredShadingCamera s_instance = null;
    public static JCDeferredShadingCamera instance
    {
        get
        {
            return s_instance;
        }
    }

    public bool debug = false;

    public Mesh pointLightMesh = null;

    private Camera cam = null;

    // 1 : diffuse(rgb) shininess(a)
    // 2 : normal(rgb)
    // 3 : position(rgb)
    // 4 : result (rgb)
    private RenderTexture[] rts = new RenderTexture[4];

    private RenderBuffer[] colorBuffers = null;

    private RenderBuffer depthBuffer;

    private Material compositeResultBufferMtrl = null;

    private int shaderPropId_diffuseBuffer = 0;
    private int shaderPropId_normalBuffer = 0;
    private int shaderPropId_positionBuffer = 0;
    private int shaderPropId_resultBuffer = 0;

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
        if(!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
        {
            Debug.LogError("Unsupported RenderTexture Format ARGBHalf");
            Destroy(this);
            return;
        }

        s_instance = this;

        cam = GetComponent<Camera>();

        compositeResultBufferMtrl = new Material(Shader.Find("Hidden/JCDeferredShading/CompositeResultBuffer"));

        shaderPropId_diffuseBuffer = Shader.PropertyToID("_DiffuseBuffer");
        shaderPropId_normalBuffer = Shader.PropertyToID("_NormalBuffer");
        shaderPropId_positionBuffer = Shader.PropertyToID("_PositionBuffer");
        shaderPropId_resultBuffer = Shader.PropertyToID("_ResultBuffer");

        shaderPropId_dirLightDir = Shader.PropertyToID("_DirLightDir");
        shaderPropId_dirLightColor = Shader.PropertyToID("_DirLightColor");

        shaderPropId_pointLightPos = Shader.PropertyToID("_PointLightPos");
        shaderPropId_pointLightColor = Shader.PropertyToID("_PointLightColor");
        shaderPropId_pointLightRange = Shader.PropertyToID("_PointLightRange");

        CollectLights();
    }

    private void OnDestroy()
    {
        if (rts != null)
        {
            int numRTs = rts.Length;
            for(int i = 0; i < numRTs; ++i)
            {
                Destroy(rts[i]);
                rts[i] = null;
            }
            rts = null;
            colorBuffers = null;
        }

        s_instance = null;
    }

    private void OnPreRender()
    {
        SetupRenderTextures();

        cam.SetTargetBuffers(colorBuffers, depthBuffer);
    }

    private void OnPostRender()
    {
        Graphics.SetRenderTarget(colorBuffers[3], depthBuffer);

        compositeResultBufferMtrl.SetTexture(shaderPropId_diffuseBuffer, rts[0]);
        compositeResultBufferMtrl.SetTexture(shaderPropId_normalBuffer, rts[1]);
        compositeResultBufferMtrl.SetTexture(shaderPropId_positionBuffer, rts[2]);
        compositeResultBufferMtrl.SetTexture(shaderPropId_resultBuffer, rts[3]);

        int numDirLights = dirLights == null ? 0 : dirLights.Length;
        for(int i = 0; i < numDirLights; ++i)
        {
            Light light = dirLights[i];
            if(light != null)
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
            if (light != null)
            {
                compositeResultBufferMtrl.SetVector(shaderPropId_pointLightPos, light.transform.position);
                compositeResultBufferMtrl.SetColor(shaderPropId_pointLightColor, light.color);
                compositeResultBufferMtrl.SetVector(shaderPropId_pointLightRange, new Vector4(1.0f / light.range, light.intensity, 0, 0));
                compositeResultBufferMtrl.SetPass(2);
                Graphics.DrawMeshNow(pointLightMesh, Matrix4x4.TRS(light.transform.position, Quaternion.identity, Vector3.one * light.range * 2));
            }
        }

        Graphics.SetRenderTarget(null);
        DrawScreenQuad(compositeResultBufferMtrl, 3, true, true);
    }

    private void OnGUI()
    {
        if(debug)
        {
            int width = (int)(Screen.width * 0.25f);
            int height = (int)(Screen.height * 0.25f);
            int numRTs = rts.Length;
            Rect rect = new Rect(0, 0, width, height);
            for(int i = 0; i < numRTs; ++i)
            {
                GUI.DrawTexture(rect, rts[i], ScaleMode.ScaleToFit, false);
                rect.y += height;
            }
        }
    }

    public void CollectLights()
    {
        dirLights = Light.GetLights(LightType.Directional, 0);
        pointLights = Light.GetLights(LightType.Point, 0);
    }

    private void SetupRenderTextures()
    {
        int numRTs = rts.Length;

        if(colorBuffers == null)
        {
            colorBuffers = new RenderBuffer[numRTs];
        }

        for(int i = 0; i < numRTs; ++i)
        {
            if(rts[i] == null)
            {
                RenderTexture rt = new RenderTexture(Screen.width, Screen.height, i == 0 ? 32 : 0, RenderTextureFormat.ARGBHalf);
                rt.generateMips = false;
                rt.useMipMap = false;
                rt.filterMode = FilterMode.Point;
                rt.Create();
                rts[i] = rt;

                colorBuffers[i] = rt.colorBuffer;
                if(i == 0)
                {
                    depthBuffer = rt.depthBuffer;
                }
            }
            else
            {
                RenderTexture rt = rts[i];
                if(!rt.IsCreated() || rt.width != Screen.width || rt.height != Screen.height)
                {
                    rt.Release();
                    rt.width = Screen.width;
                    rt.height = Screen.height;
                    rt.generateMips = false;
                    rt.useMipMap = false;
                    rt.filterMode = FilterMode.Point;
                    rt.Create();

                    colorBuffers[i] = rt.colorBuffer;
                    if(i == 0)
                    {
                        depthBuffer = rt.depthBuffer;
                    }
                }
            }
        }
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
