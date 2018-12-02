using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Hitable;
using System.Collections.Generic;

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

        public List<IHitable> hitables { get; }

        public KerrBlackHoleEquation equation { get; }

        public Scene(double r, double theta, double phi, KerrBlackHoleEquation equation, List<IHitable> hitables)
        {
            CameraDistance = r;
            CameraAngle = phi;
            CameraInclination = theta;
            CameraTilt = 0;
            CameraYaw = 0;
            this.equation = equation;
            this.hitables = hitables;
        }

        public Scene(double r, double theta, double phi, double tilt, double yaw, KerrBlackHoleEquation equation, List<IHitable> hitables)
            : this(r, phi, theta, equation, hitables)
        {
            CameraTilt = tilt;
            CameraYaw = yaw;
        }
    }
}
