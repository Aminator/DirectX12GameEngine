using System;

namespace DirectX12GameEngine.PhysX
{
    public enum DummyEnum
    {
    }

    [Flags]
    public enum PxErrors
    {
        NoError = 0,
        DebugInfo = 1,
        DebugWarning = 2,
        InvalidParameter = 4,
        InvalidOperation = 8,
        OutOfMemory = 16,
        InternalError = 32,
        Abort = 64,
        PerformanceWarning = 128,
        MaskAll = -1
    }

    public enum PxConcreteType
    {
        Undefined,

        HeightField,
        ConvexMesh,
        TriangleMeshBvh33,
        TriangleMeshBvh34,

        RigidDynamic,
        RigidStatic,
        Shape,
        Material,
        Constraint,
        Aggregate,
        Articulation,
        ArticulationReducedCoordinate,
        ArticulationLink,
        ArticulationJoint,
        ArticulationJointReducedCoordinate,
        PruningStructure,
        BvhStructure,

        PhysXCoreCount,
        FirstPhysXExtension = 256,
        FirstVehicleExtension = 512,
        FirstUserExtension = 1024
    }

    public enum PxGeometryType
    {
        Sphere,
        Plane,
        Capsule,
        Box,
        ConvexMesh,
        TriangleMesh,
        HeightField,
        GeometryCount,
        Invalid = -1
    }

    public enum PxHeightFieldFormat
    {
        S16TM = 1 << 0
    }

    public enum PxArticulationDriveType
    {
        Force = 0,
        Acceleration = 1,
        Target = 2,
        Velocity = 3,
        None = 4
    }

    [Flags]
    public enum PxConstraintFlags
    {
        Broken = 1 << 0,
        ProjectToActor0 = 1 << 1,
        ProjectToActor1 = 1 << 2,
        Projection = ProjectToActor0 | ProjectToActor1,
        CollisionEnabled = 1 << 3,
        Visualization = 1 << 4,
        DriveLimitsAreForces = 1 << 5,
        ImprovedSlerp = 1 << 7,
        DisablePreProssesing = 1 << 8,
        EnableExtendedLimits = 1 << 9,
        GpuCompatible = 1 << 10
    }

    [Flags]
    public enum PxArticulationMotions
    {
        Locked = 0,
        Limited = 1,
        Free = 2
    };

    public enum PxArticulationJointType
    {
        Prismatic = 0,
        Revolute = 1,
        Spherical = 2,
        Fix = 3,
        Undefined = 4
    };

    [Flags]
    public enum PxArticulationFlags
    {
        FixBase = 1 << 0,
        DriveLimitsAreForces = 1 << 1
    }

    public enum PxArticulationAxis
    {
        Twist = 0,
        Swing1 = 1,
        Swing2 = 2,
        X = 3,
        Y = 4,
        Z = 5,
        Count = 6
    };

    public enum PxPairFilteringMode
    {
        Keep,
        Suppress,
        Kill,
        Default = Suppress
    };

    public enum PxBroadPhaseType
    {
        Sap,
        Mbp,
        Abo,
        Gpu,

        Last
    }

    public enum PxFrictionType
    {
        Patch,
        OneDirectional,
        TwoDirectional,
        FrictionCount
    }

    public enum PxSolverType
    {
        Pgs,
        Tgs
    };

    public enum PxPruningStructureType
    {
        None,
        DynamicAabbTree,
        StaticAabbTree,

        Last
    }

    public enum PxSceneQueryUpdateMode
    {
        BuildEnabledCommitEnabled,
        BuildEnabledCommitDisabled,
        BuildDisabledCommitDisabled
    };

    [Flags]
    public enum PxPairFlags
    {
        SolveContact = 1 << 0,
        ModifyContacts = 1 << 1,
        NotifyTouchFound = 1 << 2,
        NotifyTouchPersists = 1 << 3,
        NotifyTouchLost = 1 << 4,
        NotifyTouchCcd = 1 << 5,
        NotifyThresholdForceFound = 1 << 6,
        NotifyThresholdForcePersists = 1 << 7,
        NotifyThresholdForceLost = 1 << 8,
        NotifyContactPoints = 1 << 9,
        DetectDiscreteContact = 1 << 10,
        DetectCcdContact = 1 << 11,
        PreSolverVelocity = 1 << 12,
        PostSolverVelocity = 1 << 13,
        ContactEventPose = 1 << 14,

        NextFree = 1 << 15,
        ContactDefault = SolveContact | DetectDiscreteContact,
        TriggerDefault = NotifyTouchFound | NotifyTouchLost | DetectDiscreteContact
    }

    public enum PxTaskType
    {
        Cpu,
        NotPresent,
        Completed
    }

    public enum PxJointActorIndex
    {
        Actor0,
        Actor1,
        Count
    }

    public enum PxActorType
    {
        RigidStatic,
        RigidDynamic,
        ArticulationLink,

        ActorCount,

        ActorForceDword = 0x7fffffff
    }

    public enum PxArticulationJointDriveType
    {
        Target = 0,
        Error = 1
    }

    public enum PxForceMode
    {
        Force,
        Impulse,
        VelocityChange,
        Acceleration
    }

    public enum PxBodyState
    {
        DynamicBody = 1 << 0,
        StaticBody = 1 << 1,
        KinematicBody = 1 << 2,
        Articulation = 1 << 3
    }
}
