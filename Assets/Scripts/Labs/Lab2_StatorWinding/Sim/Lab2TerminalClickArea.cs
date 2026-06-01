using UnityEngine;

public class Lab2TerminalClickArea : MonoBehaviour
{
    [SerializeField] private Lab2Terminal terminal;

    public void Initialize(Lab2Terminal owner)
    {
        terminal = owner;
    }

    private void Awake()
    {
        if (terminal == null)
            terminal = GetComponentInParent<Lab2Terminal>();
    }

    private void OnMouseDown()
    {
        if (terminal != null)
            terminal.HandleClick();
    }
}
