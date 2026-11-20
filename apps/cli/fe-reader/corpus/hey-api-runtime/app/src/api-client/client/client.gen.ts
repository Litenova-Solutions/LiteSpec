export function createConfig(): { baseUrl: string } {
  return { baseUrl: "" };
}

export function buildUrl(path: string): string {
  return path;
}

export function createClient(config: { baseUrl: string }): { baseUrl: string } {
  return config;
}
