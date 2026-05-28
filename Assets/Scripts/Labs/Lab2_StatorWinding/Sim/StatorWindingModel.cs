public static class StatorWindingModel
{
    public static bool HasContinuity(Lab2TerminalId first, Lab2TerminalId second)
    {
        if (first == Lab2TerminalId.None || second == Lab2TerminalId.None || first == second)
            return false;

        return IsPair(first, second, Lab2TerminalId.C1, Lab2TerminalId.C4)
            || IsPair(first, second, Lab2TerminalId.C2, Lab2TerminalId.C5)
            || IsPair(first, second, Lab2TerminalId.C3, Lab2TerminalId.C6);
    }

    private static bool IsPair(
        Lab2TerminalId first,
        Lab2TerminalId second,
        Lab2TerminalId expectedFirst,
        Lab2TerminalId expectedSecond)
    {
        return (first == expectedFirst && second == expectedSecond)
            || (first == expectedSecond && second == expectedFirst);
    }
}
