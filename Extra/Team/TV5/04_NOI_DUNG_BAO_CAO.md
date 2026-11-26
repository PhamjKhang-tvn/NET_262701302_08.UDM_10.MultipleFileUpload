# TV5 - Report draft for the group

## Upload queue and per-file states

The queue accepts an ordered list of jobs and invokes the Client transfer service one file at a time. It prevents simultaneous executions in a single queue object and reports per-file states through TransferUpdate. A failure limited to one file is recorded as Error and the next file is attempted while the server remains available. If the service reports a connection outage, the queue stops sending and leaves unattempted jobs in Waiting. The product does not implement byte-level Pause/Resume. The UI derives progress and speed from transfer callbacks while waiting for the final server acknowledgement before displaying Completed.

## Evidence to insert later

- The member's actual code commit URL / hash: [to fill].
- Screenshot of the real integrated application or test: [to fill].
- Test case IDs passed or failed: [fill after running tests].
- Bugs fixed and limitations: [fill from actual experience].

## Coordination notes

Needs Models.cs (UploadJob, TransferUpdate) from TV1, UploadClientService.cs from TV3, and GUI state mapping from TV2.

**This is a suggested explanation based on the supplied source, not proof the member implemented or tested it. Edit after reviewing and running the integrated project.**
