using LayerBase;
using LayerBase.Core.Event;
using LayerBase.Event.EventMetaData;
using LayerBase.Layers;
using NUnit.Framework;

namespace EventsTest;

[TestFixture]
public class RebuildEventPoliciesRegressionTests
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
            // key：
            //   普通业务键。
            //
            // amount：
            //   用于 metadata 生成 coalesce key。
            //   本测试中让不同 amount 进入不同 coalesced slot。
            Key = key;
            Amount = amount;
        }
    }

    public sealed class RebuildCoalescedMetaData : EventMetaData<RebuildCoalescedEvent>
    {
        public override EventPostPolicy? PostPolicy =>
            new EventPostPolicy(
                // mode：
                //   使用 Coalesced 投递模式。
                PostDeliveryMode.Coalesced,

                // backpressure：
                //   队列满时拒绝新事件。
                BackpressurePolicy.RejectNew,

                // maxPending：
                //   0 表示不启用 pending 上限。
                0,

                // mergeFailure：
                //   合并失败时拒绝。
                //   如果 scheduler 仍然使用旧 policyTable，没有 metadata，
                //   第二个事件会进入同一个默认 key=0 slot，然后合并失败，被拒绝。
                MergeFailurePolicy.Reject);

        public override int GetPostCoalesceKey(in RebuildCoalescedEvent value)
        {
            // Rebuild 后的新 metadata：
            //   让 Amount 参与 coalesce key。
            //
            // 这样两个事件：
            //   Amount = 10 -> key = 1010
            //   Amount = 20 -> key = 1020
            //
            // 它们应该进入两个不同 coalesced slot，最终派发 2 次。
            return value.Key * 1000 + value.Amount;
        }

        public override bool TryMergePostEvent(
            ref RebuildCoalescedEvent current,
            in  RebuildCoalescedEvent next)
        {
            // current：
            //   当前 slot 中已有事件。
            //
            // next：
            //   新来的事件。
            //
            // 这个测试里正常不会触发 merge，
            // 因为两个事件的 coalesce key 不同。
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
            // Count：
            //   派发次数。
            //
            // TotalAmount：
            //   所有派发事件的 Amount 总和。
            Count++;
            TotalAmount += e.Amount;
        }
    }

    [Test]
    public void RebuildEventPolicies_Should_Update_PostScheduler_PolicyTable_When_Metadata_Is_Added_After_Build()
    {
        // 关键点：
        //   Build 前没有注册 RebuildCoalescedEvent 的 metadata。
        //   但提前触发 EventTypeId，让 BuildPlans 的 EnsureEventCapacity(maxId)
        //   把它作为默认 Normal 事件注册进去。
        _ = EventTypeId<RebuildCoalescedEvent>.Id;

        var layer = new CaptureLayer();

        var runtime = LayerHub.CreateLayers()
                              .Push(layer)
                              .Build();

        // Build 后才注册 metadata。
        // 这不是“两个 metadata 同时存在”，而是从无 metadata -> 有 metadata。
        EventMetaDataHandler.RegisterMetaData<RebuildCoalescedEvent>(
            new RebuildCoalescedMetaData());

        // 关键步骤：
        //   RebuildEventPolicies 必须让 PostScheduler 内部也切换到新的 policyTable。
        runtime.RebuildEventPolicies();

        runtime.PostCoalesced(new RebuildCoalescedEvent(
            key: 1,
            amount: 10));

        runtime.PostCoalesced(new RebuildCoalescedEvent(
            key: 1,
            amount: 20));

        runtime.Pump(0.016f);

        // 如果 PostScheduler 已经使用新的 metadata：
        //   两个事件 coalesce key 分别为 1010 和 1020。
        //   它们不会合并，应派发 2 次。
        //
        // 如果 PostScheduler 仍然持有旧 policyTable：
        //   meta == null，coalesce key 默认是 0。
        //   第二个事件会尝试进入同一个 slot。
        //   因为没有 metadata，TryMergePostEvent 不会执行。
        //   MergeFailurePolicy.Reject 会拒绝第二个事件。
        //   最终只会派发 1 次。
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