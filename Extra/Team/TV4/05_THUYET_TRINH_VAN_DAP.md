# TV4 - Presentation, video and oral defense

## Suggested demo sequence
1. Show start and TCP listener in ServerEngine.cs.
2. Explain size/name validation and why .part files exist.
3. Point out the concurrency gate and duplicate name reservation.
4. Run a duplicate filename demo and an interruption demo if available.

## Questions to practice
### 1. Why does the Server need a .part file?
A partial upload should not appear as a completed file.

### 2. When is COMPLETE sent?
After receiving the full payload, verifying digest and committing the final file.

### 3. What happens when two clients send the same filename?
Only one receives that reservation; the other gets a DUPLICATE response.

### 4. Why maintain a session limit?
To bound resource use under concurrent clients.

### 5. If Client A disconnects, what about Client B?
Their sessions are isolated; A failure should not stop B while Server and network remain healthy.

## Required recording and evidence
- Record the individual explaining THEIR component, including their face as required in the course.
- Use the actual running build, not a mocked successful result.
- Mention a limitation and a known failure case where appropriate.
- TV2 may combine members videos into a group video only AFTER receiving real recordings.
