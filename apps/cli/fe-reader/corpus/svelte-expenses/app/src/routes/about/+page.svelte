<script>
  import { formatMoney } from "#lib/app/helpers.ts";
  import { clientName } from "../../api-client/client.gen";

  const label = formatMoney(2);
  const name = clientName();
</script>

<h1>About</h1>
