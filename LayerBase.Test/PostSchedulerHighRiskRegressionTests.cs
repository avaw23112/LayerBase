using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public partial class RebuildEventPoliciesRegressionTests
{
    [SetUp]
    public void SetUp()
    {
        LayerHub.Reset();
    }

    public partial struct RebuildCoalescedEvent
    {
        public readonly int Key;
        public readonly int Amount;

        public RebuildCoalescedEvent(int key, int amount)
        {
            // key��
            //   ��ͨҵ�����
            //
            // amount��
            //   ���� metadata ���� coalesce key��
            //   ���������ò�ͬ amount ���벻ͬ coalesced slot��
            Key = key;
            Amount = amount;
        }
    }

    public sealed class RebuildCoalescedMetaData : EventMetaData<RebuildCoalescedEvent>
    {
        public override EventPostPolicy? PostPolicy =>
            new EventPostPolicy(
                // mode��
                //   ʹ�� Coalesced Ͷ��ģʽ��
                PostDeliveryMode.Coalesced,

                // backpressure��
                //   ������ʱ�ܾ����¼���
                BackpressurePolicy.RejectNew,

                // maxPending��
                //   0 ��ʾ������ pending ���ޡ�
                0,

                // mergeFailure��
                //   �ϲ�ʧ��ʱ�ܾ���
                //   ��� scheduler ��Ȼʹ�þ� policyTable��û�� metadata��
                //   �ڶ����¼������ͬһ��Ĭ�� key=0 slot��Ȼ��ϲ�ʧ�ܣ����ܾ���
                MergeFailurePolicy.Reject);

        public override int GetPostCoalesceKey(in RebuildCoalescedEvent value)
        {
            // Rebuild ����� metadata��
            //   �� Amount ���� coalesce key��
            //
            // ���������¼���
            //   Amount = 10 -> key = 1010
            //   Amount = 20 -> key = 1020
            //
            // ����Ӧ�ý���������ͬ coalesced slot�������ɷ� 2 �Ρ�
            return value.Key * 1000 + value.Amount;
        }

        public override bool TryMergePostEvent(
            ref RebuildCoalescedEvent current,
            in  RebuildCoalescedEvent next)
        {
            // current��
            //   ��ǰ slot �������¼���
            //
            // next��
            //   �������¼���
            //
            // ����������������ᴥ�� merge��
            // ��Ϊ�����¼��� coalesce key ��ͬ��
            current = new RebuildCoalescedEvent(
                current.Key,
                current.Amount + next.Amount);

            return true;
        }
    }

    private sealed class CaptureLayer : Layer
    {
        public int Count { get; private set; }
        public int TotalAmount { get; private set; }

        public CaptureLayer()
        {
            Subscribe<RebuildCoalescedEvent>(OnEvent);
        }

        private void OnEvent(in RebuildCoalescedEvent e)
        {
            // Count��
            //   �ɷ�������
            //
            // TotalAmount��
            //   �����ɷ��¼��� Amount �ܺ͡�
            Count++;
            TotalAmount += e.Amount;
        }
    }

    [Test]
    public void RebuildEventPolicies_Should_Update_PostScheduler_PolicyTable_When_Metadata_Is_Added_After_Build()
    {
        // �ؼ��㣺
        //   Build ǰû��ע�� RebuildCoalescedEvent �� metadata��
        //   ����ǰ���� EventTypeId���� BuildPlans �� EnsureEventCapacity(maxId)
        //   ������ΪĬ�� Normal �¼�ע���ȥ��
        _ = EventTypeId<RebuildCoalescedEvent>.Id;

        var layer = new CaptureLayer();

        var runtime = LayerHub.CreateLayers()
                              .Push(layer)
                              .Build();

        // Build ���ע�� metadata��
        // �ⲻ�ǡ����� metadata ͬʱ���ڡ������Ǵ��� metadata -> �� metadata��
        EventMetaDataHandler.RegisterMetaData<RebuildCoalescedEvent>(
            new RebuildCoalescedMetaData());

        // �ؼ����裺
        //   RebuildEventPolicies ������ PostScheduler �ڲ�Ҳ�л����µ� policyTable��
        runtime.RebuildEventPolicies();

        runtime.Post(new RebuildCoalescedEvent(
            key: 1,
            amount: 10));

        runtime.Post(new RebuildCoalescedEvent(
            key: 1,
            amount: 20));

        runtime.Pump(0.016f);

        // ��� PostScheduler �Ѿ�ʹ���µ� metadata��
        //   �����¼� coalesce key �ֱ�Ϊ 1010 �� 1020��
        //   ���ǲ���ϲ���Ӧ�ɷ� 2 �Ρ�
        //
        // ��� PostScheduler ��Ȼ���о� policyTable��
        //   meta == null��coalesce key Ĭ���� 0��
        //   �ڶ����¼��᳢�Խ���ͬһ�� slot��
        //   ��Ϊû�� metadata��TryMergePostEvent ����ִ�С�
        //   MergeFailurePolicy.Reject ��ܾ��ڶ����¼���
        //   ����ֻ���ɷ� 1 �Ρ�
        Assert.That(
            layer.Count,
            Is.EqualTo(2),
            "RebuildEventPolicies should update PostScheduler's policy table. " +
            "After metadata is added, Coalesced key calculation must use the new metadata.");

        Assert.That(
            layer.TotalAmount,
            Is.EqualTo(30),
            "Both coalesced events should be dispatched after rebuild.");
    }
}