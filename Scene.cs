using System;

namespace BlackHoleRaytracer
{
    public class Scene
    {
        /// <summary>
        /// Camera position - Distance from black hole
        /// </summary>
        public double CameraDistance { get; set; }

        /// <summary>
        /// Camera position - Inclination (vertical angle) in degrees
        /// </summary>
        public double CameraInclination { get; set; }

        /// <summary>
        /// Camera position - Angle (horizontal) in degrees
        /// </summary>
        public double CameraAngle { get; set; }

        /// <summary>
        /// Camera tilt - in degrees
        /// </summary>
        public double CameraTilt { get; set; }

        /// <summary>
        /// Camera aperture - need to manipulate the camera angle.
        /// </summary>
        public double CameraAperture { get; set; }

        /// <summary>
        /// Camera yaw - if we want to look sideways.
        /// Note: this is expressed in % of image width.
        /// </summary>
        public double CameraYaw { get; set; }


        public static Scene GetScene(double Theta)
        {
            Scene result = new Scene();
            result.CameraAperture = 2.0;

            double t = 0;

            double r = 30;

            // factor of attenuation of camera's sinusoidal motions (the closer to black hole - the calmer the flight is)
            double calmFactor = Math.Pow((600 - r) / 575, 20);

            double phi = 75; // t*3;
            double theta = Theta;
            //double theta = 84
            //    + 8 * Math.Sin(phi * Math.PI / 180) * (1 - calmFactor) // precession
            //    + 3 * calmFactor;

            result.CameraAngle = phi;
            result.CameraDistance = r;
            result.CameraInclination = theta;
            result.CameraAperture = 0.5; // 24.00/500.0*r + 3.2;
            result.CameraTilt = 0; //8.0 * Math.Cos(phi * Math.PI / 180) * (1 - calmFactor);
            result.CameraYaw = 0; // calmFactor * 1.0; // we will be 'landing' on the accretion disc...

            return result;
        }
    }
}
