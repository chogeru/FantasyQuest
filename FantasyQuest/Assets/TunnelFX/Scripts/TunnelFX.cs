//------------------------------------------------------------------------------------------------------------------
// Tunnel FX
// Copyright (c) Kronnect
//------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace TunnelEffect {

				public enum TUNNEL_PRESET {
								Custom = 0,
								SpaceTravel = 1,
								MagmaTunnel = 2,
								CloudAscension = 3,
								MetalStructure = 4,
								WaterTunnel = 5,
								CavePassage = 6,
								Stripes = 7,
								Twightlight = 8,
								MysticTravel = 9,
								Chromatic = 10,
								IcePassage = 11,
								FireTornado = 12,
								ExhaustPort = 13,
								CutOutRail = 14
				}

				[ExecuteInEditMode]
				[RequireComponent (typeof(Camera))]
				[ImageEffectAllowedInSceneView]
				public class TunnelFX : MonoBehaviour {

								static TunnelFX _tunnel;

								public static TunnelFX instance { 
												get { 
																if (_tunnel == null) {
																				if (Camera.main != null)
																								_tunnel = Camera.main.GetComponent<TunnelFX> ();
																				if (_tunnel == null) {
																								foreach (Camera camera in Camera.allCameras) {
																												_tunnel = camera.GetComponent<TunnelFX> ();
																												if (_tunnel != null)
																																break;
																								}
																				}
																}
																return _tunnel;
												} 
								}

								[HideInInspector]
								public bool isDirty;

								[SerializeField]
								int _layerCount = 4;

								public int layerCount {
												get { return _layerCount; }
												set {
																if (_layerCount != value) {
																				_preset = TUNNEL_PRESET.Custom;
																				_layerCount = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								TUNNEL_PRESET _preset = TUNNEL_PRESET.SpaceTravel;

								public TUNNEL_PRESET preset {
												get { return _preset; }
												set {
																if (_preset != value) {
																				_preset = value;
																				UpdateMaterialProperties ();
																				PlayTransition (2.0f);
																}
												}
								}

								[SerializeField]
								bool _depthAware = false;

								public bool depthAware {
												get { return _depthAware; }
												set {
																if (_depthAware != value) {
																				_depthAware = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								bool _cameraRotation = false;

								public bool cameraRotation {
												get { return _cameraRotation; }
												set {
																if (_cameraRotation != value) {
																				_cameraRotation = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								Texture2D[] _layerTextures = new Texture2D[4];

								public Texture2D GetTexture (int layerIndex) {
												return _layerTextures [layerIndex];
								}

								public void SetTexture (int layerIndex, Texture2D tex) {
												if (tex != _layerTextures [layerIndex]) {
																_preset = TUNNEL_PRESET.Custom;
																_layerTextures [layerIndex] = tex;
																UpdateMaterialProperties ();
												}
								}

		
								[SerializeField]
								float[] _travelSpeed = new float[] { 1.5f, 1.5f, 1.5f, 1.5f };

								public float GetTravelSpeed (int layerIndex) {
												return _travelSpeed [layerIndex];
								}

								public void SetTravelSpeed (int layerIndex, float value) {
												if (_travelSpeed [layerIndex] != value) {
																_preset = TUNNEL_PRESET.Custom;
																_travelSpeed [layerIndex] = value;
																UpdateMaterialProperties (); 
												}
								}

								[SerializeField]
								float[] _rotationSpeed = new float[] { 0.5f, 0.5f, 0.5f, 0.5f };

								public float GetRotationSpeed (int layerIndex) {
												return _rotationSpeed [layerIndex];
								}

								public void SetRotationSpeed (int layerIndex, float value) {
												if (_rotationSpeed [layerIndex] != value) {
																_preset = TUNNEL_PRESET.Custom;
																_rotationSpeed [layerIndex] = value;
																UpdateMaterialProperties (); 
												}
								}

								[SerializeField]
								float[] _twist = new float[] { 0.1f, 0.1f, 0.1f, 0.1f };

								public float GetTwist (int layerIndex) {
												return _twist [layerIndex];
								}

								public void SetTwist (int layerIndex, float value) {
												if (_twist [layerIndex] != value) {
																_preset = TUNNEL_PRESET.Custom;
																_twist [layerIndex] = value;
																UpdateMaterialProperties (); 
												}
								}

								[SerializeField]
								float[] _exposure = new float[] { 0.9f, 0.9f, 0.9f, 0.9f };

								public float GetExposure (int layerIndex) {
												return _exposure [layerIndex];
								}

								public void SetExposure (int layerIndex, float value) {
												if (_exposure [layerIndex] != value) {
																_preset = TUNNEL_PRESET.Custom;
																_exposure [layerIndex] = value;
																UpdateMaterialProperties (); 
												}
								}

		
								[SerializeField]
								float[] _alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };

								public float GetAlpha (int layerIndex) {
												return _alpha [layerIndex];
								}

								public void SetAlpha (int layerIndex, float value) {
												if (_alpha [layerIndex] != value) {
																_preset = TUNNEL_PRESET.Custom;
																_alpha [layerIndex] = value;
																UpdateMaterialProperties (); 
												}
								}

								[SerializeField]
								Vector2 _uvOffset = new Vector2 (0f, 0f);

								public Vector2 uvOffset {
												get { return _uvOffset; }
												set {
																if (_uvOffset != value) {
																				_preset = TUNNEL_PRESET.Custom;
																				_uvOffset = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								bool _uvOffsetAnimated = false;

								public bool uvOffsetAnimated {
												get { return _uvOffsetAnimated; }
												set {
																if (_uvOffsetAnimated != value) {
																				_preset = TUNNEL_PRESET.Custom;
																				_uvOffsetAnimated = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								float _uvOffsetAnimationAmplitude = 0.15f;

								public float uvOffsetAnimationAmplitude {
												get { return _uvOffsetAnimationAmplitude; }
												set {
																if (_uvOffsetAnimationAmplitude != value) {
																				_preset = TUNNEL_PRESET.Custom;
																				_uvOffsetAnimationAmplitude = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								float _hyperSpeed = 0f;

								public float hyperSpeed {
												get { return _hyperSpeed; }
												set {
																if (_hyperSpeed != value) {
																				_preset = TUNNEL_PRESET.Custom;
																				_hyperSpeed = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								float _globalAlpha = 1f;

								public float globalAlpha {
												get { return _globalAlpha; }
												set {
																if (_globalAlpha != value) {
																				_globalAlpha = value;
																				UpdateMaterialProperties ();
																}
												}
								}


								[SerializeField]
								float _transition = 1f;

								public float transition {
												get { return _transition; }
												set {
																if (_transition != value) {
																				_preset = TUNNEL_PRESET.Custom;
																				_transition = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								Color _transitionColor = Color.white;

								public Color transitionColor {
												get { return _transitionColor; }
												set {
																if (_transitionColor != value) {
																				_preset = TUNNEL_PRESET.Custom;
																				_transitionColor = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								Color _backgroundColor = Color.white;

								public Color backgroundColor {
												get { return _backgroundColor; }
												set {
																if (_backgroundColor != value) {
																				_preset = TUNNEL_PRESET.Custom;
																				_backgroundColor = value;
																				UpdateMaterialProperties ();
																}
												}
								}

		
								[SerializeField]
								float _fallOff = 10f;

								public float fallOff {
												get { return _fallOff; }
												set {
																if (_fallOff != value) {
																				_preset = TUNNEL_PRESET.Custom;
																				_fallOff = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								bool _blendInOrder = false;

								public bool blendInOrder {
												get { return _blendInOrder; }
												set {
																if (_blendInOrder != value) {
																				_preset = TUNNEL_PRESET.Custom;
																				_blendInOrder = value;
																				UpdateMaterialProperties ();
																}
												}
								}

		
								[SerializeField]
								float _layersSpeed = 1f;

								public float layersSpeed {
												get { return _layersSpeed; }
												set {
																if (_layersSpeed != value) {
																				_preset = TUNNEL_PRESET.Custom;
																				_layersSpeed = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								bool _squareTunnel = false;

								public bool squareTunnel {
												get { return _squareTunnel; }
												set {
																if (_squareTunnel != value) {
																				_preset = TUNNEL_PRESET.Custom;
																				_squareTunnel = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								[SerializeField]
								int _downsampling = 1;

								public int downsampling {
												get { return _downsampling; }
												set {
																if (_downsampling != value) {
																				_downsampling = value;
																				UpdateMaterialProperties ();
																}
												}
								}

								// internal fields

								[SerializeField]
								Material tunnelMat;

								Camera mainCamera;
								static string[] textureNames = new string[] { "_TunnelTex1", "_TunnelTex2", "_TunnelTex3", "_TunnelTex4" };
								static string[] paramNames = new string[] { "_Params1", "_Params2", "_Params3", "_Params4" };
								Dictionary<string, Texture2D> dict;
								float transitionStart, transitionDuration;
								bool isTransitioning;
								string SKW_TUNNEL_CIRCULAR = "TUNNEL_CIRCULAR";
								string SKW_TUNNEL_SQUARE = "TUNNEL_SQUARE";
								string SKW_TUNNEL_BLEND_IN_ORDER = "TUNNEL_BLEND_IN_ORDER";
		string SKW_TUNNEL_CAMERA_ROTATES = "TUNNEL_CAMERA_ROTATES";
								List<string> shaderKeywords;
								Vector4[] tunnelParams = new Vector4[4];

								#region Game loop events

								void OnEnable () {
												if (tunnelMat == null) {
																tunnelMat = new Material (Shader.Find ("TunnelEffect/TunnelFX")); // Instantiate (Resources.Load<Material> ("Materials/TunnelFX")) as Material;
																tunnelMat.hideFlags = HideFlags.DontSave;
												}

												if (dict == null) {
																dict = new Dictionary<string, Texture2D> ();
												}

												mainCamera = gameObject.GetComponent<Camera> ();
												UpdateMaterialProperties ();
								}

								void OnDestroy () {
												if (tunnelMat != null) {
																DestroyImmediate (tunnelMat);
																tunnelMat = null;
												}
												if (dict != null) {
																dict.Clear ();
																dict = null;
												}
								}

								void Reset () {
												UpdateMaterialProperties ();
								}

								void Update () {
												if (Application.isPlaying && isTransitioning) {
																float elapsed = Mathf.Clamp01 ((Time.time - transitionStart) / transitionDuration);
																_transition = elapsed;
																if (elapsed >= 1.0f)
																				isTransitioning = false;
												}
								}


								[ImageEffectOpaque]
								void OnRenderImage (RenderTexture source, RenderTexture destination) {
												if (!enabled) {
																Graphics.Blit (source, destination);
																return;
												}

												if (_cameraRotation && _hyperSpeed == 0) {
																Matrix4x4 m = Matrix4x4.TRS (Vector3.zero, Quaternion.Inverse (mainCamera.transform.rotation), Vector3.one);
																tunnelMat.SetMatrix ("_ViewRot", m);
												}

												for (int k = 0; k < 4; k++) {
																if (Application.isPlaying) {
																				tunnelParams [k].x += _travelSpeed [k] * _layersSpeed * Time.deltaTime;
																				tunnelParams [k].y += _rotationSpeed [k] * Time.deltaTime;
																} else {
																				// fix for SceneCamera issue
																				float expos = 0f;
																				if (_exposure [k] < 0.5) {
																								expos = _exposure [k] * 2f;
																				} else {
																								expos = 1f / (1.0001f - (_exposure [k] - 0.5f) * 2f);
																				}
																				tunnelParams [k].w = expos;
																}
																tunnelMat.SetVector (paramNames [k], tunnelParams [k]);
												}

												Vector4 mixParams = new Vector4 (_uvOffset.x - 0.5f, _uvOffset.y - 0.5f, _layerCount, _transition * 10f);
												if (_uvOffsetAnimated) {
																mixParams.x += Mathf.Sin (Time.time * 0.5f) * _uvOffsetAnimationAmplitude;
																mixParams.y += Mathf.Cos (Time.time * 0.25f) * _uvOffsetAnimationAmplitude;
												}
												tunnelMat.SetVector ("_MixParams", mixParams);

												if (_downsampling > 1) {
																RenderTexture rt = RenderTexture.GetTemporary (source.width / _downsampling, source.height / _downsampling);
																Graphics.Blit (source, rt, tunnelMat, 2);
																tunnelMat.SetTexture ("_TunnelDownsampled", rt);
																Graphics.Blit (source, destination, tunnelMat, _depthAware ? 3 : 4);
																RenderTexture.ReleaseTemporary (rt);
												} else {
																Graphics.Blit (source, destination, tunnelMat, _depthAware ? 0 : 1);
												}
								}


								#endregion

								void UpdateMaterialProperties () {
												if (tunnelMat == null)
																return;

												switch (_preset) {
												case TUNNEL_PRESET.SpaceTravel:
																_layerCount = 4;
																for (int k = 0; k < 4; k++) {
																				_layerTextures [k] = GetTexture ("starfield");
																}
																_alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 2f, 1.2f, 0.7f, 0.25f };
																_rotationSpeed = new float[] { 0.7f, 0.35f, 0.2f, 0.1f };
																_twist = new float[] { 0.1f, 0.1f, 0.1f, 0.1f };
																_exposure = new float[] { 0.85f, 0.85f, 0.85f, 0.85f };
																_transition = 1f;
																_transitionColor = Color.white;
																_fallOff = 10f;
																_backgroundColor = Color.black; //new Color(45f/255f, 45f/255f, 45f/255f);
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = true;
																_uvOffsetAnimationAmplitude = 0.15f;
																_blendInOrder = false;
																_squareTunnel = false;
																_hyperSpeed = 0.9f;
																_layersSpeed = 1f;
																break;
												case TUNNEL_PRESET.MagmaTunnel:
																_layerCount = 4;
																_layerTextures [0] = GetTexture ("chromatic");
																for (int k = 1; k < 4; k++) {
																				_layerTextures [k] = GetTexture ("fire");
																}
																_alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 0.7f, 0.4f, 0.2f, 0.1f };
																_rotationSpeed = new float[] { 0f, -0.002f, 0.003f, -0.005f };
																_twist = new float[] { 0.2f, -0.2f, 0.2f, -0.2f };
																_exposure = new float[] { 0.55f, 0.55f, 0.6f, 0.65f };
																_transition = 1f;
																_transitionColor = Color.yellow;
																_fallOff = 10f;
																_backgroundColor = new Color (1f, 1f, 0.89f);
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = false;
																_uvOffsetAnimationAmplitude = 0.05f;
																_blendInOrder = false;
																_squareTunnel = false;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;
												case TUNNEL_PRESET.FireTornado:
																_layerCount = 4;
																for (int k = 0; k < 4; k++) {
																				_layerTextures [k] = GetTexture ("fire");
																}
																_alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 0.7f, 0.4f, 0.2f, 0.15f };
																_rotationSpeed = new float[] { 4f, -5f, 6f, -7f };
																_twist = new float[] { 0.5f, 0.5f, 0.5f, 0.5f };
																_exposure = new float[] { 0.5f, 0.6f, 0.7f, 0.8f };
																_transition = 1f;
																_transitionColor = Color.yellow;
																_fallOff = 10f;
																_backgroundColor = new Color (1f, 1f, 0.89f);
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = true;
																_uvOffsetAnimationAmplitude = 0.075f;
																_blendInOrder = false;
																_squareTunnel = false;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;
												case TUNNEL_PRESET.CloudAscension:
																_layerCount = 4;
																for (int k = 0; k < 4; k++) {
																				_layerTextures [k] = GetTexture ("clouds");
																}
																_alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 0.7f, 0.3f, 0.2f, 0.1f };
																_rotationSpeed = new float[] { 0.2f, 0.18f, 0.16f, 0.1f };
																_twist = new float[] { 0.05f, 0.04f, 0.03f, 0.02f };
																_exposure = new float[] { 0.45f, 0.5f, 0.55f, 0.6f };
																_transition = 1f;
																_transitionColor = new Color (0, 1f, 1f);
																_fallOff = 10f;
																_backgroundColor = new Color (0, 1f, 1f);
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = true;
																_uvOffsetAnimationAmplitude = 0.05f;
																_blendInOrder = false;
																_squareTunnel = false;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;			
												case TUNNEL_PRESET.MetalStructure:
																_layerCount = 1;
																_alpha = new float[] { 0.75f, 0.26664f, 0.13332f, 0.06666f };
																_layerTextures [0] = GetTexture ("metalstructure");
																_travelSpeed = new float[] { 5f, 0.3f, 0.2f, 0.1f };
																_rotationSpeed = new float[] { 0.2f, 0.18f, 0.16f, 0.1f };
																_twist = new float[] { 0f, 0.04f, 0.03f, 0.02f };
																_exposure = new float[] { 0.77f, 0.75f, 0.8f, 0.85f };
																_transition = 1f;
																_transitionColor = Color.black;
																_fallOff = 10f;
																_backgroundColor = Color.black;
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = true;
																_uvOffsetAnimationAmplitude = 0.1f;
																_blendInOrder = false;
																_squareTunnel = true;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;			
												case TUNNEL_PRESET.WaterTunnel:
																_layerCount = 4;
																_layerTextures [0] = GetTexture ("water");
																_layerTextures [1] = GetTexture ("water2");
																_layerTextures [2] = GetTexture ("water");
																_layerTextures [3] = GetTexture ("water2");
																_alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 0.7f, 0.3f, 0.2f, 0.1f };
																_rotationSpeed = new float[] { 0.2f, 0.18f, 0.16f, 0.1f };
																_twist = new float[] { 0.05f, 0.04f, 0.03f, 0.02f };
																_exposure = new float[] { 0.5f, 0.6f, 0.6f, 0.6f };
																_transition = 1f;
																_transitionColor = new Color (0, 1f, 1f);
																_fallOff = 10f;
																_backgroundColor = new Color (0, 1f, 1f);
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = true;
																_uvOffsetAnimationAmplitude = 0.05f;
																_blendInOrder = false;
																_squareTunnel = false;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;					
												case TUNNEL_PRESET.CavePassage:
																_layerCount = 1;
																_layerTextures [0] = GetTexture ("rocks");
																_alpha = new float[] { 1f, 0.26664f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 2f, 0.3f, 0.2f, 0.1f };
																_rotationSpeed = new float[] { 0f, 0.18f, 0.16f, 0.1f };
																_twist = new float[] { 0f, 0.04f, 0.03f, 0.02f };
																_exposure = new float[] { 0.7f, 0.75f, 0.8f, 0.85f };
																_transition = 1f;
																_transitionColor = Color.black;
																_fallOff = 2.5f;
																_backgroundColor = Color.black;
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = false;
																_uvOffsetAnimationAmplitude = 0.1f;
																_blendInOrder = false;
																_squareTunnel = true;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;
												case TUNNEL_PRESET.Stripes:
																_layerCount = 1;
																_layerTextures [0] = GetTexture ("stripes");
																_alpha = new float[] { 1f, 0.26664f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 2f, 0.3f, 0.2f, 0.1f };
																_rotationSpeed = new float[] { 1f, 0.18f, 0.16f, 0.1f };
																_twist = new float[] { 0f, 0.04f, 0.03f, 0.02f };
																_exposure = new float[] { 0.57f, 0.75f, 0.8f, 0.85f };
																_transition = 1f;
																_transitionColor = Color.black;
																_fallOff = 2.5f;
																_backgroundColor = Color.black;
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = false;
																_uvOffsetAnimationAmplitude = 0.1f;
																_blendInOrder = false;
																_squareTunnel = false;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;
												case TUNNEL_PRESET.Twightlight:
																_layerCount = 3;
																_layerTextures [0] = GetTexture ("starfield");
																_layerTextures [1] = GetTexture ("lights2");
																_layerTextures [2] = GetTexture ("lights1");
																_alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 2f, 0.3f, 0.2f, 0.1f };
																_rotationSpeed = new float[] { 1f, 0.18f, 0.16f, 0.1f };
																_twist = new float[] { 1f, 0.13f, -2.24f, 0.02f };
																_exposure = new float[] { 0.7f, 0.832f, 0.7f, 0.85f };
																_transition = 1f;
																_transitionColor = Color.black;
																_fallOff = 2.5f;
																_backgroundColor = Color.white;
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = false;
																_uvOffsetAnimationAmplitude = 0.1f;
																_blendInOrder = false;
																_squareTunnel = false;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;
												case TUNNEL_PRESET.MysticTravel:
																_layerCount = 4;
																_layerTextures [0] = GetTexture ("starfield");
																for (int k = 1; k < 4; k++) {
																				_layerTextures [k] = GetTexture ("clouds");
																}
																_alpha = new float[] { 0.533328f, 0.26664f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 0.7f, 0.3f, 0.2f, 0.1f };
																_rotationSpeed = new float[] { 0f, 0.18f, 0.16f, 0.1f };
																_twist = new float[] { 0f, 0.04f, 0.03f, 0.02f };
																_exposure = new float[] { 0.7f, 0.65f, 0.65f, 0.65f };
																_transition = 1f;
																_transitionColor = new Color (0, 1f, 1f);
																_fallOff = 4f;
																_backgroundColor = new Color (155f / 255f, 225f / 255f, 1f);
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = false;
																_uvOffsetAnimationAmplitude = 0.05f;
																_blendInOrder = false;
																_squareTunnel = false;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;
												case TUNNEL_PRESET.Chromatic:
																_layerCount = 1;
																_layerTextures [0] = GetTexture ("chromatic");
																_alpha = new float[] { 1f, 0.26664f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 0.7f, 0.3f, 0.2f, 0.1f };
																_rotationSpeed = new float[] { -0.15f, 0.18f, 0.16f, 0.1f };
																_twist = new float[] { -0.4f, 0.04f, 0.03f, 0.02f };
																_exposure = new float[] { 0.5f, 0.5f, 0.55f, 0.6f };
																_transition = 1f;
																_transitionColor = new Color (0, 1f, 1f);
																_fallOff = 15f;
																_backgroundColor = Color.white;
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = false;
																_uvOffsetAnimationAmplitude = 0.05f;
																_blendInOrder = false;
																_squareTunnel = false;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;			
												case TUNNEL_PRESET.IcePassage:
																_layerCount = 1;
																_layerTextures [0] = GetTexture ("ice");
																_alpha = new float[] { 0.75f, 0.26664f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 0.7f, 0.3f, 0.2f, 0.1f };
																_rotationSpeed = new float[] { 0f, 0.18f, 0.16f, 0.1f };
																_twist = new float[] { 0.3f, 0.04f, 0.03f, 0.02f };
																_exposure = new float[] { 0.67f, 0.5f, 0.55f, 0.6f };
																_transition = 1f;
																_transitionColor = new Color (0, 1f, 1f);
																_fallOff = 30f;
																_backgroundColor = Color.white;
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = false;
																_uvOffsetAnimationAmplitude = 0.05f;
																_blendInOrder = false;
																_squareTunnel = true;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;			
												case TUNNEL_PRESET.ExhaustPort:
																_layerCount = 2;
																_layerTextures [0] = GetTexture ("rustedmetal");
																_layerTextures [1] = GetTexture ("clouds");
																_alpha = new float[] { 0.5f, 0.1f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 0.5f, 1f, 0.35f, 0.2f };
																_rotationSpeed = new float[] { 0f, 0.5f, -0.5f, 0.5f };
																_twist = new float[] { 0f, -0.2f, 0.2f, -0.2f };
																_exposure = new float[] { 0.7f, 0.6f, 0.6f, 0.65f };
																_transition = 1f;
																_transitionColor = Color.black;
																_fallOff = 10f;
																_backgroundColor = Color.black;
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = false;
																_uvOffsetAnimationAmplitude = 0.075f;
																_blendInOrder = false;
																_squareTunnel = false;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;
												case TUNNEL_PRESET.CutOutRail:
																_layerCount = 2;
																_layerTextures [0] = GetTexture ("coloredsand");
																_layerTextures [1] = GetTexture ("stripescutout");
																_alpha = new float[] { 1f, 1f, 0.13332f, 0.06666f };
																_travelSpeed = new float[] { 1f, 9f, 0.35f, 0.2f };
																_rotationSpeed = new float[] { 0.1f, 0.1f, -0.5f, 0.5f };
																_twist = new float[] { 0f, -1f, 0.2f, -0.2f };
																_exposure = new float[] { 0.5f, 0.6f, 0.6f, 0.65f };
																_transition = 1f;
																_transitionColor = Color.black;
																_fallOff = 10f;
																_backgroundColor = Color.black;
																_uvOffset = Vector2.zero;
																_uvOffsetAnimated = false;
																_uvOffsetAnimationAmplitude = 0.075f;
																_blendInOrder = true;
																_squareTunnel = false;
																_hyperSpeed = 0f;
																_layersSpeed = 1f;
																break;
												}
												for (int k = 0; k < _layerTextures.Length; k++) {
																if (_layerTextures [k] == null) {
																				// try to get the texture from the material itself
																				_layerTextures [k] = (Texture2D)tunnelMat.GetTexture (textureNames [k]);
																				// or restore default texture (should not happen)
																				if (_layerTextures [k] == null)
																								_layerTextures [k] = GetTexture ("starfield");
																}
																tunnelMat.SetTexture (textureNames [k], _layerTextures [k]);

																float expos = 0f;
																if (_exposure [k] < 0.5) {
																				expos = _exposure [k] * 2f;
																} else {
																				expos = 1f / (1.0001f - (_exposure [k] - 0.5f) * 2f);
																}
																tunnelParams [k].w = expos;
																tunnelParams [k].z = _twist [k];
												}

												tunnelMat.SetColor ("_TransitionColor", _transitionColor);
												tunnelMat.SetColor ("_BackgroundColor", _backgroundColor);
												tunnelMat.SetVector ("_MixParams2", new Vector4 (_fallOff, (_blendInOrder && _layerCount > 1) ? 1f : 0f, _globalAlpha, Mathf.Clamp (1f - _hyperSpeed, 0.0001f, 1f)));
												Vector4 contributions = new Vector4 (_alpha [0], _alpha [1], _alpha [2], _alpha [3]);
												tunnelMat.SetVector ("_Params5", contributions);

												if (_depthAware && mainCamera.depthTextureMode != DepthTextureMode.Depth) {
																mainCamera.depthTextureMode |= DepthTextureMode.Depth;
												}

												if (shaderKeywords == null)
																shaderKeywords = new List<string> ();
												else
																shaderKeywords.Clear ();
												if (_squareTunnel) {
																shaderKeywords.Add (SKW_TUNNEL_SQUARE);
												} else {
																shaderKeywords.Add (SKW_TUNNEL_CIRCULAR);
												}
												if (_blendInOrder && _layerCount > 1)
																shaderKeywords.Add (SKW_TUNNEL_BLEND_IN_ORDER);
			if (_cameraRotation && _hyperSpeed==0) {
				shaderKeywords.Add (SKW_TUNNEL_CAMERA_ROTATES);
			}


												tunnelMat.shaderKeywords = shaderKeywords.ToArray ();

												isDirty = true;
								}


								Texture2D GetTexture (string name) {
												if (dict.ContainsKey (name)) {
																return dict [name];
												}

												Texture2D tex = Resources.Load<Texture2D> ("Textures/" + name);
												dict [name] = tex;
												return tex;
								}


								public void PlayTransition (float duration) {
												if (!Application.isPlaying)
																return;
												isTransitioning = true;
												transitionStart = Time.time;
												transitionDuration = duration + 0.0001f;
								}


				}

}