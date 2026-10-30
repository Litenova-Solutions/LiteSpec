<script>
  import { formatMoney } from "$lib/app/helpers";
  import { clientName } from "../../api-client/client";

  const label = formatMoney(2);
  const name = clientName();
</script>

<h1>About</h1>
