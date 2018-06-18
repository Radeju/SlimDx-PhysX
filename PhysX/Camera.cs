using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SlimDX;
using SlimDX.Direct3D10;
using SlimDX.DXGI;
using DX10 = SlimDX.Direct3D10;
using DXGI = SlimDX.DXGI;
using SlimDX.Windows;
using SlimDX.D3DCompiler;
using ColladaReader;
using StillDesign.PhysX;
using PhysX;

namespace PhysX
{

        class Camera : Program
        {
            private Matrix projection = Matrix.Identity;
            private Matrix view = Matrix.Identity;

            private Vector3 position;
            private Vector3 origTarget;
            private Vector3 cameraFinalTarget;
            private Matrix viewMatrix;

            private Vector3 up = new Vector3(0, 1, 0);

            private float leftRightRot = 0;
            private float upDownRot = 0;
            private int windowWidth;
            private int windowHeight;
            private float fovCamera;

            const float rotationSpeed = 0.01f;

            internal SlimDX.Ray CameraRay
            {
                get
                {
                    Vector3 direction = (cameraFinalTarget - position);
                    direction.Normalize();
                    return new SlimDX.Ray(position, direction);
                }
            }

            internal Matrix ProjectionMat
            {
                get { return projection; }
            }

            internal Matrix View
            {
                get { return view; }
            }

            internal Vector3 CamPosition
            {
                get { return position; }
            }

            private void SetPosition(Vector3 addVector)
            {
                position += addVector;
            }

            internal Matrix ViewProjection
            {
                get { return view * projection; }
            }

            public void ReadMouse(float x, float y)
            {
                float xDifference = x;
                float yDifference = y;

                leftRightRot += rotationSpeed * xDifference;
                upDownRot += rotationSpeed * yDifference;

                UpdateViewMatrix();
                UpdateCamera();

            }

            public void AddToCameraPosition(Vector3 vectorToAdd)
            {
                Matrix cameraRotation = Matrix.RotationX(upDownRot) * Matrix.RotationY(leftRightRot);
                Vector4 rotatedVector = Vector3.Transform(vectorToAdd, cameraRotation);
                this.SetPosition(new Vector3(rotatedVector.X, rotatedVector.Y, rotatedVector.Z));
                UpdateViewMatrix();
                UpdateCamera();
            }

            private void UpdateViewMatrix()
            {
                Matrix cameraRotation = Matrix.RotationX(upDownRot) * Matrix.RotationY(leftRightRot);

                Vector3 cameraOriginalTarget = origTarget;
                Vector4 cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, cameraRotation);
                Vector3 cameraFinalTarget = this.CamPosition + new Vector3(cameraRotatedTarget.X, cameraRotatedTarget.Y, cameraRotatedTarget.Z);

                Vector3 cameraOriginalUpVector = new Vector3(0, 1, 0);
                Vector4 cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, cameraRotation);

                viewMatrix = Matrix.LookAtLH(this.CamPosition, cameraFinalTarget, new Vector3(cameraRotatedUpVector.X, cameraRotatedUpVector.Y, cameraRotatedUpVector.Z));
            }

            public void UpdateCamera()
            {
                projection = Matrix.PerspectiveFovLH(fovCamera, (float)windowWidth / (float)windowHeight, 0.01f, 20000f);  // macierz projekcji
                view = viewMatrix;
            }

            public Vector4 VectorInCameraMatrix(Vector3 src)
            {
                Matrix cameraRotation = Matrix.RotationX(upDownRot) * Matrix.RotationY(leftRightRot);
                Vector4 rotatedVector = Vector3.Transform(src, cameraRotation);
                return rotatedVector;
            }

            internal Camera(Vector3 position, Vector3 target, int Width, int Height, float @fov = (float)Math.PI / 4)
            {
                windowHeight = Height;
                windowWidth = Width;
                fovCamera = fov;

                projection = Matrix.PerspectiveFovLH(fovCamera, (float)windowWidth / (float)windowHeight, 0.01f, 20000f);
                view = Matrix.LookAtLH(position, target, up);

                this.position = position;
                this.origTarget = target;
            }

            internal void TakeALook()
            {
                Matrix cameraRotation = Matrix.RotationX(upDownRot) * Matrix.RotationY(leftRightRot);

                Vector3 cameraRotatedTarget = Vector3.TransformCoordinate(origTarget, cameraRotation);
                cameraFinalTarget = position + cameraRotatedTarget;

                Vector3 cameraRotatedUpVector = Vector3.TransformCoordinate(up, cameraRotation);

                view = Matrix.LookAtLH(position, cameraFinalTarget, cameraRotatedUpVector);
            }

            private void CameraMove(object vector, EventArgs e)
            {
                //Eventhandler, called when camera moved
                //object == Vector3 -> vector to add


                Vector3 vectorToAdd = (Vector3)vector;

                Matrix cameraRotation = Matrix.RotationX(upDownRot) * Matrix.RotationY(leftRightRot);
                Vector3 rotatedVector = Vector3.TransformCoordinate(vectorToAdd, cameraRotation);

                position += rotatedVector;
            }

            private void CameraRotate(object vector, EventArgs e)
            {

                //Eventhandler, called when camera rotated
                //object == Vector2
                //x == leftRightRot
                //y == upDownRot

                Vector2 rotation = (Vector2)vector;

                this.leftRightRot += rotation.X;
                this.upDownRot -= rotation.Y;

            }
        }
    }
