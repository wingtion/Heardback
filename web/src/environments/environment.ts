import { Environment } from './environment.model';

// The API is served same-origin under /api (Netlify proxy in production, see ADR-0002).
export const environment: Environment = {
  production: true,
  apiBaseUrl: '',
};
