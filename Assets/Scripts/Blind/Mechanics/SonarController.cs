using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Shaders;
using Unity.VisualScripting; // Your namespace for the EdgeDetection feature

[RequireComponent(typeof(Rigidbody))]
public class SonarController : MonoBehaviour
{
    [Header("Effect Settings")]
    [Tooltip("The maximum radius the sonar wave will expand to.")]
    [SerializeField] private float maxRange = 20f;

    [Tooltip("How many seconds it takes for the sonar wave to expand to its maximum range.")]
    [SerializeField] private float expandDuration = 1.0f;

    [Tooltip("How many seconds it takes for the sonar wave to shrink back to the origin.")]
    [SerializeField] private float shrinkDuration = 2.0f;

    [Tooltip("The color of the expanding and shrinking sonar wave.")]
    [SerializeField, ColorUsage(true, true)]
    private Color sonarColor = Color.blue;

    [Tooltip("Controls the 'slow to fast' easing of the shrink effect. 1 = linear, 2 = quadratic, 3 = cubic.")]
    [SerializeField, Range(1f, 5f)]
    private float shrinkEasingPower = 2.0f;

    [Header("Dependencies")]
    [Tooltip("The Edge Detection render feature. Will be found automatically if left empty.")]
    [SerializeField] private EdgeDetection edgeDetectionFeature;
    [SerializeField] private ScriptableRendererData rendererData;
    
    // To keep track of the currently running effect
    private Coroutine _activeSonarCoroutine;

    void Start()
    {
        // Automatically find the EdgeDetection feature if it's not assigned in the inspector.
        if (edgeDetectionFeature == null)
        {
            // Get the current URP asset
            var urpAsset = UniversalRenderPipeline.asset as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                Debug.LogError("Current Render Pipeline is not a Universal Render Pipeline or the asset is missing.", this);
                this.enabled = false;
                return;
            }

            // Get the ScriptableRendererData from the URP asset.
            // This assumes you are using the default renderer (index 0).
            // If you use multiple renderers, you might need a more complex way to find the right one.

            // The 'rendererFeatures' list on the ScriptableRendererData asset IS public.
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature is EdgeDetection detection)
                {
                    edgeDetectionFeature = detection;
                    break;
                }
            }
        }
        
        if (edgeDetectionFeature == null)
        {
            Debug.LogError("EdgeDetection feature not found on the active URP Renderer Data asset. SonarController will be disabled.", this);
            this.enabled = false;
        }
        else
        {
            // Start with the effect off.
            edgeDetectionFeature.settings.boundaryRange = 0f;
            edgeDetectionFeature.settings.boundaryColor = Color.black;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Don't trigger the sonar if the player catches the ball.
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("PlayerHand"))
        {
            return;
        }

        // Use the first contact point as the precise origin of the sonar wave.
        Vector3 impactPoint = collision.contacts[0].point;
        
        TriggerSonar(impactPoint);
    }
    
    /// <summary>
    /// Starts the sonar animation sequence at a specific world-space position.
    /// </summary>
    /// <param name="position">The origin point for the sonar wave.</param>
    public void TriggerSonar(Vector3 position)
    {
        if (edgeDetectionFeature == null || !this.enabled) return;

        // If another sonar effect is already running, stop it before starting a new one.
        // This handles rapid firing of sonar balls.
        if (_activeSonarCoroutine != null)
        {
            StopCoroutine(_activeSonarCoroutine);
        }

        // Start the new sonar effect coroutine.
        _activeSonarCoroutine = StartCoroutine(SonarEffectCoroutine(position));
    }

    /// <summary>
    /// A coroutine that animates the sonar effect through its expand, fade, and shrink phases.
    /// </summary>
    private IEnumerator SonarEffectCoroutine(Vector3 position)
    {
        // --- SETUP ---
        // Let the EdgeDetection render feature know where the effect should originate.
        edgeDetectionFeature.settings.boundaryOrigin = position;

        // --- PHASE 1: EXPAND ---
        float timer = 0f;
        while (timer < expandDuration)
        {
            // 't' is a value from 0 to 1 representing the progress of the expansion.
            float t = timer / expandDuration;

            // Linearly interpolate the range from 0 to its maximum value.
            edgeDetectionFeature.settings.boundaryRange = Mathf.Lerp(0f, maxRange, t);
            
            // Set the color to its full initial value.
            edgeDetectionFeature.settings.boundaryColor = sonarColor;

            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        // Ensure the range is exactly at maxRange at the end of the phase.
        edgeDetectionFeature.settings.boundaryRange = maxRange;


        // --- PHASE 2: SHRINK & FADE ---
        timer = 0f; // Reset the timer for the shrink phase.
        while (timer < shrinkDuration)
        {
            // 't' is a value from 0 to 1 representing the progress of the shrink.
            float t = timer / shrinkDuration;
            
            // Apply an easing curve to make the shrink start slow and get faster.
            float easedT = Mathf.Pow(t, shrinkEasingPower);

            // Interpolate the range from max back down to 0 using the eased progress.
            edgeDetectionFeature.settings.boundaryRange = Mathf.Lerp(maxRange, 0f, easedT);
            
            // Simultaneously, fade the color from the initial sonar color to black.
            // This fade happens over a constant 0.5 seconds, but we'll cap it at shrinkDuration if it's shorter.
            float fadeDuration = 0.5f;
            float colorT = Mathf.Clamp01(timer / fadeDuration);
            edgeDetectionFeature.settings.boundaryColor = Color.Lerp(sonarColor, Color.black, colorT);

            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // --- CLEANUP ---
        // Ensure the effect is fully off once the coroutine is complete.
        edgeDetectionFeature.settings.boundaryRange = 0f;
        edgeDetectionFeature.settings.boundaryColor = Color.black;
        _activeSonarCoroutine = null; // Mark that no coroutine is running.
    }
}