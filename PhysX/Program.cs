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
    class Program
    {
        #region Device, swapchain and other variables
        static public RenderForm MainWindow;
        static public DX10.Device device;
        static public DXGI.SwapChain swapchain;
        static public Texture2D backbuffer, depthbuffer;
        static public RenderTargetView renderview;
        static public DepthStencilState depthstate;
        static public DepthStencilView depthview;
        static public Camera myCamera;

        static public float time;

        static public float phi, theta, radius;
        #endregion

        #region Geometry and Virtual Assets
        static public CGeometry Plane;

        static public List<CGeometry> Boxes;
        static public Dictionary<Actor, Matrix> BoxMatrices;

        static public List<CGeometry> Balls;
        static public Dictionary<Actor, Matrix> BallMatrices;

        static public SDXCamera Camera;
        static public Matrix Projection;
        static public float deltar = 5;
        static public float deltaang = 0.02f;
        static public List<CIntersection> Intersections;

        static public WavefrontLoader.WFOGeometry ClothGeometry;
        #endregion

        #region Effect
        static public Effect effect;

        #endregion

        #region Textures
        static public Texture2D Diffuse;
        static public Texture2D Diffuse2;
        static public Texture2D Diffuse3;
        static public Texture2D BumpMap;
        static public Texture2D Reflection;
        static public ShaderResourceView DiffuseResource;
        static public ShaderResourceView Diffuse2Resource;
        static public ShaderResourceView Diffuse3Resource;
        static public ShaderResourceView BumpMapResource;
        static public ShaderResourceView ReflectionResource;
        #endregion

        #region Lights
        public static Vector3 LightPosition;
        #endregion


        public static PEngine Engine;

        static public float oldMouseX;
        static public float oldMouseY;
        static public Vector3 scale = new Vector3(10,10,10);
        static public int howManyBeginningActors = 40;   //TA LICZBA MUSI BYC DODATNIA, INACZEJ TO NIE WIEM CO SIE STANIE
        public const int MaxMass = 100;       //TA LICZBA MUSI BYC WIEKSZA NIZ 1, INACZEJ TO NIE WIEM CO SIE STANIE
        static public int forceMultiplier = 10;
        static public bool cameraEnabled = true;

        static public bool caughtObject;
        static public int caughtObjectIndex;
        static public Actor caughtObjectKey;
        static public float caughtObjectZ;

        //proba na frame'ach
        static public float prevFrameX;
        static public float prevFrameY;
        static public float curFrameX;
        static public float curFrameY;

        static public float step = 0.6f;

        static public void InitializeMainWindow()
        {
            oldMouseX = 0;
            oldMouseY = 0;

            caughtObject = false;
            caughtObjectIndex = 0;

            curFrameY = curFrameX = prevFrameX = prevFrameY = 0;

            MainWindow = new RenderForm("First SlimDX example");
            MainWindow.KeyDown += new System.Windows.Forms.KeyEventHandler(MainWindow_KeyDown);
            
            //moje
            MainWindow.MouseMove += new System.Windows.Forms.MouseEventHandler(MainWindow_MouseMove);
            MainWindow.MouseClick += new System.Windows.Forms.MouseEventHandler(MainWindow_MouseClick);

            MainWindow.MouseDown += new System.Windows.Forms.MouseEventHandler(MainWindow_MouseDown);
            MainWindow.MouseUp += new System.Windows.Forms.MouseEventHandler(MainWindow_MouseUp);

            MainWindow.KeyUp += new System.Windows.Forms.KeyEventHandler(MainWindow_KeyUp);
            MainWindow.MouseWheel += new MouseEventHandler(MainWindow_MouseWheel);
        }

        static public bool detectCollisionsBySphere(CGeometry First, Matrix firstMatrix, CGeometry Second, Matrix secondMatrix)
        {
            bool isCollision = true;
            Vector3 scale = new Vector3();
            Quaternion rotation = new Quaternion();
            Vector3 translation = new Vector3();
            float radiusSum = 0;

            firstMatrix.Decompose(out scale, out rotation, out translation);
            Vector3 Position1 = First.BSphere.Center + translation;
            radiusSum += First.BSphere.Radius * scale.X;    //w przypadku gdy skala nie jest liniowa to powinno byc inaczej

            secondMatrix.Decompose(out scale, out rotation, out translation);
            Vector3 Position2 = Second.BSphere.Center + translation;
            radiusSum += Second.BSphere.Radius * scale.X;   //w przypadku gdy skala nie jest liniowa to powinno byc inaczej

            float distance = Math.Abs(Vector3.Distance(Position1, Position2));

            return isCollision = distance > radiusSum ? false : true;
        }

        static public bool detectCollisionsByVertex(CGeometry First, Matrix firstMatrix, CGeometry Second, Matrix secondMatrix)
        {
            DataStream FirstStream = null;
            bool isCollision = false;
            List<CIntersection> Intersections = new List<CIntersection>();
            Vector3 VertexOrig;
            Vector3 RayDirection = new Vector3(1, 1, 1);
            RayDirection.Normalize();
            Matrix inverseFirstMatrix = firstMatrix;
            Matrix inverseSecondMatrix = secondMatrix;
            inverseFirstMatrix.Invert();
            inverseSecondMatrix.Invert();

            FirstStream = First.VertexStream;
            FirstStream.Position = 0;
            //po kolei z kazdego wierzcholka figury 1szej wysyla promien do RayDirection. Sprawdza czy
            //liczba przeciec z figura 2ga jest parzysta. Jezeli nie to znaczy ze wierzcholek znajduje
            //sie w drugiej figurze
            for (int i = 0; i < First.VertexCount; i++)
            {
                FirstStream.Position = i * First.VertexSizeInBytes;

                VertexOrig = FirstStream.Read<Vector3>();
                Vector4 VertexOrigTemp = Vector3.Transform(VertexOrig, firstMatrix);
                //poprzednia, zla wersja
                //Vector4 VertexOrigTemp = Vector3.Transform(VertexOrig, Matrix.Transpose(inverseFirstMatrix));
                //VertexOrigTemp = Vector4.Transform(VertexOrigTemp, secondMatrix);
                VertexOrig.X = VertexOrigTemp.X;
                VertexOrig.Y = VertexOrigTemp.Y;
                VertexOrig.Z = VertexOrigTemp.Z;

                Second.Intersect(new SlimDX.Ray(VertexOrig, RayDirection), out Intersections, secondMatrix);

                //do kolejnego zadania
                //AllRayIntersections.Add(Intersections);
                if (Intersections.Count % 2 != 0)
                {
                    isCollision = true;
                    return isCollision;
                }
            }

            return isCollision;
        }

        static void CalculateRays(int x, int y, out Vector3 RayOrigOut, out Vector3 RayDirOut)
        {
            Vector3 v = new Vector3(2 * x, 2 * y, 1);
            v.X /= backbuffer.Description.Width;
            v.X -= 1;
            v.X /= Projection.M11;

            v.Y /= backbuffer.Description.Height;
            v.Y -= 1;
            v.Y /= -Projection.M22;
            Matrix IWV = Matrix.Transpose(Matrix.Invert(myCamera.View));
            Vector3 RayDir = new Vector3();
            RayDir.X = v.X * IWV.M11 + v.Y * IWV.M12 + v.Z * IWV.M13;
            RayDir.Y = v.X * IWV.M21 + v.Y * IWV.M22 + v.Z * IWV.M23;
            RayDir.Z = v.X * IWV.M31 + v.Y * IWV.M32 + v.Z * IWV.M33;
            Vector3 RayOrig = new Vector3(IWV.M14, IWV.M24, IWV.M34);

            RayOrigOut = RayOrig;
            RayDirOut = RayDir;
        }

        static void CalculateTransposition(int index, float xChange, float yChange, float zChange, Actor MatrixKey, Matrix objectMatrix)
        {
            //wylacz aktora i przenies go do innej grupy
            Engine.Actors[index].Group = 2;
            //Engine.Actors[index].ActorFlags;
            Engine.Actors[index].Sleep();

            //tutaj powaznie sie zastanowic czy to jest dobrze ; JEST DOBRZE !
            Vector4 cameraTransposition = myCamera.VectorInCameraMatrix(new Vector3(xChange, -yChange, zChange));

            Engine.Actors[index].GlobalPosition += new Vector3(cameraTransposition.X, cameraTransposition.Y, cameraTransposition.Z).ToPhx();
            BoxMatrices[MatrixKey].set_Rows(3, objectMatrix.get_Rows(3) + cameraTransposition);


            int i = 0;
            CGeometry movingObject = Boxes[index-7];
            foreach (var M in BoxMatrices)
            {
                if (i != index - 7)
                {
                    Matrix secondMat = M.Value;
                    secondMat = (i < howManyBeginningActors) ? ScaleMatrixNoTranslation(secondMat, scale) : secondMat;

                    if (detectCollisionsBySphere(movingObject, objectMatrix, Boxes[i], secondMat))
                    {
                        if (detectCollisionsByVertex(movingObject, objectMatrix, Boxes[i], secondMat))
                        {
                           //obudz na bardzo krotka chwile
                            Engine.Actors[index].Group = 0;
                            Engine.Actors[index].WakeUp();
                        }
                    }
                }
                i++;
            }
        }

        static void MainWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            Vector3 RayDir = new Vector3();
            Vector3 RayOrig = new Vector3();
            
            CalculateRays(e.Location.X, e.Location.Y, out RayOrig, out RayDir);


            if (caughtObject)
            {
                Matrix objectMatrix = (caughtObjectIndex - 7 < howManyBeginningActors) ?
                        ScaleMatrixNoTranslation(BoxMatrices[caughtObjectKey], scale) : BoxMatrices[caughtObjectKey];

                if (Engine.Actors[caughtObjectIndex].IsDynamic)
                {
                    float zChange = (e.Delta > 0) ? (float)2 : (float)-2;
                    caughtObjectZ = e.Delta;
                    CalculateTransposition(caughtObjectIndex, 0, 0, zChange, caughtObjectKey, objectMatrix);
                }
                return;
            }
        }

        //nadaj siłę ciału ktore kliknalem
        static public void MainWindow_MouseClick(object sender, MouseEventArgs e)
        {
            Vector3 RayDir = new Vector3();
            Vector3 RayOrig = new Vector3();

            CalculateRays(e.Location.X, e.Location.Y, out RayOrig, out RayDir);

            int i = 0;
            foreach (var M in BoxMatrices)
            {
                //kluczowy jest ostatni argument metody Intersect(..,.., M.Value) !! zmodyfikowana zostala
                //funkcja Intersect w ColladaReader.dll, zajrzec tam!
                Boxes[i].Intersect(new SlimDX.Ray(RayOrig, RayDir), out Intersections, ScaleMatrixNoTranslation(M.Value,scale));
                if (Intersections.Count > 0)
                {
                    /* MEGA WAZNE
                     * dlaczego i+7 ? jak tworzymy engine to mamy 6 scian
                     * ktore tworza nasza zamkniete przestrzen. dlaczego 7?
                     * jest jakis nullowy aktor ktory nie wiem czym jest
                     */ 
                    if (Engine.Actors[i+7].IsDynamic)
                    {
                        //i tu to co ma sie wydarzyc
                        //jezeli lewy to sila od uzytkownika
                        if (e.Button == MouseButtons.Left)
                        {
                            //Engine.Actors[i + 7].AddForce(forceMultiplier * Engine.Actors[i + 7].Mass* RayDir.ToPhx(), ForceMode.Impulse);
                            //Engine.Actors[i + 7].Group = 0;
                            //Engine.Actors[i + 7].WakeUp();
                            break;
                        }
                        //jezeli prawy to sila DO uzytkownika
                        else if (e.Button == MouseButtons.Right)
                        {
                            //Engine.Actors[i + 7].AddForce(-1 * forceMultiplier * Engine.Actors[i + 7].Mass * RayDir.ToPhx(), ForceMode.Impulse);
                            //Engine.Actors[i + 7].Group = 2;
                            //Engine.Actors[i + 7].Sleep();
                            break;
                        }
                    }
                }
                i++;
            }

            return;
        }

        //przejmij przedmiot ktory bedziesz przesuwal
        static public void MainWindow_MouseDown(object sender, MouseEventArgs e)
        {
            //MainWindow.MouseMove -= new System.Windows.Forms.MouseEventHandler(MainWindow_MouseMove);
            Vector3 RayDir = new Vector3();
            Vector3 RayOrig = new Vector3();

            CalculateRays(e.Location.X, e.Location.Y, out RayOrig, out RayDir);

            int i = 0;
            foreach (var M in BoxMatrices)
            {
                Boxes[i].Intersect(new SlimDX.Ray(RayOrig, RayDir), out Intersections, ScaleMatrixNoTranslation(M.Value, scale));
                Actor MatrixKey = M.Key;
                if (Intersections.Count > 0)
                {
                    if (Engine.Actors[i + 7].IsDynamic)
                    {
                        caughtObject = true;
                        caughtObjectKey = MatrixKey;
                        caughtObjectIndex = i + 7;
                    }
                }
                i++;
            }
            return;
        }

        //przestan przejmowac przedmiot ktory bedziesz przesuwal
        static public void MainWindow_MouseUp(object sender, MouseEventArgs e)
        {
            float frameXChange = curFrameX - prevFrameX;
            float frameYChange = curFrameY - prevFrameY;

            if (caughtObjectIndex != 0)
            {
                if (Engine.Actors[caughtObjectIndex].IsDynamic)
                {
                    caughtObjectZ /= 50;
                    //i tu to co ma sie wydarzyc
                    Vector4 cameraTransposition = myCamera.VectorInCameraMatrix(new Vector3(frameXChange, -frameYChange, caughtObjectZ));
                    Vector3 forceDir = new Vector3(cameraTransposition.X, cameraTransposition.Y, cameraTransposition.Z);
                    Engine.Actors[caughtObjectIndex].AddForce(forceMultiplier * Engine.Actors[caughtObjectIndex].Mass * forceDir.ToPhx(), ForceMode.Impulse);
                    Engine.Actors[caughtObjectIndex].Group = 0;
                    Engine.Actors[caughtObjectIndex].WakeUp();
                    caughtObject = false;
                    caughtObjectIndex = 0;
                }
            }
        }

        //rotacja kamery
        static public void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            float xChange = e.Location.X - oldMouseX;
            float yChange = e.Location.Y - oldMouseY;
            curFrameY = e.Location.Y;
            curFrameX = e.Location.X;
            caughtObjectZ = 0;

            //Console.WriteLine("{0:00}, {1:00} \n OLD {2:00}, {3:00}", e.Location.X, e.Location.Y, oldMouseX, oldMouseY);
            oldMouseX = e.Location.X;
            oldMouseY = e.Location.Y;

            Vector3 RayDir = new Vector3();
            Vector3 RayOrig = new Vector3();

            CalculateRays(e.Location.X, e.Location.Y, out RayOrig, out RayDir);
            Console.WriteLine("{0:00}, {1:00} {2:00}", xChange, yChange, e.Delta);

            if (cameraEnabled)
            {
                myCamera.ReadMouse(xChange, yChange);

                Matrix V = myCamera.View;
                Matrix VI = Matrix.Invert(V);

                effect.GetVariableByName("mWVP").AsMatrix().SetMatrix(V * Projection);
                effect.GetVariableByName("mVI").AsMatrix().SetMatrix(VI);
            }
            else if (e.Button == MouseButtons.Left)
            {
                if (caughtObject)
                {
                    Matrix objectMatrix = (caughtObjectIndex - 7 < howManyBeginningActors) ?
                                            ScaleMatrixNoTranslation(BoxMatrices[caughtObjectKey], scale) : BoxMatrices[caughtObjectKey];

                    if (Engine.Actors[caughtObjectIndex].IsDynamic)
                    {
                        Vector4 PosObj = BoxMatrices[caughtObjectKey].get_Rows(3);
                        Vector3 PosCam = myCamera.CamPosition;
                        float distance = Vector3.Distance(PosCam, new Vector3(PosObj.X, PosObj.Y, PosObj.Z) );
                            
                        xChange = xChange / 50 * (float)Math.Sqrt((double) distance);
                        yChange = yChange / 50 * (float)Math.Sqrt((double) distance);
                        CalculateTransposition(caughtObjectIndex, xChange, yChange, 0, caughtObjectKey, objectMatrix);
                    }
                }
            }
        }

        static public void MainWindow_KeyUp(object sender, KeyEventArgs f)
        {
            switch (f.KeyCode)
            {
                case Keys.F:
                    cameraEnabled = true;
                    break;
            }
        }

        static public void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            Random R = new Random();
            Vector3 moveVector = new Vector3(0, 0, 0);

            switch (e.KeyCode)
            {
                case Keys.D:  radius += deltar     ;moveVector.X += step; break; 
                case Keys.A:  radius -= deltar ;moveVector.X -= step; break; 
                case Keys.Space:  phi -= deltaang ;moveVector.Y += step; break;
                case Keys.ControlKey: phi += deltaang; moveVector.Y -= step; break; 
                case Keys.W:  theta -= deltaang ;moveVector.Z += step; break; 
                case Keys.S:  theta += deltaang ;moveVector.Z -= step; break;

                case Keys.F: cameraEnabled = false; break;
                case Keys.ShiftKey: step = (step == 0.6f) ? 2.2f : 0.6f; break;

                case Keys.C:
                    Actor B1 = Engine.AddBox(0.5f, 0.5f, 0.5f, 5, new Vector3(0, 50, -1).ToPhx());
                    Actor B2 = Engine.AddBox(0.5f, 0.5f, 0.5f, 5, new Vector3(0, 50, 1).ToPhx());
                    Engine.AddJointActors(B1, B2, new Vector3(0, 50, -1).ToPhx(), new Vector3(0, 50, 1).ToPhx());
                    Boxes.Add(CGeometry.Box(device));
                    Boxes.Add(CGeometry.Box(device));
                    BoxMatrices.Add(B1, B1.GlobalPose.As<Matrix>());
                    BoxMatrices.Add(B2, B2.GlobalPose.As<Matrix>());
                    break;
                case Keys.X:
                    

                    Cloth C = Engine.AddCloth(ClothGeometry, new Vector3(0, 50, 0).ToPhx());
                    
                    
                    break;

                case Keys.B:
                    {
                        
                        Actor B = Engine.AddBox(0.5f, 0.5f, 0.5f, R.Next(1,10), new Vector3(0, R.Next(10,50), 0).ToPhx());
                        backbuffer.DebugName = "BOX " + Boxes.Count.ToString();
                        Boxes.Add(CGeometry.Box(device));
                        BoxMatrices.Add(B, B.GlobalPose.As<Matrix>());
                        
                    }
                    break;
                case Keys.N:
                    {

                        Actor B = Engine.AddBall(1, R.Next(1, 10), new Vector3(0, R.Next(10, 50), 0).ToPhx());
                        Balls.Add(CGeometry.Ball(device));
                        BallMatrices.Add(B, B.GlobalPose.As<Matrix>());
                    }
                    break;
                case Keys.U:
                    {
                        foreach (Actor A in Engine.Actors)
                        {

                            if (A.IsDynamic) A.AddForce(new Vector3(0, R.Next(1, 10), 0).ToPhx(), ForceMode.Impulse);
                        }
                    }
                    break;
                case Keys.J:
                    {
                        foreach (Actor A in Engine.Actors)
                        {

                            if (A.IsDynamic) A.AddForce(new Vector3(0, -R.Next(1, 10), 0).ToPhx(), ForceMode.Impulse);
                        }
                    }
                    break;
                case Keys.I:
                    {
                        foreach (Actor A in Engine.Actors)
                        {
                            if (A.IsDynamic) A.AddForce(new Vector3(0, 0, R.Next(1, 10)).ToPhx(), ForceMode.Impulse);
                        }
                    }
                    break;
                case Keys.O:
                    {
                        foreach (Actor A in Engine.Actors)
                        {
                            if (A.IsDynamic) A.AddForce(new Vector3(R.Next(1, 10), 0, 0).ToPhx(), ForceMode.Impulse);
                        }
                    }
                    break;
                case Keys.L:
                    {
                        foreach (Actor A in Engine.Actors)
                        {
                            if (A.IsDynamic) A.AddForce(new Vector3(-R.Next(1, 10), 0, 0).ToPhx(), ForceMode.Impulse);
                        }
                    }
                    break;
                case Keys.K:
                    {
                        foreach (Actor A in Engine.Actors)
                        {                            
                            if (A.IsDynamic) 
                                A.AddForce(new Vector3(0, 0, -R.Next(1, 10)).ToPhx(), ForceMode.Impulse);
                        }
                    }
                    break;
                case Keys.R:
                    {
                        List<Actor> Actors = new List<Actor>();
                        foreach (var m in BoxMatrices)
                            Actors.Add(m.Key);
                        foreach (var m in BallMatrices)
                            Actors.Add(m.Key);
                        for (int i = 0; i < Actors.Count; i++)
                            Actors[i].Dispose();
                        Boxes.Clear();
                        BoxMatrices.Clear();
                        Balls.Clear();
                        BallMatrices.Clear();
                        List<Cloth> Clothes = new List<Cloth>();
                        foreach (var c in Engine.Scene.Cloths) Clothes.Add(c);
                        foreach (var c in Clothes) c.Dispose();

                        List<Joint> Joints = new List<Joint>();
                        foreach (var c in Engine.Scene.Joints) Joints.Add(c);
                        foreach (var c in Joints) c.Dispose();
                    }
                    break;

            }
            Camera.SetPositionSpherical(phi, theta, radius);
            Matrix V = Camera.CameraView;
            Matrix VI = Matrix.Invert(V);

            myCamera.AddToCameraPosition(moveVector);
            V = myCamera.View;
            VI = Matrix.Invert(V);

            effect.GetVariableByName("mWVP").AsMatrix().SetMatrix(V * Projection);
            effect.GetVariableByName("mVI").AsMatrix().SetMatrix(VI);
        }

        static public void InitializeDevice()
        {
            SwapChainDescription D = new SwapChainDescription()
            {
                BufferCount = 1,
                Usage = Usage.RenderTargetOutput,
                OutputHandle = MainWindow.Handle,
                IsWindowed = true,
                Flags = SwapChainFlags.AllowModeSwitch,
                SwapEffect = SwapEffect.Discard,
                SampleDescription = new SampleDescription(1, 0),
                ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm)
            };

            DX10.Device.CreateWithSwapChain(null, DriverType.Hardware, DeviceCreationFlags.None, D, out device, out swapchain);            
           
            device.Factory.SetWindowAssociation(MainWindow.Handle, WindowAssociationFlags.IgnoreAll);

        }

        static public void InitializeOutputMerger()
        {
            backbuffer = Texture2D.FromSwapChain<Texture2D>(swapchain, 0);
            renderview = new RenderTargetView(device, backbuffer);
            //MainWindow.ClientSize = new Size(1200, 900);
            device.Rasterizer.SetViewports(new Viewport(0, 0, MainWindow.ClientSize.Width, MainWindow.ClientSize.Height, 0.0f, 1.0f));

            DX10.Texture2DDescription dtd = new Texture2DDescription();
            dtd.Width = MainWindow.ClientSize.Width;
            dtd.Height = MainWindow.ClientSize.Height;
            dtd.MipLevels = 1;
            dtd.ArraySize = 1;
            dtd.BindFlags = BindFlags.DepthStencil;
            dtd.CpuAccessFlags = CpuAccessFlags.None;
            dtd.Format = Format.D32_Float;
            dtd.SampleDescription = new SampleDescription(1, 0);
            dtd.Usage = ResourceUsage.Default;
            dtd.OptionFlags = ResourceOptionFlags.None;

            depthbuffer = new Texture2D(device, dtd);


            depthview = new DepthStencilView(device, depthbuffer);

            DX10.DepthStencilStateDescription stencilStateDesc = new SlimDX.Direct3D10.DepthStencilStateDescription();
            stencilStateDesc.IsDepthEnabled = true;
            stencilStateDesc.IsStencilEnabled = false;
            stencilStateDesc.DepthWriteMask = DX10.DepthWriteMask.All;
            stencilStateDesc.DepthComparison = DX10.Comparison.Less;

            device.OutputMerger.SetTargets(depthview, renderview);
            depthstate = DepthStencilState.FromDescription(device, stencilStateDesc);
        }



        static public void InitializeTextures()
        {
            //Diffuse = Texture2D.FromFile(device, "media/gold.jpg");
            Diffuse = Texture2D.FromFile(device, "media/easyWall.jpg");
            Diffuse2 = Texture2D.FromFile(device, "media/easyWall.jpg");
            Diffuse3 = Texture2D.FromFile(device, "media/space.jpg");
            Reflection = Texture2D.FromFile(device, "media/lake.jpg");
            BumpMap = Texture2D.FromFile(device, "media/bump.jpg");

            DiffuseResource = new ShaderResourceView(device, Diffuse);
            Diffuse2Resource = new ShaderResourceView(device, Diffuse2);
            Diffuse3Resource = new ShaderResourceView(device, Diffuse3);
            BumpMapResource = new ShaderResourceView(device, BumpMap);
            ReflectionResource = new ShaderResourceView(device, Reflection);

        }


        static public void AddBox()
        {
            Random R = new Random();
            Actor B = Engine.AddBox(0.5f, 0.5f, 0.5f, R.Next(1, 10), new Vector3(0, R.Next(10, 50), 0).ToPhx());
            Boxes.Add(CGeometry.Box(device));
            BoxMatrices.Add(B, B.GlobalPose.As<Matrix>());

        }
        static public void AddBall()
        {
            Random R = new Random();
            Actor B = Engine.AddBall(1, R.Next(1, 10), new Vector3(0, R.Next(10, 50), 0).ToPhx());
            Balls.Add(CGeometry.Ball(device));
            BallMatrices.Add(B, B.GlobalPose.As<Matrix>());
        }

        public static void InitializeWalls()
        {
            //dolna jest wpisana w PEgine

            Engine.AddBox(0.1f, 100f, 100f, 1, new Vector3(100, 100, 0).ToPhx(), true);    //boczna
            Engine.AddBox(0.1f, 100f, 100f, 1, new Vector3(-100, 100, 0).ToPhx(), true);   //na przeciwko poprzedniej bocznej
            Engine.AddBox(100f, 100f, 0.1f, 1, new Vector3(0, 100, -100).ToPhx(), true);   //druga boczna
            Engine.AddBox(100f, 100f, 0.1f, 1, new Vector3(0, 100, 100).ToPhx(), true);    //na przeciwko drugiej bocznej
            Engine.AddBox(100f, 0.1f, 100f, 1, new Vector3(0, 100, 0).ToPhx(), true);      //gorna
        }

        public static void InitializeBeginningActors()
        {
            Random R = new Random();

            //jak zrobic zeby to bylo wieksze?
            Actor B;
            int N = howManyBeginningActors;
            for (int i = 0; i < howManyBeginningActors; i++)
            {
                //new Vector3(i * R.Next(0,20),10 + (i+1)*R.Next(0, 20), (R.Next(0,20)-10 )* i                   //  new Vector3((float)i / N * 180 + R.Next(10, 10 + 180 / N) - 100, R.Next(i / N * 90, 90), (float)i / N * 180 + R.Next(10, 10 + 180 / N) - 100).ToPhx());
                B = Engine.AddBox(scale.X * 0.5f, scale.Y * 0.5f, scale.Z * 0.5f, R.Next(1, MaxMass),
                    new Vector3(R.Next(-90,90), 
                                R.Next(0, 90), 
                                R.Next(-90, 90)).ToPhx()) ;

                //do debugowania numeru
                backbuffer.DebugName = "BOX " + Boxes.Count.ToString();

                Matrix ActorGlobal = B.GlobalPose.As<Matrix>();
                ActorGlobal.M11 *= scale.X;
                ActorGlobal.M22 *= scale.Y;
                ActorGlobal.M33 *= scale.Z;

                Boxes.Add(CGeometry.Box(device));
                BoxMatrices.Add(B, ActorGlobal);
            }

            //dosyc nieciekawie to wyglada ale w tym miejscu powinnien byc przynajmniej jeden aktor
            //proba dodania lokalnej sily
            Engine.Actors[0].AddForceAtPosition((new Vector3(0, R.Next(10, 20), R.Next(10, 20)).ToPhx()), (new Vector3(0, 25, 0).ToPhx()), ForceMode.Acceleration);

        }

        static public void InitializeGeometry()
        {
            //dlaczego musze tutaj dac ball2.obj, a przy ball.obj dostaje gigantyczna kulke? przeciez nawet nie ma ball.obj
            var F = new WavefrontLoader.WaveFrontObjFile("ball2.obj");
            ClothGeometry = F.Geometry;
            Plane = CGeometry.Plane(device);
            Boxes = new List<CGeometry>();
            BoxMatrices = new Dictionary<Actor, Matrix>();

            Balls = new List<CGeometry>();
            BallMatrices = new Dictionary<Actor, Matrix>();

            Camera = new SDXCamera();
            phi = 0; theta = 0; radius = Plane.BSphere.Radius * 3;

            Camera.SetPositionSpherical(phi, theta, radius);
            Camera.Target = Plane.BSphere.Center + new Vector3(0.0f, Plane.BSphere.Radius, 0.0f);
            Camera.Up = new Vector3(0, 1, 0);

            Projection = Matrix.PerspectiveFovLH((float)Math.PI / 4, (float)MainWindow.ClientSize.Width / (float)MainWindow.ClientSize.Height, 0.01f, Plane.BSphere.Radius * 300);

            //moja kamera
            myCamera = new Camera(new Vector3(25.0f, 25f, 25f), new Vector3(0.0f, 0.0f, 1.0f), MainWindow.ClientSize.Width, MainWindow.ClientSize.Height, (float)Math.PI / 3);
            Projection = myCamera.ProjectionMat;

            LightPosition = new Vector3(Plane.BSphere.Radius * 2, 0.3f, 0.5f);

        }

        static public void InitializeEffect()
        {
            effect = Effect.FromFile(device, "blinn.fx", "fx_4_0");

            effect.GetVariableByName("xReflectionTexture").AsResource().SetResource(ReflectionResource);
        }

        static public Matrix ScaleMatrixNoTranslation(Matrix toScale, Vector3 scale)
        {
            Matrix calcMatrix;

            calcMatrix = toScale;
            calcMatrix.M11 *= scale.X;
            calcMatrix.M12 *= scale.X;
            calcMatrix.M13 *= scale.X;

            calcMatrix.M21 *= scale.Y;
            calcMatrix.M22 *= scale.Y;
            calcMatrix.M23 *= scale.Y;

            calcMatrix.M31 *= scale.Z;
            calcMatrix.M32 *= scale.Z;
            calcMatrix.M33 *= scale.Z;

            return calcMatrix;
        }

        static public void SetMatrices(Matrix World)
        {
            Matrix V = Camera.CameraView;
            Matrix VI = Matrix.Invert(V);

            V = myCamera.View;
            VI = Matrix.Invert(V);

            effect.GetVariableByName("mWVP").AsMatrix().SetMatrix(World * V * Projection);
            effect.GetVariableByName("mVI").AsMatrix().SetMatrix(VI);
            effect.GetVariableByName("mW").AsMatrix().SetMatrix(World);
            effect.GetVariableByName("mWIT").AsMatrix().SetMatrix(Matrix.Transpose(Matrix.Invert(World)));
            
        }

        static public void UpdateLight()
        {
            LightPosition = new Vector3((float)(Plane.BSphere.Radius * 3.0f * Math.Cos(time)), 30, (float)(Plane.BSphere.Radius * 3.0f * Math.Sin(time)));
            effect.GetVariableByName("xLightPos").AsVector().Set(LightPosition);
        }



        static public void RenderFrame()
        {
            device.ClearDepthStencilView(depthview, DepthStencilClearFlags.Depth, 1, 0);
            device.ClearRenderTargetView(renderview, Color.Blue);
            device.OutputMerger.DepthStencilState = depthstate;
            UpdateLight();
            time += 0.001f;
           // if (count == 500) AddBox(); 
           // if (count > 1000) { AddBall(); count = 0; }
           // count++;
            
            EffectTechnique t = effect.GetTechniqueByName("SimpleLight");
            RasterizerStateDescription desc = new RasterizerStateDescription();
            desc.CullMode = CullMode.None;        
            desc.FillMode = FillMode.Wireframe;  //dlaczego to nei dziala?

            effect.GetVariableByName("xDiffuseTexture").AsResource().SetResource(DiffuseResource);
            //byle co, i tak renderuje tylko boxy i plane'y
            effect.GetVariableByName("xMaxMass").AsScalar().Set(MaxMass);
            effect.GetVariableByName("xMass").AsScalar().Set(1);

            
            SetMatrices(Matrix.RotationX(-(float)Math.PI / 2) * Matrix.Scaling(10, 10, 10) * Matrix.Translation(0, -0.2f, 0));
            Plane.Render(t);

            SetMatrices(Matrix.Scaling(10, 10, 10) * Matrix.Translation(0, 50f, -100));
            Plane.Render(t);


            SetMatrices(Matrix.RotationY((float)Math.PI / 2) * Matrix.Scaling(10, 10, 10) * Matrix.Translation(-100f, 50f, 0));
            Plane.Render(t);

            SetMatrices(Matrix.RotationY(-(float)Math.PI / 2) * Matrix.Scaling(10, 10, 10) * Matrix.Translation(100, 50f, 0));
            Plane.Render(t);

            SetMatrices(Matrix.RotationY(-(float)Math.PI) * Matrix.Scaling(10, 10, 10) * Matrix.Translation(0, 50f, 100));
            Plane.Render(t);

            SetMatrices(Matrix.RotationX((float)Math.PI / 2) * Matrix.Scaling(10, 10, 10) * Matrix.Translation(0, 100f, 0));
            Plane.Render(t);
            
            
            effect.GetVariableByName("xDiffuseTexture").AsResource().SetResource(Diffuse2Resource);


            int i=0;
            foreach (var M in BoxMatrices)
            {
                SetMatrices(M.Value);
                if (i < howManyBeginningActors)
                {
                    SetMatrices(ScaleMatrixNoTranslation(M.Value, scale));
                    effect.GetVariableByName("xMass").AsScalar().Set(Engine.Actors[i+7].Mass);
                }

                Boxes[i].Render(t);
                i++;
            }

            effect.GetVariableByName("xDiffuseTexture").AsResource().SetResource(Diffuse3Resource);

            i = 0;
            foreach (var M in BallMatrices)
            {
                SetMatrices(M.Value);
                Balls[i].Render(t);
                i++;
            }
            effect.GetVariableByName("xDiffuseTexture").AsResource().SetResource(Diffuse2Resource);
            foreach (var C in Engine.Scene.Cloths)
            {
                WavefrontLoader.WFOGeometry G = new WavefrontLoader.WFOGeometry(device, C, ClothGeometry);
                
                SetMatrices(Matrix.Identity);
                G.Render(device, t);

            }

            Engine.Step();  //dlaczego zmienia mi tutaj moje BoxMatrices? chyba w sumie musi zeby karta wiedziala co ma wyrenderowac
             
            
            swapchain.Present(0, PresentFlags.None);
            prevFrameX = curFrameX;
            prevFrameY = curFrameY;
        }

        public static void InitializePhysXEngine()
        {
            Engine = new PEngine();
            
            Engine.AddPlane(new StillDesign.PhysX.PlaneShapeDescription());           
            
            Engine.OnSimulationReady+=new PEngine.SimulationReadyDelegate(Engine_OnSimulationReady);

        }

        public static void Engine_OnSimulationReady(PEngine e, float time)
        {
            foreach (Actor a in e.Actors)
            {
                if (BoxMatrices.ContainsKey(a))
                {
                    BoxMatrices[a] = a.GlobalPose.As<Matrix>();
                }
                if (BallMatrices.ContainsKey(a))
                {
                    BallMatrices[a] = a.GlobalPose.As<Matrix>();
                }
            }
           // e.Scene.FlushCaches();
        }
    
        static public void DisposeAll()
        {

            effect.Dispose();
            renderview.Dispose();
            backbuffer.Dispose();
            device.Dispose();
            swapchain.Dispose();

            Diffuse.Dispose();
            Diffuse2.Dispose();
            Diffuse3.Dispose();
            BumpMap.Dispose();
            Reflection.Dispose();
            DiffuseResource.Dispose();
            Diffuse2Resource.Dispose();
            Diffuse3Resource.Dispose();
            BumpMapResource.Dispose();
            ReflectionResource.Dispose();

            for (int i = 0; i < Boxes.Count; i++)
            {
                Boxes[i].VertexStream.Dispose();
            }
            Boxes.Clear();
            BoxMatrices.Clear();

            for (int i = 0; i < Engine.Actors.Count; i++)
			{
                //jak to dokladnie nalezy zdisposowac ?
                Engine.Actors[i].Dispose();
			}
        }

        static void Main()
        {
            InitializeMainWindow();
            InitializeDevice();
            InitializeOutputMerger();
            InitializeGeometry();
            InitializeTextures();
            InitializeEffect();
            InitializePhysXEngine();
            InitializeWalls();
            InitializeBeginningActors();

            MessagePump.Run(MainWindow, RenderFrame);

            DisposeAll();
        }
    }
}
