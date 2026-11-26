# TV5 - Presentation, video and oral defense

## Suggested demo sequence
1. Show three files in a queue with their initial Waiting status.
2. Show sequential progression, per-file progress and reported speed.
3. Explain good-file -> duplicate -> good-file behavior when the Server remains available.
4. Point to RunAsync and explain why a connection outage leaves other files Waiting.

## Questions to practice
### 1. Can UDM_10 upload multiple files sequentially?
Yes: a queue serves many files with one active upload per Client, and the limit is declared.

### 2. What is the difference between queue and parallel upload?
Queue is ordered and sequential; parallel runs multiple file transfers at the same time.

### 3. Does local file error stop the queue?
No; a file-specific failure is isolated and the next file can proceed.

### 4. What if Server goes offline?
The current file errors and unattempted jobs remain Waiting until the user resumes the queue.

### 5. Is retry the same as Pause/Resume?
No. This project restarts the failed file from byte zero rather than resuming by offset.

## Required recording and evidence
- Record the individual explaining THEIR component, including their face as required in the course.
- Use the actual running build, not a mocked successful result.
- Mention a limitation and a known failure case where appropriate.
- TV2 may combine members videos into a group video only AFTER receiving real recordings.
