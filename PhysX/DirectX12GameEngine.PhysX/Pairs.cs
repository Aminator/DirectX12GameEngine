namespace DirectX12GameEngine.PhysX
{
    public partial struct PxContactModifyPair
    {
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 0, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        internal partial struct __Native
        {
            public System.IntPtr Actor;
            public System.IntPtr __Actor1;
            public System.IntPtr Shape;
            public System.IntPtr __Shape1;
            public DirectX12GameEngine.PhysX.PxTransform Transform;
            public DirectX12GameEngine.PhysX.PxTransform __Transform1;
            public DirectX12GameEngine.PhysX.PxContactSet Contacts;
        }

        internal unsafe void __MarshalFree(ref __Native @ref)
        {
        }

        internal unsafe void __MarshalFrom(ref __Native @ref)
        {
            Actor[0] = @ref.Actor != System.IntPtr.Zero ? new PxRigidActor(@ref.Actor) : null;
            Actor[1] = @ref.__Actor1 != System.IntPtr.Zero ? new PxRigidActor(@ref.__Actor1) : null;

            Shape[0] = @ref.Shape != System.IntPtr.Zero ? new PxShape(@ref.Shape) : null;
            Shape[1] = @ref.__Shape1 != System.IntPtr.Zero ? new PxShape(@ref.__Shape1) : null;

            fixed (void* __to = &Transform[0], __from = &@ref.Transform)
                SharpGen.Runtime.MemoryHelpers.CopyMemory((System.IntPtr)__to, (System.IntPtr)__from, 2 * sizeof(DirectX12GameEngine.PhysX.PxTransform));
            Contacts = @ref.Contacts;
        }

        internal unsafe void __MarshalTo(ref __Native @ref)
        {
            @ref.Actor = SharpGen.Runtime.CppObject.ToCallbackPtr<DirectX12GameEngine.PhysX.PxRigidActor>(Actor[0]);
            @ref.__Actor1 = SharpGen.Runtime.CppObject.ToCallbackPtr<DirectX12GameEngine.PhysX.PxRigidActor>(Actor[1]);

            @ref.Shape = SharpGen.Runtime.CppObject.ToCallbackPtr<DirectX12GameEngine.PhysX.PxShape>(Shape[0]);
            @ref.__Shape1 = SharpGen.Runtime.CppObject.ToCallbackPtr<DirectX12GameEngine.PhysX.PxShape>(Shape[1]);

            fixed (void* __from = &Transform[0], __to = &@ref.Transform)
                SharpGen.Runtime.MemoryHelpers.CopyMemory((System.IntPtr)__to, (System.IntPtr)__from, 2 * sizeof(DirectX12GameEngine.PhysX.PxTransform));
            @ref.Contacts = Contacts;
        }
    }

    public partial struct PxContactPair
    {
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 0, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        internal partial struct __Native
        {
            public System.IntPtr Shapes;
            public System.IntPtr __Shapes1;
            public System.IntPtr ContactPatches;
            public System.IntPtr ContactPoints;
            public System.IntPtr ContactImpulses;
            public System.UInt32 RequiredBufferSize;
            public System.Byte ContactCount;
            public System.Byte PatchCount;
            public System.UInt16 ContactStreamSize;
            public DirectX12GameEngine.PhysX.DummyEnum Flags;
            public DirectX12GameEngine.PhysX.DummyEnum Events;
            public System.UInt32 InternalData;
            public System.UInt32 __InternalData1;
        }

        internal unsafe void __MarshalFree(ref __Native @ref)
        {
        }

        internal unsafe void __MarshalFrom(ref __Native @ref)
        {
            Shapes[0] = @ref.Shapes != System.IntPtr.Zero ? new PxShape(@ref.Shapes) : null;
            Shapes[1] = @ref.__Shapes1 != System.IntPtr.Zero ? new PxShape(@ref.__Shapes1) : null;

            ContactPatches = @ref.ContactPatches;
            ContactPoints = @ref.ContactPoints;
            ContactImpulses = @ref.ContactImpulses;
            RequiredBufferSize = @ref.RequiredBufferSize;
            ContactCount = @ref.ContactCount;
            PatchCount = @ref.PatchCount;
            ContactStreamSize = @ref.ContactStreamSize;
            Flags = @ref.Flags;
            Events = @ref.Events;
            fixed (void* __to = &InternalData[0], __from = &@ref.InternalData)
                SharpGen.Runtime.MemoryHelpers.CopyMemory((System.IntPtr)__to, (System.IntPtr)__from, 2 * sizeof(System.UInt32));
        }

        internal unsafe void __MarshalTo(ref __Native @ref)
        {
            @ref.Shapes = SharpGen.Runtime.CppObject.ToCallbackPtr<DirectX12GameEngine.PhysX.PxShape>(Shapes[0]);
            @ref.__Shapes1 = SharpGen.Runtime.CppObject.ToCallbackPtr<DirectX12GameEngine.PhysX.PxShape>(Shapes[1]);

            @ref.ContactPatches = ContactPatches;
            @ref.ContactPoints = ContactPoints;
            @ref.ContactImpulses = ContactImpulses;
            @ref.RequiredBufferSize = RequiredBufferSize;
            @ref.ContactCount = ContactCount;
            @ref.PatchCount = PatchCount;
            @ref.ContactStreamSize = ContactStreamSize;
            @ref.Flags = Flags;
            @ref.Events = Events;
            fixed (void* __from = &InternalData[0], __to = &@ref.InternalData)
                SharpGen.Runtime.MemoryHelpers.CopyMemory((System.IntPtr)__to, (System.IntPtr)__from, 2 * sizeof(System.UInt32));
        }
    }

    public partial struct PxContactPairHeader
    {
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 0, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        internal partial struct __Native
        {
            public System.IntPtr Actors;
            public System.IntPtr __Actors1;
            public System.IntPtr ExtraDataStream;
            public System.UInt16 ExtraDataStreamSize;
            public DirectX12GameEngine.PhysX.DummyEnum Flags;
            public System.IntPtr Pairs;
            public System.UInt32 NbPairs;
        }

        internal unsafe void __MarshalFree(ref __Native @ref)
        {
        }

        internal unsafe void __MarshalFrom(ref __Native @ref)
        {
            Actors[0] = @ref.Actors != System.IntPtr.Zero ? new PxRigidActor(@ref.Actors) : null;
            Actors[1] = @ref.__Actors1 != System.IntPtr.Zero ? new PxRigidActor(@ref.__Actors1) : null;

            ExtraDataStream = @ref.ExtraDataStream;
            ExtraDataStreamSize = @ref.ExtraDataStreamSize;
            Flags = @ref.Flags;
            Pairs = @ref.Pairs;
            NbPairs = @ref.NbPairs;
        }

        internal unsafe void __MarshalTo(ref __Native @ref)
        {
            @ref.Actors = SharpGen.Runtime.CppObject.ToCallbackPtr<DirectX12GameEngine.PhysX.PxRigidActor>(Actors[0]);
            @ref.__Actors1 = SharpGen.Runtime.CppObject.ToCallbackPtr<DirectX12GameEngine.PhysX.PxRigidActor>(Actors[1]);

            @ref.ExtraDataStream = ExtraDataStream;
            @ref.ExtraDataStreamSize = ExtraDataStreamSize;
            @ref.Flags = Flags;
            @ref.Pairs = Pairs;
            @ref.NbPairs = NbPairs;
        }
    }
}
