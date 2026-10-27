using UnityEngine;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// Tuning for the jelly's mouth (see <see cref="JellyBody"/>): where it sits, how it follows the camera, how it
    /// looks closed and how wide and deep it opens for the item going in. Sizes are at unit body scale, where the
    /// body's radius is about 1 and it grows with the body.
    /// </summary>
    [CreateAssetMenu(menuName = "Eco/Jelly Mouth Settings", fileName = "MouthSettings")]
    public class JellyMouthSettings : ScriptableObject
    {
        [Header("Placement")]
        [Tooltip("Height on the body: 0 = ground, 1 = top.")]
        [SerializeField, Range(0f, 1f)] private float height = 0.58f;
        [Tooltip("Slide the mouth around the body toward the camera so it stays in view. Off: it stays at the front.")]
        [SerializeField] private bool facesCamera = true;
        [Tooltip("Degrees the mouth may stray from straight at the camera; within that it leans toward the front " +
                 "(the direction of travel). 0 = always dead on the camera; above ~60 it starts to turn out of view.")]
        [SerializeField, Range(0f, 90f)] private float cameraLeeway = 35f;
        [Tooltip("Shifts the camera-facing spot around the body, in degrees (positive = the creature's right).")]
        [SerializeField, Range(-90f, 90f)] private float cameraOffset;
        [Tooltip("Degrees per second the mouth slides around the body.")]
        [SerializeField, Min(0f)] private float turnSpeed = 270f;
        [Tooltip("No new tentacles within this many degrees either side of the mouth, at any height. Widens on " +
                 "its own while the mouth is open wider than that.")]
        [SerializeField, Range(0f, 180f)] private float handClearance = 40f;

        [Header("Closed")]
        [Tooltip("Half width and half height of the mouth at rest.")]
        [SerializeField] private Vector2 size = new(0.32f, 0.13f);
        [Tooltip("How far the corners curve up.")]
        [SerializeField] private float smile = 0.25f;
        [Tooltip("Thickness of the closed mouth line, as a share of the half height.")]
        [SerializeField, Range(0f, 1f)] private float closedLine = 0.22f;
        [Tooltip("Colour of the mouth; alpha is how solid it looks.")]
        [SerializeField] private Color color = new(0.02f, 0.12f, 0.12f, 0.9f);

        [Header("Opening")]
        [Tooltip("The open mouth is this many times the size of the item going in (its two longest sides at " +
                 "swallowed size), and never smaller than at rest.")]
        [SerializeField, Min(0.5f)] private float itemMargin = 1.3f;
        [Tooltip("Largest half width and half height the open mouth gets.")]
        [SerializeField] private Vector2 maxOpenSize = new(0.8f, 0.55f);
        [Tooltip("How far the mouth sinks into the body when fully open.")]
        [SerializeField, Min(0f)] private float depth = 0.5f;
        [Tooltip("How much darker the middle of the open mouth gets, so it reads as a deep hole.")]
        [SerializeField, Range(0f, 1f)] private float throatDarkness = 0.75f;
        [Tooltip("Brightness of the lip rim around the open mouth.")]
        [SerializeField, Range(0f, 2f)] private float lipHighlight = 0.6f;
        [Tooltip("How fast the mouth opens, closes and resizes (per second).")]
        [SerializeField, Min(0.1f)] private float openSpeed = 6f;

        public float Height => height;
        public bool FacesCamera => facesCamera;
        public float CameraLeeway => cameraLeeway;
        public float CameraOffset => cameraOffset;
        public float TurnSpeed => turnSpeed;
        public float HandClearance => handClearance;
        public Vector2 Size => size;
        public float Smile => smile;
        public float ClosedLine => closedLine;
        public Color Color => color;
        public float ItemMargin => itemMargin;
        public Vector2 MaxOpenSize => maxOpenSize;
        public float Depth => depth;
        public float ThroatDarkness => throatDarkness;
        public float LipHighlight => lipHighlight;
        public float OpenSpeed => openSpeed;
    }
}
