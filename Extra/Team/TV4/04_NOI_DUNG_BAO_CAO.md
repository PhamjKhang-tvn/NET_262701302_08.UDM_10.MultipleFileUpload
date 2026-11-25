# TV4 - Report draft for the group

## TCP Server engine and storage integrity

The Server engine accepts real TCP client connections using TcpListener. It validates each UPLOAD request before writing data, including filename, nonnegative size and configured limits. It reserves the target filename to prevent duplicate or concurrent conflicting uploads. Incoming bytes are written to a temporary .part file while SHA-256 is accumulated. Only after exact-length reception and a valid END digest does the server flush and move the file to the final name without overwriting existing data, then emit COMPLETE. Separate sessions and a configured limit reduce fault propagation and overload.

## Evidence to insert later

- The member's actual code commit URL / hash: [to fill].
- Screenshot of the real integrated application or test: [to fill].
- Test case IDs passed or failed: [fill after running tests].
- Bugs fixed and limitations: [fill from actual experience].

## Coordination notes

Needs Wire.cs, Models.cs and Settings.cs from TV1; protocol agreement with TV3 and ServerForm.cs from TV6.

**This is a suggested explanation based on the supplied source, not proof the member implemented or tested it. Edit after reviewing and running the integrated project.**
