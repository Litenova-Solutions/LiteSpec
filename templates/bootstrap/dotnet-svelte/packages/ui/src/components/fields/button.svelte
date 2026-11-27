<script lang="ts">
  import type { Snippet } from "svelte";
  import { cva, type VariantProps } from "class-variance-authority";
  import { cn } from "../../utils/cn.ts";

  const buttonVariants = cva(
    "inline-flex h-9 items-center justify-center gap-2 whitespace-nowrap rounded-md px-4 py-2 text-sm font-medium transition-colors outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50",
    {
      variants: {
        variant: {
          primary: "bg-primary text-primary-foreground hover:bg-primary/90",
          secondary: "bg-secondary text-secondary-foreground hover:bg-secondary/80",
          danger: "bg-destructive text-destructive-foreground hover:bg-destructive/90",
        },
      },
      defaultVariants: { variant: "primary" },
    },
  );

  type Props = {
    children: Snippet;
    variant?: VariantProps<typeof buttonVariants>["variant"];
    disabled?: boolean;
    type?: "button" | "submit";
    onclick?: (event: MouseEvent) => void;
  };

  let { children, variant = "primary", disabled = false, type = "button", onclick }: Props = $props();
</script>

<button {type} {disabled} {onclick} class={cn(buttonVariants({ variant }))}>
  {@render children()}
</button>
