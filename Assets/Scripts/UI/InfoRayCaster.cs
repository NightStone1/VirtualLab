using UnityEngine;

public class InfoRaycaster : MonoBehaviour
{
    public float maxDistance = 5f;
    public float checkInterval = 0.05f;

    private Camera cam;
    private IInfoProvider currentProvider;
    private float timer;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer < checkInterval)
            return;

        timer = 0f;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            IInfoProvider provider = hit.collider.GetComponent<IInfoProvider>();

            if (provider != currentProvider)
            {
                if (provider != null)
                {
                    currentProvider = provider;
                    string info = provider.GetInfoText();
                    // floatingInfo.Show(hit.collider.transform, info);
                }
                else
                {
                    // floatingInfo.Hide();
                    currentProvider = null;
                }
            }
        }
        else if (currentProvider != null)
        {
            // floatingInfo.Hide();
            currentProvider = null;
        }
    }
}

