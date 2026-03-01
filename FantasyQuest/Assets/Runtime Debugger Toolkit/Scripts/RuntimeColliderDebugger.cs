using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.Rendering;


namespace ThornyDevtudio.RuntimeDebuggerToolkit
{

    public class RuntimeColliderDebugger : MonoBehaviour
    {
        public Color color_Mesh = Color.green;
        public Color color_Convex_Mesh = Color.yellow;
        public Camera targetCamera;

        public Toggle toggleShow3D;
        public Toggle toggleShow2D;
        public Toggle toggleShowCharacter;
        public Toggle toggleShowMesh;
        //public Toggle toggleShowTerrain;
        //private bool showTerrainCollider = true;
        public Toggle toggleWireframe;
        private bool showWireframe = false;


        private Material lineMaterial;
        private Material yellowMaterial;

        private static Mesh _cubeMesh;
        private static Mesh _sphereMesh;

        private bool show3DColliders = true;
        private bool show2DColliders = true;
        private bool showCharacterController = true;
        private bool showMeshCollider = true;

        private Mesh _capsuleMesh;



        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (toggleShow3D) toggleShow3D.onValueChanged.AddListener(val => show3DColliders = val);
            if (toggleShow2D) toggleShow2D.onValueChanged.AddListener(val => show2DColliders = val);
            if (toggleShowCharacter) toggleShowCharacter.onValueChanged.AddListener(val => showCharacterController = val);
            if (toggleShowMesh) toggleShowMesh.onValueChanged.AddListener(val => showMeshCollider = val);
            if (toggleWireframe) toggleWireframe.onValueChanged.AddListener(val => showWireframe = val);
            //if (toggleShowTerrain) toggleShowTerrain.onValueChanged.AddListener(val => showTerrainCollider = val);

            if (_capsuleMesh == null)
            {
                GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _capsuleMesh = temp.GetComponent<MeshFilter>().sharedMesh;
                Destroy(temp);
            }

        }

        private void OnEnable()
        {
            CreateMaterial();
            InitMeshes();
            RenderPipelineManager.endCameraRendering += OnRenderPipelineRender;
            Camera.onPostRender += OnBuiltinRender;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnRenderPipelineRender;
            Camera.onPostRender -= OnBuiltinRender;
        }

        private void OnBuiltinRender(Camera cam)
        {
            if (GraphicsSettings.currentRenderPipeline == null) // Built-in
                RenderColliders(cam);
        }

        private void OnRenderPipelineRender(ScriptableRenderContext context, Camera cam)
        {
            if (GraphicsSettings.currentRenderPipeline != null) // URP or HDRP
                RenderColliders(cam);
        }

        void CreateMaterial()
        {
            if (lineMaterial == null)
            {
                lineMaterial = Resources.Load<Material>("ColliderOverlay");
                if (lineMaterial)
                {
                    var baseMat = Resources.Load<Material>("ColliderOverlay");
                    lineMaterial = new Material(baseMat);
                    lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                    lineMaterial.SetColor("_Color", color_Mesh);
                }
                else
                {
                    Debug.LogError("ColliderOverlay material not found in Resources.");
                }
            }

            if (yellowMaterial == null)
            {
                var baseYellow = Resources.Load<Material>("ColliderOverlayYellow");
                if (baseYellow)
                {
                    yellowMaterial = new Material(baseYellow);
                    yellowMaterial.hideFlags = HideFlags.HideAndDontSave;
                    yellowMaterial.SetColor("_Color", color_Convex_Mesh);

                }
                else
                {
                    Debug.LogError("ColliderOverlayYellow material not found in Resources.");
                }
            }
        }





        void RenderColliders(Camera cam)
        {
            if (targetCamera != null && cam != targetCamera) return;

            if (!lineMaterial)
                CreateMaterial();

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            if (show3DColliders)
            {
                foreach (var col in FindObjectsOfType<Collider>())
                {
                    if (!col.enabled) continue;
                    if (col is CharacterController && !showCharacterController) continue;
                    if (col is MeshCollider && !showMeshCollider) continue;
                    Draw3DCollider(col);
                }
            }

            if (show2DColliders)
            {
                if (lineMaterial != null)
                    lineMaterial.SetPass(0);

                GL.Begin(GL.LINES);
                GL.Color(color_Mesh);
                foreach (var col2D in FindObjectsOfType<Collider2D>())
                {
                    if (!col2D.enabled) continue;
                    Draw2DCollider(col2D);
                }
                GL.End();
            }

            GL.PopMatrix();
        }

