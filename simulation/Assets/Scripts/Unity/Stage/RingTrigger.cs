using UnityEngine;

public class RingTrigger : MonoBehaviour
{
    // A flag to prevent multiple triggers in a single pass.
    private bool hasBeenTriggered = false;
    
    // Reference to all materials to change their color on trigger.
    private Material[] ringMaterials;

    void Start()
    {
        // Get all Renderer components and their materials.
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            ringMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                ringMaterials[i] = renderers[i].material;
            }
        }
    }

    // This function is called when another collider enters the trigger.
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered is the drone and if it hasn't been triggered yet.
        // It's good practice to tag your drone object as "Drone".
        Debug.Log("OnTriggerEnter: " + other.name + ", tag=" + other.tag);
        if (other.CompareTag("Drone") && !hasBeenTriggered)
        {
            hasBeenTriggered = true;

            // Log a message to the console.
            Debug.Log("Ring passed!");

            // (Optional) Change the color of all ring materials to green to give visual feedback.
            if (ringMaterials != null)
            {
                foreach (Material material in ringMaterials)
                {
                    if (material != null)
                    {
                        material.color = Color.green;
                        // For emissive materials
                        // material.SetColor("_EmissionColor", Color.green);
                    }
                }
            }

            // Here you can add code to increment a score, log the time, etc.
        }
    }
}
