import {
  EnvironmentProviders,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { AuthService } from './auth.service';

/**
 * Starts restoring the session from the refresh cookie without blocking startup. The app shows its
 * session loading screen meanwhile, and route guards wait for the restore to finish.
 */
export function provideAuth(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideAppInitializer(() => {
      void inject(AuthService).restoreSession();
    }),
  ]);
}
