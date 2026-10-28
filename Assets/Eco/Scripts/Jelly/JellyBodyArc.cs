using UnityEngine;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// A smooth arc between two points that goes round the jelly body rather than through it: it sweeps around the
    /// body's vertical axis (on a chosen side when the points are on opposite sides), bows outward the further it
    /// has to go round, lifts in the middle and keeps a clearance from the surface. Used for tentacle arms and for
    /// the path an item is carried along.
    /// </summary>
    public struct JellyBodyArc
    {
        // Fraction of the arc at each end over which the clearance fades in, so the ends land exactly on their points
        private const float EndFade = 0.15f;
        // Beyond this sweep the points count as being on opposite sides and the side decides the way round
        private const float OppositeSweep = 2.1f;

        private JellyBody _body;
        private float _startAngle;
        private float _sweep;
        private float _startRadius;
        private float _endRadius;
        private float _startHeight;
        private float _endHeight;
        private float _lift;
        private float _bulge;
        private float _clearance;
        private float _arriveInward;

        /// <param name="side">Which way round to go when the points are on opposite sides of the body.</param>
        /// <param name="clearance">How far from the surface to keep, in metres.</param>
        /// <param name="lift">How far the middle of the arc rises, in metres.</param>
        /// <param name="bulgePerHalfTurn">How far the arc bows out for half a turn round the body, in metres.</param>
        /// <param name="arriveInward">
        /// 0..1: how much the arc finishes heading straight in toward the body, not round it or up, for something
        /// that carries on into the body from its end (a hand going into the mouth).
        /// </param>
        public JellyBodyArc(JellyBody body, Vector3 from, Vector3 to, Vector3 side, float clearance, float lift,
            float bulgePerHalfTurn, float arriveInward = 0f)
        {
            _body = body;
            var fromOffset = from - body.AxisPoint(from.y);
            var toOffset = to - body.AxisPoint(to.y);

            _startAngle = Mathf.Atan2(fromOffset.x, fromOffset.z);
            float endAngle = Mathf.Atan2(toOffset.x, toOffset.z);
            _sweep = Mathf.DeltaAngle(_startAngle * Mathf.Rad2Deg, endAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            if (Mathf.Abs(_sweep) > OppositeSweep)
            {
                // Nearly opposite: go round on the given side
                float other = _sweep - Mathf.Sign(_sweep) * Mathf.PI * 2f;
                float sideAngle = Mathf.Atan2(side.x, side.z) * Mathf.Rad2Deg;
                float shortMiss = Mathf.Abs(Mathf.DeltaAngle((_startAngle + _sweep * 0.5f) * Mathf.Rad2Deg, sideAngle));
                float longMiss = Mathf.Abs(Mathf.DeltaAngle((_startAngle + other * 0.5f) * Mathf.Rad2Deg, sideAngle));
                if (longMiss < shortMiss)
                {
                    _sweep = other;
                }
            }

            _startRadius = new Vector2(fromOffset.x, fromOffset.z).magnitude;
            _endRadius = new Vector2(toOffset.x, toOffset.z).magnitude;
            _startHeight = from.y;
            _endHeight = to.y;
            _arriveInward = Mathf.Clamp01(arriveInward);
            _clearance = clearance;

            // Bow out and lift enough that the arc comes in to its end from outside, not still rising or widening
            _bulge = Mathf.Max(bulgePerHalfTurn * Mathf.Abs(_sweep) / Mathf.PI,
                _arriveInward * Mathf.Max(0f, _endRadius - _startRadius) * 1.5f / Mathf.PI);
            _lift = Mathf.Max(lift, _arriveInward * Mathf.Max(0f, _endHeight - _startHeight) / Mathf.PI);
        }

        public Vector3 Evaluate(float t)
        {
            float arch = Mathf.Sin(t * Mathf.PI);
            // Arriving inward, the sweep round the body eases out so the last stretch points at the body
            float sweepT = Mathf.Lerp(t, 1f - (1f - t) * (1f - t), _arriveInward);
            float angle = _startAngle + _sweep * sweepT;
            float height = Mathf.Lerp(_startHeight, _endHeight, t) + _lift * arch;
            float radius = Mathf.Lerp(_startRadius, _endRadius, t) + _bulge * arch;

            // Keep clear of the body, softly so the arc stays smooth where the clearance takes over
            float ends = Mathf.SmoothStep(0f, 1f, t / EndFade) * Mathf.SmoothStep(0f, 1f, (1f - t) / EndFade);
            float minRadius = (_body.RadiusAtWorldHeight(height) + _clearance) * ends;
            float softness = _clearance + 0.05f;
            radius = 0.5f * (radius + minRadius + Mathf.Sqrt((radius - minRadius) * (radius - minRadius) + softness * softness));

            return _body.AxisPoint(height) + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
        }
    }
}
