//
// Fingers Lite Gestures
// (c) 2015 Digital Ruby, LLC
// http://www.digitalruby.com
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// Please see license.txt file
// 


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DigitalRubyShared
{
    public class DemoScript : MonoBehaviour
    {
        public GameObject Earth;
        public UnityEngine.UI.Text dpiLabel;
        public UnityEngine.UI.Text bottomLabel;
        public GameObject AsteroidPrefab;
        public Material LineMaterial;

        private Sprite[] asteroids;

        private TapGestureRecognizer tapGesture;
        private TapGestureRecognizer doubleTapGesture;
        private TapGestureRecognizer tripleTapGesture;
        private SwipeGestureRecognizer swipeGesture;
        private PanGestureRecognizer panGesture;
        private ScaleGestureRecognizer scaleGesture;
        private RotateGestureRecognizer rotateGesture;
        private LongPressGestureRecognizer longPressGesture;

        private float nextAsteroid = float.MinValue;
        private GameObject draggingAsteroid;

        private readonly List<Vector3> swipeLines = new List<Vector3>();

        private void DebugText(string text, params object[] format)
        {
            //bottomLabel.text = string.Format(text, format);
            Debug.Log(string.Format(text, format));
        }

        private GameObject CreateAsteroid(float screenX, float screenY)
        {
            GameObject o = GameObject.Instantiate(AsteroidPrefab) as GameObject;
            o.name = "Asteroid";
            SpriteRenderer r = o.GetComponent<SpriteRenderer>();
            r.sprite = asteroids[UnityEngine.Random.Range(0, asteroids.Length - 1)];

            if (screenX == float.MinValue || screenY == float.MinValue)
            {
                float x = UnityEngine.Random.Range(Camera.main.rect.min.x, Camera.main.rect.max.x);
                float y = UnityEngine.Random.Range(Camera.main.rect.min.y, Camera.main.rect.max.y);
                Vector3 pos = new Vector3(x, y, 0.0f);
                pos = Camera.main.ViewportToWorldPoint(pos);
                pos.z = o.transform.position.z;
                o.transform.position = pos;
            }
            else
            {
                Vector3 pos = new Vector3(screenX, screenY, 0.0f);
                pos = Camera.main.ScreenToWorldPoint(pos);
                pos.z = o.transform.position.z;
                o.transform.position = pos;
            }

            o.GetComponent<Rigidbody2D>().angularVelocity = UnityEngine.Random.Range(0.0f, 30.0f);
            Vector2 velocity = UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(0, 30.0f);
            o.GetComponent<Rigidbody2D>().velocity = velocity;
            float scale = UnityEngine.Random.Range(1.0f, 4.0f);
            o.transform.localScale = new Vector3(scale, scale, 1.0f);
            o.GetComponent<Rigidbody2D>().mass *= (scale * scale);

            return o;
        }

        private void RemoveAsteroids(float screenX, float screenY, float radius)
        {
            Vector3 pos = new Vector3(screenX, screenY, 0.0f);
            pos = Camera.main.ScreenToWorldPoint(pos);

            RaycastHit2D[] hits = Physics2D.CircleCastAll(pos, radius, Vector2.zero);
            foreach (RaycastHit2D h in hits)
            {
                GameObject.Destroy(h.transform.gameObject);
            }
        }

        private void BeginDrag(float screenX, float screenY)
        {
            Vector3 pos = new Vector3(screenX, screenY, 0.0f);
            pos = Camera.main.ScreenToWorldPoint(pos);
            RaycastHit2D hit = Physics2D.CircleCast(pos, 10.0f, Vector2.zero);
            if (hit.transform != null && hit.transform.gameObject.name == "Asteroid")
            {
                draggingAsteroid = hit.transform.gameObject;
                draggingAsteroid.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                draggingAsteroid.GetComponent<Rigidbody2D>().angularVelocity = 0.0f;
            }
            else
            {
                longPressGesture.Reset();
            }
        }

        private void DragTo(float screenX, float screenY)
        {
            if (draggingAsteroid == null)
            {
                return;
            }

            Vector3 pos = new Vector3(screenX, screenY, 0.0f);
            pos = Camera.main.ScreenToWorldPoint(pos);
            draggingAsteroid.GetComponent<Rigidbody2D>().MovePosition(pos);
        }

        private void EndDrag(float velocityXScreen, float velocityYScreen)
        {
            if (draggingAsteroid == null)
            {
                return;
            }

            Vector3 origin = Camera.main.ScreenToWorldPoint(Vector3.zero);
            Vector3 end = Camera.main.ScreenToWorldPoint(new Vector3(velocityXScreen, velocityYScreen, 0.0f));
            Vector3 velocity = (end - origin);
            draggingAsteroid.GetComponent<Rigidbody2D>().velocity = velocity;
            draggingAsteroid.GetComponent<Rigidbody2D>().angularVelocity = UnityEngine.Random.Range(5.0f, 45.0f);
            draggingAsteroid = null;

            DebugText("Long tap flick velocity: {0}", velocity);
        }

        private void HandleSwipe(float endX, float endY)
        {
            Vector2 start = new Vector2(swipeGesture.StartFocusX, swipeGesture.StartFocusY);
            Vector3 startWorld = Camera.main.ScreenToWorldPoint(start);
            Vector3 endWorld = Camera.main.ScreenToWorldPoint(new Vector2(endX, endY));
            float distance = Vector3.Distance(startWorld, endWorld);
            startWorld.z = endWorld.z = 0.0f;

            swipeLines.Add(startWorld);
            swipeLines.Add(endWorld);

            if (swipeLines.Count > 4)
            {
                swipeLines.RemoveRange(0, swipeLines.Count - 4);
            }

            RaycastHit2D[] collisions = Physics2D.CircleCastAll(startWorld, 10.0f, (endWorld - startWorld).normalized, distance);

            if (collisions.Length != 0)
            {
                Debug.Log("Raycast hits: " + collisions.Length + ", start: " + startWorld + ", end: " + endWorld + ", distance: " + distance);

                Vector3 origin = Camera.main.ScreenToWorldPoint(Vector3.zero);
                Vector3 end = Camera.main.ScreenToWorldPoint(new Vector3(swipeGesture.VelocityX, swipeGesture.VelocityY, Camera.main.nearClipPlane));
                Vector3 velocity = (end - origin);
                Vector2 force = velocity * 500.0f;

                foreach (RaycastHit2D h in collisions)
                {
                    h.rigidbody.AddForceAtPosition(force, h.point);
                }
            }
        }

        private void TapGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Ended)
            {
                DebugText("Tapped at {0}, {1}", gesture.FocusX, gesture.FocusY);
                CreateAsteroid(gesture.FocusX, gesture.FocusY);
            }
        }

        private void CreateTapGesture()
        {
            tapGesture = new TapGestureRecognizer();
            tapGesture.StateUpdated += TapGestureCallback;
            tapGesture.RequireGestureRecognizerToFail = doubleTapGesture;
            FingersScript.Instance.AddGesture(tapGesture);
        }

        private void DoubleTapGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Ended)
            {
                DebugText("Double tapped at {0}, {1}", gesture.FocusX, gesture.FocusY);
                RemoveAsteroids(gesture.FocusX, gesture.FocusY, 16.0f);
            }
        }

        private void CreateDoubleTapGesture()
        {
            doubleTapGesture = new TapGestureRecognizer();
            doubleTapGesture.NumberOfTapsRequired = 2;
            doubleTapGesture.StateUpdated += DoubleTapGestureCallback;
            doubleTapGesture.RequireGestureRecognizerToFail = tripleTapGesture;
            FingersScript.Instance.AddGesture(doubleTapGesture);
        }

        private void SwipeGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Ended)
            {
                HandleSwipe(gesture.FocusX, gesture.FocusY);
                DebugText("Swiped from {0},{1} to {2},{3}; velocity: {4}, {5}", gesture.StartFocusX, gesture.StartFocusY, gesture.FocusX, gesture.FocusY, swipeGesture.VelocityX, swipeGesture.VelocityY);
            }
        }

        private void CreateSwipeGesture()
        {
            swipeGesture = new SwipeGestureRecognizer();
            swipeGesture.Direction = SwipeGestureRecognizerDirection.Any;
            swipeGesture.StateUpdated += SwipeGestureCallback;
            swipeGesture.DirectionThreshold = 1.0f; // allow a swipe, regardless of slope
            FingersScript.Instance.AddGesture(swipeGesture);
        }

        private void PanGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Executing)
            {
                DebugText("Panned, Location: {0}, {1}, Delta: {2}, {3}", gesture.FocusX, gesture.FocusY, gesture.DeltaX, gesture.DeltaY);

                float deltaX = panGesture.DeltaX / 25.0f;
                float deltaY = panGesture.DeltaY / 25.0f;
                Vector3 pos = Earth.transform.position;
                pos.x += deltaX;
                pos.y += deltaY;
                Earth.transform.position = pos;
            }
        }

        private void CreatePanGesture()
        {
            panGesture = new PanGestureRecognizer();
            panGesture.MinimumNumberOfTouchesToTrack = 2;
            panGesture.StateUpdated += PanGestureCallback;
            FingersScript.Instance.AddGesture(panGesture);
        }

        private void ScaleGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Executing)
            {
                DebugText("Scaled: {0}, Focus: {1}, {2}", scaleGesture.ScaleMultiplier, scaleGesture.FocusX, scaleGesture.FocusY);
                Earth.transform.localScale *= scaleGesture.ScaleMultiplier;
            }
        }

        private void CreateScaleGesture()
        {
            scaleGesture = new ScaleGestureRecognizer();
            scaleGesture.StateUpdated += ScaleGestureCallback;
            FingersScript.Instance.AddGesture(scaleGesture);
        }

        private void RotateGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Executing)
            {
                Earth.transform.Rotate(0.0f, 0.0f, rotateGesture.RotationRadiansDelta * Mathf.Rad2Deg);
            }
        }

        private void CreateRotateGesture()
        {
            rotateGesture = new RotateGestureRecognizer();
            rotateGesture.StateUpdated += RotateGestureCallback;
            FingersScript.Instance.AddGesture(rotateGesture);
        }

        private void LongPressGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Began)
            {
                DebugText("Long press began: {0}, {1}", gesture.FocusX, gesture.FocusY);
                BeginDrag(gesture.FocusX, gesture.FocusY);
            }
            else if (gesture.State == GestureRecognizerState.Executing)
            {
                DebugText("Long press moved: {0}, {1}", gesture.FocusX, gesture.FocusY);
                DragTo(gesture.FocusX, gesture.FocusY);
            }
            else if (gesture.State == GestureRecognizerState.Ended)
            {
                DebugText("Long press end: {0}, {1}, delta: {2}, {3}", gesture.FocusX, gesture.FocusY, gesture.DeltaX, gesture.DeltaY);
                EndDrag(longPressGesture.VelocityX, longPressGesture.VelocityY);
            }
        }

        private void CreateLongPressGesture()
        {
            longPressGesture = new LongPressGestureRecognizer();
            longPressGesture.MaximumNumberOfTouchesToTrack = 1;
            longPressGesture.StateUpdated += LongPressGestureCallback;
            FingersScript.Instance.AddGesture(longPressGesture);
        }

        private void PlatformSpecificViewTapUpdated(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Ended)
            {
                Debug.Log("You triple tapped the platform specific label!");
            }
        }

        private void CreatePlatformSpecificViewTripleTapGesture()
        {
            tripleTapGesture = new TapGestureRecognizer();
            tripleTapGesture.StateUpdated += PlatformSpecificViewTapUpdated;
            tripleTapGesture.NumberOfTapsRequired = 3;
            tripleTapGesture.PlatformSpecificView = bottomLabel.gameObject;
            FingersScript.Instance.AddGesture(tripleTapGesture);
        }

        private static bool? CaptureGestureHandler(GameObject obj)
        {
            // I've named objects PassThrough* if the gesture should pass through and NoPass* if the gesture should be gobbled up, everything else gets default behavior
            if (obj.name.StartsWith("PassThrough"))
            {
                // allow the pass through for any element named "PassThrough*"
                return false;
            }
            else if (obj.name.StartsWith("NoPass"))
            {
                // prevent the gesture from passing through, this is done on some of the buttons and the bottom text so that only
                // the triple tap gesture can tap on it
                return true;
            }

            // fall-back to default behavior for anything else
            return null;
        }

        private void Start()
        {
            asteroids = Resources.LoadAll<Sprite>("FingersAsteroids");

            // don't reorder the creation of these :)
            CreatePlatformSpecificViewTripleTapGesture();
            CreateDoubleTapGesture();
            CreateTapGesture();
            CreateSwipeGesture();
            CreatePanGesture();
            CreateScaleGesture();
            CreateRotateGesture();
            CreateLongPressGesture();

            // pan, scale and rotate can all happen simultaneously
            panGesture.AllowSimultaneousExecution(scaleGesture);
            panGesture.AllowSimultaneousExecution(rotateGesture);
            scaleGesture.AllowSimultaneousExecution(rotateGesture);

            // prevent the one special no-pass button from passing through,
            //  even though the parent scroll view allows pass through (see FingerScript.PassThroughObjects)
            FingersScript.Instance.CaptureGestureHandler = CaptureGestureHandler;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReloadDemoScene();
            }
        }

        private void LateUpdate()
        {
            if (Time.timeSinceLevelLoad > nextAsteroid)
            {
                nextAsteroid = Time.timeSinceLevelLoad + UnityEngine.Random.Range(1.0f, 4.0f);
                CreateAsteroid(float.MinValue, float.MinValue);
            }

            int touchCount = Input.touchCount;
            if (FingersScript.Instance.TreatMousePointerAsFinger && Input.mousePresent)
            {
                touchCount += (Input.GetMouseButton(0) ? 1 : 0);
                touchCount += (Input.GetMouseButton(1) ? 1 : 0);
                touchCount += (Input.GetMouseButton(2) ? 1 : 0);
            }
            string touchIds = string.Empty;
            int gestureTouchCount = 0;
            foreach (GestureRecognizer g in FingersScript.Instance.Gestures)
            {
                gestureTouchCount += g.CurrentTrackedTouches.Count;
            }
            foreach (GestureTouch t in FingersScript.Instance.CurrentTouches)
            {
                touchIds += ":" + t.Id + ":";
            }

            dpiLabel.text = "Dpi: " + DeviceInfo.PixelsPerInch + System.Environment.NewLine +
                "Width: " + Screen.width + System.Environment.NewLine +
                "Height: " + Screen.height + System.Environment.NewLine +
                "Touches: " + FingersScript.Instance.CurrentTouches.Count + " (" + gestureTouchCount + "), ids" + touchIds + System.Environment.NewLine;
        }

        private void OnRenderObject()
        {
            if (LineMaterial == null)
            {
                return;
            }

            GL.PushMatrix();
            LineMaterial.SetPass(0);
            GL.LoadProjectionMatrix(Camera.main.projectionMatrix);
            GL.Begin(GL.LINES);
            for (int i = 0; i < swipeLines.Count; i++)
            {
                GL.Color(Color.yellow);
                GL.Vertex(swipeLines[i]);
                GL.Vertex(swipeLines[++i]);
            }
            GL.End();
            GL.PopMatrix();
        }

        public void ReloadDemoScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
