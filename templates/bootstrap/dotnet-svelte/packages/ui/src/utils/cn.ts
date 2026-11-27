// The one class merge of the __PROJECT__ shared UI package: every merge
// and variant helper import lives here, so routes read one vocabulary.
import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(...inputs));
}
