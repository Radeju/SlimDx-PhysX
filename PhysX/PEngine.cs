using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ph=StillDesign.PhysX;
using PhMath = StillDesign.PhysX.MathPrimitives;
using System.IO;

namespace PhysX
{
    public class PEngine
    {

        public bool Paused;
        public bool Stoped;


        

        public delegate void SimulationReadyDelegate(PEngine sender, float TimeElapsed);

        public event SimulationReadyDelegate OnSimulationReady;

        public delegate void SumulationProcessingDelegate(PEngine sender);

        public event SumulationProcessingDelegate OnSimulationProcessing;



        public Ph.Scene Scene { get; private set; }
        public Ph.Core Core { get; private set; }
        public Ph.ReadOnlyList<Ph.Actor> Actors
        {
            get { return Scene.Actors; }
        }

        private long Time, LastTime;
        public float DeltaTime;


        public void Stop()
        {
            Stoped = true;
        }

        public void Pause()
        {
            Paused = true;
        }

        public void Resume()
        {
            Paused = false;
        }

        public PEngine()
        {

            Stoped = true;
            Paused = false;
            Time = 0;
            LastTime = 0;
            DeltaTime = 0;
            Ph.CoreDescription CoreDesc = new Ph.CoreDescription();
            
            PUserOutput UserOut = new PUserOutput();
            Ph.Core core = new Ph.Core(CoreDesc, UserOut);
            Core = core;

            Ph.SceneDescription SceneDesc = new Ph.SceneDescription();     
            //grawitacja
            SceneDesc.Gravity = new PhMath.Vector3(0, 0, 0);
            SceneDesc.TimestepMethod = Ph.TimestepMethod.Fixed;            
            SceneDesc.GroundPlaneEnabled = true;
            Ph.Scene scene = Core.CreateScene(SceneDesc);
            Scene = scene;

            Scene.DefaultMaterial.DynamicFriction = 0.5f;
            Scene.DefaultMaterial.StaticFriction = 0.5f;
            Scene.DefaultMaterial.Restitution = 0f;
            Scene.DefaultMaterial.RestitutionCombineMode = Ph.CombineMode.Minimum;
            Scene.DefaultMaterial.FrictionCombineMode = Ph.CombineMode.Max;

            core.SetParameter(Ph.PhysicsParameter.VisualizationScale,1.0f);
            core.SetParameter(Ph.PhysicsParameter.VisualizeCollisionShapes, true);
            core.SetParameter(Ph.PhysicsParameter.VisualizeClothMesh, true);
            core.SetParameter(Ph.PhysicsParameter.VisualizeJointLocalAxes, true);
            core.SetParameter(Ph.PhysicsParameter.VisualizeJointLimits, true);
            core.SetParameter(Ph.PhysicsParameter.VisualizeFluidPosition, true);
            core.SetParameter(Ph.PhysicsParameter.VisualizeFluidEmitters, false); // Slows down rendering a bit too much
            core.SetParameter(Ph.PhysicsParameter.VisualizeForceFields, true);
            core.SetParameter(Ph.PhysicsParameter.VisualizeSoftBodyMesh, true);

            Core.Foundation.RemoteDebugger.Connect("localhost");

        }

        public void AddSoftBody()
        {
            Ph.SoftBodyMeshDescription S = new Ph.SoftBodyMeshDescription();
        }


        public void UpdateTime()
        {
            Time = QueryPerformance.Counter();
            long freq = QueryPerformance.Frequency();
            DeltaTime = (float)(Time-LastTime)/(float)(freq);
            LastTime = Time;    
        }
            

        public Ph.Actor AddPlane(Ph.PlaneShapeDescription PlaneDesc)
        {
            Ph.ActorDescription ActorDesc = new Ph.ActorDescription();
            ActorDesc.Shapes.Add(PlaneDesc);
            return Scene.CreateActor(ActorDesc);
        }

        //dodano 
        // bool @static = false
        // ActorDesc.BodyDescription = @static ? null : new Ph.BodyDescription(mass);   - ActorDesc.BodyDescription = new Ph.BodyDescription(mass);
        public Ph.Actor AddBox(float sizex, float sizey, float sizez, float mass, PhMath.Vector3 InitialPosition, bool @static = false)
        {
            Ph.BoxShapeDescription BoxDesc = new Ph.BoxShapeDescription();
            BoxDesc.Dimensions = new PhMath.Vector3(sizex, sizey, sizez);
            BoxDesc.Mass = mass;

            Ph.ActorDescription ActorDesc = new Ph.ActorDescription();
            ActorDesc.Shapes.Add(BoxDesc);
            ActorDesc.BodyDescription = @static ? null : new Ph.BodyDescription(mass);
            ActorDesc.GlobalPose = PhMath.Matrix.Translation(InitialPosition);
            return Scene.CreateActor(ActorDesc);

        }

