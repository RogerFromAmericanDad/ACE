using System.Collections.Generic;
using System.Numerics;
using ACE.Entity;
using ACE.Server.Physics.Animation;
using ACE.Server.Physics.Common;

namespace ACE.Server.Physics
{
    public class PartArray
    {
        public static readonly Sphere DefaultSortingSphere;

        public static readonly float DefaultStepHeight = 0.0099999998f;

        public uint PAState;
        public PhysicsObj Owner;
        public Sequence Sequence;
        public MotionTableManager MotionTableManager;
        public Setup Setup;
        public int NumParts;
        public List<PhysicsPart> Parts;
        public Vector3 Scale;
        public AnimFrame LastAnimFrame;
        
        public bool AllowsFreeHeading()
        {
            return Setup.AllowFreeHeading;
        }

        public void AnimationDone(bool success)
        {
            if (MotionTableManager != null)
                MotionTableManager.AnimationDone(success);
        }

        public bool CacheHasPhysicsBSP()
        {
            if (Parts == null)
            {
                PAState &= 0xFFFEFFFF;
                return false;
            }

            foreach (var part in Parts)
            {
                //foreach (var gfxObj in part.GfxObj)
                //{
                    if (part.GfxObj.PhysicsBSP == null)
                    {
                        PAState &= 0xFFFEFFFF;
                        return false;
                    }
                //}
            }
            PAState |= 0x10000;
            return true;
        }

        public void CheckForCompletedMotions()
        {
            if (MotionTableManager != null)
                MotionTableManager.CheckForCompletedMotions();
        }

        public static PartArray CreateMesh(int setupDID)
        {
            // TODO
            return null;
        }

        public static PartArray CreateParticle(PhysicsObj owner, int numParts, Sphere sortingSphere = null)
        {
            var parts = new PartArray();
            parts.Owner = owner;
            parts.Sequence.SetObject(owner);

            parts.Setup = Setup.MakeParticleSetup(numParts);

            if (parts.Setup == null || !parts.InitParts())
                return null;

            return parts;
        }

        public static PartArray CreateSetup(PhysicsObj owner, int setupDID, bool createParts)
        {
            var parts = new PartArray();
            parts.Owner = owner;
            parts.Sequence.SetObject(owner);
            if (!parts.SetSetupID(setupDID, createParts))
                return null;
            parts.SetPlacementFrame(0x65);
            return parts;
        }

        public void DestroyParts()
        {
            Parts.Clear();
            NumParts = 0;
        }

        public Sequence DoInterpretedMotion(int motion, MovementParameters movementParameters)
        {
            if (MotionTableManager == null) return null;    // 7?

            var frame = new Frame();
            frame.Origin = Vector3.Zero;
            frame.Orientation = Quaternion.Identity;    // correct?
            // cache frame
            var mvs = new MovementStruct();
            return MotionTableManager.PerformMovement(mvs, Sequence);   // mvs.Motion?
        }

        public TransitionState FindObjCollisions(Transition transition)
        {
            foreach (var part in Parts)
            {
                var result = part.FindObjCollisions(transition);
                if (result != TransitionState.OK)
                    return result;
            }
            return TransitionState.OK;
        }

        public BoundingBox GetBoundingBox()
        {
            var bbox = new BoundingBox();

            // accumulate from each part?
            foreach (var part in Parts)
            {
                var partBox = part.GetBoundingBox();
                bbox.Min = partBox.Min;
                bbox.Max = partBox.Max;
                bbox.ConvertToGlobal();
                bbox.CalcSize();
            }
            return bbox;
        }

        public List<CylSphere> GetCylSphere()
        {
            return Setup.CylSphere;
        }

        public float GetHeight()
        {
            return Setup.Height * Scale.Z;
        }

        public int GetNumCylsphere()
        {
            return Setup.NumCylsphere;
        }

        public int GetNumSphere()
        {
            return Setup.NumSphere;
        }

        public float GetRadius()
        {
            return Setup.Radius * Scale.Z;
        }

        public Sphere GetSelectionSphere(Sphere selectionSphere)
        {
            if (Setup == null) return null;

            return new Sphere(new Vector3(selectionSphere.Center.X * Scale.X, selectionSphere.Center.Y * Scale.Y, selectionSphere.Center.Z * Scale.Z), selectionSphere.Radius * Scale.Z);
        }

