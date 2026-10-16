using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class cameraController : MonoBehaviour
{
    public float standardPan = -5.4f;
    public float tilt = 4.5f;

    public float rotationSpeed = 3f;

    private CinemachineCamera vcam;
    private CinemachinePanTilt panTilt;
    public float targetPan;

    void Start()
    {
        vcam = GetComponent<CinemachineCamera>();
        panTilt = GetComponent<CinemachinePanTilt>();
        targetPan = standardPan;

        if (panTilt == null)
        {
            Debug.LogError("No se encontró el componente CinemachinePanTilt en este GameObject.");
        }
        //else
        //{
        //    // Inicializamos los valores
        //    panTilt.PanAxis.Value = standardPan;
        //    panTilt.TiltAxis.Value = tilt;
        //}

    }

    // Update is called once per frame
    void Update()
    {
        float rotation = GameMaster.Instance.cameraRotation;    
        if (rotation != 0f)
        {
            rotate(rotation);
            StartCoroutine(RotateRoutine());
            GameMaster.Instance.cameraRotation = 0f;
        }
        //panTilt.PanAxis.Value = Mathf.Lerp(panTilt.PanAxis.Value, targetPan, Time.deltaTime * rotationSpeed);
    }

    public void rotate(float grades)
    {
        targetPan = targetPan + grades;
        
    }

    IEnumerator RotateRoutine()
    {
        float startPan = panTilt.PanAxis.Value;
        float elapsed = 0f;
        float duration = 0.4f;  // Duración rotación
        float durationTwo = 0.2f;
        float durationTree = 0.1f;
        float targetPanOne = targetPan + (targetPan - startPan) * 0.2f;  
        float targetPanTwo = targetPan - (targetPan - startPan) * 0.1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            panTilt.PanAxis.Value = Mathf.LerpAngle(startPan, targetPanOne, t * t);  // Ease-out
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < durationTwo)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / durationTwo;
            panTilt.PanAxis.Value = Mathf.LerpAngle(targetPanOne, targetPanTwo, t * t);  // Ease-out
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < durationTree)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / durationTree;
            panTilt.PanAxis.Value = Mathf.LerpAngle(targetPanTwo, targetPan, t * t);  // Ease-out
            yield return null;
        }
        panTilt.PanAxis.Value = targetPan;
    }

}
