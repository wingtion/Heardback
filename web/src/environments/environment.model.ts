export interface Environment {
  readonly production: boolean;
  /** Base URL of the JobTrail API, without a trailing slash. Empty means same origin. */
  readonly apiBaseUrl: string;
}
