namespace LayerBase.Core.EventStateTrace;

    internal struct Slot<T> where T : struct
    {
        public T Value;
        public ushort Version;
        public bool InUse;
        public int NextFree;
        public bool Completed;
    }

    internal struct SlotRef
    {
        public SlotRef(int globalIndex, ushort version)
        {
            GlobalIndex = globalIndex;
            Version = version;
        }
        
        /// <summary>
        /// 当前节点在整个内存空间的相对位置
        /// </summary>
        public int GlobalIndex { get; }
        
        /// <summary>
        /// 由于GlobalIndex会被复用,需要额外参数查重.
        /// </summary>
        public ushort Version { get; }
        
        /// <summary>
        /// EventStateToken和SlotRef共用同一组数据
        /// </summary>
        /// <returns></returns>
        public EventStateToken ToToken() => new(GlobalIndex, Version);
    }
    
    /// <summary>
    /// freelist
    /// </summary>
    internal sealed class FreeList<T> where T : struct
    {
        /// <summary>
        /// 总内存空间
        /// </summary>
        private readonly List<Slot<T>[]> _slabs = new();
        
        /// <summary>
        /// 每组Slot[]的固定长度
        /// </summary>
        private readonly int _slabSize;
        
        /// <summary>
        /// 空闲节点头指针
        /// </summary>
        private int _freeHead = -1;

        public FreeList(int slabSize)
        {
            _slabSize = slabSize;
        }
        
        
        /// <summary>
        /// 租用新的slot
        /// </summary>
        /// <returns></returns>
        public SlotRef Rent()
        {
            //当目前空间不足时,即_freeHead指向最后一个节点的nextFree时,重新开辟内存
            if (_freeHead == -1)
            {
                AllocateSlab();
            }
            
            //取出最新可用空闲节点
            int globalIndex = _freeHead;
            ref var slot = ref GetSlot(globalIndex);
            _freeHead = slot.NextFree;
            
            //将已经分配的slot移除出空闲链表
            slot.NextFree = -1;
            slot.InUse = true;
            slot.Completed = false;
            slot.Version = NextVersion(slot.Version);
            
            //返回slot引用
            return new SlotRef(globalIndex, slot.Version);
        }
        
        /// <summary>
        /// 使用原始方式直接获取Slot
        /// </summary>
        /// <param name="token"></param>
        /// <param name="slotRef"></param>
        /// <returns></returns>
        public bool TryBorrow(int GlobalIndex,int Version, out SlotRef slotRef)
        {
            if (!TryValidate(GlobalIndex,Version, out var globalIndex))
            {
                slotRef = default;
                return false;
            }

            ref var slot = ref GetSlot(globalIndex);
            slotRef = new SlotRef(globalIndex, slot.Version);
            return true;
        }
        
        public ref Slot<T> Resolve(SlotRef slotRef)
        {
            return ref GetSlot(slotRef.GlobalIndex);
        }
        
        /// <summary>
        /// 回收Slot
        /// </summary>
        /// <param name="slotRef"></param>
        /// <param name="pathPool"></param>
        public void Release(in SlotRef slotRef)
        {
            ref var slot = ref GetSlot(slotRef.GlobalIndex);
            
            if (!slot.InUse || slot.Version != slotRef.Version)
            {
                return;
            }
            
            //重置当前Slot
            slot.Value = default;
            slot.InUse = false;
            slot.Completed = false;
            slot.Version = NextVersion(slot.Version);
            
            //延申freeList,使当前已经被释放的slot成为freeList的头节点.
            slot.NextFree = _freeHead;
            _freeHead = slotRef.GlobalIndex;
        }
        
        /// <summary>
        /// 验证EventStateToken对应的SlotRef是否存在,如果存在则返回Slot的位置下标,否则返回0.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="globalIndex"></param>
        /// <returns></returns>
        private bool TryValidate(int GlobalIndex,int Version, out int globalIndex)
        {
            int slabIndex = GlobalIndex / _slabSize;
            if (slabIndex < 0 || slabIndex >= _slabs.Count)
            {
                globalIndex = default;
                return false;
            }

            int slotIndex = GlobalIndex - slabIndex * _slabSize;
            ref var slot = ref _slabs[slabIndex][slotIndex];
            if (!slot.InUse || slot.Version != Version)
            {
                globalIndex = default;
                return false;
            }

            globalIndex = GlobalIndex;
            return true;
        }

        private ref Slot<T> GetSlot(int globalIndex)
        {
            int slabIndex = globalIndex / _slabSize;
            int slotIndex = globalIndex - slabIndex * _slabSize;
            return ref _slabs[slabIndex][slotIndex];
        }
        
        /// <summary>
        /// slab扩容
        /// </summary>
        private void AllocateSlab()
        {
            //计算目前容器的总大小
            int baseIndex = _slabs.Count * _slabSize;
            var slab = new Slot<T>[_slabSize];
            
            //倒序配置内存
            for (int i = _slabSize - 1; i >= 0; i--)
            {
                //新内存中正序的最后一个节点的NextFree指向-1,代表新空间的尽头
                //倒数第二个节点的NextFree指向最后一个节点,后续以此类推.假设slabSize=6,则新内存的NextFree如下: 1,2,3,4,5,-1
                slab[i].NextFree = _freeHead;
                _freeHead = baseIndex + i;
            }
            _slabs.Add(slab);
        }
        
        /// <summary>
        /// 递进当前Slot的版本
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private static ushort NextVersion(ushort current)
        {
            ushort next = (ushort)(current + 1);
            if (next == 0)
            {
                next = 1;
            }
            return next;
        }
    }