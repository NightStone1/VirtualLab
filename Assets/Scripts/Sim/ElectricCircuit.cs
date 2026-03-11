using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
using TMPro;

public class ElectricCircuit : MonoBehaviour
{
    public TextMeshProUGUI Infotext;
    private Vector3 offEuler = new Vector3(-180f, 0f, -49f);  // углы для выключенного состояния
    private Vector3 offEuler1 = new Vector3(0, -90, 90f);  // углы для выключенного состояния
    private Vector3 onEuler_Pv1, onEuler_Pv2, onEuler_Pa1, onEuler_Pa2, onEuler_Pa3, onEuler_Pa4;   // углы для включенного состояния
    private float U_Pv1, U_Pv2, A_Pa1, A_Pa2, A_Pa3, A_Pa4, R1_value, R2_value, R3_value, LLR_value, RPM, RPM1;
    public Slider R1, R2;
    public Rotator R3, LLR;
    public Switch Q1, Q2, Q3;
    public Meter Pv1, Pv2, Pa1, Pa2, Pa3, Pa4;
    public Meter info_Pv1, info_Pv2, info_Pa1, info_Pa2, info_Pa3, info_Pa4;
    public Motor Motor;
    bool engineIsOn = false;
    public float rotationSpeed = 2f;
    public float testRotation= 2f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        R1.OnValueChanged += R1ValueChange;
        R2.OnValueChanged += R2ValueChange;
        R3.OnValueChanged += R3_OnValueChanged;
        Q1.OnValueChanged += Q1_OnValueChanged;
        Q2.OnValueChanged += Q2_OnValueChanged; 
        Q3.OnValueChanged += Q3_OnValueChanged;
        LLR.OnValueChanged += LLR_OnValueChanged;
        GameManager.Instance.SetState(GameState.Playing);
        UpdateInfoText();
    }
    void UpdateInfoText()
    {
        Infotext.text =
        $"Pv1: {info_Pv1.current:F2} Вольт\n" +
        $"Pv2: {info_Pv2.current:F2} Вольт\n" +
        $"Pa1: {info_Pa1.current:F2} Ампер\n" +
        $"Pa2: {info_Pa2.current:F2} мАмпер\n" +
        $"Pa3: {info_Pa3.current:F2} мАмпер\n" +
        $"Pa4: {info_Pa4.current:F2} Ампер\n" +
        $"Количество оборотов: {RPM:F0}";
    }
    private void CheckEngine()
    {
        if (LLR_value > 50f && Q1.isOn && Q2.isOn)
        {
            engineIsOn = true;

        }
        else
        {
            engineIsOn = false;
        }
    }
    private void Q1_OnValueChanged(bool value)
    {
        //if (Q1.isOn) 
        //{
        //    CheckEngine();
        //    UpdateAllAngles();            
        //}
        CheckEngine();
        UpdateAllAngles();
        SetAllMeters();
        Debug.Log("Q1: " + value);
    }
    private void Q2_OnValueChanged(bool value)
    {
        //if (Q1.isOn && Q2.isOn)
        //{
        //    CheckEngine();
        //    UpdateAllAngles();
        //}
        CheckEngine();
        UpdateAllAngles();
        SetAllMeters();
        Debug.Log("Q2: " + value);
    }
    private void Q3_OnValueChanged(bool value)
    {
        //if (Q1.isOn && Q2.isOn && Q3.isOn && engineIsOn)
        //{
        //    UpdateAllAngles();
        //}
        UpdateAllAngles();
        SetAllMeters();
        Debug.Log("Q3: " + value);
    }
    void SetAllMeters()
    {
        StartCoroutine(RotateMeter(Q1.isOn, onEuler_Pv1, Pv1, 250f));
        StartCoroutine(RotateMeter(Q1.isOn && Q2.isOn && engineIsOn, onEuler_Pv2, Pv2, 250f));
        StartCoroutine(RotateMeter(Q1.isOn && Q2.isOn && engineIsOn, onEuler_Pa1, Pa1, 5f));
        StartCoroutine(RotateMeter(Q1.isOn && Q2.isOn && engineIsOn, onEuler_Pa2, Pa2, 250f));
        StartCoroutine(RotateMeter(Q1.isOn && Q2.isOn && engineIsOn, onEuler_Pa3, Pa3, 250f));
        StartCoroutine(RotateMeter(Q1.isOn && Q2.isOn && Q3.isOn && engineIsOn, onEuler_Pa4, Pa4, 5f));        
    }
    private void UpdateAllAngles()
    {

        CoeffCalculation.Simulate(Q1.isOn, Q2.isOn, Q3.isOn, engineIsOn, U_Pv1, R1_value, R2_value, R3_value, out A_Pa1, out A_Pa2, out A_Pa3, out A_Pa4, out U_Pv2, out RPM);
        Motor.TargetRPM = RPM;
        info_Pa1.current = A_Pa1;
        info_Pa2.current = A_Pa2*1000;
        info_Pa3.current = A_Pa3*1000;
        info_Pa4.current = A_Pa4;
        info_Pv1.current = U_Pv1;
        info_Pv2.current = U_Pv2;
        UpdateInfoText();
        Debug.Log("Сопротивление R1 " + R1_value + " % " + "Сопротивление R2 " + R2_value + " % " + "Сопротивление R3 " + R3_value + " % " + "Pv1 " + U_Pv1 + " Pv2 " + U_Pv2 + " Pa1 " + A_Pa1 + " Pa2 " + A_Pa2 + " Pa3 " + A_Pa3 + " Pa4 " + A_Pa4 + " RPM " + RPM);
        float angle_Pv1 = Mathf.Lerp(-49f, -131f, U_Pv1 / 250f);
        onEuler_Pv1 = new Vector3(-180f, 0f, angle_Pv1);
        float angle_Pv2 = Mathf.Lerp(-49f, -131f, U_Pv2 / 250f);
        onEuler_Pv2 = new Vector3(-180f, 0f, angle_Pv2);
        float angle_Pa1 = Mathf.Lerp(-49f, -131f, A_Pa1 / 5f);
        onEuler_Pa1 = new Vector3(-180f, 0f, angle_Pa1);
        float angle_Pa2 = Mathf.Lerp(-49f, -131f, A_Pa2 * 1000 / 250f);
        onEuler_Pa2 = new Vector3(-180f, 0f, angle_Pa2);
        float angle_Pa3 = Mathf.Lerp(-49f, -131f, A_Pa3 * 1000 / 250f);
        onEuler_Pa3 = new Vector3(-180f, 0f, angle_Pa3);
        float angle_Pa4 = Mathf.Lerp(-49f, -131f, A_Pa4 / 5f);
        onEuler_Pa4 = new Vector3(-180f, 0f, angle_Pa4);
    }    
    void R1ValueChange(float value)
    {

        R1_value = value;
        UpdateAllAngles();
        SetAllMeters();
        Debug.Log("Сопротивление R1 " + R1_value + " %");
    }

    void R2ValueChange(float value)
    {

        R2_value = value;
        UpdateAllAngles();
        SetAllMeters();       
        Debug.Log("Сопротивление R2 " + R2_value + " %");
    }
    private void R3_OnValueChanged(float value)
    {

        R3_value = value;
        UpdateAllAngles();
        SetAllMeters();
        Debug.Log("Сопротивление R3 " + R3_value + " %");
    }
    private void LLR_OnValueChanged(float value)
    {        
        U_Pv1 = Mathf.Clamp(value, 0f, 250f);
        LLR_value = U_Pv1;
        CheckEngine();
        UpdateAllAngles();
        SetAllMeters();
        Debug.Log("LLR " + LLR_value);
        //float Umeter = Mathf.Lerp(-49f, -131f, U_Pv1 / 100f);
        //if (!Q1.isOn) return;
        //Pv1.transform.localRotation = Quaternion.Euler(-180f, 0f, Umeter);
        //Debug.Log(U_Pv1 + " " + Umeter);
    }
    IEnumerator RotateMeter(bool toOn, Vector3 onEuler, Meter meter, float max_value)
    {
        Quaternion startRot = meter.transform.localRotation;
        Quaternion endRot = Quaternion.Euler(toOn ? onEuler : offEuler);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;
            meter.transform.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        meter.transform.localRotation = endRot;
    }
}
