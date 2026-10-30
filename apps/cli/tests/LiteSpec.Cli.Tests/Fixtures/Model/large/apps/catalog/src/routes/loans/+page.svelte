<script module lang="ts">
  import type { LiteSpecMark } from "$lib/app/litespec-records";

  export const litespec = { implements: "screen/library-app.loans" } as const satisfies LiteSpecMark;
</script>

<script>
  import { borrowCopy, cancelHold, extendLoan, listLoans, placeHold, registerCopy, retireCopy, returnCopy } from '@fixture/api';

  async function borrow() {
    await registerCopy({ copyId: 'copy-1' });
    await borrowCopy({ copyId: 'copy-1' });
    await extendLoan({ loanId: 'loan-1' });
    await listLoans({});
    await returnCopy({ loanId: 'loan-1' });
    await placeHold({ copyId: 'copy-1' });
    await cancelHold({ holdId: 'hold-1' });
    await retireCopy({ copyId: 'copy-1' });
  }
</script>

<button on:click={borrow}>Borrow</button>
