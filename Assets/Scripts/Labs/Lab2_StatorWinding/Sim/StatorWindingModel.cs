public static class StatorWindingModel
{
    public const int TerminalCount = 6;
    public const int PhaseWindingCount = 3;
    public const int TrainingGalvanometerDeflections = 30;
    public const int TrainingRotorTurns = 10;
    public const int TrainingSupplyFrequency = 50;

    public static void CalculateTrainingRotationSpeed(out int polePairs, out int synchronousSpeed)
    {
        polePairs = TrainingGalvanometerDeflections / TrainingRotorTurns;
        synchronousSpeed = 60 * TrainingSupplyFrequency / polePairs;
    }

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

    public static bool IsStarConnectionScheme(
        Lab2TerminalId starJumper1First,
        Lab2TerminalId starJumper1Second,
        Lab2TerminalId starJumper2First,
        Lab2TerminalId starJumper2Second,
        Lab2TerminalId supplyLine1First,
        Lab2TerminalId supplyLine1Second,
        Lab2TerminalId supplyLine2First,
        Lab2TerminalId supplyLine2Second)
    {
        return ConnectsAllThree(
                starJumper1First,
                starJumper1Second,
                starJumper2First,
                starJumper2Second,
                Lab2TerminalId.C4,
                Lab2TerminalId.C5,
                Lab2TerminalId.C6)
            && ConnectsAllThree(
                supplyLine1First,
                supplyLine1Second,
                supplyLine2First,
                supplyLine2Second,
                Lab2TerminalId.C1,
                Lab2TerminalId.C2,
                Lab2TerminalId.C3);
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

    private static bool ConnectsAllThree(
        Lab2TerminalId firstA,
        Lab2TerminalId firstB,
        Lab2TerminalId secondA,
        Lab2TerminalId secondB,
        Lab2TerminalId expectedA,
        Lab2TerminalId expectedB,
        Lab2TerminalId expectedC)
    {
        if (!IsAllowed(firstA, expectedA, expectedB, expectedC)
            || !IsAllowed(firstB, expectedA, expectedB, expectedC)
            || !IsAllowed(secondA, expectedA, expectedB, expectedC)
            || !IsAllowed(secondB, expectedA, expectedB, expectedC))
            return false;

        return Contains(firstA, firstB, secondA, secondB, expectedA)
            && Contains(firstA, firstB, secondA, secondB, expectedB)
            && Contains(firstA, firstB, secondA, secondB, expectedC);
    }

    private static bool IsAllowed(
        Lab2TerminalId terminal,
        Lab2TerminalId expectedA,
        Lab2TerminalId expectedB,
        Lab2TerminalId expectedC)
    {
        return terminal == expectedA || terminal == expectedB || terminal == expectedC;
    }

    private static bool Contains(
        Lab2TerminalId firstA,
        Lab2TerminalId firstB,
        Lab2TerminalId secondA,
        Lab2TerminalId secondB,
        Lab2TerminalId expected)
    {
        return firstA == expected || firstB == expected || secondA == expected || secondB == expected;
    }
}
