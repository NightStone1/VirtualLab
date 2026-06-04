using UnityEngine;

public class SyncGeneratorModel
{
    public float gridVoltage = 380f;
    public float gridFrequency = 50f;
    public float rotorSpeedRpm;
    public float generatorFrequency;
    public float excitationCurrent;
    public float generatorVoltage;
    public float phaseDifferenceDeg;
    public float loadPower;
    public float statorCurrent;
    public float powerFactor;
    public bool isPowered;
    public bool isPrimeMoverRunning;
    public bool excitationEnabled;
    public bool isConnectedToGrid;
    public bool hasFault;
    public string faultReason;

    public void Reset()
    {
        gridVoltage = 380f;
        gridFrequency = 50f;
        rotorSpeedRpm = 0f;
        generatorFrequency = 0f;
        excitationCurrent = 0f;
        generatorVoltage = 0f;
        phaseDifferenceDeg = 0f;
        loadPower = 0f;
        statorCurrent = 0f;
        powerFactor = 0f;
        isPowered = false;
        isPrimeMoverRunning = false;
        excitationEnabled = false;
        isConnectedToGrid = false;
        hasFault = false;
        faultReason = string.Empty;
    }

    public void Update(float deltaTime)
    {
        if (isConnectedToGrid)
        {
            generatorFrequency = gridFrequency;
            generatorVoltage = gridVoltage;
            UpdateLoadValues();
            return;
        }

        generatorFrequency = isPrimeMoverRunning ? rotorSpeedRpm / 60f : 0f;
        generatorVoltage = excitationEnabled
            ? gridVoltage * excitationCurrent * Mathf.Clamp01(generatorFrequency / gridFrequency)
            : 0f;

        phaseDifferenceDeg = Mathf.Repeat(
            phaseDifferenceDeg + (generatorFrequency - gridFrequency) * 360f * deltaTime,
            360f);

        statorCurrent = 0f;
        powerFactor = 0f;
    }

    public void SetPower(bool enabled)
    {
        isPowered = enabled;

        if (!enabled)
        {
            isPrimeMoverRunning = false;
            excitationEnabled = false;
            isConnectedToGrid = false;
            rotorSpeedRpm = 0f;
            generatorFrequency = 0f;
            excitationCurrent = 0f;
            generatorVoltage = 0f;
            loadPower = 0f;
            statorCurrent = 0f;
            powerFactor = 0f;
        }
    }

    public void StartPrimeMover()
    {
        if (!isPowered || isConnectedToGrid)
        {
            return;
        }

        isPrimeMoverRunning = true;
        rotorSpeedRpm = Mathf.Max(rotorSpeedRpm, 2700f);
    }

    public void StopPrimeMover()
    {
        isPrimeMoverRunning = false;
        isConnectedToGrid = false;
        rotorSpeedRpm = 0f;
        generatorFrequency = 0f;
        loadPower = 0f;
        statorCurrent = 0f;
        powerFactor = 0f;
    }

    public void IncreaseSpeed(float delta)
    {
        if (!isPowered || !isPrimeMoverRunning || isConnectedToGrid)
        {
            return;
        }

        rotorSpeedRpm = Mathf.Clamp(rotorSpeedRpm + Mathf.Abs(delta), 0f, 3300f);
    }

    public void DecreaseSpeed(float delta)
    {
        if (!isPowered || !isPrimeMoverRunning || isConnectedToGrid)
        {
            return;
        }

        rotorSpeedRpm = Mathf.Clamp(rotorSpeedRpm - Mathf.Abs(delta), 0f, 3300f);
    }

    public void IncreaseExcitation(float delta)
    {
        if (!isPowered)
        {
            return;
        }

        excitationEnabled = true;
        excitationCurrent = Mathf.Clamp(excitationCurrent + Mathf.Abs(delta), 0f, 1.5f);
    }

    public void DecreaseExcitation(float delta)
    {
        if (!isPowered)
        {
            return;
        }

        excitationCurrent = Mathf.Clamp(excitationCurrent - Mathf.Abs(delta), 0f, 1.5f);
        excitationEnabled = excitationCurrent > 0f;
    }

    public bool TryConnectToGrid(out string message)
    {
        if (!isPowered)
        {
            message = "Синхронизация невозможна: питание отключено.";
            return false;
        }

        if (!isPrimeMoverRunning)
        {
            message = "Синхронизация невозможна: приводной двигатель остановлен.";
            return false;
        }

        if (!excitationEnabled)
        {
            message = "Синхронизация невозможна: возбуждение отключено.";
            return false;
        }

        float voltageError = Mathf.Abs(generatorVoltage - gridVoltage);
        float frequencyError = Mathf.Abs(generatorFrequency - gridFrequency);
        bool phaseMatched = phaseDifferenceDeg <= 10f || phaseDifferenceDeg >= 350f;

        if (voltageError <= 20f && frequencyError <= 0.5f && phaseMatched)
        {
            isConnectedToGrid = true;
            generatorFrequency = gridFrequency;
            generatorVoltage = gridVoltage;
            phaseDifferenceDeg = 0f;
            message = "Синхронизация успешна. Генератор подключен к сети.";
            return true;
        }

        message = string.Format(
            "Синхронизация не выполнена: dU={0:0.0} В, df={1:0.00} Гц, фаза={2:0.0}°.",
            voltageError,
            frequencyError,
            phaseDifferenceDeg);
        return false;
    }

    public void SetLoadPower(float normalized)
    {
        if (!isConnectedToGrid)
        {
            loadPower = 0f;
            statorCurrent = 0f;
            powerFactor = 0f;
            return;
        }

        loadPower = Mathf.Clamp01(normalized);
        UpdateLoadValues();
    }

    public UCurvePoint CalculateUCurvePoint(float excitation, float loadPower)
    {
        float clampedLoad = Mathf.Clamp01(loadPower);
        float clampedExcitation = Mathf.Clamp(excitation, 0f, 1.5f);
        float optimalExcitation = 1f + clampedLoad * 0.15f;
        float activeCurrent = clampedLoad * 1.2f;
        float reactiveCurrent = (clampedExcitation - optimalExcitation) * 0.9f;
        float statorCurrent = Mathf.Sqrt(activeCurrent * activeCurrent + reactiveCurrent * reactiveCurrent);
        float powerFactor = statorCurrent > 0.001f ? Mathf.Clamp01(activeCurrent / statorCurrent) : 0f;

        return new UCurvePoint
        {
            excitationCurrent = clampedExcitation,
            statorCurrent = statorCurrent,
            activeCurrent = activeCurrent,
            reactiveCurrent = reactiveCurrent,
            powerFactor = powerFactor,
            loadPower = clampedLoad
        };
    }

    private void UpdateLoadValues()
    {
        UCurvePoint point = CalculateUCurvePoint(excitationCurrent, loadPower);
        statorCurrent = point.statorCurrent;
        powerFactor = point.powerFactor;
    }
}
