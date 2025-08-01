using System;
using AI;
using UnityEngine;

namespace Blind.Mechanics
{
    public class SonarBallBehaviour : MonoBehaviour
    {
        [SerializeField] private Material sonarBallMaterial;

        private bool _sonarStarted = false;
        private float _waveFalloff = 0f;
        private float _waveFalloffOpacity = 0f;
        private float _waveOutlineOpacity = 0f;
        private float _targetRange = 0f;
        private float _waveTimeDuration = 0f;
        private float _waveOutlineDuration = 0f;

        private float _waveCounter = 0f;
        private float _outlineCounter = 0f;

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.tag != "Player" || other.gameObject.tag != "PlayerHand")
            {
                StartSonarBall(transform.position, 5f, 0.5f, 2f);
                Debug.Log("Collided");
            }
        }

        private void FixedUpdate()
        {
            if (_sonarStarted)
            {
                _waveFalloff = Mathf.Lerp(_waveFalloff, _targetRange, Time.fixedDeltaTime * _waveTimeDuration);
                _waveFalloffOpacity = Mathf.Lerp(_waveFalloffOpacity, 0f, Time.fixedDeltaTime * _waveTimeDuration);
                _waveCounter += Time.fixedDeltaTime;
                if (_waveCounter >= _waveTimeDuration)
                {
                    _waveFalloffOpacity = 0f;
                    _sonarStarted = false;
                }
            }
            else
            {
                if (_outlineCounter < _waveOutlineDuration)
                {
                    _waveOutlineOpacity =
                        Mathf.Lerp(_waveOutlineOpacity, 0f, Time.fixedDeltaTime * _waveOutlineDuration);
                    _outlineCounter += Time.fixedDeltaTime;
                }
                else
                    _waveOutlineOpacity = 0f;
            }

            sonarBallMaterial.SetFloat("Range", _waveFalloff);
            sonarBallMaterial.SetFloat("Opacity", _waveFalloffOpacity);
            sonarBallMaterial.SetFloat("Edge Line Opacity", _waveOutlineOpacity);
        }

        public void StartSonarBall(Vector3 position, float maxRange, float waveTimeDuration, float waveOutlineDuration)
        {
            _targetRange = maxRange;
            _waveTimeDuration = waveTimeDuration;
            _waveOutlineDuration = waveOutlineDuration;

            _waveOutlineOpacity = _waveFalloffOpacity = 1f;
            _waveCounter = _outlineCounter = 0f;
            sonarBallMaterial.SetVector("Position", position);

            _sonarStarted = true;
        }

        /// <summary>
        /// Finds all AI agents within a given range and tells them to investigate the sound's origin.
        /// </summary>
        /// <param name="center">The center of the detection sphere (the sound's origin).</param>
        /// <param name="detectionRange">How far the sound travels.</param>
        public void DistractWithinRange(Vector3 center, float detectionRange)
        {
            Collider[] collidersInRange = Physics.OverlapSphere(center, detectionRange);

            Debug.Log($"Sonar ping detected {collidersInRange.Length} colliders in a {detectionRange}m range.");

            foreach (Collider col in collidersInRange)
            {
                if (col.TryGetComponent<PatrolAi>(out PatrolAi enemyAI))
                {
                    Debug.Log($"Distracting AI: {enemyAI.gameObject.name}");
                    enemyAI.Distract(center);
                }
            }
        }
    }
}