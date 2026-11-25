# TV2 - Report draft for the group

## Client interface and interaction design

The Client UI accepts a server IPv4 address, port, timeout, and one or more local files. It displays a row for each accepted file with name, size, progress, transfer speed, state and detail. Drag and drop is implemented in the WinForms form. The form delegates real networking to UploadClientService (TV3) and upload ordering to UploadQueue (TV5); it must never label a file Completed before a valid final server acknowledgement. Progress<T> is used to return transfer updates to the Windows UI context.

## Evidence to insert later

- The member's actual code commit URL / hash: [to fill].
- Screenshot of the real integrated application or test: [to fill].
- Test case IDs passed or failed: [fill after running tests].
- Bugs fixed and limitations: [fill from actual experience].

## Coordination notes

Needs Models.cs and Settings.cs from TV1, UploadClientService.cs from TV3, UploadQueue.cs from TV5, and shared Wire.cs from TV1.

**This is a suggested explanation based on the supplied source, not proof the member implemented or tested it. Edit after reviewing and running the integrated project.**
