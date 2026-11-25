# TV2 - Presentation, video and oral defense

## Suggested demo sequence
1. Show endpoint settings and the file selection area.
2. Drag multiple real files into the table; explain duplicate and directory filtering.
3. Start an upload using the integrated program; point at per-file states and speeds.
4. Demonstrate an invalid endpoint and a clear error message, if the build supports it.

## Questions to practice
### 1. Does TV2 implement the TCP protocol?
No. ClientForm uses a service from TV3 and queue from TV5. This avoids mixing UI and networking.

### 2. Why does the GUI need async/await?
Network file transfers can wait for I/O; using asynchronous operations keeps the UI responsive.

### 3. Why identify a row by JobId?
Updates belong to a specific file; row indexing alone may change when items are removed or reordered.

### 4. Does 100 percent mean success?
No. Uploading may show 100 percent of bytes sent while the server has not confirmed COMPLETE.

### 5. What does DragDrop validate?
Only existing files, not directories or repeated paths; errors must be visible to the user.

## Required recording and evidence
- Record the individual explaining THEIR component, including their face as required in the course.
- Use the actual running build, not a mocked successful result.
- Mention a limitation and a known failure case where appropriate.
- TV2 may combine members videos into a group video only AFTER receiving real recordings.
