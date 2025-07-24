// Amplify Shader Pack
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;

namespace AmplifyShaderPack.Assets.Scripts
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	public class CameraDepthActivation : MonoBehaviour
	{
		void Start ()
		{
			GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
		}	
	}
}
