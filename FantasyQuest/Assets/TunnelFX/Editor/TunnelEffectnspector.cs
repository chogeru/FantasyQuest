using UnityEngine;
using UnityEditor;
using System.Collections;

namespace TunnelEffect
{
	[CustomEditor(typeof(TunnelFX))]
	public class TunnelEffectInspector : Editor
	{

		TunnelFX _effect;
		static GUIStyle titleLabelStyle, sectionHeaderStyle;
		static Color titleColor;
		static string[] layerNames = new string[] {
			"Layer 1",
			"Layer 2",
			"Layer 3",
			"Layer 4"
		};
		static bool[] expandLayer = new bool[4];

		void OnEnable ()
		{
			titleColor = EditorGUIUtility.isProSkin ? new Color (0.52f, 0.66f, 0.9f) : new Color (0.12f, 0.16f, 0.4f);
			_effect = (TunnelFX)target;
			for (int k=0; k<4; k++) {
				expandLayer [k] = EditorPrefs.GetBool ("TunnelExpandLayer" + k, false);
			}
		}

		void OnDestroy ()
		{
			// Save folding sections state
			for (int k=0; k<4; k++) {
				EditorPrefs.SetBool ("TunnelExpandLayer" + k, expandLayer [k]);
			}
		}

		public override void OnInspectorGUI ()
		{
			if (_effect == null)
				return;
			_effect.isDirty = false;

			EditorGUILayout.Separator ();

			EditorGUILayout.BeginHorizontal ();
			DrawLabel ("General Settings");
			if (GUILayout.Button("Help", GUILayout.Width(40))) {
				if (!EditorUtility.DisplayDialog("Tunnel FX", "To learn more about a property in this inspector move the mouse over the label for a quick description (tooltip).\n\nPlease check README file in the root of the asset for details and contact support.\n\nIf you like Tunnel FX, please rate it on the Asset Store. For feedback and suggestions visit our support forum on kronnect.com.", "Close", "Visit Support Forum")) {
					Application.OpenURL("http://kronnect.com/taptapgo");
				}
			}

			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent ("Preset", "Quick configurations."), GUILayout.Width (90));
			_effect.preset = (TUNNEL_PRESET)EditorGUILayout.EnumPopup (_effect.preset);
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Square Tunnel", GUILayout.Width (90));
			_effect.squareTunnel = EditorGUILayout.Toggle (_effect.squareTunnel, GUILayout.Width(40));
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent ("Depth Aware", "Should effect be drawn behind any geometry?"), GUILayout.Width (90));
			_effect.depthAware = EditorGUILayout.Toggle (_effect.depthAware);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent ("Camera Rotates", "Should effect be aligned with camera rotation?"), GUILayout.Width (90));
			_effect.cameraRotation = EditorGUILayout.Toggle (_effect.cameraRotation);
			EditorGUILayout.EndHorizontal ();

			if (_effect.cameraRotation && _effect.hyperSpeed>0) {
				EditorGUILayout.HelpBox("Camera rotation is disabled with Hyper Speed value > 0.", MessageType.Info);
			}

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Center", GUILayout.Width (90));
			_effect.uvOffset = EditorGUILayout.Vector2Field ("", _effect.uvOffset);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent ("   Animated", "Should center be animated?"), GUILayout.Width (90));
			_effect.uvOffsetAnimated = EditorGUILayout.Toggle (_effect.uvOffsetAnimated, GUILayout.Width(40));
			if (_effect.uvOffsetAnimated) {
				GUILayout.Label (new GUIContent("Amplitude", "Radius for the animation of the tunnel center"));
				_effect.uvOffsetAnimationAmplitude = EditorGUILayout.Slider (_effect.uvOffsetAnimationAmplitude, 0, 0.5f);
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent("Hyper Speed", "Increase to produce a hyperspeed effect."));
			_effect.hyperSpeed = EditorGUILayout.Slider (_effect.hyperSpeed, 0, 1f);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent ("Global Alpha", "Alpha blending between effect and your scene image."), GUILayout.Width (90));
			_effect.globalAlpha = EditorGUILayout.Slider (_effect.globalAlpha, 0f, 1f);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent ("Transition", "Transition and blending for the tunnel effect. The transition goes from current screen image to transition color in the 0-0.1 range and from transition color to tunnel effect in the 0.1-0.2 range. Use a value of 1.0 to simply use the tunnel effect without any lerp (which is a bit faster). Increasing this transition value from 0 to 1 quickly creates a cool flash transition."), GUILayout.Width (90));
			_effect.transition = EditorGUILayout.Slider (_effect.transition, 0f, 1f);
			GUILayout.Label("Color");
			_effect.transitionColor = EditorGUILayout.ColorField(_effect.transitionColor,  GUILayout.Width(40));
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent ("Background", "Visible distance of tunnel."), GUILayout.Width (90));
			_effect.fallOff = EditorGUILayout.Slider (_effect.fallOff, 1f, 100f);
			GUILayout.Label("Color");
			_effect.backgroundColor = EditorGUILayout.ColorField(_effect.backgroundColor, GUILayout.Width(40));
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent ("Downsampling", "Reduces the resolution of the effect and increases performance."), GUILayout.Width (90));
			_effect.downsampling = EditorGUILayout.IntSlider (_effect.downsampling, 1, 8);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.Separator ();
			DrawLabel ("Layer Options");

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent ("Layer Count", "Number of textures used for the tunnel effect. Each layer can be customized separately."), GUILayout.Width (90));
			_effect.layerCount = EditorGUILayout.IntSlider (_effect.layerCount, 1, 4);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label (new GUIContent("Global Speed", "Speed multiplier applied to all layers."));
			_effect.layersSpeed = EditorGUILayout.Slider (_effect.layersSpeed, 0, 10f);
			EditorGUILayout.EndHorizontal ();

			if (_effect.layerCount>1) {
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label (new GUIContent ("Blend In Order", "If this option is enabled, a layer will occlude previous one based on its alpha component. For example, a cutout effect will require this option enabled."), GUILayout.Width (90));
				_effect.blendInOrder = EditorGUILayout.Toggle (_effect.blendInOrder, GUILayout.Width(40));
				EditorGUILayout.EndHorizontal ();
			}

			if (sectionHeaderStyle == null) {
				sectionHeaderStyle = new GUIStyle (EditorStyles.foldout);
			}
			sectionHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color (0.52f, 0.66f, 0.9f) : new Color (0.12f, 0.16f, 0.4f);
			sectionHeaderStyle.margin = new RectOffset (12, 0, 0, 0);
			sectionHeaderStyle.fontStyle = FontStyle.Bold;


			for (int k=0; k<_effect.layerCount; k++) {
				EditorGUILayout.BeginHorizontal ();
				expandLayer [k] = EditorGUILayout.Foldout (expandLayer [k], layerNames [k], sectionHeaderStyle);
				EditorGUILayout.EndHorizontal ();
				if (expandLayer [k]) {
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label (new GUIContent("   Texture", "The texture used in this layer. Alpha channel will be used for transparency if present."), GUILayout.Width (90));
					_effect.SetTexture (k, (Texture2D)EditorGUILayout.ObjectField (_effect.GetTexture (k), typeof(Texture2D), false));
					EditorGUILayout.EndHorizontal ();
					
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label (new GUIContent("   Alpha", "The weight of this layer in the final color mix. The sum of all layers contributions should be 1 aprox. Use along with exposure to create nice color blends."), GUILayout.Width (90));
					_effect.SetAlpha (k, EditorGUILayout.FloatField (_effect.GetAlpha (k), GUILayout.Width(80)));
					EditorGUILayout.EndHorizontal ();

					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Travel Speed", GUILayout.Width (90));
					_effect.SetTravelSpeed (k, EditorGUILayout.Slider (_effect.GetTravelSpeed (k), -20f, 20f));
					EditorGUILayout.EndHorizontal ();
				
					if (!_effect.squareTunnel) {
						EditorGUILayout.BeginHorizontal ();
						GUILayout.Label ("   Rotation", GUILayout.Width (90));
						_effect.SetRotationSpeed (k, EditorGUILayout.Slider (_effect.GetRotationSpeed (k), -10f, 10f));
						EditorGUILayout.EndHorizontal ();
					}
				
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label ("   Twist", GUILayout.Width (90));
					_effect.SetTwist (k, EditorGUILayout.Slider (_effect.GetTwist (k), -3f, 3f));
					EditorGUILayout.EndHorizontal ();
				
					EditorGUILayout.BeginHorizontal ();
					GUILayout.Label (new GUIContent("   Exposure", "An exposure below 0.5 will make this texture darker. A value above 0.5 will increase the brightness."), GUILayout.Width (90));
					_effect.SetExposure (k, EditorGUILayout.Slider (_effect.GetExposure (k), 0f, 1f));
					EditorGUILayout.EndHorizontal ();
				}


			}

			EditorGUILayout.Separator ();

			if (_effect.isDirty) {
				EditorUtility.SetDirty (target);
			}


		}

		void DrawLabel (string s)
		{
			if (titleLabelStyle == null) {
				GUIStyle skurikenModuleTitleStyle = "ShurikenModuleTitle";
				titleLabelStyle = new GUIStyle (skurikenModuleTitleStyle);
				titleLabelStyle.contentOffset = new Vector2 (5f, -2f);
				titleLabelStyle.normal.textColor = titleColor;
				titleLabelStyle.fixedHeight = 22;
				titleLabelStyle.fontStyle = FontStyle.Bold;
			}

			GUILayout.Label (s, titleLabelStyle);
		}

	}

}
