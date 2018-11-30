
namespace BlackHoleRaytracer
{
    public class Scene
    {
        /// <summary>
        /// Camera position - Distance from black hole
        /// </summary>
        public double CameraDistance { get; }

        /// <summary>
        /// Camera position - Inclination (vertical angle) in degrees
        /// </summary>
        public double CameraInclination { get; }

        /// <summary>
        /// Camera position - Angle (horizontal) in degrees
        /// </summary>
        public double CameraAngle { get; }

        /// <summary>
        /// Camera tilt - in degrees
        /// </summary>
        public double CameraTilt { get; }

        /// <summary>
        /// Camera yaw - if we want to look sideways.
        /// Note: this is expressed in % of image width.
        /// </summary>
        public double CameraYaw { get; }

        public Scene(double r, double phi, double theta)
        {
            CameraAngle = phi;
            CameraDistance = r;
            CameraInclination = theta;
            CameraTilt = 0;
            CameraYaw = 0;
        }

        public Scene(double r, double phi, double theta, double tilt, double yaw)
            : this(r, phi, theta)
        {
            CameraTilt = tilt;
            CameraYaw = yaw;
        }
    }
}
