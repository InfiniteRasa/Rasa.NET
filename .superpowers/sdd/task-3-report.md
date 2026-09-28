# Task 3 report: protocol and connection hardening

## Status

Completed and committed as `b9fb0a6` (`Harden protocol framing and connection teardown`).

## Scope and architecture

The implementation ports the protocol guarantees from roadmap commits `6b9979d` and `8362888` without cherry-picking either commit.

PR #95's networking architecture remains intact:

- `LengthedSocket` still permits one active receive per socket through `_receiveDispatching` and `_receiveDeferred`.
- Outbound frames are still serialized through `_sendLock`, `_sendQueue`, and the single in-flight `_sending` state.
- Game socket callbacks still copy inbound bytes into `_pendingChunks`.
- The game main loop still drains those chunks into `NonContiguousMemoryStream`, decodes frames, and performs game-state mutation.
- Existing per-client close guards remain in auth, game, and queue. Login handshakes and the underlying socket now also have one-shot close guards.

The roadmap `ProtocolPacketDecoder` class was not introduced because it would duplicate or replace PR #95's main-loop-owned decoder. Its observable fragmentation, ordering, malformed-frame, and peer-isolation behavior is covered through the existing `LengthedSocket`, `NonContiguousMemoryStream`, and `ProtocolPacket` APIs instead.

## Implementation

### Framing and compression

- `ProtocolPacket.Read` copies exactly the declared frame into a rented bounded buffer before parsing.
- DEFLATE input is therefore limited to the current frame and cannot consume the next frame.
- `ProtocolInflater` verifies:
  - exact expanded length;
  - final DEFLATE block completion;
  - no synthetic lookahead consumption;
  - no trailing compressed bytes;
  - invalid data/history rejection.
- `ProtocolPacket.Read` rejects undersized, oversized, truncated, and under-consumed frames.
- Read and write buffers are returned in `finally` blocks on success and all failure paths.
- The Game project references the already configured `BouncyCastle.Cryptography` 2.7.0 package used by Auth. No new package or version was introduced.

### Protocol values and payload boundaries

- Exact byte reads now throw `EndOfStreamException` for truncated payloads.
- Protocol flags, checksums, malformed seven-bit values, and unsupported Python encodings/types produce protocol-data errors.
- Python collection and string counts are checked before reading or allocation.
- Protocol string writers now prefix UTF-8 byte length rather than UTF-16 character count.
- RPC method payloads reject unknown methods and trailing bytes.

### Handshakes and teardown

- Auth credential reads require the complete encrypted credential block.
- Game key exchange accepts only 1-64 byte keys and requires all declared bytes.
- Queue and login handshakes use one reader per delivered frame and reject malformed/truncated data.
- Game cipher frames validate block length and padding before exposing payload bytes.
- `LengthedSocket.Close` is idempotent, so `OnDisconnect`, send-queue cleanup, shutdown, and handle closure run once.
- Login handshake failure paths now call one idempotent teardown path.
- Game pending-chunk enqueue and discard share a lock. A receive racing disconnect either enqueues before the final drain or observes the disconnected state and returns its rented array.

## Tests added

- Array-pool ownership tracking for compressed reads, parse failures, invalid DEFLATE, and write failures.
- Exact compressed-frame boundaries, following-frame isolation, truncated final blocks, trailing compressed bytes, forged expanded lengths, and non-contiguous input.
- Fragmented length-prefixed delivery at every byte boundary, multiple frames in one receive, invalid transport lengths, failed decryption, cipher padding, and idempotent socket close.
- Auth/game/queue handshake truncation and key bounds.
- Protocol flags, checksums, truncated counts/values, signed seven-bit integers, and UTF-8 byte lengths.
- RPC unknown-method, invalid-type, negative-count, negative-length, and trailing-payload boundaries.
- Game inbound buffer ownership during a receive/disconnect race.

## TDD evidence

### RED

Initial focused port:

```text
Failed: 37, Passed: 27, Total: 64
```

Failures covered compressed buffer ownership, truncated handshakes, frame boundaries, following-frame isolation, malformed DEFLATE, protocol value errors, failed decryption, and RPC boundaries.

