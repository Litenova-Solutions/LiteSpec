// The one money shape of the Claimwick web frontend: the wire amount stays a
// decimal string beside its three-letter code, and arithmetic runs on scaled
// integers, never Number.
export type Money = {
  amount: string;
  currency: string;
};

// Renders one amount in code form, such as EUR 125.00.
export function formatMoney(money: Money): string {
  return `${money.currency} ${money.amount}`;
}

// Splits a decimal string into scaled units and their scale, so 12.50 becomes
// 1250 units at scale 2. Only digits, one point, and a leading minus are
// accepted.
function splitAmount(amount: string): { units: bigint; scale: number } {
  const negative = amount.startsWith("-");
  const parts = (negative ? amount.slice(1) : amount).split(".");
  const [whole = "", fraction = ""] = parts;
  if (parts.length > 2 || !/^\d+$/.test(whole) || !/^\d*$/.test(fraction)) {
    throw new Error(`Invalid amount: ${amount}`);
  }
  const magnitude = BigInt(`${whole}${fraction}`);
  return { units: negative ? -magnitude : magnitude, scale: fraction.length };
}

// Renders scaled units back to a decimal string at the given scale.
function joinAmount(units: bigint, scale: number): string {
  const magnitude = units < 0n ? -units : units;
  const digits = magnitude.toString().padStart(scale + 1, "0");
  const body = scale === 0 ? digits : `${digits.slice(0, -scale)}.${digits.slice(-scale)}`;
  return `${units < 0n ? "-" : ""}${body}`;
}

// Adds two amounts of one currency on scaled integers. The sum keeps the wider
// scale of the two, so EUR 1.5 and EUR 2.25 add to EUR 3.75.
export function addMoney(left: Money, right: Money): Money {
  if (left.currency !== right.currency) {
    throw new Error(`Cannot add ${left.currency} to ${right.currency}.`);
  }
  const first = splitAmount(left.amount);
  const second = splitAmount(right.amount);
  const scale = Math.max(first.scale, second.scale);
  const units =
    first.units * 10n ** BigInt(scale - first.scale) + second.units * 10n ** BigInt(scale - second.scale);
  return { amount: joinAmount(units, scale), currency: left.currency };
}
