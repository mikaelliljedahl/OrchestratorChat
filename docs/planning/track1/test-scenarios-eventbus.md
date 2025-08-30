# EventBus Test Scenarios

## Component Under Test: EventBus
**Location**: `src/OrchestratorChat.Core/Events/EventBus.cs`

---

## Test Categories

### 1. Subscription Tests

#### Test: Subscribe_SingleHandler_AddsSuccessfully
**Given**: EventBus instance
**When**: Subscribe<TestEvent>(handler) called
**Then**:
- Handler registered
- No exceptions thrown
- Handler receives events

#### Test: Subscribe_MultipleHandlers_AllRegistered
**Given**: EventBus instance
**When**: Multiple handlers subscribed to same event type
**Then**:
- All handlers registered
- All receive events when published
- Order of execution maintained

#### Test: Subscribe_DuplicateHandler_HandledGracefully
**Given**: Handler already subscribed
**When**: Same handler subscribed again
**Then**:
- Either ignores duplicate OR
- Throws meaningful exception
- Original subscription intact

#### Test: Subscribe_NullHandler_ThrowsArgumentNullException
**Given**: EventBus instance
**When**: Subscribe called with null handler
**Then**: Throws ArgumentNullException

#### Test: Subscribe_DifferentEventTypes_IsolatedCorrectly
**Given**: EventBus instance
**When**: Handlers for EventTypeA and EventTypeB subscribed
**Then**:
- EventTypeA publications only trigger A handlers
- EventTypeB publications only trigger B handlers
- No cross-contamination

---

### 2. Unsubscribe Tests

#### Test: Unsubscribe_ExistingHandler_RemovesSuccessfully
**Given**: Handler subscribed to event
**When**: Unsubscribe called
**Then**:
- Handler removed
- No longer receives events
- Other handlers unaffected

#### Test: Unsubscribe_NonExistentHandler_NoException
**Given**: Handler never subscribed
**When**: Unsubscribe called
**Then**:
- No exception thrown
- No side effects
- Returns gracefully

#### Test: Unsubscribe_OneOfMany_OthersRemain
**Given**: 3 handlers subscribed
**When**: Middle handler unsubscribed
**Then**:
- Only specified handler removed
- Other 2 still receive events
- Order preserved

#### Test: Unsubscribe_LastHandler_CleansUpEventType
**Given**: Single handler for event type
**When**: Handler unsubscribed
**Then**:
- Event type entry cleaned up
- Memory released
- No memory leaks

---

### 3. Publishing Tests

#### Test: PublishAsync_SingleSubscriber_ReceivesEvent
**Given**: One handler subscribed
**When**: PublishAsync called with event
**Then**:
- Handler invoked once
- Receives correct event data
- Completes successfully

#### Test: PublishAsync_MultipleSubscribers_AllReceive
**Given**: 5 handlers subscribed
**When**: PublishAsync called
**Then**:
- All 5 handlers invoked
- Each receives same event instance
- All complete before PublishAsync returns

#### Test: PublishAsync_NoSubscribers_NoException
**Given**: No handlers for event type
**When**: PublishAsync called
**Then**:
- No exception thrown
- Returns successfully
- Logs debug message

#### Test: PublishAsync_NullEvent_ThrowsArgumentNullException
**Given**: Handler subscribed
**When**: PublishAsync(null) called
**Then**: Throws ArgumentNullException

#### Test: Publish_Synchronous_BlocksUntilComplete
**Given**: Handler with 100ms delay
**When**: Publish (sync) called
**Then**:
- Blocks for full duration
- Returns after handler completes
- Thread blocked during execution

---

### 4. Error Handling Tests

#### Test: PublishAsync_HandlerThrows_ContinuesOthers
**Given**: 3 handlers, middle one throws exception
**When**: PublishAsync called
**Then**:
- Exception logged
- Other handlers still execute
- PublishAsync completes normally

