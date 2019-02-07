//
// Fingers Lite Gestures
// (c) 2015 Digital Ruby, LLC
// http://www.digitalruby.com
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// Please see license.txt file
// //


using UnityEngine;
using System.Collections;

namespace DigitalRubyShared
{
	public class DemoAsteroidScript : MonoBehaviour
	{
		private void Start ()
		{
		
		}

		private void Update ()
		{
			
		}

		private void OnBecameInvisible()
		{
			GameObject.Destroy(gameObject);
		}
	}
}