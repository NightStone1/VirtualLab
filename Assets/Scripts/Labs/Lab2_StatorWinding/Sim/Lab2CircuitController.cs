using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Lab2CircuitController : MonoBehaviour
{
    [SerializeField] private Lab2Terminal[] terminals;
    [SerializeField] private TMP_Text resultText;

    private readonly List<Lab2Terminal> selectedTerminals = new();

    private void Start()
    {
        if (terminals == null || terminals.Length == 0)
            terminals = FindObjectsOfType<Lab2Terminal>();

        ClearSelection();
        SetResult("Выберите две клеммы");
    }

    public void SelectTerminal(Lab2Terminal terminal)
    {
        if (terminal == null)
            return;

        if (selectedTerminals.Contains(terminal))
        {
            selectedTerminals.Remove(terminal);
            terminal.SetSelected(false);
            SetResult("Выберите две клеммы");
            return;
        }

        if (selectedTerminals.Count >= 2)
            ClearSelection();

        selectedTerminals.Add(terminal);
        terminal.SetSelected(true);

        if (selectedTerminals.Count == 2)
            CheckContinuity();
        else
            SetResult($"Выбрана клемма {terminal.TerminalId}. Выберите вторую клемму");
    }

    private void CheckContinuity()
    {
        Lab2TerminalId first = selectedTerminals[0].TerminalId;
        Lab2TerminalId second = selectedTerminals[1].TerminalId;

        bool hasContinuity = StatorWindingModel.HasContinuity(first, second);
        string result = hasContinuity ? "Цепь есть" : "Обрыв";

        SetResult($"{first} - {second}: {result}");
    }

    private void ClearSelection()
    {
        for (int i = 0; i < selectedTerminals.Count; i++)
        {
            if (selectedTerminals[i] != null)
                selectedTerminals[i].SetSelected(false);
        }

        selectedTerminals.Clear();
    }

    private void SetResult(string message)
    {
        Debug.Log($"Lab2 continuity: {message}");

        if (resultText != null)
            resultText.text = message;
    }
}