#### Test: PublishAsync_AllHandlersThrow_StillCompletes
**Given**: All handlers throw exceptions
**When**: PublishAsync called
**Then**:
- All exceptions logged
- PublishAsync completes
- No unhandled exception

#### Test: PublishAsync_HandlerTimeout_HandledGracefully
**Given**: Handler with infinite loop
**When**: PublishAsync with timeout
**Then**:
- Timeout detected
- Other handlers execute
- Warning logged

#### Test: Subscribe_HandlerThrowsDuringExecution_DoesNotCorruptState
**Given**: Handler that throws on first call
**When**: Event published twice
**Then**:
- First call logs exception
- Second call still executes handler
- EventBus state remains valid

---

### 5. Thread Safety Tests

#### Test: ConcurrentSubscribe_ThreadSafe
**Given**: 10 threads
**When**: All subscribe handlers simultaneously
**Then**:
- All handlers registered
- No race conditions
- No lost subscriptions

#### Test: ConcurrentUnsubscribe_ThreadSafe
**Given**: 10 handlers subscribed, 10 threads
**When**: Each thread unsubscribes one handler
**Then**:
- All handlers removed
- No exceptions
- Clean final state

#### Test: ConcurrentPublish_ThreadSafe
**Given**: Handlers subscribed
**When**: 10 threads publish events simultaneously
**Then**:
- All events delivered
- No lost events
- Handlers receive all events

#### Test: SubscribeWhilePublishing_ThreadSafe
**Given**: Long-running event publication
**When**: New handler subscribed during publication
**Then**:
- Subscription succeeds
- New handler doesn't receive current event
- Receives future events

---

### 6. Memory Management Tests

#### Test: UnsubscribedHandlers_GarbageCollected
**Given**: Handler subscribed then unsubscribed
**When**: GC runs
**Then**:
- Handler object collected
- No memory leak
- WeakReference becomes null

#### Test: EventBus_NoMemoryLeakWithManyEvents
**Given**: EventBus instance
**When**: 10,000 events published
**Then**:
- Memory usage stable
- Old events not retained
- GC can collect event instances

#### Test: LargeEventPayload_HandledEfficiently
**Given**: Event with 1MB payload
**When**: Published to multiple handlers
**Then**:
- Same instance passed (no copying)
- Memory efficient
- Completes in reasonable time

---

### 7. Event Type Tests

#### Test: GenericEventTypes_HandledCorrectly
**Given**: Generic event type Event<T>
**When**: Different T types used
**Then**:
- Each type isolated
- Type safety maintained
- No type confusion

#### Test: EventInheritance_BaseClassHandlers
**Given**: DerivedEvent : BaseEvent
**When**: DerivedEvent published
**Then**:
- Only DerivedEvent handlers called
- BaseEvent handlers not called
- Type hierarchy respected

#### Test: InterfaceEvents_SupportedCorrectly
**Given**: Event implementing IEvent interface
**When**: Published
**Then**:
- Interface constraints respected
- Handlers receive correct type
- No casting errors

---

### 8. Performance Tests

#### Test: PublishAsync_1000Handlers_PerformsWell
**Given**: 1000 handlers subscribed
**When**: Event published
**Then**:
- Completes in under 100ms
- All handlers invoked
- No timeout issues

#### Test: Subscribe_Performance_Constant
**Given**: EventBus with N existing subscriptions
**When**: New subscription added
**Then**: O(1) time complexity

#### Test: PublishAsync_SmallEvent_MinimalOverhead
**Given**: Simple event with one field
**When**: Published to one handler
**Then**: Overhead < 1ms

---

### 9. Integration Tests

#### Test: EventBus_WithRealHandlers_WorksEndToEnd
**Given**: Real event handlers (not mocks)
**When**: Events published
**Then**: Business logic executes correctly

#### Test: EventBus_MultipleEventTypes_NoInterference
**Given**: 10 different event types registered
**When**: All published simultaneously
**Then**:
- Each handler receives only its events
- No cross-talk
- All complete successfully

---

### 10. Special Cases Tests

