# MUD Server Scalability Review

This project is currently a good prototype, but it is not yet ready to handle thousands of concurrent MUD players. The main limitations are in the networking, session concurrency, broadcast model, and missing world/game architecture.

## Current Architecture

Runtime flow today:

1. `Mud.Server.Program` starts `TcpConnectionManager` on port `4000`.
2. `TcpConnectionManager` accepts TCP sockets and creates `TcpConnection` instances.
3. `TcpConnection` reads input, builds messages through `MessageBuffer`, and raises `MessageReceived`.
4. `SessionManager` creates one `Session` per connection.
5. `Session` forwards input to the current `SessionState`.
6. `ConnectedState` sends a welcome message and handles a basic `exit` flow.

Important files:

- `Mud.Communication/Tcp/TcpConnectionManager.cs`
- `Mud.Communication/Tcp/TcpConnection.cs`
- `Mud.Communication/MessageBuffer.cs`
- `Mud.Core/Session/SessionManager.cs`
- `Mud.Core/Session/Session.cs`
- `Mud.Core/Session/State/ConnectedState.cs`
- `Mud.Server/Program.cs`

## Critical Issues

### 1. TCP layer will not scale

Problems:

- Reads one byte at a time.
- Uses old `BeginReceive` / `BeginSend` APIs.
- Allows overlapping socket sends.
- Has no per-user outgoing queue.
- Has no backpressure for slow clients.
- Uses `Listen(4)`, which is far too small for connection bursts.
- Disconnect handling can run multiple times concurrently.
- No command length limits.
- Message framing does not correctly handle multiple commands in one packet.

Recommended improvements:

- Replace with modern async TCP handling using `TcpListener.AcceptTcpClientAsync` or `SocketAsyncEventArgs`.
- Read into larger buffers.
- Parse complete lines from buffered input.
- Add max command size.
- Add per-connection outbound send queue.
- Add backpressure policy: drop, throttle, or disconnect slow clients.
- Make disconnect idempotent with atomic state.

### 2. Broadcast model is too simple

Current broadcast sends synchronously to every session:

```csharp
foreach (var session in sessions.Values)
{
    session.WriteToUser(message);
}
```

Problems:

- Every broadcast is global.
- No room/location broadcast.
- No area, party, admin, or nearby-player channels.
- No queueing/fanout control.
- Broadcasts can interleave with prompts and user commands.

Recommended improvements:

- Add scoped broadcast APIs:

```csharp
BroadcastToRoom(roomId, message, exceptPlayerId: senderId);
BroadcastToArea(areaId, message);
BroadcastGlobal(message);
SendToPlayer(playerId, message);
```

### 3. Session state is not thread-safe

Problems in `Session`:

- `currentState` is mutable without synchronization.
- `userAtPrompt` is mutable without synchronization.
- Socket callbacks, disconnects, broadcasts, and state changes can race.
- Prompt rendering can become corrupted when multiple messages arrive at once.

Recommended improvement:

Use a serialized event queue per session, for example with `Channel<SessionEvent>`. All user input, broadcasts, disconnects, and prompt writes should be processed by one session loop.

### 4. No real MUD world model yet

Missing core concepts:

- Player/character identity
- Login/authentication
- Rooms/locations
- Exits
- World/area/zone model
- Items
- NPCs
- Command parser
- Room-scoped interaction
- Persistence
- Action scheduling/game loop

The current `ConnectedState` is only a placeholder.

## Recommended Target Architecture

### Transport Layer

Responsibilities:

- Accept TCP connections.
- Read commands safely.
- Write messages through queued sends.
- Detect disconnects.
- Apply rate limits and backpressure.

Suggested API shape:

```csharp
public interface IConnection
{
    Guid Id { get; }
    IPAddress Ip { get; }
    ChannelReader<string> IncomingMessages { get; }
    ValueTask SendAsync(string message, CancellationToken cancellationToken);
    ValueTask DisconnectAsync(string reason, CancellationToken cancellationToken);
}
```

### Session Layer

Responsibilities:

- Own player connection state.
- Process input serially.
- Render prompts.
- Handle login/playing/disconnect states.

Suggested session states:

- `ConnectedState`
- `LoginState`
- `CharacterSelectionState`
- `PlayingState`
- `DisconnectedState`

### World/Game Layer

Responsibilities:

- Manage rooms, players, exits, items, and NPCs.
- Process movement and interactions.
- Broadcast to rooms/areas.
- Keep authoritative game state.

Possible initial model:

```csharp
public sealed class World
{
    public Dictionary<Guid, Room> Rooms { get; } = new();
    public Dictionary<Guid, Player> Players { get; } = new();
}

public sealed class Room
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public Dictionary<string, Guid> Exits { get; } = new();
    public HashSet<Guid> PlayerIds { get; } = new();
}
```

### Command Layer

Responsibilities:

- Parse text input.
- Dispatch commands to handlers.
- Support aliases.

Initial commands:

- `look`
- `say <message>`
- `go <direction>`
- `north`, `south`, `east`, `west`
- `who`
- `tell <player> <message>`
- `quit`

## Prioritized Implementation Plan

### Phase 1: Fix networking

- Replace one-byte reads with buffered async reads.
- Fix message framing.
- Support multiple commands per packet.
- Add max command length.
- Add per-connection send queues.
- Add slow-client backpressure.
- Make disconnect idempotent.
- Increase TCP accept backlog.

### Phase 2: Serialize session processing

- Add per-session event queue.
- Route all input, broadcasts, and disconnects through that queue.
- Remove `SessionManager.Current` global singleton.
- Prevent prompt/message interleaving.

### Phase 3: Add world model

- Add `World`, `Room`, `Player`, and `Exit`.
- Track which players are in which rooms.
- Add room-scoped broadcasting.
- Add movement between rooms.

### Phase 4: Add command system

- Create parser and command handlers.
- Implement basic movement and communication commands.
- Replace `input.Contains("exit")` with exact parsed commands.

### Phase 5: Production hardening

- Add logging.
- Add configuration.
- Add graceful shutdown.
- Add metrics.
- Add idle timeouts.
- Add command rate limits.
- Add load tests.
- Add tests for partial reads, multiple commands, slow clients, oversized input, and concurrent broadcasts.

## Bottom Line

Before adding complex gameplay, the server should first get a safe scalable foundation:

1. Modern async TCP server.
2. Per-connection send queues.
3. Backpressure and limits.
4. Serialized session event processing.
5. Room-scoped world broadcasting.

After that, the project can grow into a real MUD engine with rooms, players, commands, interactions, and real-time world updates.
