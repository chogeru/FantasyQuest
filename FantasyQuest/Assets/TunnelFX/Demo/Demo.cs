using UnityEngine;
using System.Collections;

namespace TunnelEffect {
				public class Demo : MonoBehaviour {
								int demoMode = 1;
								GUIStyle labelStyle;

								void Start () {
												// setup GUI resizer - only for the demo
												GUIResizer.Init (800, 500); 
												TunnelFX.instance.preset = TUNNEL_PRESET.SpaceTravel;
								}

								void OnGUI () {
												// Do autoresizing of GUI layer
												GUIResizer.AutoResize ();

												Rect rect = new Rect (10, Screen.height - 62, Screen.width - 20, 50);
												GUI.Box (rect, "");

												rect = new Rect (20, Screen.height - 60, Screen.width - 20, 30);
												GUI.Label (rect, "Press SPACE to switch preset.");

												rect = new Rect (20, Screen.height - 40, Screen.width - 20, 30);
												if (labelStyle == null) {
																labelStyle = new GUIStyle (GUI.skin.label);
																labelStyle.fontStyle = FontStyle.Bold;
												}
												GUI.Label (rect, "Customize this tunnel! Select the camera and change Tunnel FX inspector settings!", labelStyle);
								}

								void Update () {
												if (Input.GetKeyDown (KeyCode.Space) || (Input.touchSupported && Input.GetMouseButtonDown (0))) {
																demoMode++;
																if (demoMode > 14) {
																				demoMode = 1;
																}
																TunnelFX.instance.preset = (TUNNEL_PRESET)demoMode;
												}
								}

				}

}