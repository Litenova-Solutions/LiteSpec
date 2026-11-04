# Show a Loading State on Every Route

**ID:** `demo/conventions#show-a-loading-state-on-every-route`

**Strength:** advice

**Applies to:** `apps/web/src/routes/**`

**Default:** Every route shows a loading state while its reads are in flight.

**Rationale:** A route without a loading state looks broken on a slow connection.

**Reviewed for:** Which routes show nothing while their reads are in flight? Look at the route pages.
