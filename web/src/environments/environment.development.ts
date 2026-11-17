import { Environment } from './environment.model';

// The dev server proxies /api to the API (proxy.conf.json), so requests stay same-origin.
export const environment: Environment = {
  production: false,
  apiBaseUrl: '',
};
