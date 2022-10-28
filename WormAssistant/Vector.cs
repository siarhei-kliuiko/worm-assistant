namespace WormAssistant
{
    public struct Vector
    {
        public Vector(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public float X { get; private set; }
        public float Y { get; private set; }
        public bool IsEmpty => this.X == 0 && this.Y == 0;

        public static Vector operator +(Vector vec1, Vector vec2)
        {
            return new Vector(vec1.X + vec2.X, vec1.Y + vec2.Y);
        }

        public static Vector operator /(Vector vec, int number)
        {
            return new Vector(vec.X / number, vec.Y / number);
        }
    }
}
