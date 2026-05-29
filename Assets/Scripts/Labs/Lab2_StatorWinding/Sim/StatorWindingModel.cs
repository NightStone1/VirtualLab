public static class StatorWindingModel
{
    public const int PhaseWindingCount = 3;

    public static bool TryCheckFirstSecondPhaseMarkingScheme(
        Lab2TerminalId jumperFirst,
        Lab2TerminalId jumperSecond,
        Lab2TerminalId supplyFirst,
        Lab2TerminalId supplySecond,
        Lab2TerminalId meterFirst,
        Lab2TerminalId meterSecond,
        out string meterReading)
    {
        meterReading = string.Empty;

        if (!IsPair(meterFirst, meterSecond, Lab2TerminalId.C3, Lab2TerminalId.C6))
            return false;

        if (IsPair(jumperFirst, jumperSecond, Lab2TerminalId.C1, Lab2TerminalId.C2)
            && IsPair(supplyFirst, supplySecond, Lab2TerminalId.C4, Lab2TerminalId.C5))
        {
            meterReading = "PV: 0 В, стрелка не отклоняется";
            return true;
        }

        if (IsPair(jumperFirst, jumperSecond, Lab2TerminalId.C1, Lab2TerminalId.C5)
            && IsPair(supplyFirst, supplySecond, Lab2TerminalId.C4, Lab2TerminalId.C2))
        {
            meterReading = "PV: стрелка отклоняется";
            return true;
        }

        return false;
    }

    public static bool TryCheckThirdPhaseMarkingScheme(
        Lab2TerminalId jumperFirst,
        Lab2TerminalId jumperSecond,
        Lab2TerminalId supplyFirst,
        Lab2TerminalId supplySecond,
        Lab2TerminalId meterFirst,
        Lab2TerminalId meterSecond,
        out string meterReading)
    {
        meterReading = string.Empty;

        if (!IsPair(meterFirst, meterSecond, Lab2TerminalId.C1, Lab2TerminalId.C4))
            return false;

        if (IsPair(jumperFirst, jumperSecond, Lab2TerminalId.C2, Lab2TerminalId.C3)
            && IsPair(supplyFirst, supplySecond, Lab2TerminalId.C5, Lab2TerminalId.C6))
        {
            meterReading = "PV: 0 В, стрелка не отклоняется";
            return true;
        }

        if (IsPair(jumperFirst, jumperSecond, Lab2TerminalId.C2, Lab2TerminalId.C6)
            && IsPair(supplyFirst, supplySecond, Lab2TerminalId.C5, Lab2TerminalId.C3))
        {
            meterReading = "PV: стрелка отклоняется";
            return true;
        }

        return false;
    }

    public static bool HasContinuity(Lab2TerminalId first, Lab2TerminalId second)
    {
        return TryGetPhasePair(first, second, out _, out _);
    }

    public static bool TryGetPhasePair(
        Lab2TerminalId first,
        Lab2TerminalId second,
        out Lab2TerminalId pairStart,
        out Lab2TerminalId pairEnd)
    {
        if (IsPair(first, second, Lab2TerminalId.C1, Lab2TerminalId.C4))
            return SetPair(Lab2TerminalId.C1, Lab2TerminalId.C4, out pairStart, out pairEnd);

        if (IsPair(first, second, Lab2TerminalId.C2, Lab2TerminalId.C5))
            return SetPair(Lab2TerminalId.C2, Lab2TerminalId.C5, out pairStart, out pairEnd);

        if (IsPair(first, second, Lab2TerminalId.C3, Lab2TerminalId.C6))
            return SetPair(Lab2TerminalId.C3, Lab2TerminalId.C6, out pairStart, out pairEnd);

        pairStart = Lab2TerminalId.None;
        pairEnd = Lab2TerminalId.None;
        return false;
    }

    private static bool IsPair(
        Lab2TerminalId first,
        Lab2TerminalId second,
        Lab2TerminalId expectedFirst,
        Lab2TerminalId expectedSecond)
    {
        if (first == Lab2TerminalId.None || second == Lab2TerminalId.None || first == second)
            return false;

        return (first == expectedFirst && second == expectedSecond)
            || (first == expectedSecond && second == expectedFirst);
    }

    private static bool SetPair(
        Lab2TerminalId first,
        Lab2TerminalId second,
        out Lab2TerminalId pairStart,
        out Lab2TerminalId pairEnd)
    {
        pairStart = first;
        pairEnd = second;
        return true;
    }
}
