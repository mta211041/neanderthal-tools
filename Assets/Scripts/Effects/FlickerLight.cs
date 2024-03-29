using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Based on: https://gist.github.com/sinbad/4a9ded6b00cf6063c36a4837b15df969
namespace NeanderthalTools.Effects
{
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        #region Editor

        [Min(0f)]
        [SerializeField]
        [Tooltip("Minimum random light intensity")]
        private float minIntensity;

        [Min(0f)]
        [SerializeField]
        [Tooltip("Maximum random light intensity")]
        private float maxIntensity = 1f;

        [Range(1, 50)]
        [SerializeField]
        [Tooltip("How much to smooth out the randomness; lower values = sparks, higher = lantern")]
        private int smoothing = 5;

        [Min(0f)]
        [SerializeField]
        [Tooltip("Maximum random light movement amount")]
        private float movement = 0.1f;

        [Min(0f)]
        [SerializeField]
        private float movementSpeed = 1f;

        [Min(0f)]
        [SerializeField]
        [Tooltip("Minimum random move interval in seconds")]
        private float minMovementInterval = 0.5f;

        [Min(0f)]
        [SerializeField]
        [Tooltip("Maximum random move interval in seconds")]
        private float maxMovementInterval = 1f;

        #endregion

        #region Fields

        private Queue<float> smoothQueue;
        private float lastSum;
        private new Light light;

        private Vector3 originalPosition;
        private Vector3 targetPosition;
        private float nextMoveTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            originalPosition = transform.localPosition;
            smoothQueue = new Queue<float>(smoothing);
            light = GetComponent<Light>();
        }

        private void OnDisable()
        {
            smoothQueue.Clear();
            lastSum = 0;
        }

        private void Update()
        {
            UpdateIntensity();
            UpdateMovement();
        }

        #endregion

        #region Methods

        private void UpdateIntensity()
        {
            while (smoothQueue.Count >= smoothing)
            {
                lastSum -= smoothQueue.Dequeue();
            }

            // Generate random new item, calculate new average.
            var newVal = Random.Range(minIntensity, maxIntensity);
            smoothQueue.Enqueue(newVal);
            lastSum += newVal;

            // Calculate new smoothed average.
            light.intensity = lastSum / smoothQueue.Count;
        }

        private void UpdateMovement()
        {
            if (nextMoveTime <= Time.time)
            {
                targetPosition = GetMovementPosition();
                nextMoveTime = Random.Range(minMovementInterval, maxMovementInterval);
            }

            var lightTransform = transform;
            var localPosition = lightTransform.localPosition;

            localPosition = Vector3.Lerp(
                localPosition,
                targetPosition,
                Time.deltaTime * movementSpeed
            );

            lightTransform.localPosition = localPosition;
        }

        private Vector3 GetMovementPosition()
        {
            var position = new Vector3(
                GetMovement(),
                GetMovement(),
                GetMovement()
            );

            return originalPosition + position;
        }

        private float GetMovement()
        {
            var halfMovement = movement / 2;
            return Random.Range(-halfMovement, halfMovement);
        }

        #endregion
    }
}
