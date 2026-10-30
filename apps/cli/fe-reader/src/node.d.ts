// Minimal ambient declarations for the Node.js APIs the reader uses. The reader
// depends only on typescript, svelte and esbuild, so it carries these few
// signatures instead of the @types/node package. node: prefixed imports stay
// external to the bundle (platform=node); only the signatures below are used.

declare module "node:fs" {
  export function existsSync(path: string): boolean;
  export function mkdirSync(path: string, options: { recursive: boolean }): void;
  export function readdirSync(path: string): string[];
  export function readFileSync(path: string, encoding: "utf8"): string;
  export function readFileSync(fd: number, encoding: "utf8"): string;
  export function statSync(path: string): { isDirectory(): boolean; isFile(): boolean };
  export function writeFileSync(path: string, data: string, encoding: "utf8"): void;
  export function writeFileSync(fd: number, data: string, encoding: "utf8"): void;
}

declare module "node:path" {
  export const sep: string;
  export function dirname(path: string): string;
  export function join(...paths: string[]): string;
  export function relative(from: string, to: string): string;
  export function resolve(...paths: string[]): string;
}

declare module "node:crypto" {
  export interface Hash {
    update(data: string): Hash;
    digest(encoding: "hex"): string;
  }
  export function createHash(algorithm: string): Hash;
}

declare module "node:process" {
  export function exit(code: number): never;
}
