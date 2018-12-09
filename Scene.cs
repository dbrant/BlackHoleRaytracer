using BlackHoleRaytracer.Equation;
using BlackHoleRaytracer.Hitable;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace BlackHoleRaytracer
{
    public class Scene
    {
        public Vector3 CameraPosition { get; }

        public Vector3 CameraLookAt { get; }

        public Vector3 UpVector { get; }

        public float Fov { get; }

        
        public double CameraDistance { get; }
        
        public double CameraAngleHorz { get; }
        
        public double CameraAngleVert { get; }

        
        public double CameraTilt { get; }
        
        public double CameraYaw { get; }

        public List<IHitable> hitables { get; }


        public SchwarzschildBlackHoleEquation SchwarzschildEquation { get; }

        public KerrBlackHoleEquation KerrEquation { get; }


        public Scene(Vector3 CameraPosition, Vector3 CameraLookAt, Vector3 UpVector, float Fov, List<IHitable> hitables, float CurvatureCoeff, float AngularMomentum)
        {
            this.CameraPosition = CameraPosition;
            this.CameraLookAt = CameraLookAt;
            this.UpVector = UpVector;
            this.hitables = hitables;
            this.Fov = Fov;

            double tempR = 0, tempTheta = 0, tempPhi = 0;
            Util.ToSpherical(CameraPosition.X, CameraPosition.Y, CameraPosition.Z, ref tempR, ref tempTheta, ref tempPhi);
            CameraDistance = tempR;
            CameraAngleVert = tempTheta;
            CameraAngleHorz = tempPhi - 0.1;

            SchwarzschildEquation = new SchwarzschildBlackHoleEquation(CurvatureCoeff);
            KerrEquation = new KerrBlackHoleEquation(CameraDistance, CameraAngleHorz, CameraAngleVert, AngularMomentum);
        }
    }
}
