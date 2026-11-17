export interface Environment {
  readonly production: boolean;
  /** Base URL of the Heardback API, without a trailing slash. Empty means same origin. */
  readonly apiBaseUrl: string;
}