        void InitMeshes()
        {
            if (!_cubeMesh)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _cubeMesh = go.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(go);
            }

            if (!_sphereMesh)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _sphereMesh = go.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(go);
            }
        }

        void Draw3DCollider(Collider col)
        {
            if (showWireframe)
            {
                DrawWireCollider(col);
                return;
            }

            Matrix4x4 matrix = col.transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                lineMaterial.SetPass(0);

                Graphics.DrawMeshNow(_cubeMesh, matrix * Matrix4x4.TRS(box.center, Quaternion.identity, box.size));
            }
            else if (col is SphereCollider sphere)
            {
                Vector3 scale = Vector3.one * sphere.radius * 2f;
                lineMaterial.SetPass(0);

                Graphics.DrawMeshNow(_sphereMesh, matrix * Matrix4x4.TRS(sphere.center, Quaternion.identity, scale));
            }
            else if (col is CapsuleCollider capsule)
            {
                DrawCapsule(capsule);
            }
            else if (col is MeshCollider meshCol && meshCol.sharedMesh != null)
            {
                if (showMeshCollider)
                {

                    Material matToUse = meshCol.convex ? yellowMaterial : lineMaterial;

                    if (matToUse != null)
                    {
                        matToUse.SetPass(0);
                        Graphics.DrawMeshNow(meshCol.sharedMesh, matrix);
                    }
                }
            }

            else if (col is CharacterController cc)
            {
                DrawCharacterController(cc);
            }

            /*
            else if (col is TerrainCollider terrain && showTerrainCollider)
            {
                float maxDrawDistance = 100f;
                if (Vector3.Distance(targetCamera.transform.position, terrain.transform.position) < maxDrawDistance)
                {
                    DrawTerrainCollider(terrain, 32);
                }
            }
            */


        }

        void DrawCharacterController(CharacterController cc)
        {
            float radius = cc.radius;
            float height = Mathf.Max(cc.height, 2f * radius);
            Vector3 center = cc.center;
            Matrix4x4 matrix = cc.transform.localToWorldMatrix;


            float scaleFactor = radius / 0.5f;
            float heightScale = (height - 2f * radius) / 1f;

            Vector3 scale = new Vector3(scaleFactor, heightScale, scaleFactor);

            scale.y = height / 2f;

            Matrix4x4 trs = Matrix4x4.TRS(center, Quaternion.identity, scale);
            lineMaterial.SetPass(0);
            Graphics.DrawMeshNow(_capsuleMesh, matrix * trs);
        }


        void DrawCapsule(CapsuleCollider capsule)
        {
            Vector3 dir = Vector3.up;
            Quaternion rot = Quaternion.identity;

            switch (capsule.direction)
            {
                case 0: dir = Vector3.right; rot = Quaternion.Euler(0, 0, 90); break;
                case 1: dir = Vector3.up; break;
                case 2: dir = Vector3.forward; rot = Quaternion.Euler(90, 0, 0); break;
            }

            Matrix4x4 m = capsule.transform.localToWorldMatrix;

            float scaleFactor = capsule.radius / 0.5f;
            float heightScale = (capsule.height - 2f * capsule.radius) / 1f;

            Vector3 scale = new Vector3(scaleFactor, heightScale, scaleFactor);
            scale[capsule.direction] = capsule.height / 2f;
            lineMaterial.SetPass(0);
            Graphics.DrawMeshNow(_capsuleMesh, m * Matrix4x4.TRS(capsule.center, rot, scale));
        }


        void Draw2DCollider(Collider2D col)
        {
            if (col is BoxCollider2D box)
                DrawRect(col.transform.position + (Vector3)box.offset, box.size, col.transform.eulerAngles.z);
            else if (col is CircleCollider2D circle)
                DrawCircle(col.transform.position + (Vector3)circle.offset, circle.radius);
            else if (col is PolygonCollider2D poly)
            {
                for (int p = 0; p < poly.pathCount; p++)
                    DrawPolygon(col.transform, poly.GetPath(p));
            }
            else if (col is EdgeCollider2D edge)
            {
                Vector2[] points = edge.points;
                for (int i = 0; i < points.Length - 1; i++)
                {
                    GL.Vertex(edge.transform.TransformPoint(points[i] + edge.offset));
                    GL.Vertex(edge.transform.TransformPoint(points[i + 1] + edge.offset));
                }
            }
        }

        void DrawRect(Vector2 center, Vector2 size, float angle)
        {
            Vector2 half = size / 2;
            Vector2[] corners = {
            new Vector2(-half.x, -half.y),
            new Vector2( half.x, -half.y),
            new Vector2( half.x,  half.y),
            new Vector2(-half.x,  half.y),
        };

            Matrix4x4 m = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, angle), Vector3.one);
            for (int i = 0; i < 4; i++)
            {
                GL.Vertex(m.MultiplyPoint3x4(corners[i]));
                GL.Vertex(m.MultiplyPoint3x4(corners[(i + 1) % 4]));
            }
        }

        void DrawCircle(Vector2 center, float radius, int segments = 32)
        {
            float angleStep = 2 * Mathf.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * angleStep;
                float a1 = (i + 1) * angleStep;
                Vector2 p0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
                Vector2 p1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
                GL.Vertex(p0);
                GL.Vertex(p1);
            }
        }

        void DrawPolygon(Transform t, Vector2[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                GL.Vertex(t.TransformPoint(points[i]));
                GL.Vertex(t.TransformPoint(points[(i + 1) % points.Length]));
            }
        }

        void DrawWireCollider(Collider col)
        {
            GL.Begin(GL.LINES);
            GL.Color(color_Mesh);

            Matrix4x4 matrix = col.transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                if (lineMaterial != null)
                    lineMaterial.SetPass(0);

                Matrix4x4 m = matrix * Matrix4x4.TRS(box.center, Quaternion.identity, box.size);
                DrawWireCube(m);
            }
            else if (col is SphereCollider sphere)
            {
                Vector3 scaledCenter = Vector3.Scale(sphere.center, col.transform.lossyScale);
                Vector3 worldPosition = col.transform.position + scaledCenter;
                float radius = sphere.radius * MaxAbsScale(col.transform.lossyScale);
                Matrix4x4 m = Matrix4x4.TRS(worldPosition, col.transform.rotation, Vector3.one);
                DrawWireSphere(m, radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                DrawWireCapsule(capsule);
            }
            else if (col is MeshCollider meshCol && meshCol.sharedMesh != null)
            {
                Material matToUse = meshCol.convex ? yellowMaterial : lineMaterial;

                if (matToUse != null)
                {
                    matToUse.SetPass(0);
                    DrawWireMesh(meshCol.sharedMesh, col.transform.localToWorldMatrix);
                }
            }


            else if (col is CharacterController cc)
            {
                DrawWireCharacterController(cc);
            }
            /*
            else if (col is TerrainCollider terrain)
            {
                DrawWireMesh(terrain.terrainData.GetHeightsMesh(), terrain.transform.localToWorldMatrix);
            }*/

            GL.End();
        }

        void DrawWireCapsule(CapsuleCollider capsule)
        {
            Vector3 dir = Vector3.up;
            Quaternion rot = Quaternion.identity;

            switch (capsule.direction)
            {
                case 0: dir = Vector3.right; rot = Quaternion.Euler(0, 0, 90); break;
                case 1: dir = Vector3.up; break;
                case 2: dir = Vector3.forward; rot = Quaternion.Euler(90, 0, 0); break;
            }

            Matrix4x4 matrix = capsule.transform.localToWorldMatrix;
            Vector3 center = capsule.center;
            float radius = capsule.radius;
            float height = capsule.height;

            Vector3 scale = new Vector3(radius * 2f, height / 2f, radius * 2f);

            Matrix4x4 trs = Matrix4x4.TRS(center, rot, scale);

            GL.wireframe = true;
            lineMaterial.SetPass(0);
            Graphics.DrawMeshNow(_capsuleMesh, matrix * trs);
            GL.wireframe = false;
        }




        void DrawWireCube(Matrix4x4 m)
        {
            Vector3[] pts = new Vector3[8];
            float h = 0.5f;
            pts[0] = m.MultiplyPoint3x4(new Vector3(-h, -h, -h));
            pts[1] = m.MultiplyPoint3x4(new Vector3(h, -h, -h));
            pts[2] = m.MultiplyPoint3x4(new Vector3(h, -h, h));
            pts[3] = m.MultiplyPoint3x4(new Vector3(-h, -h, h));
            pts[4] = m.MultiplyPoint3x4(new Vector3(-h, h, -h));
            pts[5] = m.MultiplyPoint3x4(new Vector3(h, h, -h));
            pts[6] = m.MultiplyPoint3x4(new Vector3(h, h, h));
            pts[7] = m.MultiplyPoint3x4(new Vector3(-h, h, h));

            int[] edges = {
        0,1, 1,2, 2,3, 3,0,
        4,5, 5,6, 6,7, 7,4,
        0,4, 1,5, 2,6, 3,7
    };

            for (int i = 0; i < edges.Length; i += 2)
            {
                GL.Vertex(pts[edges[i]]);
                GL.Vertex(pts[edges[i + 1]]);
            }
        }

        void DrawWireCharacterController(CharacterController cc)
        {
            Matrix4x4 matrix = cc.transform.localToWorldMatrix;
            float radius = cc.radius;
            float height = cc.height;
            Vector3 center = cc.center;

            Vector3 scale = new Vector3(radius * 2f, height / 2f, radius * 2f);
            Matrix4x4 trs = Matrix4x4.TRS(center, Quaternion.identity, scale);

            GL.wireframe = true;
            lineMaterial.SetPass(0);
            Graphics.DrawMeshNow(_capsuleMesh, matrix * trs);
            GL.wireframe = false;
        }





        void DrawWireMesh(Mesh mesh, Matrix4x4 matrix)
        {
            var verts = mesh.vertices;
            var tris = mesh.triangles;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v0 = matrix.MultiplyPoint3x4(verts[tris[i]]);
                Vector3 v1 = matrix.MultiplyPoint3x4(verts[tris[i + 1]]);
                Vector3 v2 = matrix.MultiplyPoint3x4(verts[tris[i + 2]]);

                GL.Vertex(v0); GL.Vertex(v1);
                GL.Vertex(v1); GL.Vertex(v2);
                GL.Vertex(v2); GL.Vertex(v0);
            }
        }


        void DrawWireSphere(Matrix4x4 matrix, float radius, int segments = 16)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                for (int i = 0; i < segments; i++)
                {
                    float a0 = Mathf.PI * 2 * i / segments;
                    float a1 = Mathf.PI * 2 * (i + 1) / segments;

                    Vector3 p0 = Vector3.zero;
                    Vector3 p1 = Vector3.zero;

                    switch (axis)
                    {
                        case 0: // XY plane
                            p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * radius;
                            p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * radius;
                            break;
                        case 1: // XZ plane
                            p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius;
                            p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius;
                            break;
                        case 2: // YZ plane
                            p0 = new Vector3(0, Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
                            p1 = new Vector3(0, Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
                            break;
                    }

                    GL.Vertex(matrix.MultiplyPoint3x4(p0));
                    GL.Vertex(matrix.MultiplyPoint3x4(p1));
                }
            }
        }

        /*
        void DrawTerrainCollider(TerrainCollider terrainCol, int lod = 8)
        {
            Terrain terrain = terrainCol.GetComponent<Terrain>();
            if (terrain == null) return;

            TerrainData data = terrain.terrainData;
            if (data == null) return;

            Vector3 size = data.size;
            Vector3 position = terrain.transform.position;
            int width = data.heightmapResolution;
            int height = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, width, height);

            GL.Begin(GL.LINES);
            GL.Color(color);

            for (int x = 0; x < width - lod; x += lod)
            {
                for (int y = 0; y < height - lod; y += lod)
                {
                    Vector3 p1 = position + new Vector3(x / (float)width * size.x, heights[y, x] * size.y, y / (float)height * size.z);
                    Vector3 p2 = position + new Vector3((x + lod) / (float)width * size.x, heights[y, x + lod] * size.y, y / (float)height * size.z);
                    Vector3 p3 = position + new Vector3(x / (float)width * size.x, heights[y + lod, x] * size.y, (y + lod) / (float)height * size.z);

                    GL.Vertex(p1);
                    GL.Vertex(p2);

                    GL.Vertex(p1);
                    GL.Vertex(p3);
                }
            }

            GL.End();
        }
        */


        float MaxAbsScale(Vector3 scale)
        {
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        /*
        Mesh _cachedConvexHullMesh;

        void DrawConvexHullMeshCollider(MeshCollider meshCol)
        {
            if (!meshCol.convex || meshCol.sharedMesh == null)
                return;

            if (_cachedConvexHullMesh == null)
            {
                _cachedConvexHullMesh = ConvexHullGenerator.GenerateConvexHullMesh(meshCol.sharedMesh.vertices);
            }

            if (_cachedConvexHullMesh != null)
            {
                lineMaterial.SetPass(0);
                Graphics.DrawMeshNow(_cachedConvexHullMesh, meshCol.transform.localToWorldMatrix);
            }
        }
        */
    }

}



