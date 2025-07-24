// Amplify Shader Pack
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;

namespace AmplifyShaderPack.Assets.Scripts.Samples
{
    public class ForceShieldDestroyBall : MonoBehaviour
    {
		// Destroy the gameObject after lifetime
        public float lifetime = 5f;

        void Start()
        {
            Destroy(gameObject, lifetime);
        }
    }
}