        public Sphere GetSortingSphere()
        {
            if (Setup == null)
                return DefaultSortingSphere;
            else
                return Setup.SortingSphere;
        }

        public List<Sphere> GetSphere()
        {
            return Setup.Sphere;
        }

        public float GetStepDownHeight()
        {
            if (Setup == null) return DefaultStepHeight;

            return Setup.StepDownHeight * Scale.Z;
        }

        public float GetStepUpHeight()
        {
            if (Setup == null) return DefaultStepHeight;

            return Setup.StepUpHeight * Scale.Z;
        }

        public void HandleMovement()
        {
            MotionTableManager.UseTime();
        }

        public bool HasAnims()
        {
            return Sequence.HasAnims();
        }

        public void InitDefaults()
        {
            if (Setup.DefaultAnimID != 0)
            {
                Sequence.ClearAnimations();
                var animData = new AnimData();
                var defaultAnimId = Setup.DefaultAnimID;
                animData.AnimId = 0;
                animData.LowFrame = -1;
                animData.HighFrame = 1106247680;
                Sequence.AppendAnimation(animData);
                WeenieDesc.Destroy(animData);
            }

            if (Owner != null)
                Owner.InitDefaults(Setup);
        }

        public void InitializeMotionTables()
        {
            if (MotionTableManager != null)
                MotionTableManager.InitializeState(Sequence);
        }

        public bool InitObjDescChanges()
        {
            var result = false;

            if (Setup == null) return false;

            foreach (var part in Parts)
            {
                if (part != null && part.InitObjDescChanges())
                    result = true;
            }

            return result;
        }

        public void InitPals()
        {
        }

        public bool InitParts()
        {
            NumParts = Setup.NumParts;
            Parts = new List<PhysicsPart>(NumParts);

            for (var i = 0; i < NumParts; i++)
                Parts.Add(null);

            if (Setup.Parts == null) return true;

            var created = 0;
            for (var i = 0; i < NumParts; i++)
            {
                Parts[i] = Setup.Parts[i].MakePhysicsPart();
                if (Parts[i] == null)
                    break;

                created++;
            }

            if (created == NumParts)
            {
                for (var i = 0; i < NumParts; i++)
                {
                    Parts[i].PhysObj = Owner;
                    Parts[i].PhysObjIndex = i;
                }
                if (Setup.DefaultScale != null)
                {
                    for (var i = 0; i < NumParts; i++)
                        Parts[i].GfxObjScale = Setup.DefaultScale[i];
                }
                return true;
            }

            return false;
        }

        public bool MorphToExistingObject(PartArray obj)
        {
            DestroyParts();
            Setup = obj.Setup;
            // add reference?
            Scale = new Vector3(obj.Scale.X, obj.Scale.Y, obj.Scale.Z);
            NumParts = obj.NumParts;
            Parts = new List<PhysicsPart>(obj.NumParts);
            InitPals();
            for (var i = 0; i < NumParts; i++)
            {
                Parts[i] = obj.Parts[i].MakePhysicsPart();
                Parts[i].PhysObj = Owner;
                Parts[i].PhysObjIndex = i;
                // removed palette references
            }
            return true;
        }

        public void RemoveParts(ObjCell cell)
        {
            foreach (var part in Parts)
            {
                if (part != null)
                    cell.RemovePart(part);
            }
        }

        public void SetCellID(int cellID)
        {
            foreach (var part in Parts)
            {
                if (part != null)
                    part.Pos.ObjCellID = cellID;
            }
        }

        public void SetFrame(AFrame frame)
        {
            UpdateParts(frame);
            // remove lights
        }

        public bool SetMeshID(int meshDID)
        {
            if (meshDID == 0) return false;
            var setup = Setup.MakeSimpleSetup(meshDID);
            if (setup == null) return false;
            DestroyParts();
            Setup = setup;
            return InitParts();
        }

        public bool SetMotionTableID(int mtableID)
        {
            if (MotionTableManager != null)
            {
                if (MotionTableManager.GetMotionTableID(mtableID) == mtableID)
                    return true;

                MotionTableManager = null;
            }
            if (mtableID == 0) return true;

            // chat blob?
            return false;
        }