        public Ph.Actor AddBall(float radius, float mass, PhMath.Vector3 InitialPosition)
        {
            Ph.SphereShapeDescription BallDesc = new Ph.SphereShapeDescription();
            BallDesc.Mass = mass;
            BallDesc.Radius = radius;
            Ph.ActorDescription ActorDec = new Ph.ActorDescription();
            ActorDec.Shapes.Add(BallDesc);
            ActorDec.BodyDescription = new Ph.BodyDescription(mass);
            ActorDec.GlobalPose = PhMath.Matrix.Translation(InitialPosition);
            return Scene.CreateActor(ActorDec);
        }

        public Ph.Cloth AddCloth(WavefrontLoader.WFOGeometry G, PhMath.Vector3 InitialPosition)
        {
            Ph.ClothMeshDescription clothMeshDesc = new Ph.ClothMeshDescription();
            clothMeshDesc.AllocateVertices<WavefrontLoader.WFOVertex>(G.VertexCount);
            clothMeshDesc.AllocateTriangles<int>(G.IndexCount);

            clothMeshDesc.VertexCount = G.VertexCount;
            clothMeshDesc.TriangleCount = G.IndexCount/3;

            clothMeshDesc.VerticesStream.SetData(G.Vertices);
            clothMeshDesc.TriangleStream.SetData(G.Indices);
            
            clothMeshDesc.Flags &= ~Ph.MeshFlag.Indices16Bit;
            
            MemoryStream M =new MemoryStream();
            Ph.Cooking.InitializeCooking();
            Ph.Cooking.CookClothMesh(clothMeshDesc, M);
            Ph.Cooking.CloseCooking();
            M.Position = 0;

            Ph.ClothMesh CM = Core.CreateClothMesh(M);

            Ph.ClothDescription ClothDesc = new Ph.ClothDescription();
            ClothDesc.ClothMesh = CM;
            ClothDesc.Pressure = 4f;
            ClothDesc.SelfCollisionThickness = 3.0f;
            ClothDesc.Friction = 0.5f;
            ClothDesc.SleepLinearVelocity = 0.1f;
            ClothDesc.Thickness = 1f;
            ClothDesc.Flags = Ph.ClothFlag.Gravity | Ph.ClothFlag.Damping | Ph.ClothFlag.CollisionTwoway | Ph.ClothFlag.Pressure | Ph.ClothFlag.Bending;
            ClothDesc.GlobalPose = PhMath.Matrix.Translation(InitialPosition);            
            ClothDesc.MeshData.AllocatePositions<WavefrontLoader.WFOVertex>(G.VertexCount);
            ClothDesc.MeshData.AllocateIndices<int>(G.IndexCount);
            ClothDesc.MeshData.AllocateNormals<SlimDX.Vector3>(G.VertexCount);            
            ClothDesc.Thickness = 0.1f;
            ClothDesc.Density = 0.1f;
            ClothDesc.MeshData.MaximumVertices = G.VertexCount;
            ClothDesc.MeshData.MaximumIndices = G.IndexCount;

            ClothDesc.MeshData.NumberOfVertices = G.VertexCount;
            ClothDesc.MeshData.NumberOfIndices = G.IndexCount;
            
           // if (ClothDesc.CheckValid() == 0) return null;
            return Scene.CreateCloth(ClothDesc);
            
            
        }

        public Ph.Joint AddJointActors(Ph.Actor A1, Ph.Actor A2, PhMath.Vector3 An1, PhMath.Vector3 An2)
        {
            Ph.FixedJointDescription desc = new Ph.FixedJointDescription();
            desc.Actor1 = A1;
            desc.Actor2 = A2;
            desc.LocalAnchor1 = An1;
            desc.LocalAnchor2 = An2;
            desc.JointFlags = Ph.JointFlag.CollisionEnabled;
            desc.MaxForce = 100f;
            return Scene.CreateJoint(desc);
        }
        
        public void Start()
        {
            Stoped = false;
            if (Scene != null) Simulate();
        }


        public void Step()
        {
            UpdateTime();
            if (!Paused)
            {
                Scene.Simulate(DeltaTime);
                Scene.FlushStream();
            }
            while (!Scene.FetchResults(Ph.SimulationStatus.RigidBodyFinished, false))
            {
                if (OnSimulationProcessing != null) OnSimulationProcessing(this);
            }
            if (OnSimulationReady != null) OnSimulationReady(this, DeltaTime);
        }

        public void Step(float timestep)
        {
            UpdateTime();
            if (!Paused)
            {
                Scene.Simulate(timestep);
                Scene.FlushStream();
                
            }
            while (!Scene.FetchResults(Ph.SimulationStatus.RigidBodyFinished, false))
            {
                if (OnSimulationProcessing != null) OnSimulationProcessing(this);
            }
            if (OnSimulationReady != null) OnSimulationReady(this, timestep);
        }



        private void Simulate()
        {            
            while (!Stoped)
            {
                UpdateTime();
                if (!Paused)
                {
                    Scene.Simulate(DeltaTime);
                    Scene.FlushStream();
                }
                while (!Scene.FetchResults(Ph.SimulationStatus.RigidBodyFinished, false))
                {
                    if (OnSimulationProcessing != null) OnSimulationProcessing(this);
                }
                if (OnSimulationReady != null) OnSimulationReady(this, DeltaTime);
            }

        }

    }
}
