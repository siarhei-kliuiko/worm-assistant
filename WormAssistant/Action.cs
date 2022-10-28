using System;

namespace WormAssistant
{
    public class Action : IDisposable
    {
        private bool isDynamic;

        public Action(Types type, Animation animation)
        {
            this.Type = type;
            this.Animation = animation;
        }

        public Action(Types type, Vector force, Animation animation) : this(type, animation)
        {
            this.Force = force;
            this.isDynamic = !force.IsEmpty;
        }

        public Action(Types type, Vector force, Vector velocity, float yEndPoint, Animation animation) : this(type, force, animation)
        {
            this.YEndPoint = yEndPoint;
            this.Velocity = velocity;
            this.isDynamic = !force.IsEmpty || !velocity.IsEmpty;
        }

        public enum Types
        {
            None,
            Walk,
            Stay,
            ListenIn,
            Listen,
            CommandExecute,
            ListenOut,
            ChangeWallpaper,
            Die,
            Hang,
            Jump,
            GoingUp,
            GoingDown,
            GoingDownHard,
            Land,
            BeginFall,
            FallRoll,
            FallStraight,
            Stuck,
            Slide,
            SlideOut,
            Angry,
            BigHead,
            Blink,
            BlinkHard,
            BreathHard,
            Eat,
            Flag,
            Happy,
            LookPeek,
            LookScreen,
            LookSideways,
            LookUp,
            LookUpBlink,
            LookUpHead,
            Mustache,
            PeekBlink,
            PeekHeadBlink,
            Sad,
            SadSneeze,
            Scratch,
            Silly,
            SkipIn,
            Skip,
            SkipOut,
            MemorizeLink,
            FlagOut,
            FlagIn,
            LookFromEdge
        }

        public Types Type { get; set; }
        public bool IsDynamic => this.isDynamic;
        public Vector Force { get; set; }
        public Vector Velocity { get; set; }
        public Animation Animation { get; set; }
        public float YEndPoint { get; set; }
        public Directions Direction => this.Force.X < 0 ? Directions.Left : Directions.Right;


        public static float CalculateYEndPoint(float yLocation, float yForce, float yVelocity)
        {
            float delta = yForce;
            while (yForce < 0)
            {
                yForce += yVelocity;
                delta += yForce;
            }

            return yLocation + delta;
        }

        internal void ChangeDirection(Directions newDirection)
        {
            if (this.Direction != newDirection)
            {
                this.Force = new Vector(-this.Force.X, this.Force.Y);
                this.Velocity = new Vector(-this.Velocity.X, this.Velocity.Y);
            }
        }

        public void Dispose()
        {
            this.Animation.Dispose();
        }
    }
}