        public bool SetPart(List<DatLoader.Entity.AnimationPartChange> changes)
        {
            if (Setup == null) return false;

            var success = true;
            foreach (var change in changes)
            {
                var partIdx = change.PartIndex;

                if (partIdx < NumParts)
                {
                    if (Parts[partIdx] != null)
                    {
                        if (Parts[partIdx].SetPart((int)change.PartID))
                            continue;
                    }
                }
                success = false;
            }
            return success;
        }

        public bool SetPlacementFrame(int placementID)
        {
            PlacementType placementFrame = null;
            Setup.PlacementFrames.TryGetValue(placementID, out placementFrame);
            if (placementFrame != null)
            {
                //while (placementID != placementFrame.AnimFrame)
                // figure out dictionary key iteration
            }
            return false;
        }

        public bool SetScaleInternal(Vector3 newScale)
        {
            // is needed?
            return false;
        }

        public bool SetSetupID(int setupID, bool createParts)
        {
            if (Setup == null || Setup.m_DID != setupID)
            {
                // get new qualified DID?
                DestroyParts();
                if (Setup != null)
                    Setup = null;
                // get Setup from DBObj
                if (createParts)
                {
                    if (!InitParts())
                        return false;
                }
                InitDefaults();
            }
            return true;
        }

        public void SetTranslucencyInternal(float translucency)
        {

        }

        public Sequence StopCompletelyInternal()
        {
            if (MotionTableManager == null) return null;    // 7?
            var frame = new Frame();
            frame.Origin = Vector3.Zero;
            frame.Orientation = Quaternion.Identity;
            // add frame to cache
            var mvs = new MovementStruct();
            return MotionTableManager.PerformMovement(mvs, Sequence);   // how to add frame data?
        }

        public Sequence StopInterpretedMotion(int motion, MovementParameters movementParameters)
        {
            if (MotionTableManager == null) return null;    // 7?
            var frame = new Frame();
            frame.Origin = Vector3.Zero;
            frame.Orientation = Quaternion.Identity;
            // add frame to cache
            var mvs = new MovementStruct();
            return MotionTableManager.PerformMovement(mvs, Sequence);   // how to add frame/input data?
        }

        public void Update(double quantum, AFrame offsetFrame)
        {
            Sequence.Update(quantum, offsetFrame);
        }

        public void UpdateParts(AFrame frame)
        {
            var animFrame = Sequence.GetCurrAnimFrame();
            if (animFrame == null) return;
            var numParts = NumParts;
            if (numParts > animFrame.NumParts)
                numParts = animFrame.NumParts;
            for (var i = 0; i < numParts; i++)
            {
                Parts[i].Pos.Frame.Combine(frame, animFrame.Frame[i], Scale);
            }
        }

        public void SetNoDrawInternal(bool noDraw)
        {

        }

        public void HandleEnterWorld()
        {

        }

        public void InitLights()
        {

        }

        public void DestroyLights()
        {

        }

        public void SetDiffusionInternal(float diff)
        {

        }

        public void SetPartDiffusionInternal(int partIdx, float diff)
        {
        }

        public void SetLightingInternal(float luminosity, float diffuse)
        {

        }

        public bool SetPartLightingInternal(int partIdx, float luminosity, float diffuse)
        {
            return false;
        }

        public void SetLuminosityInternal(float luminosity)
        {

        }

        public void SetPartLuminosityInternal(int partIdx, float luminosity)
        {

        }

        public void SetPartTextureVelocityInternal(int partIdx, float du, float dv)
        {

        }

        public void SetPartTranslucencyInternal(int partIdx, float translucency)
        {

        }

        public void SetTextureVelocityInternal(float du, float dv)
        {

        }

        public int GetDataID()
        {
            return -1;
        }

        public int GetSetupID()
        {
            return -1;
        }

        public void AddPartsShadow(ShadowObj shadowObj)
        {

        }

        public void UpdateViewerDistance(float cypt, Vector3 heading)
        {

        }

        public void RemoveLightsFromCell(ObjCell cell)
        {

        }

        public void RestoreLightingInternal()
        {

        }
    }
}
