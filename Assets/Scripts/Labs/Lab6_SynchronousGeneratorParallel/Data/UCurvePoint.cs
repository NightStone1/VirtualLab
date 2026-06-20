[System.Serializable]
public struct UCurvePoint
{
    public float If;
    public float Istat;
    public float Iactive;
    public float Ireactive;
    public float cosPhi;
    public float excitationCurrent;
    public float statorCurrent;
    public float activeCurrent;
    public float reactiveCurrent;
    public float powerFactor;
    public float loadPower;
    public float loadPercent;
    public UCurveSeries seriesType;
}