The additional UTF-8 regression was also observed failing before its production change:

```text
ProtocolStringsUseUtf8ByteLength
expected: "é"
actual:   "�"
Failed: 1, Passed: 0, Total: 1
```

### GREEN

Focused protocol/networking/cryptography suite:

```text
Passed: 83, Failed: 0, Skipped: 0
```

Command:

```text
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~CompressedPacketTests|FullyQualifiedName~ProtocolFramingTests|FullyQualifiedName~ProtocolInflaterTests|FullyQualifiedName~HandshakeValidationTests|FullyQualifiedName~ProtocolValueReaderTests|FullyQualifiedName~RpcPayloadTests|FullyQualifiedName~LengthedSocketTests|FullyQualifiedName~GameInboundOwnershipTests"
```

## Final verification

Dependency restore, using the configured Microsoft feed:

```text
dotnet restore Rasa.NET.sln --source https://packagefeedproxy.microsoft.io/nuget/v3/index.json
```

Full solution tests, run once after focused tests passed:

```text
dotnet test Rasa.NET.sln --no-restore
Passed: 103, Failed: 0, Skipped: 0
```

Release build:

```text
dotnet build Rasa.NET.sln -c Release --no-restore --nologo
0 Warning(s)
0 Error(s)
```

`git diff --check` passed before commit.

## Warnings and concerns

- A focused Debug rebuild emitted existing analyzer warnings in the unchanged `src\Rasa.Test\Memory\NonContiguousMemoryStreamTests.cs`: `MSTEST0017` at lines 81-83, 140-146, 173-174, and 199-200; `CA2022` at lines 133, 138, 169, and 195. The final full test run and Release build were clean.
- Native client interoperability was not exercised. Automated coverage verifies framing and transport behavior against the repository's protocol fixtures.
- No roadmap gameplay, world, loot, ability, mission, database-model, or migration changes were imported.

## Blocking review follow-up (2026-09-18)

All blocking Task 3 review findings were fixed while preserving PR #95's serialized receive/send architecture and main-loop handoff:

- Expanded protocol payloads now have the previous measured ceiling of `4 * ushort.MaxValue` (`262140` bytes). `ProtocolPacket` rejects larger declarations before copying compressed bytes, and `ProtocolInflater` rejects them before creating its output stream or renting its work buffer. The exact boundary is tested.
- Login exchange completion and close now compete through one atomic lifecycle transition. `LoginManager.ExchangeDone` cannot invoke the game-client callback after close wins.
- Queue lifecycle state is synchronized in a dedicated narrow state gate. The enqueue action only runs when the `InQueue` transition succeeds, so a disconnect-winning transition cannot enqueue.
- Auth login, game key, queue key, and queue login payload readers now require full reader exhaustion.
- `ArrayPoolTracker` counts every rent and return per array ID. Missing returns, unmatched returns, and duplicate returns fail ownership assertions.
- `GameInboundOwnershipTests` now drives an encrypted frame through a real loopback `LengthedSocket`, blocks at the actual `ArrayPool` rent, closes through `Client.Close`, and verifies the receive callback returns the chunk exactly once. It no longer mutates client private state or invokes private methods.

### Follow-up TDD evidence

#### Expanded-length ceiling

RED command:

```text
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~ProtocolInflaterTests|FullyQualifiedName~ForgedExpandedLengthDoesNotReserveTheClaimedBuffer" --nologo
```

RED output:

```text
ProtocolInflaterTests.cs(70,79): error CS0117: 'ProtocolPacket' does not contain a definition for 'MaxExpandedSize'
ProtocolInflaterTests.cs(76,44): error CS0117: 'ProtocolPacket' does not contain a definition for 'MaxExpandedSize'
ProtocolInflaterTests.cs(83,81): error CS0117: 'ProtocolPacket' does not contain a definition for 'MaxExpandedSize'
ProtocolInflaterTests.cs(86,76): error CS0117: 'ProtocolPacket' does not contain a definition for 'MaxExpandedSize'
```

GREEN command:

```text
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~ProtocolInflaterTests|FullyQualifiedName~ForgedExpandedLengthDoesNotReserveTheClaimedBuffer" --nologo
```

