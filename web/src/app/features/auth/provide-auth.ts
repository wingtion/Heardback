import {
  EnvironmentProviders,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { AuthService } from './auth.service';

/** Restores the session from the refresh cookie before the app renders its first route. */
export function provideAuth(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideAppInitializer(() => inject(AuthService).restoreSession()),
  ]);
}
