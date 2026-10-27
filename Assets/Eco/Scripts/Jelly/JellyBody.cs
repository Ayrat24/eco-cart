using System.Collections.Generic;
using Eco.Scripts.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// The player's jelly body. Builds the body mesh, runs the springs behind its wobble, squash, growth and mouth,
    /// and feeds them to the Eco/Jelly shader, which does the actual deforming. Mirrors that deformation on the CPU
    /// (<see cref="JellyDeformation"/>) so tentacles, the mouth and mounts can be placed on the visible surface.
    /// Lives on a child of the creature root with an identity local pose; its local scale is driven here.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class JellyBody : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private JellyShape shape = new();
        [SerializeField] private float baseScale = 1.2f;

        [Header("Hands")]
        [Tooltip("Body areas tentacles can grow from.")]
        [SerializeField] private List<JellyHandZone> handZones = new()
        {
            new JellyHandZone { minHeight = 0.18f, maxHeight = 0.82f, minAngle = -180f, maxAngle = 180f }
        };
        [Tooltip("Spacing of the candidate spots inside the hand zones, at unit body scale.")]
        [SerializeField] private float anchorStep = 0.12f;
        [Tooltip("Tentacles growing at the same time keep at least this far apart, at unit body scale.")]
        [SerializeField] private float anchorSpacing = 0.45f;
        [Tooltip("No new tentacles within this many degrees either side of the mouth, at any height.")]
        [SerializeField, Range(0f, 180f)] private float mouthHandClearance = 40f;

        [Header("Mouth")]
        [SerializeField, Range(0f, 1f)] private float mouthHeight = 0.58f;
        [Tooltip("Half width, half height, how deep the lips sink when open, smile curve. Unit body scale.")]
        [SerializeField] private Vector4 mouthShape = new(0.32f, 0.13f, 0.22f, 0.25f);
        [SerializeField] private float mouthOpenSpeed = 7f;
        [Tooltip("Slide the mouth around the body toward the camera so it stays in view. Off: it stays at the front.")]
        [SerializeField] private bool mouthFacesCamera = true;
        [Tooltip("Degrees the mouth may stray from straight at the camera; within that it leans toward the front " +
                 "(the direction of travel). 0 = always dead on the camera; above ~60 it starts to turn out of view.")]
        [SerializeField, Range(0f, 90f)] private float mouthCameraLeeway = 35f;
        [Tooltip("Shifts the camera-facing spot around the body, in degrees (positive = the creature's right).")]
        [SerializeField, Range(-90f, 90f)] private float mouthCameraOffset;
        [Tooltip("Degrees per second the mouth slides around the body.")]
        [SerializeField] private float mouthTurnSpeed = 270f;

        [Header("Wobble")]
        [SerializeField] private float leanStiffness = 55f;
        [SerializeField] private float leanDamping = 5f;
        [Tooltip("How far the top lags behind, in metres per m/s² of acceleration.")]
        [SerializeField] private float leanInertia = 0.035f;
        [Tooltip("How far the top leans into the direction of travel, in metres per m/s.")]
        [SerializeField] private float moveLean = 0.014f;
        [SerializeField] private float maxLean = 0.4f;
        [SerializeField] private float squashStiffness = 140f;
        [SerializeField] private float squashDamping = 7f;
        [SerializeField] private float breathAmplitude = 0.015f;
        [SerializeField] private float breathSpeed = 1.7f;
        [SerializeField] private float gaitAmplitude = 0.045f;
        [Tooltip("Metres travelled per squash cycle while moving.")]
        [SerializeField] private float gaitStride = 2.4f;
        [Tooltip("Surface jiggle: amplitude at rest, speed, spatial frequency.")]
        [SerializeField] private Vector3 jiggle = new(0.006f, 2.5f, 1.5f);
        [SerializeField] private float jiggleDecay = 3f;

        [Header("Ripples")]
        [SerializeField] private Vector3 rippleShape = new(2.2f, 0.25f, 2.5f);

        [Header("Growth")]
        [Tooltip("Spring the body grows and shrinks on; how much it grows is set by the stomach.")]
        [SerializeField] private float growthStiffness = 60f;
        [SerializeField] private float growthDamping = 6f;

        [Header("Ground")]
        [SerializeField] private float sink = 0.05f;
        [SerializeField] private float groundSpread = 0.6f;
        [SerializeField] private Transform contactShadow;
        [Tooltip("Contact shadow radius relative to the body radius.")]
        [SerializeField] private float contactShadowSize = 1.3f;

        [Header("Digest")]
        [SerializeField] private float digestFadeSpeed = 2f;

        [Header("Mounts")]
        [Tooltip("Kept on top of the body, tilted with it. Hats go here.")]
        [SerializeField] private Transform hatMount;
        [Tooltip("How far the hat mount sits below the top surface, in metres at base size.")]
        [SerializeField] private float hatMountDepth = 0.12f;

        private const int RippleCount = 4;
        private const float TeleportDistance = 5f;
        private const float MaxStep = 1f / 120f;
        private const string BodyKeyword = "_JELLY_BODY";
        private const string RefractionKeyword = "_JELLY_REFRACTION";

        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int JellyHeightId = Shader.PropertyToID("_JellyHeight");
        private static readonly int WobbleId = Shader.PropertyToID("_Wobble");
        private static readonly int SquashId = Shader.PropertyToID("_Squash");
        private static readonly int SinkId = Shader.PropertyToID("_Sink");
        private static readonly int GroundSpreadId = Shader.PropertyToID("_GroundSpread");
        private static readonly int JiggleId = Shader.PropertyToID("_Jiggle");
        private static readonly int MouthCenterId = Shader.PropertyToID("_MouthCenter");
        private static readonly int MouthRotationId = Shader.PropertyToID("_MouthRotation");
        private static readonly int MouthShapeId = Shader.PropertyToID("_MouthShape");
        private static readonly int RippleAmpId = Shader.PropertyToID("_RippleAmp");
        private static readonly int RippleShapeId = Shader.PropertyToID("_RippleShape");
        private static readonly int DigestId = Shader.PropertyToID("_Digest");
        private static readonly int[] RippleIds =
        {
            Shader.PropertyToID("_Ripple0"), Shader.PropertyToID("_Ripple1"),
            Shader.PropertyToID("_Ripple2"), Shader.PropertyToID("_Ripple3")
        };

        private readonly List<AnchorCandidate> _candidates = new();
        private readonly HashSet<int> _takenAnchors = new();
        private readonly Vector4[] _ripples = new Vector4[RippleCount];
        private Vector4 _rippleAmplitudes;
        private int _nextRipple;

        private Mesh _mesh;
        private Material _material;
        private bool _refraction;
        private JellyDeformParams _deform;
        private Camera _camera;
        private Vector3 _mouthFront;
        private float _mouthProfile;
        private float _mouthAngle;
        private bool _mouthPlaced;

        private Vector3 _lastPosition;
        private Vector3 _velocity;
        private Vector3 _acceleration;
        private bool _hasLastPosition;

        private Vector3 _lean;
        private Vector3 _leanVelocity;
        private float _squash;
        private float _squashVelocity;
        private float _gaitPhase;
        private float _jiggleEnergy;
        private float _growth = 1f;
        private float _growthVelocity;
        private float _growthTarget = 1f;
        private float _mouthOpen;
        private int _mouthRequests;
        private float _digest;
        private bool _digesting;

        public float Growth => _growth;
        public float WorldScale => baseScale * _growth;
        public float Stretch => Mathf.Max(0.2f, 1f + _squash);
        public float RestHeight => shape.Height;

        /// <summary>Volume of the body at its normal size, in cubic metres.</summary>
        public float BaseVolume => shape.Volume * baseScale * baseScale * baseScale;
        public Vector3 Velocity => _velocity;
        public Vector3 Acceleration => _acceleration;

        /// <summary>World position of the middle of the mouth, sunk in as far as it is open.</summary>
        public Vector3 MouthPosition { get; private set; }

        /// <summary>World direction the mouth faces.</summary>
        public Vector3 MouthNormal { get; private set; }

        private struct AnchorCandidate
        {
            public float Angle;
            public float ProfileT;
            public Vector3 Position;
            public Vector3 Normal;
        }

        private void Awake()
        {
            shape.Build();
            _mesh = shape.CreateMesh();
            meshFilter.sharedMesh = _mesh;

            _material = meshRenderer.material;
            _material.EnableKeyword(BodyKeyword);
            _refraction = _material.IsKeywordEnabled(RefractionKeyword);

            _mouthProfile = shape.HeightToProfile(mouthHeight);
            shape.Evaluate(0f, _mouthProfile, out _mouthFront, out _, out _);
            BuildAnchorCandidates();

            for (int i = 0; i < RippleCount; i++)
            {
                _ripples[i] = new Vector4(0f, 0f, 0f, -1000f);
            }

            _material.SetFloat(JellyHeightId, shape.Height);
            _material.SetVector(MouthShapeId, mouthShape);
            _material.SetVector(RippleShapeId, rippleShape);

            UpdateDeformParams();
            UpdateMouthPose();
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }

            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        private void LateUpdate()
        {
            using var marker = EcoProfilerMarkers.JellyBody.Auto();

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            TrackMotion(deltaTime);

            int steps = Mathf.CeilToInt(Mathf.Min(deltaTime, 1f / 15f) / MaxStep);
            float step = Mathf.Min(deltaTime, 1f / 15f) / steps;
            for (int i = 0; i < steps; i++)
            {
                Simulate(step);
            }

            _mouthOpen = Mathf.MoveTowards(_mouthOpen, _mouthRequests > 0 ? 1f : 0f, mouthOpenSpeed * deltaTime);
            _digest = Mathf.MoveTowards(_digest, _digesting ? 1f : 0f, digestFadeSpeed * deltaTime);
            _jiggleEnergy *= Mathf.Exp(-jiggleDecay * deltaTime);

            transform.localScale = Vector3.one * WorldScale;

            UpdateMouthAngle(deltaTime);
            UpdateDeformParams();
            UpdateMouthPose();
            UpdateRefraction();
            ApplyToMaterial();
            UpdateMounts();
        }

        /// <summary>
        /// A fresh body mesh from the current shape settings. The body builds its own on Awake; this is for
        /// baking an edit-time preview.
        /// </summary>
        public Mesh BuildMesh()
        {
            shape.Build();
            return shape.CreateMesh();
        }

        /// <summary>Size to grow to, relative to normal (1). The body springs there with a little overshoot.</summary>
        public void SetGrowth(float growth)
        {
            _growthTarget = Mathf.Max(0.1f, growth);
        }

        public void SetDigesting(bool digesting)
        {
            _digesting = digesting;
        }

        public void OpenMouth()
        {
            _mouthRequests++;
        }

        public void CloseMouth()
        {
            _mouthRequests = Mathf.Max(0, _mouthRequests - 1);
        }

        /// <summary>Kicks the body vertically: negative squashes it down, positive stretches it up.</summary>
        public void Squash(float impulse)
        {
            _squashVelocity += impulse;
        }

        /// <summary>Sends a ripple across the surface from near a world point.</summary>
        public void Poke(Vector3 worldPoint, float strength)
        {
            var rest = transform.InverseTransformPoint(worldPoint);
            _ripples[_nextRipple] = new Vector4(rest.x, rest.y, rest.z, Time.timeSinceLevelLoad);
            _rippleAmplitudes[_nextRipple] = strength;
            _nextRipple = (_nextRipple + 1) % RippleCount;
            _jiggleEnergy += strength * 20f;
        }

        /// <summary>
        /// Picks a free spot in the hand zones facing the target to grow a tentacle from. Release it with
        /// <see cref="ReleaseAnchor"/> once the tentacle is gone.
        /// </summary>
        public bool TryTakeAnchor(Vector3 worldTarget, out JellyAnchor anchor)
        {
            var target = transform.InverseTransformPoint(worldTarget);
            float bestScore = float.MinValue;
            int best = -1;

            for (int i = 0; i < _candidates.Count; i++)
            {
                var candidate = _candidates[i];
                if (IsNearTakenAnchor(candidate.Position) || IsInMouthStrip(candidate.Angle))
                {
                    continue;
                }

                var toTarget = target - candidate.Position;
                float distance = toTarget.magnitude;
                float facing = distance > 1e-4f ? Vector3.Dot(candidate.Normal, toTarget / distance) : 1f;
                float score = facing - distance * 0.15f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            if (best < 0)
            {
                anchor = default;
                return false;
            }

            _takenAnchors.Add(best);
            anchor = new JellyAnchor(best, _candidates[best].Angle, _candidates[best].ProfileT);
            return true;
        }

        public void ReleaseAnchor(JellyAnchor anchor)
        {
            _takenAnchors.Remove(anchor.Index);
        }

        public void GetSurfacePose(JellyAnchor anchor, out Vector3 position, out Vector3 normal)
        {
            GetSurfacePose(anchor.Angle, anchor.ProfileT, out position, out normal);
        }

        /// <summary>Deformed surface point at degrees around the body (0 = front) and a height (0 = ground, 1 = top).</summary>
        public void GetSurfacePoseAt(float angleDegrees, float height01, out Vector3 position, out Vector3 normal)
        {
            GetSurfacePose(angleDegrees * Mathf.Deg2Rad, shape.HeightToProfile(height01), out position, out normal);
        }

        /// <summary>Rest-shape radius at a height (0 = ground, 1 = top), at unit body scale.</summary>
        public float RestRadiusAt(float height01)
        {
            return shape.RadiusAtHeight(height01);
        }

        /// <summary>Where a point of the rest body (unit scale, object space) is now, in the parent's space.</summary>
        public Vector3 RestToParent(Vector3 rest)
        {
            return transform.localPosition + transform.localRotation * (JellyDeformation.Deform(rest, Vector3.zero, _deform) * WorldScale);
        }

        private void GetSurfacePose(float angle, float profileT, out Vector3 position, out Vector3 normal)
        {
            shape.Evaluate(angle, profileT, out var rest, out var restNormal, out var tangent);
            JellyDeformation.DeformSurface(rest, restNormal, tangent, _deform, out var local, out var localNormal);
            position = transform.TransformPoint(local);
            normal = transform.TransformDirection(localNormal).normalized;
        }

        // The vertical strip of the body the mouth is on
        private bool IsInMouthStrip(float angle)
        {
            return Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, _mouthAngle)) < mouthHandClearance;
        }

        private bool IsNearTakenAnchor(Vector3 restPosition)
        {
            foreach (int index in _takenAnchors)
            {
                if ((_candidates[index].Position - restPosition).sqrMagnitude < anchorSpacing * anchorSpacing)
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildAnchorCandidates()
        {
            _candidates.Clear();
            int heightSteps = Mathf.Max(1, Mathf.CeilToInt(shape.Height / anchorStep));
            for (int h = 0; h <= heightSteps; h++)
            {
                float height01 = (float)h / heightSteps;
                float profileT = shape.HeightToProfile(height01);
                float radius = Mathf.Max(shape.RadiusAtHeight(height01), anchorStep);
                int angleSteps = Mathf.Max(4, Mathf.CeilToInt(Mathf.PI * 2f * radius / anchorStep));

                for (int a = 0; a < angleSteps; a++)
                {
                    float angle = -Mathf.PI + Mathf.PI * 2f * a / angleSteps;
                    if (!InHandZone(height01, angle * Mathf.Rad2Deg))
                    {
                        continue;
                    }

                    shape.Evaluate(angle, profileT, out var position, out var normal, out _);
                    _candidates.Add(new AnchorCandidate
                        { Angle = angle, ProfileT = profileT, Position = position, Normal = normal });
                }
            }
        }

        private bool InHandZone(float height01, float angleDegrees)
        {
            foreach (var zone in handZones)
            {
                if (zone.Contains(height01, angleDegrees))
                {
                    return true;
                }
            }

            return false;
        }

        private void TrackMotion(float deltaTime)
        {
            var position = transform.position;
            if (!_hasLastPosition || (position - _lastPosition).sqrMagnitude > TeleportDistance * TeleportDistance)
            {
                // First frame or a teleport (spawn, world switch): don't turn the jump into a lurch
                _hasLastPosition = true;
                _lastPosition = position;
                _velocity = Vector3.zero;
                _acceleration = Vector3.zero;
                return;
            }

            var velocity = (position - _lastPosition) / deltaTime;
            var acceleration = (velocity - _velocity) / deltaTime;
            _lastPosition = position;
            _velocity = velocity;
            _acceleration = Vector3.Lerp(_acceleration, acceleration, 1f - Mathf.Exp(-20f * deltaTime));
        }

        private void Simulate(float dt)
        {
            // Lean: the top is a mass on a spring that trails acceleration and leans into motion
            var flatVelocity = new Vector3(_velocity.x, 0f, _velocity.z);
            var flatAcceleration = new Vector3(_acceleration.x, 0f, _acceleration.z);
            var leanTarget = flatVelocity * moveLean - flatAcceleration * leanInertia;
            var leanForce = (leanTarget - _lean) * leanStiffness - _leanVelocity * leanDamping;
            _leanVelocity += leanForce * dt;
            _lean += _leanVelocity * dt;
            _lean = Vector3.ClampMagnitude(_lean, maxLean * WorldScale);

            // Squash: breathing at rest, a squishy gait while moving
            float speed = flatVelocity.magnitude;
            _gaitPhase += speed * dt / gaitStride * Mathf.PI * 2f;
            float gait = Mathf.Sin(_gaitPhase) * gaitAmplitude * Mathf.Clamp01(speed / 4f);
            float breath = Mathf.Sin(Time.timeSinceLevelLoad * breathSpeed) * breathAmplitude;
            float squashForce = (breath + gait - _squash) * squashStiffness - _squashVelocity * squashDamping;
            _squashVelocity += squashForce * dt;
            _squash += _squashVelocity * dt;

            // Growth overshoots a little, so every swallow and every dissolve wobbles
            float growthForce = (_growthTarget - _growth) * growthStiffness - _growthVelocity * growthDamping;
            _growthVelocity += growthForce * dt;
            _growth += _growthVelocity * dt;
        }

        private void UpdateDeformParams()
        {
            _deform.Height = shape.Height;
            _deform.Wobble = transform.InverseTransformVector(_lean);
            _deform.Squash = _squash;
            _deform.Sink = sink;
            _deform.GroundSpread = groundSpread;
            _deform.MouthCenter = _mouthFront;
            _deform.MouthAngle = _mouthAngle * Mathf.Deg2Rad;
            _deform.MouthOpen = Smooth(_mouthOpen);
            _deform.MouthShape = mouthShape;
        }

        private void UpdateMouthPose()
        {
            GetSurfacePose(_mouthAngle * Mathf.Deg2Rad, _mouthProfile, out var position, out var normal);
            MouthPosition = position;
            MouthNormal = normal;
        }

        private void UpdateMouthAngle(float deltaTime)
        {
            float target = 0f;
            if (mouthFacesCamera && TryGetCameraAngle(out float cameraAngle))
            {
                // As close to the front as the leeway allows. With the front out of reach and the camera nearly
                // straight behind, keep to the side the mouth is already on so it doesn't flip across
                float toFront = Mathf.DeltaAngle(cameraAngle, 0f);
                float offset = Mathf.Abs(toFront) <= mouthCameraLeeway
                    ? toFront
                    : Mathf.Sign(Mathf.Abs(toFront) > 150f ? Mathf.DeltaAngle(cameraAngle, _mouthAngle) : toFront)
                      * mouthCameraLeeway;
                target = AvoidTakenAnchors(cameraAngle + offset, cameraAngle);
            }

            _mouthAngle = _mouthPlaced
                ? Mathf.MoveTowardsAngle(_mouthAngle, target, mouthTurnSpeed * deltaTime)
                : target;
            _mouthPlaced = true;
        }

        // Angle of the camera around the body, in degrees (0 = front)
        private bool TryGetCameraAngle(out float angle)
        {
            if (_camera == null || !_camera.isActiveAndEnabled)
            {
                _camera = Camera.main;
            }

            angle = 0f;
            if (_camera == null)
            {
                return false;
            }

            var toCamera = transform.InverseTransformDirection(_camera.transform.position - transform.position);
            if (toCamera.x * toCamera.x + toCamera.z * toCamera.z < 1e-6f)
            {
                return false;
            }

            angle = Mathf.Atan2(toCamera.x, toCamera.z) * Mathf.Rad2Deg + mouthCameraOffset;
            return true;
        }

        // Moves the mouth off tentacles that are already out (the spade's, say), as long as it stays in the leeway
        private float AvoidTakenAnchors(float target, float cameraAngle)
        {
            foreach (int index in _takenAnchors)
            {
                float anchorAngle = _candidates[index].Angle * Mathf.Rad2Deg;
                float offset = Mathf.DeltaAngle(anchorAngle, target);
                if (Mathf.Abs(offset) >= mouthHandClearance)
                {
                    continue;
                }

                float side = offset >= 0f ? 1f : -1f;
                float nearSide = anchorAngle + side * mouthHandClearance;
                float farSide = anchorAngle - side * mouthHandClearance;
                if (Mathf.Abs(Mathf.DeltaAngle(cameraAngle, nearSide)) <= mouthCameraLeeway)
                {
                    target = nearSide;
                }
                else if (Mathf.Abs(Mathf.DeltaAngle(cameraAngle, farSide)) <= mouthCameraLeeway)
                {
                    target = farSide;
                }
            }

            return target;
        }

        private void UpdateRefraction()
        {
            bool refraction = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline
                              && pipeline.supportsCameraOpaqueTexture && pipeline.supportsCameraDepthTexture;
            if (refraction == _refraction)
            {
                return;
            }

            _refraction = refraction;
            if (refraction)
            {
                _material.EnableKeyword(RefractionKeyword);
            }
            else
            {
                _material.DisableKeyword(RefractionKeyword);
            }
        }

        private void ApplyToMaterial()
        {
            float speed = new Vector2(_velocity.x, _velocity.z).magnitude;
            float jiggleAmplitude = jiggle.x * (1f + _jiggleEnergy + speed * 0.15f);

            _material.SetFloat(ThicknessId, 2f * shape.MaxRadius * WorldScale);
            _material.SetVector(WobbleId, _deform.Wobble);
            _material.SetFloat(SquashId, _squash);
            _material.SetFloat(SinkId, sink);
            _material.SetFloat(GroundSpreadId, groundSpread);
            _material.SetVector(JiggleId, new Vector4(jiggleAmplitude, jiggle.y, jiggle.z, 0f));
            _material.SetVector(MouthCenterId, new Vector4(_mouthFront.x, _mouthFront.y, _mouthFront.z, _deform.MouthOpen));
            _material.SetVector(MouthRotationId,
                new Vector4(Mathf.Sin(_deform.MouthAngle), Mathf.Cos(_deform.MouthAngle), 0f, 0f));
            _material.SetFloat(DigestId, _digest);
            _material.SetVector(RippleAmpId, _rippleAmplitudes);
            for (int i = 0; i < RippleCount; i++)
            {
                _material.SetVector(RippleIds[i], _ripples[i]);
            }
        }

        private void UpdateMounts()
        {
            if (hatMount != null)
            {
                GetSurfacePose(0f, 1f, out var top, out var up);
                hatMount.SetPositionAndRotation(top - up * (hatMountDepth * _growth),
                    Quaternion.FromToRotation(transform.up, up) * transform.rotation);
            }

            if (contactShadow != null)
            {
                float radius = shape.MaxRadius * WorldScale / Mathf.Sqrt(Stretch) * contactShadowSize;
                var position = transform.position;
                contactShadow.SetPositionAndRotation(new Vector3(position.x, position.y + 0.03f, position.z),
                    Quaternion.Euler(90f, 0f, 0f));
                contactShadow.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            }
        }

        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                shape.Build();
                _mouthProfile = shape.HeightToProfile(mouthHeight);
                shape.Evaluate(0f, _mouthProfile, out _mouthFront, out _, out _);
                BuildAnchorCandidates();
            }

            // Spots tentacles can grow from; grey ones are in the mouth's strip right now
            float size = 0.025f * (Application.isPlaying ? WorldScale : baseScale);
            foreach (var candidate in _candidates)
            {
                var position = Application.isPlaying
                    ? transform.TransformPoint(candidate.Position)
                    : transform.position + transform.rotation * (candidate.Position * baseScale);
                Gizmos.color = IsInMouthStrip(candidate.Angle)
                    ? new Color(0.5f, 0.5f, 0.5f, 0.6f)
                    : new Color(0.3f, 1f, 0.6f, 0.8f);
                Gizmos.DrawSphere(position, size);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Application.isPlaying
                ? MouthPosition
                : transform.position + transform.rotation * (_mouthFront * baseScale), size * 3f);
        }
#endif
    }
}
