namespace Eco.Scripts.Jelly
{
    /// <summary>A spot on the jelly body a tentacle grows from, handed out by <see cref="JellyBody.TryTakeAnchor"/>.</summary>
    public readonly struct JellyAnchor
    {
        public readonly int Index;

        /// <summary>Radians around y, 0 = front.</summary>
        public readonly float Angle;

        /// <summary>Position along the body profile, 0 = bottom pole, 1 = top pole.</summary>
        public readonly float ProfileT;

        public JellyAnchor(int index, float angle, float profileT)
        {
            Index = index;
            Angle = angle;
            ProfileT = profileT;
        }
    }
}
