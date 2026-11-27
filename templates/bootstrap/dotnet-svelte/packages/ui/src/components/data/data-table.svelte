<script lang="ts">
  import TextLink from "../fields/text-link.svelte";

  // A column with href names the row key that holds the address its cell
  // opens, so each row links to its own record without a second list.
  type Column = {
    key: string;
    label: string;
    numeric?: boolean;
    href?: string;
  };

  type Props = {
    caption: string;
    columns: Column[];
    rows: Record<string, string>[];
  };

  let { caption, columns, rows }: Props = $props();
</script>

<!-- A narrow screen scrolls the table inside its own region, never the page. -->
<div class="relative w-full overflow-x-auto outline-none focus-visible:ring-2 focus-visible:ring-ring" role="region" aria-label={caption} tabindex="0">
  <table class="w-full caption-bottom text-sm">
    <caption class="py-2 text-left text-sm text-muted-foreground">{caption}</caption>
    <thead>
      <tr class="border-b">
        {#each columns as column (column.key)}
          <th scope="col" class="h-10 px-2 text-left align-middle font-medium text-muted-foreground">
            {column.label}
          </th>
        {/each}
      </tr>
    </thead>
    <tbody>
      {#each rows as row, index (index)}
        <tr class="border-b transition-colors hover:bg-muted/50">
          {#each columns as column (column.key)}
            <td class="p-2 align-middle tabular-nums">
              {#if column.href && row[column.href]}
              <TextLink href={row[column.href] ?? ""}>{row[column.key] ?? ""}</TextLink>
            {:else}
              {row[column.key] ?? ""}
            {/if}
            </td>
          {/each}
        </tr>
      {/each}
    </tbody>
  </table>
</div>