GREEN output:

```text
Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12, Duration: 57 ms - Rasa.Test.dll (net10.0)
```

#### Login teardown race

RED command:

```text
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~ClosedLoginCannotCompleteItsExchange" --nologo
```

RED output:

```text
Failed ClosedLoginCannotCompleteItsExchange [60 ms]
expected: 0
actual:   1
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 82 ms - Rasa.Test.dll (net10.0)
```

GREEN command:

```text
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~ClosedLoginCannotCompleteItsExchange" --nologo
```

GREEN output:

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 65 ms - Rasa.Test.dll (net10.0)
```

#### Queue teardown race

RED command:

```text
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~QueueDisconnectPreventsTheEnqueueAction" --nologo
```

RED output:

```text
HandshakeValidationTests.cs(126,68): error CS1660: Cannot convert lambda expression to type 'QueueState' because it is not a delegate type
```

GREEN command:

```text
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~QueueDisconnectPreventsTheEnqueueAction" --nologo
```

GREEN output:

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 25 ms - Rasa.Test.dll (net10.0)
```

#### Handshake trailing bytes

RED command:

```text
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~RejectsTrailingBytes" --nologo
```

RED output:

```text
Failed AuthLoginRejectsTrailingBytes [15 ms]
Failed GameKeyRejectsTrailingBytes [< 1 ms]
Failed QueueKeyRejectsTrailingBytes [< 1 ms]
Failed QueueLoginRejectsTrailingBytes [< 1 ms]
Failed!  - Failed:     4, Passed:     1, Skipped:     0, Total:     5, Duration: 65 ms - Rasa.Test.dll (net10.0)
```

GREEN command:

```text
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~RejectsTrailingBytes" --nologo
```

GREEN output:

```text
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 58 ms - Rasa.Test.dll (net10.0)
```

#### Exact pooled ownership and real receive/disconnect teardown

RED command:

```text
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~ArrayPoolTrackerTests|FullyQualifiedName~GameInboundOwnershipTests" --nologo
```

RED output:

```text
ArrayPoolTrackerTests.cs: error CS1061: 'ArrayPoolTracker' does not contain a definition for 'RecordRent'
ArrayPoolTrackerTests.cs: error CS1061: 'ArrayPoolTracker' does not contain a definition for 'RecordReturn'
GameInboundOwnershipTests.cs(50,21): error CS1739: The best overload for 'ArrayPoolTracker' does not have a parameter named 'currentThreadOnly'
GameInboundOwnershipTests.cs(67,35): error CS1061: 'ArrayPoolTracker' does not contain a definition for 'AllReturned'
```

GREEN command:

```text
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~ArrayPoolTrackerTests|FullyQualifiedName~GameInboundOwnershipTests" --nologo
```

GREEN output:

```text
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 52 ms - Rasa.Test.dll (net10.0)
```

### Follow-up final verification

Focused protocol/networking/handshake/ownership command:

```text
dotnet test src\Rasa.Test\Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~CompressedPacketTests|FullyQualifiedName~ProtocolFramingTests|FullyQualifiedName~ProtocolInflaterTests|FullyQualifiedName~HandshakeValidationTests|FullyQualifiedName~ProtocolValueReaderTests|FullyQualifiedName~RpcPayloadTests|FullyQualifiedName~LengthedSocketTests|FullyQualifiedName~GameInboundOwnershipTests|FullyQualifiedName~ArrayPoolTrackerTests" --nologo
```

Output:

```text
Passed!  - Failed:     0, Passed:    94, Skipped:     0, Total:    94, Duration: 347 ms - Rasa.Test.dll (net10.0)
```

Full solution test command, run once after the focused suite passed:

```text
dotnet test Rasa.NET.sln --no-restore --nologo
```

Output:

```text
Passed!  - Failed:     0, Passed:   114, Skipped:     0, Total:   114, Duration: 501 ms - Rasa.Test.dll (net10.0)
```

Release build command:

```text
dotnet build Rasa.NET.sln -c Release --no-restore --nologo -v:q
```

Output:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:00.87
```

`git diff --check` passed. Native-client interoperability remains untested; automated protocol and loopback transport coverage is green.
