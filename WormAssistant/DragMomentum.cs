using System.Drawing;

namespace WormAssistant
{
    public class DragMomentum
    {
        private PointF startLocation, endLocation;

        public DragMomentum(PointF initialLocation)
        {
            this.endLocation = this.startLocation = initialLocation;
        }

        public Vector Force => new Vector (endLocation.X - startLocation.X, endLocation.Y - startLocation.Y) / 4;

        public void AddLocation(PointF newLocation)
        {
            this.startLocation = this.endLocation;
            this.endLocation = newLocation;
        }
    }
}
