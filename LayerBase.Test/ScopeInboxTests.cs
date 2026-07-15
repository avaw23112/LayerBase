using LayerBase.Scope;
using LayerBase.Core.Event;

namespace EventsTest;

[TestFixture]
public sealed class ScopeInboxTests
{
    [Test]
    public void Scope_call_inbox_preserves_response_and_control_capacity()
    {
        var inbox = ScopeBoundedInbox<int>.CreateCallInbox(
            new ScopeCallInboxOptions(capacity: 4, reservedForResponseAndControl: 1));

        Assert.That(inbox.TryEnqueue(1, ScopeAdmissionClass.Business), Is.EqualTo(ScopeEnqueueResult.Accepted));
        Assert.That(inbox.TryEnqueue(2, ScopeAdmissionClass.Business), Is.EqualTo(ScopeEnqueueResult.Accepted));
        Assert.That(inbox.TryEnqueue(3, ScopeAdmissionClass.Business), Is.EqualTo(ScopeEnqueueResult.Accepted));
        Assert.That(inbox.TryEnqueue(4, ScopeAdmissionClass.Business), Is.EqualTo(ScopeEnqueueResult.Full));
        Assert.That(inbox.TryEnqueue(40, ScopeAdmissionClass.Response), Is.EqualTo(ScopeEnqueueResult.Accepted));

        AssertDequeues(inbox, 1);
        AssertDequeues(inbox, 2);
        AssertDequeues(inbox, 3);
        AssertDequeues(inbox, 40);
        Assert.That(inbox.TryDequeue(out _), Is.False);
    }

    [Test]
    public void Scope_event_inbox_reserves_internal_and_critical_capacity()
    {
        var inbox = ScopeBoundedInbox<int>.CreateEventInbox(
            new ScopeEventInboxOptions(capacity: 5, reservedForInternal: 1, reservedForCritical: 1));

        Assert.That(inbox.TryEnqueue(1, ScopeAdmissionClass.Business), Is.EqualTo(ScopeEnqueueResult.Accepted));
        Assert.That(inbox.TryEnqueue(2, ScopeAdmissionClass.Business), Is.EqualTo(ScopeEnqueueResult.Accepted));
        Assert.That(inbox.TryEnqueue(3, ScopeAdmissionClass.Business), Is.EqualTo(ScopeEnqueueResult.Accepted));
        Assert.That(inbox.TryEnqueue(4, ScopeAdmissionClass.Business), Is.EqualTo(ScopeEnqueueResult.Full));
        Assert.That(inbox.TryEnqueue(50, ScopeAdmissionClass.Internal), Is.EqualTo(ScopeEnqueueResult.Accepted));
        Assert.That(inbox.TryEnqueue(60, ScopeAdmissionClass.Internal), Is.EqualTo(ScopeEnqueueResult.Full));
        Assert.That(inbox.TryEnqueue(70, ScopeAdmissionClass.Critical), Is.EqualTo(ScopeEnqueueResult.Accepted));

        AssertDequeues(inbox, 1);
        AssertDequeues(inbox, 2);
        AssertDequeues(inbox, 3);
        AssertDequeues(inbox, 50);
        AssertDequeues(inbox, 70);
        Assert.That(inbox.TryDequeue(out _), Is.False);
    }

    [Test]
    public void Scope_inbox_close_business_rejects_business_but_accepts_reserved_admission()
    {
        var callInbox = ScopeBoundedInbox<int>.CreateCallInbox(
            new ScopeCallInboxOptions(capacity: 3, reservedForResponseAndControl: 1));
        var eventInbox = ScopeBoundedInbox<int>.CreateEventInbox(
            new ScopeEventInboxOptions(capacity: 3, reservedForInternal: 1, reservedForCritical: 1));

        callInbox.CloseBusinessAdmission();
        eventInbox.CloseBusinessAdmission();

        Assert.That(callInbox.TryEnqueue(1, ScopeAdmissionClass.Business), Is.EqualTo(ScopeEnqueueResult.BusinessClosed));
        Assert.That(callInbox.TryEnqueue(2, ScopeAdmissionClass.Control), Is.EqualTo(ScopeEnqueueResult.Accepted));
        Assert.That(eventInbox.TryEnqueue(3, ScopeAdmissionClass.Business), Is.EqualTo(ScopeEnqueueResult.BusinessClosed));
        Assert.That(eventInbox.TryEnqueue(4, ScopeAdmissionClass.Critical), Is.EqualTo(ScopeEnqueueResult.Accepted));
    }

    [Test]
    public void Scope_inbox_close_all_rejects_everything()
    {
        var inbox = ScopeBoundedInbox<int>.CreateEventInbox(
            new ScopeEventInboxOptions(capacity: 3, reservedForInternal: 1, reservedForCritical: 1));

        inbox.CloseAllAdmission();

        Assert.That(inbox.TryEnqueue(1, ScopeAdmissionClass.Business), Is.EqualTo(ScopeEnqueueResult.Closed));
        Assert.That(inbox.TryEnqueue(2, ScopeAdmissionClass.Internal), Is.EqualTo(ScopeEnqueueResult.Closed));
        Assert.That(inbox.TryEnqueue(3, ScopeAdmissionClass.Critical), Is.EqualTo(ScopeEnqueueResult.Closed));
    }