#### Test: RecursivePublish_HandledSafely
**Given**: Handler that publishes another event
**When**: Initial event published
**Then**:
- No stack overflow
- Both events processed
- Completes successfully

#### Test: SelfUnsubscribe_DuringHandling
**Given**: Handler that unsubscribes itself
**When**: Event published twice
**Then**:
- First call executes
- Handler unsubscribes itself
- Second call doesn't invoke handler

---

## Test Implementation Examples

```csharp
public class EventBusTests
{
    private EventBus _eventBus;
    private Mock<ILogger<EventBus>> _loggerMock;
    
    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<EventBus>>();
        _eventBus = new EventBus(_loggerMock.Object);
    }
    
    [Test]
    public async Task PublishAsync_MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        var received = new List<int>();
        Action<TestEvent> handler1 = e => received.Add(1);
        Action<TestEvent> handler2 = e => received.Add(2);
        Action<TestEvent> handler3 = e => received.Add(3);
        
        _eventBus.Subscribe(handler1);
        _eventBus.Subscribe(handler2);
        _eventBus.Subscribe(handler3);
        
        var testEvent = new TestEvent { Data = "test" };
        
        // Act
        await _eventBus.PublishAsync(testEvent);
        
        // Assert
        received.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }
    
    [Test]
    public async Task PublishAsync_HandlerThrows_OthersStillExecute()
    {
        // Arrange
        var executed = new List<int>();
        Action<TestEvent> handler1 = e => executed.Add(1);
        Action<TestEvent> handler2 = e => throw new Exception("Handler 2 error");
        Action<TestEvent> handler3 = e => executed.Add(3);
        
        _eventBus.Subscribe(handler1);
        _eventBus.Subscribe(handler2);
        _eventBus.Subscribe(handler3);
        
        // Act
        await _eventBus.PublishAsync(new TestEvent());
        
        // Assert
        executed.Should().BeEquivalentTo(new[] { 1, 3 });
        _loggerMock.Verify(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }
}
```

---

## Thread Safety Test Example

```csharp
[Test]
public async Task EventBus_ConcurrentOperations_ThreadSafe()
{
    // Arrange
    var eventCount = new ConcurrentDictionary<int, int>();
    var tasks = new List<Task>();
    
    // Act
    for (int i = 0; i < 10; i++)
    {
        var threadId = i;
        
        // Subscribe thread
        tasks.Add(Task.Run(() =>
        {
            _eventBus.Subscribe<TestEvent>(e => 
            {
                eventCount.AddOrUpdate(threadId, 1, (k, v) => v + 1);
            });
        }));
        
        // Publish thread
        tasks.Add(Task.Run(async () =>
        {
            for (int j = 0; j < 10; j++)
            {
                await _eventBus.PublishAsync(new TestEvent());
                await Task.Delay(1);
            }
        }));
    }
    
    await Task.WhenAll(tasks);
    
    // Assert
    eventCount.Should().HaveCount(10);
    eventCount.Values.Sum().Should().Be(1000); // 10 handlers * 100 events
}
```

---

## Test Event Classes

```csharp
public class TestEvent : IEvent
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string Data { get; set; }
}

public class AnotherTestEvent : IEvent
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public int Value { get; set; }
}

public class DerivedEvent : TestEvent
{
    public string AdditionalData { get; set; }
}
```

---

## Test Coverage Matrix

| Scenario | Unit Tests | Integration | Thread Safety | Performance |
|----------|------------|-------------|---------------|-------------|
| Subscribe | ✅ | ✅ | ✅ | ✅ |
| Unsubscribe | ✅ | ✅ | ✅ | ✅ |
| Publish | ✅ | ✅ | ✅ | ✅ |
| Error Handling | ✅ | ✅ | ✅ | ⚠️ |
| Memory Management | ✅ | ⚠️ | ✅ | ✅ |
| Type Safety | ✅ | ✅ | N/A | ⚠️ |

Legend: ✅ Required | ⚠️ Optional | N/A Not Applicable