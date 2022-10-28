using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WormAssistant
{
    public class Animation:IDisposable
    {
        private Queue<Bitmap> frames;
        private bool isLooped;
        private Timer animationPlayer;

        internal Animation(List<FramesSet> frameSets, Directions direction, int speed, bool isLooped)
        {
            this.isLooped = isLooped;
            this.Orientation = direction;
            this.LoadAnimationFrames(frameSets);
            this.animationPlayer = new Timer { Interval = speed };
            this.animationPlayer.Tick += AnimationsPlayer_Tick;
            this.animationPlayer.Start();
        }

        public void Dispose()
        {
            this.animationPlayer.Tick -= AnimationsPlayer_Tick;
            this.animationPlayer.Dispose();
        }

        public Directions Orientation { get; private set; }

        public Bitmap CurrentFrame => frames.Peek();

        public int Speed 
        {
            get => this.animationPlayer.Interval;
            set => this.animationPlayer.Interval = value;
        }
        
        public bool IsFinished
        {
            get => !this.animationPlayer.Enabled;
        }

        private void LoadAnimationFrames(List<FramesSet> frameSets)
        {
            this.frames = new Queue<Bitmap>();
            foreach (var set in frameSets)
            {
                var tempbitmap = new Bitmap(60, 60);
                var framesCount = set.Pic.Height / 60;
                using (var g = Graphics.FromImage(tempbitmap))
                {
                    if (set.IsReverced)
                    {
                        for (int i = framesCount - 1; i >= 0; i--)
                        {
                            this.frames.Enqueue(GetFrame(g, tempbitmap, set, i));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < framesCount; i++)
                        {
                            this.frames.Enqueue(GetFrame(g, tempbitmap, set, i));
                        }
                    }
                }
            }
        }

        private Bitmap GetFrame(Graphics g, Bitmap bitMap, FramesSet set, int frameNumber)
        {
            g.DrawImage(set.Pic, new Rectangle(0, 0, bitMap.Width, bitMap.Height), new Rectangle(0, frameNumber * bitMap.Height, bitMap.Width, bitMap.Height), GraphicsUnit.Pixel);
            if (this.Orientation == Directions.Right)
            {
                bitMap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }

            var frame = (Bitmap)bitMap.Clone();
            g.Clear(Color.Transparent);
            return frame;
        }

        internal void ChangeOrientation(Directions newOrientation)
        {
            if (this.Orientation != newOrientation)
            {
                this.Orientation = newOrientation;
                foreach (var frame in this.frames)
                {
                    frame.RotateFlip(RotateFlipType.RotateNoneFlipX);
                }
            }
        }

        private void AnimationsPlayer_Tick(object sender, EventArgs e)
        {
            if (this.isLooped)
            {
                this.frames.Enqueue(this.frames.Dequeue());
            }
            else
            {
                if (this.frames.Count > 1)
                {
                    this.frames.Dequeue();
                }
                else
                {
                    this.animationPlayer.Stop();
                }
            }
        }

        public struct FramesSet
        {
            public Bitmap Pic { get; set; }
            public bool IsReverced { get; set; }
        }
    }
}