    [Test]
    public void Scope_event_inbox_drains_envelopes_with_origin_route_class_and_payload()
    {
        var inbox = ScopeBoundedInbox<ScopeEventEnvelope>.CreateEventInbox(
            new ScopeEventInboxOptions(capacity: 3, reservedForInternal: 1, reservedForCritical: 1));
        var origin = new ScopeAddress(runtimeId: 7, runtimeGeneration: 2, scopeId: 1);
        var payload = new PayloadHandle(eventTypeId: 11, index: 4, version: 3);
        var envelope = new ScopeEventEnvelope(
            origin,
            routeId: 19,
            ScopeEventClass.Internal,
            payload);

        Assert.That(inbox.TryEnqueue(envelope, envelope.Class.ToAdmissionClass()), Is.EqualTo(ScopeEnqueueResult.Accepted));

        Assert.That(inbox.TryDequeue(out var actual), Is.True);
        Assert.That(actual.Origin, Is.EqualTo(origin));
        Assert.That(actual.RouteId, Is.EqualTo(19));
        Assert.That(actual.Class, Is.EqualTo(ScopeEventClass.Internal));
        Assert.That(actual.Payload, Is.EqualTo(payload));
        Assert.That(inbox.TryDequeue(out _), Is.False);
    }

    [Test]
    public void Scope_call_inbox_drains_request_and_response_envelopes_with_token_route_and_payload()
    {
        var inbox = ScopeBoundedInbox<ScopeCallEnvelope>.CreateCallInbox(
            new ScopeCallInboxOptions(capacity: 2, reservedForResponseAndControl: 1));
        var origin = new ScopeAddress(runtimeId: 9, runtimeGeneration: 3, scopeId: 2);
        var token = new ScopeCallToken(runtimeGeneration: 3, originScopeId: 2, sequence: 17, version: 5);
        var requestPayload = new PayloadHandle(eventTypeId: 21, index: 6, version: 1);
        var responsePayload = new PayloadHandle(eventTypeId: 22, index: 7, version: 1);
        var request = new ScopeCallEnvelope(
            ScopeCallEnvelopeKind.Request,
            ScopeCallClass.BusinessRequest,
            token,
            origin,
            routeId: 31,
            requestPayload,
            ScopeCallResult.None);
        var response = new ScopeCallEnvelope(
            ScopeCallEnvelopeKind.Response,
            ScopeCallClass.Response,
            token,
            origin,
            routeId: 31,
            responsePayload,
            ScopeCallResult.Succeeded);

        Assert.That(inbox.TryEnqueue(request, request.Class.ToAdmissionClass()), Is.EqualTo(ScopeEnqueueResult.Accepted));
        Assert.That(inbox.TryEnqueue(request, request.Class.ToAdmissionClass()), Is.EqualTo(ScopeEnqueueResult.Full));
        Assert.That(inbox.TryEnqueue(response, response.Class.ToAdmissionClass()), Is.EqualTo(ScopeEnqueueResult.Accepted));

        Assert.That(inbox.TryDequeue(out var actualRequest), Is.True);
        Assert.That(actualRequest.Kind, Is.EqualTo(ScopeCallEnvelopeKind.Request));
        Assert.That(actualRequest.Class, Is.EqualTo(ScopeCallClass.BusinessRequest));
        Assert.That(actualRequest.Token, Is.EqualTo(token));
        Assert.That(actualRequest.Origin, Is.EqualTo(origin));
        Assert.That(actualRequest.RouteId, Is.EqualTo(31));
        Assert.That(actualRequest.Payload, Is.EqualTo(requestPayload));
        Assert.That(actualRequest.Result.State, Is.EqualTo(ScopeCallTerminalState.None));

        Assert.That(inbox.TryDequeue(out var actualResponse), Is.True);
        Assert.That(actualResponse.Kind, Is.EqualTo(ScopeCallEnvelopeKind.Response));
        Assert.That(actualResponse.Class, Is.EqualTo(ScopeCallClass.Response));
        Assert.That(actualResponse.Token, Is.EqualTo(token));
        Assert.That(actualResponse.Payload, Is.EqualTo(responsePayload));
        Assert.That(actualResponse.Result.State, Is.EqualTo(ScopeCallTerminalState.Succeeded));
        Assert.That(inbox.TryDequeue(out _), Is.False);
    }

    private static void AssertDequeues(ScopeBoundedInbox<int> inbox, int expected)
    {
        Assert.That(inbox.TryDequeue(out var actual), Is.True);
        Assert.That(actual, Is.EqualTo(expected));
    }
}
