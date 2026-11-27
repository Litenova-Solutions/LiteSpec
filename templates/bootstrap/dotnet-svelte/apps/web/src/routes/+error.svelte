<script lang="ts">
  import { page } from "$app/state";
  import { TextLink } from "@__SCOPE__/ui/floorplans";
</script>

<h1>Something went wrong</h1>
<p role="alert">
  The page could not be shown (status {page.status}). {page.error?.message ??
    "Try again in a moment."}
</p>
<p><TextLink href="/">Back to home</TextLink></p>
