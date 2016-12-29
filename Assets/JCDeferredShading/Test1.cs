using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JCDeferredShading;

public class Test1 : MonoBehaviour
{
    private class LightObject
    {
        public Light light = null;

        public float lifeTime = 0.0f;
    }

    public GameObject lightPrefab = null;

    private List<LightObject> workingLights = null;

    private List<LightObject> freeLights = null;

    private float time = 0.0f;

    private void Start()
    {
        workingLights = new List<LightObject>();
        freeLights = new List<LightObject>();
    }

    private void Update()
    {
        time += Time.deltaTime;
        if(time > 0.75f)
        {
            time = 0;

            LightObject lightObj = null;
            if(freeLights.Count == 0)
            {
                lightObj = new LightObject() { light = (GameObject.Instantiate(lightPrefab) as GameObject).GetComponent<Light>() };
            }
            else
            {
                lightObj = freeLights[freeLights.Count - 1];
                freeLights.RemoveAt(freeLights.Count - 1);
            }

            lightObj.lifeTime = 60.0f;
            workingLights.Add(lightObj);
            lightObj.light.intensity = 1;
            lightObj.light.color = new Color(Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f));
            lightObj.light.transform.position = transform.position;
            lightObj.light.gameObject.SetActive(true);
            lightObj.light.GetComponent<Rigidbody>().isKinematic = false;
            lightObj.light.GetComponent<Rigidbody>().AddForce(transform.up*1000, ForceMode.Acceleration);

            if(JCDSCamera.instance != null)
            {
                JCDSCamera.instance.CollectLights();
            }
        }

        int numLights = workingLights.Count;
        for(int i = 0; i < numLights; ++i)
        {
            LightObject lightObj = workingLights[i];
            lightObj.lifeTime -= Time.deltaTime;
            if(lightObj.lifeTime < 1.0f)
            {
                lightObj.light.intensity -= 0.5f * Time.deltaTime;
            }
            if(lightObj.lifeTime < 0.0f && lightObj.light.intensity <= 0)
            {
                lightObj.light.GetComponent<Rigidbody>().isKinematic = true;
                lightObj.light.gameObject.SetActive(false);
                workingLights.RemoveAt(i);
                freeLights.Add(lightObj);
                --numLights;
                --i;
            }
        }
    }
}
