import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './features/auth/auth.service';
import { SessionLoading } from './shell/session-loading';

@Component({
  imports: [RouterOutlet, SessionLoading],
  selector: 'app-root',
  templateUrl: './app.html',
})
export class App {
  protected readonly auth = inject(AuthService);
}
