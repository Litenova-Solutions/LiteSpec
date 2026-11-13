# Keep the UI Source Lock

## Context

The UI source lock records the digest of each copied component, so a changed component shows in review. Teams computed those digests by hand, and no command wrote them, although the check needs them exact.

## Decision

`litespec model render` writes the lock. A person acknowledges a changed digest by committing it, and no person computes one.
